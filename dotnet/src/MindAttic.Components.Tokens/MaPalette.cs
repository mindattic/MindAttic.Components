using System.Collections.ObjectModel;

namespace MindAttic.Components.Tokens;

/// <summary>
/// A MindAttic colour palette: every Ma* colour token by name. <see cref="Dark"/> (the default) and
/// <see cref="Light"/> carry the same names at the same WCAG 2.2 AA standard (text 4.5:1, outlines,
/// focus and status marks 3:1, white on the accent 4.5:1). They are the one source for each
/// framework's theme: MindAttic.Components.Wpf's Theme.xaml and Themes/Light.xaml are tested against them.
/// </summary>
public sealed class MaPalette
{
    /// <summary>Every token name, in the order the palettes list them.</summary>
    public static IReadOnlyList<string> Names { get; } = Array.AsReadOnly(new[]
    {
        "MaChrome", "MaSurface", "MaHover", "MaHoverStrong", "MaChromeBorder",
        "MaText", "MaMuted", "MaRec", "MaStar",
        "MaAccent", "MaAccentHover", "MaOnAccent", "MaTileHover", "MaAccentBorder", "MaAccentDisabled", "MaOnAccentDisabled",
        "MaControlBorder", "MaFieldBorder", "MaFocus", "MaDisabledText",
        "MaGood", "MaWarn", "MaBad", "MaOther", "MaLink", "MaChartGrid", "MaBackdrop",
    });

    /// <summary>The dark palette, the default theme (Theme.xaml).</summary>
    public static MaPalette Dark { get; } = new("Dark", new Dictionary<string, string>
    {
        ["MaChrome"] = "#FF1E1F24",
        ["MaSurface"] = "#FF2A2C33",
        ["MaHover"] = "#FF2D2F37",
        ["MaHoverStrong"] = "#FF363842",
        ["MaChromeBorder"] = "#FF34363E",
        ["MaText"] = "#FFE6E6EA",
        ["MaMuted"] = "#FF9A9CA6",
        ["MaRec"] = "#FFFF7A7D",
        ["MaStar"] = "#FFF5C542",
        ["MaAccent"] = "#FF3B5BDB",
        ["MaAccentHover"] = "#FF2F4AB8",
        ["MaOnAccent"] = "#FFFFFFFF",
        ["MaTileHover"] = "#FF3B5BDB",
        ["MaAccentBorder"] = "#FF8FA3F0",
        ["MaAccentDisabled"] = "#FF3A4A5C",
        ["MaOnAccentDisabled"] = "#FF9AA7B4",
        ["MaControlBorder"] = "#FF6A6C75",
        ["MaFieldBorder"] = "#FF8A8C96",
        ["MaFocus"] = "#FF7AB8FF",
        ["MaDisabledText"] = "#FF7A7C85",
        ["MaGood"] = "#FF2FBF71",
        ["MaWarn"] = "#FFF5A524",
        ["MaBad"] = "#FFFF7A7D",
        ["MaOther"] = "#FFB48CFF",
        ["MaLink"] = "#FF7A93F0",
        ["MaChartGrid"] = "#FF6B6B6B",
        ["MaBackdrop"] = "#99000000",
    });

    /// <summary>The light palette (Themes/Light.xaml): a light grey chrome with white raised surfaces.</summary>
    public static MaPalette Light { get; } = new("Light", new Dictionary<string, string>
    {
        ["MaChrome"] = "#FFF3F4F7",
        ["MaSurface"] = "#FFFFFFFF",
        ["MaHover"] = "#FFE6E8EE",
        ["MaHoverStrong"] = "#FFDADDE5",
        ["MaChromeBorder"] = "#FFDCDFE6",
        ["MaText"] = "#FF1E1F24",
        ["MaMuted"] = "#FF595C66",
        ["MaRec"] = "#FFC42B2B",
        ["MaStar"] = "#FFA66A00",
        ["MaAccent"] = "#FF3B5BDB",
        ["MaAccentHover"] = "#FF2F4AB8",
        ["MaOnAccent"] = "#FFFFFFFF",
        ["MaTileHover"] = "#FF3B5BDB",
        ["MaAccentBorder"] = "#FF2F4AB8",
        ["MaAccentDisabled"] = "#FFC9D1EA",
        ["MaOnAccentDisabled"] = "#FF5C6680",
        ["MaControlBorder"] = "#FF868993",
        ["MaFieldBorder"] = "#FF6B6E78",
        ["MaFocus"] = "#FF1E5BD6",
        ["MaDisabledText"] = "#FF8A8D96",
        ["MaGood"] = "#FF1B7A43",
        ["MaWarn"] = "#FFA65200",
        ["MaBad"] = "#FFC42B2B",
        ["MaOther"] = "#FF7041D1",
        ["MaLink"] = "#FF2F4AB8",
        ["MaChartGrid"] = "#FF8A8D96",
        ["MaBackdrop"] = "#73000000",
    });

