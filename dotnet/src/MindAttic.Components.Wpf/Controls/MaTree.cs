using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using MindAttic.Components.Wpf.Mvvm;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// One node of an <see cref="MaTree"/>: a group (<see cref="IsLeaf"/> false, with <see cref="Children"/>) or a leaf
/// (an actual item, carrying the source item in <see cref="Tag"/>). Build a tree from a flat list with
/// <see cref="GroupByDelimiter(IEnumerable{string}, char)"/> or the generic overload, which groups by a delimiter
/// (e.g. the dots in "Mods.GTFO.EZ.AbsurdItems" nest four levels deep; a key with no delimiter, e.g. "Gadfly", is a
/// top-level leaf) -- a generic capability with no knowledge of what the keys mean.
/// <see cref="IsExpanded"/> is bindable per node (two-way, from <see cref="MaTree"/>'s implicit item style), so
/// expand/collapse state lives on the model and survives a rebuild of the tree.
/// </summary>
public sealed class MaTreeNode : ObservableObject
{
    private bool _isExpanded;
    private bool _isChecked;

    public MaTreeNode(string name, string key, bool isLeaf, object? tag = null)
    {
        Name = name;
        Key = key;
        IsLeaf = isLeaf;
        Tag = tag;
    }

    /// <summary>This node's own segment (e.g. "GTFO" for the group "Mods.GTFO", or "Gadfly" for a top-level leaf).</summary>
    public string Name { get; }

    /// <summary>The full delimited path from the root to this node (e.g. "Mods.GTFO.EZ.AbsurdItems"); a leaf's own key.</summary>
    public string Key { get; }

    /// <summary>True for an actual item (selectable, no children); false for a group (expands/collapses, never selected).</summary>
    public bool IsLeaf { get; }

    /// <summary>The source item a leaf was built from (e.g. the original string, or the item passed to the generic
    /// overload); null for a group.</summary>
    public object? Tag { get; }

    /// <summary>This node's groups and leaves, in display order (groups before leaves, each alphabetical by Name);
    /// empty for a leaf.</summary>
    public ObservableCollection<MaTreeNode> Children { get; } = new();

    /// <summary>Whether this node is expanded; bound two-way by <see cref="MaTree"/>'s implicit item style, so
    /// collapsing/expanding a node is a model change the host can read, save and restore.</summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set => Set(ref _isExpanded, value);
    }

    /// <summary>Whether this leaf's check box is ticked, while its <see cref="MaTree"/> has
    /// <see cref="MaTree.ShowCheckBoxes"/> on (M.COMP-T238); bound two-way, so the host reads every checked leaf
    /// off the model. Groups and dividers have no check box.</summary>
    public bool IsChecked
    {
        get => _isChecked;
        set => Set(ref _isChecked, value);
    }

    /// <summary>True for a <see cref="Divider"/>: a rule between rows, never selected, checked, focused or highlighted.</summary>
    public bool IsDivider { get; private init; }

    /// <summary>A divider row (M.COMP-T238): a full-width MaSeparator line between the nodes around it, with no
    /// interactivity at all (not selectable, checkable, focusable or hover-highlighted, and skipped by the keyboard).</summary>
    public static MaTreeNode Divider() => new(string.Empty, string.Empty, isLeaf: false) { IsDivider = true };

    /// <summary>Groups <paramref name="keys"/> into a prefix tree by splitting each on <paramref name="delimiter"/>
    /// (a dot by default): "Unity.GridGame" and "Unity.Eve" both land under a "Unity" group, while "Gadfly" (no
    /// delimiter) is a top-level leaf. Each leaf's <see cref="Tag"/> is its own key.</summary>
    public static ObservableCollection<MaTreeNode> GroupByDelimiter(IEnumerable<string> keys, char delimiter = '.') =>
        GroupByDelimiter(keys, key => key, delimiter);

    /// <summary>Groups <paramref name="items"/> into a prefix tree by splitting each item's <paramref name="keySelector"/>
    /// on <paramref name="delimiter"/> (a dot by default), so any item type with a name/key can be grouped without this
    /// component knowing what the keys mean. Each leaf's <see cref="Tag"/> is the source item.</summary>
    public static ObservableCollection<MaTreeNode> GroupByDelimiter<T>(
        IEnumerable<T> items, Func<T, string> keySelector, char delimiter = '.')
    {
        var roots = new ObservableCollection<MaTreeNode>();
        foreach (var item in items)
        {
            var key = keySelector(item) ?? string.Empty;
            var segments = key.Split(delimiter);
            var siblings = roots;
            var pathSoFar = string.Empty;
            for (var i = 0; i < segments.Length; i++)
            {
                var isLast = i == segments.Length - 1;
                pathSoFar = i == 0 ? segments[i] : $"{pathSoFar}{delimiter}{segments[i]}";
                var node = FindOrAdd(siblings, segments[i], pathSoFar, isLast, isLast ? item : null);
                siblings = node.Children;
            }
        }
        Sort(roots);
        return roots;
    }

    private static MaTreeNode FindOrAdd(ObservableCollection<MaTreeNode> siblings, string name, string path, bool isLeaf, object? tag)
    {
        foreach (var existing in siblings)
            if (existing.Name == name) return existing;
        var node = new MaTreeNode(name, path, isLeaf, tag);
        siblings.Add(node);
        return node;
    }

    private static void Sort(ObservableCollection<MaTreeNode> nodes)
    {
        var ordered = nodes.OrderBy(n => n.IsLeaf).ThenBy(n => n.Name, StringComparer.OrdinalIgnoreCase).ToList();
        nodes.Clear();
        foreach (var node in ordered)
        {
            nodes.Add(node);
            Sort(node.Children);
        }
    }
}

