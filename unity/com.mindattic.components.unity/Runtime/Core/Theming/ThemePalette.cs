using System;
using System.Collections.Generic;

namespace MindAttic.Components.Unity.Core
{
    /// <summary>
    /// A complete, resolved set of theme colours keyed by <see cref="ThemeTokenNames"/>. <see cref="Dark"/> and
    /// <see cref="Light"/> carry the exact values of <c>web/themes/Dark/dark.css</c> and
    /// <c>web/themes/Light/light.css</c> (Core.Tests parse those files and compare), so a Unity game and a
    /// MindAttic page share one palette. Immutable: <see cref="With"/> returns a copy.
    /// </summary>
    public sealed class ThemePalette
    {
        readonly Dictionary<string, ThemeColor> _colors;

        /// <summary>The theme's name (<c>dark</c>, <c>light</c>, or whatever a CSS file called it).</summary>
        public string Name { get; }

        /// <summary>The font stack the CSS named (<c>--font-resume</c>), for reference.</summary>
        public string FontFamily { get; }

        ThemePalette(string name, Dictionary<string, ThemeColor> colors, string fontFamily)
        {
            Name = name;
            _colors = colors;
            FontFamily = fontFamily;
        }

        /// <summary>The MindAttic dark theme (the web default, <c>:root</c>).</summary>
        public static ThemePalette Dark { get; } = Build("dark", "'Outfit',system-ui,sans-serif",
            (ThemeTokenNames.Bg, "#0d0f12"), (ThemeTokenNames.Bg2, "#13161b"), (ThemeTokenNames.Bg3, "#1a1e25"),
            (ThemeTokenNames.Border, "#2a2f3a"), (ThemeTokenNames.Accent, "#6ba3e8"), (ThemeTokenNames.Accent2, "#90bbf0"),
            (ThemeTokenNames.Text, "#e8eaf0"), (ThemeTokenNames.Text2, "#9aa0b0"), (ThemeTokenNames.Text3, "#5c6275"),
            (ThemeTokenNames.TagBg, "#1a2030"), (ThemeTokenNames.TagText, "#6ba3e8"), (ThemeTokenNames.TagBorder, "#2a3a54"),
            (ThemeTokenNames.TagHoverBg, "#6ba3e8"), (ThemeTokenNames.TagHoverText, "#0d0f12"), (ThemeTokenNames.Rule, "#3d6fa3"),
            (ThemeTokenNames.Danger, "#e5484d"), (ThemeTokenNames.Danger2, "#ff6b70"), (ThemeTokenNames.Success, "#3fb950"),
            (ThemeTokenNames.Warning, "#d29922"), (ThemeTokenNames.OnAccent, "#0d0f12"),
            (ThemeTokenNames.Overlay, "rgba(0,0,0,0.7)"), (ThemeTokenNames.Shadow, "rgba(0,0,0,0.4)"));

        /// <summary>The MindAttic light theme (<c>[data-theme="light"]</c>).</summary>
        public static ThemePalette Light { get; } = Build("light", "'Outfit',system-ui,sans-serif",
            (ThemeTokenNames.Bg, "#ffffff"), (ThemeTokenNames.Bg2, "#f8f9fb"), (ThemeTokenNames.Bg3, "#f0f2f5"),
            (ThemeTokenNames.Border, "#e2e6ea"), (ThemeTokenNames.Accent, "#1b4f8c"), (ThemeTokenNames.Accent2, "#2563b0"),
            (ThemeTokenNames.Text, "#1a1a2e"), (ThemeTokenNames.Text2, "#4a5568"), (ThemeTokenNames.Text3, "#718096"),
            (ThemeTokenNames.TagBg, "#eef2f8"), (ThemeTokenNames.TagText, "#1b4f8c"), (ThemeTokenNames.TagBorder, "#c5d5eb"),
            (ThemeTokenNames.TagHoverBg, "#1b4f8c"), (ThemeTokenNames.TagHoverText, "#ffffff"), (ThemeTokenNames.Rule, "#2c5f8a"),
            (ThemeTokenNames.Danger, "#c62f35"), (ThemeTokenNames.Danger2, "#e5484d"), (ThemeTokenNames.Success, "#1a7f37"),
            (ThemeTokenNames.Warning, "#9a6700"), (ThemeTokenNames.OnAccent, "#ffffff"),
            (ThemeTokenNames.Overlay, "rgba(0,0,0,0.5)"), (ThemeTokenNames.Shadow, "rgba(0,0,0,0.18)"));

