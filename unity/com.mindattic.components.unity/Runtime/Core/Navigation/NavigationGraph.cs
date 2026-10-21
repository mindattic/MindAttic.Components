using System;
using System.Collections.Generic;

namespace MindAttic.Components.Unity.Core
{
    /// <summary>A cell of a navigation layout: a row and the column inside it.</summary>
    public readonly struct NavCell : IEquatable<NavCell>
    {
        /// <summary>Row index.</summary>
        public readonly int Row;
        /// <summary>Column index inside the row.</summary>
        public readonly int Col;

        /// <summary>Creates a cell.</summary>
        public NavCell(int row, int col)
        {
            Row = row;
            Col = col;
        }

        /// <summary>No cell: navigation goes nowhere.</summary>
        public static NavCell None => new NavCell(-1, -1);

        /// <summary>True for a real cell.</summary>
        public bool IsValid => Row >= 0 && Col >= 0;

        /// <inheritdoc />
        public bool Equals(NavCell other) => Row == other.Row && Col == other.Col;
        /// <inheritdoc />
        public override bool Equals(object obj) => obj is NavCell c && Equals(c);
        /// <inheritdoc />
        public override int GetHashCode() => (Row * 397) ^ Col;
        /// <inheritdoc />
        public override string ToString() => IsValid ? "(" + Row + "," + Col + ")" : "none";
        /// <summary>Equality.</summary>
        public static bool operator ==(NavCell a, NavCell b) => a.Equals(b);
        /// <summary>Inequality.</summary>
        public static bool operator !=(NavCell a, NavCell b) => !a.Equals(b);
    }

    /// <summary>A direction of keyboard/gamepad navigation.</summary>
    public enum NavDirection
    {
        /// <summary>Up.</summary>
        Up,
        /// <summary>Down.</summary>
        Down,
        /// <summary>Left.</summary>
        Left,
        /// <summary>Right.</summary>
        Right,
    }

    /// <summary>
    /// Explicit keyboard/gamepad navigation for a layout of rows, each holding one or more focusable cells (a
    /// vertical menu is rows of one; a button row or a toolbar is one row of many; a code display with a copy
    /// button is a row of one cell). Up/Down go to the nearest enabled row, landing on its first cell when coming
    /// from a single-cell row and otherwise on the cell whose horizontal position best matches; Left/Right move inside the row. Disabled cells are skipped everywhere, and rows
    /// with no enabled cell are passed over. Wrapping is per axis.
    /// </summary>
    public sealed class NavigationGraph
    {
        readonly int[] _rowLengths;
        readonly bool[][] _enabled;

        /// <summary>Wrap from the last row to the first and back.</summary>
        public bool WrapVertical { get; }
        /// <summary>Wrap from a row's last cell to its first and back.</summary>
        public bool WrapHorizontal { get; }

        /// <summary>
        /// Builds the graph. <paramref name="enabled"/> holds one array per row (its length is the row's cell
        /// count; an empty row is allowed and never visited).
        /// </summary>
        public NavigationGraph(IReadOnlyList<bool[]> enabled, bool wrapVertical = true, bool wrapHorizontal = false)
        {
            if (enabled == null) throw new ArgumentNullException(nameof(enabled));
            _enabled = new bool[enabled.Count][];
            _rowLengths = new int[enabled.Count];
            for (int r = 0; r < enabled.Count; r++)
            {
                _enabled[r] = enabled[r] ?? new bool[0];
                _rowLengths[r] = _enabled[r].Length;
            }
            WrapVertical = wrapVertical;
            WrapHorizontal = wrapHorizontal;
        }

        /// <summary>A single vertical list of <paramref name="count"/> cells, all enabled.</summary>
        public static NavigationGraph VerticalList(int count, bool wrap = true)
        {
            var rows = new bool[count][];
            for (int i = 0; i < count; i++) rows[i] = new[] { true };
            return new NavigationGraph(rows, wrap, false);
        }

        /// <summary>A single horizontal row of <paramref name="count"/> cells, all enabled.</summary>
        public static NavigationGraph HorizontalRow(int count, bool wrap = false)
        {
            var row = new bool[count];
            for (int i = 0; i < count; i++) row[i] = true;
            return new NavigationGraph(new[] { row }, false, wrap);
        }

        /// <summary>Number of rows.</summary>
        public int RowCount => _rowLengths.Length;

        /// <summary>Number of cells in a row.</summary>
        public int RowLength(int row) => _rowLengths[row];

        /// <summary>True when the cell exists and is enabled.</summary>
        public bool IsEnabled(NavCell c) =>
            c.Row >= 0 && c.Row < _enabled.Length && c.Col >= 0 && c.Col < _rowLengths[c.Row] && _enabled[c.Row][c.Col];

        /// <summary>The first enabled cell, top to bottom then left to right (where a menu puts the initial selection).</summary>
        public NavCell First
        {
            get
            {
                for (int r = 0; r < _enabled.Length; r++)
                {
                    for (int c = 0; c < _rowLengths[r]; c++)
                    {
                        if (_enabled[r][c]) return new NavCell(r, c);
                    }
                }
                return NavCell.None;
            }
        }

        /// <summary>Where navigating from <paramref name="from"/> in <paramref name="direction"/> lands, or <see cref="NavCell.None"/>.</summary>
        public NavCell Move(NavCell from, NavDirection direction)
        {
            if (from.Row < 0 || from.Row >= _enabled.Length || from.Col < 0 || from.Col >= _rowLengths[from.Row]) return NavCell.None;
            switch (direction)
            {
                case NavDirection.Up: return Vertical(from, -1);
                case NavDirection.Down: return Vertical(from, +1);
                case NavDirection.Left: return Horizontal(from, -1);
                default: return Horizontal(from, +1);
            }
        }

        NavCell Horizontal(NavCell from, int step)
        {
            int n = _rowLengths[from.Row];
            int c = from.Col;
            for (int i = 1; i < n; i++)
            {
                c += step;
                if (c < 0 || c >= n)
                {
                    if (!WrapHorizontal) return NavCell.None;
                    c = (c + n) % n;
                }
                if (_enabled[from.Row][c]) return new NavCell(from.Row, c);
            }
            return NavCell.None;
        }

        NavCell Vertical(NavCell from, int step)
        {
            int rows = _enabled.Length;
            int r = from.Row;
            float center = (from.Col + 0.5f) / Math.Max(1, _rowLengths[from.Row]);
            for (int i = 1; i < rows; i++)
            {
                r += step;
                if (r < 0 || r >= rows)
                {
                    if (!WrapVertical) return NavCell.None;
                    r = (r + rows) % rows;
                }
                int n = _rowLengths[r];
                if (n == 0) continue;
                // From a single-cell row (a menu item) land on the row's first cell (a roster row, not its kick button);
                // between multi-cell rows keep the horizontal position.
                int target = _rowLengths[from.Row] == 1 ? 0 : Math.Min(n - 1, (int)Math.Floor(center * n));
                for (int d = 0; d < n; d++)
                {
                    int left = target - d, right = target + d;
                    if (left >= 0 && _enabled[r][left]) return new NavCell(r, left);
                    if (right < n && _enabled[r][right]) return new NavCell(r, right);
                }
            }
            return NavCell.None;
        }
    }
}
