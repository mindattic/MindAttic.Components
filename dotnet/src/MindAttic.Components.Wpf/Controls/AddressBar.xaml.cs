using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>The action an <see cref="AddressBar"/> is about to take, passed to
/// <see cref="AddressBar.NavigationGuard"/> so a host can veto any of them uniformly.</summary>
public enum AddressBarAction { Back, Forward, Reload, Navigate, Home }

/// <summary>Raised by <see cref="AddressBar.NavigationBlocked"/> when <see cref="AddressBar.NavigationGuard"/>
/// vetoes an action.</summary>
public sealed class AddressBarActionBlockedEventArgs(AddressBarAction action, string reason) : EventArgs
{
    public AddressBarAction Action { get; } = action;
    public string Reason { get; } = reason;
}

/// <summary>Raised by <see cref="AddressBar.HomeRequested"/> before the control's own
/// <see cref="AddressBar.HomeUrl"/> fallback runs. A host sets <see cref="Handled"/> to take over
/// home-navigation itself (e.g. per-target logic that isn't a single static URL).</summary>
public sealed class HomeRequestedEventArgs : EventArgs
{
    public bool Handled { get; set; }
}

/// <summary>
/// A themed WPF address bar for a WebView2 pane: Back/Forward/Reload(/Home) buttons plus a URL
/// textbox. <see cref="Attach"/> binds it to a live <see cref="CoreWebView2"/> and can be called
/// again to rebind it to a different one (e.g. when a host switches which pane is "active"). An
/// optional <see cref="NavigationGuard"/> lets a host veto any action — Back, Forward, Reload,
/// typed navigation or Home — uniformly, with a reason surfaced via <see cref="NavigationBlocked"/>.
/// </summary>
public partial class AddressBar : UserControl
{
    public static readonly DependencyProperty AddressTextProperty = DependencyProperty.Register(
        nameof(AddressText), typeof(string), typeof(AddressBar), new FrameworkPropertyMetadata(string.Empty));

    private readonly EventHandler<CoreWebView2SourceChangedEventArgs> _onSourceChanged;
    private readonly EventHandler<object> _onHistoryChanged;

    public AddressBar()
    {
        InitializeComponent();
        _onSourceChanged = (_, _) => { if (!Address.IsKeyboardFocusWithin) AddressText = Core!.Source; };
        _onHistoryChanged = (_, _) =>
        {
            BackButton.IsEnabled = Core!.CanGoBack;
            ForwardButton.IsEnabled = Core!.CanGoForward;
        };
    }

    public string AddressText
    {
        get => (string)GetValue(AddressTextProperty);
        set => SetValue(AddressTextProperty, value);
    }

    /// <summary>Whether the Back/Forward buttons are shown. Set once, in code-behind, before or
    /// after <see cref="Attach"/> — not meant for XAML data-binding or triggers.</summary>
    public bool ShowBackForward
    {
        get => BackButton.Visibility == Visibility.Visible;
        set
        {
            var visible = value ? Visibility.Visible : Visibility.Collapsed;
            BackButton.Visibility = visible;
            ForwardButton.Visibility = visible;
        }
    }

