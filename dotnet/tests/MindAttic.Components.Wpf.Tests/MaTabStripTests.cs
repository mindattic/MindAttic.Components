using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using MindAttic.Components.Wpf.Controls;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T50: MaStatusLight and the MaTabControl / MaTabItem tab strip the Orchestrator's per-person
/// dashboard builds its person tabs, Dashboard/Terminals sub-tabs and terminal pane tabs from.</summary>
public sealed class MaTabStripTests
{
    // ---- status light ----

    [Theory]
    [InlineData(MaStatus.Good, "MaGood", "Good")]
    [InlineData(MaStatus.Warning, "MaWarn", "Warning")]
    [InlineData(MaStatus.Bad, "MaBad", "Bad")]
    [InlineData(MaStatus.Other, "MaOther", "Other")]
    public void A_status_light_fills_with_its_theme_token_and_is_named_by_its_status(MaStatus status, string key, string word) => Sta(h =>
    {
        var light = new MaStatusLight { Status = status };
        h.Show(light);
        var dot = (Ellipse)light.Template.FindName("PART_Dot", light);

        Assert.Equal(key, MaStatusLight.BrushKey(status));
        Assert.Same(h.Window.FindResource(key), dot.Fill);
        Assert.Equal(8d, light.ActualWidth);
        Assert.Equal(8d, light.ActualHeight);
        Assert.False(light.Focusable);
        Assert.Equal(word, light.ToolTip);

        var peer = UIElementAutomationPeer.CreatePeerForElement(light);
        Assert.Equal(word, peer.GetName());
        Assert.Equal(word, peer.GetItemStatus());
        Assert.Equal(AutomationControlType.Image, peer.GetAutomationControlType());
    });

    [Fact]
    public void A_label_names_the_light_and_a_status_change_recolours_it_live() => Sta(h =>
    {
        var light = new MaStatusLight { Status = MaStatus.Good, Label = "Running" };
        h.Show(light);
        var dot = (Ellipse)light.Template.FindName("PART_Dot", light);
        var peer = UIElementAutomationPeer.CreatePeerForElement(light);
        Assert.Equal("Running", peer.GetName());
        Assert.Equal("Good", peer.GetItemStatus());

        light.Status = MaStatus.Bad;
        light.Label = null;
        h.Pump();
        Assert.Same(h.Window.FindResource("MaBad"), dot.Fill);
        Assert.Equal("Bad", peer.GetName());

        AutomationProperties.SetName(light, "Ryan: stopped");
        Assert.Equal("Ryan: stopped", peer.GetName());
    });

    [Fact]
    public void A_re_keyed_status_brush_restyles_the_light_live() => Sta(h =>
    {
        var light = new MaStatusLight { Status = MaStatus.Other };
        h.Show(light);
        var dot = (Ellipse)light.Template.FindName("PART_Dot", light);
        h.Window.Resources["MaOther"] = Brushes.Magenta;
        h.Pump();
        Assert.Same(Brushes.Magenta, dot.Fill);
    });

    // ---- tab strip ----

    [Fact]
    public void The_selected_tab_is_lifted_and_underlined_in_its_own_accent() => Sta(h =>
    {
        var (tabs, a, b) = TwoTabs(h);
        b.Accent = Brushes.Orange;
        h.Pump();

        var aLine = Part<Border>(a, MaTabItem.PartUnderline);
        var bLine = Part<Border>(b, MaTabItem.PartUnderline);
        Assert.True(a.IsSelected);
        Assert.Equal(Visibility.Visible, aLine.Visibility);
        Assert.Same(h.Window.FindResource("MaAccent"), aLine.Background);
        Assert.Same(h.Window.FindResource("MaText"), a.Foreground);
        Assert.Same(h.Window.FindResource("MaSurface"), Part<Border>(a, "Bd").Background);
        Assert.Equal(Visibility.Hidden, bLine.Visibility);
        Assert.Same(h.Window.FindResource("MaMuted"), b.Foreground);

        tabs.SelectedIndex = 1;
        h.Pump();
        Assert.Equal(Visibility.Hidden, aLine.Visibility);
        Assert.Equal(Visibility.Visible, bLine.Visibility);
        Assert.Same(Brushes.Orange, bLine.Background);
    });

