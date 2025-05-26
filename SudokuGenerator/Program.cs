using System;

namespace SudokuGenerator
{
    internal class Program
    {
        /// <summary>
        /// Entry point for the Sudoku Generator application.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        static void Main(string[] args)
        {
            int difficulty = PromptDifficulty();

            // Initialize an empty Sudoku grid for now
            var grid = new SudokuGrid();
            SudokuSolver.Solve(grid); // Solve the grid to fill it with a valid Sudoku solution

            SudokuDrawer.Print(grid);
        }

        /// <summary>
        /// Prompts the user to select a difficulty level (1-5).
        /// </summary>
        /// <returns>The selected difficulty level as an integer.</returns>
        private static int PromptDifficulty()
        {
            int difficulty;
            string? input;
            do
            {
                Console.Write("Select difficulty level (1-Easy to 5-Hard): ");
                input = Console.ReadLine();
            } while (!int.TryParse(input, out difficulty) || difficulty < 1 || difficulty > 5);
            return difficulty;
        }
    }
}
