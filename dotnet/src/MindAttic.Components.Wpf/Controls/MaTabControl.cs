using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// A themed tab strip: a <see cref="TabControl"/> whose tabs are <see cref="MaTabItem"/>s, flat
/// labels on the chrome with the selected one lifted onto <c>MaSurface</c> and underlined in its
/// own <see cref="MaTabItem.Accent"/>. Every tab can lead with an <see cref="MaStatusLight"/>
/// (<see cref="MaTabItem.Status"/>) and end with a muted trailing text (<see cref="MaTabItem.Trailing"/>,
/// e.g. "23% left").
/// <code>
/// &lt;ctl:MaTabControl ItemsSource="{Binding People}" SelectedItem="{Binding Current}" DisplayMemberPath="Name"&gt;
///     &lt;ctl:MaTabControl.ItemContainerStyle&gt;
///         &lt;Style TargetType="ctl:MaTabItem" BasedOn="{StaticResource {x:Type ctl:MaTabItem}}"&gt;
///             &lt;Setter Property="Accent" Value="{Binding AccentBrush}" /&gt;
///             &lt;Setter Property="Status" Value="{Binding Health}" /&gt;
///             &lt;Setter Property="Trailing" Value="{Binding LeftText}" /&gt;
///         &lt;/Style&gt;
///     &lt;/ctl:MaTabControl.ItemContainerStyle&gt;
/// &lt;/ctl:MaTabControl&gt;
/// </code>
/// <para>
/// It is a real <see cref="TabControl"/>, so the keyboard and UI Automation come with it: Tab
/// enters the strip once at the selected tab, Left/Right (cycling) and Home/End move the selection,
/// Ctrl+Tab / Ctrl+Shift+Tab switch from anywhere inside, and a screen reader sees a tab list of
/// selectable tab items. A tab's trailing text and status are its UI Automation item status, so
/// they're read after its name. Set <see cref="ShowContent"/> to false for a bare strip whose
/// selection drives content shown elsewhere (e.g. terminal panes). <c>TabStripPlacement="Left"</c> turns
/// it into a vertical sidebar: the tabs stack down their own column, Up/Down move the selection, and
/// the content sits to the right. An <c>ItemContainerStyle</c>
/// must be <c>BasedOn</c> the implicit <see cref="MaTabItem"/> style to keep the look. Set
/// <see cref="AllowReorder"/> to let the user drag tabs into a new order (M.COMP-T168); mark any tab
/// that should stay put with <see cref="MaTabItem.IsLocked"/>.
/// </para>
/// </summary>
public class MaTabControl : TabControl
{
    public static readonly DependencyProperty ShowContentProperty = DependencyProperty.Register(
        nameof(ShowContent), typeof(bool), typeof(MaTabControl),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty AllowReorderProperty = DependencyProperty.Register(
        nameof(AllowReorder), typeof(bool), typeof(MaTabControl), new FrameworkPropertyMetadata(false));

    /// <summary>The template part that hosts the selected tab's content.</summary>
    public const string PartContentHost = "PART_SelectedContentHost";

    /// <summary>The template part the tabs are laid out in.</summary>
    public const string PartHeaderPanel = "PART_HeaderPanel";

    /// <summary>True (the default) shows the selected tab's content under the strip; false collapses
    /// it, leaving only the strip.</summary>
    public bool ShowContent
    {
        get => (bool)GetValue(ShowContentProperty);
        set => SetValue(ShowContentProperty, value);
    }

    /// <summary>False (the default) is a plain, immovable tab strip. True (M.COMP-T168) lets the user
    /// drag an unlocked <see cref="MaTabItem"/> (<see cref="MaTabItem.IsLocked"/> is false) to a new
    /// slot among the other unlocked items; a locked item keeps its own fixed slot and nothing can be
    /// dropped before it. This control never reorders <c>ItemsSource</c>/<c>Items</c> itself — it only
    /// raises <see cref="Reordered"/> with the intended new index; the consumer applies (and persists)
    /// the move, the same way it already owns everything else about its own data.</summary>
    public bool AllowReorder
    {
        get => (bool)GetValue(AllowReorderProperty);
        set => SetValue(AllowReorderProperty, value);
    }

    /// <summary>Raised when a drag (<see cref="AllowReorder"/>) ends over a slot other than where it
    /// started. <see cref="TabReorderEventArgs.NewIndex"/> is the index <see cref="TabReorderEventArgs.MovedItem"/>
    /// should land at among the strip's current items once removed from its old slot — the same index
    /// semantics <see cref="System.Collections.ObjectModel.ObservableCollection{T}.Move"/> already uses,
    /// so a consumer typically just calls <c>Items.Move(Items.IndexOf(e.MovedItem), e.NewIndex)</c> (or
    /// the equivalent on its own bound collection) and nothing else.</summary>
    public event EventHandler<TabReorderEventArgs>? Reordered;

    internal void RaiseReordered(object movedItem, int newIndex) => Reordered?.Invoke(this, new TabReorderEventArgs(movedItem, newIndex));

    protected override DependencyObject GetContainerForItemOverride() => new MaTabItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is TabItem;
}

/// <summary>A completed drag on an <see cref="MaTabControl"/> whose <see cref="MaTabControl.AllowReorder"/>
/// is set. See <see cref="MaTabControl.Reordered"/>.</summary>
public sealed class TabReorderEventArgs(object movedItem, int newIndex) : EventArgs
{
    public object MovedItem { get; } = movedItem;
    public int NewIndex { get; } = newIndex;
}

/// <summary>
/// One tab of an <see cref="MaTabControl"/>: its <c>Header</c>, led by an optional
/// <see cref="MaStatusLight"/> and followed by an optional trailing text, underlined in its own
/// <see cref="Accent"/> while selected. See <see cref="MaTabControl"/>.
/// </summary>
public class MaTabItem : TabItem
{
    public static readonly DependencyProperty AccentProperty = DependencyProperty.Register(
        nameof(Accent), typeof(Brush), typeof(MaTabItem),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TrailingProperty = DependencyProperty.Register(
        nameof(Trailing), typeof(string), typeof(MaTabItem),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnItemStatusPartChanged));

