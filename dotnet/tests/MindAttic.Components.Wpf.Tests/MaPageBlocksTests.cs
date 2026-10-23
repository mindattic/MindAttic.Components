using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using MindAttic.Components.Wpf.Controls;
using MindAttic.Components.Wpf.Mvvm;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T52: the page building blocks the Orchestrator dashboard needs to go library-only: the body scroll
/// area with its gutter, the list styles and their panels, the card / row / bar / divider Border styles, the stat
/// value text and the accent tile.</summary>
public sealed class MaPageBlocksTests
{
    // ---- scroll areas ----

    [Fact]
    public void The_body_scroll_pads_by_its_gutter_and_puts_its_bar_inside_the_right_gutter_once_it_scrolls() => Sta(h =>
    {
        var filler = new Border { Height = 60 };
        var scroll = new ScrollViewer { Style = h.Style("MaBodyScroll"), Height = 120, Content = filler };
        h.Show(scroll);

        Assert.Equal(ScrollBarVisibility.Auto, scroll.VerticalScrollBarVisibility);
        Assert.Equal(ScrollBarVisibility.Disabled, scroll.HorizontalScrollBarVisibility);
        Assert.False(scroll.Focusable);
        Assert.Equal(Visibility.Collapsed, scroll.ComputedVerticalScrollBarVisibility);
        Assert.Equal(new Thickness(24, 0, 24, 16), scroll.Padding);

        filler.Height = 600;
        h.Pump();
        Assert.Equal(Visibility.Visible, scroll.ComputedVerticalScrollBarVisibility);
        Assert.Equal(new Thickness(24, 0, 24 - MaScrollArea.ScrollBarWidth, 16), scroll.Padding);

        MaScrollArea.SetGutter(scroll, new Thickness(16, 0, 16, 12));
        h.Pump();
        Assert.Equal(new Thickness(16, 0, 6, 12), scroll.Padding);
    });

    [Fact]
    public void The_pane_scroll_is_the_body_scroll_with_a_20px_gutter() => Sta(h =>
    {
        var scroll = new ScrollViewer { Style = h.Style("MaPaneScroll"), Height = 100, Content = new Border { Height = 500 } };
        h.Show(scroll);
        Assert.Equal(new Thickness(20, 16, 20, 20), MaScrollArea.GetGutter(scroll));
        Assert.Equal(new Thickness(20, 16, 10, 20), scroll.Padding);
        Assert.Equal(new Thickness(5, 1, 0, 2), MaScrollArea.PaddingFor(new Thickness(5, 1, 4, 2), Visibility.Visible));
        Assert.Equal(new Thickness(5, 1, 4, 2), MaScrollArea.PaddingFor(new Thickness(5, 1, 4, 2), Visibility.Collapsed));
    });

    // ---- lists ----

    [Theory]
    [InlineData("MaList", 0)]
    [InlineData("MaRowList", 8)]
    public void A_row_list_stacks_its_items_with_its_spacing_between_and_none_after_the_last(string style, double gap) => Sta(h =>
    {
        var list = new ItemsControl
        {
            Style = h.Style(style),
            ItemsSource = new[] { "a", "b", "c" },
            ItemTemplate = Template(typeof(Border), Border.HeightProperty, 20d),
        };
        h.Show(list);

        Assert.False(list.Focusable);
        var panel = Assert.IsType<MaStackPanel>(VisualTreeHelper.GetParent(list.ItemContainerGenerator.ContainerFromIndex(0)));
        Assert.Equal(gap, panel.Spacing);
        var tops = Enumerable.Range(0, 3).Select(i => Top(list, i)).ToArray();
        Assert.Equal([0, 20 + gap, 40 + 2 * gap], tops);
        Assert.Equal(60 + 2 * gap, list.ActualHeight);

        MaList.SetSpacing(list, 3);
        h.Pump();
        Assert.Equal(23d, Top(list, 1));
    });

