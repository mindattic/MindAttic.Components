using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// A callout tooltip (M.COMP-T167): the library's tooltip look (a bubble on MaSurface with an MaChromeBorder outline,
/// wrapping at 420) plus a small arrow on the edge facing the element it belongs to, pointing at that element's centre.
/// It places itself like Floating UI / Popper: on <see cref="PreferredSide"/> when it fits inside the target's monitor
/// work area, flipped to the opposite side when it doesn't (or onto the roomier side when neither fits), shifted along
/// the edge to stay on screen, with the arrow following the target (see <see cref="CalloutPlacement"/> for the rules).
/// It re-places itself every time it opens and whenever its content changes size while open, per-monitor DPI aware.
/// <para>
/// The easy way in is the <see cref="MaCallout"/> attached properties, which keep a plain text tooltip binding:
/// </para>
/// <code>
/// &lt;Setter Property="ctl:MaCallout.ToolTip" Value="{Binding HoverText}" /&gt;
/// &lt;Setter Property="ctl:MaCallout.Placement" Value="Right" /&gt;
/// </code>
/// <para>or use it directly as an element's ToolTip:</para>
/// <code>
/// &lt;Button.ToolTip&gt;&lt;ctl:MaCalloutToolTip PreferredSide="Bottom" Content="Saves the file" /&gt;&lt;/Button.ToolTip&gt;
/// </code>
/// <para>
/// Its look is the implicit <c>MaCalloutToolTip</c> style in Theme.xaml, drawn by <see cref="MaCalloutChrome"/>
/// from theme brushes only, so it follows MaTheme (light/dark) and MaHighContrast live. It sets
/// <see cref="ToolTip.Placement"/> to Custom itself; don't set ToolTipService.Placement on the owner, which WPF lets
/// win over the tooltip's own placement. ToolTipService's delays and durations on the owner still apply.
/// </para>
/// </summary>
public class MaCalloutToolTip : ToolTip
{
    public static readonly DependencyProperty PreferredSideProperty = DependencyProperty.Register(
        nameof(PreferredSide), typeof(MaCalloutSide), typeof(MaCalloutToolTip),
        new FrameworkPropertyMetadata(MaCalloutSide.Right, OnPlacementInputChanged));

