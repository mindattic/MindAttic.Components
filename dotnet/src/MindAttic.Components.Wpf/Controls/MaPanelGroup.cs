using System.Windows;
using System.Windows.Controls;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// A vertical stack of <see cref="MaPanel"/>s (M.COMP-T223): declared children in XAML, or an <c>ItemsSource</c> of
/// panels or view models, each of which gets an <see cref="MaPanel"/> as its container (style it, e.g. bind its
/// <c>Title</c>, through <c>ItemContainerStyle</c>).
/// <code>
/// &lt;ctl:MaPanelGroup&gt;
///     &lt;ctl:MaPanel Title="Sessions"&gt;...&lt;/ctl:MaPanel&gt;
///     &lt;ctl:MaPanel Title="Tickets" ctl:MaPanelGroup.FillsRemainingSpace="True"&gt;
///         &lt;ScrollViewer&gt;...&lt;/ScrollViewer&gt;
///     &lt;/ctl:MaPanel&gt;
///     &lt;ctl:MaPanel Title="Usage"&gt;...&lt;/ctl:MaPanel&gt;
/// &lt;/ctl:MaPanelGroup&gt;
/// </code>
/// <para>
/// Every panel takes its desired height except those marked <see cref="FillsRemainingSpaceProperty"/>, which share
/// the height the others leave, wherever they sit in the order. The order is <c>Items</c> (or the <c>ItemsSource</c>),
/// laid out by <see cref="MaPanelGroupPanel"/>. Dividers come from the panels themselves (each rule sits under its own
/// header), so the group draws none. A group inside an <see cref="MaPanel"/> lays out on its own.
/// </para>
/// </summary>
public class MaPanelGroup : ItemsControl
{
    /// <summary>Set on a panel in a group: it gets the height the group's other panels leave (shared evenly when
    /// several are set). Outside a group it has no effect.</summary>
    public static readonly DependencyProperty FillsRemainingSpaceProperty = DependencyProperty.RegisterAttached(
        "FillsRemainingSpace", typeof(bool), typeof(MaPanelGroup),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

    public static bool GetFillsRemainingSpace(DependencyObject element) => (bool)element.GetValue(FillsRemainingSpaceProperty);

    public static void SetFillsRemainingSpace(DependencyObject element, bool value) =>
        element.SetValue(FillsRemainingSpaceProperty, value);

    static MaPanelGroup()
    {
        FocusableProperty.OverrideMetadata(typeof(MaPanelGroup), new FrameworkPropertyMetadata(false));
        IsTabStopProperty.OverrideMetadata(typeof(MaPanelGroup), new FrameworkPropertyMetadata(false));
        var panel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(MaPanelGroupPanel)));
        panel.Seal();
        ItemsPanelProperty.OverrideMetadata(typeof(MaPanelGroup), new FrameworkPropertyMetadata(panel));
    }

    protected override bool IsItemItsOwnContainerOverride(object item) => item is MaPanel;

    protected override DependencyObject GetContainerForItemOverride() => new MaPanel();
}

/// <summary>
/// The layout behind <see cref="MaPanelGroup"/>: children stacked top to bottom in order, each at its desired height,
/// except those with <see cref="MaPanelGroup.FillsRemainingSpaceProperty"/> set, which split what is left of a finite
/// height evenly (with an unbounded height they take their desired height too). Usable as any items panel.
/// </summary>
public class MaPanelGroupPanel : Panel
{
    private static bool Fills(UIElement child) =>
        child.Visibility != Visibility.Collapsed && MaPanelGroup.GetFillsRemainingSpace(child);

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = 0, autoHeight = 0;
        var fillCount = 0;
        foreach (UIElement child in InternalChildren)
        {
            if (Fills(child)) { fillCount++; continue; }
            child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            width = Math.Max(width, child.DesiredSize.Width);
            autoHeight += child.DesiredSize.Height;
        }

        var share = double.IsInfinity(availableSize.Height) || fillCount == 0
            ? double.PositiveInfinity
            : Math.Max(0, availableSize.Height - autoHeight) / fillCount;
        var fillHeight = 0.0;
        foreach (UIElement child in InternalChildren)
        {
            if (!Fills(child)) continue;
            child.Measure(new Size(availableSize.Width, share));
            width = Math.Max(width, child.DesiredSize.Width);
            fillHeight += child.DesiredSize.Height;
        }
        return new Size(width, autoHeight + fillHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double autoHeight = 0;
        var fillCount = 0;
        foreach (UIElement child in InternalChildren)
        {
            if (Fills(child)) fillCount++;
            else autoHeight += child.DesiredSize.Height;
        }

        var share = fillCount == 0 ? 0 : Math.Max(0, finalSize.Height - autoHeight) / fillCount;
        var y = 0.0;
        foreach (UIElement child in InternalChildren)
        {
            var height = Fills(child) ? share : child.DesiredSize.Height;
            child.Arrange(new Rect(0, y, finalSize.Width, height));
            y += height;
        }
        return finalSize;
    }
}