    [Fact]
    public void A_tile_list_with_a_min_item_width_fills_the_row_with_equal_columns() => Sta(h =>
    {
        var list = new ItemsControl
        {
            Style = h.Style("MaTileList"),
            Width = 400,
            ItemsSource = Enumerable.Range(0, 5).ToArray(),
            ItemTemplate = Template(typeof(Border), Border.HeightProperty, 30d),
        };
        MaList.SetMinItemWidth(list, 120);
        h.Show(list);

        var grid = Assert.IsType<MaTileGrid>(VisualTreeHelper.GetParent(list.ItemContainerGenerator.ContainerFromIndex(0)));
        Assert.Equal(10d, grid.Spacing);
        Assert.Equal(10d, grid.RowSpacing);
        Assert.Equal(120d, grid.MinItemWidth);
        var first = (FrameworkElement)list.ItemContainerGenerator.ContainerFromIndex(0);
        var third = (FrameworkElement)list.ItemContainerGenerator.ContainerFromIndex(2);
        var fourth = (FrameworkElement)list.ItemContainerGenerator.ContainerFromIndex(3);
        Assert.Equal((400 - 20) / 3d, first.ActualWidth, 3);
        Assert.Equal(400d, third.TranslatePoint(new Point(third.ActualWidth, 0), list).X, 3); // the last column ends on the edge
        Assert.Equal(40d, fourth.TranslatePoint(default, list).Y); // a second row, 10px down
        Assert.Equal(70d, list.ActualHeight);
    });

    [Fact]
    public void A_tile_list_with_no_min_item_width_flows_its_tiles_at_their_own_widths() => Sta(h =>
    {
        var list = new ItemsControl
        {
            Style = h.Style("MaTileList"),
            Width = 400,
            ItemsSource = new[] { 1, 2, 3 },
            ItemTemplate = Template(typeof(Border), Border.WidthProperty, 150d, Border.HeightProperty, 40d),
        };
        h.Show(list);

        var points = Enumerable.Range(0, 3)
            .Select(i => ((FrameworkElement)list.ItemContainerGenerator.ContainerFromIndex(i)).TranslatePoint(default, list)).ToArray();
        Assert.Equal(new Point(0, 0), points[0]);
        Assert.Equal(new Point(160, 0), points[1]);
        Assert.Equal(new Point(0, 50), points[2]);
    });

    [Fact]
    public void A_balanced_tile_grid_spreads_its_tiles_evenly_over_the_fewest_rows()
    {
        Assert.Equal(6, MaTileGrid.ColumnsFor(1000, 8, 150, 10, balanced: false));
        Assert.Equal(4, MaTileGrid.ColumnsFor(1000, 8, 150, 10, balanced: true));
        Assert.Equal(1, MaTileGrid.ColumnsFor(100, 3, 150, 10, balanced: false));
        Assert.Equal(95d, MaTileGrid.ItemWidth(200, 2, 10));
    }

    [Fact]
    public void Uniform_rows_make_every_row_as_tall_as_the_tallest_tile() => Sta(h =>
    {
        var grid = new MaTileGrid { Width = 300, MinItemWidth = 140, UniformRows = true };
        grid.Children.Add(new Border { Height = 20 });
        grid.Children.Add(new Border { Height = 50 });
        grid.Children.Add(new Border { Height = 10 });
        grid.Children.Add(new Border { Height = 10, Visibility = Visibility.Collapsed });
        h.Show(grid);
        Assert.Equal(110d, grid.ActualHeight); // two rows of 50 and the 10px row gap; the collapsed one takes nothing
        Assert.Equal(new Rect(0, 60, 145, 50), LayoutInformation.GetLayoutSlot((FrameworkElement)grid.Children[2])); // a full-height slot on row 2
    });

    // ---- Border styles and text ----

