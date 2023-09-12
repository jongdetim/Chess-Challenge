using System;
using System.Linq;
using System.Collections.Generic;
using ChessChallenge.API;

// TODO:
// _________________________________________________________________
// track metrics (time, nodes, etc.)
// test if fixed:
//  a-b pruning interaction with transposition table
// why does move sorting not speed up search?
// early stopping (iterative deepening + clock)

// quiescence search
// threat move sorting
// better pawn evaluation
// separate early game / midgame based piecePositionValueTables
// tests & a way to measure performance
// turn into NegaScout by using a-b window of size 1
// condense code to < 1024 tokens
    // embed functions, use ternary operators, replace math.max and valuemax etc. with constant values

// change to hard values (0, 1, 2)
enum TTEntryType
{
    UpperBound,
    LowerBound,
    ExactValue
}

public class MyBot : IChessBot
{
    int NODES_VISITED; // #DEBUG
    int TABLE_HITS; // #DEBUG

    Dictionary<ulong, (int score, TTEntryType entryType, byte depth, Move bestMove)> transpositionTable = new();
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
        NODES_VISITED = 0; // #DEBUG
        TABLE_HITS = 0; // #DEBUG
        byte depth = 6;
        int color = board.IsWhiteToMove ? 1 : -1;
        int bestScore = 12345678; // #DEBUG

        // Console.WriteLine($"TEST pawn: {GetPositionScore(0, 35)}. (should be: 25)\n"); // #DEBUG
        // Console.WriteLine($"TEST king: {GetPositionScore(5, 63)}. (should be: -30)\n"); // #DEBUG
        // Console.WriteLine($"TEST queen: {GetPositionScore(4, 58)}. (should be: -10)\n"); // #DEBUG

        // (dont) Clear the transposition table at the beginning of each iteration (have to test if that's better than keeping it between searches)
        transpositionTable.Clear();

        // maybe we should call negamax from root node & return the move along with the score
            // or get move from transposition table
        // Move[] moves = board.GetLegalMoves();
        // moves = SortMoves(board, moves);
        // foreach (Move move in moves)
        // {
        //     board.MakeMove(move);
        //     int score = -Negamax(board, depth - 1, int.MinValue + 1, int.MaxValue - 1, -color);
        //     board.UndoMove(move);

        //     // Console.WriteLine($"score: {score}\n move: {move}");

        //     if (score > bestScore)
        //     {
        //         bestScore = score;
        //         bestMove = move;
        //     }
        // }

        bestScore = Negamax(board, depth, int.MinValue + 30, int.MaxValue - 30, color);

        // Iterative Deepening

        // for (byte i = 1; i <= depth; i++)
        // {
        //     // set a break if time runs out, based on timer
        //     // if (timer.MillisecondsElapsedThisTurn > 300 | bestScore > int.MaxValue - 30)
        //     if (timer.MillisecondsElapsedThisTurn > 300)
        //     {
        //         Console.WriteLine($"Time ran out at depth: {(byte)(i-1)}"); // #DEBUG
        //         depth = (byte)(i-1);
        //         break;
        //     }
        //     bestScore = Negamax(board, i, int.MinValue + 30, int.MaxValue - 30, color);
        // }



