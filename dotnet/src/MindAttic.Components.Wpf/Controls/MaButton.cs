using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>When a <see cref="MaButton"/> shows its accent.</summary>
public enum MaAccentWhen
{
    /// <summary>The accent always shows.</summary>
    Always,
    /// <summary>The accent shows only while the button is checked.</summary>
    Checked,
    /// <summary>The accent never shows.</summary>
    Never,
}

/// <summary>
/// The one button. Every button, chip, tab, nav item, link and window-chrome button is an MaButton with
/// different property values; a new look is new values (or a Style that sets them), never a new template.
/// <list type="bullet">
/// <item>Frame: <see cref="Shape"/> (none, rectangle, rounded, pill, chamfer, trapezoid, parallelogram),
/// <see cref="Chamfer"/>, <see cref="CornerRadius"/>, <see cref="Slant"/>, <see cref="StrokeThickness"/>.</item>
/// <item>Colours per state: Background/BorderBrush/Foreground, then Hover*, Pressed*, Focused*, Checked*
/// overrides (unset means "keep the normal one"). Precedence: pressed, then checked, then focused, then hover.</item>
/// <item>Accent: <see cref="AccentBrush"/> on an <see cref="AccentEdge"/>, shown <see cref="AccentWhen"/>.</item>
/// <item>Behaviour: a plain button by default; <see cref="IsToggle"/> makes it a toggle, and a
/// <see cref="GroupName"/> makes it one of a set where exactly one is checked (tabs, nav).</item>
/// </list>
/// </summary>
public class MaButton : ToggleButton
{
    /// <summary>Identifies the <see cref="Shape"/> property.</summary>
    public static readonly DependencyProperty ShapeProperty = Visual(nameof(Shape), typeof(MaShape), MaShape.Rectangle);
    /// <summary>Identifies the <see cref="Chamfer"/> property.</summary>
    public static readonly DependencyProperty ChamferProperty = Visual(nameof(Chamfer), typeof(double), 8.0);
    /// <summary>Identifies the <see cref="CornerRadius"/> property.</summary>
    public static readonly DependencyProperty CornerRadiusProperty = Visual(nameof(CornerRadius), typeof(double), 4.0);
    /// <summary>Identifies the <see cref="Slant"/> property.</summary>
    public static readonly DependencyProperty SlantProperty = Visual(nameof(Slant), typeof(double), 12.0);
    /// <summary>Identifies the <see cref="StrokeThickness"/> property.</summary>
    public static readonly DependencyProperty StrokeThicknessProperty = Visual(nameof(StrokeThickness), typeof(double), 1.0);

    /// <summary>Identifies the <see cref="HoverBackground"/> property.</summary>
    public static readonly DependencyProperty HoverBackgroundProperty = State(nameof(HoverBackground));
    /// <summary>Identifies the <see cref="HoverBorderBrush"/> property.</summary>
    public static readonly DependencyProperty HoverBorderBrushProperty = State(nameof(HoverBorderBrush));
    /// <summary>Identifies the <see cref="HoverForeground"/> property.</summary>
    public static readonly DependencyProperty HoverForegroundProperty = State(nameof(HoverForeground));
    /// <summary>Identifies the <see cref="PressedBackground"/> property.</summary>
    public static readonly DependencyProperty PressedBackgroundProperty = State(nameof(PressedBackground));
    /// <summary>Identifies the <see cref="FocusedBorderBrush"/> property.</summary>
    public static readonly DependencyProperty FocusedBorderBrushProperty = State(nameof(FocusedBorderBrush));
    /// <summary>Identifies the <see cref="CheckedBackground"/> property.</summary>
    public static readonly DependencyProperty CheckedBackgroundProperty = State(nameof(CheckedBackground));
    /// <summary>Identifies the <see cref="CheckedBorderBrush"/> property.</summary>
    public static readonly DependencyProperty CheckedBorderBrushProperty = State(nameof(CheckedBorderBrush));
    /// <summary>Identifies the <see cref="CheckedForeground"/> property.</summary>
    public static readonly DependencyProperty CheckedForegroundProperty = State(nameof(CheckedForeground));

