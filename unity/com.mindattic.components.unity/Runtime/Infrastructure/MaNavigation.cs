using System.Collections.Generic;
using MindAttic.Components.Unity.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// Wires explicit uGUI navigation (<see cref="Navigation.Mode.Explicit"/>) from rows of selectables through
    /// the engine-free <see cref="NavigationGraph"/>, so arrows, WASD, the d-pad and sticks move predictably and
    /// never escape a dialog or land on a disabled or hidden control.
    /// </summary>
    public static class MaNavigation
    {
        /// <summary>Sets explicit navigation on every selectable in <paramref name="rows"/>; returns the graph used.</summary>
        public static NavigationGraph Wire(IReadOnlyList<Selectable[]> rows, bool wrapVertical = true, bool wrapHorizontal = false)
        {
            var enabled = new List<bool[]>(rows.Count);
            foreach (var row in rows)
            {
                var flags = new bool[row?.Length ?? 0];
                for (int c = 0; c < flags.Length; c++) flags[c] = IsNavigable(row[c]);
                enabled.Add(flags);
            }
            var graph = new NavigationGraph(enabled, wrapVertical, wrapHorizontal);
            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row == null) continue;
                for (int c = 0; c < row.Length; c++)
                {
                    var s = row[c];
                    if (s == null) continue;
                    var from = new NavCell(r, c);
                    s.navigation = new Navigation
                    {
                        mode = Navigation.Mode.Explicit,
                        wrapAround = false,
                        selectOnUp = At(rows, graph.Move(from, NavDirection.Up)),
                        selectOnDown = At(rows, graph.Move(from, NavDirection.Down)),
                        selectOnLeft = At(rows, graph.Move(from, NavDirection.Left)),
                        selectOnRight = At(rows, graph.Move(from, NavDirection.Right)),
                    };
                }
            }
            return graph;
        }

        /// <summary>The first navigable selectable (top row first), or null.</summary>
        public static Selectable First(IReadOnlyList<Selectable[]> rows)
        {
            foreach (var row in rows)
            {
                if (row == null) continue;
                foreach (var s in row)
                {
                    if (IsNavigable(s)) return s;
                }
            }
            return null;
        }

        /// <summary>True when a selectable can take focus now (exists, active in the hierarchy, interactable).</summary>
        public static bool IsNavigable(Selectable s) => s != null && s.gameObject.activeInHierarchy && s.IsInteractable();

        /// <summary>True when <paramref name="go"/> is <paramref name="root"/> or under it.</summary>
        public static bool IsWithin(GameObject go, Transform root) => go != null && root != null && go.transform.IsChildOf(root);

        static Selectable At(IReadOnlyList<Selectable[]> rows, NavCell c) => c.IsValid ? rows[c.Row][c.Col] : null;
    }
}
