using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MindAttic.Components.Wpf.Controls;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T15: McModal, the in-window modal dialog.</summary>
public sealed class McModalTests
{
    [Fact]
    public void Starts_closed_and_IsOpen_shows_and_hides_it() => Sta(h =>
    {
        Assert.Equal(Visibility.Collapsed, h.Modal.Visibility);
        h.Modal.IsOpen = true;
        h.Pump();
        Assert.Equal(Visibility.Visible, h.Modal.Visibility);
        h.Modal.IsOpen = false;
        h.Pump();
        Assert.Equal(Visibility.Collapsed, h.Modal.Visibility);
    });

    [Fact]
    public void Renders_a_themed_card_with_backdrop_shadow_heading_close_button_and_footer() => Sta(h =>
    {
        h.Modal.IsOpen = true;
        h.Pump();
        var backdrop = (Border)h.Modal.Template.FindName(McModal.PartBackdrop, h.Modal);
        var card = (Border)h.Modal.Template.FindName(McModal.PartCard, h.Modal);
        var close = (Button)h.Modal.Template.FindName(McModal.PartCloseButton, h.Modal);
        var title = (TextBlock)h.Modal.Template.FindName("TitleText", h.Modal);
        var footer = (Border)h.Modal.Template.FindName("FooterArea", h.Modal);

        Assert.NotNull(backdrop.Background);
        Assert.Equal(h.Window.FindResource("McChrome"), card.Background);
        Assert.Equal(h.Window.FindResource("McChromeBorder"), card.BorderBrush);
        Assert.Contains(FindAll<Border>(h.Modal), b => b.Effect is System.Windows.Media.Effects.DropShadowEffect);
        Assert.Equal("Settings", title.Text);
        Assert.Equal(AutomationHeadingLevel.Level2, AutomationProperties.GetHeadingLevel(title));
        Assert.Equal("Close", AutomationProperties.GetName(close));
        Assert.Equal(Visibility.Visible, footer.Visibility);

        h.Modal.Footer = null;
        h.Pump();
        Assert.Equal(Visibility.Collapsed, footer.Visibility);
    });

    [Fact]
    public void Opening_focuses_inside_and_closing_returns_focus_to_where_it_was() => Sta(h =>
    {
        Assert.True(h.Outside.Focus());
        h.Modal.IsOpen = true;
        h.Pump();
        Assert.True(h.Modal.IsKeyboardFocusWithin);
        Assert.Same(h.Inside, Keyboard.FocusedElement);

        h.Modal.IsOpen = false;
        h.Pump();
        Assert.Same(h.Outside, Keyboard.FocusedElement);
    });

    [Fact]
    public void Focus_is_trapped_inside_while_open() => Sta(h =>
    {
        h.Outside.Focus();
        h.Modal.IsOpen = true;
        h.Pump();

        // Focus can't be moved to an element behind the dialog...
        h.Outside.Focus();
        Assert.True(h.Modal.IsKeyboardFocusWithin);

        // ...and Tab cycles: past the last stop (the close button) it wraps to the first.
        var close = (Button)h.Modal.Template.FindName(McModal.PartCloseButton, h.Modal);
        close.Focus();
        close.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        Assert.Same(h.Inside, Keyboard.FocusedElement);
    });

    [Fact]
    public void Escape_closes_it() => Sta(h =>
    {
        h.Modal.IsOpen = true;
        h.Pump();
        h.PressKey(Key.Escape);
        h.Pump();
        Assert.False(h.Modal.IsOpen);
        Assert.Equal(Visibility.Collapsed, h.Modal.Visibility);
    });

    [Fact]
    public void A_backdrop_click_closes_it_but_a_click_on_the_card_does_not() => Sta(h =>
    {
        h.Modal.IsOpen = true;
        h.Pump();
        var card = (Border)h.Modal.Template.FindName(McModal.PartCard, h.Modal);
        Click(card);
        Assert.True(h.Modal.IsOpen);

        Click((UIElement)h.Modal.Template.FindName(McModal.PartBackdrop, h.Modal));
        Assert.False(h.Modal.IsOpen);
    });

