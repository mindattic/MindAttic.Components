using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using MindAttic.Components.Wpf.Controls;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>MaCalloutToolTip (M.COMP-T167): the pure placement rules, the DIP/device-pixel step, the chrome's layout
/// and outline, the template and the MaCallout attached properties.</summary>
public sealed class MaCalloutToolTipTests
{
    // A 1920x1080 screen with a 40px taskbar; a 300x60 bubble; the control's defaults.
    private static readonly Rect Work = new(0, 0, 1920, 1040);
    private static readonly Size Bubble = new(300, 60);
    private const double Gap = 2, Arrow = 7, Radius = 4;
    private const double Reach = Gap + Arrow;
    private const double MinArrow = Radius + Arrow, MaxArrow = 60 - Radius - Arrow;

    private static CalloutPlacementResult Place(Rect target, MaCalloutSide preferred = MaCalloutSide.Right, Rect? work = null) =>
        CalloutPlacement.Compute(target, Bubble, work ?? Work, preferred, Gap, Arrow, Radius);

    [Fact]
    public void Preferred_side_that_fits_is_kept_with_the_arrow_tip_a_gap_from_the_targets_edge()
    {
        var p = Place(new Rect(0, 100, 200, 40));
        Assert.Equal(MaCalloutSide.Right, p.Side);
        Assert.Equal(200 + Reach, p.BubbleOrigin.X);
        Assert.Equal(120 - 30, p.BubbleOrigin.Y); // centred on the target
        Assert.Equal(30, p.ArrowOffset);          // pointing at the target's centre
    }

    [Fact]
    public void Flips_to_the_left_at_the_right_screen_edge()
    {
        var p = Place(new Rect(1800, 100, 100, 40));
        Assert.Equal(MaCalloutSide.Left, p.Side);
        Assert.Equal(1800 - Reach - 300, p.BubbleOrigin.X);
    }

    [Fact]
    public void Flips_to_the_right_at_the_left_screen_edge()
    {
        var p = Place(new Rect(10, 100, 100, 40), MaCalloutSide.Left);
        Assert.Equal(MaCalloutSide.Right, p.Side);
        Assert.Equal(110 + Reach, p.BubbleOrigin.X);
    }

    [Fact]
    public void Flips_between_top_and_bottom_on_the_vertical_axis()
    {
        var below = Place(new Rect(500, 100, 100, 30), MaCalloutSide.Bottom);
        Assert.Equal(MaCalloutSide.Bottom, below.Side);
        Assert.Equal(130 + Reach, below.BubbleOrigin.Y);
        Assert.Equal(550 - 150, below.BubbleOrigin.X);
        Assert.Equal(150, below.ArrowOffset);

        var flipped = Place(new Rect(500, 1000, 100, 30), MaCalloutSide.Bottom);
        Assert.Equal(MaCalloutSide.Top, flipped.Side);
        Assert.Equal(1000 - Reach - 60, flipped.BubbleOrigin.Y);

        var up = Place(new Rect(500, 20, 100, 30), MaCalloutSide.Top);
        Assert.Equal(MaCalloutSide.Bottom, up.Side);
    }

    [Fact]
    public void Shifts_up_to_stay_above_the_bottom_of_the_work_area()
    {
        var p = Place(new Rect(0, 1000, 200, 30));
        Assert.Equal(MaCalloutSide.Right, p.Side);
        Assert.Equal(1040 - 60, p.BubbleOrigin.Y);
        Assert.Equal(1015 - 980, p.ArrowOffset); // still at the target's centre
    }

    [Fact]
    public void Shifts_down_to_stay_below_the_top_of_the_work_area()
    {
        var p = Place(new Rect(0, 0, 200, 30), work: new Rect(0, 0, 1920, 1040));
        Assert.Equal(0, p.BubbleOrigin.Y);
        Assert.Equal(15, p.ArrowOffset);
    }

    [Fact]
    public void Shifts_sideways_to_stay_inside_the_right_edge_when_above_or_below()
    {
        var p = Place(new Rect(1880, 500, 30, 30), MaCalloutSide.Bottom);
        Assert.Equal(1920 - 300, p.BubbleOrigin.X);
        Assert.Equal(1895 - 1620, p.ArrowOffset);
    }

    [Fact]
    public void Arrow_is_clamped_clear_of_the_corners_at_both_ends()
    {
        var top = Place(new Rect(0, 0, 200, 4));
        Assert.Equal(0, top.BubbleOrigin.Y);
        Assert.Equal(MinArrow, top.ArrowOffset);

        var bottom = Place(new Rect(0, 1036, 200, 4));
        Assert.Equal(980, bottom.BubbleOrigin.Y);
        Assert.Equal(MaxArrow, bottom.ArrowOffset);
    }

