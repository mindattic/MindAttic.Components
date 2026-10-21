using System.Windows;
using System.Windows.Controls;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// Persistent tooltips (WCAG 1.4.13, content on hover or focus): <see cref="KeepOpen"/> makes every tooltip in the app
/// stay up until the pointer or focus leaves it or Esc is pressed, rather than vanishing after WPF's 5 seconds. Call it
/// once at startup, e.g. from the App's static constructor. Lifted from the Orchestrator and Workflow Desktop (M.COMP-T11).
/// </summary>
public static class MaToolTips
{
    private static int applied;

    /// <summary>Overrides <see cref="ToolTipService.ShowDurationProperty"/>'s default for every element to
    /// <see cref="int.MaxValue"/>. Safe to call more than once; an element's own ToolTipService.ShowDuration still wins.</summary>
    public static void KeepOpen()
    {
        if (Interlocked.Exchange(ref applied, 1) == 1) return;
        try
        {
            ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(int.MaxValue));
        }
        catch (ArgumentException)
        {
            // The app already overrode it itself (its own copy of this, from before it used the library).
        }
    }
}