        Move[] pv = GetPrincipalVariation(board, depth); // #DEBUG
        Console.WriteLine($"Best score: {bestScore}\n Best move: {pv[0]}"); // #DEBUG
        Console.WriteLine($"principal variation:"); // #DEBUG
        foreach (Move move in pv) // #DEBUG
            Console.WriteLine(move); // #DEBUG
        Console.WriteLine($"Nodes visited: {NODES_VISITED}"); // #DEBUG
        Console.WriteLine($"Table hits: {TABLE_HITS}"); // #DEBUG
        Console.WriteLine($"Time elapsed: {timer.MillisecondsElapsedThisTurn} ms"); // #DEBUG
        Console.WriteLine($"Nodes per second: {NODES_VISITED / (timer.MillisecondsElapsedThisTurn / 1000.0)}"); // #DEBUG
        return pv[0];
    }

    Move[] GetPrincipalVariation(Board board, int depth) // #DEBUG
    { // #DEBUG
        Move[] principalVariation = new Move[depth]; // #DEBUG
        int validMovesCount = 0; // #DEBUG

        for (int currentDepth = 0; currentDepth < depth; currentDepth++) // #DEBUG
        { // #DEBUG
            ulong zobristKey = board.ZobristKey; // #DEBUG

            // can shorten this in unsafe way by using [indexing] instead of TryGetValue
            // if (transpositionTable.TryGetValue(zobristKey, out var test))
            //     Console.WriteLine("found position in table");
            if (transpositionTable.TryGetValue(zobristKey, out var entry) && entry.bestMove != Move.NullMove) // #DEBUG
            { // #DEBUG
                principalVariation[currentDepth] = entry.bestMove; // #DEBUG
                board.MakeMove(entry.bestMove); // #DEBUG
                validMovesCount++; // #DEBUG
            } // #DEBUG
            else // #DEBUG
                break; // Transposition table entry not found, or best move is null // #DEBUG
        } // #DEBUG

        // Undo any moves made during the backtracking // #DEBUG
        for (int i = validMovesCount - 1; i >= 0; i--) // #DEBUG
        { // #DEBUG
            board.UndoMove(principalVariation[i]); // #DEBUG
        } // #DEBUG

        // Console.WriteLine("pv moves found in table " + validMovesCount);
        return principalVariation; // #DEBUG
    } // #DEBUG


    Move[] SortMoves(Board board, Move[] moves)
    {
        Dictionary<Move, int> moveScores = new();
        // t-table first, then checkmate, then checks, then captures
            // do we really want to search checks before captures?

        // captures
        int movescore = 0;
        bool found = transpositionTable.TryGetValue(board.ZobristKey, out var parent);
        foreach (Move move in moves)
        {
            bool is_defended = board.SquareIsAttackedByOpponent(move.TargetSquare);
            board.MakeMove(move);
            // highest priority for eval entries
            // perhaps better to only priority the PV move?
            // bool found = transpositionTable.TryGetValue(board.ZobristKey, out var parent);
            // movescore = found ? parent.score : 0;

            if (found && parent.bestMove == move)
            {
                // this should not be possible without iterative deepening, right? it does happen though
                Console.WriteLine($"found move in table. move: {move}");
                Console.WriteLine(board.CreateDiagram());
                movescore = 1000006;

                Environment.Exit(0);
            }
            // this time we are searching for the child node, not the parent
            // else if (transpositionTable.TryGetValue(board.ZobristKey, out var entry))
            //     movescore = -entry.score;

            // else if (found && entry.entryType == TTEntryType.ExactValue)
            // else if (found)
            //     movescore = entry.score;
            // else // not found

            else
            {
                if (board.IsInCheckmate())
                    movescore = 10000007;
                else if (board.IsInCheck())
                    movescore = 100005;
                else if (move.IsCapture) // assumed to be independent of current board state.
                    // order move by MVVLVA value
                    // this might not work for promotions!!
                    movescore = pieceValues[(int)move.CapturePieceType - 1] - pieceValues[(int)move.MovePieceType - 1] + 1000;
                    if (is_defended)
                        movescore -= 800;
                else if (is_defended)
                    movescore = -50000;
                // else // look at lesser piece moves first
                //     movescore = -(int)move.MovePieceType - 1; 
                // add check for castling moves?
                // can add another sort for GetPositionScore where we subtract the position bonus
                // score for the start square from the target square


                // these might be overkill for sorting!!!
                // perspective for these are flipped, since we already made the move
                else
                {
                    // add piece square bonus
                    movescore += GetPositionScore((int)move.MovePieceType - 1, !board.IsWhiteToMove ? move.TargetSquare.Index : 63 - move.TargetSquare.Index) - 
                        GetPositionScore((int)move.MovePieceType - 1, !board.IsWhiteToMove ? move.StartSquare.Index : 63 - move.StartSquare.Index);
                // add mobility score
                // MOBILITY SCORE VALUES SHOULD NOT BE MULTIPLIED BY MOVE TYPE, BUT A DIFFERENT VALUE TABLE
                    // movescore += (int)move.MovePieceType * (BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(move.MovePieceType, move.TargetSquare, board, !board.IsWhiteToMove)) - 
                    //     BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(move.MovePieceType, move.StartSquare, board, !board.IsWhiteToMove)));

                }
            
            };
            moveScores[move] = movescore;
            board.UndoMove(move);
        }
        return moves.OrderByDescending(move => moveScores[move]).ToArray();;
    }

    // quiescence search that only searches captures and checks
    // int Quiescence(Board board, int depth, int alpha, int beta, int color)
    // {
    //     int stand_pat = EvaluateBoard(board, color) * color;
    //     if (depth == 0 || board.IsInCheckmate() || board.IsDraw())
    //         return stand_pat;

    //     if (stand_pat >= beta)
    //         return beta;
    //     if (alpha < stand_pat)
    //         alpha = stand_pat;

    //     Move[] moves = board.GetLegalMoves();
    //     moves = SortMoves(board, moves);
    //     foreach (Move move in moves)
    //     {
    //         if (!move.IsCapture && !board.SquareIsAttackedByOpponent(move.TargetSquare))
    //             continue;
    //         board.MakeMove(move);
    //         int score = -Quiescence(board, depth - 1, -beta, -alpha, -color);
    //         board.UndoMove(move);

    //         if (score >= beta)
    //             return beta;
    //         if (score > alpha)
    //             alpha = score;
    //     }
    //     return alpha;
    // }

    // failsoft alpha-beta pruning negamax search with transposition table
    int Negamax(Board board, byte depth, int alpha, int beta, int color)
    {
        int alpha_orig = alpha;
        ulong zobristKey = board.ZobristKey;

        NODES_VISITED++; // #DEBUG
        // SAME SEARCH DEPTH TRANSPOSITION TABLE HIT
        if (transpositionTable.TryGetValue(zobristKey, out var entry) && entry.depth >= depth)
        {
            TABLE_HITS++; // #DEBUG
            // Console.WriteLine($"SAME SEARCH DEPTH TRANSPOSITION TABLE HIT");
            if (entry.entryType == TTEntryType.ExactValue)
                return entry.score;
            if (entry.entryType == TTEntryType.LowerBound)
                alpha = Math.Max(alpha, entry.score);
            else if (entry.entryType == TTEntryType.UpperBound)
                beta = Math.Min(beta, entry.score);
            if (alpha >= beta)
                return entry.score;
        }
        // LEAF NODE
        if (depth == 0 || board.IsInCheckmate() || board.IsDraw())
        {
            // Console.WriteLine($"LEAF NODE. at depth:" + depth); // #DEBUG
            // Console.WriteLine($"LEAF NODE. is draw:" + board.IsDraw()); // #DEBUG
            // if (board.IsDraw()) // #DEBUG
            //     Console.WriteLine(board.CreateDiagram()); // #DEBUG
            // should we first check if board is in t-table?
            int score = EvaluateBoard(board, color, depth) * color;
            TTEntryType entryType = TTEntryType.ExactValue;

            if (score <= alpha)
                entryType = TTEntryType.UpperBound;
            else if (score >= beta)
                entryType = TTEntryType.LowerBound;

            transpositionTable[zobristKey] = (score, entryType, depth, Move.NullMove);
            return score;
        }

        // // SAME SEARCH DEPTH TRANSPOSITION TABLE HIT
        // if (transpositionTable.TryGetValue(zobristKey, out var entry) && entry.depth >= depth)
        // {
        //     if (entry.entryType == TTEntryType.ExactValue || entry.entryType == TTEntryType.LowerBound)
        //     {
        //         alpha = Math.Max(alpha, entry.score);
        //         if (alpha >= beta)
        //             return entry.score;
        //     }
        //     else if (entry.entryType == TTEntryType.UpperBound)
        //     {
        //         beta = Math.Min(beta, entry.score);
        //         if (alpha >= beta)
        //             return entry.score;
        //     }
        // }

        int bestScore = int.MinValue;
        Move bestMove = Move.NullMove;
        Move[] moves = board.GetLegalMoves();
        moves = SortMoves(board, moves);

        foreach (Move move in moves)
        {
            // Console.WriteLine(move); // #DEBUG
            board.MakeMove(move);
            Console.WriteLine("MOVE: " + move + " DEPTH: " + depth); // #DEBUG
            int score = -Negamax(board, (byte)(depth - 1), -beta, -alpha, -color);
            board.UndoMove(move);

            // bestScore = Math.Max(bestScore, score);
            alpha = Math.Max(alpha, score);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
            if (alpha >= beta)
            {
                // Beta cutoff (remaining moves get pruned)
                    // should we write to t-table here?
                // Console.WriteLine($"beta cutoff: remaining moves pruned. best score: {bestScore}");
                break;
            }

    
            // if (alpha >= beta)
            // {
            //     // Beta cutoff (remaining moves get pruned)
            //     TTEntryType entryType = TTEntryType.LowerBound;
            //     if (bestScore <= alpha)
            //         entryType = TTEntryType.UpperBound;
            //     transpositionTable[zobristKey] = (bestScore, entryType, depth, Move.NullMove);
            //     break;
            // }
        }

        TTEntryType finalEntryType = TTEntryType.ExactValue;
        if (bestScore <= alpha_orig)
            finalEntryType = TTEntryType.UpperBound;
        else if (bestScore >= beta)
            finalEntryType = TTEntryType.LowerBound;
        
        transpositionTable[zobristKey] = (bestScore, finalEntryType, depth, bestMove);

        System.Diagnostics.Debug.Assert(bestScore != int.MinValue); // #DEBUG
        System.Diagnostics.Debug.Assert(bestMove != Move.NullMove); // #DEBUG

        // Console.WriteLine("best move: " + bestMove); // #DEBUG
        // Console.WriteLine("at depth: " + depth); // #DEBUG
        return bestScore;
    }


    // int Negamax(Board board, int depth, int alpha, int beta, int color)
    // {
    //     if (depth == 0 || board.IsInCheckmate() || board.IsDraw())
    //     {
    //         int score = EvaluateBoard(board, color) * color;
    //         // this means that the stored move has a score from the perspective of the player who is about to move
    //         // should we instead always store the score from the perspective of white?
    //         transpositionTable[board.ZobristKey] = score;
    //         return score;
    //     }

    //     int bestScore = int.MinValue;
    //     Move[] moves = board.GetLegalMoves();
    //     moves = OrderMoveByMVVLVA(moves);
    //     foreach (Move move in moves)
    //     {
    //         board.MakeMove(move);
    //         int score = -Negamax(board, depth - 1, -beta, -alpha, -color);
    //         board.UndoMove(move);

    //         bestScore = Math.Max(bestScore, score);
    //         alpha = Math.Max(alpha, score);
    //         if (alpha >= beta)
    //             break; // Beta cutoff
    //     }
    //     // Console.WriteLine($"Depth: {depth + 1}, Eval: {bestScore}");
    //     return bestScore;
    // }

    // always from White's perspective
    int EvaluateBoard(Board board, int color, byte depth)
    {
        if (board.IsInCheckmate())
            // should this be multiplied by color, or always white's perspective?
            return (int.MinValue + 20 - depth) * color;
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
                score += sign * GetPositionScore(i % 6, sign == 1 ? piece.Square.Index : 63 - piece.Square.Index);
                // add mobility score 
                // NOTE: this incentivizes the king to have mobility too. is that good? i think so for now.
                // PROBLEM! all this does now is incentivize the queen to develop early, which is not good.
                score += sign * (int)piece.PieceType * BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, board, i < 6));
            }
        }
        // King safety evaluation.
        if (board.IsInCheck())
            score -= 50 * color;

        return score;
    }

}
