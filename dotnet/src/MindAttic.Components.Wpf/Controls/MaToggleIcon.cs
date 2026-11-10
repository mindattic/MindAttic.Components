using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// A two-state icon button (M.COMP-T183, was MaThemeToggle from M.COMP-T53): it shows <see cref="OffGlyph"/> while
/// <see cref="IsChecked"/> is false and <see cref="OnGlyph"/> while it's true, and a click (or Space / Enter while focused)
/// flips it and raises <see cref="Toggled"/>. <see cref="OffText"/> / <see cref="OnText"/> say what a click does in each state;
/// the shown one (<see cref="Text"/>) is its accessible name and default tooltip.
/// <code>
/// &lt;ctl:MaToggleIcon IsChecked="{Binding PhoneLayout}" OffGlyph="&amp;#xE7F8;" OnGlyph="&amp;#xE8EA;"
///                   OffText="Switch to phone layout" OnText="Switch to desktop layout" /&gt;
/// &lt;ctl:MaToggleIcon IsThemeToggle="True" ThemeChanged="OnThemeChanged" /&gt;   // the light/dark switch
/// </code>
/// <para>
/// With <see cref="IsThemeToggle"/> it's the light/dark switch: checked means the light theme is in force (read from the
/// <c>MaThemeMode</c> resource, however it was set), it shows a sun in dark mode and a moon in light mode, and a click switches
/// the theme through <see cref="MaTheme.Set(ResourceDictionary, MaThemeMode)"/> on the nearest dictionary carrying it
/// (<see cref="MaTheme.ScopeOf"/>) and raises <see cref="ThemeChanged"/>. Its look is the implicit <c>MaToggleIcon</c> style in
/// Theme.xaml: an <c>MaToolButton</c>.
/// </para>
/// </summary>
public class MaToggleIcon : Button
{
    /// <summary>Segoe Fluent Icons / MDL2 glyphs: the sun (shown in dark mode) and the moon (shown in light mode).</summary>
    public const string SunGlyph = "", MoonGlyph = "";

