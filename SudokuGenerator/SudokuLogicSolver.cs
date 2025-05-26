using System;
using System.Collections.Generic;
using System.Linq;

namespace SudokuGenerator
{
    /// <summary>
    /// Provides logic-based solving for Sudoku puzzles.
    /// </summary>
    public static class SudokuLogicSolver
    {
        public static DifficultyAnalysis SolveWithLogic(SudokuGrid grid, bool track)
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

        public static bool SolveWithLogic(SudokuGrid grid) => SolveWithLogic(grid, track: false).Solved;

        // --- Logical Techniques ---
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
            foreach (var unit in SudokuUnitHelper.GetAllUnits())
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
            foreach (var unit in SudokuUnitHelper.GetAllUnits())
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
            foreach (var unit in SudokuUnitHelper.GetAllUnits())
            {
                var emptyCells = unit.Where(p => cand.ContainsKey(p)).ToList();
                for (int size = 2; size <= 3; size++)
                {
                    foreach (var combo in CombinatoricsHelper.GetCombinations(nums, size))
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
            foreach (var unit in SudokuUnitHelper.GetAllUnits().Where(u => u.All(p => cand.ContainsKey(p))))
            {
                var emptyCells = unit.ToList();
                for (int n = 1; n <= 9; n++)
                {
                    var pos = emptyCells.Where(p => cand[p].Contains(n)).ToList();
                    if (pos.Count < 2 || pos.Count > 3) continue;

                    if (pos.All(p => p.row == pos[0].row))
                    {
                        foreach (var col in Enumerable.Range(0, 9))
                        {
                            var key = (pos[0].row, col);
                            if (emptyCells.Contains(key) || cand.ContainsKey(key) && cand[key].Remove(n))
                                changed = true;
                        }
                    }
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
            for (int mode = 0; mode < 2; mode++)
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
    }
}
