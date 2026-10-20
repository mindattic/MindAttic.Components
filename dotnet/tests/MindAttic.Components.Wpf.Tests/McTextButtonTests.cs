using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T36: McTextButton honours HorizontalContentAlignment / VerticalContentAlignment.</summary>
public sealed class McTextButtonTests
{
    [Fact]
    public void Content_is_centred_by_default() => Sta(() =>
    {
        var (window, button) = Show(null);
        try
        {
            var presenter = Presenter(button);
            Assert.Equal(HorizontalAlignment.Center, button.HorizontalContentAlignment);
            Assert.Equal(VerticalAlignment.Center, button.VerticalContentAlignment);
            Assert.Equal(HorizontalAlignment.Center, presenter.HorizontalAlignment);
            Assert.Equal(VerticalAlignment.Center, presenter.VerticalAlignment);
            Assert.True(LeftOffset(button, presenter) > 50); // sits in the middle of a 400px-wide button
        }
        finally { window.Close(); }
    });

    [Fact]
    public void A_fill_width_button_can_left_align_its_content() => Sta(() =>
    {
        var (window, button) = Show(b =>
        {
            b.HorizontalContentAlignment = HorizontalAlignment.Left;
            b.VerticalContentAlignment = VerticalAlignment.Top;
        });
        try
        {
            var presenter = Presenter(button);
            Assert.Equal(HorizontalAlignment.Left, presenter.HorizontalAlignment);
            Assert.Equal(VerticalAlignment.Top, presenter.VerticalAlignment);
            // Flush against the left padding (14px) plus the 1px focus border, not centred.
            Assert.InRange(LeftOffset(button, presenter), 0, 20);
        }
        finally { window.Close(); }
    });

    // ---- harness ----

    private static (Window, Button) Show(Action<Button>? configure)
    {
        _ = System.IO.Packaging.PackUriHelper.UriSchemePack; // registers pack:// outside an Application
        var button = new Button { Content = "short", HorizontalAlignment = HorizontalAlignment.Stretch, Width = 400 };
        // M.COMP-T28: off-screen, borderless, out of the taskbar and never activated.
        var window = new Window
        {
            Width = 600, Height = 200, Left = -20000, Top = -20000,
            WindowStyle = WindowStyle.None, ShowInTaskbar = false, ShowActivated = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Content = new Grid { Children = { button } },
        };
        window.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/MindAttic.Components.Wpf;component/Theme.xaml"),
        });
        button.Style = (Style)window.FindResource("McTextButton");
        configure?.Invoke(button);
        window.Show();
        Pump();
        return (window, button);
    }

    private static ContentPresenter Presenter(Button button)
    {
        button.ApplyTemplate();
        return Find<ContentPresenter>(button) ?? throw new InvalidOperationException("no ContentPresenter");
    }

    private static double LeftOffset(Button button, ContentPresenter presenter) =>
        presenter.TransformToAncestor(button).Transform(new Point(0, 0)).X;

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
