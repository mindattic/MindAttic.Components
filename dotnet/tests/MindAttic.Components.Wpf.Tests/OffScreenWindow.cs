using System.Windows;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>The only way a test builds or shows a WPF window (M.COMP-T39): a real window (templates, layout, focus and
/// input need an HwndSource) that never appears: far off every screen, borderless, fully transparent (so even maximized
/// onto a monitor it draws nothing and clicks pass through), out of the taskbar and Alt+Tab, and never activated, so
/// dotnet test never pops a blank 'testhost' window or steals the user's keyboard. The window carries the library's
/// Theme.xaml. <see cref="TestWindowHygieneTests"/> fails any test source that shows or activates a window any other way.</summary>
internal static class OffScreenWindow
{
    public const double Offset = -20000;

    /// <summary>A themed window set up as above, not yet shown (so a test can look up a style first).</summary>
    public static Window Create(double width = 400, double height = 200, object? content = null)
    {
        _ = System.IO.Packaging.PackUriHelper.UriSchemePack; // registers pack:// outside an Application
        var window = new Window
        {
            Content = content, Width = width, Height = height, Left = Offset, Top = Offset,
            WindowStartupLocation = WindowStartupLocation.Manual,
            WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize, AllowsTransparency = true, Opacity = 0,
            ShowInTaskbar = false, ShowActivated = false, Topmost = false, Title = "MindAttic test window (off-screen)",
        };
        window.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/MindAttic.Components.Wpf;component/Theme.xaml"),
        });
        return window;
    }

    /// <summary>Shows a window from <see cref="Create"/> without activating it.</summary>
    public static Window Show(Window window)
    {
        window.Show();
        return window;
    }

    /// <summary>Creates and shows one without activating it.</summary>
    public static Window Show(double width = 400, double height = 200, object? content = null) =>
        Show(Create(width, height, content));
}
