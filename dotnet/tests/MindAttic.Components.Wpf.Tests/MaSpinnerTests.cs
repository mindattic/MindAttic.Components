using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MindAttic.Components.Wpf.Controls;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T33: MaSpinner, the busy ring that turns on its own thread (as the Orchestrator's loader spinner does,
/// M.ORCH-T200), so it keeps turning while the UI thread is blocked.</summary>
public sealed class MaSpinnerTests
{
    [Fact]
    public void It_keeps_turning_while_the_ui_thread_is_blocked_and_stops_its_thread_when_hidden() => Sta(root =>
    {
        var spinner = new MaSpinner { Width = 44, Height = 44 };
        root.Children.Add(spinner);
        Pump();

        Assert.True(spinner.IsSpinning);
        Assert.NotNull(spinner.SpinnerThread);
        Assert.NotSame(Thread.CurrentThread, spinner.SpinnerThread);

        // This thread now does nothing but block, as a window's thread does applying a big result; the ring still turns.
        var angles = new List<double>();
        for (var i = 0; i < 4; i++)
        {
            angles.Add(spinner.Angle ?? double.NaN);
            Thread.Sleep(150);
        }
        Assert.True(angles.Distinct().Count() > 1, $"angles while blocked: {string.Join(", ", angles)}");

        var ring = spinner.SpinnerThread!;
        spinner.Visibility = Visibility.Collapsed;
        Assert.False(spinner.IsSpinning);
        Assert.Null(spinner.Angle);
        Assert.True(ring.Join(TimeSpan.FromSeconds(5)), "the ring's thread ends once the spinner is hidden");

        spinner.Visibility = Visibility.Visible;
        Assert.True(spinner.IsSpinning);
        Assert.NotSame(ring, spinner.SpinnerThread);
    });

    [Fact]
    public void Removing_it_from_the_window_ends_its_thread() => Sta(root =>
    {
        var spinner = new MaSpinner();
        root.Children.Add(spinner);
        Pump();
        var ring = spinner.SpinnerThread!;

        root.Children.Remove(spinner);
        Pump();
        Assert.False(spinner.IsSpinning);
        Assert.True(ring.Join(TimeSpan.FromSeconds(5)));
    });

    [Fact]
    public void A_spinner_that_starts_hidden_starts_no_thread() => Sta(root =>
    {
        var spinner = new MaSpinner { Visibility = Visibility.Collapsed };
        root.Children.Add(spinner);
        Pump();
        Assert.False(spinner.IsSpinning);
        Assert.Null(spinner.SpinnerThread);
    });

    [Fact]
    public void The_theme_draws_it_in_McAccent_unless_the_host_sets_a_stroke() => Sta(root =>
    {
        var themed = new MaSpinner();
        var red = new MaSpinner { Stroke = Brushes.Red };
        root.Children.Add(themed);
        root.Children.Add(red);
        Pump();

        Assert.Same(root.FindResource("MaAccent"), themed.Stroke);
        Assert.Same(Brushes.Red, red.Stroke);
    });

    [Fact]
    public void Changing_its_look_while_it_turns_restarts_the_ring() => Sta(root =>
    {
        var spinner = new MaSpinner();
        root.Children.Add(spinner);
        Pump();
        var first = spinner.SpinnerThread!;

        spinner.Stroke = Brushes.Orange;
        Assert.True(spinner.IsSpinning);
        Assert.NotSame(first, spinner.SpinnerThread);
        Assert.True(first.Join(TimeSpan.FromSeconds(5)));
    });

    [Fact]
    public void It_is_44_square_by_default_and_takes_its_width_and_height() => Sta(root =>
    {
        var plain = new MaSpinner { HorizontalAlignment = HorizontalAlignment.Left };
        var small = new MaSpinner { Width = 20, Height = 20, HorizontalAlignment = HorizontalAlignment.Left };
        root.Children.Add(plain);
        root.Children.Add(small);
        Pump();

        Assert.Equal(new Size(44, 44), plain.RenderSize);
        Assert.Equal(new Size(20, 20), small.RenderSize);
    });

    [Fact]
    public void It_reads_as_a_progress_bar_named_Loading_and_is_never_a_tab_stop() => Sta(root =>
    {
        var plain = new MaSpinner();
        var named = new MaSpinner();
        AutomationProperties.SetName(named, "Loading the workspace");
        root.Children.Add(plain);
        root.Children.Add(named);
        Pump();

        var peer = UIElementAutomationPeer.CreatePeerForElement(plain);
        Assert.Equal(AutomationControlType.ProgressBar, peer.GetAutomationControlType());
        Assert.Equal("Loading", peer.GetName());
        Assert.Equal(nameof(MaSpinner), peer.GetClassName());
        Assert.False(peer.IsKeyboardFocusable());
        Assert.False(plain.Focusable);
        Assert.Equal("Loading the workspace", UIElementAutomationPeer.CreatePeerForElement(named).GetName());
    });

    // ---- harness ----

    private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ContextIdle, () => { });

    private static void Sta(Action<StackPanel> test)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            Window? window = null;
            try
            {
                var root = new StackPanel();
                window = OffScreenWindow.Show(200, 200, root);
                Pump();
                test(root);
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