    [Fact]
    public void Arrow_is_centred_when_the_edge_is_too_short_for_its_base()
    {
        Assert.Equal(10, CalloutPlacement.ClampArrow(3, 20, Arrow, Radius));
        Assert.Equal(10, CalloutPlacement.ClampArrow(double.NaN, 20, Arrow, Radius));
        Assert.Equal(MinArrow, CalloutPlacement.ClampArrow(0, 60, Arrow, Radius));
    }

    [Fact]
    public void Neither_side_fits_takes_the_roomier_one_and_stays_inside_the_work_area()
    {
        var narrow = new Rect(0, 0, 400, 1040);
        var roomierRight = Place(new Rect(100, 100, 100, 40), MaCalloutSide.Left, narrow); // 200 right vs 100 left
        Assert.Equal(MaCalloutSide.Right, roomierRight.Side);
        Assert.Equal(400 - 300, roomierRight.BubbleOrigin.X);

        var roomierLeft = Place(new Rect(200, 100, 100, 40), MaCalloutSide.Right, narrow); // 100 right vs 200 left
        Assert.Equal(MaCalloutSide.Left, roomierLeft.Side);
        Assert.Equal(0, roomierLeft.BubbleOrigin.X);

        var tie = Place(new Rect(150, 100, 100, 40), MaCalloutSide.Right, narrow);
        Assert.Equal(MaCalloutSide.Right, tie.Side); // a tie keeps the preferred side
    }

    [Fact]
    public void Target_partly_off_screen_flips_and_clamps_the_arrow()
    {
        var offRight = Place(new Rect(1880, 100, 100, 40)); // runs 60px past the right edge
        Assert.Equal(MaCalloutSide.Left, offRight.Side);
        Assert.Equal(1880 - Reach - 300, offRight.BubbleOrigin.X);

        var offBottom = Place(new Rect(0, 1020, 200, 60)); // centre (1050) below the work area
        Assert.Equal(980, offBottom.BubbleOrigin.Y);
        Assert.Equal(MaxArrow, offBottom.ArrowOffset);
    }

    [Fact]
    public void Monitor_with_a_non_zero_origin_is_respected()
    {
        var second = new Rect(1920, -200, 1280, 984); // a monitor to the right, higher up
        var p = Place(new Rect(3100, -195, 80, 20), work: second);
        Assert.Equal(MaCalloutSide.Left, p.Side);
        Assert.Equal(-200, p.BubbleOrigin.Y);
        Assert.Equal(-185 - -200, p.ArrowOffset);
    }

    [Fact]
    public void Screen_placement_converts_between_device_pixels_and_dips_at_150_percent()
    {
        // A 300x40 DIP target at 150% (450x60 device pixels); a 300x60 DIP bubble measured with its arrow on the left.
        const double s = 1.5;
        var target = new Rect(300, 150, 450, 60);
        var popup = new Size((300 + Arrow) * s, 60 * s);
        var (side, origin, arrowDip) = MaCalloutToolTip.PlaceOnScreen(target, popup, MaCalloutSide.Right,
            new Rect(0, 0, 2880, 1560), MaCalloutSide.Right, Gap, Arrow, Radius, s, s);
        Assert.Equal(MaCalloutSide.Right, side);
        Assert.Equal(750 + Gap * s, origin.X, 6);   // the popup (arrow strip included) starts a gap past the target
        Assert.Equal(180 - 45, origin.Y, 6);        // bubble centred on the target in device pixels
        Assert.Equal(30, arrowDip, 6);              // the template gets DIPs

        // Measured while it was still laid out for Top (arrow strip at the bottom): the same bubble, the same answer.
        var (side2, origin2, arrow2) = MaCalloutToolTip.PlaceOnScreen(target, new Size(300 * s, (60 + Arrow) * s), MaCalloutSide.Top,
            new Rect(0, 0, 2880, 1560), MaCalloutSide.Right, Gap, Arrow, Radius, s, s);
        Assert.Equal((side, origin.X, origin.Y, arrowDip), (side2, origin2.X, origin2.Y, arrow2));
    }

