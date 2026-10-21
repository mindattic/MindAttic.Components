using System;
using System.Globalization;

namespace MindAttic.Components.Unity.Core
{
    /// <summary>
    /// An engine-free RGBA colour (each channel 0..1), parsed from the CSS forms the web themes use:
    /// <c>#rgb</c>, <c>#rgba</c>, <c>#rrggbb</c>, <c>#rrggbbaa</c>, <c>rgb(r,g,b)</c>, <c>rgba(r,g,b,a)</c>
    /// and the names <c>white</c>, <c>black</c> and <c>transparent</c>.
    /// </summary>
    public readonly struct ThemeColor : IEquatable<ThemeColor>
    {
        /// <summary>Red, 0..1.</summary>
        public readonly float R;
        /// <summary>Green, 0..1.</summary>
        public readonly float G;
        /// <summary>Blue, 0..1.</summary>
        public readonly float B;
        /// <summary>Alpha, 0..1.</summary>
        public readonly float A;

        /// <summary>Creates a colour from 0..1 channels (clamped).</summary>
        public ThemeColor(float r, float g, float b, float a = 1f)
        {
            R = Clamp01(r);
            G = Clamp01(g);
            B = Clamp01(b);
            A = Clamp01(a);
        }

        /// <summary>Opaque black.</summary>
        public static ThemeColor Black => new ThemeColor(0, 0, 0);
        /// <summary>Opaque white.</summary>
        public static ThemeColor White => new ThemeColor(1, 1, 1);
        /// <summary>Fully transparent black.</summary>
        public static ThemeColor Transparent => new ThemeColor(0, 0, 0, 0);

        /// <summary>Creates a colour from 0..255 channels.</summary>
        public static ThemeColor FromBytes(int r, int g, int b, int a = 255) =>
            new ThemeColor(r / 255f, g / 255f, b / 255f, a / 255f);

        /// <summary>The same colour with a different alpha.</summary>
        public ThemeColor WithAlpha(float a) => new ThemeColor(R, G, B, a);

        /// <summary>Linear interpolation towards <paramref name="to"/> (t clamped to 0..1), alpha included.</summary>
        public ThemeColor Lerp(ThemeColor to, float t)
        {
            t = Clamp01(t);
            return new ThemeColor(R + (to.R - R) * t, G + (to.G - G) * t, B + (to.B - B) * t, A + (to.A - A) * t);
        }

        /// <summary>This colour drawn over an opaque <paramref name="backdrop"/>: the opaque colour a viewer sees.</summary>
        public ThemeColor Over(ThemeColor backdrop) =>
            new ThemeColor(R * A + backdrop.R * (1 - A), G * A + backdrop.G * (1 - A), B * A + backdrop.B * (1 - A), 1f);

        /// <summary><c>#rrggbb</c>, or <c>#rrggbbaa</c> when not opaque (lower-case).</summary>
        public string ToHex()
        {
            string rgb = "#" + Byte(R).ToString("x2") + Byte(G).ToString("x2") + Byte(B).ToString("x2");
            return Byte(A) == 255 ? rgb : rgb + Byte(A).ToString("x2");
        }

        /// <inheritdoc />
        public override string ToString() => ToHex();

        /// <summary>Parses a CSS colour; throws <see cref="FormatException"/> when it isn't one.</summary>
        public static ThemeColor Parse(string css)
        {
            if (TryParse(css, out ThemeColor c)) return c;
            throw new FormatException("Not a CSS colour: '" + css + "'");
        }

        /// <summary>Parses a CSS colour without throwing.</summary>
        public static bool TryParse(string css, out ThemeColor color)
        {
            color = default;
            if (css == null) return false;
            string s = css.Trim().ToLowerInvariant();
            if (s.Length == 0) return false;

            switch (s)
            {
                case "white": color = White; return true;
                case "black": color = Black; return true;
                case "transparent": color = Transparent; return true;
            }

            if (s[0] == '#') return TryParseHex(s.Substring(1), out color);

            if (s.StartsWith("rgb(") || s.StartsWith("rgba("))
            {
                int open = s.IndexOf('(');
                int close = s.LastIndexOf(')');
                if (close <= open) return false;
                string[] parts = s.Substring(open + 1, close - open - 1)
                    .Replace('/', ',')
                    .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 3 && parts.Length != 4) return false;
                var ch = new float[4];
                for (int i = 0; i < 3; i++)
                {
                    if (!TryChannel(parts[i], 255f, out ch[i])) return false;
                }
                ch[3] = 1f;
                if (parts.Length == 4 && !TryChannel(parts[3], 1f, out ch[3])) return false;
                color = new ThemeColor(ch[0], ch[1], ch[2], ch[3]);
                return true;
            }

            return false;
        }

        static bool TryChannel(string part, float scale, out float value)
        {
            value = 0;
            bool percent = part.EndsWith("%");
            string num = percent ? part.Substring(0, part.Length - 1) : part;
            if (!float.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out float v)) return false;
            value = percent ? v / 100f : v / scale;
            return true;
        }

        static bool TryParseHex(string hex, out ThemeColor color)
        {
            color = default;
            foreach (char ch in hex)
            {
                if (!Uri.IsHexDigit(ch)) return false;
            }
            switch (hex.Length)
            {
                case 3:
                case 4:
                {
                    int r = Nibble(hex[0]), g = Nibble(hex[1]), b = Nibble(hex[2]);
                    int a = hex.Length == 4 ? Nibble(hex[3]) : 15;
                    color = FromBytes(r * 17, g * 17, b * 17, a * 17);
                    return true;
                }
                case 6:
                case 8:
                {
                    int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                    int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                    int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                    int a = hex.Length == 8 ? Convert.ToInt32(hex.Substring(6, 2), 16) : 255;
                    color = FromBytes(r, g, b, a);
                    return true;
                }
                default:
                    return false;
            }
        }

        static int Nibble(char c) => Convert.ToInt32(c.ToString(), 16);
        static int Byte(float v) => (int)Math.Round(Clamp01(v) * 255f, MidpointRounding.AwayFromZero);
        static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;

        /// <summary>Equal when every channel rounds to the same 0..255 byte.</summary>
        public bool Equals(ThemeColor other) =>
            Byte(R) == Byte(other.R) && Byte(G) == Byte(other.G) && Byte(B) == Byte(other.B) && Byte(A) == Byte(other.A);

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is ThemeColor other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => (Byte(R) << 24) ^ (Byte(G) << 16) ^ (Byte(B) << 8) ^ Byte(A);

        /// <summary>Byte-wise equality.</summary>
        public static bool operator ==(ThemeColor a, ThemeColor b) => a.Equals(b);

        /// <summary>Byte-wise inequality.</summary>
        public static bool operator !=(ThemeColor a, ThemeColor b) => !a.Equals(b);
    }
}
