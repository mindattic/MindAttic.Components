using MindAttic.Components.Tokens;
using Xunit;

namespace MindAttic.Components.Tokens.Tests;

public class MaPaletteTests
{
    public static TheoryData<string> Palettes => new() { "Dark", "Light" };

    [Fact]
    public void Both_palettes_have_exactly_the_same_names()
    {
        Assert.Equal(MaPalette.Names.OrderBy(n => n), MaPalette.Dark.Colors.Keys.OrderBy(n => n));
        Assert.Equal(MaPalette.Names.OrderBy(n => n), MaPalette.Light.Colors.Keys.OrderBy(n => n));
        Assert.Equal(MaPalette.Names.Count, MaPalette.Names.Distinct().Count());
    }

    [Theory]
    [InlineData("dark", "Dark")]
    [InlineData("LIGHT", "Light")]
    public void Get_finds_a_palette_by_name_in_any_case(string name, string expected) =>
        Assert.Equal(expected, MaPalette.Get(name).Name);

    [Fact]
    public void Get_refuses_an_unknown_palette() =>
        Assert.Throws<ArgumentException>(() => MaPalette.Get("Sepia"));

    [Fact]
    public void Named_properties_read_their_tokens()
    {
        Assert.Equal(MaColor.Parse("#FF1E1F24"), MaPalette.Dark.Chrome);
        Assert.Equal(MaColor.Parse("#FF1E1F24"), MaPalette.Light.Text);
        Assert.Equal(MaPalette.Light["MaBackdrop"], MaPalette.Light.Backdrop);
    }

    [Theory]
    [MemberData(nameof(Palettes))]
    public void Every_colour_but_the_backdrop_is_opaque(string name)
    {
        foreach (var (key, color) in MaPalette.Get(name).Colors)
        {
            if (key != "MaBackdrop")
            {
                Assert.True(color.A == 255, $"{name} {key} is translucent.");
            }
        }
    }

    // The WCAG 2.2 AA standard both palettes promise, on both backgrounds.
    [Theory]
    [MemberData(nameof(Palettes))]
    public void The_palette_meets_wcag_aa(string name)
    {
        var p = MaPalette.Get(name);
        foreach (var ground in new[] { p.Chrome, p.Surface })
        {
            AtLeast(4.5, p.Text, ground, name, "MaText");
            AtLeast(4.5, p.Muted, ground, name, "MaMuted");
            AtLeast(4.5, p.Rec, ground, name, "MaRec");
            AtLeast(4.5, p.Link, ground, name, "MaLink");
            AtLeast(3.0, p.FieldBorder, ground, name, "MaFieldBorder");
            AtLeast(3.0, p.Focus, ground, name, "MaFocus");
            AtLeast(3.0, p.Good, ground, name, "MaGood");
            AtLeast(3.0, p.Warn, ground, name, "MaWarn");
            AtLeast(3.0, p.Bad, ground, name, "MaBad");
            AtLeast(3.0, p.Other, ground, name, "MaOther");
        }

        AtLeast(3.0, p.ControlBorder, p.Chrome, name, "MaControlBorder");
        AtLeast(4.5, p.OnAccent, p.Accent, name, "MaOnAccent on MaAccent");
        AtLeast(4.5, p.OnAccent, p.AccentHover, name, "MaOnAccent on MaAccentHover");
    }

    private static void AtLeast(double needed, MaColor fore, MaColor back, string palette, string what)
    {
        var ratio = MaColor.ContrastRatio(fore, back);
        Assert.True(ratio >= needed, $"{palette} {what}: {ratio:F2}:1 on {back}, needs {needed}:1.");
    }
}
