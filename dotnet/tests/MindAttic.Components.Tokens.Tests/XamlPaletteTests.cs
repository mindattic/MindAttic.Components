using System.Xml.Linq;
using MindAttic.Components.Tokens;
using Xunit;

namespace MindAttic.Components.Tokens.Tests;

/// <summary>
/// Holds MindAttic.Components.Wpf's two palettes to the tokens, so the tokens stay the one source:
/// change a colour in one place without the other and this fails.
/// </summary>
public class XamlPaletteTests
{
    private static readonly XNamespace X = "http://schemas.microsoft.com/winfx/2006/xaml";
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    /// <summary>The top-level SolidColorBrush entries keyed Ma* in a theme dictionary, by key.</summary>
    private static Dictionary<string, string> XamlBrushes(string file)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Xaml", file);
        var root = XDocument.Load(path).Root!;
        return root.Elements(Presentation + "SolidColorBrush")
            .Select(e => (Key: (string?)e.Attribute(X + "Key"), Color: (string?)e.Attribute("Color")))
            .Where(b => b.Key is not null && b.Key.StartsWith("Ma", StringComparison.Ordinal))
            .ToDictionary(b => b.Key!, b => b.Color ?? throw new InvalidOperationException($"{file}: {b.Key} has no Color."));
    }

    public static TheoryData<string, string> Themes => new()
    {
        { "Dark", "Theme.xaml" },
        { "Light", "Light.xaml" },
    };

    [Theory]
    [MemberData(nameof(Themes))]
    public void Every_xaml_brush_is_a_token_of_the_same_colour(string palette, string file)
    {
        var tokens = MaPalette.Get(palette);
        foreach (var (key, color) in XamlBrushes(file))
        {
            Assert.True(tokens.Colors.ContainsKey(key), $"{file} has {key}, which isn't a token.");
            Assert.Equal(tokens[key], MaColor.Parse(color));
        }
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void Every_token_is_a_xaml_brush(string palette, string file)
    {
        var brushes = XamlBrushes(file);
        var missing = MaPalette.Get(palette).Colors.Keys.Where(k => !brushes.ContainsKey(k)).ToList();
        Assert.True(missing.Count == 0, $"{file} is missing {string.Join(", ", missing)}.");
    }

    [Fact]
    public void The_package_is_version_1()
    {
        var csproj = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Xaml", "MindAttic.Components.Tokens.csproj"));
        Assert.Equal("1.0.0", csproj.Descendants("Version").Single().Value);
    }
}