    [Fact]
    public void Screen_placement_puts_the_popup_origin_at_the_arrow_strip_for_left_and_top()
    {
        var target = new Rect(1800, 500, 100, 40);
        var popup = new Size(300 + Arrow, 60);
        var (side, origin, _) = MaCalloutToolTip.PlaceOnScreen(target, popup, MaCalloutSide.Right, Work, MaCalloutSide.Right, Gap, Arrow, Radius, 1, 1);
        Assert.Equal(MaCalloutSide.Left, side);
        Assert.Equal(1800 - Reach - 300, origin.X); // the bubble's left; the arrow strip is on its right

        var below = MaCalloutToolTip.PlaceOnScreen(new Rect(500, 100, 100, 30), new Size(300, 60 + Arrow), MaCalloutSide.Bottom,
            Work, MaCalloutSide.Bottom, Gap, Arrow, Radius, 1, 1);
        Assert.Equal(130 + Gap, below.PopupOrigin.Y); // the arrow strip is on top, so the popup starts at the arrow tip
    }

    [Theory]
    [InlineData(MaCalloutSide.Right, 7, 0)]
    [InlineData(MaCalloutSide.Left, 7, 0)]
    [InlineData(MaCalloutSide.Top, 0, 7)]
    [InlineData(MaCalloutSide.Bottom, 0, 7)]
    public void Chrome_reserves_the_arrow_strip_on_the_facing_edge_only(MaCalloutSide side, double extraW, double extraH) => StaHost.Run(h =>
    {
        var child = new Border { Width = 100, Height = 20 };
        var chrome = h.Show(new MaCalloutChrome
        {
            Side = side, ArrowSize = 7, BorderThickness = new Thickness(1), Padding = new Thickness(8, 5, 8, 5),
            Child = child, HorizontalAlignment = HorizontalAlignment.Left,
        });
        h.Pump();
        Assert.Equal(100 + 18 + extraW, chrome.ActualWidth, 3);
        Assert.Equal(20 + 12 + extraH, chrome.ActualHeight, 3);
        var at = child.TranslatePoint(new Point(0, 0), chrome);
        Assert.Equal(side == MaCalloutSide.Right ? 7 + 9 : 9, at.X, 3);
        Assert.Equal(side == MaCalloutSide.Bottom ? 7 + 6 : 6, at.Y, 3);
    });

    [Theory]
    [InlineData(MaCalloutSide.Right)]
    [InlineData(MaCalloutSide.Left)]
    [InlineData(MaCalloutSide.Top)]
    [InlineData(MaCalloutSide.Bottom)]
    public void Chrome_outline_reaches_the_arrow_tip_on_the_facing_edge(MaCalloutSide side)
    {
        var bounds = new Rect(0, 0, 120, 40);
        var g = MaCalloutChrome.BuildGeometry(bounds, side, 7, 20, 4, 1)!;
        var box = g.Bounds;
        // The tip touches the outer edge (inset by half the pen); the other edges are the bubble's.
        Assert.Equal(0.5, side switch
        {
            MaCalloutSide.Right => box.Left,
            MaCalloutSide.Left => bounds.Right - box.Right,
            MaCalloutSide.Bottom => box.Top,
            _ => bounds.Bottom - box.Bottom,
        }, 6);
        var tip = side switch
        {
            MaCalloutSide.Right => new Point(0.5, 20),
            MaCalloutSide.Left => new Point(119.5, 20),
            MaCalloutSide.Bottom => new Point(20, 0.5),
            _ => new Point(20, 39.5),
        };
        Assert.True(g.FillContains(tip, 0.01, System.Windows.Media.ToleranceType.Absolute) || g.StrokeContains(new System.Windows.Media.Pen(null, 1), tip));
        // Beside the tip, outside the bubble, is empty (it's a point, not a strip).
        var beside = side switch
        {
            MaCalloutSide.Right => new Point(2, 4),
            MaCalloutSide.Left => new Point(118, 4),
            MaCalloutSide.Bottom => new Point(4, 2),
            _ => new Point(4, 38),
        };
        Assert.False(g.FillContains(beside));
    }

    [Fact]
    public void Template_draws_with_theme_brushes_and_follows_the_side() => StaHost.Run(h =>
    {
        var tip = Themed(new MaCalloutToolTip { Content = "Cutoffs: 98% / 90%" });
        var chrome = Assert.IsType<MaCalloutChrome>(tip.Template.FindName("PART_Chrome", tip));
        Assert.Same(tip.FindResource("MaSurface"), chrome.Background);
        Assert.Same(tip.FindResource("MaChromeBorder"), chrome.BorderBrush);
        Assert.Same(tip.FindResource("MaText"), tip.Foreground);
        Assert.Equal(420, tip.MaxWidth);
        Assert.Equal(PlacementMode.Custom, tip.Placement);
        Assert.NotNull(tip.CustomPopupPlacementCallback);
        Assert.Equal(MaCalloutSide.Right, chrome.Side);

        tip.PreferredSide = MaCalloutSide.Bottom; // closed: the template shows the preferred side
        Assert.Equal(MaCalloutSide.Bottom, tip.Side);
        Assert.Equal(MaCalloutSide.Bottom, chrome.Side);
    });

