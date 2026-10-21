using System.Collections.ObjectModel;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MindAttic.Components.Wpf.Controls;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T30: MaToast, the notification card, and MaToastHost, the row that stacks and announces them.</summary>
public sealed class MaToastTests
{
    [Fact]
    public void A_toast_renders_a_themed_card_with_accent_stripe_title_body_and_dismiss_button() => Sta(h =>
    {
        var red = Brushes.Red;
        var toast = new MaToast { Title = "Build failed", Body = "MindAttic.Vault: 3 tests failed.", Accent = red };
        h.Root.Children.Add(toast);
        h.Pump();

        var card = (Border)toast.Template.FindName("Card", toast);
        var stripe = (Border)toast.Template.FindName("AccentStripe", toast);
        var open = (Button)toast.Template.FindName(MaToast.PartOpenButton, toast);
        var dismiss = (Button)toast.Template.FindName(MaToast.PartDismissButton, toast);

        Assert.Equal(h.Window.FindResource("MaSurface"), card.Background);
        Assert.Same(red, card.BorderBrush);
        Assert.Same(red, stripe.Background);
        Assert.Equal(4, stripe.ActualWidth);
        Assert.Equal(h.Window.FindResource("MaToolButton"), dismiss.Style);
        Assert.Equal("Build failed", ((TextBlock)toast.Template.FindName("TitleText", toast)).Text);
        var body = (TextBlock)toast.Template.FindName("BodyText", toast);
        Assert.Equal("MindAttic.Vault: 3 tests failed.", body.Text);
        Assert.Equal(32, body.MaxHeight); // two 16px lines
        // The title and body are both inside the one clickable button.
        Assert.True(open.IsAncestorOf(body));
    });

    [Fact]
    public void The_accent_defaults_to_McAccent() => Sta(h =>
    {
        var toast = new MaToast { Title = "Hi" };
        h.Root.Children.Add(toast);
        h.Pump();
        Assert.Equal(h.Window.FindResource("MaAccent"), toast.Accent);
    });

    [Fact]
    public void A_long_body_is_clipped_to_two_lines() => Sta(h =>
    {
        var toast = new MaToast { Title = "Long", Body = string.Join(" ", Enumerable.Repeat("words", 200)) };
        h.Root.Children.Add(toast);
        h.Pump();
        var body = (TextBlock)toast.Template.FindName("BodyText", toast);
        Assert.Equal(32, body.ActualHeight);
    });

    [Fact]
    public void Clicking_the_card_runs_Command_and_raises_Click_and_dismiss_runs_DismissCommand() => Sta(h =>
    {
        object? opened = null, dismissed = null;
        var clicks = 0;
        var toast = new MaToast
        {
            Title = "Needs you",
            Command = new Command(p => opened = p), CommandParameter = "repo",
            DismissCommand = new Command(p => dismissed = p), DismissCommandParameter = "gone",
        };
        toast.Click += (_, _) => clicks++;
        h.Root.Children.Add(toast);
        h.Pump();

        ClickButton((Button)toast.Template.FindName(MaToast.PartOpenButton, toast));
        Assert.Equal("repo", opened);
        Assert.Equal(1, clicks);

        ClickButton((Button)toast.Template.FindName(MaToast.PartDismissButton, toast));
        Assert.Equal("gone", dismissed);
        Assert.Equal(Visibility.Visible, toast.Visibility); // the command owns removal
    });

    [Fact]
    public void A_standalone_toast_with_no_dismiss_command_hides_itself() => Sta(h =>
    {
        var toast = new MaToast { Title = "Saved" };
        h.Root.Children.Add(toast);
        h.Pump();
        ClickButton((Button)toast.Template.FindName(MaToast.PartDismissButton, toast));
        Assert.Equal(Visibility.Collapsed, toast.Visibility);
    });

    [Fact]
    public void Accessible_names_title_help_text_body_and_dismiss_names_the_toast() => Sta(h =>
    {
        var toast = new MaToast { Title = "All quiet", Body = "Nothing is working in any CLI." };
        h.Root.Children.Add(toast);
        h.Pump();
        var open = (Button)toast.Template.FindName(MaToast.PartOpenButton, toast);
        var dismiss = (Button)toast.Template.FindName(MaToast.PartDismissButton, toast);
        Assert.Equal("All quiet", AutomationProperties.GetName(open));
        Assert.Equal("Nothing is working in any CLI.", AutomationProperties.GetHelpText(open));
        Assert.Equal("Dismiss: All quiet", AutomationProperties.GetName(dismiss));
        Assert.Equal("All quiet", UIElementAutomationPeer.CreatePeerForElement(toast).GetName());
        Assert.True(open.Focusable && dismiss.Focusable);
        Assert.False(toast.Focusable);
    });

