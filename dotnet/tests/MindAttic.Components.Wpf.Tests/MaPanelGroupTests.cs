using System.Windows;
using System.Windows.Controls;
using MindAttic.Components.Wpf.Controls;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T223: MaPanelGroup stacks MaPanels at their desired heights, gives the leftover height to the
/// FillsRemainingSpace panel wherever it sits, and nests inside a panel.</summary>
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
}
