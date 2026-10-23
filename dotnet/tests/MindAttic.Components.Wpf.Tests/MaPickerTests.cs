using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MindAttic.Components.Wpf.Controls;
using MindAttic.Components.Wpf.Mvvm;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T52: MaPicker / MaPickerItem, the drop-down the Orchestrator's per-user Tickets view assigns a ticket
/// to a person or login with: a name, an optional status light and a muted trailing text per option and in the closed
/// box. No test opens the drop-down: its popup is its own top-level window, which would land on the user's screen.</summary>
public sealed class MaPickerTests
{
    public sealed class Person(string name, MaStatus? health, string left) : ObservableObject
    {
        private string _left = left;
        private MaStatus? _health = health;
        public string Name { get; } = name;
        public MaStatus? Health { get => _health; set => Set(ref _health, value); }
        public string Left { get => _left; set => Set(ref _left, value); }
        public string Words => Health == MaStatus.Bad ? "Signed out" : "Signed in";
        public override string ToString() => Name;
    }

    private static Person[] People() => [new("Ryan", MaStatus.Good, "23% left"), new("Ada", MaStatus.Bad, "week 97%"), new("Lin", null, "")];

    private static MaPicker Picker(Host h, Person[] people)
    {
        var picker = new MaPicker
        {
            ItemsSource = people, DisplayMemberPath = nameof(Person.Name), StatusMemberPath = nameof(Person.Health),
            TrailingMemberPath = nameof(Person.Left), Placeholder = "Unassigned", Width = 260,
        };
        AutomationProperties.SetName(picker, "Assigned to");
        h.Show(picker);
        return picker;
    }

    [Fact]
    public void The_closed_box_shows_the_chosen_persons_name_status_light_and_trailing_text() => Sta(h =>
    {
        var people = People();
        var picker = Picker(h, people);
        var light = Part<MaStatusLight>(picker, MaPicker.PartSelectedStatusLight);
        var trailing = Part<TextBlock>(picker, MaPicker.PartSelectedTrailing);
        var placeholder = Part<TextBlock>(picker, MaPicker.PartPlaceholder);

        Assert.Equal(Visibility.Visible, placeholder.Visibility);
        Assert.Equal("Unassigned", placeholder.Text);
        Assert.Same(h.Window.FindResource("MaMuted"), placeholder.Foreground);
        Assert.Equal(Visibility.Collapsed, light.Visibility);
        Assert.Equal(Visibility.Collapsed, trailing.Visibility);

        picker.SelectedItem = people[0];
        h.Pump();
        Assert.Equal(Visibility.Collapsed, placeholder.Visibility);
        Assert.Equal(MaStatus.Good, picker.SelectedStatus);
        Assert.Equal(Visibility.Visible, light.Visibility);
        Assert.Equal(MaStatus.Good, light.Status);
        Assert.Equal("23% left", trailing.Text);
        Assert.Equal(Visibility.Visible, trailing.Visibility);
        Assert.Same(h.Window.FindResource("MaMuted"), trailing.Foreground);
        Assert.Contains("Ryan", Texts(picker));

        picker.SelectedItem = people[2]; // no status, no trailing
        h.Pump();
        Assert.Null(picker.SelectedStatus);
        Assert.Equal(Visibility.Collapsed, light.Visibility);
        Assert.Equal(Visibility.Collapsed, trailing.Visibility);
    });

