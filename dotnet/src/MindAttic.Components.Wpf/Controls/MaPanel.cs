using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// A titled content container (M.COMP-T221): an optional <see cref="Title"/> header in the <c>MaSectionLabel</c>
/// look, a 1px light-gray (<c>MaMuted</c>) rule under it, and the content set in by <see cref="ContentInset"/>
/// (the shared <c>MaPanelInset</c> resource by default), so every panel's content starts at the same left pixel.
/// <code>
/// &lt;ctl:MaPanel Title="Sessions"&gt;
///     &lt;StackPanel&gt;
///         &lt;TextBlock Text="..." /&gt;
///         &lt;ctl:MaPanel Title="Idle"&gt;...&lt;/ctl:MaPanel&gt;
///     &lt;/StackPanel&gt;
/// &lt;/ctl:MaPanel&gt;
/// </code>
/// <para>
/// Panels nest: a panel inside another (<see cref="IsNested"/>) draws a smaller sub-header and takes the
/// <c>MaPanelNestedInset</c> instead, whose left edge is 0, so its header and content line up with the parent's other
/// content rather than doubling the inset; a host that wants the extra step sets <see cref="ContentInset"/> itself.
/// With no <see cref="Title"/> the header row (and its rule) collapses and the panel is just the inset content.
/// <see cref="ShowDivider"/> false hides the rule alone. The panel is a UI Automation Group named by its title.
/// </para>
/// <para>
/// <see cref="IsCollapsible"/> (M.COMP-T222) adds a chevron (<see cref="PartToggle"/>) to the header; clicking it, or
/// Enter/Space on the focused header, flips <see cref="IsExpanded"/>, which hides the rule and the content while false
/// and raises <see cref="Expanded"/> or <see cref="Collapsed"/> on this panel only. A collapsible panel's automation
/// peer also offers the ExpandCollapse pattern.
/// </para>
/// <para>
/// Its look is the implicit <c>MaPanel</c> style in Theme.xaml; the header row (<see cref="PartHeader"/>) keeps
/// leading columns free for a drag grip and an expand/collapse chevron.
/// </para>
/// <para>
/// In an <see cref="MaPanelGroup"/> with <see cref="MaPanelGroup.IsDraggableProperty"/> set (<see cref="CanDrag"/>,
/// M.COMP-T225) the header shows a grip (<see cref="PartGrip"/>): pressing it and moving past the system drag threshold
/// drags a ghost of the panel with an insertion line at the target slot, and the drop calls
/// <see cref="MaPanelGroup.MovePanel"/>. Esc or a lost mouse capture cancels with no change.
/// </para>
/// </summary>
public class MaPanel : ContentControl
{
    /// <summary>The template part holding the header row and its rule; collapsed while <see cref="Title"/> is null.</summary>
    public const string PartHeader = "PART_Header";

    /// <summary>The template part that presents the <see cref="Title"/>.</summary>
    public const string PartTitle = "PART_Title";

    /// <summary>The template part that draws the rule under the header.</summary>
    public const string PartDivider = "PART_Divider";

    /// <summary>The template part that presents the content, set in by <see cref="ContentInset"/>.</summary>
    public const string PartContent = "PART_Content";

    /// <summary>The template part, the header's expand/collapse chevron, shown while <see cref="IsCollapsible"/>.</summary>
    public const string PartToggle = "PART_Toggle";

    /// <summary>The <see cref="Title"/> property.</summary>
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(object), typeof(MaPanel),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>The template part, the header's drag grip, shown while <see cref="CanDrag"/>.</summary>
    public const string PartGrip = "PART_Grip";

    /// <summary>The <see cref="TitleTemplate"/> property.</summary>
    public static readonly DependencyProperty TitleTemplateProperty = DependencyProperty.Register(
        nameof(TitleTemplate), typeof(DataTemplate), typeof(MaPanel),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>The <see cref="ShowDivider"/> property.</summary>
    public static readonly DependencyProperty ShowDividerProperty = DependencyProperty.Register(
        nameof(ShowDivider), typeof(bool), typeof(MaPanel),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>The <see cref="ContentInset"/> property.</summary>
    public static readonly DependencyProperty ContentInsetProperty = DependencyProperty.Register(
        nameof(ContentInset), typeof(Thickness), typeof(MaPanel),
        new FrameworkPropertyMetadata(default(Thickness), FrameworkPropertyMetadataOptions.AffectsMeasure));

    private static readonly DependencyPropertyKey IsNestedPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsNested), typeof(bool), typeof(MaPanel), new FrameworkPropertyMetadata(false));