        /// <summary>The built-in palette for a name (<c>dark</c> or <c>light</c>, case-insensitive), or null.</summary>
        public static ThemePalette BuiltIn(string name)
        {
            switch ((name ?? "").Trim().ToLowerInvariant())
            {
                case "dark": return Dark;
                case "light": return Light;
                default: return null;
            }
        }

        /// <summary>The colour of a token; throws <see cref="KeyNotFoundException"/> for an unknown name.</summary>
        public ThemeColor this[string token] => _colors[token];

        /// <summary>The colour of a token, without throwing.</summary>
        public bool TryGet(string token, out ThemeColor color) => _colors.TryGetValue(token, out color);

        /// <summary>Every token and its colour, in <see cref="ThemeTokenNames.All"/> order.</summary>
        public IEnumerable<KeyValuePair<string, ThemeColor>> Colors
        {
            get
            {
                foreach (string name in ThemeTokenNames.All)
                    yield return new KeyValuePair<string, ThemeColor>(name, _colors[name]);
            }
        }

        /// <summary>True when the background is dark (relative luminance under 0.5).</summary>
        public bool IsDark => Contrast.Luminance(Bg) < 0.5;

        /// <summary><c>--bg</c>.</summary>
        public ThemeColor Bg => _colors[ThemeTokenNames.Bg];
        /// <summary><c>--bg2</c>.</summary>
        public ThemeColor Bg2 => _colors[ThemeTokenNames.Bg2];
        /// <summary><c>--bg3</c>.</summary>
        public ThemeColor Bg3 => _colors[ThemeTokenNames.Bg3];
        /// <summary><c>--border</c>.</summary>
        public ThemeColor Border => _colors[ThemeTokenNames.Border];
        /// <summary><c>--accent</c>.</summary>
        public ThemeColor Accent => _colors[ThemeTokenNames.Accent];
        /// <summary><c>--accent2</c>.</summary>
        public ThemeColor Accent2 => _colors[ThemeTokenNames.Accent2];
        /// <summary><c>--text</c>.</summary>
        public ThemeColor Text => _colors[ThemeTokenNames.Text];
        /// <summary><c>--text2</c>.</summary>
        public ThemeColor Text2 => _colors[ThemeTokenNames.Text2];
        /// <summary><c>--text3</c>.</summary>
        public ThemeColor Text3 => _colors[ThemeTokenNames.Text3];
        /// <summary><c>--tag-bg</c>.</summary>
        public ThemeColor TagBg => _colors[ThemeTokenNames.TagBg];
        /// <summary><c>--tag-text</c>.</summary>
        public ThemeColor TagText => _colors[ThemeTokenNames.TagText];
        /// <summary><c>--tag-border</c>.</summary>
        public ThemeColor TagBorder => _colors[ThemeTokenNames.TagBorder];
        /// <summary><c>--tag-hover-bg</c>.</summary>
        public ThemeColor TagHoverBg => _colors[ThemeTokenNames.TagHoverBg];
        /// <summary><c>--tag-hover-text</c>.</summary>
        public ThemeColor TagHoverText => _colors[ThemeTokenNames.TagHoverText];
        /// <summary><c>--rule</c>.</summary>
        public ThemeColor Rule => _colors[ThemeTokenNames.Rule];
        /// <summary><c>--danger</c>.</summary>
        public ThemeColor Danger => _colors[ThemeTokenNames.Danger];
        /// <summary><c>--danger2</c>.</summary>
        public ThemeColor Danger2 => _colors[ThemeTokenNames.Danger2];
        /// <summary><c>--success</c>.</summary>
        public ThemeColor Success => _colors[ThemeTokenNames.Success];
        /// <summary><c>--warning</c>.</summary>
        public ThemeColor Warning => _colors[ThemeTokenNames.Warning];
        /// <summary><c>--on-accent</c>.</summary>
        public ThemeColor OnAccent => _colors[ThemeTokenNames.OnAccent];
        /// <summary><c>--overlay</c>.</summary>
        public ThemeColor Overlay => _colors[ThemeTokenNames.Overlay];
        /// <summary><c>--shadow</c>.</summary>
        public ThemeColor Shadow => _colors[ThemeTokenNames.Shadow];

