using System.Collections;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
/// <para>
/// Panels marked <see cref="IsDraggableProperty"/> can be reordered (M.COMP-T224) with <see cref="MovePanel"/> or
/// Alt+Up/Down on their header; the others keep fixed slots. <see cref="OrderChanged"/> reports each move with every
/// panel's <see cref="PanelKeyProperty"/> so a host can save the order.
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

    /// <summary>Set on a panel in a group (M.COMP-T224): it can be moved by <see cref="MovePanel"/> and Alt+Up/Down on
    /// its header. False (the default) pins the panel to its own fixed slot, which no other panel can land in.</summary>
    public static readonly DependencyProperty IsDraggableProperty = DependencyProperty.RegisterAttached(
        "IsDraggable", typeof(bool), typeof(MaPanelGroup), new FrameworkPropertyMetadata(false));

    public static bool GetIsDraggable(DependencyObject element) => (bool)element.GetValue(IsDraggableProperty);

    public static void SetIsDraggable(DependencyObject element, bool value) => element.SetValue(IsDraggableProperty, value);

    /// <summary>Set on a panel in a group: the stable id a host saves the group's order by (see
    /// <see cref="PanelOrderChangedEventArgs.PanelKeys"/>).</summary>
    public static readonly DependencyProperty PanelKeyProperty = DependencyProperty.RegisterAttached(
        "PanelKey", typeof(string), typeof(MaPanelGroup), new FrameworkPropertyMetadata(null));

    public static string? GetPanelKey(DependencyObject element) => (string?)element.GetValue(PanelKeyProperty);

    public static void SetPanelKey(DependencyObject element, string? value) => element.SetValue(PanelKeyProperty, value);

    private static readonly DependencyPropertyKey LastAnnouncementPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(LastAnnouncement), typeof(string), typeof(MaPanelGroup), new FrameworkPropertyMetadata(string.Empty));

    /// <summary>The read-only <see cref="LastAnnouncement"/> property.</summary>
    public static readonly DependencyProperty LastAnnouncementProperty = LastAnnouncementPropertyKey.DependencyProperty;

    /// <summary>The <see cref="Order"/> property; binds two-way by default.</summary>
    public static readonly DependencyProperty OrderProperty = DependencyProperty.Register(
        nameof(Order), typeof(IList<string>), typeof(MaPanelGroup),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnOrderPropertyChanged));

    static MaPanelGroup()
    {
        FocusableProperty.OverrideMetadata(typeof(MaPanelGroup), new FrameworkPropertyMetadata(false));
        IsTabStopProperty.OverrideMetadata(typeof(MaPanelGroup), new FrameworkPropertyMetadata(false));
        var panel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(MaPanelGroupPanel)));
        panel.Seal();
        ItemsPanelProperty.OverrideMetadata(typeof(MaPanelGroup), new FrameworkPropertyMetadata(panel));
    }

    public MaPanelGroup()
    {
        OrderChanged += (_, e) => SyncOrder(e.PanelKeys.OfType<string>().ToList());
        Loaded += (_, _) => ReapplyOrder();
        ItemContainerGenerator.StatusChanged += (_, _) =>
        {
            if (ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                ReapplyOrder();
        };
    }

    /// <summary>Raised after a panel moves. With an <c>ItemsSource</c> that is not an <see cref="IList"/> the group
    /// cannot move the item itself: it raises this with the intended order for the host to apply, as
    /// <see cref="MaTabControl.Reordered"/> does.</summary>
    public event EventHandler<PanelOrderChangedEventArgs>? OrderChanged;

    /// <summary>The text most recently sent to screen readers by a keyboard move.</summary>
    public string LastAnnouncement => (string)GetValue(LastAnnouncementProperty);

    /// <summary>
    /// The host's persistence hook (M.COMP-T226): the panels' <see cref="PanelKeyProperty">keys</see> in order. Setting
    /// it (or calling <see cref="ApplyOrder"/>) rearranges the draggable panels to match, and it is kept as given, so a
    /// panel that arrives later (a container generated, an item added) is placed by it too. Each move by the user
    /// replaces it with the group's new order, before <see cref="OrderChanged"/> reaches the host's handlers. Bind it
    /// to a saved <c>List&lt;string&gt;</c>. The group does not store collapse state: hosts save each panel's
    /// <see cref="MaPanel.IsExpanded"/> per <see cref="PanelKeyProperty"/> themselves.
    /// </summary>
    public IList<string>? Order
    {
        get => (IList<string>?)GetValue(OrderProperty);
        set => SetValue(OrderProperty, value);
    }

    /// <summary>
    /// Moves a draggable <paramref name="panel"/> to <paramref name="newIndex"/> in the group's order. Panels that are
    /// not <see cref="IsDraggableProperty">draggable</see> keep their own slots, so the draggable ones shift around
    /// them; a move onto a fixed slot, of a fixed panel, or out of range does nothing. Declared children and an
    /// <see cref="IList"/> <c>ItemsSource</c> are reordered in place; any other <c>ItemsSource</c> is left to the host.
    /// Returns whether the order changed (or, for a host-applied source, whether <see cref="OrderChanged"/> was raised).
    /// </summary>
    public bool MovePanel(MaPanel panel, int newIndex)
    {
        var item = ItemContainerGenerator.ItemFromContainer(panel);
        if (item == DependencyProperty.UnsetValue) item = panel;
        var oldIndex = Items.IndexOf(item);
        if (oldIndex < 0 || newIndex < 0 || newIndex >= Items.Count || newIndex == oldIndex) return false;
        if (!GetIsDraggable(panel)) return false;

        var slots = new List<int>();
        for (var i = 0; i < Items.Count; i++)
            if (i == oldIndex || PanelAt(i) is { } p && GetIsDraggable(p)) slots.Add(i);
        var newSlot = slots.IndexOf(newIndex);
        if (newSlot < 0) return false;

        var order = Items.Cast<object>().ToList();
        var moving = slots.Select(i => order[i]).ToList();
        moving.RemoveAt(slots.IndexOf(oldIndex));
        moving.Insert(newSlot, item);
        for (var s = 0; s < slots.Count; s++) order[slots[s]] = moving[s];

        var keys = order.Select(KeyOf).ToList();
        if (ItemsSource is null) Apply(Items, order);
        else if (ItemsSource is IList list && !list.IsFixedSize && !list.IsReadOnly) Apply(list, order);

        OrderChanged?.Invoke(this, new PanelOrderChangedEventArgs(item, oldIndex, newIndex, keys));
        return true;
    }

    /// <summary>Alt+Up / Alt+Down on a draggable panel's header: moves it to the neighbouring slot it may take,
    /// skipping fixed panels, and announces the new position. Returns whether it moved.</summary>
    internal bool HandleReorderKey(object? originalSource, Key key, ModifierKeys modifiers)
    {
        if (modifiers != ModifierKeys.Alt || key is not (Key.Up or Key.Down)) return false;
        if (PanelOwningHeader(originalSource as DependencyObject) is not { } panel || !GetIsDraggable(panel)) return false;

        var item = ItemContainerGenerator.ItemFromContainer(panel);
        if (item == DependencyProperty.UnsetValue) item = panel;
        var step = key == Key.Up ? -1 : 1;
        for (var i = Items.IndexOf(item) + step; i >= 0 && i < Items.Count; i += step)
        {
            if (PanelAt(i) is not { } other || !GetIsDraggable(other)) continue;
            if (!MovePanel(panel, i)) return false;
            Announce(panel, i);
            UpdateLayout();
            if (ContainerFor(item) is { } moved && moved.Template?.FindName(MaPanel.PartHeader, moved) is UIElement header)
                header.Focus();
            return true;
        }
        return false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled) return;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (HandleReorderKey(e.OriginalSource, key, Keyboard.Modifiers)) e.Handled = true;
    }

    private void Announce(MaPanel panel, int index)
    {
        var name = AutomationProperties.GetName(panel) is { Length: > 0 } n ? n : panel.Title as string;
        var text = $"{(string.IsNullOrEmpty(name) ? "Panel" : name)} moved to position {index + 1} of {Items.Count}";
        SetValue(LastAnnouncementPropertyKey, text);
        (UIElementAutomationPeer.FromElement(this) ?? UIElementAutomationPeer.CreatePeerForElement(this))
            ?.RaiseNotificationEvent(AutomationNotificationKind.ActionCompleted,
                AutomationNotificationProcessing.MostRecent, text, "MaPanelGroup.Reorder");
    }

    /// <summary>The panel of this group whose header contains <paramref name="source"/>.</summary>
    private MaPanel? PanelOwningHeader(DependencyObject? source)
    {
        for (var node = source; node is not null && node != this; node = ParentOf(node))
        {
            if (node is not MaPanel panel || ItemsControlFromItemContainer(panel) != this) continue;
            var header = panel.Template?.FindName(MaPanel.PartHeader, panel) as DependencyObject;
            for (var inner = source; inner is not null && inner != panel; inner = ParentOf(inner))
                if (inner == header) return panel;
            return null;
        }
        return null;
    }

    private static DependencyObject? ParentOf(DependencyObject node) =>
        node is Visual ? VisualTreeHelper.GetParent(node) : LogicalTreeHelper.GetParent(node);

    private MaPanel? PanelAt(int index) => Items[index] as MaPanel ?? ItemContainerGenerator.ContainerFromIndex(index) as MaPanel;

    private MaPanel? ContainerFor(object item) => item as MaPanel ?? ItemContainerGenerator.ContainerFromItem(item) as MaPanel;

    private string? KeyOf(object item) => ContainerFor(item) is { } panel ? GetPanelKey(panel) : null;

    /// <summary>Puts <paramref name="list"/> into <paramref name="order"/>, moving only what is out of place.</summary>
    private void Apply(IList list, IReadOnlyList<object> order)
    {
        _reordering = true;
        try
        {
            for (var i = 0; i < order.Count; i++)
            {
                if (Equals(list[i], order[i])) continue;
                var from = IndexOfFrom(list, order[i], i + 1);
                var moving = list[from];
                list.RemoveAt(from);
                list.Insert(i, moving);
            }
        }
        finally
        {
            _reordering = false;
        }
    }

    private bool _reordering;
    private bool _syncingOrder;

    /// <summary>
    /// Rearranges the draggable panels to follow <paramref name="keys"/> (M.COMP-T226) and makes them the
    /// <see cref="Order"/>. Unknown keys are ignored; a draggable panel whose key is not listed keeps its place, so a
    /// panel added after the order was saved still shows; panels that are not draggable keep their fixed slots. Does not
    /// raise <see cref="OrderChanged"/>, so a host that saves on it does not write back what it just loaded. With an
    /// <c>ItemsSource</c> that is not a writable <see cref="IList"/> nothing moves.
    /// </summary>
    public void ApplyOrder(IEnumerable<string> keys)
    {
        var list = keys.ToList();
        SyncOrder(list);
        ArrangeBy(list);
    }

    private static void OnOrderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var group = (MaPanelGroup)d;
        if (!group._syncingOrder && e.NewValue is IList<string> keys) group.ArrangeBy(keys);
    }

    private void SyncOrder(List<string> keys)
    {
        _syncingOrder = true;
        try { SetCurrentValue(OrderProperty, keys); }
        finally { _syncingOrder = false; }
    }

    private void ReapplyOrder()
    {
        if (!_reordering && Order is { } keys) ArrangeBy(keys);
    }

    protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);
        if (e.Action is System.Collections.Specialized.NotifyCollectionChangedAction.Add
            or System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            ReapplyOrder();
    }

    private void ArrangeBy(IList<string> keys)
    {
        if (_reordering) return;
        var rank = new Dictionary<string, int>();
        foreach (var key in keys) rank.TryAdd(key, rank.Count);

        var order = Items.Cast<object>().ToList();
        var listed = new List<int>();
        for (var i = 0; i < order.Count; i++)
            if (PanelAt(i) is { } p && GetIsDraggable(p) && GetPanelKey(p) is { } k && rank.ContainsKey(k)) listed.Add(i);
        var sorted = listed.Select(i => order[i]).OrderBy(item => rank[KeyOf(item)!]).ToList();
        var changed = false;
        for (var s = 0; s < listed.Count; s++)
        {
            changed |= !ReferenceEquals(order[listed[s]], sorted[s]);
            order[listed[s]] = sorted[s];
        }
        if (!changed) return;

        if (ItemsSource is null) Apply(Items, order);
        else if (ItemsSource is IList list && !list.IsFixedSize && !list.IsReadOnly) Apply(list, order);
    }

    private static int IndexOfFrom(IList list, object item, int start)
    {
        for (var i = start; i < list.Count; i++)
            if (ReferenceEquals(list[i], item)) return i;
        for (var i = start; i < list.Count; i++)
            if (Equals(list[i], item)) return i;
        return -1;
    }

    protected override bool IsItemItsOwnContainerOverride(object item) => item is MaPanel;

    protected override DependencyObject GetContainerForItemOverride() => new MaPanel();
}

/// <summary>A panel moved in an <see cref="MaPanelGroup"/>. See <see cref="MaPanelGroup.OrderChanged"/>.</summary>
public sealed class PanelOrderChangedEventArgs(object movedItem, int oldIndex, int newIndex, IReadOnlyList<string?> panelKeys)
    : EventArgs
{
    /// <summary>The panel moved, or its <c>ItemsSource</c> item.</summary>
    public object MovedItem { get; } = movedItem;

    public int OldIndex { get; } = oldIndex;

    public int NewIndex { get; } = newIndex;

    /// <summary>Every panel's <see cref="MaPanelGroup.PanelKeyProperty"/>, in the order after the move.</summary>
    public IReadOnlyList<string?> PanelKeys { get; } = panelKeys;
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
