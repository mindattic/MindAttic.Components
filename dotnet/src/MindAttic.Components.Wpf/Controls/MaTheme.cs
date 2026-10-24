using System.Windows;
using System.Windows.Media;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>The library's two palettes. <see cref="Dark"/> is the default (Theme.xaml on its own).</summary>
public enum MaThemeMode
{
    /// <summary>The dark chrome palette: Theme.xaml's own brushes.</summary>
    Dark,

    /// <summary>The light palette: Themes/Light.xaml merged over Theme.xaml.</summary>
    Light,
}

/// <summary>Raised by <see cref="MaTheme.Changed"/> and <see cref="MaThemeToggle.ThemeChanged"/> once a theme switch
/// has been applied: the new <see cref="Theme"/> and the dictionary it was applied to.</summary>
public sealed class MaThemeChangedEventArgs(ResourceDictionary resources, MaThemeMode theme) : RoutedEventArgs
{
    /// <summary>The theme now in force.</summary>
    public MaThemeMode Theme { get; } = theme;

    /// <summary>The dictionary that was switched (usually the Application's resources).</summary>
    public ResourceDictionary Resources { get; } = resources;
}

/// <summary>
/// Light and dark themes, switched live (M.COMP-T53). Theme.xaml is the dark palette and stays merged;
/// <see cref="Set(MaThemeMode)"/> with <see cref="MaThemeMode.Light"/> merges Themes/Light.xaml (the same keys, light
/// colours, the same contrast standard) just after it, and <see cref="MaThemeMode.Dark"/> takes it back out. Every style
/// refers to the palette as DynamicResource, so every control, the tab strip, picker, charts and toasts follow at once,
/// with no restart. An app's own re-keyed brushes (the dictionary's own entries) still win over either palette, and so
/// does <see cref="MaHighContrast"/>, which writes those entries: while Windows high contrast is on, the system colours
/// show whichever theme is set, and the chosen theme returns when it's turned off.
/// <code>
/// MaTheme.Set(settings.Theme == "Light" ? MaThemeMode.Light : MaThemeMode.Dark); // at startup, before or after the window
/// settings.Theme = MaTheme.Current.ToString();                                  // to save it
/// </code>
/// The theme in force is also the resource <c>MaThemeMode</c> (<see cref="ModeKey"/>), so anything can follow it
/// with <c>{DynamicResource MaThemeMode}</c> or <see cref="Get(FrameworkElement)"/>.
/// </summary>
public static class MaTheme
{
    /// <summary>The resource key whose value is the <see cref="MaThemeMode"/> in force where it's looked up.</summary>
    public const string ModeKey = "MaThemeMode";

    // Touching the helper registers the pack:// scheme. Field initializers run in declaration order as part of this
    // type's own static constructor, so this has to come before LightPaletteUri's: otherwise the first type (tests,
    // tools) to touch MaTheme with no WPF Application yet registered throws UriFormatException building that Uri
    // (M.COMP-T54; MindAttic.Orchestrator carried its own copy of this fix in Palette for M.ORCH-T703).
    private static readonly string PackSchemeRegistered = System.IO.Packaging.PackUriHelper.UriSchemePack;

    /// <summary>The light palette dictionary.</summary>
    public static Uri LightPaletteUri { get; } = new("pack://application:,,,/MindAttic.Components.Wpf;component/Themes/Light.xaml");

    private const string ThemeSuffix = "component/Theme.xaml", LightSuffix = "component/Themes/Light.xaml";

    /// <summary>Raised after any <see cref="Set(ResourceDictionary, MaThemeMode)"/> that changed the theme.</summary>
    public static event EventHandler<MaThemeChangedEventArgs>? Changed;

    /// <summary>The Application's theme (<see cref="MaThemeMode.Dark"/> when there's no Application): what to save.</summary>
    public static MaThemeMode Current => Application.Current is { } app ? Get(app.Resources) : MaThemeMode.Dark;

