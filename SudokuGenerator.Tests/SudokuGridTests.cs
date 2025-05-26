namespace SudokuGenerator.Tests
{
    public class SudokuGridTests
    {
        [Fact]
        public void IsSafe_ReturnsFalse_WhenNumberExistsInRow()
        {
            var grid = new SudokuGrid();
            grid[0, 0] = 5;
            Assert.False(grid.IsSafe(0, 1, 5));
        }
    }
}