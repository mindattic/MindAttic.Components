using System;

namespace MindAttic.Components.Unity.Core
{
    /// <summary>WCAG 2.2 relative luminance and contrast ratio.</summary>
    public static class Contrast
    {
        /// <summary>Relative luminance (0 black .. 1 white) of a colour's RGB, alpha ignored.</summary>
        public static double Luminance(ThemeColor c) =>
            0.2126 * Linear(c.R) + 0.7152 * Linear(c.G) + 0.0722 * Linear(c.B);

        /// <summary>
        /// The contrast ratio (1..21) of <paramref name="foreground"/> over <paramref name="background"/>. A
        /// translucent foreground is composited over the background first; the background is taken as opaque.
        /// </summary>
        public static double Ratio(ThemeColor foreground, ThemeColor background)
        {
            var bg = background.WithAlpha(1);
            var fg = foreground.A < 1 ? foreground.Over(bg) : foreground;
            double l1 = Luminance(fg), l2 = Luminance(bg);
            if (l1 < l2) { double t = l1; l1 = l2; l2 = t; }
            return (l1 + 0.05) / (l2 + 0.05);
        }

        static double Linear(float channel) =>
            channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }
}
