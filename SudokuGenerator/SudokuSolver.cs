using System;
using System.Collections.Generic;
using System.Linq;

namespace SudokuGenerator
{
    /// <summary>
    /// Sudoku puzzle generator & logical solver (up to X‑Wing) with automatic
    /// difficulty calibration. Difficulty levels:
    /// 1 = requires only singles,
    /// 2 = requires pairs/triples,
    /// 3 = requires pointing pairs,
    /// 4+ = requires X‑Wing.
    /// </summary>
    public static class SudokuSolver
    {
        // Strategy classification
        public enum StrategyLevel { None = 0, Singles = 1, PairsTriples = 2, Pointing = 3, XWing = 4 }
        public sealed class DifficultyAnalysis
        {
            public bool Solved { get; init; }
            public StrategyLevel Hardest { get; init; }
            public IReadOnlyCollection<StrategyLevel> Used { get; init; } = Array.Empty<StrategyLevel>();
        }

        private static readonly Random _random = new();
        private const int MaxGenerationAttempts = 100;
        private const int MaxRecursionDepth = 10000;

        /// <summary>
        /// Attempts to generate a puzzle of exactly the given difficulty.
        /// Returns false if unable after maxAttempts.
        /// </summary>
        public static bool TryGeneratePuzzle(int difficulty,
                                             out SudokuGrid puzzle,
                                             out DifficultyAnalysis analysis,
                                             int maxAttempts = 40)
        {
            puzzle = null;
            analysis = null;
            var targetLevel = TargetStrategyForDifficulty(difficulty);

            for (int i = 0; i < maxAttempts; i++)
            {
                // 1. generate a fully solved grid
                var full = new SudokuGrid();
                if (!GenerateSolvedGrid(full))
                    continue;

                // 2. dig holes based on difficulty
                var candidate = CloneGrid(full);
                DigHoles(candidate, difficulty);

                // 3. analyze
                var clone = CloneGrid(candidate);
                var result = SolveWithLogic(clone, track: true);
                if (!result.Solved) continue;
                if (result.Hardest != targetLevel) continue;

                puzzle = candidate;
                analysis = result;
                return true;
            }
            // fallback: generate any puzzle
            puzzle = new SudokuGrid();
            GenerateSolvedGrid(puzzle);
            DigHoles(puzzle, difficulty);
            analysis = new DifficultyAnalysis
            {
                Solved = SolveWithLogic(CloneGrid(puzzle)),
                Hardest = StrategyLevel.None,
                Used = Array.Empty<StrategyLevel>()
            };
            return false;
        }

        /// <summary>
        /// Solves by logic only
        /// </summary>
        public static bool SolveWithLogic(SudokuGrid grid) => SolveWithLogic(grid, track: false).Solved;