    [Fact]
    public void The_chosen_persons_changes_show_live_and_join_the_pickers_item_status() => Sta(h =>
    {
        var people = People();
        var picker = Picker(h, people);
        picker.StatusLabelMemberPath = nameof(Person.Words);
        picker.SelectedItem = people[0];
        h.Pump();

        var peer = UIElementAutomationPeer.CreatePeerForElement(picker);
        Assert.Equal(AutomationControlType.ComboBox, peer.GetAutomationControlType());
        Assert.Equal("Assigned to", peer.GetName());
        Assert.NotNull(peer.GetPattern(PatternInterface.ExpandCollapse));
        Assert.NotNull(peer.GetPattern(PatternInterface.Selection));
        Assert.Equal("Signed in, 23% left", peer.GetItemStatus());

        people[0].Left = "4% left";
        h.Pump();
        Assert.Equal("4% left", picker.SelectedTrailing);
        Assert.Equal("4% left", Part<TextBlock>(picker, MaPicker.PartSelectedTrailing).Text);

        picker.SelectedItem = null;
        h.Pump();
        Assert.Equal(DependencyProperty.UnsetValue, picker.ReadLocalValue(AutomationProperties.ItemStatusProperty));

        AutomationProperties.SetItemStatus(picker, "host's own");
        picker.SelectedItem = people[1];
        Assert.Equal("host's own", AutomationProperties.GetItemStatus(picker));
    });

    [Fact]
    public void Up_Down_Home_and_End_change_the_choice_while_it_stays_closed() => Sta(h =>
    {
        var people = People();
        var picker = Picker(h, people);
        picker.SelectedIndex = 0;
        Assert.True(picker.Focus());
        h.Pump();
        Assert.True(picker.IsKeyboardFocusWithin);
        Assert.True(picker.IsTabStop);
        Assert.Equal(new Thickness(2), Part<Control>(picker, "Toggle").BorderThickness);

        Press(h, picker, Key.Down);
        Assert.Same(people[1], picker.SelectedItem);
        Assert.Equal(MaStatus.Bad, picker.SelectedStatus);
        Press(h, picker, Key.End);
        Assert.Same(people[2], picker.SelectedItem);
        Press(h, picker, Key.Home);
        Assert.Same(people[0], picker.SelectedItem);
        Press(h, picker, Key.Up);
        Assert.Same(people[0], picker.SelectedItem);
        Assert.False(picker.IsDropDownOpen);
    });

    [Fact]
    public void A_data_options_container_takes_its_status_trailing_and_name_from_the_member_paths() => Sta(h =>
    {
        var people = People();
        var picker = new TestPicker
        {
            ItemsSource = people, DisplayMemberPath = nameof(Person.Name), StatusMemberPath = nameof(Person.Health),
            TrailingMemberPath = nameof(Person.Left),
        };
        h.Show(picker);
        var item = Assert.IsType<MaPickerItem>(picker.Container());
        item.DataContext = people[1];
        picker.Prepare(item, people[1]);
        h.Show(item);

        Assert.Equal(MaStatus.Bad, item.Status);
        Assert.Equal("week 97%", item.Trailing);
        Assert.Equal("Ada", AutomationProperties.GetName(item));
        Assert.Equal("Bad, week 97%", AutomationProperties.GetItemStatus(item));
        Assert.Equal(Visibility.Visible, Part<MaStatusLight>(item, MaPickerItem.PartStatusLight).Visibility);
        Assert.Same(h.Window.FindResource("MaBad"), Part<MaStatusLight>(item, MaPickerItem.PartStatusLight).Foreground);
        Assert.Equal("week 97%", Part<TextBlock>(item, MaPickerItem.PartTrailing).Text);

        people[1].Health = MaStatus.Good;
        Assert.Equal(MaStatus.Good, item.Status);
        Assert.False(picker.IsOwnContainer(people[0]));
        Assert.True(picker.IsOwnContainer(new MaPickerItem()));
    });