    public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
        nameof(IsChecked), typeof(bool), typeof(MaToggleIcon),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnShownChanged));

    public static readonly DependencyProperty OffGlyphProperty = DependencyProperty.Register(
        nameof(OffGlyph), typeof(string), typeof(MaToggleIcon), new FrameworkPropertyMetadata("", OnShownChanged));

    public static readonly DependencyProperty OnGlyphProperty = DependencyProperty.Register(
        nameof(OnGlyph), typeof(string), typeof(MaToggleIcon), new FrameworkPropertyMetadata("", OnShownChanged));

    public static readonly DependencyProperty OffTextProperty = DependencyProperty.Register(
        nameof(OffText), typeof(string), typeof(MaToggleIcon), new FrameworkPropertyMetadata("", OnShownChanged));

    public static readonly DependencyProperty OnTextProperty = DependencyProperty.Register(
        nameof(OnText), typeof(string), typeof(MaToggleIcon), new FrameworkPropertyMetadata("", OnShownChanged));

    public static readonly DependencyProperty IsThemeToggleProperty = DependencyProperty.Register(
        nameof(IsThemeToggle), typeof(bool), typeof(MaToggleIcon), new FrameworkPropertyMetadata(false, OnIsThemeToggleChanged));

    private static readonly DependencyPropertyKey GlyphPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Glyph), typeof(string), typeof(MaToggleIcon), new FrameworkPropertyMetadata(""));

    /// <summary>The read-only <see cref="Glyph"/> property.</summary>
    public static readonly DependencyProperty GlyphProperty = GlyphPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey TextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Text), typeof(string), typeof(MaToggleIcon), new FrameworkPropertyMetadata(""));

    /// <summary>The read-only <see cref="Text"/> property.</summary>
    public static readonly DependencyProperty TextProperty = TextPropertyKey.DependencyProperty;

    // With IsThemeToggle, follows the MaThemeMode resource.
    private static readonly DependencyProperty ResourceThemeProperty = DependencyProperty.Register(
        "ResourceTheme", typeof(object), typeof(MaToggleIcon), new FrameworkPropertyMetadata(null, OnResourceThemeChanged));

    /// <summary>Raised (bubbling) after a click has flipped <see cref="IsChecked"/>.</summary>
    public static readonly RoutedEvent ToggledEvent = EventManager.RegisterRoutedEvent(
        nameof(Toggled), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MaToggleIcon));

    /// <summary>Raised (bubbling) after a click has switched the theme (<see cref="IsThemeToggle"/> only):
    /// <see cref="MaThemeChangedEventArgs.Theme"/> is the new one, to save.</summary>
    public static readonly RoutedEvent ThemeChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ThemeChanged), RoutingStrategy.Bubble, typeof(EventHandler<MaThemeChangedEventArgs>), typeof(MaToggleIcon));

    public bool IsChecked { get => (bool)GetValue(IsCheckedProperty); set => SetValue(IsCheckedProperty, value); }

    /// <summary>The icon while unchecked.</summary>
    public string OffGlyph { get => (string)GetValue(OffGlyphProperty); set => SetValue(OffGlyphProperty, value); }

    /// <summary>The icon while checked.</summary>
    public string OnGlyph { get => (string)GetValue(OnGlyphProperty); set => SetValue(OnGlyphProperty, value); }

    /// <summary>What a click does while unchecked ("Switch to phone layout").</summary>
    public string OffText { get => (string)GetValue(OffTextProperty); set => SetValue(OffTextProperty, value); }

    /// <summary>What a click does while checked ("Switch to desktop layout").</summary>
    public string OnText { get => (string)GetValue(OnTextProperty); set => SetValue(OnTextProperty, value); }

    /// <summary>The light/dark switch: <see cref="IsChecked"/> follows the theme (checked = light) and a click switches it.</summary>
    public bool IsThemeToggle { get => (bool)GetValue(IsThemeToggleProperty); set => SetValue(IsThemeToggleProperty, value); }

    /// <summary>The icon shown now.</summary>
    public string Glyph => (string)GetValue(GlyphProperty);

    /// <summary>What a click does now: the accessible name and default tooltip.</summary>
    public string Text => (string)GetValue(TextProperty);

    /// <summary>The theme in force (<see cref="IsThemeToggle"/>): Light when checked, else Dark.</summary>
    public MaThemeMode Theme => IsChecked ? MaThemeMode.Light : MaThemeMode.Dark;

    /// <summary>The theme a click switches to: the other one.</summary>
    public MaThemeMode TargetTheme => MaTheme.Opposite(Theme);

    public event RoutedEventHandler Toggled
    {
        add => AddHandler(ToggledEvent, value);
        remove => RemoveHandler(ToggledEvent, value);
    }

    public event EventHandler<MaThemeChangedEventArgs> ThemeChanged
    {
        add => AddHandler(ThemeChangedEvent, value);
        remove => RemoveHandler(ThemeChangedEvent, value);
    }

    /// <summary>What the theme switch says while <paramref name="target"/> is the theme it would switch to.</summary>
    public static string Describe(MaThemeMode target) => target == MaThemeMode.Light ? "Switch to light theme" : "Switch to dark theme";

    /// <summary>Flips <see cref="IsChecked"/> (switching the theme when <see cref="IsThemeToggle"/>), raises <see cref="Toggled"/>,
    /// then Click and the Command as any button does.</summary>
    protected override void OnClick()
    {
        if (IsThemeToggle)
        {
            if (MaTheme.ScopeOf(this) is { } scope)
            {
                var next = TargetTheme;
                MaTheme.Set(scope, next);
                SetCurrentValue(IsCheckedProperty, next == MaThemeMode.Light); // at once, before the resource change arrives
                RaiseEvent(new RoutedEventArgs(ToggledEvent, this));
                RaiseEvent(new MaThemeChangedEventArgs(scope, next) { RoutedEvent = ThemeChangedEvent, Source = this });
            }
        }
        else
        {
            SetCurrentValue(IsCheckedProperty, !IsChecked);
            RaiseEvent(new RoutedEventArgs(ToggledEvent, this));
        }
        base.OnClick();
    }

    private static void OnShownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((MaToggleIcon)d).Show();

    private static void OnIsThemeToggleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var toggle = (MaToggleIcon)d;
        if ((bool)e.NewValue)
        {
            toggle.SetCurrentValue(OffGlyphProperty, SunGlyph);
            toggle.SetCurrentValue(OnGlyphProperty, MoonGlyph);
            toggle.SetCurrentValue(OffTextProperty, Describe(MaThemeMode.Light));
            toggle.SetCurrentValue(OnTextProperty, Describe(MaThemeMode.Dark));
            toggle.SetResourceReference(ResourceThemeProperty, MaTheme.ModeKey);
        }
        else BindingOperations.ClearBinding(toggle, ResourceThemeProperty);
    }

    private static void OnResourceThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var toggle = (MaToggleIcon)d;
        if (toggle.IsThemeToggle) toggle.SetCurrentValue(IsCheckedProperty, e.NewValue is MaThemeMode.Light);
    }

    private void Show()
    {
        SetValue(GlyphPropertyKey, IsChecked ? OnGlyph : OffGlyph);
        SetValue(TextPropertyKey, IsChecked ? OnText : OffText);
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new MaToggleIconAutomationPeer(this);

    /// <summary>A button named for what a click does ("Switch to dark theme"), not its glyph; <c>AutomationProperties.Name</c>
    /// wins. A theme switch's help text names the theme in force.</summary>
    private sealed class MaToggleIconAutomationPeer(MaToggleIcon owner) : ButtonAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(MaToggleIcon);

        protected override string GetNameCore()
        {
            var name = AutomationProperties.GetName(Owner);
            return string.IsNullOrEmpty(name) ? ((MaToggleIcon)Owner).Text : name;
        }

        protected override string GetHelpTextCore()
        {
            var help = AutomationProperties.GetHelpText(Owner);
            var toggle = (MaToggleIcon)Owner;
            return !string.IsNullOrEmpty(help) ? help : toggle.IsThemeToggle ? $"{toggle.Theme} theme is on" : base.GetHelpTextCore();
        }
    }
}
