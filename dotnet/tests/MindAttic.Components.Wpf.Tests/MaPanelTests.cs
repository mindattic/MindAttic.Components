using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
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

    private static void ClickToggle(MaPanel panel)
    {
        Part<UIElement>(panel, MaPanel.PartToggle).RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonDownEvent,
        });
    }

    private static void PressOnHeader(StaHost h, MaPanel panel, Key key)
    {
        var header = Part<UIElement>(panel, MaPanel.PartHeader);
        header.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(h.Window)!, 0, key)
        {
            RoutedEvent = Keyboard.KeyDownEvent,
        });
        h.Pump();
    }

    [Fact]
    public void Toggling_a_collapsible_panel_hides_its_content_and_rule_but_keeps_the_header() => StaHost.Run(h =>
    {
        var panel = h.Show(new MaPanel { Title = "Sessions", IsCollapsible = true, Content = new TextBlock { Text = "a" } });
        var toggle = Part<FrameworkElement>(panel, MaPanel.PartToggle);

        Assert.True(panel.IsExpanded);
        Assert.Equal(Visibility.Visible, toggle.Visibility);
        Assert.True(Part<UIElement>(panel, MaPanel.PartHeader).Focusable);

        ClickToggle(panel);
        h.Pump();
        Assert.False(panel.IsExpanded);
        Assert.Equal(Visibility.Collapsed, Part<FrameworkElement>(panel, MaPanel.PartContent).Visibility);
        Assert.Equal(Visibility.Collapsed, Part<FrameworkElement>(panel, MaPanel.PartDivider).Visibility);
        Assert.Equal(Visibility.Visible, Part<FrameworkElement>(panel, MaPanel.PartHeader).Visibility);

        PressOnHeader(h, panel, Key.Enter);
        Assert.True(panel.IsExpanded);
        Assert.Equal(Visibility.Visible, Part<FrameworkElement>(panel, MaPanel.PartContent).Visibility);

        PressOnHeader(h, panel, Key.Space);
        Assert.False(panel.IsExpanded);
    });

    [Fact]
    public void A_panel_that_is_not_collapsible_ignores_the_chevron_and_keys() => StaHost.Run(h =>
    {
        var panel = h.Show(new MaPanel { Title = "Sessions", Content = new TextBlock { Text = "a" } });

        Assert.Equal(Visibility.Collapsed, Part<FrameworkElement>(panel, MaPanel.PartToggle).Visibility);
        Assert.False(Part<UIElement>(panel, MaPanel.PartHeader).Focusable);

        ClickToggle(panel);
        PressOnHeader(h, panel, Key.Enter);
        PressOnHeader(h, panel, Key.Space);
        Assert.True(panel.IsExpanded);
    });

    [Fact]
    public void Expanded_and_Collapsed_fire_once_per_toggle() => StaHost.Run(h =>
    {
        var panel = h.Show(new MaPanel { Title = "Sessions", IsCollapsible = true });
        int expanded = 0, collapsed = 0;
        panel.Expanded += (_, _) => expanded++;
        panel.Collapsed += (_, _) => collapsed++;

        ClickToggle(panel);
        Assert.Equal((0, 1), (expanded, collapsed));
        ClickToggle(panel);
        Assert.Equal((1, 1), (expanded, collapsed));
    });

    [Fact]
    public void A_nested_panels_toggle_does_not_raise_its_parents_events() => StaHost.Run(h =>
    {
        var inner = new MaPanel { Title = "Idle", IsCollapsible = true };
        var outer = h.Show(new MaPanel { Title = "Sessions", IsCollapsible = true, Content = inner });
        h.Pump();
        var outerEvents = 0;
        outer.Expanded += (_, _) => outerEvents++;
        outer.Collapsed += (_, _) => outerEvents++;

        ClickToggle(inner);
        PressOnHeader(h, inner, Key.Enter);
        PressOnHeader(h, inner, Key.Space);

        Assert.False(inner.IsExpanded);
        Assert.True(outer.IsExpanded);
        Assert.Equal(0, outerEvents);
    });

    [Fact]
    public void Only_a_collapsible_panel_offers_the_ExpandCollapse_pattern() => StaHost.Run(h =>
    {
        var plain = h.Show(new MaPanel { Title = "Plain" });
        var collapsible = h.Show(new MaPanel { Title = "Sessions", IsCollapsible = true });

        Assert.Null(UIElementAutomationPeer.CreatePeerForElement(plain).GetPattern(PatternInterface.ExpandCollapse));

        var provider = (IExpandCollapseProvider)UIElementAutomationPeer.CreatePeerForElement(collapsible)
            .GetPattern(PatternInterface.ExpandCollapse);
        Assert.Equal(ExpandCollapseState.Expanded, provider.ExpandCollapseState);
        provider.Collapse();
        Assert.False(collapsible.IsExpanded);
        Assert.Equal(ExpandCollapseState.Collapsed, provider.ExpandCollapseState);
        provider.Expand();
        Assert.True(collapsible.IsExpanded);
    });
}
