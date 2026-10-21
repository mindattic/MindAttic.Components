using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>One item in a <see cref="DropdownMenu"/>: an icon-font glyph, a label, a tooltip and
/// the handler to run when it's picked.</summary>
public sealed record DropdownAction(string Id, string Label, string Icon, string ToolTip, RoutedEventHandler Execute);

/// <summary>
/// A themed, pinnable dropdown driven by a trigger <see cref="ButtonBase"/> (usually a cog icon).
/// Every action can be pinned to its own button in a host <see cref="Panel"/>; picking an action
/// from the dropdown closes it first, so it's never on screen for a screenshot or the start of a
/// recording. The trigger itself toggles the dropdown open/closed rather than closing and
/// immediately reopening it on a second click.
/// </summary>
public sealed class DropdownMenu
{
    private readonly ButtonBase _trigger;
    private readonly Func<IReadOnlyList<DropdownAction>> _actions;
    private readonly Func<string, bool> _isPinned;
    private readonly Action<string> _togglePinned;
    private readonly Panel? _pinnedHost;

    private ContextMenu? _menu;
    private Window? _window;

    public DropdownMenu(ButtonBase trigger, Func<IReadOnlyList<DropdownAction>> actions,
        Func<string, bool> isPinned, Action<string> togglePinned, Panel? pinnedHost = null)
    {
        _trigger = trigger;
        _actions = actions;
        _isPinned = isPinned;
        _togglePinned = togglePinned;
        _pinnedHost = pinnedHost;
        _trigger.Click += (_, _) => Open();
    }

    /// <summary>Rebuilds the pinned-host's buttons for whichever actions are currently pinned, in
    /// the fixed order <see cref="_actions"/> returns them.</summary>
    public void RefreshPinned()
    {
        if (_pinnedHost is null) return;
        _pinnedHost.Children.Clear();
        foreach (var action in _actions())
        {
            if (!_isPinned(action.Id)) continue;
            var button = new Button
            {
                Style = (Style)_trigger.FindResource("MaToolButton"),
                Content = action.Icon,
                ToolTip = action.ToolTip,
                Tag = action.Id,
            };
            System.Windows.Automation.AutomationProperties.SetName(button, action.Label);
            button.Click += action.Execute;
            _pinnedHost.Children.Add(button);
        }
    }

    /// <summary>The pinned button for <paramref name="actionId"/>, if it's currently pinned and
    /// rendered — for callers that need to keep one button's live state in sync (an icon or color
    /// that changes while the dropdown is closed, e.g. a record button while recording).</summary>
    public Button? PinnedButton(string actionId) =>
        _pinnedHost?.Children.OfType<Button>().FirstOrDefault(b => Equals(b.Tag, actionId));

    private void Open()
    {
        if (_menu is { IsOpen: true })
        {
            Close();
            return;
        }

        var menu = new ContextMenu
        {
            Style = (Style)_trigger.FindResource("MaDropdownMenu"),
            PlacementTarget = _trigger,
            Placement = PlacementMode.Bottom,
            StaysOpen = true,
        };
        foreach (var action in _actions())
        {
            var icon = new TextBlock
            {
                Text = action.Icon,
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                Width = 20,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var label = new TextBlock { Text = action.Label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 16, 0) };

            var header = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(icon, 0);
            Grid.SetColumn(label, 1);
            header.Children.Add(icon);
            header.Children.Add(label);

            if (_pinnedHost is not null)
            {
                var pin = new ToggleButton { Style = (Style)_trigger.FindResource("MaPinToggle") };
                var id = action.Id;
                ShowPinState(pin, _isPinned(id));
                // Toggled on mouse-down and marked handled: that keeps the press from also reaching the
                // menu item (which would run the action). It also means ToggleButton never raises its own
                // Click, so the toggle can't live in a Click handler.
                pin.PreviewMouseLeftButtonDown += (_, args) =>
                {
                    _togglePinned(id);
                    ShowPinState(pin, _isPinned(id));
                    args.Handled = true;
                    RefreshPinned();
                };
                header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                Grid.SetColumn(pin, 2);
                header.Children.Add(pin);
            }

            var item = new MenuItem
            {
                Style = (Style)_trigger.FindResource("MaDropdownMenuItem"),
                Header = header,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                StaysOpenOnClick = true,
            };
            var execute = action.Execute;
            item.Click += (_, args) =>
            {
                // Close first and let the dropdown actually disappear before the action runs (otherwise it can
                // still be on screen for a screenshot or the start of a recording).
                Close();
                _trigger.Dispatcher.BeginInvoke(() => execute(_trigger, args), DispatcherPriority.Background);
            };
            menu.Items.Add(item);
        }

        // The ContextMenu can also close itself (Esc). Forget it then too, or the next click on the
        // trigger is taken for "close the open menu" and swallowed.
        menu.Closed += (_, _) => { if (ReferenceEquals(_menu, menu)) Close(); };
        _menu = menu;
        _window = Window.GetWindow(_trigger);
        if (_window is not null)
        {
            _window.PreviewMouseDown += OnWindowPreviewMouseDown;
            _window.Deactivated += OnWindowDeactivated;
        }
        menu.IsOpen = true;
    }

    private void ShowPinState(ToggleButton pin, bool pinned)
    {
        pin.IsChecked = pinned;
        pin.Content = pinned ? "" : "";
        pin.Foreground = (Brush)_trigger.FindResource(pinned ? "MaAccent" : "MaMuted");
        pin.ToolTip = pinned ? "Unpin from toolbar" : "Pin to toolbar";
    }

    private void Close()
    {
        if (_menu is not { } menu) return;
        _menu = null;
        if (_window is not null)
        {
            _window.PreviewMouseDown -= OnWindowPreviewMouseDown;
            _window.Deactivated -= OnWindowDeactivated;
        }
        _window = null;
        menu.IsOpen = false;
    }

    private void OnWindowDeactivated(object? sender, EventArgs e) => Close();

    /// <summary>Disabling the ContextMenu's own light-dismiss (<c>StaysOpen = true</c> above) and
    /// doing it here ourselves means there's exactly one deterministic path for "click elsewhere
    /// closes it, click the trigger again toggles it closed" — no racing against WPF's internal
    /// popup-dismiss timing, which doesn't reliably beat (or reliably lose to) the trigger's own
    /// Click event on the same mouse-down.</summary>
    private void OnWindowPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_menu is not { } menu) return;
        var source = e.OriginalSource as DependencyObject;
        if (source is not null && IsWithin(menu, source)) return; // let the menu handle its own clicks
        var onTrigger = source is not null && IsWithin(_trigger, source);
        Close();
        if (onTrigger) e.Handled = true; // swallow so the trigger's own Click doesn't fire and reopen it
    }

    private static bool IsWithin(DependencyObject root, DependencyObject node)
    {
        for (var d = node; d is not null; d = GetParent(d))
            if (ReferenceEquals(d, root)) return true;
        return false;
    }

    private static DependencyObject? GetParent(DependencyObject d) =>
        d is Visual or Visual3D ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);
}
