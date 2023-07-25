using System;
using System.Linq;
using System.Collections.Generic;
using ChessChallenge.API;

// TODO:
// _________________________________________________________
// move ordering
// early stopping (iterative deepening + clock)
// quiescence search
// fix a-b pruning interaction with transposition table
// tests & a way to measure performance
// condense code to < 1024 tokens

public class MyBot : IChessBot
{
    Dictionary<ulong, int> transpositionTable = new();
    int[] pieceValues = {100, 320, 330, 500, 900, 20000};
    // values for WHITE player. for BLACK player, index needs to be mirrored or matrix 180 rotated
    static int[] pawnTable = { 0, 0, 0, 0, 0, 0, 0, 0,
                        5, 10, 10, -20, -20, 10, 10, 5,
                        5, -5, -10, 0, 0, -10, -5, 5,
                        0, 0, 0, 20, 20, 0, 0, 0,
                        5, 5, 10, 25, 25, 10, 5, 5,
                        10, 10, 20, 30, 30, 20, 10, 10,
                        50, 50, 50, 50, 50, 50, 50, 50,
                        0, 0, 0, 0, 0, 0, 0, 0 };
    static int[] knightTable = {
    -50, -40, -30, -30, -30, -30, -40, -50,
    -40, -20, 0, 5, 5, 0, -20, -40,
    -30, 5, 10, 15, 15, 10, 5, -30,
    -30, 0, 15, 20, 20, 15, 0, -30,
    -30, 5, 15, 20, 20, 15, 5, -30,
    -30, 0, 10, 15, 15, 10, 0, -30,
    -40, -20, 0, 0, 0, 0, -20, -40,
    -50, -40, -30, -30, -30, -30, -40, -50};

    static int[] bishopTable = {
        -20, -10, -10, -10, -10, -10, -10, -20,
        -10, 5, 0, 0, 0, 0, 5, -10,
        -10, 10, 10, 10, 10, 10, 10, -10,
        -10, 0, 10, 10, 10, 10, 0, -10,
        -10, 5, 5, 10, 10, 5, 5, -10,
        -10, 0, 5, 10, 10, 5, 0, -10,
        -10, 0, 0, 0, 0, 0, 0, -10,
        -20, -10, -10, -10, -10, -10, -10, -20};

    static int[] rookTable = {
        0, 0, 0, 5, 5, 0, 0, 0,
        -5, 0, 0, 0, 0, 0, 0, -5,
        -5, 0, 0, 0, 0, 0, 0, -5,
        -5, 0, 0, 0, 0, 0, 0, -5,
        -5, 0, 0, 0, 0, 0, 0, -5,
        -5, 0, 0, 0, 0, 0, 0, -5,
        5, 10, 10, 10, 10, 10, 10, 5,
        0, 0, 0, 0, 0, 0, 0, 0};

    static int[] queenTable = {
        -20, -10, -10, -5, -5, -10, -10, -20,
        -10, 0, 0, 0, 0, 0, 0, -10,
        -10, 5, 5, 5, 5, 5, 0, -10,
        0, 0, 5, 5, 5, 5, 0, -5,
        -5, 0, 5, 5, 5, 5, 0, -5,
        -10, 0, 5, 5, 5, 5, 0, -10,
        -10, 0, 0, 0, 0, 0, 0, -10,
        -20, -10, -10, -5, -5, -10, -10, -20};

    static int[] kingTable = {
        20, 30, 10, 0, 0, 10, 30, 20,
        20, 20, 0, 0, 0, 0, 20, 20,
        -10, -20, -20, -20, -20, -20, -20, -10,
        -20, -30, -30, -40, -40, -30, -30, -20,
        -30, -40, -40, -50, -50, -40, -40, -30,
        -30, -40, -40, -50, -50, -40, -40, -30,
        -30, -40, -40, -50, -50, -40, -40, -30,
        -30, -40, -40, -50, -50, -40, -40, -30};

    int[][] piecePositionTable = {pawnTable, knightTable, bishopTable, rookTable, queenTable, kingTable};
    
    public Move Think(Board board, Timer timer)
    {
        int bestScore = int.MinValue;
        Move bestMove = default;
        int depth = 4;
        int color = board.IsWhiteToMove ? 1 : -1;

        // Move[] moves = board.GetLegalMoves();
        // Array.Sort(moves, SortByThreats);
        // foreach (Move move in moves)
        Move[] captureMoves = board.GetLegalMoves(true);
        foreach (Move captureMove in captureMoves)
        {
            board.MakeMove(captureMove);
            int score = -Negamax(board, depth - 1, int.MinValue + 1, int.MaxValue - 1, -color);
            board.UndoMove(captureMove);

            // Console.WriteLine($"score: {score}\n move: {move}");

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = captureMove;
            }
        }

        foreach (Move move in board.GetLegalMoves()) if (!captureMoves.Contains(move))
        {
            board.MakeMove(move);
            int score = -Negamax(board, depth - 1, int.MinValue + 1, int.MaxValue - 1, -color);
            board.UndoMove(move);

            // Console.WriteLine($"score: {score}\n move: {move}");

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }
        Console.WriteLine($"Best score: {bestScore}\n Best move: {bestMove}");
        return bestMove;
    }

    public int Negamax(Board board, int depth, int alpha, int beta, int color)
    {
        if (depth == 0 || board.IsInCheckmate() || board.IsDraw())
            return color * EvaluateBoard(board, color);

        int maxEval = int.MinValue;
        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            int score = -Negamax(board, depth - 1, -beta, -alpha, -color);
            board.UndoMove(move);

            maxEval = Math.Max(maxEval, score);
            alpha = Math.Max(alpha, score);
            if (alpha >= beta)
                break; // Beta cutoff
        }
        // Console.WriteLine($"Depth: {depth + 1}, Eval: {maxEval}");
        return maxEval;
    }

    public int EvaluateBoard(Board board, int color)
    {
        // doesn't have to take into account color, should always be from white's perspective
        ulong boardHash = board.ZobristKey;
        if (transpositionTable.ContainsKey(boardHash))
            return transpositionTable[boardHash];

        if (board.IsInCheckmate())
        {
            transpositionTable[boardHash] = (int.MinValue + 1) * color;
            return (int.MinValue + 1) * color;
        }
        if (board.IsDraw())
        {
            transpositionTable[boardHash] = 0;
            return 0;
        }

        // count piece values
        int score = 0;
        PieceList[] piecelists = board.GetAllPieceLists();
        for (int i = 0; i < 12; i++)
        {
            int sign = i < 6 ? 1 : -1;
            score += sign * pieceValues[i % 6] * piecelists[i].Count;

            foreach (Piece piece in piecelists[i])
            {
                // need to reverse index for black player
                int index = sign == 1 ? piece.Square.Index : 63 - piece.Square.Index;
                score += sign * piecePositionTable[i % 6][index];
            }
        }
        // King safety evaluation.
        if (board.IsInCheck())
            score += 50 * color;

        // should score be reversed here as well?
        transpositionTable[boardHash] = score;
        return score;
    }
}
//     public int SortByThreats(Move move1, Move move2)
//     {
//         return move2.IsCapture - move1.Threats;
//     }
// }