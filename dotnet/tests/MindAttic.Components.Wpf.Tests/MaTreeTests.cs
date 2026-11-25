using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MindAttic.Components.Wpf.Controls;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T110: MaTreeNode.GroupByDelimiter (plain model logic) and the MaTree / MaTreeItem control it feeds
/// -- the tree the Orchestrator's "Open Project" repo picker groups ~90 repo names with by their dot-prefixes
/// (M.ORCH-T2107). MaTree has no popup of its own, so every test shows it directly.</summary>
public sealed class MaTreeTests
{
    // ---- MaTreeNode.GroupByDelimiter: no UI involved ----

    [Fact]
    public void Dots_group_into_a_multi_level_prefix_tree_with_a_no_dot_leaf_at_the_top()
    {
        var roots = MaTreeNode.GroupByDelimiter(["Unity.GridGame", "Unity.Eve", "Unity.Shared", "Mods.GTFO.EZ.AbsurdItems", "Gadfly"]);

        Assert.Equal(["Mods", "Unity", "Gadfly"], roots.Select(n => n.Name));

        var mods = roots[0];
        Assert.False(mods.IsLeaf);
        Assert.Equal("Mods", mods.Key);
        var gtfo = Assert.Single(mods.Children);
        Assert.False(gtfo.IsLeaf);
        Assert.Equal("Mods.GTFO", gtfo.Key);
        var ez = Assert.Single(gtfo.Children);
        Assert.False(ez.IsLeaf);
        Assert.Equal("Mods.GTFO.EZ", ez.Key);
        var absurdItems = Assert.Single(ez.Children);
        Assert.True(absurdItems.IsLeaf);
        Assert.Equal("Mods.GTFO.EZ.AbsurdItems", absurdItems.Key);
        Assert.Empty(absurdItems.Children);

        var unity = roots[1];
        Assert.Equal(["Eve", "GridGame", "Shared"], unity.Children.Select(n => n.Name));
        Assert.All(unity.Children, n => Assert.True(n.IsLeaf));

        var gadfly = roots[2];
        Assert.True(gadfly.IsLeaf);
        Assert.Equal("Gadfly", gadfly.Key);
        Assert.Empty(gadfly.Children);
    }

    [Fact]
    public void A_divider_is_neither_a_leaf_nor_checked_and_leaves_start_unchecked()
    {
        var divider = MaTreeNode.Divider();
        Assert.True(divider.IsDivider);
        Assert.False(divider.IsLeaf);
        Assert.False(divider.IsChecked);
        Assert.Empty(divider.Children);
        Assert.All(MaTreeNode.GroupByDelimiter(["A.B", "C"]), n => Assert.False(n.IsDivider || n.IsChecked));
    }

    [Fact]
    public void Groups_sort_before_leaves_each_alphabetical_and_collapsed_by_default()
    {
        var roots = MaTreeNode.GroupByDelimiter(["Zebra", "Ant.One", "Beta.Two", "Apple"]);
        Assert.Equal(["Ant", "Beta", "Apple", "Zebra"], roots.Select(n => n.Name));
        Assert.All(roots, n => Assert.False(n.IsExpanded));
    }

    [Fact]
    public void A_custom_delimiter_and_a_non_string_item_type_both_work_and_a_leaf_carries_its_source_item()
    {
        var items = new[] { (Name: "a/b", Id: 1), (Name: "a/c", Id: 2), (Name: "solo", Id: 3) };
        var roots = MaTreeNode.GroupByDelimiter(items, i => i.Name, '/');

        var a = roots.Single(n => n.Name == "a");
        Assert.False(a.IsLeaf);
        Assert.Null(a.Tag);
        Assert.Equal(2, a.Children.Count);
        var b = a.Children.Single(n => n.Name == "b");
        Assert.True(b.IsLeaf);
        Assert.Equal(items[0], b.Tag);

        var solo = roots.Single(n => n.Name == "solo");
        Assert.True(solo.IsLeaf);
        Assert.Equal(items[2], solo.Tag);
    }

    // ---- MaTree / MaTreeItem ----

    private static MaTree Tree(StaHost h, IEnumerable<MaTreeNode> roots) => h.Show(new MaTree { ItemsSource = roots, Width = 260 });

    private static MaTreeItem Container(ItemsControl owner, MaTreeNode node) =>
        (MaTreeItem)owner.ItemContainerGenerator.ContainerFromItem(node);

    private static T Part<T>(Control owner, string name) where T : class => (T)owner.Template.FindName(name, owner);