    /// <summary>Both palettes, dark first.</summary>
    public static IReadOnlyList<MaPalette> All { get; } = Array.AsReadOnly(new[] { Dark, Light });

    private MaPalette(string name, Dictionary<string, string> hex)
    {
        Name = name;
        var colors = new Dictionary<string, MaColor>(StringComparer.Ordinal);
        foreach (var key in Names)
        {
            colors[key] = MaColor.Parse(hex[key]);
        }

        if (hex.Count != Names.Count)
        {
            throw new InvalidOperationException($"The {name} palette has tokens outside MaPalette.Names.");
        }

        Colors = new ReadOnlyDictionary<string, MaColor>(colors);
    }

    /// <summary>"Dark" or "Light".</summary>
    public string Name { get; }

    /// <summary>Every token by its Ma* name.</summary>
    public IReadOnlyDictionary<string, MaColor> Colors { get; }

    /// <summary>The token called <paramref name="name"/>, e.g. <c>palette["MaAccent"]</c>.</summary>
    /// <exception cref="KeyNotFoundException">No token has that name.</exception>
    public MaColor this[string name] => Colors[name];

    /// <summary>The palette called <paramref name="name"/> ("Dark" or "Light", any case).</summary>
    /// <exception cref="ArgumentException">Neither palette has that name.</exception>
    public static MaPalette Get(string name) =>
        All.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException($"There is no '{name}' palette; use Dark or Light.", nameof(name));

    /// <summary>Window and page background.</summary>
    public MaColor Chrome => this["MaChrome"];
    /// <summary>Raised surfaces: cards, menus, fields.</summary>
    public MaColor Surface => this["MaSurface"];
    /// <summary>Hover fill.</summary>
    public MaColor Hover => this["MaHover"];
    /// <summary>Stronger hover / pressed fill.</summary>
    public MaColor HoverStrong => this["MaHoverStrong"];
    /// <summary>Decorative rule between chrome areas.</summary>
    public MaColor ChromeBorder => this["MaChromeBorder"];
    /// <summary>Body text.</summary>
    public MaColor Text => this["MaText"];
    /// <summary>Secondary text.</summary>
    public MaColor Muted => this["MaMuted"];
    /// <summary>Red as text or a mark (recording, errors).</summary>
    public MaColor Rec => this["MaRec"];
    /// <summary>A favourite star.</summary>
    public MaColor Star => this["MaStar"];
    /// <summary>The accent blue.</summary>
    public MaColor Accent => this["MaAccent"];
    /// <summary>The accent under the pointer.</summary>
    public MaColor AccentHover => this["MaAccentHover"];
    /// <summary>Text and glyphs on the accent.</summary>
    public MaColor OnAccent => this["MaOnAccent"];
    /// <summary>A tile under the pointer.</summary>
    public MaColor TileHover => this["MaTileHover"];
    /// <summary>The outline of an accent button.</summary>
    public MaColor AccentBorder => this["MaAccentBorder"];
    /// <summary>A disabled accent button's fill.</summary>
    public MaColor AccentDisabled => this["MaAccentDisabled"];
    /// <summary>A disabled accent button's text.</summary>
    public MaColor OnAccentDisabled => this["MaOnAccentDisabled"];
    /// <summary>A control's outline (3:1).</summary>
    public MaColor ControlBorder => this["MaControlBorder"];
    /// <summary>A text field's outline (3:1 on every surface).</summary>
    public MaColor FieldBorder => this["MaFieldBorder"];
    /// <summary>The keyboard focus ring.</summary>
    public MaColor Focus => this["MaFocus"];
    /// <summary>Disabled text.</summary>
    public MaColor DisabledText => this["MaDisabledText"];
    /// <summary>Good status.</summary>
    public MaColor Good => this["MaGood"];
    /// <summary>Warning status.</summary>
    public MaColor Warn => this["MaWarn"];
    /// <summary>Bad status.</summary>
    public MaColor Bad => this["MaBad"];
    /// <summary>Other / neutral status.</summary>
    public MaColor Other => this["MaOther"];
    /// <summary>Link text.</summary>
    public MaColor Link => this["MaLink"];
    /// <summary>Chart gridlines.</summary>
    public MaColor ChartGrid => this["MaChartGrid"];
    /// <summary>A modal's dimmed backdrop (translucent).</summary>
    public MaColor Backdrop => this["MaBackdrop"];
}
