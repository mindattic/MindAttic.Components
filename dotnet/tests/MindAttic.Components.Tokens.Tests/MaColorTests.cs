using MindAttic.Components.Tokens;
using Xunit;

namespace MindAttic.Components.Tokens.Tests;

public class MaColorTests
{
    [Theory]
    [InlineData("#FF3B5BDB", 255, 0x3B, 0x5B, 0xDB)]
    [InlineData("#99000000", 0x99, 0, 0, 0)]
    [InlineData("#3b5bdb", 255, 0x3B, 0x5B, 0xDB)]
    [InlineData("3B5BDB", 255, 0x3B, 0x5B, 0xDB)]
    public void Parse_reads_xaml_hex(string hex, int a, int r, int g, int b) =>
        Assert.Equal(new MaColor((byte)a, (byte)r, (byte)g, (byte)b), MaColor.Parse(hex));

    [Theory]
    [InlineData("")]
    [InlineData("#FFF")]
    [InlineData("#GG3B5BDB")]
    [InlineData("#FF3B5BDB00")]
    public void Parse_refuses_anything_else(string hex) =>
        Assert.Throws<FormatException>(() => MaColor.Parse(hex));

    [Fact]
    public void Hex_round_trips_and_css_puts_alpha_last()
    {
        Assert.Equal("#FF3B5BDB", MaColor.Parse("#ff3b5bdb").ToHex());
        Assert.Equal("#3B5BDB", MaColor.Parse("#FF3B5BDB").ToCssHex());
        Assert.Equal("#00000099", MaColor.Parse("#99000000").ToCssHex());
    }

    [Fact]
    public void Contrast_ratio_matches_wcag()
    {
        var black = MaColor.FromRgb(0, 0, 0);
        var white = MaColor.FromRgb(255, 255, 255);
        Assert.Equal(21.0, MaColor.ContrastRatio(black, white), 3);
        Assert.Equal(21.0, MaColor.ContrastRatio(white, black), 3);
        Assert.Equal(1.0, MaColor.ContrastRatio(white, white), 3);
        // White on the accent blue: 5.7:1, as Themes/Light.xaml notes.
        Assert.Equal(5.7, MaColor.ContrastRatio(white, MaColor.Parse("#FF3B5BDB")), 1);
    }
}
