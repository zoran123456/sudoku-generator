using System;
using System.Collections.Generic;

namespace SudokuGenerator
{
    /// <summary>
    /// Provides static methods to solve and generate Sudoku puzzles using backtracking and basic logical strategies.
    /// </summary>
    public static class SudokuSolver
    {
        private static readonly Random _random = new();
        private const int MaxGenerationAttempts = 100;
        private const int MaxRecursionDepth = 10000;

        #region Backtracking Solver
        /// <summary>
        /// Attempts to solve the given Sudoku grid using backtracking.
        /// </summary>
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
            return false;
        }

        /// <summary>
        /// Counts the number of solutions for the given Sudoku grid, up to a specified maximum.
        /// </summary>
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
        #endregion

        #region Logical Solver (Singles)
        /// <summary>
        /// Applies naked and hidden singles iteratively until no more placements are possible.
        /// Returns true if the grid is completely filled using only singles.
        /// </summary>
        public static bool SolveWithSingles(SudokuGrid grid)
        {
            bool progress;
            do
            {
                progress = false;
                if (ApplyNakedSingles(grid)) progress = true;
                if (ApplyHiddenSingles(grid)) progress = true;
            }
            while (progress);

            // Check if solved
            return !grid.FindEmptyCell(out _, out _);
        }

        // Finds cells with only one candidate and fills them.
        private static bool ApplyNakedSingles(SudokuGrid grid)
        {
            bool anyPlaced = false;
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    if (grid[row, col] != 0)
                        continue;

                    var candidates = new List<int>();
                    for (int num = 1; num <= 9; num++)
                    {
                        if (grid.IsSafe(row, col, num))
                            candidates.Add(num);
                    }

                    if (candidates.Count == 1)
                    {
                        grid[row, col] = candidates[0];
                        anyPlaced = true;
                    }
                }
            }
            return anyPlaced;
        }

        // Finds hidden singles in each unit and fills them.
        private static bool ApplyHiddenSingles(SudokuGrid grid)
        {
            bool anyPlaced = false;

            // Check rows
            for (int row = 0; row < 9; row++)
            {
                anyPlaced |= FindAndPlaceHiddenSingles(grid, GetRowCells(row));
            }
            // Check columns
            for (int col = 0; col < 9; col++)
            {
                anyPlaced |= FindAndPlaceHiddenSingles(grid, GetColumnCells(col));
            }
            // Check 3x3 blocks
            for (int br = 0; br < 3; br++)
            {
                for (int bc = 0; bc < 3; bc++)
                {
                    anyPlaced |= FindAndPlaceHiddenSingles(grid, GetBlockCells(br * 3, bc * 3));
                }
            }

            return anyPlaced;
        }

        // Helper to find and place hidden singles in a given unit of cells
        private static bool FindAndPlaceHiddenSingles(SudokuGrid grid, List<(int row, int col)> cells)
        {
            bool placed = false;
            for (int num = 1; num <= 9; num++)
            {
                int count = 0;
                (int row, int col) last = default;
                foreach (var (r, c) in cells)
                {
                    if (grid[r, c] == 0 && grid.IsSafe(r, c, num))
                    {
                        count++;
                        last = (r, c);
                    }
                }
                if (count == 1)
                {
                    grid[last.row, last.col] = num;
                    placed = true;
                }
            }
            return placed;
        }

        // Helpers to list cells in units
        private static List<(int row, int col)> GetRowCells(int row)
        {
            var cells = new List<(int, int)>();
            for (int c = 0; c < 9; c++) cells.Add((row, c));
            return cells;
        }

        private static List<(int row, int col)> GetColumnCells(int col)
        {
            var cells = new List<(int, int)>();
            for (int r = 0; r < 9; r++) cells.Add((r, col));
            return cells;
        }

        private static List<(int row, int col)> GetBlockCells(int startRow, int startCol)
        {
            var cells = new List<(int, int)>();
            for (int r = startRow; r < startRow + 3; r++)
                for (int c = startCol; c < startCol + 3; c++)
                    cells.Add((r, c));
            return cells;
        }
        #endregion

        #region Generator
        /// <summary>
        /// Generates a fully solved Sudoku grid using randomized backtracking.
        /// </summary>
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
        public static void DigHoles(SudokuGrid grid, int difficulty)
        {
            var clueRanges = new (int min, int max)[]
            {
                (36, 40), // Easy
                (32, 35), // Normal
                (28, 31), // Medium
                (22, 27), // Hard
                (17, 21)  // Expert
            };
            int minClues = clueRanges[Math.Clamp(difficulty - 1, 0, 4)].min;
            int maxClues = clueRanges[Math.Clamp(difficulty - 1, 0, 4)].max;
            int targetClues = _random.Next(minClues, maxClues + 1);

            var cells = new List<(int row, int col)>();
            for (int row = 0; row < 9; row++)
                for (int col = 0; col < 9; col++)
                    cells.Add((row, col));
            Shuffle(cells);

            int cluesLeft = 81;
            foreach (var (row, col) in cells)
            {
                if (cluesLeft <= targetClues)
                    break;
                int backup = grid[row, col];
                grid[row, col] = 0;
                if (CountSolutions(grid, 2) != 1)
                {
                    grid[row, col] = backup;
                }
                else
                {
                    cluesLeft--;
                }
            }
        }
        #endregion

        #region Utilities
        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
        #endregion
    }
}
