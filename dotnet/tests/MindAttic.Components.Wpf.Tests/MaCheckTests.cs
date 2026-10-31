using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using MindAttic.Components.Wpf.Controls;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T121: the MaCheck check box style.</summary>
public sealed class MaCheckTests
{
    private static CheckBox Check(StaHost h, object? content = null, bool? isChecked = false) =>
        h.Show(new CheckBox { Style = (Style)h.Window.FindResource("MaCheck"), Content = content, IsChecked = isChecked });

    private static T Part<T>(CheckBox box, string name) where T : class => (T)box.Template.FindName(name, box);

    [Fact]
    public void Unchecked_is_an_outlined_surface_box_with_no_mark() => StaHost.Run(h =>
    {
        var box = Check(h, "Dark theme");
        var square = Part<Border>(box, "PART_Box");

        Assert.Same(h.Window.FindResource("MaSurface"), square.Background);
        Assert.Same(h.Window.FindResource("MaFieldBorder"), square.BorderBrush);
        Assert.Equal(Visibility.Collapsed, Part<TextBlock>(box, "PART_Glyph").Visibility);
        Assert.Equal(Visibility.Collapsed, Part<Rectangle>(box, "PART_Dash").Visibility);
        Assert.Same(h.Window.FindResource("MaText"), box.Foreground);
        Assert.Equal(18d, square.ActualWidth);
    });

    [Fact]
    public void Checked_fills_with_the_accent_and_shows_the_icon_font_check_mark() => StaHost.Run(h =>
    {
        var box = Check(h, "Dark theme", true);
        var square = Part<Border>(box, "PART_Box");
        var glyph = Part<TextBlock>(box, "PART_Glyph");

        Assert.Same(h.Window.FindResource("MaAccent"), square.Background);
        Assert.Equal(Visibility.Visible, glyph.Visibility);
        Assert.Equal("", glyph.Text);
        Assert.Equal("Segoe Fluent Icons, Segoe MDL2 Assets", glyph.FontFamily.Source);
        Assert.Same(h.Window.FindResource("MaOnAccent"), glyph.Foreground);
    });

    [Fact]
    public void Indeterminate_shows_a_bar_instead_of_the_mark() => StaHost.Run(h =>
    {
        var box = Check(h, "Some", null);
        box.IsThreeState = true;
        h.Pump();
        Assert.Equal(Visibility.Visible, Part<Rectangle>(box, "PART_Dash").Visibility);
        Assert.Equal(Visibility.Collapsed, Part<TextBlock>(box, "PART_Glyph").Visibility);
    });

    [Fact]
    public void Toggling_through_automation_checks_and_unchecks_it() => StaHost.Run(h =>
    {
        var box = Check(h, "Enabled");
        var peer = UIElementAutomationPeer.CreatePeerForElement(box);
        var toggle = (IToggleProvider)peer.GetPattern(PatternInterface.Toggle);

        toggle.Toggle();
        h.Pump();
        Assert.True(box.IsChecked);
        Assert.Equal(Visibility.Visible, Part<TextBlock>(box, "PART_Glyph").Visibility);
        Assert.Equal("Enabled", peer.GetName());

        toggle.Toggle();
        h.Pump();
        Assert.False(box.IsChecked);
    });

    [Fact]
    public void Keyboard_focus_outlines_the_box_in_MaFocus() => StaHost.Run(h =>
    {
        var box = Check(h, "Enabled");
        Assert.True(box.Focus());
        h.Pump();
        Assert.Same(h.Window.FindResource("MaFocus"), Part<Border>(box, "PART_Box").BorderBrush);
    });

    [Fact]
    public void Switching_theme_recolours_it_live() => StaHost.Run(h =>
    {
        var box = Check(h, "Enabled", true);
        var square = Part<Border>(box, "PART_Box");
        var darkSurface = ((SolidColorBrush)h.Window.FindResource("MaSurface")).Color;

        box.IsChecked = false;
        h.Pump();
        Assert.Equal(darkSurface, ((SolidColorBrush)square.Background).Color);

        MaTheme.Set(h.Window.Resources, MaThemeMode.Light);
        h.Pump();
        var lightSurface = ((SolidColorBrush)h.Window.FindResource("MaSurface")).Color;
        Assert.NotEqual(darkSurface, lightSurface);
        Assert.Equal(lightSurface, ((SolidColorBrush)square.Background).Color);
        Assert.Equal(((SolidColorBrush)h.Window.FindResource("MaText")).Color, ((SolidColorBrush)box.Foreground).Color);
    });

    [Fact]
    public void Disabled_is_dimmed() => StaHost.Run(h =>
    {
        var box = Check(h, "Enabled");
        box.IsEnabled = false;
        h.Pump();
        Assert.Equal(0.5, box.Opacity);
    });
}