        private static DifficultyAnalysis SolveWithLogic(SudokuGrid grid, bool track)
        {
            bool usedPairs = false, usedPointing = false, usedX = false;
            bool anyProgress;
            do
            {
                anyProgress = false;
                if (ApplyNakedSingles(grid) | ApplyHiddenSingles(grid))
                    anyProgress = true;

                var candidates = ComputeCandidates(grid);
                bool inner;
                do
                {
                    inner = false;
                    if ((ApplyNakedPairsTriples(candidates) | ApplyHiddenPairsTriples(candidates)))
                    { inner = true; usedPairs = true; }
                    if (ApplyPointingPairs(candidates))
                    { inner = true; usedPointing = true; }
                    if (ApplyXWing(candidates))
                    { inner = true; usedX = true; }

                    // fill singles
                    var singles = candidates.Where(kv => kv.Value.Count == 1)
                                             .Select(kv => (kv.Key.row, kv.Key.col, kv.Value.First()))
                                             .ToList();
                    foreach (var (r, c, v) in singles)
                    {
                        grid[r, c] = v;
                        candidates.Remove((r, c));
                        inner = true;
                    }
                    if (inner)
                        candidates = ComputeCandidates(grid);
                    anyProgress |= inner;
                }
                while (inner);
            }
            while (anyProgress);

            bool solved = !grid.FindEmptyCell(out _, out _);
            if (!track)
                return new DifficultyAnalysis { Solved = solved, Hardest = StrategyLevel.None };

            var used = new List<StrategyLevel> { StrategyLevel.Singles };
            var hardest = StrategyLevel.Singles;
            if (usedPairs) { used.Add(StrategyLevel.PairsTriples); hardest = StrategyLevel.PairsTriples; }
            if (usedPointing) { used.Add(StrategyLevel.Pointing); hardest = StrategyLevel.Pointing; }
            if (usedX) { used.Add(StrategyLevel.XWing); hardest = StrategyLevel.XWing; }

            return new DifficultyAnalysis
            {
                Solved = solved,
                Hardest = hardest,
                Used = used
            };
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

        // ----- Logical Techniques -----
        private static bool ApplyNakedSingles(SudokuGrid g)
        {
            bool placed = false;
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    if (g[r, c] == 0)
                    {
                        var list = Enumerable.Range(1, 9).Where(n => g.IsSafe(r, c, n)).ToList();
                        if (list.Count == 1)
                        {
                            g[r, c] = list[0]; placed = true;
                        }
                    }
            return placed;
        }

        private static bool ApplyHiddenSingles(SudokuGrid g)
        {
            bool placed = false;
            foreach (var unit in GetAllUnits())
            {
                for (int n = 1; n <= 9; n++)
                {
                    var empties = unit.Where(p => g[p.row, p.col] == 0 && g.IsSafe(p.row, p.col, n)).ToList();
                    if (empties.Count == 1)
                    {
                        g[empties[0].row, empties[0].col] = n;
                        placed = true;
                    }
                }
            }
            return placed;
        }

        private static Dictionary<(int row, int col), HashSet<int>> ComputeCandidates(SudokuGrid g)
        {
            var dict = new Dictionary<(int, int), HashSet<int>>();
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    if (g[r, c] == 0)
                        dict[(r, c)] = new HashSet<int>(Enumerable.Range(1, 9).Where(n => g.IsSafe(r, c, n)));
            return dict;
        }

        private static bool ApplyNakedPairsTriples(Dictionary<(int row, int col), HashSet<int>> cand)
        {
            bool changed = false;
            foreach (var unit in GetAllUnits())
            {
                var emptyCells = unit.Where(p => cand.ContainsKey(p)).ToList();
                for (int size = 2; size <= 3; size++)
                {
                    var groups = emptyCells
                        .Where(p => cand[p].Count == size)
                        .GroupBy(p => string.Join(',', cand[p].OrderBy(x => x)));

                    foreach (var grp in groups)
                    {
                        var list = grp.ToList();
                        if (list.Count != size) continue;
                        var nums = cand[list[0]].ToHashSet();
                        foreach (var other in emptyCells.Except(list))
                            if (cand[other].RemoveWhere(nums.Contains) > 0)
                                changed = true;
                    }
                }
            }
            return changed;
        }
        private static bool ApplyHiddenPairsTriples(Dictionary<(int row, int col), HashSet<int>> cand)
        {
            bool changed = false;
            var nums = Enumerable.Range(1, 9).ToList();
            foreach (var unit in GetAllUnits())
            {
                var emptyCells = unit.Where(p => cand.ContainsKey(p)).ToList();
                for (int size = 2; size <= 3; size++)
                {
                    foreach (var combo in GetCombinations(nums, size))
                    {
                        var cells = emptyCells.Where(p => cand[p].Overlaps(combo)).ToList();
                        if (cells.Count != size) continue;
                        if (!combo.All(n => emptyCells.Count(p => cand[p].Contains(n)) == size)) continue;
                        foreach (var p in cells)
                            if (!cand[p].SetEquals(combo))
                            {
                                cand[p].IntersectWith(combo);
                                changed = true;
                            }
                    }
                }
            }
            return changed;
        }

        private static bool ApplyPointingPairs(Dictionary<(int row, int col), HashSet<int>> cand)
        {
            bool changed = false;
            foreach (var unit in GetAllUnits().Where(u => u.All(p => cand.ContainsKey(p))))
            {
                // unit is a row, col or block of interest only if it contains empties
                var emptyCells = unit.ToList();
                for (int n = 1; n <= 9; n++)
                {
                    var pos = emptyCells.Where(p => cand[p].Contains(n)).ToList();
                    if (pos.Count < 2 || pos.Count > 3) continue;

                    // check row alignment inside block-line reduction
                    if (pos.All(p => p.row == pos[0].row))
                    {
                        foreach (var col in Enumerable.Range(0, 9))
                        {
                            var key = (pos[0].row, col);
                            if (emptyCells.Contains(key) || cand.ContainsKey(key) && cand[key].Remove(n))
                                changed = true;
                        }
                    }
                    // check column alignment
                    if (pos.All(p => p.col == pos[0].col))
                    {
                        foreach (var row in Enumerable.Range(0, 9))
                        {
                            var key = (row, pos[0].col);
                            if (emptyCells.Contains(key) || cand.ContainsKey(key) && cand[key].Remove(n))
                                changed = true;
                        }
                    }
                }
            }
            return changed;
        }
        private static bool ApplyXWing(Dictionary<(int row, int col), HashSet<int>> cand)
        {
            bool changed = false;
            for (int mode = 0; mode < 2; mode++) // 0 = rows, 1 = columns
            {
                for (int n = 1; n <= 9; n++)
                {
                    var lines = new Dictionary<int, List<int>>();
                    for (int i = 0; i < 9; i++)
                    {
                        var positions = Enumerable.Range(0, 9)
                            .Where(j =>
                            {
                                var key = mode == 0 ? (i, j) : (j, i);
                                return cand.TryGetValue(key, out var s) && s.Contains(n);
                            })
                            .ToList();
                        if (positions.Count == 2) lines[i] = positions;
                    }

                    foreach (var (l1, p1) in lines)
                        foreach (var (l2, p2) in lines)
                        {
                            if (l2 <= l1) continue;
                            if (p1[0] == p2[0] && p1[1] == p2[1])
                            {
                                for (int k = 0; k < 9; k++)
                                {
                                    if (k == l1 || k == l2) continue;
                                    var keyA = mode == 0 ? (k, p1[0]) : (p1[0], k);
                                    var keyB = mode == 0 ? (k, p1[1]) : (p1[1], k);
                                    if (cand.TryGetValue(keyA, out var sA) && sA.Remove(n)) changed = true;
                                    if (cand.TryGetValue(keyB, out var sB) && sB.Remove(n)) changed = true;
                                }
                            }
                        }
                }
            }
            return changed;
        }

        // ----- Backtracking & generation -----
        public static bool GenerateSolvedGrid(SudokuGrid grid)
        {
            for (int attempt = 0; attempt < MaxGenerationAttempts; attempt++)
            {
                ClearGrid(grid);
                if (SolveBacktrack(grid)) return true;
            }
            return false;
        }

        private static void ClearGrid(SudokuGrid g)
        {
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    g[r, c] = 0;
        }

        private static bool SolveBacktrack(SudokuGrid g)
        {
            if (!g.FindEmptyCell(out int r, out int c)) return true;
            for (int n = 1; n <= 9; n++)
            {
                if (!g.IsSafe(r, c, n)) continue;
                g[r, c] = n;
                if (SolveBacktrack(g)) return true;
                g[r, c] = 0;
            }
            return false;
        }

        public static int CountSolutions(SudokuGrid grid, int max = 2)
        {
            int cnt = 0;
            CountSolutionsRecursive(grid, ref cnt, max);
            return cnt;
        }

        private static void CountSolutionsRecursive(SudokuGrid g, ref int cnt, int max)
        {
            if (cnt >= max) return;
            if (!g.FindEmptyCell(out int r, out int c)) { cnt++; return; }
            for (int n = 1; n <= 9; n++)
            {
                if (!g.IsSafe(r, c, n)) continue;
                g[r, c] = n;
                CountSolutionsRecursive(g, ref cnt, max);
                g[r, c] = 0;
            }
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
                if (CountSolutions(grid, 2) != 1) grid[r, c] = backup;
                else clues--;
            }
        }

        // ----- Unit helpers -----
        private static IEnumerable<(int row, int col)> GetRowCells(int row) => Enumerable.Range(0, 9).Select(c => (row, c));
        private static IEnumerable<(int row, int col)> GetColumnCells(int col) => Enumerable.Range(0, 9).Select(r => (r, col));
        private static IEnumerable<(int row, int col)> GetBlockCells(int sr, int sc) =>
            from r in Enumerable.Range(sr, 3)
            from c in Enumerable.Range(sc, 3)
            select (r, c);

        private static IEnumerable<IEnumerable<(int row, int col)>> GetAllUnits()
        {
            for (int i = 0; i < 9; i++) yield return GetRowCells(i);
            for (int i = 0; i < 9; i++) yield return GetColumnCells(i);
            for (int br = 0; br < 3; br++) for (int bc = 0; bc < 3; bc++) yield return GetBlockCells(br * 3, bc * 3);
        }

        private static IEnumerable<HashSet<int>> GetCombinations(List<int> nums, int size)
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
