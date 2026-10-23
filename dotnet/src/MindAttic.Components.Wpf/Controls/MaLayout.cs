using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// The settings of the theme's list styles for an <see cref="ItemsControl"/> (M.COMP-T52), set on the list itself:
/// <c>MaList</c> (a plain vertical list), <c>MaRowList</c> (vertical rows, <see cref="SpacingProperty"/> apart) and
/// <c>MaTileList</c> (a wrap of tiles laid out by an <see cref="MaTileGrid"/>).
/// <code>
/// &lt;ItemsControl Style="{StaticResource MaRowList}" ItemsSource="{Binding Tickets}" /&gt;
/// &lt;ItemsControl Style="{StaticResource MaTileList}" ItemsSource="{Binding Repos}" ctl:MaList.MinItemWidth="240" ctl:MaList.UniformRows="True" /&gt;
/// </code>
/// The list owns the gaps, so its items carry no margin of their own: use the margin-less card styles
/// (<c>MaListCard</c>, <c>MaRowCard</c>, <c>MaAccentCard</c>) in its item template, not <c>MaCard</c>.
/// </summary>
public static class MaList
{
    /// <summary>The gap between two items (two rows of <c>MaRowList</c>, two columns of <c>MaTileList</c>).</summary>
    public static readonly DependencyProperty SpacingProperty = DependencyProperty.RegisterAttached(
        "Spacing", typeof(double), typeof(MaList), new FrameworkPropertyMetadata(0d));

    /// <summary><c>MaTileList</c>'s gap between two rows of tiles.</summary>
    public static readonly DependencyProperty RowSpacingProperty = DependencyProperty.RegisterAttached(
        "RowSpacing", typeof(double), typeof(MaList), new FrameworkPropertyMetadata(0d));

    /// <summary><c>MaTileList</c>'s narrowest column (see <see cref="MaTileGrid.MinItemWidth"/>): 0 or NaN (the default)
    /// flows the tiles at their own widths like a <see cref="WrapPanel"/>.</summary>
    public static readonly DependencyProperty MinItemWidthProperty = DependencyProperty.RegisterAttached(
        "MinItemWidth", typeof(double), typeof(MaList), new FrameworkPropertyMetadata(double.NaN));

    /// <summary><c>MaTileList</c>: every row as tall as the tallest tile.</summary>
    public static readonly DependencyProperty UniformRowsProperty = DependencyProperty.RegisterAttached(
        "UniformRows", typeof(bool), typeof(MaList), new FrameworkPropertyMetadata(false));

    /// <summary><c>MaTileList</c>: spread the tiles evenly over the fewest rows (8 that don't fit one row: 4 and 4).</summary>
    public static readonly DependencyProperty BalancedProperty = DependencyProperty.RegisterAttached(
        "Balanced", typeof(bool), typeof(MaList), new FrameworkPropertyMetadata(false));

    public static double GetSpacing(DependencyObject o) => (double)o.GetValue(SpacingProperty);
    public static void SetSpacing(DependencyObject o, double value) => o.SetValue(SpacingProperty, value);
    public static double GetRowSpacing(DependencyObject o) => (double)o.GetValue(RowSpacingProperty);
    public static void SetRowSpacing(DependencyObject o, double value) => o.SetValue(RowSpacingProperty, value);
    public static double GetMinItemWidth(DependencyObject o) => (double)o.GetValue(MinItemWidthProperty);
    public static void SetMinItemWidth(DependencyObject o, double value) => o.SetValue(MinItemWidthProperty, value);
    public static bool GetUniformRows(DependencyObject o) => (bool)o.GetValue(UniformRowsProperty);
    public static void SetUniformRows(DependencyObject o, bool value) => o.SetValue(UniformRowsProperty, value);
    public static bool GetBalanced(DependencyObject o) => (bool)o.GetValue(BalancedProperty);
    public static void SetBalanced(DependencyObject o, bool value) => o.SetValue(BalancedProperty, value);
}

