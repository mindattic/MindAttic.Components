using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>The outline a <see cref="MaShapeBorder"/> (and so an <see cref="MaButton"/>) draws.</summary>
public enum MaShape
{
    /// <summary>No frame at all: just the content (links, text buttons).</summary>
    None,

    /// <summary>A plain rectangle filling the bounds.</summary>
    Rectangle,

    /// <summary>A rectangle with <see cref="MaShapeBorder.CornerRadius"/> corners.</summary>
    Rounded,

    /// <summary>A rectangle with fully round ends.</summary>
    Pill,

    /// <summary>Cut top-left and bottom-right corners, <see cref="MaShapeBorder.Chamfer"/> deep.</summary>
    Chamfer,

    /// <summary>A tab: the top edge inset by <see cref="MaShapeBorder.Slant"/> on both sides.</summary>
    Trapezoid,

    /// <summary>Slanted sides: the top edge shifted right by <see cref="MaShapeBorder.Slant"/>.</summary>
    Parallelogram,
}

/// <summary>Where a <see cref="MaShapeBorder"/> draws its accent.</summary>
public enum MaAccentEdge
{
    /// <summary>No accent.</summary>
    None,
    /// <summary>Along the top edge (for slanted shapes, along the slanted top).</summary>
    Top,
    /// <summary>Along the bottom edge.</summary>
    Bottom,
    /// <summary>Along the left edge (a nav-item bar).</summary>
    Left,
    /// <summary>Along the right edge.</summary>
    Right,

    /// <summary>Short diagonal ticks along a chamfer's cut corners.</summary>
    ChamferTicks,
}

/// <summary>
/// A decorator that draws one configurable outline (<see cref="MaShape"/>) with a fill, a stroke and
/// an optional accent, around a padded child. It is the frame of <see cref="MaButton"/> and can frame
/// panels, cards and badges the same way, so a new look is a property value, not a new control.
/// </summary>
public sealed class MaShapeBorder : Decorator
{
    /// <summary>Identifies the <see cref="Shape"/> property.</summary>
    public static readonly DependencyProperty ShapeProperty = Register(nameof(Shape), typeof(MaShape), MaShape.Rectangle, render: true);
    /// <summary>Identifies the <see cref="Fill"/> property.</summary>
    public static readonly DependencyProperty FillProperty = Register(nameof(Fill), typeof(Brush), null, render: true);
    /// <summary>Identifies the <see cref="Stroke"/> property.</summary>
    public static readonly DependencyProperty StrokeProperty = Register(nameof(Stroke), typeof(Brush), null, render: true);
    /// <summary>Identifies the <see cref="StrokeThickness"/> property.</summary>
    public static readonly DependencyProperty StrokeThicknessProperty = Register(nameof(StrokeThickness), typeof(double), 1.0, render: true);
    /// <summary>Identifies the <see cref="CornerRadius"/> property.</summary>
    public static readonly DependencyProperty CornerRadiusProperty = Register(nameof(CornerRadius), typeof(double), 4.0, render: true);
    /// <summary>Identifies the <see cref="Chamfer"/> property.</summary>
    public static readonly DependencyProperty ChamferProperty = Register(nameof(Chamfer), typeof(double), 8.0, render: true);
    /// <summary>Identifies the <see cref="Slant"/> property.</summary>
    public static readonly DependencyProperty SlantProperty = Register(nameof(Slant), typeof(double), 12.0, render: true);
    /// <summary>Identifies the <see cref="AccentBrush"/> property.</summary>
    public static readonly DependencyProperty AccentBrushProperty = Register(nameof(AccentBrush), typeof(Brush), null, render: true);
    /// <summary>Identifies the <see cref="AccentEdge"/> property.</summary>
    public static readonly DependencyProperty AccentEdgeProperty = Register(nameof(AccentEdge), typeof(MaAccentEdge), MaAccentEdge.None, render: true);
    /// <summary>Identifies the <see cref="AccentThickness"/> property.</summary>
    public static readonly DependencyProperty AccentThicknessProperty = Register(nameof(AccentThickness), typeof(double), 2.0, render: true);
    /// <summary>Identifies the <see cref="Padding"/> property.</summary>
    public static readonly DependencyProperty PaddingProperty = Register(nameof(Padding), typeof(Thickness), default(Thickness), render: false);

