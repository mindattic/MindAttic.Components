using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MindAttic.Components.Wpf.Controls;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T12: MaLineChart, the Orchestrator's utilization chart as a library control.</summary>
public sealed class MaLineChartTests
{
    private static readonly DateTime Start = new(2026, 10, 16, 14, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = Start.AddHours(5);
    private static readonly DateTime Now = Start.AddHours(3);

    private static MaLineChartData Running(Brush over, Brush under, Brush lands) => new(Start, End,
        [new(Start, 0), new(Start.AddHours(1), 30, over), new(Start.AddHours(2), 45, under), new(Now, 70, over)],
        EndLabel: "now 70%",
        Projection: new(End, 110, lands, "110% at reset"),
        ReferenceLines:
        [
            MaChartReferenceLine.Level(Start, End, 99, Brushes.SteelBlue, label: "99% target"),
            MaChartReferenceLine.Curve(Start, End, at => 99 * Math.Sqrt((at - Start) / (End - Start))),
        ]);

    [Fact]
    public void A_running_span_draws_axes_reference_lines_coloured_segments_and_a_dotted_projection() => Sta(() =>
    {
        var (over, under, lands) = (Brushes.Red, Brushes.Green, Brushes.Orange);
        var (window, chart) = Show(Running(over, under, lands));
        try
        {
            var lines = Geometry(chart);
            var text = Texts(chart);

            // Percent axis: a gridline and label every 25%; time axis labelled at both ends.
            foreach (var p in MaLineChart.PercentTicks) Assert.Contains($"{p}%", text);
            Assert.Contains(Start.ToLocalTime().ToString("t"), text);
            Assert.Contains(End.ToLocalTime().ToString("t"), text);
            Assert.Contains(lines, g => g.Pen?.Brush == window.FindResource("MaChartGrid"));

            // Reference lines: the solid target with its label, and the dashed pace curve in MaMuted.
            Assert.Contains("99% target", text);
            Assert.Contains(lines, g => g.Pen?.Brush == Brushes.SteelBlue && g.Pen.DashStyle == DashStyles.Solid);
            Assert.Contains(lines, g => g.Geometry is StreamGeometry && g.Pen?.Brush == window.FindResource("MaMuted") && g.Pen.DashStyle == DashStyles.Dash);

            // The line: each segment in its end point's brush.
            Assert.Equal(2, lines.Count(g => g.Pen?.Brush == over && g.Pen.Thickness == 2.5));
            Assert.Single(lines, g => g.Pen?.Brush == under && g.Pen.Thickness == 2.5);

            // The projection: dotted, in its own brush, labelled; the last point's dot and label.
            Assert.Single(lines, g => g.Pen?.Brush == lands && g.Pen.DashStyle == DashStyles.Dot);
            Assert.Contains("now 70%", text);
            Assert.Contains("110% at reset", text);
            Assert.Contains(lines, g => g.Geometry is EllipseGeometry && g.Brush == window.FindResource("MaText"));
        }
        finally { window.Close(); }
    });

    [Fact]
    public void An_ended_span_draws_its_line_and_end_label_with_no_projection() => Sta(() =>
    {
        var data = Running(Brushes.Red, Brushes.Green, Brushes.Orange) with { EndLabel = "ended 70%", Projection = null };
        var (window, chart) = Show(data);
        try
        {
            var lines = Geometry(chart);
            var text = Texts(chart);
            Assert.Contains("ended 70%", text);
            Assert.DoesNotContain("110% at reset", text);
            Assert.DoesNotContain(lines, g => g.Pen?.DashStyle == DashStyles.Dot && g.Pen.Thickness == 2);
            Assert.Equal(3, lines.Count(g => g.Pen?.Thickness == 2.5));
            Assert.Contains(lines, g => g.Geometry is EllipseGeometry);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void With_no_points_it_draws_an_empty_axis() => Sta(() =>
    {
        var (window, chart) = Show(new MaLineChartData(Start, End, []));
        try
        {
            var lines = Geometry(chart);
            var text = Texts(chart);
            foreach (var p in MaLineChart.PercentTicks) Assert.Contains($"{p}%", text);
            Assert.Contains(Start.ToLocalTime().ToString("t"), text);
            Assert.DoesNotContain(lines, g => g.Pen?.Thickness == 2.5);
            Assert.DoesNotContain(lines, g => g.Geometry is EllipseGeometry);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Null_data_or_an_empty_span_draws_nothing() => Sta(() =>
    {
        var (window, chart) = Show(null);
        try
        {
            Assert.Empty(Geometry(chart));
            chart.Data = new MaLineChartData(Start, Start, [new(Start, 10)]);
            Pump();
            Assert.Empty(Geometry(chart));
        }
        finally { window.Close(); }
    });

    [Fact]
    public void A_segment_without_a_brush_is_drawn_in_McAccent() => Sta(() =>
    {
        var (window, chart) = Show(new MaLineChartData(Start, End, [new(Start, 0), new(Now, 40)], "now 40%"));
        try { Assert.Single(Geometry(chart), g => g.Pen?.Brush == window.FindResource("MaAccent") && g.Pen.Thickness == 2.5); }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData(0, 115)]
    [InlineData(99, 115)]
    [InlineData(110, 125)]
    [InlineData(400, 170)]
    public void The_percent_axis_leaves_headroom_over_the_highest_point(double highest, double top) =>
        Assert.Equal(top, MaLineChart.AxisTop(highest));

    // ---- hour ticks (M.ORCH-T464): stepped in UTC, so a clock change never stacks or drops a mark ----

    private static readonly TimeZoneInfo Central = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");

    private static string[] Marks(DateTime startUtc, DateTime endUtc, TimeZoneInfo zone) =>
        [.. MaLineChart.HourTicks(startUtc, endUtc, zone).Select(t => TimeZoneInfo.ConvertTimeFromUtc(t, zone).ToString("HH:mm", CultureInfo.InvariantCulture) + "@" + t.ToString("HH:mm", CultureInfo.InvariantCulture))];

    [Fact]
    public void Hour_marks_fall_on_every_whole_local_hour_between_the_ends()
    {
        Assert.Equal(["10:00@15:00", "11:00@16:00", "12:00@17:00", "13:00@18:00", "14:00@19:00"],
            Marks(new DateTime(2026, 10, 16, 14, 20, 0, DateTimeKind.Utc), new DateTime(2026, 10, 16, 19, 20, 0, DateTimeKind.Utc), Central));
        Assert.Equal(["11:00@16:00"],
            Marks(new DateTime(2026, 10, 16, 15, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 16, 17, 0, 0, DateTimeKind.Utc), Central));
        var india = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        Assert.Equal(["10:00@04:30", "11:00@05:30"],
            Marks(new DateTime(2026, 10, 16, 4, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 16, 6, 0, 0, DateTimeKind.Utc), india));
    }

    [Fact]
    public void Across_the_spring_forward_no_two_hour_marks_share_a_place() =>
        Assert.Equal(["00:00@06:00", "01:00@07:00", "03:00@08:00", "04:00@09:00", "05:00@10:00"],
            Marks(new DateTime(2027, 3, 14, 5, 30, 0, DateTimeKind.Utc), new DateTime(2027, 3, 14, 10, 30, 0, DateTimeKind.Utc), Central));

    [Fact]
    public void Across_the_fall_back_both_one_oclocks_are_marked() =>
        Assert.Equal(["00:00@05:00", "01:00@06:00", "01:00@07:00", "02:00@08:00", "03:00@09:00"],
            Marks(new DateTime(2026, 11, 1, 4, 30, 0, DateTimeKind.Utc), new DateTime(2026, 11, 1, 9, 30, 0, DateTimeKind.Utc), Central));

    // ---- end labels (M.ORCH-T194): the last point's label never runs into the projection's ----

    private static readonly Rect Plot = new(44, 10, 1344, 226);
    private static readonly Size NowSize = new(62, 17), EndSize = new(92, 17);

    [Fact]
    public void Far_from_the_end_the_last_points_label_sits_above_right_of_the_dot()
    {
        var (now, end) = MaLineChart.PlaceEndLabels(new Point(800, 120), NowSize, new Point(Plot.Right, 60), EndSize, Plot);
        Assert.Equal(806, now.Left);
        Assert.Equal(102, now.Top + now.Height / 2);
        Assert.NotNull(end);
    }

    [Theory]
    [InlineData(1370, 80, 70)]
    [InlineData(1380, 60, 60)]
    [InlineData(1340, 75, 65)]
    [InlineData(1387, 12, 12)]
    [InlineData(1300, 120, 118)]
    public void Near_the_end_the_two_labels_do_not_overlap(double nowX, double nowY, double endY)
    {
        var (now, end) = MaLineChart.PlaceEndLabels(new Point(nowX, nowY), NowSize, new Point(Plot.Right, endY), EndSize, Plot);
        if (end is { } r) Assert.False(now.IntersectsWith(r), $"now {now} overlaps end {r}");
        Assert.True(now.Left >= Plot.Left);
        Assert.True(now.Right <= Plot.Right + 12);
    }

    // ---- harness ----

    private static (Window, MaLineChart) Show(MaLineChartData? data)
    {
        var chart = new MaLineChart { Data = data, Height = 260 };
        var window = OffScreenWindow.Show(1400, 300, new Grid { Children = { chart } });
        Pump();
        return (window, chart);
    }

    private static List<GeometryDrawing> Geometry(MaLineChart chart) => [.. Drawings(VisualTreeHelper.GetDrawing(chart)).OfType<GeometryDrawing>()];

    private static List<string> Texts(MaLineChart chart) =>
        [.. Drawings(VisualTreeHelper.GetDrawing(chart)).OfType<GlyphRunDrawing>().Select(g => new string([.. g.GlyphRun.Characters]))];

    private static IEnumerable<Drawing> Drawings(Drawing? drawing)
    {
        if (drawing is DrawingGroup group)
        {
            foreach (var child in group.Children)
                foreach (var d in Drawings(child)) yield return d;
        }
        else if (drawing is not null) yield return drawing;
    }

    private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.SystemIdle, () => { });

    private static void Sta(Action test)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try { test(); }
            catch (Exception ex) { failure = ExceptionDispatchInfo.Capture(ex); }
            finally { Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}
