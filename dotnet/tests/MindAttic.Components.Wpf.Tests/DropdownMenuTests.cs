using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using MindAttic.Components.Wpf.Controls;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T17: DropdownMenu after its popup closes on its own (Esc).</summary>
public sealed class DropdownMenuTests
{
    [Fact]
    public void After_the_menu_closes_on_its_own_the_next_trigger_click_reopens_it() => Sta(() =>
    {
        var trigger = new Button { Content = "Menu" };
        // M.COMP-T39: off-screen and never activated, like every test window.
        var window = OffScreenWindow.Create(400, 300, new Grid { Children = { trigger } });
        try
        {
            OffScreenWindow.Show(window);
            Pump();
            var dropdown = new DropdownMenu(trigger,
                () => [new DropdownAction("a", "Action", "", "Action", (_, _) => { })],
                _ => false, _ => { });

            trigger.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, trigger));
            Pump();
            var menu = Menu(dropdown)!;
            Assert.True(menu.IsOpen);

            // The ContextMenu closes itself (what Esc does), not through DropdownMenu. Its popup
            // finishes closing on a timer, so wait for Closed.
            var closed = 0; menu.Closed += (_, _) => closed++;
            menu.IsOpen = false;
            var until = DateTime.UtcNow.AddSeconds(3);
            while (closed == 0 && DateTime.UtcNow < until) { Pump(); Thread.Sleep(20); }
            Assert.Equal(1, closed);
            Assert.Null(Menu(dropdown));

            // A real click on the trigger must not be swallowed as a "close the open menu" click.
            var down = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
            {
                RoutedEvent = UIElement.PreviewMouseDownEvent,
                Source = trigger,
            };
            trigger.RaiseEvent(down);
            Assert.False(down.Handled);
        }
        finally
        {
            window.Close();
        }
    });

    private static ContextMenu? Menu(DropdownMenu dropdown) =>
        (ContextMenu?)typeof(DropdownMenu).GetField("_menu", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(dropdown);

    private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.SystemIdle, () => { });

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
