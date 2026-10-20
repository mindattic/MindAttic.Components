using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using MindAttic.Components.Wpf.Controls;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T38: McMessageBox, the themed stand-in for the system MessageBox. Every box is shown non-modally
/// off-screen through <see cref="OffScreenWindow.ShowMessageBox"/> and its buttons are clicked in code, so no test
/// blocks on ShowDialog or puts a dialog on the user's screen.</summary>
public sealed class McMessageBoxTests
{
    [Theory]
    [InlineData(MessageBoxButton.OK, new[] { MessageBoxResult.OK })]
    [InlineData(MessageBoxButton.OKCancel, new[] { MessageBoxResult.OK, MessageBoxResult.Cancel })]
    [InlineData(MessageBoxButton.YesNo, new[] { MessageBoxResult.Yes, MessageBoxResult.No })]
    [InlineData(MessageBoxButton.YesNoCancel, new[] { MessageBoxResult.Yes, MessageBoxResult.No, MessageBoxResult.Cancel })]
    public void Shows_the_buttons_asked_for_in_order(MessageBoxButton buttons, MessageBoxResult[] expected) => Sta(() =>
    {
        var box = OffScreenWindow.ShowMessageBox("Install?", "MindAttic Orchestrator", buttons);
        Assert.Equal(expected, box.Buttons.Select(b => (MessageBoxResult)b.Tag).ToArray());
        box.Close();
    });

