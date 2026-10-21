using MindAttic.Components.Unity.Core;
using TMPro;
using UnityEngine;

namespace MindAttic.Components.Unity
{
    /// <summary>A themed TextMesh Pro label in one of the package's <see cref="TextStyle"/>s.</summary>
    [AddComponentMenu("MindAttic/UI/Text")]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class MaText : MaElement
    {
        [SerializeField] TextMeshProUGUI tmp;
        [SerializeField] TextStyle style = TextStyle.Body;
        [SerializeField] bool hasColorOverride;
        [SerializeField] Color colorOverride;

        /// <summary>Creates a label under <paramref name="parent"/>; body text wraps, other styles stay on one line.</summary>
        public static MaText Create(Component parent, string text, TextStyle style = TextStyle.Body,
            TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            var tmp = MaUi.Text(ResolveParent(parent), text, style, "Text");
            tmp.gameObject.name = "Text: " + Short(text);
            var t = tmp.gameObject.AddComponent<MaText>();
            t.tmp = tmp;
            t.style = style;
            tmp.alignment = alignment;
            bool wraps = style == TextStyle.Body || style == TextStyle.Label || style == TextStyle.Muted || style == TextStyle.Caption;
            tmp.textWrappingMode = wraps ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.ApplyCurrentTheme();
            return t;
        }

        /// <summary>The text.</summary>
        public string Text
        {
            get => tmp.text;
            set
            {
                tmp.text = value ?? "";
                gameObject.name = "Text: " + Short(value);
            }
        }

        /// <summary>The style.</summary>
        public TextStyle Style
        {
            get => style;
            set
            {
                style = value;
                ApplyCurrentTheme();
            }
        }

        /// <summary>The TextMesh Pro component, for anything not covered here.</summary>
        public TextMeshProUGUI Tmp => tmp;

        /// <summary>Overrides the style's colour (null returns to the theme colour).</summary>
        public MaText WithColor(Color? color)
        {
            hasColorOverride = color.HasValue;
            if (color.HasValue) colorOverride = color.Value;
            ApplyCurrentTheme();
            return this;
        }

        /// <summary>Sets the alignment.</summary>
        public MaText WithAlignment(TextAlignmentOptions alignment)
        {
            tmp.alignment = alignment;
            return this;
        }

        /// <inheritdoc />
        public override void ApplyTheme(ThemeTokens theme)
        {
            if (tmp == null) tmp = GetComponent<TextMeshProUGUI>();
            MaUi.Style(tmp, style, theme);
            if (hasColorOverride) tmp.color = colorOverride;
        }

        /// <inheritdoc />
        public override MenuItemDefinition Describe() => new MenuItemDefinition(MenuItemKind.Text, Text, false);

        internal static string Short(string s)
        {
            s = s ?? "";
            return s.Length > 24 ? s.Substring(0, 24) + "..." : s;
        }
    }
}
