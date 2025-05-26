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
            // prepare tracking containers up‐front
            var used = new List<StrategyLevel> { StrategyLevel.Singles };
            var hardest = StrategyLevel.Singles;

            bool anyProgress;
            int safety = 0;    // ultimate bail‐out guard

            do
            {
                if (++safety > 100_000)         // just in case
                    break;

                anyProgress = false;

                // Singles first
                if (ApplyNakedSingles(grid) | ApplyHiddenSingles(grid))
                    anyProgress = true;

                var candidates = ComputeCandidates(grid);
                bool gridChanged;

                do
                {
                    gridChanged = false;

                    // — NAKED/HIDDEN PAIRS & TRIPLES —
                    if (ApplyNakedPairsTriples(candidates) | ApplyHiddenPairsTriples(candidates))
                    {
                        if (track && !used.Contains(StrategyLevel.PairsTriples))
                            used.Add(StrategyLevel.PairsTriples);
                        hardest = StrategyLevel.PairsTriples;
                    }

                    // — POINTING PAIRS —
                    if (ApplyPointingPairs(candidates))
                    {
                        if (track && !used.Contains(StrategyLevel.Pointing))
                            used.Add(StrategyLevel.Pointing);
                        hardest = StrategyLevel.Pointing;
                    }

                    // — X‐WING —
                    if (ApplyXWing(candidates))
                    {
                        if (track && !used.Contains(StrategyLevel.XWing))
                            used.Add(StrategyLevel.XWing);
                        hardest = StrategyLevel.XWing;
                    }

                    // Promote any new singles
                    foreach (var (r, c, v) in candidates
                                .Where(kv => kv.Value.Count == 1)
                                .Select(kv => (kv.Key.row, kv.Key.col, kv.Value.First()))
                                .ToList())
                    {
                        grid[r, c] = v;
                        candidates.Remove((r, c));
                        gridChanged = true;
                    }

                    if (gridChanged)
                        candidates = ComputeCandidates(grid);

                } while (gridChanged);

                anyProgress |= gridChanged;

            } while (anyProgress);

            // build the result
            bool solved = !grid.FindEmptyCell(out _, out _);
            if (!track)
                return new DifficultyAnalysis { Solved = solved, Hardest = StrategyLevel.None, Solution = grid };

            return new DifficultyAnalysis
            {
                Solved = solved,
                Hardest = hardest,
                Used = used,
                Solution = grid
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

            // get only the 3×3 blocks
            var blocks = SudokuUnitHelper.GetAllUnits()
                         .Where(u =>
                             u.Select(p => (p.row / 3, p.col / 3))
                              .Distinct().Count() == 1
                         );

            foreach (var block in blocks)
            {
                // precompute for fast lookups
                var blockSet = new HashSet<(int row, int col)>(block);

                for (int n = 1; n <= 9; n++)
                {
                    // which empty cells in this block can be n?
                    var pos = block
                        .Where(p => cand.TryGetValue(p, out var s) && s.Contains(n))
                        .ToList();

                    if (pos.Count < 2 || pos.Count > 3)
                        continue;

                    // all in same row → eliminate n from that row outside this block
                    if (pos.All(p => p.row == pos[0].row))
                    {
                        int row = pos[0].row;
                        for (int col = 0; col < 9; col++)
                        {
                            var key = (row, col);
                            if (!blockSet.Contains(key) &&
                                cand.TryGetValue(key, out var s) &&
                                s.Remove(n))
                            {
                                changed = true;
                            }
                        }
                    }

                    // all in same col → eliminate n from that column outside this block
                    if (pos.All(p => p.col == pos[0].col))
                    {
                        int col = pos[0].col;
                        for (int row = 0; row < 9; row++)
                        {
                            var key = (row, col);
                            if (!blockSet.Contains(key) &&
                                cand.TryGetValue(key, out var s) &&
                                s.Remove(n))
                            {
                                changed = true;
                            }
                        }
                    }
                }
            }

            return changed;
        }
        private static bool ApplyXWing(Dictionary<(int row, int col), HashSet<int>> cand)
        {
            bool changed = false;

            for (int mode = 0; mode < 2; mode++)  // 0 = rows→cols, 1 = cols→rows
            {
                // collect lines that have exactly two candidates for each digit
                for (int n = 1; n <= 9; n++)
                {
                    var lines = new Dictionary<int, List<int>>();
                    for (int i = 0; i < 9; i++)
                    {
                        var pos = Enumerable.Range(0, 9)
                            .Where(j =>
                            {
                                var key = mode == 0 ? (i, j) : (j, i);
                                return cand.TryGetValue(key, out var s) && s.Contains(n);
                            })
                            .ToList();
                        if (pos.Count == 2)
                            lines[i] = pos;
                    }

                    if (lines.Count < 2) continue;

                    // group by the two‐column (or two‐row) positions
                    var groups = lines
                        .GroupBy(kv => string.Join(',', kv.Value))
                        .Where(g => g.Count() == 2);

                    foreach (var grp in groups)
                    {
                        var lineIndices = grp.Select(kv => kv.Key).ToList();
                        var fixedIndices = grp.First().Value; // the shared columns (or rows)

                        // eliminate n from the rest of the grid
                        foreach (var otherLine in Enumerable.Range(0, 9).Except(lineIndices))
                        {
                            foreach (var fixedIdx in fixedIndices)
                            {
                                var key = mode == 0
                                    ? (otherLine, fixedIdx)
                                    : (fixedIdx, otherLine);

                                if (cand.TryGetValue(key, out var s) && s.Remove(n))
                                    changed = true;
                            }
                        }
                    }
                }
            }

            return changed;
        }
    }
}
