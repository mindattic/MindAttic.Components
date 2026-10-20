using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// A busy spinner: a dashed ring that keeps turning while the UI thread is blocked (M.COMP-T33, lifted from the
/// Orchestrator's loader spinner, M.ORCH-T200). WPF ticks an animation on the thread that owns it, so a storyboard
/// spinner freezes exactly when it matters: while the window's thread applies a big result or lays out a heavy page
/// behind a loader. This one draws into a <see cref="HostVisual"/> whose content lives on a private dispatcher thread,
/// which the render thread composes however busy the window's thread is. That thread runs only while the spinner is
/// loaded and visible, and shuts down when it's hidden or unloaded.
/// <code>
/// &lt;ctl:McSpinner Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibility}}" /&gt;
/// &lt;ctl:McSpinner Width="24" Height="24" StrokeThickness="3" /&gt;
/// </code>
/// <para>
/// The implicit <c>McSpinner</c> style in Theme.xaml draws the ring in <c>McAccent</c>; set <see cref="Stroke"/> for
/// another colour. It is 44x44 unless <c>Width</c>/<c>Height</c> say otherwise (the ring fits the smaller side). The
/// ring's look is fixed when it starts turning; changing <see cref="Stroke"/>, <see cref="StrokeThickness"/>,
/// <see cref="Period"/> or the size while it turns restarts it. It reads to UI Automation as a progress bar named
/// "Loading" (or its <c>AutomationProperties.Name</c>), and is never a tab stop.
/// </para>
/// </summary>
public sealed class McSpinner : FrameworkElement
{
    /// <summary>The size used when <c>Width</c>/<c>Height</c> aren't set.</summary>
    public const double DefaultSize = 44;

    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        nameof(Stroke), typeof(Brush), typeof(McSpinner),
        new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender, Restart));

    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
        nameof(StrokeThickness), typeof(double), typeof(McSpinner),
        new FrameworkPropertyMetadata(4.0, FrameworkPropertyMetadataOptions.AffectsRender, Restart));

    public static readonly DependencyProperty PeriodProperty = DependencyProperty.Register(
        nameof(Period), typeof(Duration), typeof(McSpinner),
        new FrameworkPropertyMetadata(new Duration(TimeSpan.FromSeconds(1.2)), Restart));

    static McSpinner()
    {
        WidthProperty.OverrideMetadata(typeof(McSpinner), new FrameworkPropertyMetadata(double.NaN, Restart));
        HeightProperty.OverrideMetadata(typeof(McSpinner), new FrameworkPropertyMetadata(double.NaN, Restart));
    }

    /// <summary>The ring's colour; <c>McAccent</c> under the theme.</summary>
    public Brush Stroke { get => (Brush)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>The ring's line width; 4 by default.</summary>
    public double StrokeThickness { get => (double)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }

    /// <summary>One full turn; 1.2 seconds by default.</summary>
    public Duration Period { get => (Duration)GetValue(PeriodProperty); set => SetValue(PeriodProperty, value); }

    private HostVisual? host;
    private Dispatcher? worker;
    private RotateTransform? turn; // created on, and only touched from, the worker thread

    public McSpinner()
    {
        IsVisibleChanged += (_, _) => { if (IsVisible) Start(); else Stop(); };
        Unloaded += (_, _) => Stop();
        Loaded += (_, _) => { if (IsVisible) Start(); };
    }

    /// <summary>Whether the ring's own thread is up (the spinner is loaded and visible); for tests and diagnostics.</summary>
    public bool IsSpinning => worker is not null;

    /// <summary>The thread the ring turns on, null while it isn't spinning; for tests and diagnostics.</summary>
    public Thread? SpinnerThread => worker?.Thread;

    /// <summary>The ring's current angle in degrees, read on its own thread; null while it isn't spinning. For tests
    /// and diagnostics.</summary>
    public double? Angle => worker?.Invoke(() => turn?.Angle);

    protected override int VisualChildrenCount => host is null ? 0 : 1;

    protected override Visual GetVisualChild(int index) =>
        index == 0 && host is not null ? host : throw new ArgumentOutOfRangeException(nameof(index));

    protected override Size MeasureOverride(Size availableSize) =>
        new(double.IsNaN(Width) ? DefaultSize : Width, double.IsNaN(Height) ? DefaultSize : Height);

    protected override AutomationPeer OnCreateAutomationPeer() => new McSpinnerAutomationPeer(this);

    private static void Restart(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var spinner = (McSpinner)d;
        if (!spinner.IsSpinning) return;
        spinner.Stop();
        spinner.Start();
    }

    private void Start()
    {
        if (worker is not null || !IsLoaded || !IsVisible) return;
        double size = Math.Min(double.IsNaN(Width) ? DefaultSize : Width, double.IsNaN(Height) ? DefaultSize : Height);
        if (double.IsNaN(size) || size <= 0) size = DefaultSize;
        var thickness = Math.Max(0, StrokeThickness);
        var stroke = (Stroke ?? Brushes.Gray).CloneCurrentValue();
        stroke.Freeze(); // crosses to the worker thread
        var period = Period.HasTimeSpan && Period.TimeSpan > TimeSpan.Zero ? Period : new Duration(TimeSpan.FromSeconds(1.2));

        host = new HostVisual();
        AddVisualChild(host);
        var target = host;
        using var ready = new ManualResetEventSlim();
        Dispatcher? started = null;
        var thread = new Thread(() =>
        {
            started = Dispatcher.CurrentDispatcher;
            var source = new VisualTarget(target);
            source.RootVisual = Ring(size, thickness, stroke, period, out turn);
            ready.Set();
            Dispatcher.Run();
            source.Dispose();
        }) { IsBackground = true, Name = "McSpinner" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        ready.Wait();
        worker = started;
    }

    private void Stop()
    {
        worker?.BeginInvokeShutdown(DispatcherPriority.Send);
        worker = null;
        turn = null;
        if (host is not null) RemoveVisualChild(host);
        host = null;
    }

    /// <summary>The ring itself, built and animated on the worker thread: a dashed circle turned about its centre.</summary>
    private static DrawingVisual Ring(double size, double thickness, Brush stroke, Duration period, out RotateTransform turn)
    {
        var pen = new Pen(stroke, thickness) { DashStyle = new DashStyle([6, 4], 0), DashCap = PenLineCap.Round };
        pen.Freeze();
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            var r = Math.Max(0, (size - thickness) / 2);
            dc.DrawEllipse(null, pen, new Point(size / 2, size / 2), r, r);
        }
        turn = new RotateTransform(0, size / 2, size / 2);
        visual.Transform = turn;
        turn.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, 360, period) { RepeatBehavior = RepeatBehavior.Forever });
        return visual;
    }

    /// <summary>Reads as an indeterminate progress bar named "Loading" unless <c>AutomationProperties.Name</c> says
    /// otherwise.</summary>
    private sealed class McSpinnerAutomationPeer(McSpinner owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(McSpinner);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ProgressBar;

        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrEmpty(name) ? "Loading" : name;
        }

        protected override bool IsKeyboardFocusableCore() => false;
    }
}
