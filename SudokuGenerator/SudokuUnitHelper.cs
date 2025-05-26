using System;
using System.Collections.Generic;
using System.Linq;

namespace SudokuGenerator
{
    /// <summary>
    /// Provides helpers for Sudoku units (rows, columns, blocks).
    /// </summary>
    public static class SudokuUnitHelper
    {
        public static IEnumerable<(int row, int col)> GetRowCells(int row) => Enumerable.Range(0, 9).Select(c => (row, c));
        public static IEnumerable<(int row, int col)> GetColumnCells(int col) => Enumerable.Range(0, 9).Select(r => (r, col));
        public static IEnumerable<(int row, int col)> GetBlockCells(int sr, int sc) =>
            from r in Enumerable.Range(sr, 3)
            from c in Enumerable.Range(sc, 3)
            select (r, c);

        public static IEnumerable<IEnumerable<(int row, int col)>> GetAllUnits()
        {
            for (int i = 0; i < 9; i++) yield return GetRowCells(i);
            for (int i = 0; i < 9; i++) yield return GetColumnCells(i);
            for (int br = 0; br < 3; br++) for (int bc = 0; bc < 3; bc++) yield return GetBlockCells(br * 3, bc * 3);
        }
    }
}