/// <summary>
/// A <see cref="StackPanel"/> with a <see cref="Spacing"/> between its children (none after the last, and none
/// around a collapsed child). <c>MaList</c> and <c>MaRowList</c> lay their items out in one (M.COMP-T52).
/// </summary>
public class MaStackPanel : Panel
{
    public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register(
        nameof(Spacing), typeof(double), typeof(MaStackPanel),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation), typeof(Orientation), typeof(MaStackPanel),
        new FrameworkPropertyMetadata(Orientation.Vertical, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>The gap between two visible children.</summary>
    public double Spacing { get => (double)GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }

    /// <summary>Vertical (the default) stacks top to bottom; Horizontal left to right.</summary>
    public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    protected override Size MeasureOverride(Size available)
    {
        var vertical = Orientation == Orientation.Vertical;
        var slot = vertical ? new Size(available.Width, double.PositiveInfinity) : new Size(double.PositiveInfinity, available.Height);
        double along = 0, across = 0;
        var shown = 0;
        foreach (UIElement child in InternalChildren)
        {
            child.Measure(slot);
            if (child.Visibility == Visibility.Collapsed) continue;
            var size = child.DesiredSize;
            along += vertical ? size.Height : size.Width;
            across = Math.Max(across, vertical ? size.Width : size.Height);
            shown++;
        }
        along += Math.Max(0, shown - 1) * Spacing;
        return vertical ? new Size(across, along) : new Size(along, across);
    }

    protected override Size ArrangeOverride(Size final)
    {
        var vertical = Orientation == Orientation.Vertical;
        double at = 0;
        foreach (UIElement child in InternalChildren)
        {
            if (child.Visibility == Visibility.Collapsed) continue;
            var size = child.DesiredSize;
            if (vertical)
            {
                child.Arrange(new Rect(0, at, final.Width, size.Height));
                at += size.Height + Spacing;
            }
            else
            {
                child.Arrange(new Rect(at, 0, size.Width, final.Height));
                at += size.Width + Spacing;
            }
        }
        return final;
    }
}

/// <summary>
/// A grid of tiles (M.COMP-T52, from the Orchestrator's AlignedGrid). With a <see cref="MinItemWidth"/>, its children
/// sit in equal columns that fill the width exactly: every column the same width, <see cref="Spacing"/> between two,
/// the first on the left edge and the last ending on the right, so nothing leaves a ragged strip and a resize reflows
/// in step (with <see cref="Balanced"/>, the rows are filled evenly). With no <see cref="MinItemWidth"/> (0 or NaN),
/// they flow at their own widths and wrap like a <see cref="WrapPanel"/>. A row is as tall as its tallest child, or
/// every row as tall as the tallest with <see cref="UniformRows"/>; <see cref="RowSpacing"/> separates rows.
/// <c>MaTileList</c> lays its items out in one, set from <see cref="MaList"/>; it also works on its own (a row of stat tiles).
/// </summary>
public class MaTileGrid : Panel
{
    public static readonly DependencyProperty MinItemWidthProperty = DependencyProperty.Register(
        nameof(MinItemWidth), typeof(double), typeof(MaTileGrid),
        new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register(
        nameof(Spacing), typeof(double), typeof(MaTileGrid),
        new FrameworkPropertyMetadata(10d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty RowSpacingProperty = DependencyProperty.Register(
        nameof(RowSpacing), typeof(double), typeof(MaTileGrid),
        new FrameworkPropertyMetadata(10d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty BalancedProperty = DependencyProperty.Register(
        nameof(Balanced), typeof(bool), typeof(MaTileGrid),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty UniformRowsProperty = DependencyProperty.Register(
        nameof(UniformRows), typeof(bool), typeof(MaTileGrid),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>The narrowest a column may get before the grid drops to fewer columns; 0 or NaN (the default) flows the
    /// children at their own widths instead.</summary>
    public double MinItemWidth { get => (double)GetValue(MinItemWidthProperty); set => SetValue(MinItemWidthProperty, value); }

    /// <summary>The space between two columns (10).</summary>
    public double Spacing { get => (double)GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }

    /// <summary>The space between two rows (10).</summary>
    public double RowSpacing { get => (double)GetValue(RowSpacingProperty); set => SetValue(RowSpacingProperty, value); }

    /// <summary>Equal columns only: spread the children evenly over the fewest rows instead of filling each row first.</summary>
    public bool Balanced { get => (bool)GetValue(BalancedProperty); set => SetValue(BalancedProperty, value); }

    /// <summary>Make every row as tall as the tallest child.</summary>
    public bool UniformRows { get => (bool)GetValue(UniformRowsProperty); set => SetValue(UniformRowsProperty, value); }

    private bool Columns => !double.IsNaN(MinItemWidth) && MinItemWidth > 0;

    /// <summary>How many equal columns fit <paramref name="count"/> children in <paramref name="width"/>.</summary>
    public static int ColumnsFor(double width, int count, double minItemWidth, double spacing, bool balanced)
    {
        if (count <= 0) return 1;
        var fit = double.IsInfinity(width) || double.IsNaN(width)
            ? count
            : (int)Math.Floor((width + spacing) / (Math.Max(1, minItemWidth) + spacing));
        fit = Math.Max(1, fit);
        if (!balanced) return fit;
        var columns = Math.Min(fit, count);
        var rows = (count + columns - 1) / columns;
        return (count + rows - 1) / rows;
    }

    /// <summary>The width of one column when <paramref name="columns"/> of them and their gaps fill <paramref name="width"/>.</summary>
    public static double ItemWidth(double width, int columns, double spacing) => Math.Max(0, (width - (columns - 1) * spacing) / columns);

    private List<UIElement> Visible()
    {
        var shown = new List<UIElement>();
        foreach (UIElement child in InternalChildren)
        {
            if (child.Visibility == Visibility.Collapsed) child.Measure(default);
            else shown.Add(child);
        }
        return shown;
    }

    protected override Size MeasureOverride(Size available)
    {
        var children = Visible();
        if (children.Count == 0) return new Size(0, 0);

        if (!Columns || double.IsInfinity(available.Width))
        {
            foreach (var child in children) child.Measure(new Size(available.Width, double.PositiveInfinity));
            var rows = FlowRows(children, available.Width);
            var heights = Heights(rows);
            var width = rows.Max(r => r.Sum(c => c.DesiredSize.Width) + (r.Count - 1) * Spacing);
            return new Size(Math.Min(width, available.Width), heights.Sum() + (heights.Count - 1) * RowSpacing);
        }

        var columns = ColumnsFor(available.Width, children.Count, MinItemWidth, Spacing, Balanced);
        var itemWidth = ItemWidth(available.Width, columns, Spacing);
        foreach (var child in children) child.Measure(new Size(itemWidth, double.PositiveInfinity));
        var gridHeights = Heights(Chunk(children, columns));
        return new Size(available.Width, gridHeights.Sum() + (gridHeights.Count - 1) * RowSpacing);
    }

    protected override Size ArrangeOverride(Size final)
    {
        var children = Visible();
        if (children.Count == 0) return final;

        if (!Columns || double.IsInfinity(final.Width) || final.Width <= 0)
        {
            var rows = FlowRows(children, final.Width);
            var heights = Heights(rows);
            double y = 0;
            for (var r = 0; r < rows.Count; r++)
            {
                double x = 0;
                foreach (var child in rows[r])
                {
                    var width = Math.Min(child.DesiredSize.Width, Math.Max(0, final.Width - x));
                    child.Arrange(new Rect(x, y, width, heights[r]));
                    x += child.DesiredSize.Width + Spacing;
                }
                y += heights[r] + RowSpacing;
            }
            return final;
        }

        var columns = ColumnsFor(final.Width, children.Count, MinItemWidth, Spacing, Balanced);
        var itemWidth = ItemWidth(final.Width, columns, Spacing);
        var gridRows = Chunk(children, columns);
        var gridHeights = Heights(gridRows);
        double top = 0;
        for (var r = 0; r < gridRows.Count; r++)
        {
            for (var c = 0; c < gridRows[r].Count; c++)
            {
                // The last column ends exactly on the right edge, so rounding never leaves a sliver.
                var x = c * (itemWidth + Spacing);
                var width = c == columns - 1 ? final.Width - x : itemWidth;
                gridRows[r][c].Arrange(new Rect(x, top, Math.Max(0, width), gridHeights[r]));
            }
            top += gridHeights[r] + RowSpacing;
        }
        return final;
    }

    private static List<List<UIElement>> Chunk(List<UIElement> children, int columns) =>
        children.Select((c, i) => (c, i)).GroupBy(p => p.i / columns).Select(g => g.Select(p => p.c).ToList()).ToList();

    private List<List<UIElement>> FlowRows(List<UIElement> children, double width)
    {
        var rows = new List<List<UIElement>> { new() };
        double x = 0;
        foreach (var child in children)
        {
            var w = child.DesiredSize.Width;
            if (rows[^1].Count > 0 && x + w > width + 0.5)
            {
                rows.Add([]);
                x = 0;
            }
            rows[^1].Add(child);
            x += w + Spacing;
        }
        return rows;
    }

    private List<double> Heights(List<List<UIElement>> rows)
    {
        var heights = rows.Select(r => r.Max(c => c.DesiredSize.Height)).ToList();
        if (UniformRows && heights.Count > 0)
        {
            var tallest = heights.Max();
            for (var r = 0; r < heights.Count; r++) heights[r] = tallest;
        }
        return heights;
    }
}

/// <summary>
/// The body scroll area's gutter (M.COMP-T52): the <c>MaBodyScroll</c> and <c>MaPaneScroll</c> styles pad their content
/// by <see cref="GutterProperty"/>, and while the vertical scroll bar shows, the bar sits inside the right gutter
/// (that side shrinks by <see cref="ScrollBarWidth"/>) instead of pushing the content in, so a page's cards end where
/// its header and toolbar end whether or not it scrolls.
/// <code>
/// &lt;ScrollViewer Style="{StaticResource MaBodyScroll}"&gt;...&lt;/ScrollViewer&gt;
/// &lt;ScrollViewer Style="{StaticResource MaBodyScroll}" ctl:MaScrollArea.Gutter="16,0,16,12"&gt;...&lt;/ScrollViewer&gt;
/// </code>
/// Set the gutter, not <c>Padding</c> (which the style computes from it).
/// </summary>
public static class MaScrollArea
{
    /// <summary>A vertical scroll bar's width: the theme's MaScrollBar, which every ScrollViewer takes.</summary>
    public const double ScrollBarWidth = 10;

    /// <summary>The padding around the scrolled content while no scroll bar shows: <c>MaBodyScroll</c> 24,0,24,16 (the
    /// window's 24px side gutters), <c>MaPaneScroll</c> 20,16,20,20.</summary>
    public static readonly DependencyProperty GutterProperty = DependencyProperty.RegisterAttached(
        "Gutter", typeof(Thickness), typeof(MaScrollArea), new FrameworkPropertyMetadata(new Thickness(24, 0, 24, 16)));

    public static Thickness GetGutter(DependencyObject o) => (Thickness)o.GetValue(GutterProperty);
    public static void SetGutter(DependencyObject o, Thickness value) => o.SetValue(GutterProperty, value);

    /// <summary>The padding for <paramref name="gutter"/> while the vertical bar is <paramref name="bar"/>: the gutter,
    /// its right side less the bar's width while the bar shows.</summary>
    public static Thickness PaddingFor(Thickness gutter, Visibility bar) => bar == Visibility.Visible
        ? new Thickness(gutter.Left, gutter.Top, Math.Max(0, gutter.Right - ScrollBarWidth), gutter.Bottom)
        : gutter;

    /// <summary>(Gutter, ComputedVerticalScrollBarVisibility) to Padding, for the scroll styles.</summary>
    public static IMultiValueConverter PaddingConverter { get; } = new GutterPadding();

    private sealed class GutterPadding : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
            PaddingFor(values.Length > 0 && values[0] is Thickness t ? t : new Thickness(24, 0, 24, 16),
                values.Length > 1 && values[1] is Visibility v ? v : Visibility.Collapsed);

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}

/// <summary>
/// A tile's chosen state (M.COMP-T52), for every tile on <c>MaTileButton</c>: <c>MaStatCard</c>, <c>MaSmallStat</c> and
/// <see cref="MaAccentTile"/>. While <see cref="IsSelectedProperty"/> is true the tile is outlined 2px in
/// <see cref="SelectedBrushProperty"/> (<c>MaFocus</c> by default; e.g. <c>MaWarn</c> on the "Need you" stat while it
/// filters the page) and its UI Automation item status is "Selected", so the state is never colour alone.
/// <code>
/// &lt;Button Style="{StaticResource MaSmallStat}" ctl:MaTile.IsSelected="{Binding IsWaitingFocus}" ctl:MaTile.SelectedBrush="{DynamicResource MaWarn}" /&gt;
/// </code>
/// </summary>
public static class MaTile
{
    /// <summary>The item status a selected tile reads as.</summary>
    public const string SelectedStatus = "Selected";

    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.RegisterAttached(
        "IsSelected", typeof(bool), typeof(MaTile), new FrameworkPropertyMetadata(false, OnIsSelectedChanged));

    public static readonly DependencyProperty SelectedBrushProperty = DependencyProperty.RegisterAttached(
        "SelectedBrush", typeof(Brush), typeof(MaTile), new FrameworkPropertyMetadata(null));

    public static bool GetIsSelected(DependencyObject o) => (bool)o.GetValue(IsSelectedProperty);
    public static void SetIsSelected(DependencyObject o, bool value) => o.SetValue(IsSelectedProperty, value);
    public static Brush? GetSelectedBrush(DependencyObject o) => (Brush?)o.GetValue(SelectedBrushProperty);
    public static void SetSelectedBrush(DependencyObject o, Brush? value) => o.SetValue(SelectedBrushProperty, value);

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var local = d.ReadLocalValue(AutomationProperties.ItemStatusProperty);
        if (local != DependencyProperty.UnsetValue && !Equals(local, SelectedStatus)) return; // the host's own
        if ((bool)e.NewValue) d.SetValue(AutomationProperties.ItemStatusProperty, SelectedStatus);
        else d.ClearValue(AutomationProperties.ItemStatusProperty);
    }
}
