using System;
using System.Collections.Generic;
using MindAttic.Components.Unity.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// A horizontal row of controls that navigate Left/Right among themselves (e.g. "Cancel | Join" under a
    /// field). Buttons share the width equally unless <see cref="EqualWidths"/> is off.
    /// </summary>
    [AddComponentMenu("MindAttic/UI/Row")]
    public class MaRow : MaElement
    {
        [SerializeField] protected HorizontalLayoutGroup layout;

        /// <summary>Creates an empty row under <paramref name="parent"/>.</summary>
        public static MaRow Create(Component parent, bool equalWidths = true)
        {
            var rt = MaUi.Rect("Row", ResolveParent(parent));
            var row = rt.gameObject.AddComponent<MaRow>();
            row.layout = MaUi.HRow(rt.gameObject, MaTheme.Metrics.SpaceMd, null, TextAnchor.MiddleCenter, expandWidth: equalWidths);
            MaUi.Layout(rt.gameObject, flexW: 1);
            return row;
        }

        /// <summary>Children share the width equally.</summary>
        public bool EqualWidths
        {
            get => layout.childForceExpandWidth;
            set => layout.childForceExpandWidth = value;
        }

        /// <summary>The child alignment (centre by default).</summary>
        public TextAnchor Alignment
        {
            get => layout.childAlignment;
            set => layout.childAlignment = value;
        }

        /// <summary>Adds a button.</summary>
        public MaButton AddButton(string label, Action onClick, ButtonVariant variant = ButtonVariant.Secondary, MaIcon icon = MaIcon.None)
        {
            var b = MaButton.Create(this, label, onClick, variant, icon);
            if (EqualWidths) b.Stretch();
            NotifyNavigationChanged();
            return b;
        }

        /// <summary>Adds an icon button.</summary>
        public MaIconButton AddIconButton(MaIcon icon, Action onClick, string tooltip = null, ButtonVariant variant = ButtonVariant.Secondary)
        {
            var b = MaIconButton.Create(this, icon, onClick, tooltip, variant);
            NotifyNavigationChanged();
            return b;
        }

        /// <summary>Adds a label.</summary>
        public MaText AddText(string text, TextStyle style = TextStyle.Label)
        {
            var t = MaText.Create(this, text, style);
            t.Tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            return t;
        }

        /// <summary>Adds a flexible gap that pushes what follows to the far end.</summary>
        public void AddFlexibleSpace()
        {
            var gap = MaUi.Rect("Space", transform);
            MaUi.Layout(gap.gameObject, flexW: 1000);
        }

        /// <summary>Moves an existing element into the row.</summary>
        public T Add<T>(T element) where T : MaElement
        {
            element.transform.SetParent(transform, false);
            NotifyNavigationChanged();
            return element;
        }

        /// <summary>All controls in the row, left to right, as one navigation row.</summary>
        public override void CollectNavRows(List<Selectable[]> rows)
        {
            var cells = new List<Selectable>();
            var inner = new List<Selectable[]>();
            foreach (Transform child in transform)
            {
                if (!child.gameObject.activeSelf || !(MaElement.Of(child) is MaElement el)) continue;
                inner.Clear();
                el.CollectNavRows(inner);
                foreach (var r in inner) cells.AddRange(r);
            }
            if (cells.Count > 0) rows.Add(cells.ToArray());
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
        public override void ApplyTheme(ThemeTokens theme)
        {
            if (layout != null) layout.spacing = theme.Metrics.SpaceMd;
        }

        /// <inheritdoc />
        public override MenuItemDefinition Describe()
        {
            var rows = new List<Selectable[]>();
            CollectNavRows(rows);
            return new MenuItemDefinition(MenuItemKind.Row, name, MaNavigation.First(rows) != null);
        }
    }
}