    /// <summary>The read-only <see cref="IsNested"/> property.</summary>
    public static readonly DependencyProperty IsNestedProperty = IsNestedPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey CanDragPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(CanDrag), typeof(bool), typeof(MaPanel), new FrameworkPropertyMetadata(false));

    /// <summary>The read-only <see cref="CanDrag"/> property.</summary>
    public static readonly DependencyProperty CanDragProperty = CanDragPropertyKey.DependencyProperty;

    /// <summary>The <see cref="IsCollapsible"/> property.</summary>
    public static readonly DependencyProperty IsCollapsibleProperty = DependencyProperty.Register(
        nameof(IsCollapsible), typeof(bool), typeof(MaPanel),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>The <see cref="IsExpanded"/> property.</summary>
    public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
        nameof(IsExpanded), typeof(bool), typeof(MaPanel),
        new FrameworkPropertyMetadata(true,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsMeasure,
            OnIsExpandedChanged));

    /// <summary>The <see cref="Expanded"/> event. Direct: only handlers on the panel that expanded see it.</summary>
    public static readonly RoutedEvent ExpandedEvent = EventManager.RegisterRoutedEvent(
        nameof(Expanded), RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(MaPanel));

    /// <summary>The <see cref="Collapsed"/> event. Direct: only handlers on the panel that collapsed see it.</summary>
    public static readonly RoutedEvent CollapsedEvent = EventManager.RegisterRoutedEvent(
        nameof(Collapsed), RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(MaPanel));

    private UIElement? _header;
    private UIElement? _toggle;
    private UIElement? _grip;

    static MaPanel()
    {
        // A container, not a stop in the tab order: its content takes focus.
        FocusableProperty.OverrideMetadata(typeof(MaPanel), new FrameworkPropertyMetadata(false));
        IsTabStopProperty.OverrideMetadata(typeof(MaPanel), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Creates a panel; whether it is nested is worked out once it loads.</summary>
    public MaPanel()
    {
        Loaded += (_, _) =>
        {
            UpdateIsNested();
            UpdateCanDrag();
        };
    }

    /// <summary>The header (usually a string); null (the default) collapses the header row and its rule.</summary>
    public object? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>How a non-string <see cref="Title"/> is drawn.</summary>
    public DataTemplate? TitleTemplate
    {
        get => (DataTemplate?)GetValue(TitleTemplateProperty);
        set => SetValue(TitleTemplateProperty, value);
    }

    /// <summary>Whether the 1px light-gray rule under the header shows; true by default.</summary>
    public bool ShowDivider
    {
        get => (bool)GetValue(ShowDividerProperty);
        set => SetValue(ShowDividerProperty, value);
    }

    /// <summary>The space around the content: the <c>MaPanelInset</c> resource by default, <c>MaPanelNestedInset</c>
    /// (no left step) while <see cref="IsNested"/>. A local value wins over both.</summary>
    public Thickness ContentInset
    {
        get => (Thickness)GetValue(ContentInsetProperty);
        set => SetValue(ContentInsetProperty, value);
    }

    /// <summary>True while this panel sits inside another <see cref="MaPanel"/>.</summary>
    public bool IsNested => (bool)GetValue(IsNestedProperty);

    /// <summary>True while this panel sits in an <see cref="MaPanelGroup"/> with
    /// <see cref="MaPanelGroup.IsDraggableProperty"/> set (M.COMP-T225): its header shows the drag grip and takes
    /// focus, so it can be dragged or moved with Alt+Up/Down.</summary>
    public bool CanDrag => (bool)GetValue(CanDragProperty);

    /// <summary>Whether the header shows a chevron that expands and collapses the panel; false by default.</summary>
    public bool IsCollapsible
    {
        get => (bool)GetValue(IsCollapsibleProperty);
        set => SetValue(IsCollapsibleProperty, value);
    }

    /// <summary>Whether the content (and the rule) shows; true by default, false leaves only the header. Binds two-way
    /// by default.</summary>
    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <summary>Raised on this panel when <see cref="IsExpanded"/> turns true.</summary>
    public event RoutedEventHandler Expanded
    {
        add => AddHandler(ExpandedEvent, value);
        remove => RemoveHandler(ExpandedEvent, value);
    }

    /// <summary>Raised on this panel when <see cref="IsExpanded"/> turns false.</summary>
    public event RoutedEventHandler Collapsed
    {
        add => AddHandler(CollapsedEvent, value);
        remove => RemoveHandler(CollapsedEvent, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (_toggle is not null) _toggle.MouseLeftButtonDown -= OnToggleMouseDown;
        if (_header is not null) _header.KeyDown -= OnHeaderKeyDown;
        if (_grip is not null) _grip.MouseLeftButtonDown -= OnGripMouseDown;
        base.OnApplyTemplate();
        _toggle = GetTemplateChild(PartToggle) as UIElement;
        _header = GetTemplateChild(PartHeader) as UIElement;
        _grip = GetTemplateChild(PartGrip) as UIElement;
        if (_toggle is not null) _toggle.MouseLeftButtonDown += OnToggleMouseDown;
        if (_header is not null)
        {
            _header.KeyDown += OnHeaderKeyDown;
            MaPanelGroup.SetHeaderOwner(_header, this);
        }
        if (_grip is not null) _grip.MouseLeftButtonDown += OnGripMouseDown;
    }

    private void OnToggleMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!IsCollapsible) return;
        IsExpanded = !IsExpanded;
        e.Handled = true;
    }

    private void OnHeaderKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsCollapsible || e.OriginalSource != _header || e.Key is not (Key.Enter or Key.Space)) return;
        IsExpanded = !IsExpanded;
        e.Handled = true;
    }

    private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var panel = (MaPanel)d;
        var expanded = (bool)e.NewValue;
        if (UIElementAutomationPeer.FromElement(panel) is MaPanelAutomationPeer peer)
            peer.RaiseExpandCollapseChanged(!expanded, expanded);
        panel.RaiseEvent(new RoutedEventArgs(expanded ? ExpandedEvent : CollapsedEvent, panel));
    }

    /// <inheritdoc />
    protected override void OnVisualParentChanged(DependencyObject oldParent)
    {
        base.OnVisualParentChanged(oldParent);
        UpdateIsNested();
        UpdateCanDrag();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == MaPanelGroup.IsDraggableProperty) UpdateCanDrag();
    }

    private void UpdateCanDrag() =>
        SetValue(CanDragPropertyKey, MaPanelGroup.GetIsDraggable(this) && Group is not null);

    private MaPanelGroup? Group => ItemsControl.ItemsControlFromItemContainer(this) as MaPanelGroup;

    // ---- drag-to-reorder (M.COMP-T225) ------------------------------------------------------------

    private MaPanelGroup? _dragGroup;
    private Point _dragStart;
    private double _grabOffsetY;
    private int _dragStartIndex;
    private bool _isDragging;
    private AdornerLayer? _dragAdornerLayer;
    private PanelDragAdorner? _dragAdorner;

    /// <summary>True between the pointer passing the drag threshold and the drop or cancel.</summary>
    internal bool IsDragging => _isDragging;

    /// <summary>The slot the panel would land in if dropped now (its own while no drag is in progress).</summary>
    internal int DropIndex { get; private set; } = -1;

    private void OnGripMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (Group is not { } group || !BeginDrag(group, e.GetPosition(group))) return;
        _header?.Focus();
        CaptureMouse();
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragGroup is not { } group) return;
        if (e.LeftButton != MouseButtonState.Pressed) CancelDrag();
        else UpdateDrag(e.GetPosition(group));
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_dragGroup is not { } group) return;
        EndDrag(e.GetPosition(group));
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        CancelDrag();
    }

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (!_isDragging || e.Key != Key.Escape) return;
        CancelDrag();
        e.Handled = true;
    }

    /// <summary>Arms a drag pressed on the grip at <paramref name="groupPosition"/> (in <paramref name="group"/>'s
    /// coordinates); a no-op unless this panel <see cref="CanDrag"/> in that group. Like <see cref="UpdateDrag"/> and
    /// <see cref="EndDrag"/> it takes plain points, so a whole gesture can be driven from a test.</summary>
    internal bool BeginDrag(MaPanelGroup group, Point groupPosition)
    {
        if (!CanDrag || Group != group) return false;
        _dragGroup = group;
        _dragStart = groupPosition;
        _grabOffsetY = groupPosition.Y - TranslatePoint(new Point(0, 0), group).Y;
        _dragStartIndex = group.ItemContainerGenerator.IndexFromContainer(this);
        DropIndex = _dragStartIndex;
        return true;
    }

    /// <summary>Moves an armed drag to <paramref name="groupPosition"/>: past the system drag threshold it starts the
    /// ghost and the insertion line, then keeps them under the pointer.</summary>
    internal void UpdateDrag(Point groupPosition)
    {
        if (_dragGroup is not { } group) return;
        if (!_isDragging)
        {
            if (Math.Abs(groupPosition.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
            _isDragging = true;
            _dragAdornerLayer = AdornerLayer.GetAdornerLayer(group);
            if (_dragAdornerLayer is not null)
            {
                _dragAdorner = new PanelDragAdorner(group, this);
                _dragAdornerLayer.Add(_dragAdorner);
            }
        }

        DropIndex = DropIndexAt(group, groupPosition.Y);
        if (_dragAdorner is null) return;
        _dragAdorner.GhostTop = groupPosition.Y - _grabOffsetY;
        _dragAdorner.LineY = InsertionLineY(group, DropIndex);
        _dragAdorner.InvalidateVisual();
    }

    /// <summary>Drops an armed drag at <paramref name="groupPosition"/>: moves the panel with
    /// <see cref="MaPanelGroup.MovePanel"/> (so <see cref="MaPanelGroup.OrderChanged"/> fires once) when it started
    /// and lands in another slot. Returns whether the panel moved.</summary>
    internal bool EndDrag(Point groupPosition)
    {
        if (_dragGroup is not { } group) return false;
        var dropped = _isDragging ? DropIndexAt(group, groupPosition.Y) : _dragStartIndex;
        var start = _dragStartIndex;
        StopDrag();
        return dropped != start && group.MovePanel(this, dropped);
    }

    /// <summary>Ends a drag with no change, as Esc or a lost mouse capture does.</summary>
    internal void CancelDrag() => StopDrag();

    private void StopDrag()
    {
        if (_dragGroup is null) return;
        _dragGroup = null;
        _isDragging = false;
        DropIndex = -1;
        if (_dragAdornerLayer is not null && _dragAdorner is not null) _dragAdornerLayer.Remove(_dragAdorner);
        _dragAdornerLayer = null;
        _dragAdorner = null;
        if (IsMouseCaptured) ReleaseMouseCapture();
    }

    private int DropIndexAt(MaPanelGroup group, double pointerY)
    {
        var midpoints = new List<double>();
        var draggable = new List<bool>();
        for (var i = 0; i < group.Items.Count; i++)
        {
            var panel = group.ItemContainerGenerator.ContainerFromIndex(i) as MaPanel;
            midpoints.Add(panel is null ? double.NaN : panel.TranslatePoint(new Point(0, 0), group).Y + panel.ActualHeight / 2);
            draggable.Add(panel is not null && MaPanelGroup.GetIsDraggable(panel));
        }
        return ComputeDropIndex(midpoints, draggable, _dragStartIndex, pointerY);
    }

    /// <summary>Where a drag drops: the dragged panel and the other draggable ones share the draggable slots, and it
    /// takes the slot after every other draggable panel whose midpoint is above <paramref name="pointerY"/>. Fixed
    /// (non-draggable) slots are skipped, as <see cref="MaPanelGroup.MovePanel"/> requires.</summary>
    internal static int ComputeDropIndex(IReadOnlyList<double> midpoints, IReadOnlyList<bool> isDraggable, int draggedIndex, double pointerY)
    {
        var slots = new List<int>();
        var before = 0;
        for (var i = 0; i < midpoints.Count; i++)
        {
            if (i != draggedIndex && !isDraggable[i]) continue;
            slots.Add(i);
            if (i != draggedIndex && midpoints[i] < pointerY) before++;
        }
        return slots.Count == 0 ? draggedIndex : slots[Math.Min(before, slots.Count - 1)];
    }

    /// <summary>The insertion line's y in <paramref name="group"/>: above the panel whose slot it takes when moving up,
    /// below it when moving down; NaN (no line) over its own slot.</summary>
    private double InsertionLineY(MaPanelGroup group, int dropIndex)
    {
        if (dropIndex == _dragStartIndex || group.ItemContainerGenerator.ContainerFromIndex(dropIndex) is not MaPanel target)
            return double.NaN;
        var top = target.TranslatePoint(new Point(0, 0), group).Y;
        return dropIndex < _dragStartIndex ? top : top + target.ActualHeight;
    }

    /// <summary>Drawn over the group while a panel is dragged: a live <see cref="VisualBrush"/> ghost of the panel
    /// following the pointer (as <see cref="MaTabItem"/>'s drag does) and a 2px <c>MaPanelDropIndicator</c> line at
    /// the target slot.</summary>
    private sealed class PanelDragAdorner : Adorner
    {
        private readonly VisualBrush _ghost;
        private readonly Rect _ghostSize;
        private readonly Brush _lineBrush;

        public PanelDragAdorner(MaPanelGroup group, MaPanel panel) : base(group)
        {
            _ghost = new VisualBrush(panel) { Opacity = 0.7 };
            _ghostSize = new Rect(panel.TranslatePoint(new Point(0, 0), group), new Size(panel.ActualWidth, panel.ActualHeight));
            _lineBrush = group.TryFindResource("MaPanelDropIndicator") as Brush ?? Brushes.CornflowerBlue;
            IsHitTestVisible = false;
        }

        public double GhostTop { get; set; }

        public double LineY { get; set; } = double.NaN;

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(_ghost, null, new Rect(_ghostSize.X, GhostTop, _ghostSize.Width, _ghostSize.Height));
            if (!double.IsNaN(LineY))
                drawingContext.DrawRectangle(_lineBrush, null, new Rect(0, LineY - 1, AdornedElement.RenderSize.Width, 2));
        }
    }

    private void UpdateIsNested()
    {
        var nested = false;
        for (var node = ParentOf(this); node is not null && !nested; node = ParentOf(node))
            nested = node is MaPanel;
        SetValue(IsNestedPropertyKey, nested);
    }

    private static DependencyObject? ParentOf(DependencyObject node) =>
        (node is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(node) : null)
        ?? LogicalTreeHelper.GetParent(node);

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new MaPanelAutomationPeer(this);

    /// <summary>A panel is a Group named by its title (unless <c>AutomationProperties.Name</c> is set); a collapsible
    /// one also offers the ExpandCollapse pattern.</summary>
    private sealed class MaPanelAutomationPeer(MaPanel owner) : FrameworkElementAutomationPeer(owner), IExpandCollapseProvider
    {
        private MaPanel Panel => (MaPanel)Owner;

        public ExpandCollapseState ExpandCollapseState =>
            Panel.IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

        public void Expand()
        {
            if (!Panel.IsEnabled) throw new ElementNotEnabledException();
            Panel.IsExpanded = true;
        }

        public void Collapse()
        {
            if (!Panel.IsEnabled) throw new ElementNotEnabledException();
            Panel.IsExpanded = false;
        }

        public override object? GetPattern(PatternInterface patternInterface) =>
            patternInterface == PatternInterface.ExpandCollapse && Panel.IsCollapsible ? this : base.GetPattern(patternInterface);

        public void RaiseExpandCollapseChanged(bool wasExpanded, bool isExpanded) => RaisePropertyChangedEvent(
            ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
            wasExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
            isExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);

        protected override string GetClassNameCore() => nameof(MaPanel);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            if (!string.IsNullOrEmpty(name)) return name;
            return ((MaPanel)Owner).Title switch
            {
                null => string.Empty,
                string s => s,
                UIElement => string.Empty,
                var other => other.ToString() ?? string.Empty,
            };
        }
    }
}
