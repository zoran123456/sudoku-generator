using System;
using System.Collections.Generic;

namespace SudokuGenerator
{
    /// <summary>
    /// Represents a 9x9 Sudoku grid and provides methods for manipulation and validation.
    /// </summary>
    public class SudokuGrid
    {
        /// <summary>
        /// The 9x9 grid of integers representing the Sudoku board. 0 means empty.
        /// </summary>
        private readonly int[,] _grid;

        /// <summary>
        /// Initializes a new instance of the <see cref="SudokuGrid"/> class.
        /// </summary>
        public SudokuGrid()
        {
            _grid = new int[9, 9];
        }

        /// <summary>
        /// Gets or sets the value at the specified row and column.
        /// </summary>
        /// <param name="row">The row index (0-8).</param>
        /// <param name="col">The column index (0-8).</param>
        /// <returns>The value at the specified cell.</returns>
        public int this[int row, int col]
        {
            get => _grid[row, col];
            set => _grid[row, col] = value;
        }

        /// <summary>
        /// Checks if placing a number at the given row and column is valid according to Sudoku rules.
        /// </summary>
        /// <param name="row">Row index (0-8).</param>
        /// <param name="col">Column index (0-8).</param>
        /// <param name="num">Number to check (1-9).</param>
        /// <returns>True if the placement is valid, false otherwise.</returns>
        public bool IsSafe(int row, int col, int num)
        {
            // Check row and column
            for (int i = 0; i < 9; i++)
            {
                if (_grid[row, i] == num || _grid[i, col] == num)
                    return false;
            }
            // Check 3x3 subgrid
            int startRow = row - row % 3;
            int startCol = col - col % 3;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (_grid[startRow + i, startCol + j] == num)
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Finds the next empty cell in the grid.
        /// </summary>
        /// <param name="row">Output row index of the empty cell.</param>
        /// <param name="col">Output column index of the empty cell.</param>
        /// <returns>True if an empty cell is found, false otherwise.</returns>
        public bool FindEmptyCell(out int row, out int col)
        {
            for (row = 0; row < 9; row++)
            {
                for (col = 0; col < 9; col++)
                {
                    if (_grid[row, col] == 0)
                        return true;
                }
            }
            row = -1;
            col = -1;
            return false;
        }

        /// <summary>
        /// Gets a list of possible candidates for a given empty cell.
        /// </summary>
        /// <param name="row">Row index (0-8).</param>
        /// <param name="col">Column index (0-8).</param>
        /// <returns>List of valid candidate numbers for the cell.</returns>
        public List<int> GetCandidates(int row, int col)
        {
            var candidates = new List<int>();
            if (_grid[row, col] != 0)
                return candidates;
            for (int num = 1; num <= 9; num++)
            {
                if (IsSafe(row, col, num))
                    candidates.Add(num);
            }
            return candidates;
        }
    }
}
