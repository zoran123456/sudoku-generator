namespace SudokuGenerator
{
    /// <summary>
    /// Provides puzzle generation logic for Sudoku.
    /// </summary>
    public static class SudokuPuzzleGenerator
    {
        private static readonly Random _random = new();

        public static bool TryGeneratePuzzle(int difficulty,
                                     out SudokuGrid? puzzle,
                                     out DifficultyAnalysis? analysis,
                                     int maxFullAttempts = 40,
                                     int maxDigAttempts = 10000)
        {
            var targetLevel = TargetStrategyForDifficulty(difficulty);

            for (int fullTry = 0; fullTry < maxFullAttempts; fullTry++)
            {
                // 1) build a fully solved grid once
                var full = new SudokuGrid();
                if (!SudokuBacktrackingSolver.GenerateSolvedGrid(full))
                    continue;

                // 2) try many digs on that same solution
                for (int digTry = 0; digTry < maxDigAttempts; digTry++)
                {
                    var candidate = CloneGrid(full);
                    DigHoles(candidate, difficulty);

                    // 3) analyze
                    var result = SudokuLogicSolver.SolveWithLogic(CloneGrid(candidate), track: true);
                    if (result.Solved && result.Hardest == targetLevel)
                    {
                        puzzle = candidate;
                        analysis = result;
                        return true;
                    }
                }
                // if we got here, none of the maxDigAttempts on this 'full' worked — try a new solution
            }

            // fallback
            puzzle = new SudokuGrid();
            SudokuBacktrackingSolver.GenerateSolvedGrid(puzzle);
            DigHoles(puzzle, difficulty);
            analysis = new DifficultyAnalysis
            {
                Solved = SudokuLogicSolver.SolveWithLogic(CloneGrid(puzzle), out _),
                Hardest = StrategyLevel.None,
                Used = Array.Empty<StrategyLevel>()
            };
            return false;
        }

        public static void DigHoles(SudokuGrid grid, int difficulty)
        {
            var ranges = new[] { (36, 40), (32, 35), (28, 31), (22, 27), (17, 21) };
            var (min, max) = ranges[Math.Clamp(difficulty - 1, 0, 4)];
            int target = _random.Next(min, max + 1);
            var cells = Enumerable.Range(0, 9).SelectMany(r => Enumerable.Range(0, 9).Select(c => (r, c))).OrderBy(_ => _random.Next()).ToList();
            int clues = 81;
            foreach (var (r, c) in cells)
            {
                if (clues <= target) break;
                int backup = grid[r, c]; grid[r, c] = 0;
                if (SudokuBacktrackingSolver.CountSolutions(grid, 2) != 1) grid[r, c] = backup;
                else clues--;
            }
        }

        private static StrategyLevel TargetStrategyForDifficulty(int difficulty)
        {
            return difficulty switch
            {
                1 => StrategyLevel.Singles,
                2 => StrategyLevel.PairsTriples,
                3 => StrategyLevel.Pointing,
                _ => StrategyLevel.XWing
            };
        }

        private static SudokuGrid CloneGrid(SudokuGrid src)
        {
            var copy = new SudokuGrid();
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    copy[r, c] = src[r, c];
            return copy;
        }
    }
}
