using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
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

    static MaPanel()
    {
        // A container, not a stop in the tab order: its content takes focus.
        FocusableProperty.OverrideMetadata(typeof(MaPanel), new FrameworkPropertyMetadata(false));
        IsTabStopProperty.OverrideMetadata(typeof(MaPanel), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Creates a panel; whether it is nested is worked out once it loads.</summary>
    public MaPanel()
    {
        Loaded += (_, _) => UpdateIsNested();
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
        base.OnApplyTemplate();
        _toggle = GetTemplateChild(PartToggle) as UIElement;
        _header = GetTemplateChild(PartHeader) as UIElement;
        if (_toggle is not null) _toggle.MouseLeftButtonDown += OnToggleMouseDown;
        if (_header is not null) _header.KeyDown += OnHeaderKeyDown;
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