    [Fact]
    public void Callout_text_wraps_at_the_tooltip_width() => StaHost.Run(_ =>
    {
        var tip = Themed(new MaCalloutToolTip { Content = string.Join(" ", Enumerable.Repeat("weekly reset Friday 9:00", 30)) });
        Assert.True(tip.DesiredSize.Width <= 420.5, $"{tip.DesiredSize.Width}");
        Assert.True(tip.DesiredSize.Height > 40, $"{tip.DesiredSize.Height}");
    });

    /// <summary>A ToolTip can't sit in a panel (WPF only parents it to its popup), so a test themes and lays one out
    /// on its own: Theme.xaml in its own resources, its implicit style applied, measured and arranged.</summary>
    internal static MaCalloutToolTip Themed(MaCalloutToolTip tip, params Uri[] extra)
    {
        _ = typeof(MaTheme);
        tip.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/MindAttic.Components.Wpf;component/Theme.xaml") });
        foreach (var uri in extra) tip.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = uri });
        tip.Style = (Style)tip.FindResource(typeof(MaCalloutToolTip));
        tip.ApplyTemplate();
        tip.Measure(new Size(2000, 2000));
        tip.Arrange(new Rect(tip.DesiredSize));
        tip.UpdateLayout();
        return tip;
    }

    [Fact]
    public void Attached_tooltip_becomes_a_callout_and_follows_its_value_and_placement() => StaHost.Run(h =>
    {
        var button = h.Show(new Button { Content = "x" });
        MaCallout.SetToolTip(button, "first");
        var callout = Assert.IsType<MaCalloutToolTip>(button.ToolTip);
        Assert.Equal("first", callout.Content);
        Assert.Equal(MaCalloutSide.Right, callout.PreferredSide);

        MaCallout.SetToolTip(button, "second");
        Assert.Same(callout, button.ToolTip); // one callout per element, its content kept live
        Assert.Equal("second", callout.Content);

        MaCallout.SetPlacement(button, MaCalloutSide.Top);
        Assert.Equal(MaCalloutSide.Top, callout.PreferredSide);

        MaCallout.SetToolTip(button, "");
        Assert.Null(button.ToolTip);
        MaCallout.SetToolTip(button, "back");
        Assert.Same(callout, button.ToolTip);
        MaCallout.SetToolTip(button, null);
        Assert.Null(button.ToolTip);
    });

    [Fact]
    public void Attached_tooltip_in_a_style_gives_each_element_its_own_bound_callout() => StaHost.Run(h =>
    {
        var style = new Style(typeof(Button));
        style.Setters.Add(new Setter(MaCallout.PlacementProperty, MaCalloutSide.Left));
        style.Setters.Add(new Setter(MaCallout.ToolTipProperty, new Binding("Tip")));
        style.Setters.Add(new Setter(ToolTipService.InitialShowDelayProperty, 0));
        var a = h.Show(new Button { Style = style, DataContext = new { Tip = "alpha" } });
        var b = h.Show(new Button { Style = style, DataContext = new { Tip = "beta" } });
        h.Pump();
        var ca = Assert.IsType<MaCalloutToolTip>(a.ToolTip);
        var cb = Assert.IsType<MaCalloutToolTip>(b.ToolTip);
        Assert.NotSame(ca, cb);
        Assert.Equal(("alpha", "beta"), (ca.Content, cb.Content));
        Assert.Equal(MaCalloutSide.Left, ca.PreferredSide);
        Assert.Equal(0, ToolTipService.GetInitialShowDelay(a)); // the owner's ToolTipService settings still apply
    });

    [Fact]
    public void Placement_callback_without_a_target_falls_back_beside_it() => StaHost.Run(_ =>
    {
        // WPF never calls it without a target on screen, but it mustn't throw if it did.
        var tip = new MaCalloutToolTip();
        var placed = Assert.Single(tip.CustomPopupPlacementCallback(new Size(100, 30), new Size(50, 20), new Point()));
        Assert.Equal(new Point(50, 0), placed.Point);
    });
}
