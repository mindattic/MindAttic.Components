using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// Window chrome helpers. <c>mc:MaWindow.DarkTitleBar="True"</c> on a <see cref="Window"/> asks
/// Windows (10 20H1 and later) to draw its title bar and frame dark, so the system caption matches
/// the MaChrome theme instead of a light strip above it. Older Windows ignores the request.
/// </summary>
public static class MaWindow
{
    public static readonly DependencyProperty DarkTitleBarProperty = DependencyProperty.RegisterAttached(
        "DarkTitleBar", typeof(bool), typeof(MaWindow), new PropertyMetadata(false, OnDarkTitleBarChanged));

    public static bool GetDarkTitleBar(Window window) => (bool)window.GetValue(DarkTitleBarProperty);

    public static void SetDarkTitleBar(Window window, bool value) => window.SetValue(DarkTitleBarProperty, value);

    private static void OnDarkTitleBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window) return;
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd != IntPtr.Zero) Apply(hwnd, (bool)e.NewValue);
        else window.SourceInitialized += (_, _) => Apply(new WindowInteropHelper(window).Handle, GetDarkTitleBar(window));
    }

    // DWMWA_USE_IMMERSIVE_DARK_MODE is 20 from Windows 10 20H1; builds before that used 19.
    private const int DwmUseImmersiveDarkMode = 20, DwmUseImmersiveDarkModeBefore20H1 = 19;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private static void Apply(IntPtr hwnd, bool dark)
    {
        int on = dark ? 1 : 0;
        if (DwmSetWindowAttribute(hwnd, DwmUseImmersiveDarkMode, ref on, sizeof(int)) != 0)
            DwmSetWindowAttribute(hwnd, DwmUseImmersiveDarkModeBefore20H1, ref on, sizeof(int));
    }
}