    [Fact]
    public void Root_containers_are_wired_to_their_nodes_children_and_named_for_automation() => StaHost.Run(h =>
    {
        var roots = MaTreeNode.GroupByDelimiter(["Unity.GridGame", "Unity.Eve", "Gadfly"]);
        var tree = Tree(h, roots);
        var unity = roots.Single(n => n.Name == "Unity");
        var gadfly = roots.Single(n => n.Name == "Gadfly");

        var unityItem = Container(tree, unity);
        var gadflyItem = Container(tree, gadfly);

        Assert.Same(unity.Children, unityItem.ItemsSource);
        Assert.Equal("Unity", AutomationProperties.GetName(unityItem));
        Assert.Equal("Gadfly", AutomationProperties.GetName(gadflyItem));
        Assert.Equal(Visibility.Hidden, Part<TextBlock>(gadflyItem, MaTreeItem.PartArrow).Visibility);
        Assert.NotEqual(Visibility.Hidden, Part<TextBlock>(unityItem, MaTreeItem.PartArrow).Visibility);
    });

    [Fact]
    public void Clicking_a_group_row_toggles_its_node_expanded_instead_of_selecting_it() => StaHost.Run(h =>
    {
        var roots = MaTreeNode.GroupByDelimiter(["Unity.GridGame", "Unity.Eve"]);
        var tree = Tree(h, roots);
        var unity = roots[0];
        var unityItem = Container(tree, unity);

        Assert.False(unity.IsExpanded);
        Click(unityItem);
        h.Pump();
        Assert.True(unity.IsExpanded);
        Assert.True(unityItem.IsExpanded);
        Assert.False(unityItem.IsSelected);
        Assert.Null(tree.SelectedLeaf);

        Click(unityItem);
        h.Pump();
        Assert.False(unity.IsExpanded);
        Assert.False(unityItem.IsExpanded);
    });

    [Fact]
    public void Expanding_nodes_in_the_model_expands_their_containers_down_to_a_nested_leaf() => StaHost.Run(h =>
    {
        var roots = MaTreeNode.GroupByDelimiter(["Mods.GTFO.EZ.AbsurdItems"]);
        roots[0].IsExpanded = true; // Mods
        roots[0].Children[0].IsExpanded = true; // GTFO
        roots[0].Children[0].Children[0].IsExpanded = true; // EZ
        var tree = Tree(h, roots);
        h.Pump();

        var mods = Container(tree, roots[0]);
        Assert.True(mods.IsExpanded);
        var gtfo = Container(mods, roots[0].Children[0]);
        Assert.NotNull(gtfo);
        Assert.True(gtfo.IsExpanded);
        var ez = Container(gtfo, roots[0].Children[0].Children[0]);
        Assert.NotNull(ez);
        Assert.True(ez.IsExpanded);
        var leaf = Container(ez, roots[0].Children[0].Children[0].Children[0]);
        Assert.NotNull(leaf);
        Assert.Equal("Mods.GTFO.EZ.AbsurdItems", ((MaTreeNode)leaf.DataContext).Key);
    });

    [Fact]
    public void Selecting_a_leaf_sets_SelectedLeaf_and_raises_LeafSelected_once() => StaHost.Run(h =>
    {
        var roots = MaTreeNode.GroupByDelimiter(["Unity.GridGame", "Unity.Eve"]);
        roots[0].IsExpanded = true;
        var tree = Tree(h, roots);
        h.Pump();
        var unityItem = Container(tree, roots[0]);
        var eve = roots[0].Children.Single(n => n.Name == "Eve");
        var eveItem = Container(unityItem, eve);

        var raisedCount = 0;
        MaTreeNode? raisedOld = null, raisedNew = null;
        tree.LeafSelected += (_, e) =>
        {
            raisedCount++;
            raisedOld = e.OldValue;
            raisedNew = e.NewValue;
        };

        eveItem.IsSelected = true;
        h.Pump();

        Assert.Equal(1, raisedCount);
        Assert.Null(raisedOld);
        Assert.Same(eve, raisedNew);
        Assert.Same(eve, tree.SelectedLeaf);
        Assert.Equal(Visibility.Visible, Part<Border>(eveItem, MaTreeItem.PartSelectedBar).Visibility);
        Assert.Equal(FontWeights.SemiBold, eveItem.FontWeight);
    });

    [Fact]
    public void A_groups_container_never_becomes_selected() => StaHost.Run(h =>
    {
        var roots = MaTreeNode.GroupByDelimiter(["Unity.GridGame"]);
        var tree = Tree(h, roots);
        var unityItem = Container(tree, roots[0]);

        Click(unityItem);
        h.Pump();
        Assert.False(unityItem.IsSelected);
        Assert.Null(tree.SelectedLeaf);
    });

    [Fact]
    public void The_tree_is_themed_transparent_with_MaText_rows() => StaHost.Run(h =>
    {
        var roots = MaTreeNode.GroupByDelimiter(["Gadfly"]);
        var tree = Tree(h, roots);
        Assert.Equal(Brushes.Transparent, tree.Background);
        Assert.Same(h.Window.FindResource("MaText"), tree.Foreground);

        var gadflyItem = Container(tree, roots[0]);
        Assert.Same(h.Window.FindResource("MaText"), gadflyItem.Foreground);
    });

    // ---- harness ----

    private static void Click(UIElement element) =>
        element.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonDownEvent,
            Source = element,
        });
}
