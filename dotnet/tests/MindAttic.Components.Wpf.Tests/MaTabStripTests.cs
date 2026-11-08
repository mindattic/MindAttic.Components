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
    public void TrailingContent_draws_arbitrary_content_in_its_own_template_and_collapses_when_null() => Sta(h =>
    {
        var (tabs, a, b) = TwoTabs(h);
        a.TrailingContentTemplate = new DataTemplate(typeof(string)) { VisualTree = new FrameworkElementFactory(typeof(TextBlock)) };
        a.TrailingContentTemplate.VisualTree!.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding());
        a.TrailingContent = "15% / 29%";
        h.Pump();

        var content = Part<ContentPresenter>(a, MaTabItem.PartTrailingContent);
        Assert.Equal(Visibility.Visible, content.Visibility);
        Assert.Equal("15% / 29%", content.Content);
        Assert.Equal(Visibility.Collapsed, Part<ContentPresenter>(b, MaTabItem.PartTrailingContent).Visibility);

        a.TrailingContent = null;
        h.Pump();
        Assert.Equal(Visibility.Collapsed, content.Visibility);
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
    public void Left_placement_stacks_the_tabs_in_a_column_with_the_rule_on_its_right() => Sta(h =>
    {
        var (tabs, a, b) = TwoTabs(h);
        tabs.TabStripPlacement = Dock.Left;
        h.Pump();

        var header = (Border)tabs.Template.FindName("HeaderArea", tabs);
        var content = (FrameworkElement)tabs.Template.FindName("ContentArea", tabs);
        Assert.Equal(new Thickness(0, 0, 1, 0), header.BorderThickness);
        Assert.Equal(0, Grid.GetColumn(header));
        Assert.Equal(1, Grid.GetColumn(content));
        Assert.Equal(0, Grid.GetRow(content));
        Assert.True(b.TranslatePoint(new Point(), a).Y >= a.ActualHeight, "the second tab sits below the first");
        Assert.Equal(new Thickness(0, 0, 0, 2), a.Margin);

        var bar = Part<Border>(a, MaTabItem.PartUnderline);
        Assert.Equal(3d, bar.Width);
        Assert.Equal(HorizontalAlignment.Left, bar.HorizontalAlignment);
        Assert.True(bar.ActualHeight > bar.ActualWidth, "a leading-edge bar, not an underline");

        Assert.Equal(Dock.Right, DockPanel.GetDock(Part<TextBlock>(a, MaTabItem.PartTrailing)));
        Assert.Equal(Dock.Right, DockPanel.GetDock(Part<ContentPresenter>(a, MaTabItem.PartTrailingContent)));

        tabs.ShowContent = false;
        h.Pump();
        Assert.Equal(new Thickness(0), header.BorderThickness);

        Assert.True(a.Focus());
        Assert.True(a.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down)));
        h.Pump();
        Assert.True(b.IsSelected);
    });

    [Fact]
    public void MaOther_is_a_theme_brush_high_contrast_swaps() =>
        Assert.Contains("MaOther", MaHighContrast.Foregrounds);

    // ---- drag-to-reorder (M.COMP-T168) ----

    [Fact]
    public void AllowReorder_and_IsLocked_default_to_false() => Sta(_ =>
    {
        var tabs = new MaTabControl();
        var item = new MaTabItem();
        Assert.False(tabs.AllowReorder);
        Assert.False(item.IsLocked);
    });

    // These drive MaTabItem's internal HandleMouseDown/Move/Up directly with plain Points rather than
    // raising synthetic mouse events: WPF's MouseEventArgs.GetPosition reads real OS cursor state, which a
    // unit test can't drive, so the gesture's state machine takes explicit positions instead (see the XML
    // doc on HandleMouseDown) and that's exactly the seam these tests exercise.

    [Fact]
    public void A_drag_with_AllowReorder_off_raises_nothing() => Sta(h =>
    {
        var (tabs, a, _) = TwoTabs(h);
        TabReorderEventArgs? raised = null;
        tabs.Reordered += (_, e) => raised = e;

        a.HandleMouseDown(tabs, new Point(10, 0), new Point(0, 0));
        a.HandleMouseMove(tabs, new Point(200, 0), new Point(190, 0));
        a.HandleMouseUp(tabs, new Point(200, 0));

        Assert.Null(raised);
    });

    [Fact]
    public void A_locked_tab_never_starts_a_drag_even_with_AllowReorder_on() => Sta(h =>
    {
        var (tabs, a, _) = TwoTabs(h);
        tabs.AllowReorder = true;
        a.IsLocked = true;
        TabReorderEventArgs? raised = null;
        tabs.Reordered += (_, e) => raised = e;

        a.HandleMouseDown(tabs, new Point(10, 0), new Point(0, 0));
        a.HandleMouseMove(tabs, new Point(200, 0), new Point(190, 0));
        a.HandleMouseUp(tabs, new Point(200, 0));

        Assert.Null(raised);
    });

    [Fact]
    public void Dragging_past_the_drop_distance_threshold_reorders_and_raises_once() => Sta(h =>
    {
        var (tabs, a, b) = TwoTabs(h);
        tabs.AllowReorder = true;
        var raised = new List<TabReorderEventArgs>();
        tabs.Reordered += (_, e) => raised.Add(e);
        var past = b.ActualWidth + a.ActualWidth / 2 + 5;

        a.HandleMouseDown(tabs, new Point(10, 0), new Point(0, 0));
        a.HandleMouseMove(tabs, new Point(past, 0), new Point(past - 10, 0));
        a.HandleMouseUp(tabs, new Point(past, 0));

        var e = Assert.Single(raised);
        Assert.Same(a, e.MovedItem);
        Assert.Equal(1, e.NewIndex);
    });

    [Fact]
    public void A_drag_that_ends_back_where_it_started_raises_nothing() => Sta(h =>
    {
        var (tabs, a, _) = TwoTabs(h);
        tabs.AllowReorder = true;
        TabReorderEventArgs? raised = null;
        tabs.Reordered += (_, e) => raised = e;

        // Past the threshold, but not far enough to cross the next tab's midpoint, and back to the start.
        a.HandleMouseDown(tabs, new Point(10, 0), new Point(0, 0));
        a.HandleMouseMove(tabs, new Point(30, 0), new Point(20, 0));
        a.HandleMouseUp(tabs, new Point(10, 0));

        Assert.Null(raised);
    });

    [Fact]
    public void A_mid_drag_capture_loss_cancels_without_raising_Reordered() => Sta(h =>
    {
        var (tabs, a, b) = TwoTabs(h);
        tabs.AllowReorder = true;
        TabReorderEventArgs? raised = null;
        tabs.Reordered += (_, e) => raised = e;
        var past = b.ActualWidth + a.ActualWidth / 2 + 5;

        a.HandleMouseDown(tabs, new Point(10, 0), new Point(0, 0));
        a.HandleMouseMove(tabs, new Point(past, 0), new Point(past - 10, 0));
        a.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = UIElement.LostMouseCaptureEvent });

        Assert.Null(raised);
    });

    [Fact]
    public void Escape_mid_drag_cancels_without_raising_Reordered() => Sta(h =>
    {
        // Unlike position, a KeyEventArgs' Key is part of the constructed args, not live device state, so
        // this one gesture step IS reliably simulated with a real RaiseEvent (see the comment above).
        var (tabs, a, b) = TwoTabs(h);
        tabs.AllowReorder = true;
        TabReorderEventArgs? raised = null;
        tabs.Reordered += (_, e) => raised = e;
        var past = b.ActualWidth + a.ActualWidth / 2 + 5;
        var source = PresentationSource.FromVisual(h.Window)!;

        a.HandleMouseDown(tabs, new Point(10, 0), new Point(0, 0));
        a.HandleMouseMove(tabs, new Point(past, 0), new Point(past - 10, 0));
        a.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Escape) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        a.HandleMouseUp(tabs, new Point(past, 0));

        Assert.Null(raised);
    });

    [Theory]
    // 4 unlocked items (midpoints 50, 150, 250, 350): a pointer past item i's midpoint (and before item
    // i+1's) drops just after i. Comparisons are strict ("<"), so the probe points sit just past each
    // midpoint rather than exactly on it — landing exactly on a boundary is an unexercised tie, not a case
    // these values are meant to pin down.
    [InlineData(new[] { false, false, false, false }, 0, 25.0, 0)] // barely past start: stays put
    [InlineData(new[] { false, false, false, false }, 0, 151.0, 1)]
    [InlineData(new[] { false, false, false, false }, 0, 251.0, 2)]
    [InlineData(new[] { false, false, false, false }, 3, 10.0, 0)] // dragging the last item to the front
    // one locked item fixed at index 0: index 0 is never reachable as a drop target.
    [InlineData(new[] { true, false, false }, 1, 5.0, 1)]
    [InlineData(new[] { true, false, false }, 2, 5.0, 1)]
    public void ComputeDropIndex_matches_ObservableCollection_Move_semantics(bool[] locked, int draggedIndex, double pointer, int expected)
    {
        // Midpoints of same-width-100 tabs laid out left to right: 50, 150, 250, ...
        var midpoints = Enumerable.Range(0, locked.Length).Select(i => 50.0 + i * 100).ToList();

        Assert.Equal(expected, MaTabItem.ComputeDropIndex(midpoints, locked, draggedIndex, pointer));
    }

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
