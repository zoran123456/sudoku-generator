﻿namespace SudokuGenerator
{
    /// <summary>
    /// Facade for Sudoku puzzle generation and solving.
    /// </summary>
    public static class SudokuSolver
    {
        public static bool TryGeneratePuzzle(int difficulty,
                                             out SudokuGrid puzzle,
                                             out DifficultyAnalysis analysis,
                                             int maxAttempts = 10000)
            => SudokuPuzzleGenerator.TryGeneratePuzzle(difficulty, out puzzle, out analysis, maxAttempts);

        public static bool SolveWithLogic(SudokuGrid grid, out SudokuGrid solution)
            => SudokuLogicSolver.SolveWithLogic(grid, out solution!);
    }
}
