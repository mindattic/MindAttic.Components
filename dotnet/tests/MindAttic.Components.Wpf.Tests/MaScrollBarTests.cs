using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T37: the implicit dark ScrollBar and ScrollViewer styles.</summary>
public sealed class MaScrollBarTests
{
    [Fact]
    public void A_vertical_scroll_bar_is_a_slim_thumb_on_chrome() => Sta(window =>
    {
        var viewer = Tall(window);
        var bar = (ScrollBar)viewer.Template.FindName("PART_VerticalScrollBar", viewer);

        Assert.Equal(Visibility.Visible, bar.Visibility);
        Assert.Equal(window.FindResource("MaChrome"), bar.Background);
        Assert.Equal(10, bar.ActualWidth);
        var thumb = bar.Track.Thumb;
        Assert.NotNull(thumb);
        Assert.Equal(window.FindResource("MaControlBorder"), ThumbBorder(thumb).Background);
        Assert.Null(FindChild<RepeatButton>(bar, b => b.Command == ScrollBar.LineUpCommand)); // no arrow buttons
    });

    [Fact]
    public void A_horizontal_scroll_bar_is_slim_too() => Sta(window =>
    {
        var viewer = new ScrollViewer
        {
            Width = 200, Height = 100,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
            Content = new Border { Width = 2000, Height = 50 },
        };
        Host(window).Children.Add(viewer);
        Pump();
        var bar = (ScrollBar)viewer.Template.FindName("PART_HorizontalScrollBar", viewer);

        Assert.Equal(Visibility.Visible, bar.Visibility);
        Assert.Equal(10, bar.ActualHeight);
        Assert.True(bar.ActualWidth > 100);
        Assert.Equal(window.FindResource("MaControlBorder"), ThumbBorder(bar.Track.Thumb).Background);
    });

    [Fact]
    public void The_thumb_lightens_on_hover_and_while_dragged() => Sta(window =>
    {
        var viewer = Tall(window);
        var thumb = ((ScrollBar)viewer.Template.FindName("PART_VerticalScrollBar", viewer)).Track.Thumb;

        // The theme refers to its brushes dynamically (M.COMP-T11), so a setter holds the reference; resolve it here.
        object? Setter(DependencyProperty trigger) => thumb.Template.Triggers.OfType<Trigger>()
            .Where(t => t.Property == trigger && Equals(t.Value, true))
            .SelectMany(t => t.Setters.OfType<Setter>())
            .Where(s => s.TargetName == "Bd" &&s.Property == Border.BackgroundProperty)
            .Select(s => s.Value is DynamicResourceExtension d ? window.FindResource(d.ResourceKey) : s.Value).SingleOrDefault();

        Assert.Equal(window.FindResource("MaMuted"), Setter(UIElement.IsMouseOverProperty));
        Assert.Equal(window.FindResource("MaText"), Setter(Thumb.IsDraggingProperty));
    });

    [Fact]
    public void The_corner_where_the_bars_meet_is_chrome() => Sta(window =>
    {
        var viewer = new ScrollViewer
        {
            Width = 200, Height = 100,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            Content = new Border { Width = 2000, Height = 2000 },
        };
        Host(window).Children.Add(viewer);
        Pump();
        var corner = (Rectangle)viewer.Template.FindName("Corner", viewer);
        Assert.Equal(window.FindResource("MaChrome"), corner.Fill);
    });

    [Fact]
    public void Scrolls_and_follows_the_offset() => Sta(window =>
    {
        var viewer = Tall(window);
        var bar = (ScrollBar)viewer.Template.FindName("PART_VerticalScrollBar", viewer);
        viewer.ScrollToVerticalOffset(500);
        Pump();
        Assert.Equal(500, bar.Value, 1);
        bar.Value = 0;
        ScrollBar.PageDownCommand.Execute(null, bar);
        Pump();
        Assert.True(viewer.VerticalOffset > 0);
    });

    [Fact]
    public void Templated_controls_scroll_bars_are_themed_too() => Sta(window =>
    {
        // A multi-line text box (as MaTextArea is) and a list box: their scroll bars live inside
        // their own templates, and still pick up the implicit style.
        var text = new TextBox
        {
            Width = 200, Height = 80, AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            Text = string.Join("\n", Enumerable.Range(1, 40)),
        };
        var list = new ListBox { Width = 200, Height = 80, ItemsSource = Enumerable.Range(1, 100).ToList() };
        Host(window).Children.Add(text);
        Host(window).Children.Add(list);
        Pump();

        foreach (var control in new Control[] { text, list })
        {
            var bar = FindChild<ScrollBar>(control, b => b.Orientation == Orientation.Vertical);
            Assert.NotNull(bar);
            Assert.Equal(window.FindResource("MaChrome"), bar!.Background);
            Assert.Equal(10, bar.ActualWidth);
            Assert.Equal(window.FindResource("MaControlBorder"), ThumbBorder(bar.Track.Thumb).Background);
        }
    });

    [Fact]
    public void MaTextArea_shares_the_thumb_over_its_own_surface() => Sta(window =>
    {
        var area = new TextBox
        {
            Width = 200, Height = 80, Style = (Style)window.FindResource("MaTextArea"),
            Text = string.Join("\n", Enumerable.Range(1, 40)),
        };
        Host(window).Children.Add(area);
        Pump();

        var bar = FindChild<ScrollBar>(area, b => b.Orientation == Orientation.Vertical && b.IsVisible);
        Assert.NotNull(bar);
        Assert.Equal(10, bar!.ActualWidth);
        Assert.Equal(Brushes.Transparent, bar.Background); // the box's MaSurface shows through
        Assert.Equal(window.FindResource("MaControlBorder"), ThumbBorder(bar.Track.Thumb).Background);
    });

    // ---- harness ----

    private static StackPanel Host(Window window) => (StackPanel)window.Content;

    private static ScrollViewer Tall(Window window)
    {
        var viewer = new ScrollViewer
        {
            Width = 200, Height = 100,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new Border { Height = 2000 },
        };
        Host(window).Children.Add(viewer);
        Pump();
        return viewer;
    }

    private static Border ThumbBorder(Thumb thumb) => (Border)thumb.Template.FindName("Bd", thumb);

    private static T? FindChild<T>(DependencyObject root, Func<T, bool> match) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T t && match(t)) return t;
            var found = FindChild(child, match);
            if (found is not null) return found;
        }
        return null;
    }

    private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ContextIdle, () => { });

    private static void Sta(Action<Window> test)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            Window? window = null;
            try
            {
                window = OffScreenWindow.Show(400, 600, new StackPanel()); // M.COMP-T28/T39
                Pump();
                test(window);
            }
            catch (Exception ex)
            {
                failure = ExceptionDispatchInfo.Capture(ex);
            }
            finally
            {
                window?.Close();
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}
