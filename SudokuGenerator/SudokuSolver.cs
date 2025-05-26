using System;
using System.Collections.Generic;
using System.Linq;

namespace SudokuGenerator
{
    /// <summary>
    /// Static Sudoku solver/generator with logical strategies up to X‑Wing.
    /// </summary>
    public static class SudokuSolver
    {
        private static readonly Random _random = new();
        private const int MaxGenerationAttempts = 100;
        private const int MaxRecursionDepth = 10000;

        #region Backtracking Solver
        public static bool Solve(SudokuGrid grid)
        {
            if (!grid.FindEmptyCell(out int row, out int col))
                return true;
            for (int num = 1; num <= 9; num++)
            {
                if (grid.IsSafe(row, col, num))
                {
                    grid[row, col] = num;
                    if (Solve(grid)) return true;
                    grid[row, col] = 0;
                }
            }
            return false;
        }

        public static int CountSolutions(SudokuGrid grid, int maxSolutions = 2)
        {
            int count = 0;
            CountSolutionsRecursive(grid, ref count, maxSolutions);
            return count;
        }

        private static void CountSolutionsRecursive(SudokuGrid grid, ref int count, int maxSolutions)
        {
            if (count >= maxSolutions) return;
            if (!grid.FindEmptyCell(out int row, out int col))
            {
                count++;
                return;
            }
            for (int num = 1; num <= 9; num++)
            {
                if (grid.IsSafe(row, col, num))
                {
                    grid[row, col] = num;
                    CountSolutionsRecursive(grid, ref count, maxSolutions);
                    grid[row, col] = 0;
                }
            }
        }
        #endregion

        #region Logical Solver (Singles → Pairs/Triples → Pointing → X‑Wing)
        /// <summary>
        /// Human‑style logical solver: naked/hidden singles, pairs/triples, pointing pairs, then X‑Wing.
        /// Returns true if puzzle is completely solved without guessing.
        /// </summary>
        public static bool SolveWithLogic(SudokuGrid grid)
        {
            bool progress;
            do
            {
                // 1. basic singles
                progress = false;
                if (ApplyNakedSingles(grid) | ApplyHiddenSingles(grid)) progress = true;

                // 2. candidate‑based eliminations
                var cand = ComputeAllCandidates(grid);
                bool inner;
                do
                {
                    inner = false;
                    if (ApplyNakedPairsTriples(cand) | ApplyHiddenPairsTriples(cand) | ApplyPointingPairs(cand) | ApplyXWing(cand))
                        inner = true;

                    // promote any new singles found after eliminations
                    var singles = cand.Where(kv => kv.Value.Count == 1)
                                      .Select(kv => (kv.Key.row, kv.Key.col, kv.Value.First()))
                                      .ToList();
                    foreach (var (r, c, v) in singles)
                    {
                        grid[r, c] = v;
                        cand.Remove((r, c));
                        inner = true;
                    }
                    if (inner) cand = ComputeAllCandidates(grid);
                    progress |= inner;
                }
                while (inner);

            } while (progress);

            return !grid.FindEmptyCell(out _, out _);
        }

        #region Singles
        private static bool ApplyNakedSingles(SudokuGrid grid)
        {
            bool placed = false;
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                {
                    if (grid[r, c] != 0) continue;
                    var cand = Enumerable.Range(1, 9).Where(n => grid.IsSafe(r, c, n)).ToList();
                    if (cand.Count == 1) { grid[r, c] = cand[0]; placed = true; }
                }
            return placed;
        }

        private static bool ApplyHiddenSingles(SudokuGrid grid)
        {
            bool placed = false;
            for (int i = 0; i < 9; i++)
            {
                placed |= FindAndPlaceHiddenSingle(grid, GetRowCells(i));
                placed |= FindAndPlaceHiddenSingle(grid, GetColumnCells(i));
            }
            for (int br = 0; br < 3; br++)
                for (int bc = 0; bc < 3; bc++)
                    placed |= FindAndPlaceHiddenSingle(grid, GetBlockCells(br * 3, bc * 3));
            return placed;
        }

