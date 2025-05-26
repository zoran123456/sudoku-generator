namespace SudokuGenerator
{
    /// <summary>
    /// Provides combinatorics helpers for Sudoku logic.
    /// </summary>
    public static class CombinatoricsHelper
    {
        public static IEnumerable<HashSet<int>> GetCombinations(List<int> nums, int size)
        {
            if (size == 0) yield return new HashSet<int>();
            else for (int i = 0; i <= nums.Count - size; i++)
                    foreach (var tail in GetCombinations(nums.Skip(i + 1).ToList(), size - 1))
                    {
                        tail.Add(nums[i]); yield return tail;
                    }
        }
    }
}