    /// <summary>Switches the Application's resources to <paramref name="theme"/>.</summary>
    public static void Set(MaThemeMode theme) =>
        Set((Application.Current ?? throw new InvalidOperationException("No Application: pass the resource dictionary to switch.")).Resources, theme);

    /// <summary>Switches <paramref name="resources"/> (the dictionary Theme.xaml is merged into: an Application's or a
    /// window's) to <paramref name="theme"/>, live. Setting the theme it already has does nothing.</summary>
    public static void Set(ResourceDictionary resources, MaThemeMode theme)
    {
        ArgumentNullException.ThrowIfNull(resources);
        if (Get(resources) == theme) return;
        var merged = resources.MergedDictionaries;
        if (theme == MaThemeMode.Light)
        {
            var light = new ResourceDictionary { Source = LightPaletteUri };
            // Just after Theme.xaml, so an app dictionary merged after the theme still wins over the light palette.
            var at = -1;
            for (var i = 0; i < merged.Count; i++) if (Holds(merged[i], ThemeSuffix)) at = i;
            merged.Insert(at < 0 ? merged.Count : at + 1, light);
        }
        else
        {
            for (var i = merged.Count - 1; i >= 0; i--) if (Is(merged[i], LightSuffix)) merged.RemoveAt(i);
        }
        Changed?.Invoke(null, new MaThemeChangedEventArgs(resources, theme));
    }

    /// <summary>Switches the Application to the other theme and returns it.</summary>
    public static MaThemeMode Toggle()
    {
        var next = Opposite(Current);
        Set(next);
        return next;
    }

    /// <summary>The theme of <paramref name="resources"/>: Light when the light palette is merged into it (at any depth).</summary>
    public static MaThemeMode Get(ResourceDictionary resources) => Holds(resources, LightSuffix) ? MaThemeMode.Light : MaThemeMode.Dark;

    /// <summary>The theme in force at <paramref name="element"/> (the <c>MaThemeMode</c> resource it sees).</summary>
    public static MaThemeMode Get(FrameworkElement element) =>
        element.TryFindResource(ModeKey) is MaThemeMode theme ? theme : MaThemeMode.Dark;

    /// <summary>The other theme.</summary>
    public static MaThemeMode Opposite(MaThemeMode theme) => theme == MaThemeMode.Light ? MaThemeMode.Dark : MaThemeMode.Light;

    /// <summary>A saved setting ("Light", "dark", ...) as a theme; anything else (null, empty, unknown) is
    /// <see cref="MaThemeMode.Dark"/>, the default.</summary>
    public static MaThemeMode Parse(string? saved) =>
        Enum.TryParse<MaThemeMode>(saved?.Trim(), ignoreCase: true, out var theme) && Enum.IsDefined(theme) ? theme : MaThemeMode.Dark;

    /// <summary>The dictionary a theme switch made from <paramref name="element"/> should change: the nearest one up its
    /// tree that carries the theme (Theme.xaml or the light palette merged in), else the Application's, else its window's.</summary>
    public static ResourceDictionary? ScopeOf(DependencyObject element)
    {
        for (DependencyObject? at = element; at is not null; at = Parent(at))
            if (at is FrameworkElement { Resources: { } r } && (Holds(r, ThemeSuffix) || Holds(r, LightSuffix))) return r;
        if (Application.Current is { } app) return app.Resources;
        return Window.GetWindow(element)?.Resources;
    }

    private static DependencyObject? Parent(DependencyObject d) =>
        (d is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(d) : null) ?? LogicalTreeHelper.GetParent(d);

    private static bool Is(ResourceDictionary d, string suffix) =>
        d.Source?.OriginalString.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) == true;

    private static bool Holds(ResourceDictionary d, string suffix) => Is(d, suffix) || d.MergedDictionaries.Any(m => Holds(m, suffix));
}
