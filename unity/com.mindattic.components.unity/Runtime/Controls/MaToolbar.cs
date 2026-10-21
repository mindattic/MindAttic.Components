using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// A bar of buttons and labels along an edge of the screen (the web's <c>.ma-toolbar</c>): a HUD's top bar or a
    /// lobby's action strip. On its own (not inside a menu screen or dialog) it wires its own Left/Right
    /// navigation; inside one it is a single navigation row of that screen.
    /// </summary>
    [AddComponentMenu("MindAttic/UI/Toolbar")]
    public sealed class MaToolbar : MaRow, IMaNavContainer
    {
        [SerializeField] MaPanel panel;
        bool _navDirty = true;

        /// <summary>Where a toolbar docks when created on a canvas.</summary>
        public enum Dock
        {
            /// <summary>Full width along the top.</summary>
            Top,
            /// <summary>Full width along the bottom.</summary>
            Bottom,
            /// <summary>Not docked: laid out by its parent.</summary>
            None,
        }

        /// <summary>Creates a toolbar under <paramref name="parent"/> docked to <paramref name="dock"/>.</summary>
        public static MaToolbar Create(Component parent, Dock dock = Dock.Top)
        {
            var m = MaTheme.Metrics;
            var p = MaPanel.Create(ResolveParent(parent), PanelStyle.Bar, "Toolbar");
            var tb = p.gameObject.AddComponent<MaToolbar>();
            tb.panel = p;
            tb.layout = MaUi.HRow(p.gameObject, m.SpaceSm + 4, MaUi.Pad(m.SpaceLg, m.SpaceSm + 4), TextAnchor.MiddleLeft, expandWidth: false);
            float h = m.IconButtonSize + (m.SpaceSm + 4) * 2;
            var rt = p.RectTransform;
            switch (dock)
            {
                case Dock.Top:
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(0.5f, 1);
                    rt.sizeDelta = new Vector2(0, h);
                    break;
                case Dock.Bottom:
                    rt.anchorMin = new Vector2(0, 0);
                    rt.anchorMax = new Vector2(1, 0);
                    rt.pivot = new Vector2(0.5f, 0);
                    rt.sizeDelta = new Vector2(0, h);
                    break;
                default:
                    MaUi.Layout(p.gameObject, minH: h, prefH: h, flexW: 1);
                    break;
            }
            tb.ApplyCurrentTheme();
            return tb;
        }

        /// <inheritdoc />
        public void MarkNavigationDirty() => _navDirty = true;

        /// <inheritdoc />
        protected override void NotifyNavigationChanged() => _navDirty = true;

        /// <summary>Wires Left/Right navigation across the bar now (when it is not inside a screen or dialog).</summary>
        public void RebuildNavigation()
        {
            _navDirty = false;
            if (transform.parent != null && transform.parent.GetComponentInParent<IMaNavContainer>(true) != null)
            {
                base.NotifyNavigationChanged(); // the enclosing screen wires us as one of its rows
                return;
            }
            var rows = new List<Selectable[]>();
            CollectNavRows(rows);
            MaNavigation.Wire(rows, wrapVertical: false, wrapHorizontal: true);
        }

        void LateUpdate()
        {
            if (_navDirty) RebuildNavigation();
        }

        /// <inheritdoc />
        public override void ApplyTheme(ThemeTokens theme)
        {
            if (layout != null) layout.spacing = theme.Metrics.SpaceSm + 4;
        }
    }
}
