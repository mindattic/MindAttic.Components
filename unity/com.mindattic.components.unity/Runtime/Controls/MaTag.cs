using MindAttic.Components.Unity.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>The colouring of a <see cref="MaTag"/>.</summary>
    public enum TagTone
    {
        /// <summary>The web tag colours (<c>tag-bg</c>, <c>tag-text</c>, <c>tag-border</c>).</summary>
        Neutral,
        /// <summary>Filled with the accent ("You", "Host").</summary>
        Accent,
        /// <summary>Outlined in success green ("Ready").</summary>
        Success,
        /// <summary>Outlined in warning amber ("Lagging").</summary>
        Warning,
        /// <summary>Outlined in danger red ("Kicked").</summary>
        Danger,
    }

    /// <summary>A small pill label (the web's <c>.ma-tag</c>): roster badges, states, categories.</summary>
    [AddComponentMenu("MindAttic/UI/Tag")]
    public sealed class MaTag : MaElement
    {
        [SerializeField] Image fill;
        [SerializeField] Image border;
        [SerializeField] TextMeshProUGUI label;
        [SerializeField] TagTone tone;

        /// <summary>Creates a tag under <paramref name="parent"/>.</summary>
        public static MaTag Create(Component parent, string text, TagTone tone = TagTone.Neutral)
        {
            var m = MaTheme.Metrics;
            var rt = MaUi.Rect("Tag: " + MaText.Short(text), ResolveParent(parent));
            var t = rt.gameObject.AddComponent<MaTag>();
            t.tone = tone;
            t.fill = rt.gameObject.AddComponent<Image>();
            t.fill.raycastTarget = false;
            t.border = MaUi.Image("Border", rt);
            MaUi.Stretch((RectTransform)t.border.transform);
            MaUi.GetOrAdd<LayoutElement>(t.border.gameObject).ignoreLayout = true;
            MaUi.HRow(rt.gameObject, 0, MaUi.Pad(m.SpaceMd + 2, m.SpaceXs), TextAnchor.MiddleCenter);
            t.label = MaUi.Text(rt, text, TextStyle.Caption);
            t.label.alignment = TextAlignmentOptions.Center;
            MaUi.Layout(rt.gameObject, minH: 34, prefH: 34, flexW: 0);
            t.ApplyCurrentTheme();
            return t;
        }

        /// <summary>The text.</summary>
        public string Text
        {
            get => label.text;
            set => label.text = value ?? "";
        }

        /// <summary>The colouring.</summary>
        public TagTone Tone
        {
            get => tone;
            set
            {
                tone = value;
                ApplyCurrentTheme();
            }
        }

        /// <inheritdoc />
        public override void ApplyTheme(ThemeTokens theme)
        {
            if (fill == null) return;
            MaSprites.Apply(fill, MaSprites.Rounded(17));
            MaSprites.Apply(border, MaSprites.Ring(17, theme.Metrics.BorderWidth));
            MaUi.Style(label, TextStyle.Caption, theme);
            label.fontStyle = FontStyles.Bold;
            Color f, b, t;
            switch (tone)
            {
                case TagTone.Accent: f = theme.accent; b = theme.accent; t = theme.onAccent; break;
                case TagTone.Success: f = theme.bg2; b = theme.success; t = theme.success; break;
                case TagTone.Warning: f = theme.bg2; b = theme.warning; t = theme.warning; break;
                case TagTone.Danger: f = theme.bg2; b = theme.danger; t = theme.danger; break;
                default: f = theme.tagBg; b = theme.tagBorder; t = theme.tagText; break;
            }
            fill.color = f;
            border.color = b;
            label.color = t;
        }

        /// <inheritdoc />
        public override MenuItemDefinition Describe() => new MenuItemDefinition(MenuItemKind.Text, Text, false);
    }
}
