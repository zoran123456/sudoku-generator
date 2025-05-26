using System;
using System.Collections.Generic;

namespace SudokuGenerator
{
    /// <summary>
    /// Provides static methods to solve Sudoku puzzles using backtracking.
    /// </summary>
    public static class SudokuSolver
    {
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
    }
}
