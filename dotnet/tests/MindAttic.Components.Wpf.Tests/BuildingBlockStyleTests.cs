using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T12: the building blocks the Orchestrator had to define itself are theme styles now.</summary>
public sealed class BuildingBlockStyleTests
{
    [Fact]
    public void A_pill_choice_row_wraps_its_items_and_fills_the_chosen_one_with_the_accent() => Sta(() =>
    {
        var list = new ListBox { ItemsSource = new[] { "Linear", "Front-loaded", "Off" }, SelectedIndex = 1 };
        var window = Show(list, "MaPillChoice");
        try
        {
            Assert.Equal(Brushes.Transparent.Color, ((SolidColorBrush)list.Background).Color);
            Assert.IsType<WrapPanel>(Find<WrapPanel>(list));
            var chosen = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(1);
            var other = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(0);
            Assert.Equal(window.FindResource("MaAccent"), Pill(chosen).Background);
            Assert.Equal(window.FindResource("MaChrome"), Pill(other).Background);
            Assert.Equal(new CornerRadius(14), Pill(chosen).CornerRadius);
            Assert.Same(window.FindResource("MaFocusRing"), other.FocusVisualStyle);
            Assert.True(other.MinHeight >= 32);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void A_pill_toggle_fills_with_the_accent_when_on() => Sta(() =>
    {
        var toggle = new ToggleButton { Content = "Autopilot" };
        var window = Show(toggle, "MaPillToggle");
        try
        {
            Assert.Equal(window.FindResource("MaSurface"), Pill(toggle).Background);
            toggle.IsChecked = true;
            Pump();
            Assert.Equal(window.FindResource("MaAccent"), Pill(toggle).Background);
            Assert.Same(window.FindResource("MaFocusRing"), toggle.FocusVisualStyle);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void A_card_is_a_raised_bordered_surface() => Sta(() =>
    {
        var card = new Border();
        var window = Show(card, "MaCard");
        try
        {
            Assert.Equal(window.FindResource("MaSurface"), card.Background);
            Assert.Equal(window.FindResource("MaChromeBorder"), card.BorderBrush);
            Assert.Equal(new CornerRadius(6), card.CornerRadius);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Stat_tiles_are_tile_buttons_with_the_focus_ring_and_the_small_one_speaks_its_tooltip() => Sta(() =>
    {
        var small = new Button { Content = "12", ToolTip = "Twelve agents working" };
        var window = Show(small, "MaSmallStat");
        try
        {
            var stat = (Style)window.FindResource("MaStatCard");
            Assert.Same(window.FindResource("MaTileButton"), stat.BasedOn);
            Assert.Same(stat, small.Style.BasedOn);
            Assert.Equal(74, small.Height);
            Assert.Equal(0, small.MinWidth);
            Assert.Same(window.FindResource("MaFocusRing"), small.FocusVisualStyle);
            Assert.Equal("Twelve agents working", AutomationProperties.GetHelpText(small));
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Text_styles_are_headings_where_they_should_be() => Sta(() =>
    {
        var heading = new TextBlock { Text = "Repos" };
        var window = Show(heading, "MaHeading");
        try
        {
            Assert.Equal(AutomationHeadingLevel.Level2, AutomationProperties.GetHeadingLevel(heading));
            // In the window, as in an app: the theme's brushes are dynamic and resolve from the tree (M.COMP-T11).
            var label = new TextBlock { Style = (Style)window.FindResource("MaSectionLabel") };
            var muted = new TextBlock { Style = (Style)window.FindResource("MaMutedText") };
            ((Grid)window.Content).Children.Add(label);
            ((Grid)window.Content).Children.Add(muted);
            Pump();
            Assert.Equal(AutomationHeadingLevel.Level3, AutomationProperties.GetHeadingLevel(label));
            Assert.Equal(window.FindResource("MaMuted"), label.Foreground);
            Assert.Equal(window.FindResource("MaMuted"), muted.Foreground);
            Assert.Equal(TextTrimming.CharacterEllipsis, muted.TextTrimming);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void The_focus_ring_is_a_2px_McFocus_outline_just_outside_the_control() => Sta(() =>
    {
        var window = Show(new Border(), "MaCard");
        try
        {
            var ring = (Style)window.FindResource("MaFocusRing");
            var template = (ControlTemplate)((Setter)ring.Setters.Single()).Value;
            var outline = (Border)template.LoadContent();
            ((Grid)window.Content).Children.Add(outline); // its MaFocus is dynamic and resolves from the tree (M.COMP-T11)
            Pump();
            Assert.Equal(new Thickness(2), outline.BorderThickness);
            Assert.Equal(new Thickness(-3), outline.Margin);
            Assert.Equal(((SolidColorBrush)window.FindResource("MaFocus")).Color, ((SolidColorBrush)outline.BorderBrush).Color);
        }
        finally { window.Close(); }
    });

    // ---- harness ----

    private static Window Show(FrameworkElement element, string styleKey)
    {
        var window = OffScreenWindow.Create(600, 300, new Grid { Children = { element } });
        element.Style = (Style)window.FindResource(styleKey);
        OffScreenWindow.Show(window);
        Pump();
        return window;
    }

    private static Border Pill(Control control)
    {
        control.ApplyTemplate();
        return (Border)control.Template.FindName("Bd", control);
    }

    private static T? Find<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) return match;
            if (Find<T>(child) is { } deeper) return deeper;
        }
        return null;
    }

    private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.SystemIdle, () => { });

    private static void Sta(Action test)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try { test(); }
            catch (Exception ex) { failure = ExceptionDispatchInfo.Capture(ex); }
            finally { Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}
