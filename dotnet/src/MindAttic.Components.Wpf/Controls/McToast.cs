using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// A themed notification card: an <see cref="Accent"/> outline and left stripe on McSurface, a
/// <see cref="Title"/> and a body clipped to two lines (<see cref="Body"/>), both inside one
/// clickable button, plus a dismiss (x) McToolButton.
/// <para>
/// Clicking the card runs <see cref="Command"/> and raises <see cref="Click"/>; the dismiss
/// button runs <see cref="DismissCommand"/> and raises <see cref="Dismissed"/>. A toast with no
/// <see cref="DismissCommand"/> whose <see cref="Dismissed"/> nobody handles hides itself. Inside
/// a <see cref="McToastHost"/> the host supplies both commands and removes dismissed items.
/// </para>
/// <para>
/// The card's accessible name is the <see cref="Title"/> with the <see cref="Body"/> as its help
/// text, and the dismiss button is named "Dismiss: {Title}". Its look is the implicit
/// <c>McToast</c> style in Theme.xaml, so the theme must be merged.
/// </para>
/// </summary>
[TemplatePart(Name = PartOpenButton, Type = typeof(ButtonBase))]
[TemplatePart(Name = PartDismissButton, Type = typeof(ButtonBase))]
public class McToast : Control
{
    public const string PartOpenButton = "PART_OpenButton";
    public const string PartDismissButton = "PART_DismissButton";

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(McToast), new FrameworkPropertyMetadata(string.Empty));

    public static readonly DependencyProperty BodyProperty = DependencyProperty.Register(
        nameof(Body), typeof(string), typeof(McToast), new FrameworkPropertyMetadata(string.Empty));

    public static readonly DependencyProperty AccentProperty = DependencyProperty.Register(
        nameof(Accent), typeof(Brush), typeof(McToast), new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command), typeof(ICommand), typeof(McToast), new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
        nameof(CommandParameter), typeof(object), typeof(McToast), new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty DismissCommandProperty = DependencyProperty.Register(
        nameof(DismissCommand), typeof(ICommand), typeof(McToast), new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty DismissCommandParameterProperty = DependencyProperty.Register(
        nameof(DismissCommandParameter), typeof(object), typeof(McToast), new FrameworkPropertyMetadata(null));

    public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent(
        nameof(Click), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(McToast));

    public static readonly RoutedEvent DismissedEvent = EventManager.RegisterRoutedEvent(
        nameof(Dismissed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(McToast));

    private ButtonBase? _openButton;
    private ButtonBase? _dismissButton;

    static McToast()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(McToast), new FrameworkPropertyMetadata(typeof(McToast)));
        FocusableProperty.OverrideMetadata(typeof(McToast), new FrameworkPropertyMetadata(false));
    }

    /// <summary>The first line, in semibold. Also the card's accessible name.</summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>The muted body text, wrapped and clipped to two lines. Also the card's help text.</summary>
    public string Body
    {
        get => (string)GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    /// <summary>The outline and left stripe colour (McAccent by default), e.g. a status colour.</summary>
    public Brush? Accent
    {
        get => (Brush?)GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    /// <summary>Run when the card (title and body) is clicked, if it can execute.</summary>
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <summary>Run when the dismiss (x) button is clicked, if it can execute.</summary>
    public ICommand? DismissCommand
    {
        get => (ICommand?)GetValue(DismissCommandProperty);
        set => SetValue(DismissCommandProperty, value);
    }

    public object? DismissCommandParameter
    {
        get => GetValue(DismissCommandParameterProperty);
        set => SetValue(DismissCommandParameterProperty, value);
    }

    /// <summary>Raised after the card is clicked (and <see cref="Command"/> has run).</summary>
    public event RoutedEventHandler Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    /// <summary>Raised after the dismiss button is clicked (and <see cref="DismissCommand"/> has run).</summary>
    public event RoutedEventHandler Dismissed
    {
        add => AddHandler(DismissedEvent, value);
        remove => RemoveHandler(DismissedEvent, value);
    }

    /// <summary>Does what a click on the card does: runs <see cref="Command"/>, then raises <see cref="Click"/>.</summary>
    public void PerformClick()
    {
        if (Command is { } command && command.CanExecute(CommandParameter)) command.Execute(CommandParameter);
        RaiseEvent(new RoutedEventArgs(ClickEvent, this));
    }

    /// <summary>Does what the dismiss button does: runs <see cref="DismissCommand"/>, then raises
    /// <see cref="Dismissed"/>; with no command and no handler the toast collapses itself.</summary>
    public void Dismiss()
    {
        var command = DismissCommand;
        if (command is not null && command.CanExecute(DismissCommandParameter)) command.Execute(DismissCommandParameter);
        var args = new RoutedEventArgs(DismissedEvent, this);
        RaiseEvent(args);
        if (!args.Handled && command is null) SetCurrentValue(VisibilityProperty, Visibility.Collapsed);
    }

    public override void OnApplyTemplate()
    {
        if (_openButton is not null) _openButton.Click -= OnOpenButtonClick;
        if (_dismissButton is not null) _dismissButton.Click -= OnDismissButtonClick;

        base.OnApplyTemplate();

        _openButton = GetTemplateChild(PartOpenButton) as ButtonBase;
        _dismissButton = GetTemplateChild(PartDismissButton) as ButtonBase;

        if (_openButton is not null) _openButton.Click += OnOpenButtonClick;
        if (_dismissButton is not null) _dismissButton.Click += OnDismissButtonClick;
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new McToastAutomationPeer(this);

    private void OnOpenButtonClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true; // the toast's own Click replaces the inner button's
        PerformClick();
    }

    private void OnDismissButtonClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        Dismiss();
    }

    private sealed class McToastAutomationPeer(McToast owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        protected override string GetClassNameCore() => nameof(McToast);

        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrEmpty(name) ? ((McToast)Owner).Title ?? string.Empty : name;
        }
    }
}
