using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using MindAttic.Components.Wpf.Controls;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T34: McTextBox's placeholder and the multi-line McTextArea with scroll bars.</summary>
public sealed class McTextBoxTests
{
    [Fact]
    public void Placeholder_shows_muted_only_while_the_box_is_empty() => Sta(window =>
    {
        var box = Add(window, "McTextBox", b => McTextBox.SetPlaceholder(b, "Search repos and tickets"));
        var placeholder = (TextBlock)box.Template.FindName("Placeholder", box);

        Assert.Equal("Search repos and tickets", placeholder.Text);
        Assert.Equal(Visibility.Visible, placeholder.Visibility);
        Assert.Equal(window.FindResource("McMuted"), placeholder.Foreground);
        Assert.False(placeholder.IsHitTestVisible);

        box.Text = "orch";
        Pump();
        Assert.Equal(Visibility.Collapsed, placeholder.Visibility);

        box.Text = "";
        Pump();
        Assert.Equal(Visibility.Visible, placeholder.Visibility);
    });

    [Fact]
    public void A_click_on_the_placeholder_reaches_the_box() => Sta(window =>
    {
        var box = Add(window, "McTextBox", b => McTextBox.SetPlaceholder(b, "Reply to this tab"));
        var placeholder = (TextBlock)box.Template.FindName("Placeholder", box);
        var centre = placeholder.TranslatePoint(new Point(placeholder.ActualWidth / 2, placeholder.ActualHeight / 2), box);

        var hit = box.InputHitTest(centre) as DependencyObject;
        Assert.NotNull(hit);
        Assert.NotSame(placeholder, hit);
        Assert.Same(box, Ancestor<TextBox>(hit!));
    });

    [Fact]
    public void Placeholder_is_kept_out_of_the_accessible_name() => Sta(window =>
    {
        var named = Add(window, "McTextBox", b =>
        {
            McTextBox.SetPlaceholder(b, "Search repos and tickets   Ctrl+F");
            AutomationProperties.SetName(b, "Search repos and tickets");
        });
        var unnamed = Add(window, "McTextArea", b => McTextBox.SetPlaceholder(b, "Opening prompt (optional)"));

        var peer = UIElementAutomationPeer.CreatePeerForElement(named);
        Assert.Equal("Search repos and tickets", peer.GetName());
        Assert.DoesNotContain("Opening prompt", UIElementAutomationPeer.CreatePeerForElement(unnamed).GetName());
        foreach (var box in new[] { named, unnamed })
        {
            var children = UIElementAutomationPeer.CreatePeerForElement(box).GetChildren() ?? [];
            Assert.DoesNotContain(children, c => c.IsControlElement() && c.GetName().Contains(McTextBox.GetPlaceholder(box)));
        }
    });

    [Fact]
    public void McTextBox_keeps_its_scroll_bars_hidden_unless_the_host_asks() => Sta(window =>
    {
        var box = Add(window, "McTextBox", b => { b.Height = 60; b.AcceptsReturn = true; b.Text = Lines(40); });
        Assert.Equal(Visibility.Collapsed, Host(box).ComputedVerticalScrollBarVisibility);

        box.VerticalScrollBarVisibility = ScrollBarVisibility.Auto; // the Orchestrator's build log asks
        Pump();
        Assert.Equal(Visibility.Visible, Host(box).ComputedVerticalScrollBarVisibility);
    });

    [Fact]
    public void McTextArea_is_multi_line_and_shows_themed_scroll_bars_when_the_text_overflows() => Sta(window =>
    {
        var area = Add(window, "McTextArea", b => b.Height = 80);
        Assert.True(area.AcceptsReturn);
        Assert.Equal(TextWrapping.Wrap, area.TextWrapping);
        Assert.Equal(Visibility.Collapsed, Host(area).ComputedVerticalScrollBarVisibility);

        area.Text = Lines(40);
        Pump();
        var host = Host(area);
        Assert.Equal(Visibility.Visible, host.ComputedVerticalScrollBarVisibility);
        Assert.Equal(Visibility.Collapsed, host.ComputedHorizontalScrollBarVisibility); // wrapped text never needs one

        var bar = (ScrollBar)host.Template.FindName("PART_VerticalScrollBar", host);
        var thumb = Descendant<Thumb>(bar)!;
        Assert.Equal(window.FindResource("McControlBorder"), ((Border)VisualTreeHelper.GetChild(thumb, 0)).Background);
        Assert.True(bar.ActualWidth <= 12);

        area.TextWrapping = TextWrapping.NoWrap; // a log
        area.Text = new string('x', 400) + "\n" + Lines(40);
        Pump();
        Assert.Equal(Visibility.Visible, Host(area).ComputedHorizontalScrollBarVisibility);
    });

    // ---- harness ----

    private static string Lines(int count) => string.Join("\n", Enumerable.Range(1, count).Select(i => $"line {i}"));

    private static ScrollViewer Host(TextBox box) => (ScrollViewer)box.Template.FindName("PART_ContentHost", box);

    private static T? Ancestor<T>(DependencyObject node) where T : DependencyObject
    {
        for (var n = node; n is not null; n = VisualTreeHelper.GetParent(n))
            if (n is T t) return t;
        return null;
    }

    private static T? Descendant<T>(DependencyObject node) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
            var child = VisualTreeHelper.GetChild(node, i);
            if (child is T t) return t;
            if (Descendant<T>(child) is { } found) return found;
        }
        return null;
    }

    private static TextBox Add(Window window, string style, Action<TextBox> setup)
    {
        var box = new TextBox { Width = 300, Style = (Style)window.FindResource(style) };
        setup(box);
        ((StackPanel)window.Content).Children.Add(box);
        Pump();
        return box;
    }

    private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ContextIdle, () => { });

    private static void Sta(Action<Window> test)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            Window? window = null;
            try
            {
                window = OffScreenWindow.Show(400, 400, new StackPanel());
                Pump();
                test(window);
            }
            catch (Exception ex)
            {
                failure = ExceptionDispatchInfo.Capture(ex);
            }
            finally
            {
                window?.Close();
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}
