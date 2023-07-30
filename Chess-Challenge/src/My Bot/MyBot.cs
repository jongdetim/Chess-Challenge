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

    ulong[] piecePositionValueTable = {
        0x00000000050A0AEC, 0x05FBF60000000014, 0x05050A190A0A141E, 0x3232323200000000, // pawns
        0xCED8E2E2D8EC0005, 0xE2050A0FE2000F14, 0xE2050F14E2000A0F, 0xD8EC0000CED8E2E2, // knights
        0xECF6F6F6F6050000, 0xF60A0A0AF6000A0A, 0xF605050AF600050A, 0xF6000000ECF6F6F6, // bishops
        0x00000005FB000000, 0xFB000000FB000000, 0xFB000000FB000000, 0x050A0A0A00000000, // rooks
        0xECF6F6FBF6000000, 0xF605050500000505, 0xFB000505F6000505, 0xF6000000ECF6F6FB, // queens
        0x141E0A0014140000, 0xF6ECECECECE2E2D8, 0xE2D8D8CEE2D8D8CE, 0xE2D8D8CEE2D8D8CE  // kings
    };

    int GetPositionScore(int pieceType, int index) =>
        (sbyte)((piecePositionValueTable[pieceType * 4 + index / 16] >> (8 * (7 - (index % 8 < 4 ? index % 8 : 7 - index % 8) + index % 16 / 8 * 4))) & 0xFF);
    
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

        Console.WriteLine($"TEST pawn: {GetPositionScore(0, 35)}. (should be: 25)\n");
        Console.WriteLine($"TEST king: {GetPositionScore(5, 63)}. (should be: -30)\n");
        Console.WriteLine($"TEST queen: {GetPositionScore(4, 58)}. (should be: -10)\n");

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
                score += sign * GetPositionScore(i % 6, index);
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