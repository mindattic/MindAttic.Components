using System.Collections;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// A masked field for an API key or other secret (M.COMP-T155). Masked by default; the eye button reveals it on a click
/// and it hides again after <see cref="RevealTimeout"/> (30 s), when keyboard focus leaves the control, when its window
/// deactivates or when it unloads. The copy button puts the value on the clipboard in a form Windows keeps out of
/// clipboard history and cloud sync (<see cref="MaSecretClipboard"/>) and empties the clipboard after
/// <see cref="ClipboardTimeout"/> (30 s) unless something else was copied since. Ctrl+C in the revealed text goes the
/// same way; cut and drag are blocked.
/// <para>
/// The value lives only in the edit buffer, never in a bindable property, so no binding, trace or diagnostics tool can
/// read it: the host loads it with <see cref="SetSecret"/>, reads it with <see cref="GetSecret"/> when saving, and
/// <see cref="SecretChanged"/> (no value in its args) tells it the user edited it. <see cref="HasValue"/> says whether
/// one is set. The automation tree is named by <c>AutomationProperties.Name</c> (or "Secret"), reports "Set"/"Not set"
/// as its item status, is a password field, and has no value or text pattern anywhere inside it.
/// </para>
/// </summary>
public class MaSecretBox : FrameworkElement
{
    private static readonly DependencyPropertyKey IsRevealedKey = DependencyProperty.RegisterReadOnly(
        nameof(IsRevealed), typeof(bool), typeof(MaSecretBox), new PropertyMetadata(false));

    /// <summary>The read-only <see cref="IsRevealed"/> property.</summary>
    public static readonly DependencyProperty IsRevealedProperty = IsRevealedKey.DependencyProperty;

    private static readonly DependencyPropertyKey HasValueKey = DependencyProperty.RegisterReadOnly(
        nameof(HasValue), typeof(bool), typeof(MaSecretBox), new PropertyMetadata(false));

    /// <summary>The read-only <see cref="HasValue"/> property.</summary>
    public static readonly DependencyProperty HasValueProperty = HasValueKey.DependencyProperty;

