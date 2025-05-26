using SudokuGenerator;
using Xunit;

namespace SudokuGenerator.Tests
{
    public class SudokuSolverTests
    {
        [Fact]
        public void SolveWithLogic_FillsLastCellInRowWithOnlyCandidate()
        {
            var grid = new SudokuGrid();
            // Fill all but one cell in the first row
            for (int col = 0; col < 8; col++)
                grid[0, col] = col + 1;
            // The only valid value for (0,8) is 9

            _ = SudokuSolver.SolveWithLogic(grid, out SudokuGrid solution);
            Assert.Equal(9, solution[0, 8]);
        }

        [Fact]
        public void TryGeneratePuzzle_ReturnsSolvedPuzzle_ForEasyDifficulty()
        {
            int difficulty = 1;

            SudokuSolver.TryGeneratePuzzle(difficulty, out _, out DifficultyAnalysis info);

            Assert.True(info.Solved);
        }

        [Fact]
        public void TryGeneratePuzzle_EnsuresSolutionIsPresent_ForEasyDifficulty()
        {
            int difficulty = 1;
            SudokuSolver.TryGeneratePuzzle(difficulty, out _, out DifficultyAnalysis info);
            var isPopulated = IsGridFullyPopulated(info.Solution);

            Assert.True(isPopulated);
        }

        private static bool IsGridFullyPopulated(SudokuGrid grid)
        {
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    if (grid[row, col] == 0)
                        return false;
                }
            }
            return true;
        }
    }
}
