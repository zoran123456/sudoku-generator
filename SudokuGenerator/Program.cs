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

            // Generate a fully solved Sudoku grid
            var grid = new SudokuGrid();
            if (SudokuSolver.GenerateSolvedGrid(grid))
            {
                Console.WriteLine("Sample solved Sudoku grid:");
                SudokuDrawer.Print(grid);
                Console.WriteLine();

                // Create a puzzle by removing clues, using the selected difficulty
                SudokuSolver.DigHoles(grid, difficulty);
                Console.WriteLine($"Playable Sudoku puzzle (unique solution, difficulty {difficulty}):");
                SudokuDrawer.Print(grid);
            }
            else
            {
                Console.WriteLine("Failed to generate a solved Sudoku grid after multiple attempts.");
            }
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
