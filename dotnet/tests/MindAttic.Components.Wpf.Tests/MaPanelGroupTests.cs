using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using MindAttic.Components.Wpf.Controls;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T223: MaPanelGroup stacks MaPanels at their desired heights, gives the leftover height to the
/// FillsRemainingSpace panel wherever it sits, and nests inside a panel. M.COMP-T224: draggable panels reorder around
/// fixed slots, by MovePanel or Alt+Up/Down, reporting each move through OrderChanged.</summary>
public sealed class MaPanelGroupTests
{
    private static MaPanel Panel(string title, double contentHeight) =>
        new() { Title = title, Content = new Border { Height = contentHeight } };

    private static double TopIn(UIElement relativeTo, UIElement element) => element.TranslatePoint(new Point(0, 0), relativeTo).Y;

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Auto_panels_take_their_desired_height_and_the_fill_panel_the_rest(int fillIndex) => StaHost.Run(h =>
    {
        var panels = new[] { Panel("A", 20), Panel("B", 30), Panel("C", 40) };
        MaPanelGroup.SetFillsRemainingSpace(panels[fillIndex], true);
        var group = new MaPanelGroup { Height = 300 };
        foreach (var p in panels) group.Items.Add(p);
        h.Show(group);
        h.Pump();

        var autoTotal = 0.0;
        for (var i = 0; i < panels.Length; i++)
        {
            if (i == fillIndex) continue;
            Assert.Equal(panels[i].DesiredSize.Height, panels[i].ActualHeight, 3);
            autoTotal += panels[i].ActualHeight;
        }
        Assert.Equal(300 - autoTotal, panels[fillIndex].ActualHeight, 3);

        var y = 0.0;
        foreach (var p in panels)
        {
            Assert.Equal(y, TopIn(group, p), 3);
            y += p.ActualHeight;
        }
        Assert.Equal(300, y, 3);
    });

    [Fact]
    public void Without_a_fill_panel_every_panel_takes_its_desired_height() => StaHost.Run(h =>
    {
        var a = Panel("A", 20);
        var b = Panel("B", 30);
        var group = h.Show(new MaPanelGroup { Items = { a, b } });
        h.Pump();

        Assert.Equal(a.DesiredSize.Height, a.ActualHeight, 3);
        Assert.Equal(b.DesiredSize.Height, b.ActualHeight, 3);
        Assert.Equal(a.ActualHeight + b.ActualHeight, group.ActualHeight, 3);
    });

    [Fact]
    public void FillsRemainingSpace_outside_a_group_has_no_effect() => StaHost.Run(h =>
    {
        var panel = Panel("A", 20);
        MaPanelGroup.SetFillsRemainingSpace(panel, true);
        h.Show(panel);

        Assert.Equal(panel.DesiredSize.Height, panel.ActualHeight, 3);
    });

    [Fact]
    public void A_nested_group_lays_out_inside_its_parent_panel() => StaHost.Run(h =>
    {
        var innerAuto = Panel("Idle", 20);
        var innerFill = Panel("Busy", 10);
        MaPanelGroup.SetFillsRemainingSpace(innerFill, true);
        var inner = new MaPanelGroup { Items = { innerAuto, innerFill } };
        var host = new MaPanel { Title = "Sessions", Content = inner };
        MaPanelGroup.SetFillsRemainingSpace(host, true);
        var top = Panel("Usage", 30);
        var outer = new MaPanelGroup { Height = 300, Items = { top, host } };
        h.Show(outer);
        h.Pump();

        Assert.Equal(300 - top.ActualHeight, host.ActualHeight, 3);
        Assert.True(innerAuto.IsNested);
        Assert.Equal(innerAuto.DesiredSize.Height, innerAuto.ActualHeight, 3);
        Assert.Equal(inner.ActualHeight - innerAuto.ActualHeight, innerFill.ActualHeight, 3);
        Assert.True(innerFill.ActualHeight > innerFill.DesiredSize.Height);
        Assert.True(TopIn(outer, inner) >= TopIn(outer, host));
        Assert.True(TopIn(outer, inner) + inner.ActualHeight <= TopIn(outer, host) + host.ActualHeight + 0.001);
    });

    [Fact]
    public void View_models_in_ItemsSource_get_MaPanel_containers_in_order() => StaHost.Run(h =>
    {
        var group = h.Show(new MaPanelGroup { ItemsSource = new[] { "one", "two" } });
        h.Pump();

        var first = Assert.IsType<MaPanel>(group.ItemContainerGenerator.ContainerFromIndex(0));
        var second = Assert.IsType<MaPanel>(group.ItemContainerGenerator.ContainerFromIndex(1));
        Assert.Equal("one", first.Content);
        Assert.Equal("two", second.Content);
        Assert.True(TopIn(group, first) < TopIn(group, second));
    });

    private static MaPanel Movable(string key, bool draggable = true)
    {
        var panel = Panel(key, 10);
        MaPanelGroup.SetPanelKey(panel, key);
        MaPanelGroup.SetIsDraggable(panel, draggable);
        return panel;
    }

