using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T32: McProgressBar, the thin rounded progress bar style.</summary>
public sealed class McProgressBarTests
{
    [Fact]
    public void Is_a_thin_rounded_track_with_a_coloured_fill() => Sta((window, bar) =>
    {
        var track = (Border)bar.Template.FindName("TrackBorder", bar);
        var fill = (Border)bar.Template.FindName("PART_Indicator", bar);

        Assert.Equal(6, bar.ActualHeight);
        Assert.Equal(window.FindResource("McSurface"), track.Background);
        Assert.Equal(window.FindResource("McControlBorder"), track.BorderBrush);
        Assert.True(track.CornerRadius.TopLeft > 0);
        Assert.Equal(window.FindResource("McAccent"), fill.Background);

        var budget = new SolidColorBrush(Colors.Orange);
        bar.Foreground = budget;
        Pump();
        Assert.Same(budget, fill.Background); // a host's colour (e.g. a budget brush) reaches the fill
    });

    [Fact]
    public void Determinate_fills_the_values_share_of_the_track() => Sta((window, bar) =>
    {
        var track = (FrameworkElement)bar.Template.FindName("PART_Track", bar);
        var fill = (FrameworkElement)bar.Template.FindName("PART_Indicator", bar);

        bar.Value = 40;
        Pump();
        Assert.Equal(Visibility.Visible, fill.Visibility);
        Assert.Equal(track.ActualWidth * 0.4, fill.ActualWidth, 1);

        bar.Minimum = 0;
        bar.Maximum = 1; // a 0..1 fraction, the way the Orchestrator's budget fill is expressed
        bar.Value = 0.75;
        Pump();
        Assert.Equal(track.ActualWidth * 0.75, fill.ActualWidth, 1);

        bar.Value = 0;
        Pump();
        Assert.Equal(0, fill.ActualWidth, 1);
    });

    [Fact]
    public void Indeterminate_sweeps_a_segment_instead_of_a_fill() => Sta((window, bar) =>
    {
        var fill = (FrameworkElement)bar.Template.FindName("PART_Indicator", bar);
        var sweep = (Border)bar.Template.FindName("IndeterminateFill", bar);
        Assert.Equal(Visibility.Collapsed, sweep.Visibility);

        bar.IsIndeterminate = true;
        Pump();
        Assert.Equal(Visibility.Collapsed, fill.Visibility);
        Assert.Equal(Visibility.Visible, sweep.Visibility);
        Assert.True(((TranslateTransform)sweep.OpacityMask.RelativeTransform).HasAnimatedProperties);

        bar.IsIndeterminate = false;
        Pump();
        Assert.Equal(Visibility.Visible, fill.Visibility);
        Assert.Equal(Visibility.Collapsed, sweep.Visibility);
        Assert.False(((TranslateTransform)sweep.OpacityMask.RelativeTransform).HasAnimatedProperties);
    });

    [Fact]
    public void Starts_indeterminate_when_set_before_it_is_shown() => Sta((window, bar) =>
    {
        var fresh = new ProgressBar { IsIndeterminate = true, Width = 200, Style = (Style)window.FindResource("McProgressBar") };
        ((StackPanel)window.Content).Children.Add(fresh);
        Pump();
        var sweep = (Border)fresh.Template.FindName("IndeterminateFill", fresh);
        Assert.Equal(Visibility.Visible, sweep.Visibility);
        Assert.True(((TranslateTransform)sweep.OpacityMask.RelativeTransform).HasAnimatedProperties);
    });

    // ---- harness ----

    private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ContextIdle, () => { });

    private static void Sta(Action<Window, ProgressBar> test)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            Window? window = null;
            try
            {
                _ = System.IO.Packaging.PackUriHelper.UriSchemePack; // registers pack:// outside an Application
                // M.COMP-T28: a real window for layout, but far off-screen, borderless, out of the
                // taskbar and shown without activation, so dotnet test never steals the keyboard.
                window = new Window
                {
                    Width = 400, Height = 200, Left = -20000, Top = -20000,
                    WindowStyle = WindowStyle.None, ShowInTaskbar = false, ShowActivated = false,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                };
                window.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/MindAttic.Components.Wpf;component/Theme.xaml"),
                });
                var bar = new ProgressBar { Width = 300, Style = (Style)window.FindResource("McProgressBar") };
                window.Content = new StackPanel { Children = { bar } };
                window.Show();
                Pump();
                test(window, bar);
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
