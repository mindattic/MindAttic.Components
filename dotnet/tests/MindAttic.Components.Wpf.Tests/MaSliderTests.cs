using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using MindAttic.Components.Wpf.Controls;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T228: the MaSlider slider style.</summary>
public sealed class MaSliderTests
{
    private static Slider Slider(StaHost h, Orientation orientation = Orientation.Horizontal)
    {
        var slider = new Slider { Style = (Style)h.Window.FindResource("MaSlider"), Orientation = orientation, Maximum = 100, Value = 40 };
        if (orientation == Orientation.Horizontal) slider.Width = 300;
        else slider.Height = 200;
        return h.Show(slider);
    }

    private static T Part<T>(Slider slider, string name) where T : class => (T)slider.Template.FindName(name, slider);

    private static Thumb Thumb(Slider slider) => Part<Track>(slider, "PART_Track").Thumb;

    private static Ellipse ThumbPart(Slider slider, string name) => (Ellipse)Thumb(slider).Template.FindName(name, Thumb(slider));

    [Fact]
    public void Horizontal_is_a_thin_surface_rail_filled_in_the_accent_up_to_the_thumb() => StaHost.Run(h =>
    {
        var slider = Slider(h);
        var rail = Part<Border>(slider, "PART_Rail");
        var track = Part<Track>(slider, "PART_Track");

        Assert.Equal(4d, rail.ActualHeight);
        Assert.Same(h.Window.FindResource("MaSurface"), rail.Background);
        Assert.Same(h.Window.FindResource("MaControlBorder"), rail.BorderBrush);
        Assert.Same(h.Window.FindResource("MaAccent"), track.DecreaseRepeatButton.Background);
        Assert.Same(h.Window.FindResource("MaText"), ThumbPart(slider, "Dot").Fill);
        Assert.Equal(14d, Thumb(slider).ActualWidth);
        Assert.True(track.DecreaseRepeatButton.ActualWidth < track.IncreaseRepeatButton.ActualWidth);
    });

    [Fact]
    public void Vertical_runs_the_rail_up_the_middle() => StaHost.Run(h =>
    {
        var slider = Slider(h, Orientation.Vertical);
        var rail = Part<Border>(slider, "PART_Rail");
        var track = Part<Track>(slider, "PART_Track");

        Assert.Equal(4d, rail.ActualWidth);
        Assert.Equal(Orientation.Vertical, track.Orientation);
        Assert.True(track.DecreaseRepeatButton.ActualHeight < track.IncreaseRepeatButton.ActualHeight);
    });

    [Fact]
    public void A_host_foreground_reaches_the_fill() => StaHost.Run(h =>
    {
        var slider = Slider(h);
        var brush = new SolidColorBrush(Colors.Orange);
        slider.Foreground = brush;
        h.Pump();
        Assert.Same(brush, Part<Track>(slider, "PART_Track").DecreaseRepeatButton.Background);
    });

    [Fact]
    public void Keyboard_focus_rings_the_thumb_in_MaFocus() => StaHost.Run(h =>
    {
        var slider = Slider(h);
        var ring = ThumbPart(slider, "Ring");
        Assert.Equal(Visibility.Collapsed, ring.Visibility);

        Assert.True(slider.Focus());
        h.Pump();
        Assert.Equal(Visibility.Visible, ring.Visibility);
        Assert.Same(h.Window.FindResource("MaFocus"), ring.Stroke);
    });

    [Fact]
    public void Switching_theme_recolours_it_live() => StaHost.Run(h =>
    {
        var slider = Slider(h);
        var rail = Part<Border>(slider, "PART_Rail");
        var darkSurface = ((SolidColorBrush)rail.Background).Color;

        MaTheme.Set(h.Window.Resources, MaThemeMode.Light);
        h.Pump();
        var lightSurface = ((SolidColorBrush)h.Window.FindResource("MaSurface")).Color;
        Assert.NotEqual(darkSurface, lightSurface);
        Assert.Equal(lightSurface, ((SolidColorBrush)rail.Background).Color);
    });

    [Fact]
    public void Disabled_is_dimmed() => StaHost.Run(h =>
    {
        var slider = Slider(h);
        slider.IsEnabled = false;
        h.Pump();
        Assert.Equal(0.5, slider.Opacity);
    });
}