        /// <summary>A copy with one token changed.</summary>
        public ThemePalette With(string token, ThemeColor color)
        {
            if (!_colors.ContainsKey(token)) throw new KeyNotFoundException("Unknown theme token '" + token + "'");
            var copy = new Dictionary<string, ThemeColor>(_colors, StringComparer.OrdinalIgnoreCase) { [token] = color };
            return new ThemePalette(Name, copy, FontFamily);
        }

        /// <summary>A copy with a different name.</summary>
        public ThemePalette Renamed(string name) =>
            new ThemePalette(name, new Dictionary<string, ThemeColor>(_colors, StringComparer.OrdinalIgnoreCase), FontFamily);

        /// <summary>
        /// A palette from raw CSS token values (as <see cref="CssTokenParser.ReadTheme"/> returns them). Tokens the
        /// CSS leaves out, or whose value isn't a plain colour (a gradient, a <c>var()</c>), come from
        /// <paramref name="fallback"/> (by default the built-in palette whose darkness matches the CSS background).
        /// </summary>
        public static ThemePalette FromTokens(string name, IReadOnlyDictionary<string, string> tokens, ThemePalette fallback = null)
        {
            if (tokens == null) throw new ArgumentNullException(nameof(tokens));
            if (fallback == null)
            {
                fallback = BuiltIn(name);
                if (fallback == null)
                {
                    bool dark = !(tokens.TryGetValue(ThemeTokenNames.Bg, out string bg) && ThemeColor.TryParse(bg, out var bgc))
                                || Contrast.Luminance(bgc) < 0.5;
                    fallback = dark ? Dark : Light;
                }
            }

            var colors = new Dictionary<string, ThemeColor>(StringComparer.OrdinalIgnoreCase);
            foreach (string token in ThemeTokenNames.All)
            {
                colors[token] = tokens.TryGetValue(token, out string raw) && ThemeColor.TryParse(raw, out var c)
                    ? c
                    : fallback[token];
            }
            string font = tokens.TryGetValue(ThemeTokenNames.Font, out string f) ? f : fallback.FontFamily;
            return new ThemePalette(name, colors, font);
        }

        /// <summary>Parses one theme out of a stylesheet (see <see cref="CssTokenParser.ReadTheme"/>), then <see cref="FromTokens"/>.</summary>
        public static ThemePalette FromCss(string css, string themeName, ThemePalette fallback = null) =>
            FromTokens(themeName, CssTokenParser.ReadTheme(css, themeName), fallback);

        /// <summary>
        /// The palette as a CSS rule (<c>:root</c> for <c>dark</c>, else <c>[data-theme="name"]</c>), every token
        /// one declaration, so <see cref="FromCss"/> reads it back unchanged.
        /// </summary>
        public string ToCss()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(Name == "dark" ? ":root" : "[data-theme=\"" + Name + "\"]").Append(" {\n");
            foreach (var kv in Colors) sb.Append("  --").Append(kv.Key).Append(':').Append(kv.Value.ToHex()).Append(";\n");
            if (!string.IsNullOrEmpty(FontFamily)) sb.Append("  --").Append(ThemeTokenNames.Font).Append(':').Append(FontFamily).Append(";\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        static ThemePalette Build(string name, string font, params (string token, string css)[] values)
        {
            var colors = new Dictionary<string, ThemeColor>(StringComparer.OrdinalIgnoreCase);
            foreach (var (token, css) in values) colors[token] = ThemeColor.Parse(css);
            foreach (string token in ThemeTokenNames.All)
            {
                if (!colors.ContainsKey(token)) throw new InvalidOperationException("Built-in palette '" + name + "' is missing " + token);
            }
            return new ThemePalette(name, colors, font);
        }
    }
}