/// <summary>
/// A themed, data-bindable tree (M.COMP-T110): bind <see cref="ItemsControl.ItemsSource"/> to an
/// <see cref="ObservableCollection{T}"/> of <see cref="MaTreeNode"/> (usually built with
/// <see cref="MaTreeNode.GroupByDelimiter(IEnumerable{string}, char)"/>), e.g.
/// <code>
/// &lt;ctl:MaTree ItemsSource="{Binding Roots}" LeafSelected="OnLeafSelected" /&gt;
/// </code>
/// Groups expand and collapse (click anywhere on the row, Enter/Space, or the arrow keys); only leaves are
/// selectable, single-select, and choosing one sets the read-only <see cref="SelectedLeaf"/> and raises
/// <see cref="LeafSelected"/> with the node (its <see cref="MaTreeNode.Key"/> is the item's full dotted name).
/// Keeps no knowledge of what a leaf represents: the first concrete consumer is the Orchestrator's "Open Project"
/// repo picker (M.ORCH-T2107), grouping ~90 repo names by their dot-prefixes.
/// </summary>
public class MaTree : TreeView
{
    private static readonly DependencyPropertyKey SelectedLeafPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(SelectedLeaf), typeof(MaTreeNode), typeof(MaTree), new FrameworkPropertyMetadata(null));

    /// <summary>The read-only <see cref="SelectedLeaf"/> property.</summary>
    public static readonly DependencyProperty SelectedLeafProperty = SelectedLeafPropertyKey.DependencyProperty;

    /// <summary>The <see cref="LeafSelected"/> routed event.</summary>
    public static readonly RoutedEvent LeafSelectedEvent = EventManager.RegisterRoutedEvent(
        nameof(LeafSelected), RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<MaTreeNode?>), typeof(MaTree));

    /// <summary>Raised when the selected leaf changes (never for a group, which is never selected).</summary>
    public event RoutedPropertyChangedEventHandler<MaTreeNode?> LeafSelected
    {
        add => AddHandler(LeafSelectedEvent, value);
        remove => RemoveHandler(LeafSelectedEvent, value);
    }

    /// <summary>The chosen leaf, or null while nothing is chosen (a group is never the selected leaf).</summary>
    public MaTreeNode? SelectedLeaf => (MaTreeNode?)GetValue(SelectedLeafProperty);

    /// <summary>The <see cref="ShowCheckBoxes"/> property.</summary>
    public static readonly DependencyProperty ShowCheckBoxesProperty = DependencyProperty.Register(
        nameof(ShowCheckBoxes), typeof(bool), typeof(MaTree), new FrameworkPropertyMetadata(false));

    /// <summary>Gives every leaf a check box bound to <see cref="MaTreeNode.IsChecked"/> (M.COMP-T238), for picking
    /// several leaves at once; clicking a leaf's row, or Space on it, toggles it too. Off by default.</summary>
    public bool ShowCheckBoxes
    {
        get => (bool)GetValue(ShowCheckBoxesProperty);
        set => SetValue(ShowCheckBoxesProperty, value);
    }

    protected override DependencyObject GetContainerForItemOverride() => new MaTreeItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is MaTreeItem;

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        MaTreeItem.PrepareNode(element, item);
    }

    protected override void OnSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e)
    {
        base.OnSelectedItemChanged(e);
        var leaf = e.NewValue as MaTreeNode;
        if (leaf is { IsLeaf: false }) leaf = null;
        var previous = SelectedLeaf;
        if (ReferenceEquals(previous, leaf)) return;
        SetValue(SelectedLeafPropertyKey, leaf);
        RaiseEvent(new RoutedPropertyChangedEventArgs<MaTreeNode?>(previous, leaf, LeafSelectedEvent));
    }
}

