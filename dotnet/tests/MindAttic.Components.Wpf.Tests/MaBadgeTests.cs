using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MindAttic.Components.Wpf.Controls;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T31: MaBadge, the outlined state pill, and its plain MaTag variant.</summary>
public sealed class MaBadgeTests
{
    private static readonly SolidColorBrush Running = Frozen(new SolidColorBrush(Color.FromRgb(0x3F, 0xB9, 0x50)));

    private static SolidColorBrush Frozen(SolidColorBrush brush) { brush.Freeze(); return brush; }

    [Fact]
    public void A_state_badge_draws_glyph_word_and_outline_in_its_state_colour() => Sta(h =>
    {
        var badge = new MaBadge { Glyph = "\uE768", Content = "Running", Foreground = Running };
        h.Show(badge);

        var border = (Border)badge.Template.FindName("PART_Border", badge);
        var glyph = (TextBlock)badge.Template.FindName("PART_Glyph", badge);

        Assert.Same(Running, border.BorderBrush);
        Assert.Equal(new CornerRadius(10), border.CornerRadius);
        Assert.Equal(new Thickness(1), border.BorderThickness);
        Assert.Equal(Visibility.Visible, glyph.Visibility);
        Assert.Equal("\uE768", glyph.Text);
        Assert.Equal("Segoe MDL2 Assets", glyph.FontFamily.Source);
        Assert.Same(Running, glyph.Foreground);
        Assert.Equal(FontWeights.SemiBold, badge.FontWeight);
        var word = Assert.Single(FindAll<TextBlock>(badge), t => t.Text == "Running");
        Assert.Same(Running, word.Foreground);
        Assert.False(badge.Focusable);
    });

    [Fact]
    public void The_outline_follows_a_state_colour_that_changes() => Sta(h =>
    {
        var badge = new MaBadge { Glyph = "\uE768", Content = "Running", Foreground = Running };
        h.Show(badge);
        var border = (Border)badge.Template.FindName("PART_Border", badge);

        badge.Foreground = Brushes.Orange;
        h.Pump();
        Assert.Same(Brushes.Orange, border.BorderBrush);
    });

    [Fact]
    public void Without_a_glyph_the_glyph_and_its_gap_collapse() => Sta(h =>
    {
        var badge = new MaBadge { Content = "Done" };
        h.Show(badge);
        var glyph = (TextBlock)badge.Template.FindName("PART_Glyph", badge);
        Assert.Equal(Visibility.Collapsed, glyph.Visibility);
        Assert.Equal(h.Window.FindResource("MaText"), badge.Foreground);
    });

    [Fact]
    public void The_glyph_is_hidden_from_screen_readers_which_read_only_the_word() => Sta(h =>
    {
        var badge = new MaBadge { Glyph = "\uE768", Content = "Running", Foreground = Running };
        h.Show(badge);
        var peer = UIElementAutomationPeer.CreatePeerForElement(badge);
        Assert.Equal("Running", peer.GetName());
        Assert.Equal(AutomationControlType.Text, peer.GetAutomationControlType());
        var texts = Flatten(peer).Where(p => p.GetAutomationControlType() == AutomationControlType.Text)
                                 .Select(p => p.GetName()).ToList();
        Assert.Contains("Running", texts);
        Assert.DoesNotContain("\uE768", texts);
    });

    [Fact]
    public void MaTag_is_a_plain_square_neutral_label() => Sta(h =>
    {
        var tag = new MaBadge { Content = "general-purpose", Style = (Style)h.Window.FindResource("MaTag") };
        h.Show(tag);
        var border = (Border)tag.Template.FindName("PART_Border", tag);
        var glyph = (TextBlock)tag.Template.FindName("PART_Glyph", tag);

        Assert.Equal(h.Window.FindResource("MaControlBorder"), border.BorderBrush);
        Assert.Equal(new CornerRadius(3), border.CornerRadius);
        Assert.Equal(new Thickness(5, 0, 5, 0), border.Padding);
        Assert.Equal(11d, tag.FontSize);
        Assert.Equal(FontWeights.Normal, tag.FontWeight);
        Assert.Equal(Visibility.Collapsed, glyph.Visibility);
        Assert.Single(FindAll<TextBlock>(tag), t => t.Text == "general-purpose");
    });

    // ---- harness ----

    private static IEnumerable<AutomationPeer> Flatten(AutomationPeer peer)
    {
        foreach (var child in peer.GetChildren() ?? [])
        {
            yield return child;
            foreach (var nested in Flatten(child)) yield return nested;
        }
    }

    private static IEnumerable<T> FindAll<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T t) yield return t;
            foreach (var nested in FindAll<T>(child)) yield return nested;
        }
    }

    private sealed class Host
    {
        private readonly StackPanel _root = new();
        public Window Window { get; }

        public Host()
        {
            Window = OffScreenWindow.Show(400, 200, _root);
            Pump();
        }

        public void Show(FrameworkElement element)
        {
            _root.Children.Add(element);
            Pump();
            element.ApplyTemplate();
        }

        public void Pump() => Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ContextIdle, () => { });
    }

    private static void Sta(Action<Host> test)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            Host? host = null;
            try
            {
                host = new Host();
                test(host);
            }
            catch (Exception ex)
            {
                failure = ExceptionDispatchInfo.Capture(ex);
            }
            finally
            {
                host?.Window.Close();
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}
