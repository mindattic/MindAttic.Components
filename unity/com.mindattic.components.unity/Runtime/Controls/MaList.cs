using System;
using System.Collections.Generic;
using MindAttic.Components.Unity.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// A scrolling list of <see cref="MaListRow"/>s in a raised panel: a lobby roster (colour swatch, name, a "Host"
    /// tag, ping on the right, a kick button), a server browser, a save-slot list. Rows with a click handler or
    /// action buttons are navigation stops; the list scrolls to keep the focused row in view.
    /// </summary>
    [AddComponentMenu("MindAttic/UI/List")]
    public sealed class MaList : MaElement
    {
        [SerializeField] MaPanel panel;
        [SerializeField] ScrollRect scroll;
        [SerializeField] RectTransform content;
        [SerializeField] MaText empty;
        [SerializeField] int maxVisibleRows = 6;

        readonly List<MaListRow> _rows = new List<MaListRow>();

        /// <summary>Creates an empty list showing up to <paramref name="maxVisibleRows"/> rows before it scrolls.</summary>
        public static MaList Create(Component parent, int maxVisibleRows = 6, string emptyText = "Nothing here yet")
        {
            var m = MaTheme.Metrics;
            var p = MaPanel.Create(ResolveParent(parent), PanelStyle.Raised, "List");
            var list = p.gameObject.AddComponent<MaList>();
            list.panel = p;
            list.maxVisibleRows = Mathf.Max(1, maxVisibleRows);

            list.scroll = p.gameObject.AddComponent<ScrollRect>();
            list.scroll.horizontal = false;
            list.scroll.movementType = ScrollRect.MovementType.Clamped;
            list.scroll.scrollSensitivity = m.RowHeight;
            var viewport = MaUi.Rect("Viewport", p.transform);
            MaUi.Stretch(viewport, m.SpaceSm, m.SpaceSm, m.SpaceSm, m.SpaceSm);
            viewport.gameObject.AddComponent<RectMask2D>();
            list.content = MaUi.Rect("Rows", viewport);
            list.content.anchorMin = new Vector2(0, 1);
            list.content.anchorMax = new Vector2(1, 1);
            list.content.pivot = new Vector2(0.5f, 1);
            list.content.sizeDelta = Vector2.zero;
            MaUi.VColumn(list.content.gameObject, 2, null, TextAnchor.UpperCenter);
            list.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            list.scroll.viewport = viewport;
            list.scroll.content = list.content;

            list.empty = MaText.Create(viewport, emptyText ?? "", TextStyle.Muted, TMPro.TextAlignmentOptions.Center);
            MaUi.Stretch(list.empty.RectTransform);
            list.UpdateSize();
            list.ApplyCurrentTheme();
            return list;
        }

        /// <summary>The rows, top to bottom.</summary>
        public IReadOnlyList<MaListRow> Rows => _rows;

        /// <summary>How many rows.</summary>
        public int Count => _rows.Count;

        /// <summary>The text shown while the list is empty.</summary>
        public string EmptyText
        {
            get => empty.Text;
            set => empty.Text = value;
        }

        /// <summary>
        /// Adds a row: <paramref name="text"/>, an optional colour <paramref name="swatch"/> dot before it, an optional
        /// <paramref name="detail"/> on the right, and an optional click handler (which makes the row focusable).
        /// </summary>
        public MaListRow AddRow(string text, Color? swatch = null, string detail = null, Action onClick = null)
        {
            var row = MaListRow.Create(content, text, swatch, detail, onClick);
            row.Owner = this;
            _rows.Add(row);
            UpdateSize();
            NotifyNavigationChanged();
            return row;
        }

        /// <summary>Removes a row.</summary>
        public void Remove(MaListRow row)
        {
            if (row == null || !_rows.Remove(row)) return;
            MaUi.Destroy(row.gameObject);
            UpdateSize();
            NotifyNavigationChanged();
        }

        /// <summary>Removes every row.</summary>
        public void Clear()
        {
            foreach (var r in _rows)
            {
                if (r != null) MaUi.Destroy(r.gameObject);
            }
            _rows.Clear();
            UpdateSize();
            NotifyNavigationChanged();
        }

        /// <summary>One navigation row per list row that has a focusable part.</summary>
        public override void CollectNavRows(List<Selectable[]> rows)
        {
            foreach (var r in _rows)
            {
                if (r != null && r.gameObject.activeSelf) r.CollectNavRows(rows);
            }
        }

        /// <inheritdoc />
        public override Selectable PrimarySelectable
        {
            get
            {
                var rows = new List<Selectable[]>();
                CollectNavRows(rows);
                return MaNavigation.First(rows);
            }
        }

        /// <inheritdoc />
        public override void ApplyTheme(ThemeTokens theme) => UpdateSize();

        /// <inheritdoc />
        public override MenuItemDefinition Describe() => new MenuItemDefinition(MenuItemKind.List, name, PrimarySelectable != null);

        internal void ScrollIntoView(RectTransform row)
        {
            if (scroll == null || content == null) return;
            Canvas.ForceUpdateCanvases();
            float viewH = scroll.viewport.rect.height;
            float contentH = content.rect.height;
            if (contentH <= viewH) return;
            float top = -row.anchoredPosition.y - row.rect.height * (1 - row.pivot.y);
            float bottom = top + row.rect.height;
            float y = content.anchoredPosition.y;
            if (top < y) y = top;
            else if (bottom > y + viewH) y = bottom - viewH;
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, Mathf.Clamp(y, 0, contentH - viewH));
        }

        void UpdateSize()
        {
            if (panel == null) return;
            var m = MaTheme.Metrics;
            int shown = Mathf.Clamp(_rows.Count, 1, maxVisibleRows);
            float h = shown * (m.RowHeight + 2) + m.SpaceSm * 2;
            MaUi.Layout(panel.gameObject, minH: h, prefH: h, flexW: 1);
            if (empty != null) empty.gameObject.SetActive(_rows.Count == 0);
        }
    }
}