    /// <summary>Whether the Home button is shown. False (no button) by default.</summary>
    public bool ShowHome
    {
        get => HomeButton.Visibility == Visibility.Visible;
        set => HomeButton.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Optional static fallback URL for Home, used only if <see cref="HomeRequested"/>
    /// doesn't set <c>Handled = true</c>.</summary>
    public string? HomeUrl { get; set; }

    /// <summary>Optional search-engine template containing a <c>{query}</c> placeholder (e.g.
    /// "https://www.bing.com/search?q={query}"). Null (the default) means typed text that isn't a
    /// URL just gets an "https://" prefix and is navigated to literally, with no search fallback.</summary>
    public string? SearchUrlTemplate { get; set; }

    /// <summary>Returns null to allow an action, or a short reason string to block it (surfaced via
    /// <see cref="NavigationBlocked"/>). Null (the default) means every action is always allowed.</summary>
    public Func<AddressBarAction, string?>? NavigationGuard { get; set; }

    /// <summary>The <see cref="CoreWebView2"/> this bar is currently attached to, or null.</summary>
    public CoreWebView2? Core { get; private set; }

    public event EventHandler<AddressBarActionBlockedEventArgs>? NavigationBlocked;
    public event EventHandler<HomeRequestedEventArgs>? HomeRequested;

    /// <summary>Fired with the resolved URL after a typed Enter or an unhandled Home drives a
    /// navigation — a hook for a host that wants to e.g. hand focus back to its WebView2.</summary>
    public event EventHandler<string>? Navigated;

    /// <summary>Binds (or rebinds) this bar to <paramref name="core"/>. Safe to call repeatedly,
    /// including with the same instance (a cheap re-sync) or with null (detaches and blanks the
    /// bar). Call this again whenever a host's "active" <see cref="CoreWebView2"/> changes.</summary>
    public void Attach(CoreWebView2? core)
    {
        if (!ReferenceEquals(Core, core))
        {
            if (Core is not null)
            {
                Core.SourceChanged -= _onSourceChanged;
                Core.HistoryChanged -= _onHistoryChanged;
            }

            Core = core;

            if (Core is not null)
            {
                Core.SourceChanged += _onSourceChanged;
                Core.HistoryChanged += _onHistoryChanged;
            }
        }

        AddressText = Core?.Source ?? string.Empty;
        BackButton.IsEnabled = Core?.CanGoBack ?? false;
        ForwardButton.IsEnabled = Core?.CanGoForward ?? false;
    }

    /// <summary>Moves keyboard focus into the URL textbox, for a host that wants its own keybinding
    /// (e.g. Ctrl+L) to jump there.</summary>
    public void FocusAddress() => Address.Focus();

    public void GoBack() => OnBackClick(this, new RoutedEventArgs());
    public void GoForward() => OnForwardClick(this, new RoutedEventArgs());
    public void Reload() => OnReloadClick(this, new RoutedEventArgs());

    private bool TryGate(AddressBarAction action)
    {
        if (NavigationGuard?.Invoke(action) is { } reason)
        {
            NavigationBlocked?.Invoke(this, new AddressBarActionBlockedEventArgs(action, reason));
            return false;
        }

        return true;
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        if (TryGate(AddressBarAction.Back) && Core?.CanGoBack == true) Core.GoBack();
    }

    private void OnForwardClick(object sender, RoutedEventArgs e)
    {
        if (TryGate(AddressBarAction.Forward) && Core?.CanGoForward == true) Core.GoForward();
    }

    private void OnReloadClick(object sender, RoutedEventArgs e)
    {
        if (TryGate(AddressBarAction.Reload)) Core?.Reload();
    }

    private void OnHomeClick(object sender, RoutedEventArgs e)
    {
        if (!TryGate(AddressBarAction.Home)) return;

        var args = new HomeRequestedEventArgs();
        HomeRequested?.Invoke(this, args);
        if (!args.Handled && !string.IsNullOrEmpty(HomeUrl)) NavigateCore(HomeUrl);
    }

    private void OnAddressKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var url = Normalize(Address.Text, SearchUrlTemplate);
            if (url is null) return;
            if (!TryGate(AddressBarAction.Navigate)) return;
            NavigateCore(url);
        }
        else if (e.Key == Key.Escape)
        {
            AddressText = Core?.Source ?? AddressText;
        }
    }

    private void OnAddressFocused(object sender, KeyboardFocusChangedEventArgs e) => Address.SelectAll();

    private void NavigateCore(string url)
    {
        AddressText = url;
        Core?.Navigate(url);
        Navigated?.Invoke(this, url);
    }

    /// <summary>Trim → empty is a no-op (null) → an absolute http/https/file/about/edge URL is kept
    /// as-is → no-space text with a dot (or starting with localhost/127.0.0.1) gets a scheme prefix
    /// → otherwise, with a search template configured, substitutes the (URL-escaped) text into its
    /// "{query}" placeholder; with no template, falls back to a bare scheme-prefix-or-literal.</summary>
    internal static string? Normalize(string text, string? searchUrlTemplate)
    {
        var t = text.Trim();
        if (t.Length == 0) return null;

        if (Uri.TryCreate(t, UriKind.Absolute, out var abs) &&
            abs.Scheme is "http" or "https" or "file" or "about" or "edge")
        {
            return abs.ToString();
        }

        if (!t.Contains(' ') && (t.Contains('.') || t.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)))
        {
            var scheme = t.StartsWith("localhost", StringComparison.OrdinalIgnoreCase) ||
                         t.StartsWith("127.0.0.1", StringComparison.Ordinal)
                ? "http://"
                : "https://";
            return scheme + t;
        }

        if (searchUrlTemplate is { } template && template.Contains("{query}", StringComparison.Ordinal))
        {
            return template.Replace("{query}", Uri.EscapeDataString(t), StringComparison.Ordinal);
        }

        return t.Contains("://", StringComparison.Ordinal) ? t : "https://" + t;
    }
}