    private static readonly DependencyPropertyKey SidePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Side), typeof(MaCalloutSide), typeof(MaCalloutToolTip), new FrameworkPropertyMetadata(MaCalloutSide.Right));

    public static readonly DependencyProperty SideProperty = SidePropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey ArrowOffsetPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(ArrowOffset), typeof(double), typeof(MaCalloutToolTip), new FrameworkPropertyMetadata(double.NaN));

    public static readonly DependencyProperty ArrowOffsetProperty = ArrowOffsetPropertyKey.DependencyProperty;

    public static readonly DependencyProperty ArrowSizeProperty = DependencyProperty.Register(
        nameof(ArrowSize), typeof(double), typeof(MaCalloutToolTip), new FrameworkPropertyMetadata(7d, OnPlacementInputChanged));

    public static readonly DependencyProperty GapProperty = DependencyProperty.Register(
        nameof(Gap), typeof(double), typeof(MaCalloutToolTip), new FrameworkPropertyMetadata(2d, OnPlacementInputChanged));

    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
        nameof(CornerRadius), typeof(double), typeof(MaCalloutToolTip), new FrameworkPropertyMetadata(4d, OnPlacementInputChanged));

    public MaCalloutToolTip()
    {
        // Local values: the implicit style can't be relied on to set these before WPF first places the popup.
        Placement = PlacementMode.Custom;
        CustomPopupPlacementCallback = Place;
        Side = PreferredSide;
    }

    /// <summary>The side to show on when it fits (Right by default).</summary>
    public MaCalloutSide PreferredSide { get => (MaCalloutSide)GetValue(PreferredSideProperty); set => SetValue(PreferredSideProperty, value); }

    /// <summary>The side it is actually on, set each time it is placed; the template draws the arrow from it.</summary>
    public MaCalloutSide Side { get => (MaCalloutSide)GetValue(SideProperty); private set => SetValue(SidePropertyKey, value); }

    /// <summary>The arrow tip's distance along the bubble's facing edge, in DIPs, set each time it is placed
    /// (NaN, centred, until then).</summary>
    public double ArrowOffset { get => (double)GetValue(ArrowOffsetProperty); private set => SetValue(ArrowOffsetPropertyKey, value); }

    /// <summary>How far the arrow sticks out of the bubble, in DIPs (7).</summary>
    public double ArrowSize { get => (double)GetValue(ArrowSizeProperty); set => SetValue(ArrowSizeProperty, value); }

    /// <summary>Space between the arrow tip and the target's edge, in DIPs (2).</summary>
    public double Gap { get => (double)GetValue(GapProperty); set => SetValue(GapProperty, value); }

    /// <summary>The bubble's corner radius, in DIPs (4); the arrow keeps clear of it.</summary>
    public double CornerRadius { get => (double)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    private static void OnPlacementInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var tip = (MaCalloutToolTip)d;
        if (!tip.IsOpen) tip.Side = tip.PreferredSide;
        else tip.Reposition();
    }

    /// <summary>Asks the open popup to run the placement again (it does by itself when the content's size changes).
    /// Popup.Reposition is internal, but an offset change makes it re-place, so nudge the offset and put it back.</summary>
    private void Reposition()
    {
        if (ReadLocalValue(HorizontalOffsetProperty) != DependencyProperty.UnsetValue) return;
        HorizontalOffset += 0.01;
        ClearValue(HorizontalOffsetProperty);
    }

    /// <summary>The Popup's Custom placement: WPF passes the popup's and the target's sizes in device pixels and adds
    /// the returned point to the target's top-left on screen, so everything here is in screen device pixels.</summary>
    private CustomPopupPlacement[] Place(Size popupSize, Size targetSize, Point offset)
    {
        var target = PlacementTarget;
        if (target is null || PresentationSource.FromVisual(target) is null)
            return [new CustomPopupPlacement(new Point(targetSize.Width, 0), PopupPrimaryAxis.Vertical)];

        var dpi = VisualTreeHelper.GetDpi(target);
        var topLeft = target.PointToScreen(new Point(0, 0));
        var targetRect = new Rect(topLeft, targetSize);
        var result = PlaceOnScreen(targetRect, popupSize, Side, MonitorWorkArea(targetRect, dpi), PreferredSide,
            Gap, ArrowSize, CornerRadius, dpi.DpiScaleX, dpi.DpiScaleY);

        Side = result.Side;
        ArrowOffset = result.ArrowOffsetDip;
        var axis = result.Side is MaCalloutSide.Left or MaCalloutSide.Right ? PopupPrimaryAxis.Vertical : PopupPrimaryAxis.Horizontal;
        return [new CustomPopupPlacement(new Point(result.PopupOrigin.X - topLeft.X, result.PopupOrigin.Y - topLeft.Y), axis)];
    }

    /// <summary>Where the whole popup (bubble plus arrow strip) goes, in device pixels, given its current size
    /// <paramref name="popupSize"/> as laid out for <paramref name="measuredSide"/>; and the arrow offset in DIPs for
    /// the template. Pulled out of the Popup callback so the DIP/device-pixel arithmetic is testable.</summary>
    public static (MaCalloutSide Side, Point PopupOrigin, double ArrowOffsetDip) PlaceOnScreen(
        Rect targetDevice, Size popupSize, MaCalloutSide measuredSide, Rect workAreaDevice, MaCalloutSide preferred,
        double gapDip, double arrowDip, double cornerDip, double scaleX, double scaleY)
    {
        // The bubble alone: the popup less the arrow strip it was measured with (the bubble's size doesn't depend on
        // the side, so the side chosen from it is stable once the template catches up).
        var strip = MaCalloutChrome.ArrowStrip(measuredSide, arrowDip);
        var bubble = new Size(Math.Max(0, popupSize.Width - (strip.Left + strip.Right) * scaleX),
            Math.Max(0, popupSize.Height - (strip.Top + strip.Bottom) * scaleY));

        // A flip stays on the preferred side's axis, so the main and cross scales are known up front.
        var horizontal = preferred is MaCalloutSide.Left or MaCalloutSide.Right;
        var main = horizontal ? scaleX : scaleY;
        var cross = horizontal ? scaleY : scaleX;
        var placed = CalloutPlacement.Compute(targetDevice, bubble, workAreaDevice, preferred, gapDip * main, arrowDip * main, cornerDip * cross);

        var origin = placed.BubbleOrigin;
        if (placed.Side == MaCalloutSide.Right) origin.X -= arrowDip * scaleX;
        if (placed.Side == MaCalloutSide.Bottom) origin.Y -= arrowDip * scaleY;
        return (placed.Side, origin, placed.ArrowOffset / cross);
    }

    /// <summary>The work area (screen minus taskbar) of the monitor showing most of <paramref name="targetDevice"/>, in
    /// device pixels; SystemParameters.WorkArea scaled to the target's DPI if Windows won't say.</summary>
    private static Rect MonitorWorkArea(Rect targetDevice, DpiScale dpi)
    {
        try
        {
            var rect = new RECT
            {
                Left = (int)Math.Floor(targetDevice.Left), Top = (int)Math.Floor(targetDevice.Top),
                Right = (int)Math.Ceiling(targetDevice.Right), Bottom = (int)Math.Ceiling(targetDevice.Bottom),
            };
            var monitor = MonitorFromRect(ref rect, MONITOR_DEFAULTTONEAREST);
            var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref info))
                return new Rect(info.rcWork.Left, info.rcWork.Top, info.rcWork.Right - info.rcWork.Left, info.rcWork.Bottom - info.rcWork.Top);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
        }
        var w = SystemParameters.WorkArea;
        return new Rect(w.Left * dpi.DpiScaleX, w.Top * dpi.DpiScaleY, w.Width * dpi.DpiScaleX, w.Height * dpi.DpiScaleY);
    }

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO { public int cbSize; public RECT rcMonitor; public RECT rcWork; public uint dwFlags; }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromRect(ref RECT lprc, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
}

