using System.Windows;
using System.Windows.Controls;
using MindAttic.Components.Wpf.Controls;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T225: a draggable MaPanel in an MaPanelGroup drags by its header grip, driven here through the
/// internal Begin/Update/End seams: it lands past fixed slots via MovePanel, cancels with no change, and a nested
/// group's drag never moves the outer group's panels.</summary>
public sealed class MaPanelDragTests
{
    private static MaPanel Movable(string key, bool draggable = true)
    {
        var panel = new MaPanel { Title = key, Content = new Border { Height = 10 } };
        MaPanelGroup.SetPanelKey(panel, key);
        MaPanelGroup.SetIsDraggable(panel, draggable);
        return panel;
    }

    private static string?[] Keys(MaPanelGroup group) =>
        group.Items.Cast<MaPanel>().Select(MaPanelGroup.GetPanelKey).ToArray();

    private static double TopIn(UIElement relativeTo, UIElement element) => element.TranslatePoint(new Point(0, 0), relativeTo).Y;

    private static Point MidOf(UIElement group, MaPanel panel) => new(10, TopIn(group, panel) + panel.ActualHeight / 2);

    private static Point Below(UIElement group, MaPanel panel) => new(10, TopIn(group, panel) + panel.ActualHeight + 1);

    [Fact]
    public void A_drag_lands_past_fixed_slots_and_raises_OrderChanged_once() => StaHost.Run(h =>
    {
        var a = Movable("a");
        var pinned = Movable("pinned", draggable: false);
        var c = Movable("c");
        var d = Movable("d");
        var group = h.Show(new MaPanelGroup { Items = { a, pinned, c, d } });
        h.Pump();
        var raised = 0;
        group.OrderChanged += (_, _) => raised++;

        Assert.True(a.BeginDrag(group, MidOf(group, a)));
        a.UpdateDrag(MidOf(group, pinned));
        Assert.True(a.IsDragging);
        Assert.Equal(0, a.DropIndex);
        a.UpdateDrag(Below(group, c));
        Assert.Equal(2, a.DropIndex);
        Assert.True(a.EndDrag(Below(group, c)));

        Assert.Equal(new[] { "c", "pinned", "a", "d" }, Keys(group));
        Assert.Equal(1, raised);
        Assert.False(a.IsDragging);
    });

    [Fact]
    public void A_cancelled_drag_drops_nothing() => StaHost.Run(h =>
    {
        var a = Movable("a");
        var b = Movable("b");
        var group = h.Show(new MaPanelGroup { Items = { a, b } });
        h.Pump();
        var raised = 0;
        group.OrderChanged += (_, _) => raised++;

        Assert.True(a.BeginDrag(group, MidOf(group, a)));
        a.UpdateDrag(Below(group, b));
        Assert.Equal(1, a.DropIndex);
        a.CancelDrag();

        Assert.False(a.IsDragging);
        Assert.False(a.EndDrag(Below(group, b)));
        Assert.Equal(new[] { "a", "b" }, Keys(group));
        Assert.Equal(0, raised);
    });

    [Fact]
    public void A_press_that_never_passes_the_threshold_moves_nothing() => StaHost.Run(h =>
    {
        var a = Movable("a");
        var b = Movable("b");
        var group = h.Show(new MaPanelGroup { Items = { a, b } });
        h.Pump();

        Assert.True(a.BeginDrag(group, MidOf(group, a)));
        Assert.False(a.EndDrag(Below(group, b)));
        Assert.Equal(new[] { "a", "b" }, Keys(group));
    });

    [Fact]
    public void A_drag_in_a_nested_group_never_moves_the_outer_groups_panels() => StaHost.Run(h =>
    {
        var innerA = Movable("ia");
        var innerB = Movable("ib");
        var inner = new MaPanelGroup { Items = { innerA, innerB } };
        var outerA = Movable("oa");
        outerA.Content = inner;
        var outerB = Movable("ob");
        var outer = h.Show(new MaPanelGroup { Items = { outerA, outerB } });
        h.Pump();
        var outerRaised = 0;
        outer.OrderChanged += (_, _) => outerRaised++;

        Assert.False(innerA.BeginDrag(outer, MidOf(outer, innerA)));
        Assert.True(innerA.BeginDrag(inner, MidOf(inner, innerA)));
        innerA.UpdateDrag(Below(inner, innerB));
        Assert.True(innerA.EndDrag(Below(inner, innerB)));

        Assert.Equal(new[] { "ib", "ia" }, Keys(inner));
        Assert.Equal(new[] { "oa", "ob" }, Keys(outer));
        Assert.Equal(0, outerRaised);
        Assert.False(outerA.IsDragging);
    });

    [Fact]
    public void The_grip_shows_and_the_header_takes_focus_only_on_a_draggable_panel_in_a_group() => StaHost.Run(h =>
    {
        var draggable = Movable("a");
        var pinned = Movable("pinned", draggable: false);
        var loose = Movable("loose");
        h.Show(new StackPanel { Children = { new MaPanelGroup { Items = { draggable, pinned } }, loose } });
        h.Pump();

        static UIElement Part(MaPanel p, string name) => (UIElement)p.Template.FindName(name, p);
        Assert.True(draggable.CanDrag);
        Assert.Equal(Visibility.Visible, Part(draggable, MaPanel.PartGrip).Visibility);
        Assert.True(Part(draggable, MaPanel.PartHeader).Focusable);
        Assert.False(pinned.CanDrag);
        Assert.Equal(Visibility.Collapsed, Part(pinned, MaPanel.PartGrip).Visibility);
        Assert.False(Part(pinned, MaPanel.PartHeader).Focusable);
        Assert.False(loose.CanDrag);
        Assert.Equal(Visibility.Collapsed, Part(loose, MaPanel.PartGrip).Visibility);

        MaPanelGroup.SetIsDraggable(pinned, true);
        Assert.True(pinned.CanDrag);
    });
}
