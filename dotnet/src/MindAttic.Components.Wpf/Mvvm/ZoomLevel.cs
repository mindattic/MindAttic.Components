using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace MindAttic.Components.Wpf.Mvvm;

/// <summary>
/// A window's zoom (WCAG 1.4.4, resize text): <see cref="Scale"/> from 80% to 200% in 10% steps, the "125%" status
/// <see cref="Text"/> (empty at 100%), and commands to zoom in, out and back to 100%. <see cref="Attach"/> scales a
/// window's content by it and binds Ctrl+Plus / Ctrl+Minus / Ctrl+0 (main row and number pad). Pass
/// <c>changed</c> to save the level, e.g. in per-user settings. Lifted from the Orchestrator and Workflow Desktop (M.COMP-T11).
/// </summary>
public sealed class ZoomLevel : ObservableObject
{
    public const double Min = 0.8, Max = 2.0, Step = 0.1;

    private readonly Action<double>? changed;
    private double scale;
    private ICommand? zoomIn, zoomOut, reset;

    public ZoomLevel(double initial = 1, Action<double>? changed = null)
    {
        scale = Clamp(initial);
        this.changed = changed;
    }

    /// <summary>The scale factor, 1 at 100%. Set values are clamped to <see cref="Min"/>..<see cref="Max"/> and rounded
    /// to two places (so repeated steps never drift); a NaN is 100%.</summary>
    public double Scale
    {
        get => scale;
        set
        {
            if (!Set(ref scale, Clamp(value))) return;
            Raise(nameof(Text));
            changed?.Invoke(scale);
        }
    }

    /// <summary>"125%" while zoomed; empty at 100%.</summary>
    public string Text => Math.Abs(Scale - 1) < 0.001 ? "" : $"{Scale * 100:0}%";

    public ICommand InCommand => zoomIn ??= new RelayCommand(() => Scale += Step);
    public ICommand OutCommand => zoomOut ??= new RelayCommand(() => Scale -= Step);
    public ICommand ResetCommand => reset ??= new RelayCommand(() => Scale = 1);

    /// <summary>Scales <paramref name="content"/> (its LayoutTransform, so it lays out at the new size and its panels
    /// wrap and scroll rather than clip) and adds the zoom keys to <paramref name="keys"/> (the window, so they work
    /// wherever focus is; <paramref name="content"/> when null).</summary>
    public void Attach(FrameworkElement content, UIElement? keys = null)
    {
        var transform = new ScaleTransform();
        BindingOperations.SetBinding(transform, ScaleTransform.ScaleXProperty, new Binding(nameof(Scale)) { Source = this });
        BindingOperations.SetBinding(transform, ScaleTransform.ScaleYProperty, new Binding(nameof(Scale)) { Source = this });
        content.LayoutTransform = transform;

        var target = keys ?? content;
        foreach (var (command, key) in new[]
                 {
                     (InCommand, Key.OemPlus), (InCommand, Key.Add),
                     (OutCommand, Key.OemMinus), (OutCommand, Key.Subtract),
                     (ResetCommand, Key.D0), (ResetCommand, Key.NumPad0),
                 })
            target.InputBindings.Add(new KeyBinding(command, key, ModifierKeys.Control));
    }

    private static double Clamp(double value) =>
        double.IsNaN(value) ? 1 : Math.Round(Math.Clamp(value, Min, Max), 2);
}