    [Fact]
    public void Trailing_text_and_status_light_show_only_when_set_and_join_the_item_status() => Sta(h =>
    {
        var (tabs, a, b) = TwoTabs(h);
        a.Status = MaStatus.Good;
        a.Trailing = "23% left";
        h.Pump();

        Assert.Equal(Visibility.Visible, Part<MaStatusLight>(a, MaTabItem.PartStatusLight).Visibility);
        Assert.Equal(MaStatus.Good, Part<MaStatusLight>(a, MaTabItem.PartStatusLight).Status);
        var trailing = Part<TextBlock>(a, MaTabItem.PartTrailing);
        Assert.Equal(Visibility.Visible, trailing.Visibility);
        Assert.Equal("23% left", trailing.Text);
        Assert.Same(h.Window.FindResource("MaMuted"), trailing.Foreground);
        Assert.Equal(Visibility.Collapsed, Part<MaStatusLight>(b, MaTabItem.PartStatusLight).Visibility);
        Assert.Equal(Visibility.Collapsed, Part<TextBlock>(b, MaTabItem.PartTrailing).Visibility);

        var items = UIElementAutomationPeer.CreatePeerForElement(tabs).GetChildren()!;
        var aPeer = Assert.Single(items, p => p.GetName() == "Ryan");
        Assert.Equal(AutomationControlType.TabItem, aPeer.GetAutomationControlType());
        Assert.Equal("Good, 23% left", aPeer.GetItemStatus());

        a.StatusLabel = "Running";
        a.Status = MaStatus.Bad;
        Assert.Equal("Running, 23% left", a.GetValue(AutomationProperties.ItemStatusProperty));
        a.Status = null;
        a.Trailing = null;
        Assert.Equal(DependencyProperty.UnsetValue, a.ReadLocalValue(AutomationProperties.ItemStatusProperty));

        AutomationProperties.SetItemStatus(b, "host's own");
        b.Trailing = "5% left";
        Assert.Equal("host's own", AutomationProperties.GetItemStatus(b));
    });

    [Fact]
    public void Bound_items_get_MaTabItem_containers_styled_through_a_based_on_container_style() => Sta(h =>
    {
        var style = new Style(typeof(MaTabItem), (Style)h.Window.FindResource(typeof(MaTabItem)));
        style.Setters.Add(new Setter(MaTabItem.TrailingProperty, new System.Windows.Data.Binding("Left")));
        var tabs = new MaTabControl
        {
            ItemsSource = new[] { new Person("Ryan", "23% left"), new Person("Ada", "80% left") },
            DisplayMemberPath = nameof(Person.Name),
            ItemContainerStyle = style,
            ShowContent = false,
        };
        h.Show(tabs);

        var first = Assert.IsType<MaTabItem>(tabs.ItemContainerGenerator.ContainerFromIndex(0));
        first.ApplyTemplate();
        Assert.Equal("23% left", Part<TextBlock>(first, MaTabItem.PartTrailing).Text);
        Assert.NotNull(first.Template.FindName(MaTabItem.PartUnderline, first));
        var content = (FrameworkElement)tabs.Template.FindName("ContentArea", tabs);
        Assert.Equal(Visibility.Collapsed, content.Visibility);
    });

    [Fact]
    public void Home_and_End_move_the_selection_and_focus_like_any_TabControl() => Sta(h =>
    {
        var (tabs, a, b) = TwoTabs(h);
        Assert.True(a.Focus());
        Assert.True(a.IsKeyboardFocused);
        Assert.Equal(KeyboardNavigationMode.Once,
            KeyboardNavigation.GetTabNavigation((DependencyObject)tabs.Template.FindName(MaTabControl.PartHeaderPanel, tabs)));

        Press(h, tabs, Key.End);
        Assert.True(b.IsSelected);
        Assert.True(b.IsKeyboardFocused);
        Press(h, tabs, Key.Home);
        Assert.True(a.IsSelected);

        var peer = (ISelectionItemProvider)UIElementAutomationPeer.CreatePeerForElement(tabs).GetChildren()!
            .Single(p => p.GetName() == "Ada").GetPattern(PatternInterface.SelectionItem);
        peer.Select();
        Assert.True(b.IsSelected);
    });

    [Fact]
    public void MaOther_is_a_theme_brush_high_contrast_swaps() =>
        Assert.Contains("MaOther", MaHighContrast.Foregrounds);

    // ---- harness ----

    public sealed record Person(string Name, string Left);

    private static (MaTabControl Tabs, MaTabItem A, MaTabItem B) TwoTabs(Host h)
    {
        var a = new MaTabItem { Header = "Ryan", Content = new TextBlock { Text = "Ryan's dashboard" } };
        var b = new MaTabItem { Header = "Ada", Content = new TextBlock { Text = "Ada's dashboard" } };
        var tabs = new MaTabControl { Items = { a, b } };
        h.Show(tabs);
        a.ApplyTemplate();
        b.ApplyTemplate();
        h.Pump();
        return (tabs, a, b);
    }

    private static T Part<T>(Control owner, string name) where T : class => (T)owner.Template.FindName(name, owner);

    private static void Press(Host h, UIElement target, Key key)
    {
        var source = PresentationSource.FromVisual(h.Window)!;
        var focused = Keyboard.FocusedElement as UIElement ?? target;
        focused.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key) { RoutedEvent = Keyboard.KeyDownEvent });
        h.Pump();
    }

    private sealed class Host
    {
        private readonly StackPanel _root = new();
        public Window Window { get; }

        public Host()
        {
            Window = OffScreenWindow.Show(400, 200, _root);
            Pump();
        }

        public void Show(FrameworkElement element)
        {
            _root.Children.Add(element);
            Pump();
            element.ApplyTemplate();
        }

        public void Pump() => Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ContextIdle, () => { });
    }

    private static void Sta(Action<Host> test)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            Host? host = null;
            try
            {
                host = new Host();
                test(host);
            }
            catch (Exception ex)
            {
                failure = ExceptionDispatchInfo.Capture(ex);
            }
            finally
            {
                host?.Window.Close();
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}
