using System;

namespace SudokuGenerator
{
    /// <summary>
    /// Represents a 9x9 Sudoku grid and provides methods for manipulation and display.
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
    }
}
