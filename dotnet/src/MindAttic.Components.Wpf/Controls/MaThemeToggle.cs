using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// The light/dark switch (M.COMP-T53): a small icon button that shows a moon while the light theme is on and a sun
/// while the dark one is, and switches to the other theme when clicked (or Space / Enter while focused), through
/// <see cref="MaTheme.Set(ResourceDictionary, MaThemeMode)"/> on the nearest dictionary carrying the theme
/// (<see cref="MaTheme.ScopeOf"/>, usually the Application's). It reads <see cref="Theme"/> from the
/// <c>MaThemeMode</c> resource, so it always shows the theme in force, however it was set.
/// <code>
/// &lt;ctl:MaThemeToggle ThemeChanged="OnThemeChanged" /&gt;   // OnThemeChanged: settings.Theme = e.Theme.ToString()
/// </code>
/// <para>
/// Its accessible name and tooltip say what it does: "Switch to light theme" / "Switch to dark theme"
/// (<see cref="SwitchText"/>); <c>AutomationProperties.Name</c> or a set <c>ToolTip</c> win. Its look is the implicit
/// <c>MaThemeToggle</c> style in Theme.xaml: an <c>MaToolButton</c>.
/// </para>
/// </summary>
public class MaThemeToggle : Button
{
    /// <summary>Segoe Fluent Icons / MDL2 glyphs: the sun (shown in dark mode) and the moon (shown in light mode).</summary>
    public const string SunGlyph = "", MoonGlyph = "";

    // Follows the MaThemeMode resource; drives the read-only public properties below.
    private static readonly DependencyProperty ResourceThemeProperty = DependencyProperty.Register(
        "ResourceTheme", typeof(object), typeof(MaThemeToggle), new FrameworkPropertyMetadata(null, OnResourceThemeChanged));

    private static readonly DependencyPropertyKey ThemePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Theme), typeof(MaThemeMode), typeof(MaThemeToggle), new FrameworkPropertyMetadata(MaThemeMode.Dark));

    /// <summary>The read-only <see cref="Theme"/> property.</summary>
    public static readonly DependencyProperty ThemeProperty = ThemePropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey TargetThemePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(TargetTheme), typeof(MaThemeMode), typeof(MaThemeToggle), new FrameworkPropertyMetadata(MaThemeMode.Light));

    /// <summary>The read-only <see cref="TargetTheme"/> property.</summary>
    public static readonly DependencyProperty TargetThemeProperty = TargetThemePropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey GlyphPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Glyph), typeof(string), typeof(MaThemeToggle), new FrameworkPropertyMetadata(SunGlyph));

    /// <summary>The read-only <see cref="Glyph"/> property.</summary>
    public static readonly DependencyProperty GlyphProperty = GlyphPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey SwitchTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(SwitchText), typeof(string), typeof(MaThemeToggle), new FrameworkPropertyMetadata(Describe(MaThemeMode.Light)));

    /// <summary>The read-only <see cref="SwitchText"/> property.</summary>
    public static readonly DependencyProperty SwitchTextProperty = SwitchTextPropertyKey.DependencyProperty;

    /// <summary>Raised (bubbling) after a click has switched the theme: <see cref="MaThemeChangedEventArgs.Theme"/> is
    /// the new one, to save.</summary>
    public static readonly RoutedEvent ThemeChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ThemeChanged), RoutingStrategy.Bubble, typeof(EventHandler<MaThemeChangedEventArgs>), typeof(MaThemeToggle));

    /// <summary>Follows the theme resource from the start.</summary>
    public MaThemeToggle() => SetResourceReference(ResourceThemeProperty, MaTheme.ModeKey);

    /// <summary>The theme in force where the toggle is (Dark when no theme is merged).</summary>
    public MaThemeMode Theme => (MaThemeMode)GetValue(ThemeProperty);

    /// <summary>The theme a click switches to: the other one.</summary>
    public MaThemeMode TargetTheme => (MaThemeMode)GetValue(TargetThemeProperty);

    /// <summary>The icon shown: <see cref="SunGlyph"/> in dark mode, <see cref="MoonGlyph"/> in light mode.</summary>
    public string Glyph => (string)GetValue(GlyphProperty);

    /// <summary>"Switch to light theme" or "Switch to dark theme": the accessible name and default tooltip.</summary>
    public string SwitchText => (string)GetValue(SwitchTextProperty);

    /// <summary>After a click has switched the theme.</summary>
    public event EventHandler<MaThemeChangedEventArgs> ThemeChanged
    {
        add => AddHandler(ThemeChangedEvent, value);
        remove => RemoveHandler(ThemeChangedEvent, value);
    }

    /// <summary>What the toggle says while <paramref name="target"/> is the theme it would switch to.</summary>
    public static string Describe(MaThemeMode target) => target == MaThemeMode.Light ? "Switch to light theme" : "Switch to dark theme";

    /// <summary>Switches the theme, raises <see cref="ThemeChanged"/>, then Click and the Command as any button does.</summary>
    protected override void OnClick()
    {
        if (MaTheme.ScopeOf(this) is { } scope)
        {
            var next = TargetTheme;
            MaTheme.Set(scope, next);
            Show(next); // at once, even before the resource change reaches the toggle
            RaiseEvent(new MaThemeChangedEventArgs(scope, next) { RoutedEvent = ThemeChangedEvent, Source = this });
        }
        base.OnClick();
    }

    private static void OnResourceThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((MaThemeToggle)d).Show(e.NewValue is MaThemeMode theme ? theme : MaThemeMode.Dark);

    private void Show(MaThemeMode theme)
    {
        var target = MaTheme.Opposite(theme);
        SetValue(ThemePropertyKey, theme);
        SetValue(TargetThemePropertyKey, target);
        SetValue(GlyphPropertyKey, theme == MaThemeMode.Light ? MoonGlyph : SunGlyph);
        SetValue(SwitchTextPropertyKey, Describe(target));
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new MaThemeToggleAutomationPeer(this);

    /// <summary>A button named for what it does ("Switch to dark theme"), not its glyph; <c>AutomationProperties.Name</c>
    /// wins. Its help text names the theme in force.</summary>
    private sealed class MaThemeToggleAutomationPeer(MaThemeToggle owner) : ButtonAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(MaThemeToggle);

        protected override string GetNameCore()
        {
            var name = AutomationProperties.GetName(Owner);
            return string.IsNullOrEmpty(name) ? ((MaThemeToggle)Owner).SwitchText : name;
        }

        protected override string GetHelpTextCore()
        {
            var help = AutomationProperties.GetHelpText(Owner);
            return string.IsNullOrEmpty(help) ? $"{((MaThemeToggle)Owner).Theme} theme is on" : help;
        }
    }
}
