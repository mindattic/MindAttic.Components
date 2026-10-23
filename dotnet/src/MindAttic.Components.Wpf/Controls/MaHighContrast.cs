using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// Windows high-contrast themes (WCAG 1.4.11 and the OS's own contrast setting): while one is on, every theme brush is
/// replaced, in the dictionary given (the Application's resources, which win over the merged Theme.xaml), by the user's
/// system colour for its role: backgrounds by Window, text and lines by WindowText, the accent and focus by Highlight
/// and text on the accent by HighlightText. Turned off, the replacements are removed and anything they covered is put
/// back. The theme's styles use DynamicResource, so every library control follows live, with no restart. An app adds
/// its own brush keys through the optional lists. It wins over either <see cref="MaTheme"/> palette, which are merged
/// dictionaries under these entries (M.COMP-T53). Lifted from Workflow Desktop's Theme/HighContrast (M.COMP-T11).
/// </summary>
public static class MaHighContrast
{
    /// <summary>The theme's background brushes (filled with the system Window colour).</summary>
    public static IReadOnlyList<string> Backgrounds { get; } = ["MaChrome", "MaSurface", "MaHover", "MaHoverStrong", "MaAccentDisabled"];

    /// <summary>The theme's text, line and status brushes (WindowText).</summary>
    public static IReadOnlyList<string> Foregrounds { get; } =
    [
        "MaText", "MaMuted", "MaRec", "MaStar", "MaChromeBorder", "MaControlBorder", "MaFieldBorder",
        "MaGood", "MaWarn", "MaBad", "MaOther", "MaLink", "MaChartGrid", "MaDisabledText", "MaOnAccentDisabled",
    ];

    /// <summary>The theme's accent and focus brushes (Highlight).</summary>
    public static IReadOnlyList<string> Accents { get; } = ["MaAccent", "MaAccentHover", "MaAccentBorder", "MaFocus", "MaTileHover"];

    /// <summary>Text on the accent (HighlightText).</summary>
    public const string OnAccent = "MaOnAccent";

    /// <summary>Every theme key this swaps.</summary>
    public static IEnumerable<string> Keys => [.. Backgrounds, .. Foregrounds, .. Accents, OnAccent];

    /// <summary>What each dictionary held under a swapped key before it was swapped (null: nothing), to put back.</summary>
    private static readonly ConditionalWeakTable<ResourceDictionary, Dictionary<string, object?>> Saved = new();

    /// <summary>Applies the current system setting (or high contrast regardless, when <paramref name="force"/>, to check
    /// the colours without changing the Windows theme) to <paramref name="app"/>'s resources, and follows later changes.
    /// Dispose the result to stop following.</summary>
    public static IDisposable Track(Application app, bool force = false, IEnumerable<string>? backgrounds = null,
        IEnumerable<string>? foregrounds = null, IEnumerable<string>? accents = null) =>
        Track(app.Resources, app.Dispatcher, force, backgrounds, foregrounds, accents);

    /// <summary><see cref="Track(Application, bool, IEnumerable{string}?, IEnumerable{string}?, IEnumerable{string}?)"/>
    /// for any resource dictionary (a window's, say), updated on <paramref name="dispatcher"/>.</summary>
    public static IDisposable Track(ResourceDictionary resources, Dispatcher dispatcher, bool force = false,
        IEnumerable<string>? backgrounds = null, IEnumerable<string>? foregrounds = null, IEnumerable<string>? accents = null)
    {
        string[]? bg = backgrounds?.ToArray(), fg = foregrounds?.ToArray(), ac = accents?.ToArray();
        Apply(resources, force || SystemParameters.HighContrast, bg, fg, ac);
        if (force) return new Subscription(null);
        PropertyChangedEventHandler handler = (_, e) =>
        {
            if (e.PropertyName == nameof(SystemParameters.HighContrast))
                dispatcher.BeginInvoke(() => Apply(resources, SystemParameters.HighContrast, bg, fg, ac));
        };
        SystemParameters.StaticPropertyChanged += handler;
        return new Subscription(handler);
    }

    /// <summary>Swaps the system colours in (<paramref name="on"/>) or back out of <paramref name="resources"/>. Pass the
    /// same extra keys both ways.</summary>
    public static void Apply(ResourceDictionary resources, bool on, IEnumerable<string>? backgrounds = null,
        IEnumerable<string>? foregrounds = null, IEnumerable<string>? accents = null)
    {
        var roles = new List<(string Key, Brush Brush)>();
        foreach (var k in Backgrounds.Concat(backgrounds ?? [])) roles.Add((k, SystemColors.WindowBrush));
        foreach (var k in Foregrounds.Concat(foregrounds ?? [])) roles.Add((k, SystemColors.WindowTextBrush));
        foreach (var k in Accents.Concat(accents ?? [])) roles.Add((k, SystemColors.HighlightBrush));
        roles.Add((OnAccent, SystemColors.HighlightTextBrush));

        var saved = Saved.GetOrCreateValue(resources);
        if (on)
        {
            foreach (var (key, brush) in roles)
            {
                // Only the dictionary's own entry (an app's override), not one it merges in, such as Theme.xaml's.
                if (!saved.ContainsKey(key)) saved[key] = resources.Keys.Cast<object>().Contains(key) ? resources[key] : null;
                resources[key] = brush;
            }
            return;
        }
        foreach (var (key, _) in roles)
        {
            if (!saved.Remove(key, out var before)) continue;
            if (before is null) resources.Remove(key);
            else resources[key] = before;
        }
    }

    private sealed class Subscription(PropertyChangedEventHandler? handler) : IDisposable
    {
        private PropertyChangedEventHandler? handler = handler;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref handler, null) is { } h) SystemParameters.StaticPropertyChanged -= h;
        }
    }
}
