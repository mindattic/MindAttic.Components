using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>The surfaces a panel can be.</summary>
    public enum PanelStyle
    {
        /// <summary>A dialog or menu card: <c>bg</c>, hairline border, large radius, soft drop shadow (the web's <c>.ma-modal__dialog</c>).</summary>
        Surface,
        /// <summary>A raised group inside a card: <c>bg2</c>, border, medium radius, no shadow.</summary>
        Raised,
        /// <summary>A recessed well (code display, track): <c>bg3</c>, border, small radius.</summary>
        Inset,
        /// <summary>A bar along an edge (toolbar): <c>bg</c>, border, no radius.</summary>
        Bar,
    }

    /// <summary>
    /// A rounded, themed panel that blocks clicks to what is behind it. Put content (and a layout group) on the
    /// panel itself; its shadow, fill and border are ignored by layout.
    /// </summary>
    [AddComponentMenu("MindAttic/UI/Panel")]
    public sealed class MaPanel : MaElement
    {
        [SerializeField] PanelStyle style;
        [SerializeField] Image shadow;
        [SerializeField] Image fill;
        [SerializeField] Image border;

        /// <summary>Creates a panel under <paramref name="parent"/>.</summary>
        public static MaPanel Create(Component parent, PanelStyle style = PanelStyle.Surface, string name = "Panel")
        {
            var rt = MaUi.Rect(name, ResolveParent(parent));
            var p = rt.gameObject.AddComponent<MaPanel>();
            p.style = style;
            if (style == PanelStyle.Surface)
            {
                p.shadow = MaUi.Image("Shadow", rt);
                Ignore(p.shadow);
            }
            p.fill = MaUi.Image("Fill", rt, raycast: true);
            Ignore(p.fill);
            p.border = MaUi.Image("Border", rt);
            Ignore(p.border);
            p.ApplyCurrentTheme();
            return p;
        }

        /// <summary>The style.</summary>
        public PanelStyle Style
        {
            get => style;
            set
            {
                style = value;
                ApplyCurrentTheme();
            }
        }

        /// <summary>The fill image (its colour follows the theme).</summary>
        public Image Fill => fill;

        /// <inheritdoc />
        public override void ApplyTheme(ThemeTokens theme)
        {
            var m = theme.Metrics;
            float radius = style == PanelStyle.Surface ? m.RadiusLg : style == PanelStyle.Raised ? m.RadiusMd : style == PanelStyle.Inset ? m.RadiusSm : 0f;
            Color fillColor = style == PanelStyle.Raised ? theme.bg2 : style == PanelStyle.Inset ? theme.bg3 : theme.bg;
            MaSprites.Apply(fill, MaSprites.Rounded(radius));
            MaUi.SetColor(fill, fillColor);
            MaSprites.Apply(border, MaSprites.Ring(radius, m.BorderWidth));
            MaUi.SetColor(border, theme.border);
            if (shadow != null)
            {
                const float blur = 28f;
                MaSprites.Apply(shadow, MaSprites.Shadow(radius, blur));
                MaUi.Stretch((RectTransform)shadow.transform, -blur, -blur + 6, -blur, -blur - 10);
                MaUi.SetColor(shadow, theme.shadow);
            }
        }

        static void Ignore(Graphic g)
        {
            MaUi.Stretch((RectTransform)g.transform);
            MaUi.GetOrAdd<LayoutElement>(g.gameObject).ignoreLayout = true;
        }
    }
}