/// <summary>
/// One row of an <see cref="MaTree"/>: a group row (an arrow that rotates to point down when
/// <see cref="TreeViewItem.IsExpanded"/>, never selectable) or a leaf row (selectable, the chosen one SemiBold with
/// an <c>MaAccent</c> bar, as <see cref="MaPickerItem"/> marks its chosen option). Clicking, Enter or Space on a
/// group row toggles <see cref="TreeViewItem.IsExpanded"/> instead of selecting it.
/// </summary>
public class MaTreeItem : TreeViewItem
{
    /// <summary>The template part that draws the expand/collapse arrow.</summary>
    public const string PartArrow = "PART_Arrow";

    /// <summary>The template part that marks the chosen leaf.</summary>
    public const string PartSelectedBar = "PART_SelectedBar";

    // A TreeViewItem is itself the ItemsControl for its own children, so a node nested two or more levels deep
    // (e.g. the 4th segment of "Mods.GTFO.EZ.AbsurdItems") is generated by its parent MaTreeItem, not by the owning
    // MaTree: it needs the same container type and wiring MaTree.PrepareContainerForItemOverride gives the top level.
    protected override DependencyObject GetContainerForItemOverride() => new MaTreeItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is MaTreeItem;

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        PrepareNode(element, item);
    }

    /// <summary>Points a generated container at its node's children and names it for UI Automation; shared by
    /// <see cref="MaTree"/> (the roots) and <see cref="MaTreeItem"/> (every deeper level).</summary>
    internal static void PrepareNode(DependencyObject element, object item)
    {
        if (element is not MaTreeItem container || item is not MaTreeNode node) return;
        container.ItemsSource = node.Children;
        if (container.ReadLocalValue(AutomationProperties.NameProperty) == DependencyProperty.UnsetValue)
            container.SetValue(AutomationProperties.NameProperty, node.Name);
    }

    /// <summary>The <see cref="MaTree"/> this row belongs to, however deep it's nested.</summary>
    private MaTree? OwnerTree
    {
        get
        {
            var owner = ItemsControlFromItemContainer(this);
            while (owner is MaTreeItem parent) owner = ItemsControlFromItemContainer(parent);
            return owner as MaTree;
        }
    }

    private bool IsCheckableLeaf => DataContext is MaTreeNode { IsLeaf: true } && OwnerTree is { ShowCheckBoxes: true };

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (DataContext is MaTreeNode { IsDivider: true }) { e.Handled = true; return; }
        if (DataContext is MaTreeNode { IsLeaf: false })
        {
            IsExpanded = !IsExpanded;
            Focus();
            e.Handled = true;
            return;
        }
        if (IsCheckableLeaf && DataContext is MaTreeNode leaf) leaf.IsChecked = !leaf.IsChecked;
        base.OnMouseLeftButtonDown(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (DataContext is MaTreeNode { IsLeaf: false } && e.Key is Key.Enter or Key.Space)
        {
            IsExpanded = !IsExpanded;
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Space && IsCheckableLeaf && DataContext is MaTreeNode leaf)
        {
            leaf.IsChecked = !leaf.IsChecked;
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }
}