    [Fact]
    public void An_option_marks_highlight_focus_and_choice_without_colour_alone() => Sta(h =>
    {
        var item = new MaPickerItem { Content = "Ryan" };
        h.Show(item);
        var bar = Part<Border>(item, MaPickerItem.PartSelectedBar);
        Assert.Same(h.Window.FindResource("MaText"), item.Foreground);
        Assert.Equal(Visibility.Hidden, bar.Visibility);
        Assert.Equal(Visibility.Collapsed, Part<MaStatusLight>(item, MaPickerItem.PartStatusLight).Visibility);
        Assert.Equal(Visibility.Collapsed, Part<TextBlock>(item, MaPickerItem.PartTrailing).Visibility);

        item.IsSelected = true;
        h.Pump();
        Assert.Equal(Visibility.Visible, bar.Visibility);
        Assert.Same(h.Window.FindResource("MaAccent"), bar.Background);
        Assert.Equal(FontWeights.SemiBold, item.FontWeight);

        Assert.True(item.Focus());
        h.Pump();
        Assert.Same(h.Window.FindResource("MaFocus"), Part<Border>(item, "Bd").BorderBrush);
    });

    [Fact]
    public void Direct_MaPickerItems_carry_their_own_status_and_trailing_into_the_closed_box() => Sta(h =>
    {
        var ryan = new MaPickerItem { Content = "Ryan", Status = MaStatus.Warning, StatusLabel = "Low", Trailing = "8% left" };
        var picker = new MaPicker { Items = { ryan, new MaPickerItem { Content = "Ada" } } };
        h.Show(picker);
        picker.SelectedIndex = 0;
        h.Pump();

        Assert.Equal(MaStatus.Warning, picker.SelectedStatus);
        Assert.Equal("Low", picker.SelectedStatusLabel);
        Assert.Equal("8% left", picker.SelectedTrailing);
        Assert.Equal("Low", Part<MaStatusLight>(picker, MaPicker.PartSelectedStatusLight).Description);
        Assert.Contains("Ryan", Texts(picker));

        ryan.Trailing = "2% left";
        Assert.Equal("2% left", picker.SelectedTrailing);
        picker.SelectedIndex = 1;
        h.Pump();
        Assert.Null(picker.SelectedStatus);
        Assert.Null(picker.SelectedTrailing);
    });

    [Fact]
    public void A_status_word_from_a_view_model_counts_as_a_status()
    {
        Assert.Equal(MaStatus.Warning, MaPicker.ToStatus("warning"));
        Assert.Equal(MaStatus.Other, MaPicker.ToStatus(MaStatus.Other));
        Assert.Null(MaPicker.ToStatus("sideways"));
        Assert.Null(MaPicker.ToStatus(null));
    }

    [Fact]
    public void The_picker_is_themed_and_follows_high_contrast_live() => Sta(h =>
    {
        var picker = Picker(h, People());
        var toggle = Part<Control>(picker, "Toggle");
        Assert.Same(h.Window.FindResource("MaSurface"), toggle.Background);
        Assert.Same(h.Window.FindResource("MaFieldBorder"), toggle.BorderBrush);
        Assert.Same(h.Window.FindResource("MaText"), picker.Foreground);
        Assert.True(picker.ActualHeight >= 32);

        MaHighContrast.Apply(h.Window.Resources, on: true);
        h.Pump();
        Assert.Equal(SystemColors.WindowBrush.Color, ((SolidColorBrush)toggle.Background).Color);
        Assert.Equal(SystemColors.WindowTextBrush.Color, ((SolidColorBrush)picker.Foreground).Color);
        MaHighContrast.Apply(h.Window.Resources, on: false);
    });

    // ---- harness ----

    private sealed class TestPicker : MaPicker
    {
        public DependencyObject Container() => GetContainerForItemOverride();
        public void Prepare(DependencyObject container, object item) => PrepareContainerForItemOverride(container, item);
        public bool IsOwnContainer(object item) => IsItemItsOwnContainerOverride(item);
    }

    private static T Part<T>(Control owner, string name) where T : class => (T)owner.Template.FindName(name, owner);

    private static List<string> Texts(DependencyObject root)
    {
        var texts = new List<string>();
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is TextBlock { Text: { Length: > 0 } text }) texts.Add(text);
            texts.AddRange(Texts(child));
        }
        return texts;
    }

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
            Window = OffScreenWindow.Show(500, 300, _root);
            Pump();
        }

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
