using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// The look of <see cref="MaCalloutToolTip"/> (M.COMP-T167): a rounded bubble with a small right-angled arrow on the
/// edge facing its target, drawn as one outline so the arrow's sides run straight on from the bubble's border.
/// <see cref="Side"/> is where the bubble sits relative to the target (the arrow is on the opposite edge) and
/// <see cref="ArrowOffset"/> how far along that edge the arrow's tip is. The arrow's strip is reserved on that edge
/// only, so the child is laid out inside the bubble. Normally used through MaCalloutToolTip's template; it is a
/// plain <see cref="Decorator"/>, so it can frame anything else that should point somewhere.
/// </summary>
public class MaCalloutChrome : Decorator
{
    public static readonly DependencyProperty BackgroundProperty = Panel.BackgroundProperty.AddOwner(typeof(MaCalloutChrome),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));

    public static readonly DependencyProperty BorderBrushProperty = Border.BorderBrushProperty.AddOwner(typeof(MaCalloutChrome),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));

    public static readonly DependencyProperty BorderThicknessProperty = Border.BorderThicknessProperty.AddOwner(typeof(MaCalloutChrome),
        new FrameworkPropertyMetadata(new Thickness(1), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty PaddingProperty = Border.PaddingProperty.AddOwner(typeof(MaCalloutChrome),
        new FrameworkPropertyMetadata(new Thickness(0), FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
        nameof(CornerRadius), typeof(double), typeof(MaCalloutChrome),
        new FrameworkPropertyMetadata(4d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SideProperty = DependencyProperty.Register(
        nameof(Side), typeof(MaCalloutSide), typeof(MaCalloutChrome),
        new FrameworkPropertyMetadata(MaCalloutSide.Right, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ArrowSizeProperty = DependencyProperty.Register(
        nameof(ArrowSize), typeof(double), typeof(MaCalloutChrome),
        new FrameworkPropertyMetadata(7d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ArrowOffsetProperty = DependencyProperty.Register(
        nameof(ArrowOffset), typeof(double), typeof(MaCalloutChrome),
        new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>The bubble's fill (MaSurface in the theme).</summary>
    public Brush? Background { get => (Brush?)GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }

    /// <summary>The outline of the bubble and its arrow (MaChromeBorder in the theme).</summary>
    public Brush? BorderBrush { get => (Brush?)GetValue(BorderBrushProperty); set => SetValue(BorderBrushProperty, value); }

    /// <summary>The outline's width; only <see cref="Thickness.Left"/> is used (the outline is uniform).</summary>
    public Thickness BorderThickness { get => (Thickness)GetValue(BorderThicknessProperty); set => SetValue(BorderThicknessProperty, value); }

    /// <summary>Space between the outline and the child.</summary>
    public Thickness Padding { get => (Thickness)GetValue(PaddingProperty); set => SetValue(PaddingProperty, value); }

    /// <summary>The bubble's corner radius (4, like the plain tooltip).</summary>
    public double CornerRadius { get => (double)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    /// <summary>Where the bubble is relative to its target; the arrow is drawn on the opposite edge.</summary>
    public MaCalloutSide Side { get => (MaCalloutSide)GetValue(SideProperty); set => SetValue(SideProperty, value); }

    /// <summary>How far the arrow sticks out of the bubble (its base is twice this).</summary>
    public double ArrowSize { get => (double)GetValue(ArrowSizeProperty); set => SetValue(ArrowSizeProperty, value); }

    /// <summary>The arrow tip's distance along its edge from the bubble's top (left/right edges) or left (top/bottom
    /// edges); NaN centres it. It is clamped to the edge's straight part, clear of the rounded corners.</summary>
    public double ArrowOffset { get => (double)GetValue(ArrowOffsetProperty); set => SetValue(ArrowOffsetProperty, value); }

    /// <summary>The strip the arrow takes on the bubble's facing edge.</summary>
    public static Thickness ArrowStrip(MaCalloutSide side, double arrowSize) => side switch
    {
        MaCalloutSide.Right => new Thickness(arrowSize, 0, 0, 0),
        MaCalloutSide.Left => new Thickness(0, 0, arrowSize, 0),
        MaCalloutSide.Bottom => new Thickness(0, arrowSize, 0, 0),
        _ => new Thickness(0, 0, 0, arrowSize),
    };

    private Thickness Chrome()
    {
        var strip = ArrowStrip(Side, ArrowSize);
        var b = BorderThickness.Left;
        var p = Padding;
        return new Thickness(strip.Left + b + p.Left, strip.Top + b + p.Top, strip.Right + b + p.Right, strip.Bottom + b + p.Bottom);
    }

    protected override Size MeasureOverride(Size constraint)
    {
        var c = Chrome();
        var extraW = c.Left + c.Right;
        var extraH = c.Top + c.Bottom;
        if (Child is null) return new Size(extraW, extraH);
        Child.Measure(new Size(Math.Max(0, constraint.Width - extraW), Math.Max(0, constraint.Height - extraH)));
        return new Size(Child.DesiredSize.Width + extraW, Child.DesiredSize.Height + extraH);
    }

    protected override Size ArrangeOverride(Size arrangeSize)
    {
        var c = Chrome();
        Child?.Arrange(new Rect(c.Left, c.Top,
            Math.Max(0, arrangeSize.Width - c.Left - c.Right), Math.Max(0, arrangeSize.Height - c.Top - c.Bottom)));
        return arrangeSize;
    }

    protected override void OnRender(DrawingContext dc)
    {
        var geometry = BuildGeometry(new Rect(RenderSize), Side, ArrowSize, ArrowOffset, CornerRadius, BorderThickness.Left);
        if (geometry is null) return;
        var t = BorderThickness.Left;
        var pen = BorderBrush is null || t <= 0 ? null : new Pen(BorderBrush, t) { LineJoin = PenLineJoin.Miter };
        dc.DrawGeometry(Background, pen, geometry);
    }

    /// <summary>The bubble-plus-arrow outline inside <paramref name="bounds"/>, inset by half the pen so a
    /// <paramref name="thickness"/> stroke lands inside it; null when there's no room to draw.</summary>
    public static StreamGeometry? BuildGeometry(Rect bounds, MaCalloutSide side, double arrowSize, double arrowOffset,
        double cornerRadius, double thickness)
    {
        var strip = ArrowStrip(side, arrowSize);
        var bubble = new Rect(bounds.Left + strip.Left, bounds.Top + strip.Top,
            Math.Max(0, bounds.Width - strip.Left - strip.Right), Math.Max(0, bounds.Height - strip.Top - strip.Bottom));
        var h = thickness / 2;
        if (bubble.Width <= thickness || bubble.Height <= thickness) return null;
        var b = new Rect(bubble.Left + h, bubble.Top + h, bubble.Width - thickness, bubble.Height - thickness);
        var r = Math.Max(0, Math.Min(cornerRadius - h, Math.Min(b.Width, b.Height) / 2));
        var a = arrowSize;
        var horizontal = side is MaCalloutSide.Left or MaCalloutSide.Right;
        var along = CalloutPlacement.ClampArrow(arrowOffset, horizontal ? bubble.Height : bubble.Width, a, cornerRadius);
        var c = (horizontal ? bubble.Top : bubble.Left) + along;
        var corner = new Size(r, r);

        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            ctx.BeginFigure(new Point(b.Left + r, b.Top), isFilled: true, isClosed: true);
            if (a > 0 && side == MaCalloutSide.Bottom) Arrow(ctx, new(c - a, b.Top), new(c, b.Top - a), new(c + a, b.Top));
            ctx.LineTo(new Point(b.Right - r, b.Top), true, false);
            ctx.ArcTo(new Point(b.Right, b.Top + r), corner, 0, false, SweepDirection.Clockwise, true, false);
            if (a > 0 && side == MaCalloutSide.Left) Arrow(ctx, new(b.Right, c - a), new(b.Right + a, c), new(b.Right, c + a));
            ctx.LineTo(new Point(b.Right, b.Bottom - r), true, false);
            ctx.ArcTo(new Point(b.Right - r, b.Bottom), corner, 0, false, SweepDirection.Clockwise, true, false);
            if (a > 0 && side == MaCalloutSide.Top) Arrow(ctx, new(c + a, b.Bottom), new(c, b.Bottom + a), new(c - a, b.Bottom));
            ctx.LineTo(new Point(b.Left + r, b.Bottom), true, false);
            ctx.ArcTo(new Point(b.Left, b.Bottom - r), corner, 0, false, SweepDirection.Clockwise, true, false);
            if (a > 0 && side == MaCalloutSide.Right) Arrow(ctx, new(b.Left, c + a), new(b.Left - a, c), new(b.Left, c - a));
            ctx.LineTo(new Point(b.Left, b.Top + r), true, false);
            ctx.ArcTo(new Point(b.Left + r, b.Top), corner, 0, false, SweepDirection.Clockwise, true, false);
        }
        g.Freeze();
        return g;
    }

    private static void Arrow(StreamGeometryContext ctx, Point baseStart, Point tip, Point baseEnd)
    {
        ctx.LineTo(baseStart, true, false);
        ctx.LineTo(tip, true, false);
        ctx.LineTo(baseEnd, true, false);
    }
}
