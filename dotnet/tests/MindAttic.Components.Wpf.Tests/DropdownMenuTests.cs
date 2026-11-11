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

    [Fact]
    public void A_dropdown_row_with_items_is_a_branch_whose_items_open_beside_it() => Sta(() =>
    {
        // M.COMP-T189: nested MaDropdownMenuItems make a tree menu; a leaf keeps no chevron and no popup.
        var target = new Button { Content = "Projects" };
        var window = OffScreenWindow.Create(400, 300, new Grid { Children = { target } });
        try
        {
            OffScreenWindow.Show(window);
            Pump();
            var menu = new ContextMenu { PlacementTarget = target, Style = (Style)window.FindResource("MaDropdownMenu") };
            var leafStyle = (Style)window.FindResource("MaDropdownMenuItem");
            var branch = new MenuItem { Header = "Unity", Style = leafStyle };
            var child = new MenuItem { Header = "GridGame", Style = leafStyle };
            branch.Items.Add(child);
            var leaf = new MenuItem { Header = "Prose", Style = leafStyle };
            menu.Items.Add(branch);
            menu.Items.Add(leaf);
            menu.IsOpen = true;
            Pump();

            Assert.Equal(MenuItemRole.SubmenuHeader, branch.Role);
            Assert.Equal(Visibility.Visible, ((FrameworkElement)branch.Template.FindName("Chevron", branch)).Visibility);
            Assert.Equal(Visibility.Collapsed, ((FrameworkElement)leaf.Template.FindName("Chevron", leaf)).Visibility);

            var popup = (Popup)branch.Template.FindName("PART_Popup", branch);
            branch.IsSubmenuOpen = true;
            Pump();
            Assert.True(popup.IsOpen);
            Assert.True(child.IsVisible, "the branch's own item shows in its popup");
            menu.IsOpen = false;
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
