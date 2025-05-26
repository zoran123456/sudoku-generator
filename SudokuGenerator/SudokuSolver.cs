using System;
using System.Collections.Generic;

namespace SudokuGenerator
{
    /// <summary>
    /// Provides static methods to solve and generate Sudoku puzzles using backtracking.
    /// </summary>
    public static class SudokuSolver
    {
        private static readonly Random _random = new();
        private const int MaxGenerationAttempts = 100;
        private const int MaxRecursionDepth = 10000;

        /// <summary>
        /// Attempts to solve the given Sudoku grid using backtracking.
        /// </summary>
        /// <param name="grid">The SudokuGrid to solve.</param>
        /// <returns>True if a solution is found, false otherwise.</returns>
        public static bool Solve(SudokuGrid grid)
        {
            if (!grid.FindEmptyCell(out int row, out int col))
                return true; // No empty cell, puzzle solved

            for (int num = 1; num <= 9; num++)
            {
                if (grid.IsSafe(row, col, num))
                {
                    grid[row, col] = num;
                    if (Solve(grid))
                        return true;
                    grid[row, col] = 0; // Backtrack
                }
            }
            return false; // Trigger backtracking
        }

        /// <summary>
        /// Counts the number of solutions for the given Sudoku grid, up to a specified maximum.
        /// </summary>
        /// <param name="grid">The SudokuGrid to check.</param>
        /// <param name="maxSolutions">Maximum number of solutions to search for (e.g., 2 for uniqueness check).</param>
        /// <returns>The number of solutions found (up to maxSolutions).</returns>
        public static int CountSolutions(SudokuGrid grid, int maxSolutions = 2)
        {
            int count = 0;
            CountSolutionsRecursive(grid, ref count, maxSolutions);
            return count;
        }

        private static void CountSolutionsRecursive(SudokuGrid grid, ref int count, int maxSolutions)
        {
            if (count >= maxSolutions)
                return;
            if (!grid.FindEmptyCell(out int row, out int col))
            {
                count++;
                return;
            }
            for (int num = 1; num <= 9; num++)
            {
                if (grid.IsSafe(row, col, num))
                {
                    grid[row, col] = num;
                    CountSolutionsRecursive(grid, ref count, maxSolutions);
                    grid[row, col] = 0;
                }
            }
        }

        /// <summary>
        /// Generates a fully solved Sudoku grid using randomized backtracking.
        /// </summary>
        /// <param name="grid">The SudokuGrid to fill.</param>
        /// <returns>True if a solution is generated, false otherwise.</returns>
        public static bool GenerateSolvedGrid(SudokuGrid grid)
        {
            for (int attempt = 0; attempt < MaxGenerationAttempts; attempt++)
            {
                // Clear grid before each attempt
                for (int r = 0; r < 9; r++)
                    for (int c = 0; c < 9; c++)
                        grid[r, c] = 0;
                if (GenerateSolvedGridRecursive(grid, 0))
                    return true;
            }
            return false;
        }

        private static bool GenerateSolvedGridRecursive(SudokuGrid grid, int depth)
        {
            if (depth > MaxRecursionDepth)
                return false;
            if (!grid.FindEmptyCell(out int row, out int col))
                return true;
            var nums = new List<int>(9);
            for (int i = 1; i <= 9; i++) nums.Add(i);
            Shuffle(nums);
            foreach (int num in nums)
            {
                if (grid.IsSafe(row, col, num))
                {
                    grid[row, col] = num;
                    if (GenerateSolvedGridRecursive(grid, depth + 1))
                        return true;
                    grid[row, col] = 0;
                }
            }
            return false;
        }

        /// <summary>
        /// Creates a playable Sudoku puzzle by removing clues from a solved grid while ensuring uniqueness.
        /// </summary>
        /// <param name="grid">A fully solved SudokuGrid. This grid will be modified in-place to become a puzzle.</param>
        public static void DigHoles(SudokuGrid grid)
        {
            // Create a list of all cell positions
            var cells = new List<(int row, int col)>();
            for (int row = 0; row < 9; row++)
                for (int col = 0; col < 9; col++)
                    cells.Add((row, col));
            Shuffle(cells);

            foreach (var (row, col) in cells)
            {
                int backup = grid[row, col];
                grid[row, col] = 0;
                // Check uniqueness after removal
                if (CountSolutions(grid, 2) != 1)
                {
                    grid[row, col] = backup; // Undo removal if not unique
                }
            }
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
