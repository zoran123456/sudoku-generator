using System.Collections.Generic;

namespace SudokuGenerator
{
    /// <summary>
    /// Strategy classification for Sudoku solving.
    /// </summary>
    public enum StrategyLevel { None = 0, Singles = 1, PairsTriples = 2, Pointing = 3, XWing = 4 }

    /// <summary>
    /// Analysis result for Sudoku difficulty.
    /// </summary>
    public sealed class DifficultyAnalysis
    {
        public bool Solved { get; init; }
        public StrategyLevel Hardest { get; init; }
        public IReadOnlyCollection<StrategyLevel> Used { get; init; } = System.Array.Empty<StrategyLevel>();
    }
}