        private static bool FindAndPlaceHiddenSingle(SudokuGrid grid, List<(int row, int col)> cells)
        {
            for (int n = 1; n <= 9; n++)
            {
                var poss = cells.Where(rc => grid[rc.row, rc.col] == 0 && grid.IsSafe(rc.row, rc.col, n)).ToList();
                if (poss.Count == 1)
                {
                    grid[poss[0].row, poss[0].col] = n;
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Pairs/Triples/Pointing/X‑Wing Eliminations
        private static Dictionary<(int row, int col), HashSet<int>> ComputeAllCandidates(SudokuGrid grid)
        {
            var dict = new Dictionary<(int, int), HashSet<int>>();
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    if (grid[r, c] == 0)
                        dict[(r, c)] = new HashSet<int>(Enumerable.Range(1, 9).Where(n => grid.IsSafe(r, c, n)));
            return dict;
        }

        private static bool ApplyNakedPairsTriples(Dictionary<(int row, int col), HashSet<int>> cand)
        {
            bool changed = false;
            foreach (var unit in GetAllUnits(cand.Keys))
            {
                for (int size = 2; size <= 3; size++)
                {
                    var groups = unit.Where(c => cand[c].Count == size)
                                      .GroupBy(c => string.Join(',', cand[c].OrderBy(x => x)));
                    foreach (var g in groups)
                    {
                        var cells = g.ToList();
                        if (cells.Count != size) continue;
                        var nums = cand[cells[0]].ToHashSet();
                        foreach (var other in unit.Except(cells))
                            if (cand[other].RemoveWhere(nums.Contains) > 0) changed = true;
                    }
                }
            }
            return changed;
        }

        private static bool ApplyHiddenPairsTriples(Dictionary<(int row, int col), HashSet<int>> cand)
        {
            bool changed = false;
            var allNums = Enumerable.Range(1, 9).ToList();
            foreach (var unit in GetAllUnits(cand.Keys))
            {
                for (int size = 2; size <= 3; size++)
                    foreach (var combo in GetCombinations(allNums, size))
                    {
                        var cells = unit.Where(c => cand[c].Overlaps(combo)).ToList();
                        if (cells.Count != size) continue;
                        if (!combo.All(n => unit.Count(c => cand[c].Contains(n)) == size)) continue;
                        foreach (var cell in cells)
                            if (!cand[cell].SetEquals(combo)) { cand[cell].IntersectWith(combo); changed = true; }
                    }
            }
            return changed;
        }

        private static bool ApplyPointingPairs(Dictionary<(int row, int col), HashSet<int>> cand)
        {
            bool changed = false;
            for (int br = 0; br < 3; br++)
                for (int bc = 0; bc < 3; bc++)
                {
                    var block = cand.Keys.Where(k => k.row / 3 == br && k.col / 3 == bc).ToList();
                    for (int n = 1; n <= 9; n++)
                    {
                        var rel = block.Where(k => cand[k].Contains(n)).ToList();
                        if (rel.Count < 2 || rel.Count > 3) continue;
                        if (rel.All(k => k.row == rel[0].row))
                        {
                            int row = rel[0].row;
                            foreach (var cell in cand.Keys.Where(k => k.row == row && k.col / 3 != bc))
                                if (cand[cell].Remove(n)) changed = true;
                        }
                        if (rel.All(k => k.col == rel[0].col))
                        {
                            int col = rel[0].col;
                            foreach (var cell in cand.Keys.Where(k => k.col == col && k.row / 3 != br))
                                if (cand[cell].Remove(n)) changed = true;
                        }
                    }
                }
            return changed;
        }

        // --- X‑Wing ---
        private static bool ApplyXWing(Dictionary<(int row, int col), HashSet<int>> cand)
        {
            bool changed = false;
            // Scan rows then columns (transpose logic reused)
            for (int mode = 0; mode < 2; mode++) // 0 = row‑based, 1 = column‑based
            {
                for (int n = 1; n <= 9; n++)
                {
                    // Map index -> list of otherIndex where candidate appears
                    var lines = new Dictionary<int, List<int>>();
                    for (int i = 0; i < 9; i++)
                    {
                        var positions = new List<int>();
                        for (int j = 0; j < 9; j++)
                        {
                            var key = mode == 0 ? (i, j) : (j, i);
                            if (cand.TryGetValue(key, out var set) && set.Contains(n)) positions.Add(j);
                        }
                        if (positions.Count == 2) lines[i] = positions;
                    }
                    // check each pair of lines for identical positions
                    foreach (var (r1, pos1) in lines)
                        foreach (var (r2, pos2) in lines)
                        {
                            if (r2 <= r1) continue;
                            if (pos1[0] == pos2[0] && pos1[1] == pos2[1])
                            {
                                int c1 = pos1[0], c2 = pos1[1];
                                // eliminate from columns (or rows) other than the two lines
                                for (int k = 0; k < 9; k++)
                                {
                                    if (k == r1 || k == r2) continue;
                                    var key1 = mode == 0 ? (k, c1) : (c1, k);
                                    var key2 = mode == 0 ? (k, c2) : (c2, k);
                                    if (cand.TryGetValue(key1, out var set1) && set1.Remove(n)) changed = true;
                                    if (cand.TryGetValue(key2, out var set2) && set2.Remove(n)) changed = true;
                                }
                            }
                        }
                }
            }
            return changed;
        }
        #endregion

        #endregion

        #region Generator (unchanged)
        public static bool GenerateSolvedGrid(SudokuGrid grid)
        {
            for (int attempt = 0; attempt < MaxGenerationAttempts; attempt++)
            {
                for (int r = 0; r < 9; r++) for (int c = 0; c < 9; c++) grid[r, c] = 0;
                if (GenerateSolvedGridRecursive(grid, 0)) return true;
            }
            return false;
        }

        private static bool GenerateSolvedGridRecursive(SudokuGrid grid, int d)
        {
            if (d > MaxRecursionDepth) return false;
            if (!grid.FindEmptyCell(out int row, out int col)) return true;
            var nums = Enumerable.Range(1, 9).ToList(); Shuffle(nums);
            foreach (var n in nums)
            {
                if (grid.IsSafe(row, col, n))
                {
                    grid[row, col] = n;
                    if (GenerateSolvedGridRecursive(grid, d + 1)) return true;
                    grid[row, col] = 0;
                }
            }
            return false;
        }

        public static void DigHoles(SudokuGrid grid, int difficulty)
        {
            var ranges = new (int min, int max)[] { (36, 40), (32, 35), (28, 31), (22, 27), (17, 21) };
            int min = ranges[Math.Clamp(difficulty - 1, 0, 4)].min;
            int max = ranges[Math.Clamp(difficulty - 1, 0, 4)].max;
            int target = _random.Next(min, max + 1);
            var cells = new List<(int, int)>();
            for (int r = 0; r < 9; r++) for (int c = 0; c < 9; c++) cells.Add((r, c));
            Shuffle(cells);
            int clues = 81;
            foreach (var (r, c) in cells)
            {
                if (clues <= target) break;
                int backup = grid[r, c]; grid[r, c] = 0;
                if (CountSolutions(grid, 2) != 1) grid[r, c] = backup; else clues--;
            }
        }
        #endregion

        #region Misc Helpers
        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static List<(int row, int col)> GetRowCells(int row)
        {
            var l = new List<(int, int)>(); for (int c = 0; c < 9; c++) l.Add((row, c)); return l;
        }
        private static List<(int row, int col)> GetColumnCells(int col)
        {
            var l = new List<(int, int)>(); for (int r = 0; r < 9; r++) l.Add((r, col)); return l;
        }
        private static List<(int row, int col)> GetBlockCells(int sr, int sc)
        {
            var l = new List<(int, int)>();
            for (int r = sr; r < sr + 3; r++) for (int c = sc; c < sc + 3; c++) l.Add((r, c));
            return l;
        }

        private static IEnumerable<List<(int row, int col)>> GetAllUnits(IEnumerable<(int row, int col)> keys)
        {
            for (int r = 0; r < 9; r++) yield return keys.Where(k => k.row == r).ToList();
            for (int c = 0; c < 9; c++) yield return keys.Where(k => k.col == c).ToList();
            for (int br = 0; br < 3; br++)
                for (int bc = 0; bc < 3; bc++)
                    yield return keys.Where(k => k.row / 3 == br && k.col / 3 == bc).ToList();
        }

        private static IEnumerable<HashSet<int>> GetCombinations(List<int> nums, int size)
        {
            if (size == 0) { yield return new HashSet<int>(); yield break; }
            for (int i = 0; i <= nums.Count - size; i++)
            {
                int head = nums[i];
                foreach (var tail in GetCombinations(nums.Skip(i + 1).ToList(), size - 1))
                {
                    tail.Add(head); yield return tail;
                }
            }
        }
        #endregion
    }
}
