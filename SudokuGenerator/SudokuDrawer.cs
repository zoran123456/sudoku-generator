namespace SudokuGenerator
{
    public static class SudokuDrawer
    {
        /// <summary>
        /// Prints the Sudoku grid to the console in a formatted way.
        /// </summary>
        /// <param name="grid">The SudokuGrid to print.</param>
        public static void Print(SudokuGrid grid)
        {
            Console.WriteLine("+---+---+---+");
            for (int i = 0; i < 9; i++)
            {
                Console.Write("|");
                for (int j = 0; j < 9; j++)
                {
                    // Print digit or single space for empty
                    Console.Write(grid[i, j] == 0 ? " " : grid[i, j].ToString());

                    // Block border after each third digit
                    if ((j + 1) % 3 == 0)
                        Console.Write("|");
                }
                Console.WriteLine();
                // Line border after each third row
                if ((i + 1) % 3 == 0)
                    Console.WriteLine("+---+---+---+");
            }
        }
    }
}
