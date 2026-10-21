using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>One reading on a <see cref="MaLineChart"/>: a percent at an instant. <paramref name="Brush"/> colours the
/// segment that ends at this point (e.g. <c>MaBad</c> while above a pace line, <c>MaGood</c> below); null draws it in
/// <c>MaAccent</c>.</summary>
public sealed record MaChartPoint(DateTime AtUtc, double Percent, Brush? Brush = null);

/// <summary>How a <see cref="MaChartReferenceLine"/> is stroked.</summary>
public enum MaLineDash
{
    /// <summary>A continuous line.</summary>
    Solid,
    /// <summary>Dashes (a pace line).</summary>
    Dashed,
    /// <summary>Dots.</summary>
    Dotted,
}

/// <summary>
/// A line drawn behind the data: a level (a target, a limit) or any path (a pace curve). <paramref name="Label"/>, when
/// set, is written just above its first point in the line's own brush. A null <paramref name="Brush"/> is <c>MaMuted</c>.
/// </summary>
public sealed record MaChartReferenceLine(IReadOnlyList<MaChartPoint> Path, Brush? Brush = null, MaLineDash Dash = MaLineDash.Solid,
    double Thickness = 1, string? Label = null)
{
    /// <summary>A horizontal line at <paramref name="percent"/> across <paramref name="fromUtc"/>..<paramref name="toUtc"/>.</summary>
    public static MaChartReferenceLine Level(DateTime fromUtc, DateTime toUtc, double percent, Brush? brush = null,
        MaLineDash dash = MaLineDash.Solid, double thickness = 1, string? label = null) =>
        new([new(fromUtc, percent), new(toUtc, percent)], brush, dash, thickness, label);

    /// <summary><paramref name="percentAt"/> sampled <paramref name="steps"/> times across the span: a straight line
    /// needs 1 step, a curve about 60.</summary>
    public static MaChartReferenceLine Curve(DateTime fromUtc, DateTime toUtc, Func<DateTime, double> percentAt, int steps = 60,
        Brush? brush = null, MaLineDash dash = MaLineDash.Dashed, double thickness = 1.5, string? label = null)
    {
        steps = Math.Max(1, steps);
        var points = new MaChartPoint[steps + 1];
        for (var i = 0; i <= steps; i++)
        {
            var at = fromUtc + (toUtc - fromUtc) * ((double)i / steps);
            points[i] = new(at, percentAt(at));
        }
        return new(points, brush, dash, thickness, label);
    }
}

/// <summary>Where the line is headed: a dotted segment from the last point to (<paramref name="AtUtc"/>,
/// <paramref name="Percent"/>), labelled at its end. A null <paramref name="Brush"/> is <c>MaMuted</c>.</summary>
public sealed record MaChartProjection(DateTime AtUtc, double Percent, Brush? Brush = null, string? Label = null);

/// <summary>
/// What a <see cref="MaLineChart"/> draws. The time axis runs <paramref name="StartUtc"/> to <paramref name="EndUtc"/>.
/// <paramref name="Points"/> is the line (empty draws only the axes); its last point gets a dot labelled
/// <paramref name="EndLabel"/> (e.g. "now 42%", or "ended 42%" for a finished span, which has no
/// <paramref name="Projection"/>).
/// </summary>
public sealed record MaLineChartData(DateTime StartUtc, DateTime EndUtc, IReadOnlyList<MaChartPoint> Points, string? EndLabel = null,
    MaChartProjection? Projection = null, IReadOnlyList<MaChartReferenceLine>? ReferenceLines = null);