    /// <summary>Identifies the <see cref="AccentBrush"/> property.</summary>
    public static readonly DependencyProperty AccentBrushProperty = State(nameof(AccentBrush));
    /// <summary>Identifies the <see cref="AccentEdge"/> property.</summary>
    public static readonly DependencyProperty AccentEdgeProperty = Visual(nameof(AccentEdge), typeof(MaAccentEdge), MaAccentEdge.None);
    /// <summary>Identifies the <see cref="AccentWhen"/> property.</summary>
    public static readonly DependencyProperty AccentWhenProperty = Visual(nameof(AccentWhen), typeof(MaAccentWhen), MaAccentWhen.Always);
    /// <summary>Identifies the <see cref="AccentThickness"/> property.</summary>
    public static readonly DependencyProperty AccentThicknessProperty = Visual(nameof(AccentThickness), typeof(double), 2.0);

    /// <summary>Identifies the <see cref="DisabledOpacity"/> property.</summary>
    public static readonly DependencyProperty DisabledOpacityProperty = DependencyProperty.Register(
        nameof(DisabledOpacity), typeof(double), typeof(MaButton), new PropertyMetadata(0.38, (d, _) => ((MaButton)d).UpdateState()));

    /// <summary>Identifies the <see cref="IsToggle"/> property.</summary>
    public static readonly DependencyProperty IsToggleProperty = DependencyProperty.Register(
        nameof(IsToggle), typeof(bool), typeof(MaButton), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="GroupName"/> property.</summary>
    public static readonly DependencyProperty GroupNameProperty = DependencyProperty.Register(
        nameof(GroupName), typeof(string), typeof(MaButton), new PropertyMetadata(null, OnGroupNameChanged));

    /// <summary>Identifies the <see cref="IsDefault"/> property.</summary>
    public static readonly DependencyProperty IsDefaultProperty = DependencyProperty.Register(
        nameof(IsDefault), typeof(bool), typeof(MaButton), new PropertyMetadata(false, (d, e) => ((MaButton)d).SetAccessKey(EnterKey, (bool)e.NewValue)));

    /// <summary>Identifies the <see cref="IsCancel"/> property.</summary>
    public static readonly DependencyProperty IsCancelProperty = DependencyProperty.Register(
        nameof(IsCancel), typeof(bool), typeof(MaButton), new PropertyMetadata(false, (d, e) => ((MaButton)d).SetAccessKey(EscapeKey, (bool)e.NewValue)));

    private static readonly DependencyPropertyKey EffectiveBackgroundKey = Effective(nameof(EffectiveBackground));
    private static readonly DependencyPropertyKey EffectiveBorderBrushKey = Effective(nameof(EffectiveBorderBrush));
    private static readonly DependencyPropertyKey EffectiveForegroundKey = Effective(nameof(EffectiveForeground));
    private static readonly DependencyPropertyKey EffectiveAccentKey = Effective(nameof(EffectiveAccent));

    /// <summary>Identifies the <see cref="EffectiveBackground"/> property.</summary>
    public static readonly DependencyProperty EffectiveBackgroundProperty = EffectiveBackgroundKey.DependencyProperty;
    /// <summary>Identifies the <see cref="EffectiveBorderBrush"/> property.</summary>
    public static readonly DependencyProperty EffectiveBorderBrushProperty = EffectiveBorderBrushKey.DependencyProperty;
    /// <summary>Identifies the <see cref="EffectiveForeground"/> property.</summary>
    public static readonly DependencyProperty EffectiveForegroundProperty = EffectiveForegroundKey.DependencyProperty;
    /// <summary>Identifies the <see cref="EffectiveAccent"/> property.</summary>
    public static readonly DependencyProperty EffectiveAccentProperty = EffectiveAccentKey.DependencyProperty;

    private const string EnterKey = "\r";
    private const string EscapeKey = "\u001b";

    // One registry per UI thread: a button only ever groups with buttons on its own Dispatcher, and touching
    // another thread's button would throw.
    [ThreadStatic]
    private static Dictionary<string, List<WeakReference<MaButton>>>? t_groups;

    private static Dictionary<string, List<WeakReference<MaButton>>> Groups => t_groups ??= new(StringComparer.Ordinal);

    static MaButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MaButton), new FrameworkPropertyMetadata(typeof(MaButton)));

        // Foreground resolves to the current state's colour (coercion keeps the set value as the resting
        // colour underneath), so content that inherits its text colour, such as an icon, follows it too.
        ForegroundProperty.OverrideMetadata(typeof(MaButton), new FrameworkPropertyMetadata(null, CoerceForeground));
    }

    /// <summary>Creates a button with its state brushes resolved.</summary>
    public MaButton()
    {
        UpdateState();
    }

    /// <summary>The frame's outline.</summary>
    public MaShape Shape { get => (MaShape)GetValue(ShapeProperty); set => SetValue(ShapeProperty, value); }

    /// <summary>How deep the <see cref="MaShape.Chamfer"/> corners are cut.</summary>
    public double Chamfer { get => (double)GetValue(ChamferProperty); set => SetValue(ChamferProperty, value); }

    /// <summary>Identifies the <see cref="ChamferCorners"/> property.</summary>
    public static readonly DependencyProperty ChamferCornersProperty = Visual(nameof(ChamferCorners), typeof(MaCorners), MaCorners.Diagonal);

    /// <summary>Which corners a <see cref="MaShape.Chamfer"/> cuts; top-left and bottom-right by default.</summary>
    public MaCorners ChamferCorners { get => (MaCorners)GetValue(ChamferCornersProperty); set => SetValue(ChamferCornersProperty, value); }

    /// <summary>Corner radius for <see cref="MaShape.Rounded"/>.</summary>
    public double CornerRadius { get => (double)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    /// <summary>The horizontal inset of a <see cref="MaShape.Trapezoid"/> or <see cref="MaShape.Parallelogram"/> top edge.</summary>
    public double Slant { get => (double)GetValue(SlantProperty); set => SetValue(SlantProperty, value); }

    /// <summary>The border's thickness; 0 draws no border.</summary>
    public double StrokeThickness { get => (double)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }

    /// <summary>Background while the pointer is over the button; null keeps <c>Background</c>.</summary>
    public Brush? HoverBackground { get => (Brush?)GetValue(HoverBackgroundProperty); set => SetValue(HoverBackgroundProperty, value); }

    /// <summary>Border while the pointer is over the button; null keeps <c>BorderBrush</c>.</summary>
    public Brush? HoverBorderBrush { get => (Brush?)GetValue(HoverBorderBrushProperty); set => SetValue(HoverBorderBrushProperty, value); }

    /// <summary>Text colour while the pointer is over the button; null keeps <c>Foreground</c>.</summary>
    public Brush? HoverForeground { get => (Brush?)GetValue(HoverForegroundProperty); set => SetValue(HoverForegroundProperty, value); }

    /// <summary>Background while the button is held down.</summary>
    public Brush? PressedBackground { get => (Brush?)GetValue(PressedBackgroundProperty); set => SetValue(PressedBackgroundProperty, value); }

    /// <summary>Border while the button has keyboard focus.</summary>
    public Brush? FocusedBorderBrush { get => (Brush?)GetValue(FocusedBorderBrushProperty); set => SetValue(FocusedBorderBrushProperty, value); }

    /// <summary>Background while checked (toggles, tabs, nav items).</summary>
    public Brush? CheckedBackground { get => (Brush?)GetValue(CheckedBackgroundProperty); set => SetValue(CheckedBackgroundProperty, value); }

    /// <summary>Border while checked.</summary>
    public Brush? CheckedBorderBrush { get => (Brush?)GetValue(CheckedBorderBrushProperty); set => SetValue(CheckedBorderBrushProperty, value); }

    /// <summary>Text colour while checked.</summary>
    public Brush? CheckedForeground { get => (Brush?)GetValue(CheckedForegroundProperty); set => SetValue(CheckedForegroundProperty, value); }

    /// <summary>The accent's brush (a strip, bar or chamfer ticks, per <see cref="AccentEdge"/>).</summary>
    public Brush? AccentBrush { get => (Brush?)GetValue(AccentBrushProperty); set => SetValue(AccentBrushProperty, value); }

    /// <summary>Where the accent is drawn.</summary>
    public MaAccentEdge AccentEdge { get => (MaAccentEdge)GetValue(AccentEdgeProperty); set => SetValue(AccentEdgeProperty, value); }

    /// <summary>When the accent shows: always, only while checked, or never.</summary>
    public MaAccentWhen AccentWhen { get => (MaAccentWhen)GetValue(AccentWhenProperty); set => SetValue(AccentWhenProperty, value); }

    /// <summary>The accent's thickness in pixels.</summary>
    public double AccentThickness { get => (double)GetValue(AccentThicknessProperty); set => SetValue(AccentThicknessProperty, value); }

    /// <summary>Opacity while disabled (default 0.38).</summary>
    public double DisabledOpacity { get => (double)GetValue(DisabledOpacityProperty); set => SetValue(DisabledOpacityProperty, value); }

    /// <summary>False (default): a plain button that never stays checked. True: clicking toggles it.</summary>
    public bool IsToggle { get => (bool)GetValue(IsToggleProperty); set => SetValue(IsToggleProperty, value); }

    /// <summary>Buttons sharing a group name act as one choice: clicking one checks it and unchecks the rest.</summary>
    public string? GroupName { get => (string?)GetValue(GroupNameProperty); set => SetValue(GroupNameProperty, value); }

    /// <summary>Enter presses this button (a dialog's default action), as with <c>Button.IsDefault</c>.</summary>
    public bool IsDefault { get => (bool)GetValue(IsDefaultProperty); set => SetValue(IsDefaultProperty, value); }

    /// <summary>Esc presses this button (a dialog's cancel action), as with <c>Button.IsCancel</c>.</summary>
    public bool IsCancel { get => (bool)GetValue(IsCancelProperty); set => SetValue(IsCancelProperty, value); }

    /// <summary>The background for the current state (what the template draws).</summary>
    public Brush? EffectiveBackground => (Brush?)GetValue(EffectiveBackgroundProperty);

    /// <summary>The border for the current state (what the template draws).</summary>
    public Brush? EffectiveBorderBrush => (Brush?)GetValue(EffectiveBorderBrushProperty);

    /// <summary>The text colour for the current state (what the template draws).</summary>
    public Brush? EffectiveForeground => (Brush?)GetValue(EffectiveForegroundProperty);

    /// <summary>The accent brush when the accent should show right now, else null.</summary>
    public Brush? EffectiveAccent => (Brush?)GetValue(EffectiveAccentProperty);

    /// <inheritdoc/>
    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (_updating)
        {
            return;
        }

        if (e.Property == BackgroundProperty || e.Property == BorderBrushProperty || e.Property == ForegroundProperty
            || e.Property == IsMouseOverProperty || e.Property == IsPressedProperty || e.Property == IsCheckedProperty
            || e.Property == IsKeyboardFocusedProperty || e.Property == IsEnabledProperty)
        {
            UpdateState();
        }
    }

    /// <summary>
    /// Plain MaButtons are buttons to UI Automation and screen readers (Invoke); only toggles and grouped buttons
    /// (chips, tabs, nav items) also expose the Toggle pattern.
    /// </summary>
    protected override AutomationPeer OnCreateAutomationPeer() => new MaButtonAutomationPeer(this);

    /// <summary>True when a click changes <see cref="ToggleButton.IsChecked"/> (a toggle or a group member).</summary>
    internal bool Toggles => IsToggle || !string.IsNullOrEmpty(GroupName);

    /// <summary>Clicks the button for UI Automation, as a mouse click would.</summary>
    internal void AutomationClick() => OnClick();

    /// <inheritdoc/>
    protected override void OnToggle()
    {
        if (!string.IsNullOrEmpty(GroupName))
        {
            if (IsChecked != true)
            {
                SetCurrentValue(IsCheckedProperty, true);
            }

            return;
        }

        if (IsToggle)
        {
            base.OnToggle();
        }
    }

    /// <inheritdoc/>
    protected override void OnChecked(RoutedEventArgs e)
    {
        base.OnChecked(e);
        if (string.IsNullOrEmpty(GroupName) || !Groups.TryGetValue(GroupName, out var members))
        {
            return;
        }

        // Like RadioButton: a group is scoped to one window (or, before it's shown, one element tree).
        var scope = ScopeOf(this);
        members.RemoveAll(r => !r.TryGetTarget(out _));
        foreach (var weak in members.ToList())
        {
            if (weak.TryGetTarget(out var other) && !ReferenceEquals(other, this) && other.IsChecked == true
                && ReferenceEquals(ScopeOf(other), scope))
            {
                other.SetCurrentValue(IsCheckedProperty, false);
            }
        }
    }

    /// <summary>The window's root visual once shown; otherwise the top of the element's own tree.</summary>
    private static DependencyObject ScopeOf(MaButton button)
    {
        if (PresentationSource.FromVisual(button)?.RootVisual is { } root)
        {
            return root;
        }

        DependencyObject node = button;
        while (true)
        {
            var parent = (node is System.Windows.Media.Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(node) : null)
                ?? LogicalTreeHelper.GetParent(node);
            if (parent is null)
            {
                return node;
            }

            node = parent;
        }
    }

    private static void OnGroupNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var button = (MaButton)d;
        if (e.OldValue is string old && Groups.TryGetValue(old, out var previous))
        {
            previous.RemoveAll(r => !r.TryGetTarget(out var t) || ReferenceEquals(t, button));
        }

        if (e.NewValue is string name && name.Length > 0)
        {
            if (!Groups.TryGetValue(name, out var members))
            {
                Groups[name] = members = [];
            }

            members.Add(new WeakReference<MaButton>(button));
        }
    }

    private void SetAccessKey(string key, bool on)
    {
        if (on)
        {
            AccessKeyManager.Register(key, this);
        }
        else
        {
            AccessKeyManager.Unregister(key, this);
        }
    }

    private bool _updating;

    private void UpdateState()
    {
        if (_updating)
        {
            return;
        }

        _updating = true;
        try
        {
            UpdateStateCore();
        }
        finally
        {
            _updating = false;
        }
    }

    private void UpdateStateCore()
    {
        var on = IsChecked == true;
        var hover = IsMouseOver;
        SetValue(EffectiveBackgroundKey, Pick(IsPressed ? PressedBackground : null, on ? CheckedBackground : null, hover ? HoverBackground : null, Background));
        SetValue(EffectiveBorderBrushKey, Pick(on ? CheckedBorderBrush : null, IsKeyboardFocused ? FocusedBorderBrush : null, hover ? HoverBorderBrush : null, BorderBrush));
        CoerceValue(ForegroundProperty);
        SetValue(EffectiveForegroundKey, Foreground);
        SetValue(EffectiveAccentKey, AccentWhen switch
        {
            MaAccentWhen.Always => AccentBrush,
            MaAccentWhen.Checked when on => AccentBrush,
            _ => null,
        });
        SetCurrentValue(OpacityProperty, IsEnabled ? 1.0 : DisabledOpacity);
    }

    private static object CoerceForeground(DependencyObject d, object baseValue)
    {
        var b = (MaButton)d;
        var on = b.IsChecked == true;
        return Pick(on ? b.CheckedForeground : null, b.IsMouseOver ? b.HoverForeground : null, baseValue as Brush) ?? baseValue;
    }

    private static Brush? Pick(params Brush?[] choices) => choices.FirstOrDefault(c => c is not null);

    private static DependencyProperty Visual(string name, Type type, object fallback) =>
        DependencyProperty.Register(name, type, typeof(MaButton), new FrameworkPropertyMetadata(fallback, (d, _) => ((MaButton)d).UpdateState()));

    private static DependencyProperty State(string name) =>
        DependencyProperty.Register(name, typeof(Brush), typeof(MaButton), new FrameworkPropertyMetadata(null, (d, _) => ((MaButton)d).UpdateState()));

    private static DependencyPropertyKey Effective(string name) =>
        DependencyProperty.RegisterReadOnly(name, typeof(Brush), typeof(MaButton), new FrameworkPropertyMetadata(null));
}

/// <summary>UI Automation peer for <see cref="MaButton"/>: Invoke always, Toggle only for toggles and groups.</summary>
public sealed class MaButtonAutomationPeer(MaButton owner) : ToggleButtonAutomationPeer(owner), IInvokeProvider
{
    private MaButton Button => (MaButton)Owner;

    /// <inheritdoc/>
    public override object? GetPattern(PatternInterface patternInterface) => patternInterface switch
    {
        PatternInterface.Invoke => this,
        PatternInterface.Toggle => Button.Toggles ? base.GetPattern(patternInterface) : null,
        _ => base.GetPattern(patternInterface),
    };

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(MaButton);

    /// <inheritdoc/>
    public void Invoke()
    {
        if (!IsEnabled())
        {
            throw new ElementNotEnabledException();
        }

        // Like ButtonAutomationPeer: click asynchronously so the automation client isn't blocked by handlers.
        Button.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(Button.AutomationClick));
    }
}