    [Fact]
    public void The_card_styles_are_MaCard_without_the_margin_a_list_owns() => Sta(h =>
    {
        var list = Shown<Border>(h, "MaListCard");
        Assert.Equal(new Thickness(0), list.Margin);
        Assert.Equal(new Thickness(12, 10, 12, 10), list.Padding);
        Assert.Same(h.Window.FindResource("MaSurface"), list.Background);
        Assert.Same(h.Window.FindResource("MaChromeBorder"), list.BorderBrush);
        Assert.Equal(new CornerRadius(6), list.CornerRadius);

        Assert.Equal(new Thickness(10, 8, 10, 8), Shown<Border>(h, "MaRowCard").Padding);
        Assert.Equal(new Thickness(0), Shown<Border>(h, "MaAccentCard").Padding);

        var inset = Shown<Border>(h, "MaInsetCard");
        Assert.Same(h.Window.FindResource("MaChrome"), inset.Background);
        Assert.Equal(new Thickness(0, 10, 0, 0), inset.Margin);
    });

    [Fact]
    public void The_bar_divider_pane_status_bar_veil_and_cover_draw_in_theme_tokens() => Sta(h =>
    {
        var bar = Shown<Border>(h, "MaAccentBar");
        Assert.Equal(5d, bar.Width);
        Assert.Equal(Dock.Left, DockPanel.GetDock(bar));
        Assert.Same(h.Window.FindResource("MaAccent"), bar.Background);

        var divider = Shown<Border>(h, "MaDivider");
        Assert.Equal(1d, divider.ActualHeight);
        Assert.Same(h.Window.FindResource("MaChromeBorder"), divider.Background);
        Assert.False(divider.IsHitTestVisible);

        var pane = Shown<Border>(h, "MaSidePane");
        Assert.Equal(new Thickness(1, 0, 0, 0), pane.BorderThickness);
        Assert.Same(h.Window.FindResource("MaChrome"), pane.Background);

        var status = Shown<Border>(h, "MaStatusBar");
        Assert.Equal(new Thickness(0, 1, 0, 0), status.BorderThickness);
        Assert.Equal(new Thickness(24, 6, 24, 6), status.Padding);

        var veil = Shown<Border>(h, "MaVeil");
        Assert.Equal(0.82, veil.Opacity, 3);
        Assert.Same(h.Window.FindResource("MaSurface"), veil.Background);

        var cover = Shown<Border>(h, "MaCover");
        Assert.Equal(100, Panel.GetZIndex(cover));
        Assert.Same(h.Window.FindResource("MaChrome"), cover.Background);
    });

    [Fact]
    public void A_stat_value_is_24px_SemiBold_with_tabular_figures() => Sta(h =>
    {
        var value = Shown<TextBlock>(h, "MaStatValue");
        Assert.Equal(24d, value.FontSize);
        Assert.Equal(FontWeights.SemiBold, value.FontWeight);
        Assert.Equal(FontNumeralAlignment.Tabular, Typography.GetNumeralAlignment(value));
    });

    // ---- tiles ----

    [Fact]
    public void An_accent_tile_draws_its_left_bar_in_its_own_accent() => Sta(h =>
    {
        var tile = new MaAccentTile { Content = new TextBlock { Text = "MindAttic.Components" }, Width = 240 };
        h.Show(tile);
        var bar = (Border)tile.Template.FindName(MaAccentTile.PartAccentBar, tile);

        Assert.Same(h.Window.FindResource("MaAccent"), bar.Background);
        Assert.Equal(4d, bar.ActualWidth);
        Assert.True(tile.ActualHeight >= 64);
        Assert.Equal(new Thickness(0), tile.Margin);
        Assert.Same(h.Window.FindResource("MaSurface"), tile.Background);
        Assert.Same(h.Window.FindResource("MaFocusRing"), tile.FocusVisualStyle);

        tile.Accent = Brushes.Orange;
        tile.AccentWidth = 6;
        h.Pump();
        Assert.Same(Brushes.Orange, bar.Background);
        Assert.Equal(6d, bar.ActualWidth);
    });

    [Fact]
    public void An_accent_tile_is_a_button_a_screen_reader_can_invoke() => Sta(h =>
    {
        var clicked = 0;
        var tile = new MaAccentTile { Content = "Repo", Command = new RelayCommand(() => clicked++) };
        AutomationProperties.SetName(tile, "MindAttic.Components");
        h.Show(tile);

        var peer = UIElementAutomationPeer.CreatePeerForElement(tile);
        Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
        Assert.Equal(nameof(MaAccentTile), peer.GetClassName());
        Assert.Equal("MindAttic.Components", peer.GetName());
        Assert.True(tile.Focusable);
        ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();
        h.Pump();
        Assert.Equal(1, clicked);
    });