/// <summary>
/// Attached properties that make an element's tooltip a <see cref="MaCalloutToolTip"/> while keeping a plain value
/// (usually a text binding) as its content (M.COMP-T167). Set <c>ctl:MaCallout.ToolTip</c> instead of
/// <c>ToolTip</c>; null or empty shows no tooltip. <c>ctl:MaCallout.Placement</c> picks the preferred side (Right).
/// <code>
/// &lt;Style TargetType="TabItem"&gt;
///     &lt;Setter Property="ctl:MaCallout.ToolTip" Value="{Binding TabHoverTip}" /&gt;
///     &lt;Setter Property="ctl:MaCallout.Placement" Value="Right" /&gt;
/// &lt;/Style&gt;
/// </code>
/// Each element gets its own callout, kept in step as the bound value changes (live, even while it's open).
/// </summary>
public static class MaCallout
{
    public static readonly DependencyProperty ToolTipProperty = DependencyProperty.RegisterAttached(
        "ToolTip", typeof(object), typeof(MaCallout), new PropertyMetadata(null, OnToolTipChanged));

    public static readonly DependencyProperty PlacementProperty = DependencyProperty.RegisterAttached(
        "Placement", typeof(MaCalloutSide), typeof(MaCallout), new PropertyMetadata(MaCalloutSide.Right, OnPlacementChanged));

    private static readonly DependencyProperty CalloutProperty = DependencyProperty.RegisterAttached(
        "Callout", typeof(MaCalloutToolTip), typeof(MaCallout), new PropertyMetadata(null));

    /// <summary>The callout's content (text, or anything a ToolTip can show).</summary>
    public static object? GetToolTip(DependencyObject element) => element.GetValue(ToolTipProperty);

    /// <summary>Sets the callout's content; null or an empty string removes the tooltip.</summary>
    public static void SetToolTip(DependencyObject element, object? value) => element.SetValue(ToolTipProperty, value);

    /// <summary>The preferred side (Right by default).</summary>
    public static MaCalloutSide GetPlacement(DependencyObject element) => (MaCalloutSide)element.GetValue(PlacementProperty);

    /// <summary>Sets the preferred side.</summary>
    public static void SetPlacement(DependencyObject element, MaCalloutSide value) => element.SetValue(PlacementProperty, value);

    /// <summary>The callout an element shows, or null if it has none yet.</summary>
    public static MaCalloutToolTip? GetCallout(DependencyObject element) => (MaCalloutToolTip?)element.GetValue(CalloutProperty);

    private static void OnToolTipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element) return;
        var callout = GetCallout(element);
        if (e.NewValue is null || e.NewValue is string { Length: 0 })
        {
            if (callout is not null && ReferenceEquals(element.ReadLocalValue(FrameworkElement.ToolTipProperty), callout))
                element.ClearValue(FrameworkElement.ToolTipProperty);
            if (callout is not null) callout.Content = null;
            return;
        }
        if (callout is null)
        {
            callout = new MaCalloutToolTip { PreferredSide = GetPlacement(element) };
            element.SetValue(CalloutProperty, callout);
        }
        callout.Content = e.NewValue;
        if (!ReferenceEquals(element.ReadLocalValue(FrameworkElement.ToolTipProperty), callout))
            element.ToolTip = callout;
    }

    private static void OnPlacementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (GetCallout(d) is { } callout) callout.PreferredSide = (MaCalloutSide)e.NewValue;
    }
}
