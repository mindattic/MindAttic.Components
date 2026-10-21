using System.Collections.Generic;

namespace MindAttic.Components.Unity.Core
{
    /// <summary>
    /// The token names, spelled as the web themes spell their CSS custom properties without the leading
    /// <c>--</c>. The first fifteen are the shared set every <c>web/themes</c> palette defines; the rest are
    /// Unity additions (the web keeps these colours in each component's own CSS) that a theme's CSS may also
    /// set, e.g. <c>--danger:#e5484d</c>.
    /// </summary>
    public static class ThemeTokenNames
    {
        /// <summary>Page background (<c>--bg</c>); dialogs and menu panels.</summary>
        public const string Bg = "bg";
        /// <summary>Raised surface (<c>--bg2</c>); buttons and fields at rest.</summary>
        public const string Bg2 = "bg2";
        /// <summary>Hover surface (<c>--bg3</c>); tracks and hovered rows.</summary>
        public const string Bg3 = "bg3";
        /// <summary>Hairline borders (<c>--border</c>).</summary>
        public const string Border = "border";
        /// <summary>Brand accent (<c>--accent</c>): primary fills, focus rings, slider fills.</summary>
        public const string Accent = "accent";
        /// <summary>Lighter accent (<c>--accent2</c>): a hovered primary.</summary>
        public const string Accent2 = "accent2";
        /// <summary>Body text (<c>--text</c>).</summary>
        public const string Text = "text";
        /// <summary>Secondary text (<c>--text2</c>): labels, idle button text.</summary>
        public const string Text2 = "text2";
        /// <summary>Muted text (<c>--text3</c>): placeholders, captions.</summary>
        public const string Text3 = "text3";
        /// <summary>Tag pill fill (<c>--tag-bg</c>).</summary>
        public const string TagBg = "tag-bg";
        /// <summary>Tag pill text (<c>--tag-text</c>).</summary>
        public const string TagText = "tag-text";
        /// <summary>Tag pill outline (<c>--tag-border</c>).</summary>
        public const string TagBorder = "tag-border";
        /// <summary>Hovered tag fill (<c>--tag-hover-bg</c>).</summary>
        public const string TagHoverBg = "tag-hover-bg";
        /// <summary>Hovered tag text (<c>--tag-hover-text</c>).</summary>
        public const string TagHoverText = "tag-hover-text";
        /// <summary>Section rules (<c>--rule</c>).</summary>
        public const string Rule = "rule";

        /// <summary>Destructive actions (Unity addition; the web's <c>.ma-btn</c> red).</summary>
        public const string Danger = "danger";
        /// <summary>A hovered destructive action's outline (Unity addition).</summary>
        public const string Danger2 = "danger2";
        /// <summary>Success toasts and states (Unity addition).</summary>
        public const string Success = "success";
        /// <summary>Warning toasts and states (Unity addition).</summary>
        public const string Warning = "warning";
        /// <summary>Text on an accent or danger fill (Unity addition; the web uses <c>--bg</c> on a primary button).</summary>
        public const string OnAccent = "on-accent";
        /// <summary>The dimmed backdrop behind a modal (Unity addition; the web's <c>rgba(0,0,0,0.7)</c>).</summary>
        public const string Overlay = "overlay";
        /// <summary>Drop shadows under panels and dialogs (Unity addition).</summary>
        public const string Shadow = "shadow";

        /// <summary>The font stack token (<c>--font-resume</c>); informational in Unity, which uses a TMP font asset.</summary>
        public const string Font = "font-resume";

        /// <summary>The fifteen colour tokens every web theme defines.</summary>
        public static readonly IReadOnlyList<string> Web = new[]
        {
            Bg, Bg2, Bg3, Border, Accent, Accent2, Text, Text2, Text3,
            TagBg, TagText, TagBorder, TagHoverBg, TagHoverText, Rule,
        };

        /// <summary>The Unity-only colour tokens.</summary>
        public static readonly IReadOnlyList<string> Extended = new[]
        {
            Danger, Danger2, Success, Warning, OnAccent, Overlay, Shadow,
        };

        /// <summary>Every colour token, web set first.</summary>
        public static readonly IReadOnlyList<string> All = Concat(Web, Extended);

        static string[] Concat(IReadOnlyList<string> a, IReadOnlyList<string> b)
        {
            var all = new string[a.Count + b.Count];
            for (int i = 0; i < a.Count; i++) all[i] = a[i];
            for (int i = 0; i < b.Count; i++) all[a.Count + i] = b[i];
            return all;
        }
    }
}
