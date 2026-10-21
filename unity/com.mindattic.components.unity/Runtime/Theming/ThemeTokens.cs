using System;
using MindAttic.Components.Unity.Core;
using TMPro;
using UnityEngine;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// A theme as an asset: every colour token of the web themes (same names: <c>bg</c>, <c>bg2</c>, <c>bg3</c>,
    /// <c>border</c>, <c>accent</c>, <c>accent2</c>, <c>text</c>, <c>text2</c>, <c>text3</c>, <c>tag-*</c>,
    /// <c>rule</c>) plus the Unity additions (<c>danger</c>, <c>success</c>, <c>warning</c>, <c>on-accent</c>,
    /// <c>overlay</c>, <c>shadow</c>), the size metrics and an optional font. <see cref="MaTheme.Dark"/> (the
    /// default) and <see cref="MaTheme.Light"/> are built in; make your own with Assets > Create > MindAttic >
    /// Theme Tokens, or <see cref="FromCss"/> from any web theme stylesheet. Editing the current theme's asset
    /// restyles every component live.
    /// </summary>
    [CreateAssetMenu(menuName = "MindAttic/Theme Tokens", fileName = "Theme Tokens", order = 300)]
    public sealed class ThemeTokens : ScriptableObject
    {
        [Tooltip("The theme's name: dark, light, or the name of the web theme it came from.")]
        [SerializeField] string themeName = "dark";

        [Header("Web palette (web/themes)")]
        public Color bg;
        public Color bg2;
        public Color bg3;
        public Color border;
        public Color accent;
        public Color accent2;
        public Color text;
        public Color text2;
        public Color text3;
        public Color tagBg;
        public Color tagText;
        public Color tagBorder;
        public Color tagHoverBg;
        public Color tagHoverText;
        public Color rule;

        [Header("Unity additions")]
        public Color danger;
        public Color danger2;
        public Color success;
        public Color warning;
        public Color onAccent;
        public Color overlay;
        public Color shadow;

        [Header("Type and sizes")]
        [Tooltip("Font for every label. Empty uses TMP Settings' default font (LiberationSans SDF).")]
        public TMP_FontAsset font;
        public ThemeMetrics metrics = new ThemeMetrics();

        [NonSerialized] ThemePalette _palette;

        /// <summary>The theme's name.</summary>
        public string ThemeName
        {
            get => themeName;
            set { themeName = value; Changed(); }
        }

        /// <summary>The colours as an engine-free palette (cached until a field changes).</summary>
        public ThemePalette Palette
        {
            get
            {
                if (_palette == null)
                {
                    var p = ThemePalette.BuiltIn(themeName) ?? ThemePalette.Dark;
                    p = p.Renamed(string.IsNullOrEmpty(themeName) ? "custom" : themeName);
                    foreach (string token in ThemeTokenNames.All) p = p.With(token, this[token].ToThemeColor());
                    _palette = p;
                }
                return _palette;
            }
        }

        /// <summary>True when the background is dark.</summary>
        public bool IsDark => Palette.IsDark;

        /// <summary>The metrics, never null.</summary>
        public ThemeMetrics Metrics => metrics ?? (metrics = new ThemeMetrics());

        /// <summary>A colour by its web token name (<c>bg</c>, <c>tag-hover-text</c>, <c>danger</c>, ...).</summary>
        public Color this[string token]
        {
            get => Field(token);
            set { SetField(token, value); Changed(); }
        }

        /// <summary>A new, unsaved theme with the given palette (and metrics, default when null).</summary>
        public static ThemeTokens Create(ThemePalette palette, ThemeMetrics metrics = null)
        {
            if (palette == null) throw new ArgumentNullException(nameof(palette));
            var t = CreateInstance<ThemeTokens>();
            t.name = "Theme " + palette.Name;
            t.metrics = metrics != null ? metrics.Clone() : new ThemeMetrics();
            t.SetPalette(palette);
            return t;
        }

        /// <summary>A new copy of the built-in dark palette.</summary>
        public static ThemeTokens CreateDark() => Create(ThemePalette.Dark);

        /// <summary>A new copy of the built-in light palette.</summary>
        public static ThemeTokens CreateLight() => Create(ThemePalette.Light);

        /// <summary>
        /// A theme read from a web theme stylesheet: <c>:root</c> for <c>dark</c>, <c>[data-theme="name"]</c>
        /// otherwise. Tokens the CSS leaves out come from the built-in palette of matching darkness.
        /// </summary>
        public static ThemeTokens FromCss(string css, string themeName) => Create(ThemePalette.FromCss(css, themeName));

        /// <summary>Copies every colour (and the name) from a palette.</summary>
        public void SetPalette(ThemePalette palette)
        {
            if (palette == null) throw new ArgumentNullException(nameof(palette));
            themeName = palette.Name;
            foreach (var kv in palette.Colors) SetField(kv.Key, kv.Value.ToColor());
            Changed();
        }

        /// <summary>Copies colours, metrics and font from another theme.</summary>
        public void CopyFrom(ThemeTokens other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            themeName = other.themeName;
            foreach (string token in ThemeTokenNames.All) SetField(token, other.Field(token));
            metrics = other.Metrics.Clone();
            font = other.font;
            Changed();
        }

        /// <summary>The palette as CSS custom properties (paste into a web theme to match this asset).</summary>
        public string ToCss() => Palette.ToCss();

        void OnValidate() => Changed();

        void Changed()
        {
            _palette = null;
            if (MaTheme.IsCurrent(this)) MaTheme.NotifyChanged();
        }

        Color Field(string token)
        {
            switch (token)
            {
                case ThemeTokenNames.Bg: return bg;
                case ThemeTokenNames.Bg2: return bg2;
                case ThemeTokenNames.Bg3: return bg3;
                case ThemeTokenNames.Border: return border;
                case ThemeTokenNames.Accent: return accent;
                case ThemeTokenNames.Accent2: return accent2;
                case ThemeTokenNames.Text: return text;
                case ThemeTokenNames.Text2: return text2;
                case ThemeTokenNames.Text3: return text3;
                case ThemeTokenNames.TagBg: return tagBg;
                case ThemeTokenNames.TagText: return tagText;
                case ThemeTokenNames.TagBorder: return tagBorder;
                case ThemeTokenNames.TagHoverBg: return tagHoverBg;
                case ThemeTokenNames.TagHoverText: return tagHoverText;
                case ThemeTokenNames.Rule: return rule;
                case ThemeTokenNames.Danger: return danger;
                case ThemeTokenNames.Danger2: return danger2;
                case ThemeTokenNames.Success: return success;
                case ThemeTokenNames.Warning: return warning;
                case ThemeTokenNames.OnAccent: return onAccent;
                case ThemeTokenNames.Overlay: return overlay;
                case ThemeTokenNames.Shadow: return shadow;
                default: throw new ArgumentException("Unknown theme token '" + token + "'", nameof(token));
            }
        }

        void SetField(string token, Color c)
        {
            switch (token)
            {
                case ThemeTokenNames.Bg: bg = c; break;
                case ThemeTokenNames.Bg2: bg2 = c; break;
                case ThemeTokenNames.Bg3: bg3 = c; break;
                case ThemeTokenNames.Border: border = c; break;
                case ThemeTokenNames.Accent: accent = c; break;
                case ThemeTokenNames.Accent2: accent2 = c; break;
                case ThemeTokenNames.Text: text = c; break;
                case ThemeTokenNames.Text2: text2 = c; break;
                case ThemeTokenNames.Text3: text3 = c; break;
                case ThemeTokenNames.TagBg: tagBg = c; break;
                case ThemeTokenNames.TagText: tagText = c; break;
                case ThemeTokenNames.TagBorder: tagBorder = c; break;
                case ThemeTokenNames.TagHoverBg: tagHoverBg = c; break;
                case ThemeTokenNames.TagHoverText: tagHoverText = c; break;
                case ThemeTokenNames.Rule: rule = c; break;
                case ThemeTokenNames.Danger: danger = c; break;
                case ThemeTokenNames.Danger2: danger2 = c; break;
                case ThemeTokenNames.Success: success = c; break;
                case ThemeTokenNames.Warning: warning = c; break;
                case ThemeTokenNames.OnAccent: onAccent = c; break;
                case ThemeTokenNames.Overlay: overlay = c; break;
                case ThemeTokenNames.Shadow: shadow = c; break;
                default: throw new ArgumentException("Unknown theme token '" + token + "'", nameof(token));
            }
        }
    }

    /// <summary>Conversions between the engine-free <see cref="ThemeColor"/> and <see cref="Color"/>.</summary>
    public static class ThemeColorExtensions
    {
        /// <summary>To a Unity colour (sRGB values, as uGUI expects).</summary>
        public static Color ToColor(this ThemeColor c) => new Color(c.R, c.G, c.B, c.A);

        /// <summary>To an engine-free colour.</summary>
        public static ThemeColor ToThemeColor(this Color c) => new ThemeColor(c.r, c.g, c.b, c.a);
    }
}