/// <summary>
/// A percent-over-time line chart in the theme's brushes, lifted from MindAttic.Orchestrator's utilization chart. A
/// percent axis with a gridline every 25% (0-100, with headroom above the highest point so its label clears the line);
/// a time axis labelled at both ends, with a faint tick and local time on every whole hour between; reference lines
/// behind the data; the line itself, each segment in its end point's brush; a dot on the last point with its label; and
/// a dotted projection to where the line is headed, labelled at its end. With no points it draws just the axes, and
/// without a projection just the line as it ended. Brushes come from the theme: <c>MaText</c>, <c>MaMuted</c>,
/// <c>MaChartGrid</c> and <c>MaAccent</c> (re-key any of them to restyle every chart). Nothing is told by colour alone:
/// pair the chart with a caption that says the same in words.
/// </summary>
public sealed class MaLineChart : FrameworkElement
{
    /// <summary>Identifies <see cref="Data"/>.</summary>
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(nameof(Data), typeof(MaLineChartData), typeof(MaLineChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>What to draw; null draws nothing.</summary>
    public MaLineChartData? Data
    {
        get => (MaLineChartData?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    /// <summary>The percents that get a gridline and a label (the 0, 50 and 100 lines a little heavier).</summary>
    public static IReadOnlyList<double> PercentTicks { get; } = [0, 25, 50, 75, 100];

    private const double Left = 44, Bottom = 24, Top = 10, Right = 12, FontSize = 12.5;

    /// <summary>The top of the percent axis: at least 115, headroom of 15 over <paramref name="highest"/> rounded up to
    /// a 10, at most 170.</summary>
    public static double AxisTop(double highest) => Math.Max(115, Math.Min(170, Math.Ceiling(highest / 10) * 10 + 15));

    /// <inheritdoc />
    protected override void OnRender(DrawingContext dc)
    {
        if (Data is not { } d || d.EndUtc <= d.StartUtc || ActualWidth <= Left + Right || ActualHeight <= Top + Bottom) return;
        var text = Brush("MaText", Brushes.White);
        var muted = Brush("MaMuted", Brushes.Gray);
        var grid = Brush("MaChartGrid", Brushes.DimGray);
        var accent = Brush("MaAccent", Brushes.SteelBlue);
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var typeface = new Typeface("Segoe UI");
        FormattedText Text(string s, Brush b) => new(s, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, FontSize, b, dpi);

        var last = d.Points.Count > 0 ? d.Points[^1] : null;
        var top = AxisTop(Math.Max(d.Projection is { } pr && last is not null ? pr.Percent : 0, last?.Percent ?? 0));
        var (w, h) = (ActualWidth - Left - Right, ActualHeight - Top - Bottom);
        var span = (d.EndUtc - d.StartUtc).TotalSeconds;
        Point P(DateTime at, double percent) => new(
            Left + w * Math.Clamp((at - d.StartUtc).TotalSeconds / span, 0, 1),
            Top + h * (1 - Math.Clamp(percent, 0, top) / top));

        void Label(string s, Brush b, Point at, bool right = false)
        {
            var f = Text(s, b);
            dc.DrawText(f, new Point(right ? at.X - f.Width : at.X, at.Y - f.Height / 2));
        }

        foreach (var p in PercentTicks)
        {
            dc.DrawLine(new Pen(grid, p % 50 == 0 ? 1 : 0.5), P(d.StartUtc, p), P(d.EndUtc, p));
            Label($"{p}%", muted, new Point(Left - 6, P(d.StartUtc, p).Y), right: true);
        }

        // The span's start and end at the ends, and a faint tick on every hour between them.
        var labelY = ActualHeight - Bottom / 2;
        Label(d.StartUtc.ToLocalTime().ToString("t"), muted, new Point(Left, labelY));
        Label(d.EndUtc.ToLocalTime().ToString("t"), muted, new Point(ActualWidth - Right, labelY), right: true);
        var hourPen = new Pen(grid, 0.5) { DashStyle = DashStyles.Dot };
        foreach (var at in HourTicks(d.StartUtc, d.EndUtc, TimeZoneInfo.Local))
        {
            var x = P(at, 0).X;
            if (x - Left < 48 || ActualWidth - Right - x < 48) continue; // leave the end labels room
            dc.DrawLine(hourPen, new Point(x, Top), new Point(x, Top + h));
            var f = Text(TimeZoneInfo.ConvertTimeFromUtc(at, TimeZoneInfo.Local).ToString("t"), muted);
            dc.DrawText(f, new Point(x - f.Width / 2, labelY - f.Height / 2));
        }

        foreach (var r in d.ReferenceLines ?? [])
        {
            if (r.Path.Count == 0) continue;
            var brush = r.Brush ?? muted;
            var pen = new Pen(brush, r.Thickness) { DashStyle = r.Dash switch { MaLineDash.Dashed => DashStyles.Dash, MaLineDash.Dotted => DashStyles.Dot, _ => DashStyles.Solid } };
            if (r.Path.Count == 2) dc.DrawLine(pen, P(r.Path[0].AtUtc, r.Path[0].Percent), P(r.Path[1].AtUtc, r.Path[1].Percent));
            else if (r.Path.Count > 2)
            {
                var path = new StreamGeometry();
                using (var g = path.Open())
                {
                    g.BeginFigure(P(r.Path[0].AtUtc, r.Path[0].Percent), isFilled: false, isClosed: false);
                    for (var i = 1; i < r.Path.Count; i++) g.LineTo(P(r.Path[i].AtUtc, r.Path[i].Percent), isStroked: true, isSmoothJoin: true);
                }
                path.Freeze();
                dc.DrawGeometry(null, pen, path);
            }
            if (r.Label is { Length: > 0 } label)
            {
                var start = P(r.Path[0].AtUtc, r.Path[0].Percent);
                Label(label, brush, new Point(start.X + 6, start.Y - 10));
            }
        }

        if (last is null) return; // no data: just the axes

        // The line, each stretch in its end point's brush.
        var pens = new Dictionary<Brush, Pen>();
        Pen PenFor(Brush b) => pens.TryGetValue(b, out var pen) ? pen : pens[b] = new Pen(b, 2.5);
        for (var i = 1; i < d.Points.Count; i++)
        {
            var (a, b) = (d.Points[i - 1], d.Points[i]);
            dc.DrawLine(PenFor(b.Brush ?? accent), P(a.AtUtc, a.Percent), P(b.AtUtc, b.Percent));
        }

        var plot = new Rect(Left, Top, w, h);
        var now = P(last.AtUtc, last.Percent);
        var nowText = Text(d.EndLabel ?? "", text);

        // No projection: the line as it ended, its last point labelled (the end-of-axis spot left empty).
        if (d.Projection is not { } proj)
        {
            dc.DrawEllipse(text, null, now, 4, 4);
            if (d.EndLabel is { Length: > 0 })
                dc.DrawText(nowText, PlaceEndLabels(now, new Size(nowText.Width, nowText.Height), P(d.EndUtc, 0), new Size(0, 0), plot).Now.TopLeft);
            return;
        }

        var lands = proj.Brush ?? muted;
        var end = P(proj.AtUtc, proj.Percent);
        dc.DrawLine(new Pen(lands, 2) { DashStyle = DashStyles.Dot }, now, end);
        dc.DrawEllipse(text, null, now, 4, 4);
        var endText = Text(proj.Label ?? "", lands);
        var (nowAt, endAt) = PlaceEndLabels(now, new Size(nowText.Width, nowText.Height), end, new Size(endText.Width, endText.Height), plot);
        if (d.EndLabel is { Length: > 0 }) dc.DrawText(nowText, nowAt.TopLeft);
        if (endAt is { } e && proj.Label is { Length: > 0 }) dc.DrawText(endText, e.TopLeft);
    }

    /// <summary>
    /// Every whole hour on <paramref name="zone"/>'s clock strictly between <paramref name="startUtc"/> and
    /// <paramref name="endUtc"/>, as UTC instants. Stepped in UTC, a quarter hour at a time (every zone's offset is whole
    /// quarters): stepping the local clock across a clock change drew the spring-forward's missing 2:00 on top of 3:00,
    /// two labels in one place, and left out the fall-back's first 1:00.
    /// </summary>
    public static IEnumerable<DateTime> HourTicks(DateTime startUtc, DateTime endUtc, TimeZoneInfo zone)
    {
        var quarter = TimeSpan.FromMinutes(15);
        var t = new DateTime(startUtc.Ticks - startUtc.Ticks % quarter.Ticks, DateTimeKind.Utc);
        for (; t < endUtc; t += quarter)
            if (t > startUtc && TimeZoneInfo.ConvertTimeFromUtc(t, zone).Minute == 0) yield return t;
    }

    /// <summary>
    /// Where the last point's label and the projection's label go. The projection's sits just left of its end, above it;
    /// the last point's sits above and right of the dot. When the two are close they would overlap, so the last point's
    /// tries the dot's other sides in turn, and if none of them is clear the projection's label is dropped (null): the
    /// last point is the fact, the projection a guess.
    /// </summary>
    public static (Rect Now, Rect? End) PlaceEndLabels(Point now, Size nowSize, Point end, Size endSize, Rect plot)
    {
        Rect At(double x, double y, Size s) => new(x, Math.Max(plot.Top, y - s.Height / 2), s.Width, s.Height);
        var reset = At(end.X - 4 - endSize.Width, Math.Max(plot.Top + 8, end.Y - 12), endSize);
        var aboveY = Math.Max(plot.Top + 8, now.Y - 18);
        var belowY = now.Y + 18;
        var candidates = new[]
        {
            At(now.X + 6, aboveY, nowSize),                  // above-right: the usual spot
            At(now.X - 6 - nowSize.Width, aboveY, nowSize),  // above-left
            At(now.X + 6, belowY, nowSize),                  // below-right
            At(now.X - 6 - nowSize.Width, belowY, nowSize),  // below-left
        };
        var bounds = new Rect(plot.Left, plot.Top - 2, plot.Right + 8 - plot.Left, plot.Height + 4); // the right gutter may hold a few pixels
        bool Fits(Rect r) => r.Left >= bounds.Left && r.Right <= bounds.Right && r.Top >= bounds.Top && r.Bottom <= bounds.Bottom;
        var gap = Rect.Inflate(reset, 3, 1);
        foreach (var c in candidates)
            if (Fits(c) && !c.IntersectsWith(gap)) return (c, reset);
        return (candidates.FirstOrDefault(Fits, candidates[1]), null);
    }

    private Brush Brush(string key, Brush fallback) => TryFindResource(key) as Brush ?? fallback;
}
