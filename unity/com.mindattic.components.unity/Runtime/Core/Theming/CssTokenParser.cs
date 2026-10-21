using System;
using System.Collections.Generic;
using System.Text;

namespace MindAttic.Components.Unity.Core
{
    /// <summary>One CSS rule's selector and the custom properties (<c>--name: value</c>) it declares.</summary>
    public sealed class CssTokenRule
    {
        /// <summary>The rule's selector, trimmed (e.g. <c>:root</c> or <c>[data-theme="light"]</c>).</summary>
        public string Selector { get; }

        /// <summary>Custom properties by name without the leading <c>--</c>, in declaration order of first appearance.</summary>
        public IReadOnlyDictionary<string, string> Tokens { get; }

        internal CssTokenRule(string selector, Dictionary<string, string> tokens)
        {
            Selector = selector;
            Tokens = tokens;
        }
    }

    /// <summary>
    /// Reads the custom-property palettes out of a web theme stylesheet (<c>web/themes/Dark/dark.css</c>,
    /// <c>web/themes/Light/light.css</c>, or any theme written the same way). Only top-level rules are read;
    /// <c>@media</c>/<c>@keyframes</c> blocks and ordinary properties are skipped.
    /// </summary>
    public static class CssTokenParser
    {
        /// <summary>Every top-level rule that declares at least one custom property.</summary>
        public static IReadOnlyList<CssTokenRule> ParseRules(string css)
        {
            var rules = new List<CssTokenRule>();
            if (string.IsNullOrEmpty(css)) return rules;
            string text = StripComments(css);

            int i = 0;
            while (i < text.Length)
            {
                int open = text.IndexOf('{', i);
                if (open < 0) break;
                string selector = text.Substring(i, open - i).Trim();
                int close = MatchingBrace(text, open);
                if (close < 0) break;
                string body = text.Substring(open + 1, close - open - 1);
                i = close + 1;

                if (selector.StartsWith("@")) continue; // at-rule block: not a palette
                var tokens = ParseDeclarations(body);
                if (tokens.Count > 0) rules.Add(new CssTokenRule(selector, tokens));
            }
            return rules;
        }

        /// <summary>
        /// The tokens a theme defines, merged in source order: <c>:root</c> (when <paramref name="themeName"/> is
        /// <c>dark</c>, the web default) and every rule whose selector is <c>[data-theme="name"]</c> (optionally
        /// prefixed by <c>html</c>). Names compare case-insensitively.
        /// </summary>
        public static IReadOnlyDictionary<string, string> ReadTheme(string css, string themeName)
        {
            var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string want = (themeName ?? "").Trim().ToLowerInvariant();
            foreach (var rule in ParseRules(css))
            {
                if (!SelectsTheme(rule.Selector, want)) continue;
                foreach (var kv in rule.Tokens) merged[kv.Key] = kv.Value;
            }
            return merged;
        }

        /// <summary>The theme names a stylesheet defines through <c>[data-theme="name"]</c> selectors (plus <c>dark</c> for <c>:root</c>).</summary>
        public static IReadOnlyList<string> ThemeNames(string css)
        {
            var names = new List<string>();
            foreach (var rule in ParseRules(css))
            {
                foreach (string sel in rule.Selector.Split(','))
                {
                    string s = sel.Trim();
                    string name = s == ":root" ? "dark" : ThemeNameOf(s);
                    if (name != null && !names.Contains(name)) names.Add(name);
                }
            }
            return names;
        }

        static bool SelectsTheme(string selector, string want)
        {
            foreach (string sel in selector.Split(','))
            {
                string s = sel.Trim();
                if (s == ":root" && want == "dark") return true;
                if (ThemeNameOf(s) == want) return true;
            }
            return false;
        }

        static string ThemeNameOf(string selector)
        {
            string s = selector.StartsWith("html", StringComparison.OrdinalIgnoreCase) ? selector.Substring(4) : selector;
            s = s.Replace(" ", "");
            const string prefix = "[data-theme=";
            if (!s.StartsWith(prefix) || !s.EndsWith("]")) return null;
            string value = s.Substring(prefix.Length, s.Length - prefix.Length - 1).Trim('"', '\'');
            return value.ToLowerInvariant();
        }

        static Dictionary<string, string> ParseDeclarations(string body)
        {
            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string decl in SplitTopLevel(body, ';'))
            {
                int colon = decl.IndexOf(':');
                if (colon < 0) continue;
                string name = decl.Substring(0, colon).Trim();
                if (!name.StartsWith("--")) continue;
                string value = decl.Substring(colon + 1).Trim();
                tokens[name.Substring(2)] = value;
            }
            return tokens;
        }

        static IEnumerable<string> SplitTopLevel(string s, char separator)
        {
            int depth = 0;
            char quote = '\0';
            var current = new StringBuilder();
            foreach (char c in s)
            {
                if (quote != '\0')
                {
                    if (c == quote) quote = '\0';
                }
                else if (c == '"' || c == '\'') quote = c;
                else if (c == '(') depth++;
                else if (c == ')') depth--;
                else if (c == separator && depth == 0)
                {
                    yield return current.ToString();
                    current.Clear();
                    continue;
                }
                current.Append(c);
            }
            if (current.Length > 0) yield return current.ToString();
        }

        static int MatchingBrace(string s, int open)
        {
            int depth = 0;
            char quote = '\0';
            for (int i = open; i < s.Length; i++)
            {
                char c = s[i];
                if (quote != '\0')
                {
                    if (c == quote) quote = '\0';
                    continue;
                }
                if (c == '"' || c == '\'') quote = c;
                else if (c == '{') depth++;
                else if (c == '}' && --depth == 0) return i;
            }
            return -1;
        }

        static string StripComments(string css)
        {
            var sb = new StringBuilder(css.Length);
            int i = 0;
            while (i < css.Length)
            {
                if (i + 1 < css.Length && css[i] == '/' && css[i + 1] == '*')
                {
                    int end = css.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    i = end < 0 ? css.Length : end + 2;
                    continue;
                }
                sb.Append(css[i]);
                i++;
            }
            return sb.ToString();
        }
    }
}
