﻿using System;
// using System.Linq;
using System.Collections.Generic;
using ChessChallenge.API;

// TODO:
// _________________________________________________________________
// principal variation search & better move ordering
<<<<<<< Updated upstream
=======
// early stopping (iterative deepening + clock)
// separate early game / midgame based piecePositionValueTables and pieceValues
// better pawn evaluation
>>>>>>> Stashed changes
// fix a-b pruning interaction with transposition table
// early stopping (iterative deepening + clock)
// quiescence search
// better pawn evaluation
// separate early game / midgame based piecePositionValueTables
// tests & a way to measure performance
// turn into NegaScout by using a-b window of size 1
// condense code to < 1024 tokens

public class MyBot : IChessBot
{
    Dictionary<ulong, int> transpositionTable = new();
    static int[] pieceValues = {100, 320, 330, 500, 900, 20000};

    // every 32 bits is a row. every 64-bit int here is 2 rows
    static ulong[] piecePositionValueTable = {
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

        Console.WriteLine($"TEST pawn: {GetPositionScore(0, 35)}. (should be: 25)\n"); // #DEBUG
        Console.WriteLine($"TEST king: {GetPositionScore(5, 63)}. (should be: -30)\n"); // #DEBUG
        Console.WriteLine($"TEST queen: {GetPositionScore(4, 58)}. (should be: -10)\n"); // #DEBUG

        // Clear the transposition table at the beginning of each iteration (have to test if that's better than keeping it between searches)
        // transpositionTable.Clear();

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
        Console.WriteLine($"Best score: {bestScore}\n Best move: {bestMove}"); // #DEBUG
        return bestMove;
    }

    // order move by MVVLVA value
    int GetMVVLVAValue(Move move) {
        if (move.IsCapture) {
            return pieceValues[(int)move.CapturePieceType - 1] - pieceValues[(int)move.MovePieceType - 1];
        }
        return -pieceValues[(int)move.MovePieceType - 1];
    }

    int CompareMovesByMVVLVA(Move move1, Move move2) =>
        GetMVVLVAValue(move2).CompareTo(GetMVVLVAValue(move1));

    Move[] OrderMoveByMVVLVA(Move[] moves)
    {
        Array.Sort(moves, CompareMovesByMVVLVA);
        return moves;
    }

int Negamax(Board board, int depth, int alpha, int beta, int color)
{
    ulong zobristKey = board.ZobristKey;
    if (depth == 0 || board.IsInCheckmate() || board.IsDraw())
    {
        int score = EvaluateBoard(board, color) * color;
        TTEntryType entryType = TTEntryType.ExactValue;

        if (score <= alpha)
            entryType = TTEntryType.UpperBound;
        else if (score >= beta)
            entryType = TTEntryType.LowerBound;

        transpositionTable[zobristKey] = (score, entryType, depth);
        return score;
    }

    if (transpositionTable.TryGetValue(zobristKey, out var entry) && entry.depth >= depth)
    {
        if (entry.entryType == TTEntryType.ExactValue || entry.entryType == TTEntryType.LowerBound)
        {
            alpha = Math.Max(alpha, entry.score);
            if (alpha >= beta)
                return entry.score;
        }
        else if (entry.entryType == TTEntryType.UpperBound)
        {
            beta = Math.Min(beta, entry.score);
            if (alpha >= beta)
                return entry.score;
        }
    }

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
        {
            // Beta cutoff
            TTEntryType entryType = TTEntryType.LowerBound;
            if (maxEval <= alpha)
                entryType = TTEntryType.UpperBound;
            transpositionTable[zobristKey] = (maxEval, entryType, depth);
            break;
        }
    }

    TTEntryType finalEntryType = TTEntryType.ExactValue;
    if (maxEval <= alpha)
        finalEntryType = TTEntryType.UpperBound;
    else if (maxEval >= beta)
        finalEntryType = TTEntryType.LowerBound;

    transpositionTable[zobristKey] = (maxEval, finalEntryType, depth);
    return maxEval;
}


    int Negamax(Board board, int depth, int alpha, int beta, int color)
    {
        if (depth == 0 || board.IsInCheckmate() || board.IsDraw())
        {
            int score = EvaluateBoard(board, color) * color;
            // this means that the stored move has a score from the perspective of the player who is about to move
            // should we instead always store the score from the perspective of white?
            transpositionTable[board.ZobristKey] = score;
            return score;
        }

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
        ulong boardHash = board.ZobristKey;

        if (transpositionTable.ContainsKey(boardHash))
            return transpositionTable[boardHash];

        if (board.IsInCheckmate())
<<<<<<< Updated upstream
        {
            // should these be multiplied by color, or always white's perspective?
            transpositionTable[boardHash] = (int.MinValue + 1) * color;
=======
>>>>>>> Stashed changes
            return (int.MinValue + 1) * color;

        if (board.IsDraw())
            return 0;

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

        return score;
    }
}