    /// <summary>The <see cref="Placeholder"/> property.</summary>
    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
        nameof(Placeholder), typeof(string), typeof(MaSecretBox), new PropertyMetadata("Not set", (d, e) =>
            ((MaSecretBox)d)._placeholder.Text = (string?)e.NewValue ?? ""));

    /// <summary>The template-free parts, public by name for tests and hosts that restyle them.</summary>
    public const string PartMasked = "PART_Masked", PartRevealed = "PART_Revealed", PartReveal = "PART_Reveal",
        PartCopy = "PART_Copy", PartPlaceholder = "PART_Placeholder";

    private const string RevealGlyph = "", HideGlyph = "", CopyGlyph = "";

    private readonly Grid _root = new();
    private readonly PasswordBox _masked = new() { Name = PartMasked };
    private readonly RevealedSecretBox _revealed = new() { Name = PartRevealed };
    private readonly TextBlock _placeholder = new() { Name = PartPlaceholder, Text = "Not set" };
    private readonly Button _reveal = new() { Name = PartReveal, Content = RevealGlyph };
    private readonly Button _copy = new() { Name = PartCopy, Content = CopyGlyph };
    private readonly DispatcherTimer _hideTimer;
    private readonly DispatcherTimer _clipboardTimer;
    private IMaSecretClipboard _clipboard = MaSecretClipboard.System;
    private long? _copiedStamp;
    private bool _quiet;
    private Window? _window;

    /// <summary>Builds the field: the masked box, the reveal and copy buttons.</summary>
    public MaSecretBox()
    {
        _hideTimer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher) { Interval = TimeSpan.FromSeconds(30) };
        _hideTimer.Tick += (_, _) => Hide();
        _clipboardTimer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher) { Interval = TimeSpan.FromSeconds(30) };
        _clipboardTimer.Tick += (_, _) => ClearCopied();

        _root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _masked.SetResourceReference(StyleProperty, "MaPasswordBox");
        _masked.PasswordChanged += (_, _) => Edited();
        AutomationProperties.SetName(_masked, "Secret value");

        _revealed.SetResourceReference(StyleProperty, "MaTextBox");
        _revealed.Visibility = Visibility.Collapsed;
        _revealed.IsUndoEnabled = false;
        _revealed.TextChanged += (_, _) => Edited();
        _revealed.ContextMenuOpening += (_, e) => e.Handled = true;
        DataObject.AddCopyingHandler(_revealed, (_, e) => e.CancelCommand());
        _revealed.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, (_, _) => CopyToClipboard(), (_, e) => { e.CanExecute = true; e.Handled = true; }));
        _revealed.CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, (_, _) => { }, (_, e) => { e.CanExecute = false; e.Handled = true; }));
        AutomationProperties.SetName(_revealed, "Secret value");

        _placeholder.SetResourceReference(TextBlock.ForegroundProperty, "MaMuted");
        _placeholder.IsHitTestVisible = false;
        _placeholder.Margin = new Thickness(9, 0, 0, 0);
        _placeholder.VerticalAlignment = VerticalAlignment.Center;

        _reveal.SetResourceReference(StyleProperty, "MaToolButton");
        _reveal.Click += (_, _) => { if (IsRevealed) Hide(); else Reveal(); };
        AutomationProperties.SetName(_reveal, "Show secret");
        _reveal.ToolTip = "Show for 30 seconds";

        _copy.SetResourceReference(StyleProperty, "MaToolButton");
        _copy.Click += (_, _) => CopyToClipboard();
        AutomationProperties.SetName(_copy, "Copy secret");
        _copy.ToolTip = "Copy (cleared from the clipboard after 30 seconds)";

        Grid.SetColumn(_reveal, 1);
        Grid.SetColumn(_copy, 2);
        _root.Children.Add(_masked);
        _root.Children.Add(_revealed);
        _root.Children.Add(_placeholder);
        _root.Children.Add(_reveal);
        _root.Children.Add(_copy);
        AddVisualChild(_root);
        AddLogicalChild(_root);
        UpdateState();

        IsKeyboardFocusWithinChanged += (_, e) => { if (!(bool)e.NewValue) Hide(); };
        Loaded += (_, _) => Attach(Window.GetWindow(this));
        Unloaded += (_, _) => { Hide(); Attach(null); };
    }

    /// <summary>Raised when the user edits the value. Carries no value: call <see cref="GetSecret"/> to read it.</summary>
    public event EventHandler? SecretChanged;

    /// <summary>True while the value is shown in plain text.</summary>
    public bool IsRevealed => (bool)GetValue(IsRevealedProperty);

    /// <summary>True when a value is set (never the value itself).</summary>
    public bool HasValue => (bool)GetValue(HasValueProperty);

    /// <summary>Muted text shown while no value is set ("Not set" by default).</summary>
    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>How long a reveal lasts before the value is masked again (30 s).</summary>
    public TimeSpan RevealTimeout
    {
        get => _hideTimer.Interval;
        set => _hideTimer.Interval = value;
    }

    /// <summary>How long a copy stays on the clipboard before it's cleared (30 s).</summary>
    public TimeSpan ClipboardTimeout
    {
        get => _clipboardTimer.Interval;
        set => _clipboardTimer.Interval = value;
    }

    /// <summary>Where copies go: the Windows clipboard unless replaced (tests).</summary>
    public IMaSecretClipboard Clipboard
    {
        get => _clipboard;
        set => _clipboard = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>The value in the edit buffer ("" when none). Read it only to store it.</summary>
    public string GetSecret() => IsRevealed ? _revealed.Text : _masked.Password;

    /// <summary>Loads a value into the edit buffer without raising <see cref="SecretChanged"/>.</summary>
    public void SetSecret(string? secret) => Quietly(() =>
    {
        if (IsRevealed) _revealed.Text = secret ?? "";
        else _masked.Password = secret ?? "";
    });

    /// <summary>Empties the value without raising <see cref="SecretChanged"/>.</summary>
    public void Clear() => SetSecret(null);

    /// <summary>Shows the value in plain text until <see cref="RevealTimeout"/>, focus leaves or the window deactivates.</summary>
    public void Reveal()
    {
        if (IsRevealed || !IsEnabled) return;
        var focus = _masked.IsKeyboardFocused;
        Quietly(() =>
        {
            _revealed.Text = _masked.Password;
            _masked.Clear();
        });
        SetValue(IsRevealedKey, true);
        UpdateState();
        if (focus)
        {
            _revealed.Focus();
            _revealed.CaretIndex = _revealed.Text.Length;
        }
        _hideTimer.Stop();
        _hideTimer.Start();
    }

    /// <summary>Masks the value again and empties the plain-text buffer.</summary>
    public void Hide()
    {
        _hideTimer.Stop();
        if (!IsRevealed) return;
        var focus = _revealed.IsKeyboardFocused;
        Quietly(() =>
        {
            _masked.Password = _revealed.Text;
            _revealed.Clear();
        });
        SetValue(IsRevealedKey, false);
        UpdateState();
        if (focus) _masked.Focus();
    }

    /// <summary>Copies the value to <see cref="Clipboard"/> (kept out of history and cloud sync) and clears it after
    /// <see cref="ClipboardTimeout"/> unless the clipboard changed since. False when there's nothing to copy or the
    /// clipboard is busy.</summary>
    public bool CopyToClipboard()
    {
        var secret = GetSecret();
        if (secret.Length == 0) return false;
        try
        {
            _copiedStamp = _clipboard.Put(secret);
        }
        catch (ExternalException)
        {
            return false;
        }
        _clipboardTimer.Stop();
        _clipboardTimer.Start();
        return true;
    }

    private void ClearCopied()
    {
        _clipboardTimer.Stop();
        if (_copiedStamp is { } stamp)
            try
            {
                if (_clipboard.Stamp == stamp) _clipboard.Clear();
            }
            catch (ExternalException) { }
        _copiedStamp = null;
    }

    private void Edited()
    {
        UpdateState();
        if (!_quiet) SecretChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Quietly(Action change)
    {
        _quiet = true;
        try { change(); }
        finally { _quiet = false; }
        UpdateState();
    }

    private void UpdateState()
    {
        var revealed = IsRevealed;
        var has = (revealed ? _revealed.Text.Length : _masked.Password.Length) > 0;
        SetValue(HasValueKey, has);
        _masked.Visibility = revealed ? Visibility.Collapsed : Visibility.Visible;
        _revealed.Visibility = revealed ? Visibility.Visible : Visibility.Collapsed;
        _placeholder.Visibility = has ? Visibility.Collapsed : Visibility.Visible;
        _reveal.Content = revealed ? HideGlyph : RevealGlyph;
        AutomationProperties.SetName(_reveal, revealed ? "Hide secret" : "Show secret");
        _copy.IsEnabled = has;
    }

    private void Attach(Window? window)
    {
        if (_window is not null) _window.Deactivated -= WindowDeactivated;
        _window = window;
        if (_window is not null) _window.Deactivated += WindowDeactivated;
    }

    private void WindowDeactivated(object? sender, EventArgs e) => Hide();

    /// <inheritdoc />
    protected override int VisualChildrenCount => 1;

    /// <inheritdoc />
    protected override Visual GetVisualChild(int index) => index == 0 ? _root : throw new ArgumentOutOfRangeException(nameof(index));

    /// <inheritdoc />
    protected override IEnumerator LogicalChildren => new object[] { _root }.GetEnumerator();

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        _root.Measure(availableSize);
        return _root.DesiredSize;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        _root.Arrange(new Rect(finalSize));
        return finalSize;
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new MaSecretBoxAutomationPeer(this);

    private sealed class MaSecretBoxAutomationPeer(MaSecretBox owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(MaSecretBox);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Edit;
        protected override bool IsPasswordCore() => true;
        protected override string GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : "Secret";
        protected override string GetItemStatusCore() => owner.HasValue ? "Set" : "Not set";
        public override object? GetPattern(PatternInterface patternInterface) => null;
    }
}

/// <summary>The plain-text buffer of a revealed <see cref="MaSecretBox"/>: a TextBox whose automation peer is a
/// password edit with no value or text pattern, so a screen reader or UI Automation client never reads the text.</summary>
internal sealed class RevealedSecretBox : TextBox
{
    protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this);

    private sealed class Peer(RevealedSecretBox owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(TextBox);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Edit;
        protected override bool IsPasswordCore() => true;
        protected override string GetNameCore() => AutomationProperties.GetName(Owner) is { Length: > 0 } name ? name : "Secret value";
        protected override List<AutomationPeer>? GetChildrenCore() => null;
        public override object? GetPattern(PatternInterface patternInterface) => null;
    }
}
