using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using MindAttic.Components.Wpf.Controls;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T221: MaPanel, the titled container -- a Title header that collapses when null, a light-gray rule
/// under it that ShowDivider hides, and one content inset that a nested panel doesn't double.</summary>
public sealed class MaPanelTests
{
    private static T Part<T>(Control owner, string name) where T : class => (T)owner.Template.FindName(name, owner);

    private static double LeftIn(Window window, UIElement element) => element.TranslatePoint(new Point(0, 0), window).X;

    [Fact]
    public void A_title_shows_the_header_and_no_title_collapses_it() => StaHost.Run(h =>
    {
        var titled = h.Show(new MaPanel { Title = "Sessions", Content = new TextBlock { Text = "a" } });
        var untitled = h.Show(new MaPanel { Content = new TextBlock { Text = "b" } });

        Assert.Equal(Visibility.Visible, Part<FrameworkElement>(titled, MaPanel.PartHeader).Visibility);
        Assert.Equal("Sessions", Part<ContentPresenter>(titled, MaPanel.PartTitle).Content);
        Assert.Equal(Visibility.Collapsed, Part<FrameworkElement>(untitled, MaPanel.PartHeader).Visibility);

        untitled.Title = "Later";
        h.Pump();
        Assert.Equal(Visibility.Visible, Part<FrameworkElement>(untitled, MaPanel.PartHeader).Visibility);
    });

    [Fact]
    public void The_divider_is_a_light_gray_rule_that_ShowDivider_hides() => StaHost.Run(h =>
    {
        var panel = h.Show(new MaPanel { Title = "Sessions" });
        var divider = Part<Border>(panel, MaPanel.PartDivider);

        Assert.True(panel.ShowDivider);
        Assert.Equal(Visibility.Visible, divider.Visibility);
        Assert.Equal(1, divider.ActualHeight);
        Assert.Same(h.Window.FindResource("MaMuted"), divider.Background);

        panel.ShowDivider = false;
        h.Pump();
        Assert.Equal(Visibility.Collapsed, divider.Visibility);
    });

    [Fact]
    public void Content_is_set_in_by_the_shared_MaPanelInset() => StaHost.Run(h =>
    {
        var content = new Border { Height = 10 };
        var panel = h.Show(new MaPanel { Title = "Sessions", Content = content });

        Assert.False(panel.IsNested);
        Assert.Equal((Thickness)h.Window.FindResource("MaPanelInset"), panel.ContentInset);
        Assert.Equal(LeftIn(h.Window, panel) + panel.ContentInset.Left, LeftIn(h.Window, content), 3);
    });

    [Fact]
    public void A_nested_panels_header_and_content_keep_the_parents_content_left_edge() => StaHost.Run(h =>
    {
        var other = new TextBlock { Text = "other" };
        var innerContent = new Border { Height = 10 };
        var inner = new MaPanel { Title = "Idle", Content = innerContent };
        var outer = h.Show(new MaPanel { Title = "Sessions", Content = new StackPanel { Children = { other, inner } } });
        h.Pump();

        Assert.False(outer.IsNested);
        Assert.True(inner.IsNested);
        Assert.Equal(LeftIn(h.Window, other), LeftIn(h.Window, innerContent), 3);
        Assert.Equal(LeftIn(h.Window, other), LeftIn(h.Window, Part<ContentPresenter>(inner, MaPanel.PartTitle)), 3);
        Assert.True(Part<ContentPresenter>(inner, MaPanel.PartTitle).ActualHeight
                    < Part<ContentPresenter>(outer, MaPanel.PartTitle).ActualHeight);
    });

    [Fact]
    public void A_host_set_inset_on_a_nested_panel_wins() => StaHost.Run(h =>
    {
        var inner = new MaPanel { Title = "Idle", ContentInset = new Thickness(12, 0, 0, 0) };
        h.Show(new MaPanel { Title = "Sessions", Content = inner });
        h.Pump();

        Assert.True(inner.IsNested);
        Assert.Equal(new Thickness(12, 0, 0, 0), inner.ContentInset);
    });

    [Fact]
    public void The_panel_is_an_automation_group_named_by_its_title() => StaHost.Run(h =>
    {
        var panel = h.Show(new MaPanel { Title = "Sessions" });
        var peer = UIElementAutomationPeer.CreatePeerForElement(panel);

        Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
        Assert.Equal("Sessions", peer.GetName());
        Assert.False(panel.Focusable);
    });
}