    [Fact]
    public void A_host_builds_a_toast_per_view_model_item() => Sta(h =>
    {
        var items = new ObservableCollection<Note>
        {
            new("First", "one", Brushes.Orange),
            new("Second", "two", null),
        };
        var host = h.AddHost(items);

        var toasts = FindAll<MaToast>(host).ToList();
        Assert.Equal(2, toasts.Count);
        Assert.Equal("First", toasts[0].Title);
        Assert.Equal("one", toasts[0].Body);
        Assert.Same(Brushes.Orange, toasts[0].Accent);
        Assert.Equal(h.Window.FindResource("MaAccent"), toasts[1].Accent); // no accent: the theme's
        Assert.IsType<WrapPanel>(VisualTreeHelper.GetParent(toasts[0]));
    });

    [Fact]
    public void The_host_runs_its_commands_with_the_item() => Sta(h =>
    {
        var note = new Note("Needs you", "Waiting.", null);
        var items = new ObservableCollection<Note> { note };
        var host = h.AddHost(items);
        object? opened = null, dismissed = null;
        host.OpenCommand = new Command(p => opened = p);
        host.DismissCommand = new Command(p => { dismissed = p; items.Remove((Note)p!); });
        h.Pump();

        var toast = FindAll<MaToast>(host).Single();
        ClickButton((Button)toast.Template.FindName(MaToast.PartOpenButton, toast));
        Assert.Same(note, opened);
        ClickButton((Button)toast.Template.FindName(MaToast.PartDismissButton, toast));
        Assert.Same(note, dismissed);
        h.Pump();
        Assert.Empty(FindAll<MaToast>(host));
    });

    [Fact]
    public void With_no_dismiss_command_the_host_removes_the_item_itself() => Sta(h =>
    {
        var items = new ObservableCollection<Note> { new("A", "", null), new("B", "", null) };
        var host = h.AddHost(items);
        var first = FindAll<MaToast>(host).First();
        ClickButton((Button)first.Template.FindName(MaToast.PartDismissButton, first));
        h.Pump();
        Assert.Equal(["B"], items.Select(n => n.Title));
        Assert.Single(FindAll<MaToast>(host));
    });

    [Fact]
    public void Each_new_toast_is_announced_politely_but_the_initial_ones_are_not() => Sta(h =>
    {
        var items = new ObservableCollection<Note> { new("Old", "already here", null) };
        var host = h.AddHost(items);
        Assert.Equal(string.Empty, host.LastAnnouncement);

        var announcer = (TextBlock)host.Template.FindName(MaToastHost.PartAnnouncer, host);
        Assert.Equal(AutomationLiveSetting.Polite, AutomationProperties.GetLiveSetting(announcer));

        items.Insert(0, new Note("Pace changed", "Now at 92% of the window.", null));
        Assert.Equal("Pace changed. Now at 92% of the window.", host.LastAnnouncement);
        Assert.Equal(host.LastAnnouncement, announcer.Text);

        items.Insert(0, new Note("All quiet", "", null));
        Assert.Equal("All quiet", host.LastAnnouncement);
        Assert.False(h.IsForeground); // M.COMP-T28: never steals the user's keyboard
    });

    [Fact]
    public void The_host_is_a_named_group_and_unfocusable() => Sta(h =>
    {
        var host = h.AddHost(new ObservableCollection<Note>());
        Assert.False(host.Focusable);
        var peer = UIElementAutomationPeer.CreatePeerForElement(host);
        Assert.Equal("Notifications", peer.GetName());
        Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
    });

    [Fact]
    public void MaToast_elements_can_be_host_items_directly() => Sta(h =>
    {
        var host = new MaToastHost();
        h.Root.Children.Add(host);
        h.Pump();
        var toast = new MaToast { Title = "Direct", Body = "item" };
        host.Items.Add(toast);
        h.Pump();
        Assert.Equal("Direct. item", host.LastAnnouncement);
        ClickButton((Button)toast.Template.FindName(MaToast.PartDismissButton, toast));
        Assert.Empty(host.Items);
    });

    // ---- harness ----

    public sealed record Note(string Title, string Body, Brush? Accent);

    private static void ClickButton(ButtonBase button) =>
        button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));

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
        public StackPanel Root { get; } = new();

        public Host()
        {
            Window = OffScreenWindow.Show(800, 600, Root);
            Pump();
        }

        public MaToastHost AddHost(System.Collections.IEnumerable items)
        {
            var host = new MaToastHost { ItemsSource = items };
            Root.Children.Add(host);
            Pump();
            return host;
        }

        public bool IsForeground =>
            GetForegroundWindow() == new System.Windows.Interop.WindowInteropHelper(Window).Handle;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern nint GetForegroundWindow();

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

    private sealed class Command(Action<object?> execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute(parameter);
    }
}