    /// <summary>The frame's outline.</summary>
    public MaShape Shape { get => (MaShape)GetValue(ShapeProperty); set => SetValue(ShapeProperty, value); }

    /// <summary>The shape's fill.</summary>
    public Brush? Fill { get => (Brush?)GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The shape's outline brush.</summary>
    public Brush? Stroke { get => (Brush?)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>The border's thickness; 0 draws no border.</summary>
    public double StrokeThickness { get => (double)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }

    /// <summary>Corner radius for <see cref="MaShape.Rounded"/>.</summary>
    public double CornerRadius { get => (double)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    /// <summary>How deep the <see cref="MaShape.Chamfer"/> corners are cut (capped at a third of the smaller side).</summary>
    public double Chamfer { get => (double)GetValue(ChamferProperty); set => SetValue(ChamferProperty, value); }

    /// <summary>The horizontal inset of a <see cref="MaShape.Trapezoid"/> or <see cref="MaShape.Parallelogram"/> top edge.</summary>
    public double Slant { get => (double)GetValue(SlantProperty); set => SetValue(SlantProperty, value); }

    /// <summary>The accent's brush (a strip, bar or chamfer ticks, per <see cref="AccentEdge"/>).</summary>
    public Brush? AccentBrush { get => (Brush?)GetValue(AccentBrushProperty); set => SetValue(AccentBrushProperty, value); }

    /// <summary>Where the accent is drawn.</summary>
    public MaAccentEdge AccentEdge { get => (MaAccentEdge)GetValue(AccentEdgeProperty); set => SetValue(AccentEdgeProperty, value); }

    /// <summary>The accent's thickness in pixels.</summary>
    public double AccentThickness { get => (double)GetValue(AccentThicknessProperty); set => SetValue(AccentThicknessProperty, value); }

    /// <summary>Space between the frame and the child.</summary>
    public Thickness Padding { get => (Thickness)GetValue(PaddingProperty); set => SetValue(PaddingProperty, value); }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size constraint)
    {
        var pad = Padding;
        Child?.Measure(new Size(
            Math.Max(0, constraint.Width - pad.Left - pad.Right),
            Math.Max(0, constraint.Height - pad.Top - pad.Bottom)));
        var desired = Child?.DesiredSize ?? default;
        return new Size(desired.Width + pad.Left + pad.Right, desired.Height + pad.Top + pad.Bottom);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size arrangeSize)
    {
        var pad = Padding;
        Child?.Arrange(new Rect(
            pad.Left,
            pad.Top,
            Math.Max(0, arrangeSize.Width - pad.Left - pad.Right),
            Math.Max(0, arrangeSize.Height - pad.Top - pad.Bottom)));
        return arrangeSize;
    }

    /// <inheritdoc/>
    protected override void OnRender(DrawingContext drawingContext)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);
        var w = ActualWidth;
        var h = ActualHeight;
        if (w <= 0 || h <= 0)
        {
            return;
        }

        // The whole frame is clickable, even where it's transparent (a fill-less tab, nav item or chrome button, and a
        // shapeless link, which would otherwise only take clicks on the strokes of its letters).
        drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, w, h));
        if (Shape == MaShape.None)
        {
            return;
        }

        var t = StrokeThickness;
        var pen = Stroke is null || t <= 0 ? null : new Pen(Stroke, t);
        switch (Shape)
        {
            case MaShape.Rectangle when pen is null:
                drawingContext.DrawRectangle(Fill, null, new Rect(0, 0, w, h));
                break;
            case MaShape.Rectangle:
                drawingContext.DrawRectangle(Fill, pen, new Rect(t / 2, t / 2, Math.Max(0, w - t), Math.Max(0, h - t)));
                break;
            case MaShape.Rounded:
            case MaShape.Pill:
                var r = Shape == MaShape.Pill ? h / 2 : CornerRadius;
                var inset = pen is null ? 0 : t / 2;
                drawingContext.DrawRoundedRectangle(Fill, pen, new Rect(inset, inset, Math.Max(0, w - 2 * inset), Math.Max(0, h - 2 * inset)), r, r);
                break;
            default:
                drawingContext.DrawGeometry(Fill, pen, Outline(w, h, t));
                break;
        }

        DrawAccent(drawingContext, w, h);
    }

    /// <summary>
    /// The polygon shapes. Chamfer insets its vertices by half the stroke (as a framed panel does); the
    /// slanted shapes are scaled into the bounds less the stroke, exactly as a <c>Path</c> with
    /// <c>Stretch="Fill"</c> lays out the same figure.
    /// </summary>
    private StreamGeometry Outline(double w, double h, double t)
    {
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            if (Shape == MaShape.Chamfer)
            {
                var c = Math.Min(Chamfer, Math.Min(w, h) / 3);
                var half = t / 2;
                g.BeginFigure(new Point(c + half, half), isFilled: true, isClosed: true);
                g.LineTo(new Point(w - half, half), isStroked: true, isSmoothJoin: false);
                g.LineTo(new Point(w - half, h - c - half), isStroked: true, isSmoothJoin: false);
                g.LineTo(new Point(w - c - half, h - half), isStroked: true, isSmoothJoin: false);
                g.LineTo(new Point(half, h - half), isStroked: true, isSmoothJoin: false);
                g.LineTo(new Point(half, c + half), isStroked: true, isSmoothJoin: false);
            }
            else
            {
                var s = Math.Min(Slant, w / 2);
                Point[] figure = Shape == MaShape.Trapezoid
                    ? [new(s, 0), new(w - s, 0), new(w, h), new(0, h)]
                    : [new(s, 0), new(w, 0), new(w - s, h), new(0, h)];
                var pad = Stroke is null ? 0 : t;
                var sx = (w - pad) / w;
                var sy = (h - pad) / h;
                Point Fit(Point p) => new(pad / 2 + p.X * sx, pad / 2 + p.Y * sy);
                g.BeginFigure(Fit(figure[0]), isFilled: true, isClosed: true);
                for (var i = 1; i < figure.Length; i++)
                {
                    g.LineTo(Fit(figure[i]), isStroked: true, isSmoothJoin: false);
                }
            }
        }

        geometry.Freeze();
        return geometry;
    }

    private void DrawAccent(DrawingContext dc, double w, double h)
    {
        if (AccentBrush is null || AccentEdge == MaAccentEdge.None)
        {
            return;
        }

        var a = AccentThickness;
        switch (AccentEdge)
        {
            case MaAccentEdge.ChamferTicks:
                var c = Math.Min(Chamfer, Math.Min(w, h) / 3);
                if (c > 0)
                {
                    var tick = new Pen(AccentBrush, a) { StartLineCap = PenLineCap.Flat, EndLineCap = PenLineCap.Flat };
                    dc.DrawLine(tick, new Point(1, c + 1), new Point(c + 1, 1));
                    dc.DrawLine(tick, new Point(w - c - 1, h - 1), new Point(w - 1, h - c - 1));
                }

                break;
            case MaAccentEdge.Top when Shape is MaShape.Trapezoid or MaShape.Parallelogram:
                // Along the slanted shape's top edge, centred on it like a stroked line at y = 0.
                var s = Math.Min(Slant, w / 2);
                var end = Shape == MaShape.Trapezoid ? w - s : w;
                dc.DrawLine(new Pen(AccentBrush, a), new Point(s, 0), new Point(end, 0));
                break;
            case MaAccentEdge.Top:
                dc.DrawRectangle(AccentBrush, null, new Rect(0, 0, w, Math.Min(a, h)));
                break;
            case MaAccentEdge.Bottom:
                dc.DrawRectangle(AccentBrush, null, new Rect(0, Math.Max(0, h - a), w, Math.Min(a, h)));
                break;
            case MaAccentEdge.Left:
                dc.DrawRectangle(AccentBrush, null, new Rect(0, 0, Math.Min(a, w), h));
                break;
            case MaAccentEdge.Right:
                dc.DrawRectangle(AccentBrush, null, new Rect(Math.Max(0, w - a), 0, Math.Min(a, w), h));
                break;
        }
    }

    private static DependencyProperty Register(string name, Type type, object? fallback, bool render) =>
        DependencyProperty.Register(name, type, typeof(MaShapeBorder), new FrameworkPropertyMetadata(
            fallback,
            render ? FrameworkPropertyMetadataOptions.AffectsRender : FrameworkPropertyMetadataOptions.AffectsMeasure));
}
