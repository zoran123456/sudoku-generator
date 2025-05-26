namespace SudokuGenerator
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            int diff = PromptDifficulty();

            if (SudokuSolver.TryGeneratePuzzle(diff, out var puzzle, out var info))
            {
                Console.WriteLine($"Calibrated puzzle for level {diff} (needs {info.Hardest}):");
                SudokuDrawer.Print(puzzle);

                var solution = info.Solution;
                SudokuDrawer.Print(solution);

            }
            else
            {
                Console.WriteLine("Failed to generate a puzzle matching that difficulty after several tries.");
            }
        }

        private static int PromptDifficulty()
        {
            int d; string? input;
            do
            {
                Console.Write("Select difficulty level (1-Easy … 5-Hard): ");
                input = Console.ReadLine();
            } while (!int.TryParse(input, out d) || d < 1 || d > 5);
            return d;
        }
    }
}
