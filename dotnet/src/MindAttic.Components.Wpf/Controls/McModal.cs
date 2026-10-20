using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// An in-window modal dialog: a dimmed backdrop over the whole host area with a centred, themed
/// card (McChrome, McChromeBorder outline, drop shadow) holding a <see cref="Title"/> (a level-2
/// heading), a close button, the <see cref="ContentControl.Content"/> and an optional
/// <see cref="Footer"/> for buttons.
/// <para>
/// Place it last in the window's root <see cref="Grid"/>, spanning every row and column, and bind
/// <see cref="IsOpen"/> (two-way by default). Esc, the close button and a click on the backdrop
/// all close it: through <see cref="CloseCommand"/> when one is set (for a view model whose open
/// flag is read-only), otherwise by setting <see cref="IsOpen"/> to false. While it's open,
/// keyboard focus is trapped inside the card (Tab cycles, access keys are scoped to it, focus
/// can't be moved out), and on close focus goes back to whatever had it before it opened.
/// </para>
/// <para>
/// Its accessible name is <c>AutomationProperties.Name</c> when set, otherwise the
/// <see cref="Title"/>; screen readers see it as a dialog window. Its look is the implicit
/// <c>McModal</c> style in Theme.xaml, so the theme must be merged.
/// </para>
/// </summary>
[TemplatePart(Name = PartBackdrop, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartCard, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartCloseButton, Type = typeof(ButtonBase))]
public class McModal : ContentControl
{
    public const string PartBackdrop = "PART_Backdrop";
    public const string PartCard = "PART_Card";
    public const string PartCloseButton = "PART_CloseButton";

    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen), typeof(bool), typeof(McModal),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOpenChanged));

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(McModal), new FrameworkPropertyMetadata(string.Empty));

    public static readonly DependencyProperty FooterProperty = DependencyProperty.Register(
        nameof(Footer), typeof(object), typeof(McModal), new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty DialogWidthProperty = DependencyProperty.Register(
        nameof(DialogWidth), typeof(double), typeof(McModal), new FrameworkPropertyMetadata(480d));

    public static readonly DependencyProperty CloseCommandProperty = DependencyProperty.Register(
        nameof(CloseCommand), typeof(ICommand), typeof(McModal), new FrameworkPropertyMetadata(null));

    public static readonly RoutedEvent OpenedEvent = EventManager.RegisterRoutedEvent(
        nameof(Opened), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(McModal));

    public static readonly RoutedEvent ClosedEvent = EventManager.RegisterRoutedEvent(
        nameof(Closed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(McModal));

    private FrameworkElement? _backdrop;
    private ButtonBase? _closeButton;
    private IInputElement? _returnFocusTo;

    static McModal()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(McModal), new FrameworkPropertyMetadata(typeof(McModal)));
        VisibilityProperty.OverrideMetadata(typeof(McModal), new FrameworkPropertyMetadata(Visibility.Collapsed));
        FocusableProperty.OverrideMetadata(typeof(McModal), new FrameworkPropertyMetadata(false));
        KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(McModal),
            new FrameworkPropertyMetadata(KeyboardNavigationMode.Cycle));
        KeyboardNavigation.ControlTabNavigationProperty.OverrideMetadata(typeof(McModal),
            new FrameworkPropertyMetadata(KeyboardNavigationMode.Cycle));
        KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(typeof(McModal),
            new FrameworkPropertyMetadata(KeyboardNavigationMode.Contained));
    }

    public McModal()
    {
        AccessKeyManager.AddAccessKeyPressedHandler(this, OnAccessKeyPressed);
    }

    /// <summary>Whether the dialog is showing. Two-way by default.</summary>
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>The heading at the top of the card (a level-2 heading for screen readers), and the
    /// dialog's accessible name unless <c>AutomationProperties.Name</c> is set.</summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Optional footer content, right-aligned under a rule (usually a row of McButton /
    /// McPrimaryButton). The footer is hidden when this is null.</summary>
    public object? Footer
    {
        get => GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }

    /// <summary>The card's width (it never exceeds the host area less a margin). 480 by default.</summary>
    public double DialogWidth
    {
        get => (double)GetValue(DialogWidthProperty);
        set => SetValue(DialogWidthProperty, value);
    }

    /// <summary>Optional: run instead of setting <see cref="IsOpen"/> to false when Esc, the close
    /// button or the backdrop asks to close (e.g. a view model's toggle command). If it can't
    /// execute, the dialog stays open.</summary>
    public ICommand? CloseCommand
    {
        get => (ICommand?)GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    /// <summary>Raised once the dialog has opened.</summary>
    public event RoutedEventHandler Opened
    {
        add => AddHandler(OpenedEvent, value);
        remove => RemoveHandler(OpenedEvent, value);
    }

    /// <summary>Raised once the dialog has closed and focus has been returned.</summary>
    public event RoutedEventHandler Closed
    {
        add => AddHandler(ClosedEvent, value);
        remove => RemoveHandler(ClosedEvent, value);
    }

    /// <summary>Closes the dialog the way Esc, the close button and the backdrop do: through
    /// <see cref="CloseCommand"/> if set, otherwise by clearing <see cref="IsOpen"/>.</summary>
    public void RequestClose()
    {
        if (!IsOpen) return;
        if (CloseCommand is { } command)
        {
            if (command.CanExecute(null)) command.Execute(null);
            return;
        }
        SetCurrentValue(IsOpenProperty, false);
    }

    public override void OnApplyTemplate()
    {
        if (_backdrop is not null) _backdrop.MouseLeftButtonDown -= OnBackdropMouseDown;
        if (_closeButton is not null) _closeButton.Click -= OnCloseButtonClick;

        base.OnApplyTemplate();

        _backdrop = GetTemplateChild(PartBackdrop) as FrameworkElement;
        _closeButton = GetTemplateChild(PartCloseButton) as ButtonBase;

        if (_backdrop is not null) _backdrop.MouseLeftButtonDown += OnBackdropMouseDown;
        if (_closeButton is not null) _closeButton.Click += OnCloseButtonClick;
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new McModalAutomationPeer(this);

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Handled || !IsOpen || e.Key != Key.Escape) return;
        RequestClose();
        e.Handled = true;
    }

    /// <summary>The trap's last line: refuse any focus move to an element outside the dialog while
    /// it's open (a mouse click can't reach one past the backdrop, Tab cycles and access keys are
    /// scoped, but code or an automation client still could). Focus leaving the window entirely
    /// (NewFocus null) is allowed.</summary>
    protected override void OnPreviewLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnPreviewLostKeyboardFocus(e);
        if (IsOpen && e.NewFocus is DependencyObject next && !Contains(next))
            e.Handled = true;
    }

    /// <summary>Whether <paramref name="element"/> is this dialog or inside it. A focusable
    /// ContentElement (e.g. a Hyperlink) isn't a Visual, which <see cref="System.Windows.Media.Visual.IsAncestorOf"/>
    /// rejects, so walk up to its nearest Visual host first.</summary>
    private bool Contains(DependencyObject element)
    {
        DependencyObject? d = element;
        while (d is not null and not System.Windows.Media.Visual and not System.Windows.Media.Media3D.Visual3D)
            d = LogicalTreeHelper.GetParent(d) ?? (d is ContentElement ce ? ContentOperations.GetParent(ce) : null);
        return d is not null && (ReferenceEquals(d, this) || IsAncestorOf(d));
    }

    private void OnAccessKeyPressed(object sender, AccessKeyPressedEventArgs e)
    {
        // Scope Alt+key to the dialog, so an access key can't click a button behind the backdrop.
        if (!IsOpen || e.Handled || e.Scope is not null) return;
        e.Scope = this;
        e.Handled = true;
    }

    private void OnBackdropMouseDown(object sender, MouseButtonEventArgs e)
    {
        RequestClose();
        e.Handled = true;
    }

    private void OnCloseButtonClick(object sender, RoutedEventArgs e) => RequestClose();

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var modal = (McModal)d;
        if ((bool)e.NewValue) modal.Open();
        else modal.Close();
    }

    private void Open()
    {
        var focused = Keyboard.FocusedElement;
        _returnFocusTo = focused is DependencyObject f && Contains(f) ? null : focused;

        SetCurrentValue(VisibilityProperty, Visibility.Visible);
        // Focus the first focusable element once the card has been laid out (its template and
        // content may only exist after this pass).
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            if (!IsOpen) return;
            FocusFirst();
            RaiseEvent(new RoutedEventArgs(OpenedEvent, this));
        }));
    }

    private void Close()
    {
        SetCurrentValue(VisibilityProperty, Visibility.Collapsed);
        var target = _returnFocusTo;
        _returnFocusTo = null;
        if (target is UIElement { IsVisible: true, Focusable: true, IsEnabled: true } or ContentElement { Focusable: true, IsEnabled: true })
            target.Focus();
        RaiseEvent(new RoutedEventArgs(ClosedEvent, this));
    }

    /// <summary>Moves keyboard focus to the first tab stop in the card: the content's, then the
    /// footer's, then the close button (the template puts it last in tab order).</summary>
    public void FocusFirst()
    {
        ApplyTemplate();
        UpdateLayout();
        if (IsKeyboardFocusWithin) return;
        if (!MoveFocus(new TraversalRequest(FocusNavigationDirection.First)) || !IsKeyboardFocusWithin)
            _closeButton?.Focus();
    }

    private sealed class McModalAutomationPeer(McModal owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Window;

        protected override string GetClassNameCore() => nameof(McModal);

        protected override bool IsDialogCore() => true;

        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrEmpty(name) ? ((McModal)Owner).Title ?? string.Empty : name;
        }
    }
}
