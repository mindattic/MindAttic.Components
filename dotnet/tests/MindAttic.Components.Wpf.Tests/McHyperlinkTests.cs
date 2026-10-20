using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using MindAttic.Components.Wpf.Controls;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T35: McHyperlink, the themed link style for a Hyperlink inside text.</summary>
public sealed class McHyperlinkTests
{
    [Fact]
    public void At_rest_a_link_is_McLink_SemiBold_and_not_underlined() => Sta(h =>
    {
        var link = h.AddLink("M.ORCH-T499");

        Assert.Same(h.Window.FindResource("McLink"), link.Foreground);
        Assert.Equal(FontWeights.SemiBold, link.FontWeight);
        Assert.False(IsUnderlined(link.TextDecorations));
    });

    [Fact]
    public void Keyboard_focus_underlines_the_link_and_losing_it_removes_the_underline() => Sta(h =>
    {
        var link = h.AddLink("M.ORCH-T499");
        var other = new Button { Content = "Elsewhere" };
        h.Root.Children.Add(other);
        h.Pump();

        Assert.True(link.Focus());
        h.Pump();
        Assert.True(IsUnderlined(link.TextDecorations));

        Assert.True(other.Focus());
        h.Pump();
        Assert.False(IsUnderlined(link.TextDecorations));
    });

    [Fact]
    public void Mouse_hover_underlines_the_link() => Sta(h =>
    {
        // IsMouseOver is read-only, so check the style's own trigger.
        var style = (Style)h.Window.FindResource("McHyperlink");
        var hover = Assert.Single(style.Triggers.OfType<Trigger>(), t => t.Property == ContentElement.IsMouseOverProperty);
        Assert.Equal(true, hover.Value);
        var setter = Assert.IsType<Setter>(Assert.Single(hover.Setters));
        Assert.Equal(Inline.TextDecorationsProperty, setter.Property);
        Assert.True(IsUnderlined(setter.Value as TextDecorationCollection));
    });

    [Fact]
    public void A_disabled_link_is_muted() => Sta(h =>
    {
        var link = h.AddLink("M.ORCH-T499");
        link.IsEnabled = false;
        h.Pump();
        Assert.Same(h.Window.FindResource("McMuted"), link.Foreground);
    });

    /// <summary>M.COMP-T16 still holds with the style: a styled link inside an open McModal
    /// takes focus (and its underline) without throwing, and focus stays trapped.</summary>
    [Fact]
    public void A_styled_link_inside_an_McModal_takes_focus() => Sta(h =>
    {
        var outside = h.AddLink("Help");
        var inside = new Hyperlink(new Run("Docs")) { Focusable = true, Style = (Style)h.Window.FindResource("McHyperlink") };
        var modal = new McModal { Title = "Settings", Content = new TextBlock(inside) };
        h.Grid.Children.Add(modal);
        h.Pump();

        Assert.True(outside.Focus());
        modal.IsOpen = true;
        h.Pump();
        Assert.True(inside.Focus());
        h.Pump();
        Assert.Same(inside, Keyboard.FocusedElement);
        Assert.True(IsUnderlined(inside.TextDecorations));

        modal.IsOpen = false;
        h.Pump();
        Assert.Same(outside, Keyboard.FocusedElement);
    });

    // ---- harness ----

    private static bool IsUnderlined(TextDecorationCollection? decorations) =>
        decorations is not null && decorations.Any(d => d.Location == TextDecorationLocation.Underline);

    private sealed class Host
    {
        public Grid Grid { get; } = new();
        public StackPanel Root { get; } = new();
        public Window Window { get; }

        public Host()
        {
            Grid.Children.Add(Root);
            // M.COMP-T28/T39: off-screen, never activated.
            Window = OffScreenWindow.Show(400, 300, Grid);
            Pump();
        }

        public Hyperlink AddLink(string text)
        {
            var link = new Hyperlink(new Run(text)) { Focusable = true, Style = (Style)Window.FindResource("McHyperlink") };
            Root.Children.Add(new TextBlock(link));
            Pump();
            return link;
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
