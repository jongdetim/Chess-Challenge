using System;
// using System.Linq;
using System.Collections.Generic;
using ChessChallenge.API;

// TODO:
// _________________________________________________________________
// quiescence search
// early stopping (iterative deepening + clock) & principal variation search
// fix a-b pruning interaction with transposition table
// tests & a way to measure performance
// turn into NegaScout by using a-b window of size 1
// condense code to < 1024 tokens

public class MyBot : IChessBot
{
    Dictionary<ulong, int> transpositionTable = new();
    int[] pieceValues = {100, 320, 330, 500, 900, 20000};

    // ulong[,] piecePositionValueTable =
    // {
    // {0x0000000000000000, 0x050A0AECEC0A0A05, 0x05FBF600F6FB0505, 0x0000001414000000, 0x05050A19190A0505, 0x0A0A141E1E140A0A, 0x3232323232323232, 0x0000000000000000},
    // {0xCEC8E2E2E2E2C8CE, 0xC8D800050500D8C8, 0xE2050A0F0F0A05E2, 0xE2000F1414140FE2, 0xE2051514141505E2, 0xE20005100F1005E2, 0xC8D80000FF00D8C8, 0xCEC8E2E2E2E2C8CE},
    // {0xECEAEAEAEAEAEAEA, 0xEAF500000000F5EA, 0xEAF5F5F5F5F5F5EA, 0xEA00F5F5F5F500EA, 0xF50505F5F50505F5, 0xF50005F5F50500F5, 0xF50000FFFFFF00F5, 0xECEAEAEAEAEAEAEA},
    // {0x0000000505000000, 0xFB000000000000FB, 0xFB000000000000FB, 0xFB000000000000FB, 0xFB000000000000FB, 0xFB000000000000FB, 0x050AFAFAFAFAFA05, 0x0000050505000000},
    // {0xECEAEAA5A5AAEAEA, 0xEAF500050505F5EA, 0xF50005050505F5FB, 0xFB00555555FFFBFB, 0xFBFF55555500FBFB, 0xFA005555550005FA, 0xFA00FFFFFFFF00FA, 0xECEAA5A5A5A5EAEA},
    // {0x141E0A00000A1E14, 0x1414000000001414, 0xF6E6E6E6E6E6E6F6, 0xECE2E2D8D8E2E2EC, 0xD8D8D8C8C8D8D8D8, 0xD8D8D8C8C8D8D8D8, 0xD8D8D8C8C8D8D8D8, 0x1414000014141414}
    // };
    int GetPositionScore(int pieceType, int index) => (sbyte)((piecePositionValueTable[pieceType, index / 8] >> (8 * (index % 8))) & 0xFF);

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
        // Move[] captureMoves = board.GetLegalMoves(true);
        // foreach (Move captureMove in captureMoves)
        // {
        //     board.MakeMove(captureMove);
        //     int score = -Negamax(board, depth - 1, int.MinValue + 1, int.MaxValue - 1, -color);
        //     board.UndoMove(captureMove);

        //     // Console.WriteLine($"score: {score}\n move: {move}");

        //     if (score > bestScore)
        //     {
        //         bestScore = score;
        //         bestMove = captureMove;
        //     }
        // }

        Console.WriteLine($"TEST pawn: {GetPositionScore(0, 12)}\n");
        Console.WriteLine($"TEST knight: {GetPositionScore(1, 0)}\n");

        Move[] moves = board.GetLegalMoves();
        moves = OrderMoveByMVVLVA(moves);
        foreach (Move move in moves)
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

    // order move by MVVLVA value
    int GetMVVLVAValue(Move move) {
        if (move.IsCapture) {
            return pieceValues[(int)move.CapturePieceType - 1] - pieceValues[(int)move.MovePieceType - 1];
        }
        return -pieceValues[(int)move.MovePieceType - 1];
    }

    // Move[] OrderMoveByMVVLVA(Move[] moves) {
    //     Array.Sort(moves, (move1, move2) => {
    //         int mvvLvaValue1 = GetMVVLVAValue(move1);
    //         int mvvLvaValue2 = GetMVVLVAValue(move2);
    //         return mvvLvaValue2.CompareTo(mvvLvaValue1);
    //     });
    //     return moves;
    // }

    int CompareMovesByMVVLVA(Move move1, Move move2)
    {
        int value1 = GetMVVLVAValue(move1);
        int value2 = GetMVVLVAValue(move2);

        return value2.CompareTo(value1);
    }

    Move[] OrderMoveByMVVLVA(Move[] moves)
    {
        Array.Sort(moves, CompareMovesByMVVLVA);

        return moves;
    }

    int Negamax(Board board, int depth, int alpha, int beta, int color)
    {
        if (depth == 0 || board.IsInCheckmate() || board.IsDraw())
            return color * EvaluateBoard(board, color);

        int maxEval = int.MinValue;
        Move[] moves = board.GetLegalMoves();
        moves = OrderMoveByMVVLVA(moves);
        foreach (Move move in moves)
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

    int EvaluateBoard(Board board, int color)
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