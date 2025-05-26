namespace SudokuGenerator
{
    /// <summary>
    /// Provides backtracking and solution counting for Sudoku puzzles,
    /// with randomized choices to produce varied grids.
    /// </summary>
    public static class SudokuBacktrackingSolver
    {
        private const int MaxGenerationAttempts = 100;
        private static readonly Random _rand = new();

        public static bool GenerateSolvedGrid(SudokuGrid grid)
        {
            for (int attempt = 0; attempt < MaxGenerationAttempts; attempt++)
            {
                ClearGrid(grid);
                if (SolveBacktrack(grid)) return true;
            }
            return false;
        }

        public static int CountSolutions(SudokuGrid grid, int max = 2)
        {
            int cnt = 0;
            CountSolutionsRecursive(grid, ref cnt, max);
            return cnt;
        }

        private static void ClearGrid(SudokuGrid g)
        {
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    g[r, c] = 0;
        }

        private static bool SolveBacktrack(SudokuGrid g)
        {
            if (!g.FindEmptyCell(out int r, out int c))
                return true;

            // shuffle 1–9
            foreach (int n in GetShuffledNumbers())
            {
                if (!g.IsSafe(r, c, n)) continue;
                g[r, c] = n;
                if (SolveBacktrack(g)) return true;
                g[r, c] = 0;
            }

            return false;
        }

        private static void CountSolutionsRecursive(SudokuGrid g, ref int cnt, int max)
        {
            if (cnt >= max) return;
            if (!g.FindEmptyCell(out int r, out int c))
            {
                cnt++;
                return;
            }

            foreach (int n in GetShuffledNumbers())
            {
                if (!g.IsSafe(r, c, n)) continue;
                g[r, c] = n;
                CountSolutionsRecursive(g, ref cnt, max);
                g[r, c] = 0;
                if (cnt >= max) return;
            }
        }

        /// <summary>
        /// Returns the numbers 1–9 in a random order.
        /// </summary>
        private static int[] GetShuffledNumbers()
        {
            return [.. Enumerable
                .Range(1, 9)
                .OrderBy(_ => _rand.Next())];
        }
    }
}