    [Fact]
    public void A_selected_tile_is_outlined_in_its_selected_brush_and_reads_Selected() => Sta(h =>
    {
        var tile = new MaAccentTile { Content = "Repo" };
        var stat = new Button { Style = h.Style("MaSmallStat"), Content = "3" };
        h.Show(tile);
        h.Show(stat);
        Assert.Equal(new Thickness(1), tile.BorderThickness);

        MaTile.SetIsSelected(tile, true);
        MaTile.SetIsSelected(stat, true);
        MaTile.SetSelectedBrush(stat, (Brush)h.Window.FindResource("MaWarn"));
        h.Pump();
        Assert.Same(h.Window.FindResource("MaFocus"), tile.BorderBrush);
        Assert.Equal(new Thickness(2), tile.BorderThickness);
        Assert.Same(h.Window.FindResource("MaWarn"), stat.BorderBrush);
        Assert.Equal(MaTile.SelectedStatus, UIElementAutomationPeer.CreatePeerForElement(tile).GetItemStatus());

        MaTile.SetIsSelected(tile, false);
        h.Pump();
        Assert.Same(h.Window.FindResource("MaControlBorder"), tile.BorderBrush);
        Assert.Equal(DependencyProperty.UnsetValue, tile.ReadLocalValue(AutomationProperties.ItemStatusProperty));

        AutomationProperties.SetItemStatus(stat, "Filtering");
        MaTile.SetIsSelected(stat, false);
        Assert.Equal("Filtering", AutomationProperties.GetItemStatus(stat));
    });

    [Fact]
    public void The_new_pieces_follow_high_contrast_live() => Sta(h =>
    {
        var tile = new MaAccentTile { Content = "Repo" };
        var card = Shown<Border>(h, "MaListCard");
        var bar = Shown<Border>(h, "MaAccentBar");
        h.Show(tile);

        MaHighContrast.Apply(h.Window.Resources, on: true);
        h.Pump();
        Assert.Equal(SystemColors.WindowBrush.Color, ((SolidColorBrush)tile.Background).Color);
        Assert.Equal(SystemColors.WindowBrush.Color, ((SolidColorBrush)card.Background).Color);
        Assert.Equal(SystemColors.HighlightBrush.Color, ((SolidColorBrush)bar.Background).Color);
        Assert.Equal(SystemColors.HighlightBrush.Color,
            ((SolidColorBrush)((Border)tile.Template.FindName(MaAccentTile.PartAccentBar, tile)).Background).Color);

        MaHighContrast.Apply(h.Window.Resources, on: false);
        h.Pump();
        Assert.Same(h.Window.FindResource("MaSurface"), tile.Background);
    });

    // ---- harness ----

    private static DataTemplate Template(Type type, params object[] propertyValues)
    {
        var factory = new FrameworkElementFactory(type);
        for (var i = 0; i < propertyValues.Length; i += 2)
            factory.SetValue((DependencyProperty)propertyValues[i], propertyValues[i + 1]);
        return new DataTemplate { VisualTree = factory };
    }

    private static double Top(ItemsControl list, int index) =>
        ((FrameworkElement)list.ItemContainerGenerator.ContainerFromIndex(index)).TranslatePoint(default, list).Y;

    private static T Shown<T>(Host h, string style) where T : FrameworkElement, new()
    {
        var element = new T { Style = h.Style(style) };
        h.Show(element);
        return element;
    }

    private sealed class Host
    {
        private readonly StackPanel _root = new();
        public Window Window { get; }

        public Host()
        {
            Window = OffScreenWindow.Show(600, 400, _root);
            Pump();
        }

        public Style Style(string key) => (Style)Window.FindResource(key);

        public void Show(FrameworkElement element)
        {
            _root.Children.Add(element);
            Pump();
            element.ApplyTemplate();
            Pump();
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