    [Theory]
    [InlineData(MessageBoxButton.OK, MessageBoxResult.OK)]
    [InlineData(MessageBoxButton.YesNo, MessageBoxResult.Yes)]
    [InlineData(MessageBoxButton.YesNo, MessageBoxResult.No)]
    [InlineData(MessageBoxButton.YesNoCancel, MessageBoxResult.Yes)]
    [InlineData(MessageBoxButton.YesNoCancel, MessageBoxResult.No)]
    [InlineData(MessageBoxButton.YesNoCancel, MessageBoxResult.Cancel)]
    public void Clicking_a_button_returns_its_result_and_closes_the_box(MessageBoxButton buttons, MessageBoxResult click) => Sta(() =>
    {
        var box = OffScreenWindow.ShowMessageBox("Build first?", "MindAttic Orchestrator", buttons, MessageBoxImage.Question);
        var closed = false;
        box.Closed += (_, _) => closed = true;
        var button = box.Buttons.Single(b => (MessageBoxResult)b.Tag == click);
        button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));
        Pump();
        Assert.True(closed);
        Assert.Equal(click, box.Result);
    });

    [Theory]
    [InlineData(MessageBoxButton.OK, MessageBoxResult.OK)]
    [InlineData(MessageBoxButton.OKCancel, MessageBoxResult.Cancel)]
    [InlineData(MessageBoxButton.YesNo, MessageBoxResult.No)]
    [InlineData(MessageBoxButton.YesNoCancel, MessageBoxResult.Cancel)]
    public void Escape_and_the_close_button_give_the_safe_answer(MessageBoxButton buttons, MessageBoxResult expected) => Sta(() =>
    {
        var box = OffScreenWindow.ShowMessageBox("Proceed?", "Test", buttons);
        var source = PresentationSource.FromVisual(box)!;
        box.Buttons[0].RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Escape) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        Pump();
        Assert.False(box.IsVisible);
        Assert.Equal(expected, box.Result);

        var closedByChrome = OffScreenWindow.ShowMessageBox("Proceed?", "Test", buttons);
        closedByChrome.Close(); // what the title bar's X does
        Assert.Equal(expected, closedByChrome.Result);
    });

    [Fact]
    public void The_first_button_is_the_default_unless_another_is_named() => Sta(() =>
    {
        var box = OffScreenWindow.ShowMessageBox("Build first?", "Test", MessageBoxButton.YesNoCancel);
        Pump();
        Assert.Equal(MessageBoxResult.Yes, box.DefaultResult);
        Assert.True(box.Buttons[0].IsDefault);
        Assert.Same(box.Buttons[0], FocusManager.GetFocusedElement(box));
        Assert.Equal(box.FindResource("McPrimaryButton"), box.Buttons[0].Style);
        Assert.Equal(box.FindResource("McButton"), box.Buttons[1].Style);
        Assert.Equal(box.FindResource("McButton"), box.Buttons[2].Style);
        box.Close();

        var no = OffScreenWindow.ShowMessageBox("Delete?", "Test", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        Assert.True(no.Buttons[1].IsDefault);
        Assert.False(no.Buttons[0].IsDefault);
        Assert.Equal(no.FindResource("McPrimaryButton"), no.Buttons[1].Style);
        no.Close();
    });

    [Fact]
    public void Is_themed_with_a_dark_title_bar_the_message_and_the_caption() => Sta(() =>
    {
        var box = OffScreenWindow.ShowMessageBox("Couldn't install.", "MindAttic Orchestrator", MessageBoxButton.OK, MessageBoxImage.Warning);
        Pump();
        Assert.True(McWindow.GetDarkTitleBar(box));
        Assert.Equal(box.FindResource("McChrome"), box.Background);
        Assert.Equal(box.FindResource("McText"), box.Foreground);
        Assert.Equal("MindAttic Orchestrator", box.Title);
        Assert.Contains(FindAll<TextBlock>(box), t => t.Text == "Couldn't install.");
        Assert.Equal(ResizeMode.NoResize, box.ResizeMode);
        box.Close();
    });

    [Theory]
    [InlineData(MessageBoxImage.Information, "Information", "McFocus")]
    [InlineData(MessageBoxImage.Warning, "Warning", "McWarn")]
    [InlineData(MessageBoxImage.Error, "Error", "McBad")]
    [InlineData(MessageBoxImage.Question, "Question", "McFocus")]
    public void The_icon_shows_and_reads_as_its_severity(MessageBoxImage image, string name, string brush) => Sta(() =>
    {
        var box = OffScreenWindow.ShowMessageBox("Message", "Test", MessageBoxButton.OK, image);
        Pump();
        var icon = Assert.IsType<TextBlock>(box.IconElement);
        Assert.Equal(name, AutomationProperties.GetName(icon));
        Assert.Equal(box.FindResource(brush), icon.Foreground);
        Assert.True(icon.IsVisible);
        box.Close();
    });

    [Fact]
    public void No_image_means_no_icon() => Sta(() =>
    {
        var box = OffScreenWindow.ShowMessageBox("Message", "Test");
        Assert.Null(box.IconElement);
        box.Close();
    });

    [Fact]
    public void Is_named_by_its_caption_with_the_message_as_help_text_and_named_buttons() => Sta(() =>
    {
        var box = OffScreenWindow.ShowMessageBox("Setup couldn't finish.", "MindAttic Orchestrator", MessageBoxButton.YesNoCancel);
        Pump();
        var peer = UIElementAutomationPeer.CreatePeerForElement(box);
        Assert.Equal("MindAttic Orchestrator", peer.GetName());
        Assert.Equal("Setup couldn't finish.", peer.GetHelpText());
        Assert.Equal(new[] { "Yes", "No", "Cancel" }, box.Buttons.Select(AutomationProperties.GetName).ToArray());
        box.Close();
    });

    [Fact]
    public void Showing_it_never_takes_the_users_keyboard() => Sta(() =>
    {
        var box = OffScreenWindow.ShowMessageBox("Message", "Test", MessageBoxButton.YesNo);
        Pump();
        Assert.NotEqual(GetForegroundWindow(), new System.Windows.Interop.WindowInteropHelper(box).Handle);
        Assert.False(box.IsActive);
        box.Close();
    });

    [Fact]
    public void The_package_is_version_16() =>
        Assert.Equal(16, typeof(McMessageBox).Assembly.GetName().Version!.Major);

    // ---- harness ----

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ContextIdle, () => { });

    private static IEnumerable<T> FindAll<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            if (child is T t) yield return t;
            foreach (var nested in FindAll<T>(child)) yield return nested;
        }
    }

    private static void Sta(Action test)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try { test(); }
            catch (Exception ex) { failure = ExceptionDispatchInfo.Capture(ex); }
            finally { Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}
