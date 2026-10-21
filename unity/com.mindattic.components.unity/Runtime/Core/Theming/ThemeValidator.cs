using System.Collections.Generic;

namespace MindAttic.Components.Unity.Core
{
    /// <summary>
    /// Checks a palette's text and focus colours against WCAG 2.2 AA: body and label text 4.5:1 on every surface
    /// it sits on, muted text and the focus ring 3:1 (1.4.11 non-text contrast). An empty result means it passes.
    /// </summary>
    public static class ThemeValidator
    {
        /// <summary>The minimum ratio for normal text.</summary>
        public const double Text = 4.5;
        /// <summary>The minimum ratio for muted text and UI component boundaries.</summary>
        public const double NonText = 3.0;

        /// <summary>One human-readable line per failed pair.</summary>
        public static IReadOnlyList<string> Validate(ThemePalette p)
        {
            var issues = new List<string>();
            Check(issues, p, ThemeTokenNames.Text, ThemeTokenNames.Bg, Text);
            Check(issues, p, ThemeTokenNames.Text, ThemeTokenNames.Bg2, Text);
            Check(issues, p, ThemeTokenNames.Text, ThemeTokenNames.Bg3, Text);
            Check(issues, p, ThemeTokenNames.Text2, ThemeTokenNames.Bg, Text);
            Check(issues, p, ThemeTokenNames.Text2, ThemeTokenNames.Bg2, Text);
            Check(issues, p, ThemeTokenNames.Text3, ThemeTokenNames.Bg, NonText);
            Check(issues, p, ThemeTokenNames.OnAccent, ThemeTokenNames.Accent, Text);
            Check(issues, p, ThemeTokenNames.OnAccent, ThemeTokenNames.Accent2, Text);
            Check(issues, p, ThemeTokenNames.Accent, ThemeTokenNames.Bg, NonText);
            Check(issues, p, ThemeTokenNames.Accent, ThemeTokenNames.Bg2, NonText);
            Check(issues, p, ThemeTokenNames.TagText, ThemeTokenNames.TagBg, Text);
            Check(issues, p, ThemeTokenNames.Danger, ThemeTokenNames.Bg2, NonText);
            // Status pills: the dot (and outline) of every kind on the pill's bg2 fill (1.4.11 non-text contrast).
            Check(issues, p, ThemeTokenNames.Success, ThemeTokenNames.Bg2, NonText);
            Check(issues, p, ThemeTokenNames.Warning, ThemeTokenNames.Bg2, NonText);
            Check(issues, p, ThemeTokenNames.Text2, ThemeTokenNames.Bg2, NonText);
            CheckButtons(issues, p);
            return issues;
        }

        /// <summary>
        /// Every button variant's label on its own fill, as drawn on a <c>bg</c> card and a <c>bg2</c> group: at rest
        /// and selected (including selected and disabled: the chosen answer after a vote) it needs 4.5:1; a plain
        /// disabled label needs 3:1 against its faded fill.
        /// </summary>
        static void CheckButtons(List<string> issues, ThemePalette p)
        {
            foreach (ButtonVariant v in new[] { ButtonVariant.Primary, ButtonVariant.Secondary, ButtonVariant.Danger, ButtonVariant.Ghost })
            {
                foreach (var surface in new[] { (ThemeTokenNames.Bg, p.Bg), (ThemeTokenNames.Bg2, p.Bg2) })
                {
                    CheckColors(issues, p, v + " button", ControlStyles.Button(p, v, ControlState.Normal), surface, Text);
                    CheckColors(issues, p, "selected " + v + " button", ControlStyles.Button(p, v, ControlState.Normal, true), surface, Text);
                    CheckColors(issues, p, "selected disabled " + v + " button", ControlStyles.Button(p, v, ControlState.Disabled, true), surface, Text);
                    CheckColors(issues, p, "disabled " + v + " button", ControlStyles.Button(p, v, ControlState.Disabled), surface, ControlStyles.DisabledTextContrast);
                }
            }
        }

        static void CheckColors(List<string> issues, ThemePalette p, string what, ControlColors c, (string Name, ThemeColor Color) surface, double min)
        {
            var under = c.Fill.Over(surface.Color.WithAlpha(1));
            double ratio = Contrast.Ratio(c.Text, under);
            if (ratio + 1e-9 < min)
            {
                issues.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "{0}: {1} label on --{2} is {3:0.00}:1, needs {4:0.0}:1", p.Name, what, surface.Name, ratio, min));
            }
        }

        static void Check(List<string> issues, ThemePalette p, string fg, string bg, double min)
        {
            double ratio = Contrast.Ratio(p[fg], p[bg]);
            if (ratio + 1e-9 < min)
            {
                issues.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "{0}: --{1} on --{2} is {3:0.00}:1, needs {4:0.0}:1", p.Name, fg, bg, ratio, min));
            }
        }
    }
}
