using System.Windows;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>Which side of its target a callout (<see cref="MaCalloutToolTip"/>) sits on. Its arrow is on the bubble's
/// opposite edge, pointing back at the target: a callout on the <see cref="Right"/> has its arrow on its left edge.</summary>
public enum MaCalloutSide
{
    /// <summary>To the right of the target (the arrow on the bubble's left edge).</summary>
    Right,
    /// <summary>To the left of the target (the arrow on the bubble's right edge).</summary>
    Left,
    /// <summary>Above the target (the arrow on the bubble's bottom edge).</summary>
    Top,
    /// <summary>Below the target (the arrow on the bubble's top edge).</summary>
    Bottom,
}

/// <summary>Where <see cref="CalloutPlacement.Compute"/> puts a callout.</summary>
/// <param name="Side">The side it ended up on (the preferred one, or the opposite after a flip).</param>
/// <param name="BubbleOrigin">The bubble's top-left (the arrow excluded), in the same units as the inputs.</param>
/// <param name="ArrowOffset">The arrow tip's distance along the bubble's facing edge, from the bubble's top (for
/// <see cref="MaCalloutSide.Left"/>/<see cref="MaCalloutSide.Right"/>) or left (for Top/Bottom).</param>
public readonly record struct CalloutPlacementResult(MaCalloutSide Side, Point BubbleOrigin, double ArrowOffset);

/// <summary>
/// The callout's placement rules, as a pure function (M.COMP-T167), in the spirit of Floating UI / Popper:
/// <list type="number">
/// <item><b>Side</b>: the preferred side if the bubble, its arrow and the gap fit between the target and the work
/// area's edge; otherwise the opposite side if that fits; otherwise whichever of the two has more room.</item>
/// <item><b>Main axis</b>: the arrow tip sits <c>gap</c> from the target's edge. Only when neither side fits is the
/// bubble pushed back inside the work area (it may then overlap the target, rather than be cut off).</item>
/// <item><b>Cross axis</b>: centred on the target, then shifted to stay inside the work area (a tooltip near the
/// bottom of the screen slides up).</item>
/// <item><b>Arrow</b>: points at the target's centre (target centre minus bubble origin), clamped so its base never
/// leaves the straight part of the edge: at least <c>cornerRadius + arrowSize</c> from either end.</item>
/// </list>
/// All inputs share one unit and one coordinate space (<see cref="MaCalloutToolTip"/> passes screen device pixels).
/// </summary>
public static class CalloutPlacement
{
    /// <summary>Places a <paramref name="bubble"/> beside <paramref name="target"/> inside <paramref name="workArea"/>.</summary>
    /// <param name="target">The element pointed at.</param>
    /// <param name="bubble">The bubble's size, without its arrow.</param>
    /// <param name="workArea">The usable area of the target's monitor (its work area, outside the taskbar).</param>
    /// <param name="preferred">The side to use when it fits.</param>
    /// <param name="gap">Space between the arrow tip and the target's edge.</param>
    /// <param name="arrowSize">How far the arrow sticks out of the bubble; its base is twice this (a right-angled tip).</param>
    /// <param name="cornerRadius">The bubble's corner radius, which the arrow's base stays clear of.</param>
    public static CalloutPlacementResult Compute(Rect target, Size bubble, Rect workArea, MaCalloutSide preferred,
        double gap, double arrowSize, double cornerRadius)
    {
        var side = ChooseSide(target, bubble, workArea, preferred, gap, arrowSize);
        var horizontal = side is MaCalloutSide.Left or MaCalloutSide.Right;
        var reach = gap + arrowSize;

        double x, y;
        if (horizontal)
        {
            x = side == MaCalloutSide.Right ? target.Right + reach : target.Left - reach - bubble.Width;
            x = Clamp(x, workArea.Left, workArea.Right - bubble.Width);
            y = Clamp(target.Top + target.Height / 2 - bubble.Height / 2, workArea.Top, workArea.Bottom - bubble.Height);
        }
        else
        {
            y = side == MaCalloutSide.Bottom ? target.Bottom + reach : target.Top - reach - bubble.Height;
            y = Clamp(y, workArea.Top, workArea.Bottom - bubble.Height);
            x = Clamp(target.Left + target.Width / 2 - bubble.Width / 2, workArea.Left, workArea.Right - bubble.Width);
        }

        var arrow = horizontal
            ? ClampArrow(target.Top + target.Height / 2 - y, bubble.Height, arrowSize, cornerRadius)
            : ClampArrow(target.Left + target.Width / 2 - x, bubble.Width, arrowSize, cornerRadius);
        return new CalloutPlacementResult(side, new Point(x, y), arrow);
    }

    /// <summary>The side the bubble goes on: preferred, else the opposite, else the roomier of the two.</summary>
    public static MaCalloutSide ChooseSide(Rect target, Size bubble, Rect workArea, MaCalloutSide preferred, double gap, double arrowSize)
    {
        var opposite = Opposite(preferred);
        var preferredRoom = Room(target, workArea, preferred);
        var oppositeRoom = Room(target, workArea, opposite);
        var need = (preferred is MaCalloutSide.Left or MaCalloutSide.Right ? bubble.Width : bubble.Height) + gap + arrowSize;
        if (preferredRoom >= need) return preferred;
        if (oppositeRoom >= need) return opposite;
        return oppositeRoom > preferredRoom ? opposite : preferred;
    }

    /// <summary>The other side of the same axis.</summary>
    public static MaCalloutSide Opposite(MaCalloutSide side) => side switch
    {
        MaCalloutSide.Right => MaCalloutSide.Left,
        MaCalloutSide.Left => MaCalloutSide.Right,
        MaCalloutSide.Top => MaCalloutSide.Bottom,
        _ => MaCalloutSide.Top,
    };

    /// <summary>The arrow's offset along an edge <paramref name="edgeLength"/> long, kept on the edge's straight part
    /// (centred when the edge is too short for that).</summary>
    public static double ClampArrow(double wanted, double edgeLength, double arrowSize, double cornerRadius)
    {
        var min = cornerRadius + arrowSize;
        var max = edgeLength - cornerRadius - arrowSize;
        if (double.IsNaN(wanted) || min > max) return edgeLength / 2;
        return Math.Min(Math.Max(wanted, min), max);
    }

    private static double Room(Rect target, Rect workArea, MaCalloutSide side) => side switch
    {
        MaCalloutSide.Right => workArea.Right - target.Right,
        MaCalloutSide.Left => target.Left - workArea.Left,
        MaCalloutSide.Top => target.Top - workArea.Top,
        _ => workArea.Bottom - target.Bottom,
    };

    // Lower bound wins when the bubble is bigger than the area (it starts at the area's top/left edge).
    private static double Clamp(double value, double min, double max) => Math.Max(Math.Min(value, max), min);
}