    [Fact]
    public void The_close_button_closes_it() => Sta(h =>
    {
        h.Modal.IsOpen = true;
        h.Pump();
        var close = (Button)h.Modal.Template.FindName(McModal.PartCloseButton, h.Modal);
        close.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, close));
        Assert.False(h.Modal.IsOpen);
    });

    [Fact]
    public void IsOpen_binds_two_way_to_a_view_model() => Sta(h =>
    {
        var vm = new ViewModel();
        h.Modal.DataContext = vm;
        h.Modal.SetBinding(McModal.IsOpenProperty, new Binding(nameof(ViewModel.IsSettingsOpen)));
        vm.IsSettingsOpen = true;
        h.Pump();
        Assert.Equal(Visibility.Visible, h.Modal.Visibility);

        h.PressKey(Key.Escape);
        h.Pump();
        Assert.False(vm.IsSettingsOpen);
    });

    [Fact]
    public void A_CloseCommand_runs_instead_of_clearing_IsOpen() => Sta(h =>
    {
        var vm = new ViewModel();
        h.Modal.DataContext = vm;
        h.Modal.SetBinding(McModal.IsOpenProperty, new Binding(nameof(ViewModel.IsSettingsOpen)) { Mode = BindingMode.OneWay });
        var ran = 0;
        h.Modal.CloseCommand = new Command(() => { ran++; vm.IsSettingsOpen = false; });
        vm.IsSettingsOpen = true;
        h.Pump();

        h.PressKey(Key.Escape);
        h.Pump();
        Assert.Equal(1, ran);
        Assert.False(h.Modal.IsOpen);
        Assert.NotNull(BindingOperations.GetBindingExpression(h.Modal, McModal.IsOpenProperty)); // binding kept
    });

    [Fact]
    public void Has_an_accessible_name_and_is_a_dialog() => Sta(h =>
    {
        h.Modal.IsOpen = true;
        h.Pump();
        var peer = UIElementAutomationPeer.CreatePeerForElement(h.Modal);
        Assert.Equal("Settings", peer.GetName());
        Assert.Equal(AutomationControlType.Window, peer.GetAutomationControlType());
        Assert.True(peer.IsDialog());

        AutomationProperties.SetName(h.Modal, "Dashboard settings");
        Assert.Equal("Dashboard settings", peer.GetName());
    });

    /// <summary>M.COMP-T16: a Hyperlink is a focusable ContentElement, not a Visual; focus on one
    /// (behind the dialog when it opens, or inside it while open) must not throw.</summary>
    [Fact]
    public void Focus_on_a_ContentElement_such_as_a_Hyperlink_works_inside_and_outside() => Sta(h =>
    {
        var outsideLink = new System.Windows.Documents.Hyperlink(new System.Windows.Documents.Run("Help")) { Focusable = true };
        ((Grid)h.Window.Content).Children.Insert(0, new TextBlock(outsideLink));
        var insideLink = new System.Windows.Documents.Hyperlink(new System.Windows.Documents.Run("Docs")) { Focusable = true };
        ((StackPanel)h.Modal.Content).Children.Add(new TextBlock(insideLink));
        h.Pump();

        Assert.True(outsideLink.Focus());
        h.Modal.IsOpen = true;
        h.Pump();
        Assert.True(h.Modal.IsKeyboardFocusWithin);

        Assert.True(insideLink.Focus());
        Assert.Same(insideLink, Keyboard.FocusedElement);
        outsideLink.Focus();
        Assert.Same(insideLink, Keyboard.FocusedElement); // still trapped

        h.Modal.IsOpen = false;
        h.Pump();
        Assert.Same(outsideLink, Keyboard.FocusedElement);
    });

    [Fact]
    public void The_package_is_version_7() =>
        Assert.Equal(7, typeof(McModal).Assembly.GetName().Version!.Major);

    // ---- harness ----

    private static void Click(UIElement element) =>
        element.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonDownEvent,
            Source = element,
        });

    private static IEnumerable<T> FindAll<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T t) yield return t;
            foreach (var nested in FindAll<T>(child)) yield return nested;
        }
    }

    private sealed class Host
    {
        public Window Window { get; }
        public McModal Modal { get; }
        public Button Outside { get; } = new() { Content = "Open settings" };
        public TextBox Inside { get; } = new();

        public Host()
        {
            _ = System.IO.Packaging.PackUriHelper.UriSchemePack; // registers pack:// outside an Application
            Modal = new McModal
            {
                Title = "Settings",
                Content = new StackPanel { Children = { Inside } },
                Footer = new Button { Content = "Done" },
            };
            var root = new Grid();
            root.Children.Add(Outside);
            root.Children.Add(Modal);
            Window = new Window
            {
                Width = 800, Height = 600, Left = 0, Top = 0, ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Content = root,
            };
            Window.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/MindAttic.Components.Wpf;component/Theme.xaml"),
            });
            Window.Show();
            Window.Activate();
            Pump();
        }

        public void Pump() => Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ContextIdle, () => { });

        public void PressKey(Key key)
        {
            var target = Keyboard.FocusedElement as UIElement ?? Modal;
            var source = PresentationSource.FromVisual(Window)!;
            target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        }
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

    private sealed class ViewModel : INotifyPropertyChanged
    {
        private bool _isSettingsOpen;

        public bool IsSettingsOpen
        {
            get => _isSettingsOpen;
            set { _isSettingsOpen = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private sealed class Command(Action execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute();
    }
}
