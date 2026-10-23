using System.Globalization;

namespace MindAttic.Components.Tokens;

/// <summary>
/// One colour token as 8-bit ARGB channels, with no UI framework attached. A framework package
/// turns it into its own colour type (WPF <c>Color.FromArgb(A, R, G, B)</c>, CSS <see cref="ToCssHex"/>).
/// </summary>
/// <param name="A">Alpha, 0 (transparent) to 255 (opaque).</param>
/// <param name="R">Red, 0-255.</param>
/// <param name="G">Green, 0-255.</param>
/// <param name="B">Blue, 0-255.</param>
public readonly record struct MaColor(byte A, byte R, byte G, byte B)
{
    /// <summary>An opaque colour from its red, green and blue channels.</summary>
    public static MaColor FromRgb(byte r, byte g, byte b) => new(255, r, g, b);

    /// <summary>
    /// Parses <c>#RRGGBB</c> (opaque) or <c>#AARRGGBB</c>, the forms XAML uses; the leading # is optional.
    /// </summary>
    /// <exception cref="FormatException">The text isn't 6 or 8 hex digits.</exception>
    public static MaColor Parse(string hex)
    {
        ArgumentNullException.ThrowIfNull(hex);
        var digits = hex.StartsWith('#') ? hex[1..] : hex;
        if ((digits.Length != 6 && digits.Length != 8)
            || !uint.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value))
        {
            throw new FormatException($"'{hex}' is not a #RRGGBB or #AARRGGBB colour.");
        }

        if (digits.Length == 6)
        {
            value |= 0xFF000000;
        }

        return new MaColor((byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value);
    }

    /// <summary>The colour as <c>#AARRGGBB</c>, upper case, the form Theme.xaml writes.</summary>
    public string ToHex() => $"#{A:X2}{R:X2}{G:X2}{B:X2}";

    /// <summary>
    /// The colour as CSS hex: <c>#RRGGBB</c> when opaque, otherwise <c>#RRGGBBAA</c> (CSS puts alpha last).
    /// </summary>
    public string ToCssHex() => A == 255 ? $"#{R:X2}{G:X2}{B:X2}" : $"#{R:X2}{G:X2}{B:X2}{A:X2}";

    /// <inheritdoc />
    public override string ToString() => ToHex();

    /// <summary>WCAG 2.2 relative luminance of the colour's RGB (alpha ignored), 0 (black) to 1 (white).</summary>
    public double RelativeLuminance =>
        (0.2126 * Linear(R)) + (0.7152 * Linear(G)) + (0.0722 * Linear(B));

    /// <summary>
    /// WCAG 2.2 contrast ratio between two opaque colours, 1 to 21. Text needs 4.5:1 and outlines,
    /// focus rings and status marks 3:1 for AA.
    /// </summary>
    public static double ContrastRatio(MaColor a, MaColor b)
    {
        var la = a.RelativeLuminance;
        var lb = b.RelativeLuminance;
        var (hi, lo) = la >= lb ? (la, lb) : (lb, la);
        return (hi + 0.05) / (lo + 0.05);
    }

    private static double Linear(byte channel)
    {
        var c = channel / 255.0;
        return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }
}