    public static readonly DependencyProperty TrailingForegroundProperty = DependencyProperty.Register(
        nameof(TrailingForeground), typeof(Brush), typeof(MaTabItem),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
        nameof(Status), typeof(MaStatus?), typeof(MaTabItem),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnItemStatusPartChanged));

    public static readonly DependencyProperty StatusLabelProperty = DependencyProperty.Register(
        nameof(StatusLabel), typeof(string), typeof(MaTabItem),
        new FrameworkPropertyMetadata(null, OnItemStatusPartChanged));

    public static readonly DependencyProperty IsLockedProperty = DependencyProperty.Register(
        nameof(IsLocked), typeof(bool), typeof(MaTabItem), new FrameworkPropertyMetadata(false));

    /// <summary>The template part that draws the selected underline.</summary>
    public const string PartUnderline = "PART_Underline";

    /// <summary>The template part that draws <see cref="Status"/>.</summary>
    public const string PartStatusLight = "PART_StatusLight";

    /// <summary>The template part that draws <see cref="Trailing"/>.</summary>
    public const string PartTrailing = "PART_Trailing";

    /// <summary>The tab's own colour: its underline while selected. <c>MaAccent</c> by default
    /// (from the implicit style), so set it per tab, e.g. one colour per person.</summary>
    public Brush? Accent
    {
        get => (Brush?)GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    /// <summary>Short text after the header (e.g. "23% left"); null or empty (the default) for none.
    /// It is part of the tab's UI Automation item status.</summary>
    public string? Trailing
    {
        get => (string?)GetValue(TrailingProperty);
        set => SetValue(TrailingProperty, value);
    }

    /// <summary>The trailing text's colour: <c>MaMuted</c> by default (from the implicit style); set
    /// e.g. <c>MaBad</c> when a limit is nearly gone.</summary>
    public Brush? TrailingForeground
    {
        get => (Brush?)GetValue(TrailingForegroundProperty);
        set => SetValue(TrailingForegroundProperty, value);
    }

    /// <summary>A status light before the header; null (the default) for none.</summary>
    public MaStatus? Status
    {
        get => (MaStatus?)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    /// <summary>The status light's words (its <see cref="MaStatusLight.Label"/>); null uses the
    /// status word.</summary>
    public string? StatusLabel
    {
        get => (string?)GetValue(StatusLabelProperty);
        set => SetValue(StatusLabelProperty, value);
    }

    /// <summary>False (the default) is an ordinary tab: when the owning <see cref="MaTabControl.AllowReorder"/>
    /// is set, it can be dragged, and other tabs can land before or after it. True (M.COMP-T168) keeps
    /// this tab in its own fixed slot: it never starts a drag, and nothing can be dropped before it. Only
    /// a contiguous run of locked tabs at the front of the strip is exercised by this control's tests;
    /// it's offered as a general primitive, not because any one topology beyond that was asked for.</summary>
    public bool IsLocked
    {
        get => (bool)GetValue(IsLockedProperty);
        set => SetValue(IsLockedProperty, value);
    }

    /// <summary>The item status this tab last set on itself, so one the host sets is never overwritten.</summary>
    private string? ownItemStatus;

    // ---- drag-to-reorder (M.COMP-T168) ------------------------------------------------------------

    private Point? dragStartPoint;
    private Point grabOffset;
    private bool isDragging;
    private int dragStartIndex;
    private AdornerLayer? dragAdornerLayer;
    private DragGhostAdorner? dragAdorner;

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);
        if (ItemsControl.ItemsControlFromItemContainer(this) is MaTabControl owner)
            HandleMouseDown(owner, e.GetPosition(owner), e.GetPosition(this));
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        base.OnPreviewMouseMove(e);
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (ItemsControl.ItemsControlFromItemContainer(this) is MaTabControl owner)
            HandleMouseMove(owner, e.GetPosition(owner), e.GetPosition(this));
    }

    protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonUp(e);
        if (ItemsControl.ItemsControlFromItemContainer(this) is MaTabControl owner && HeaderPanel(owner) is { } panel)
            HandleMouseUp(owner, e.GetPosition(panel));
        else
            EndDrag(null, default, commit: false);
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        EndDrag(null, default, commit: false);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (!isDragging || e.Key != Key.Escape) return;
        EndDrag(null, default, commit: false);
        e.Handled = true;
    }

    /// <summary>Starts tracking a possible drag from <paramref name="ownerPosition"/> (this item's position
    /// within <paramref name="owner"/>'s coordinate space); a no-op when locked or <see cref="MaTabControl.AllowReorder"/>
    /// is off. Takes plain points rather than a <see cref="MouseEventArgs"/> so the whole gesture (this, <see
    /// cref="HandleMouseMove"/>, <see cref="HandleMouseUp"/>) is directly unit-testable: <see
    /// cref="MouseEventArgs.GetPosition"/> reads real OS cursor state and can't be driven from a unit test,
    /// so the three protected mouse overrides read it once each and hand the plain <see cref="Point"/> in
    /// here — this method and the two below never touch <c>MouseEventArgs</c> themselves.</summary>
    internal void HandleMouseDown(MaTabControl owner, Point ownerPosition, Point selfPosition)
    {
        if (IsLocked || !owner.AllowReorder) return;
        dragStartPoint = ownerPosition;
        grabOffset = selfPosition;
    }

    /// <summary>Continues a drag <see cref="HandleMouseDown"/> started: begins it for real once the pointer
    /// has moved past the platform's drag threshold, then keeps the ghost under the pointer.</summary>
    internal void HandleMouseMove(MaTabControl owner, Point ownerPosition, Point selfPosition)
    {
        if (dragStartPoint is not { } start) return;
        var vertical = owner.TabStripPlacement is Dock.Left or Dock.Right;
        var moved = vertical ? Math.Abs(ownerPosition.Y - start.Y) : Math.Abs(ownerPosition.X - start.X);
        var threshold = vertical ? SystemParameters.MinimumVerticalDragDistance : SystemParameters.MinimumHorizontalDragDistance;
        if (!isDragging)
        {
            if (moved < threshold) return;
            BeginDrag(owner);
        }

        if (dragAdorner is null) return;
        dragAdorner.OffsetX = selfPosition.X - grabOffset.X;
        dragAdorner.OffsetY = selfPosition.Y - grabOffset.Y;
        dragAdornerLayer?.Update();
    }

    /// <summary>Ends the drag over <paramref name="panelPosition"/> (the pointer's position within the
    /// header panel) and, if that resolves to a different slot than it started in, raises <see
    /// cref="MaTabControl.Reordered"/>.</summary>
    internal void HandleMouseUp(MaTabControl owner, Point panelPosition) => EndDrag(owner, panelPosition, commit: true);

    private void BeginDrag(MaTabControl owner)
    {
        isDragging = true;
        dragStartIndex = owner.ItemContainerGenerator.IndexFromContainer(this);
        CaptureMouse();
        if (HeaderPanel(owner) is not { } panel) return;
        dragAdornerLayer = AdornerLayer.GetAdornerLayer(panel);
        if (dragAdornerLayer is null) return;
        dragAdorner = new DragGhostAdorner(this);
        dragAdornerLayer.Add(dragAdorner);
    }

    /// <summary>Ends the gesture started in <see cref="BeginDrag"/>; a no-op when no drag is in progress
    /// (every exit path, including a plain click's own mouse-up, funnels through here). <paramref name="commit"/>
    /// is false for Esc-cancel or an externally stolen mouse capture — those tear down the adorner without
    /// raising <see cref="MaTabControl.Reordered"/>; <paramref name="owner"/>/<paramref name="panelPosition"/>
    /// are only read when <paramref name="commit"/> is true.</summary>
    private void EndDrag(MaTabControl? owner, Point panelPosition, bool commit)
    {
        dragStartPoint = null;
        if (!isDragging) return;
        isDragging = false;

        if (dragAdornerLayer is not null && dragAdorner is not null) dragAdornerLayer.Remove(dragAdorner);
        dragAdornerLayer = null;
        dragAdorner = null;
        if (IsMouseCaptured) ReleaseMouseCapture();

        if (!commit || owner is null) return;
        if (HeaderPanel(owner) is not { } panel) return;

        var vertical = owner.TabStripPlacement is Dock.Left or Dock.Right;
        var pointer = vertical ? panelPosition.Y : panelPosition.X;
        var midpoints = new List<double>();
        var locks = new List<bool>();
        foreach (var child in panel.Children)
        {
            if (child is not MaTabItem item) continue;
            var topLeft = item.TranslatePoint(new Point(0, 0), panel);
            midpoints.Add(vertical ? topLeft.Y + item.ActualHeight / 2 : topLeft.X + item.ActualWidth / 2);
            locks.Add(item.IsLocked);
        }

        var newIndex = ComputeDropIndex(midpoints, locks, dragStartIndex, pointer);
        if (newIndex != dragStartIndex) owner.RaiseReordered(DataContext ?? (object)this, newIndex);
    }

    private static Panel? HeaderPanel(MaTabControl owner) => owner.Template?.FindName(MaTabControl.PartHeaderPanel, owner) as Panel;

    /// <summary>Where a drag should drop, kept as pure data-in/data-out so it's directly unit-testable:
    /// <see cref="MouseEventArgs.GetPosition"/> reads real device state and can't be driven from a unit
    /// test, but this takes already-measured midpoints instead. Counts how many of the OTHER items'
    /// midpoints sit before <paramref name="pointerCoordinate"/> (the same index <see
    /// cref="System.Collections.ObjectModel.ObservableCollection{T}.Move"/> expects: a position in the
    /// list with the dragged item already removed), then raises that to however many items are locked at
    /// the front of the strip, since a locked item keeps its own slot and nothing may land before it.</summary>
    internal static int ComputeDropIndex(IReadOnlyList<double> midpoints, IReadOnlyList<bool> isLocked, int draggedIndex, double pointerCoordinate)
    {
        var count = 0;
        for (var i = 0; i < midpoints.Count; i++)
        {
            if (i == draggedIndex) continue;
            if (midpoints[i] < pointerCoordinate) count++;
        }

        var minIndex = 0;
        for (var i = 0; i < isLocked.Count && isLocked[i]; i++)
        {
            if (i == draggedIndex) continue;
            minIndex++;
        }

        return Math.Max(count, minIndex);
    }

    /// <summary>The floating, semi-transparent copy of a tab shown while it's dragged
    /// (<see cref="MaTabControl.AllowReorder"/>): a live <see cref="VisualBrush"/> of the item itself, so
    /// it looks exactly like the real tab, translated to follow the pointer.</summary>
    private sealed class DragGhostAdorner : Adorner
    {
        private readonly VisualBrush brush;
        private readonly double width;
        private readonly double height;

        public double OffsetX { get; set; }
        public double OffsetY { get; set; }

        public DragGhostAdorner(UIElement adorned) : base(adorned)
        {
            brush = new VisualBrush(adorned) { Opacity = 0.7 };
            width = ((FrameworkElement)adorned).ActualWidth;
            height = ((FrameworkElement)adorned).ActualHeight;
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext drawingContext) =>
            drawingContext.DrawRectangle(brush, null, new Rect(OffsetX, OffsetY, width, height));
    }

    private static void OnItemStatusPartChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((MaTabItem)d).UpdateItemStatus();

    /// <summary>The status words then the trailing text, comma separated, e.g. "Good, 23% left".</summary>
    internal string ComposeItemStatus()
    {
        var parts = new List<string>(2);
        if (Status is { } status) parts.Add(string.IsNullOrEmpty(StatusLabel) ? MaStatusLight.Describe(status) : StatusLabel!);
        if (!string.IsNullOrEmpty(Trailing)) parts.Add(Trailing!);
        return string.Join(", ", parts);
    }

    private void UpdateItemStatus()
    {
        var local = ReadLocalValue(AutomationProperties.ItemStatusProperty);
        if (local != DependencyProperty.UnsetValue && !Equals(local, ownItemStatus)) return; // the host's own
        ownItemStatus = ComposeItemStatus();
        if (ownItemStatus.Length == 0)
        {
            ownItemStatus = null;
            ClearValue(AutomationProperties.ItemStatusProperty);
        }
        else
        {
            SetValue(AutomationProperties.ItemStatusProperty, ownItemStatus);
        }
    }
}