    private static string?[] Keys(MaPanelGroup group) =>
        group.Items.Cast<MaPanel>().Select(MaPanelGroup.GetPanelKey).ToArray();

    [Fact]
    public void MovePanel_reorders_a_draggable_panel_and_raises_OrderChanged_with_the_keys() => StaHost.Run(h =>
    {
        var a = Movable("a");
        var b = Movable("b");
        var c = Movable("c");
        var group = h.Show(new MaPanelGroup { Items = { a, b, c } });
        PanelOrderChangedEventArgs? args = null;
        group.OrderChanged += (_, e) => args = e;

        Assert.True(group.MovePanel(a, 2));

        Assert.Equal(new[] { "b", "c", "a" }, Keys(group));
        Assert.NotNull(args);
        Assert.Same(a, args!.MovedItem);
        Assert.Equal(0, args.OldIndex);
        Assert.Equal(2, args.NewIndex);
        Assert.Equal(new[] { "b", "c", "a" }, args.PanelKeys);
    });

    [Fact]
    public void Fixed_panels_keep_their_slots_and_cannot_be_moved_or_landed_on() => StaHost.Run(h =>
    {
        var a = Movable("a");
        var pinned = Movable("pinned", draggable: false);
        var c = Movable("c");
        var d = Movable("d");
        var group = h.Show(new MaPanelGroup { Items = { a, pinned, c, d } });
        var raised = 0;
        group.OrderChanged += (_, _) => raised++;

        Assert.False(group.MovePanel(pinned, 0));
        Assert.False(group.MovePanel(a, 1));
        Assert.Equal(0, raised);

        Assert.True(group.MovePanel(a, 3));
        Assert.Equal(new[] { "c", "pinned", "d", "a" }, Keys(group));
        Assert.Equal(1, raised);
    });

    [Fact]
    public void Alt_Up_and_Down_on_a_header_move_the_panel_past_fixed_slots_and_announce_it() => StaHost.Run(h =>
    {
        var a = Movable("a");
        var pinned = Movable("pinned", draggable: false);
        var c = Movable("c");
        var group = h.Show(new MaPanelGroup { Items = { a, pinned, c } });
        h.Pump();
        var header = (UIElement)c.Template.FindName(MaPanel.PartHeader, c);

        Assert.False(group.HandleReorderKey(header, Key.Up, ModifierKeys.None));
        Assert.False(group.HandleReorderKey(c, Key.Up, ModifierKeys.Alt));
        Assert.True(group.HandleReorderKey(header, Key.Up, ModifierKeys.Alt));
        Assert.Equal(new[] { "c", "pinned", "a" }, Keys(group));
        Assert.Equal("c moved to position 1 of 3", group.LastAnnouncement);

        Assert.False(group.HandleReorderKey(header, Key.Up, ModifierKeys.Alt));
        Assert.True(group.HandleReorderKey(header, Key.Down, ModifierKeys.Alt));
        Assert.Equal(new[] { "a", "pinned", "c" }, Keys(group));
    });

    [Fact]
    public void An_ItemsSource_list_has_its_item_moved() => StaHost.Run(h =>
    {
        var source = new ObservableCollection<string> { "one", "two", "three" };
        var style = new Style(typeof(MaPanel));
        style.Setters.Add(new Setter(MaPanelGroup.IsDraggableProperty, true));
        style.Setters.Add(new Setter(MaPanelGroup.PanelKeyProperty, new Binding()));
        var group = h.Show(new MaPanelGroup { ItemsSource = source, ItemContainerStyle = style });
        h.Pump();
        PanelOrderChangedEventArgs? args = null;
        group.OrderChanged += (_, e) => args = e;

        var first = (MaPanel)group.ItemContainerGenerator.ContainerFromIndex(0);
        Assert.True(group.MovePanel(first, 2));

        Assert.Equal(new[] { "two", "three", "one" }, source);
        Assert.Equal("one", args!.MovedItem);
        Assert.Equal(new[] { "two", "three", "one" }, args.PanelKeys);
    });

    [Fact]
    public void A_read_only_ItemsSource_is_left_to_the_host() => StaHost.Run(h =>
    {
        var source = new[] { "one", "two" };
        var style = new Style(typeof(MaPanel));
        style.Setters.Add(new Setter(MaPanelGroup.IsDraggableProperty, true));
        var group = h.Show(new MaPanelGroup { ItemsSource = source, ItemContainerStyle = style });
        h.Pump();
        PanelOrderChangedEventArgs? args = null;
        group.OrderChanged += (_, e) => args = e;

        Assert.True(group.MovePanel((MaPanel)group.ItemContainerGenerator.ContainerFromIndex(1), 0));

        Assert.Equal(new[] { "one", "two" }, source);
        Assert.Equal("two", args!.MovedItem);
        Assert.Equal(0, args.NewIndex);
    });
}
