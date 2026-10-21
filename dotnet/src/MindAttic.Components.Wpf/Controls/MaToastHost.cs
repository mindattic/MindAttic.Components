using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// A row of <see cref="MaToast"/> cards (a wrapping row by default, so full-width toasts stack).
/// Give it its own row in the window so new toasts push the page down instead of covering
/// whatever has keyboard focus (WCAG 2.4.11).
/// <para>
/// Bind <see cref="ItemsControl.ItemsSource"/> to a collection of view models: each item becomes a
/// <see cref="MaToast"/> whose Title, Body and Accent come from <see cref="TitlePath"/>,
/// <see cref="BodyPath"/> and <see cref="AccentPath"/> ("Title", "Body", "Accent" by default; an
/// item without an Accent keeps MaAccent). <see cref="OpenCommand"/> and <see cref="DismissCommand"/>
/// run with the item as their parameter. With no <see cref="DismissCommand"/>, dismissing removes
/// the item itself (from Items, or from an editable ItemsSource list). MaToast elements can be
/// added directly as items too.
/// </para>
/// <para>
/// Each toast added after the first load is announced politely to screen readers as
/// "{Title}. {Body}" through a hidden live region (WCAG 4.1.3), without moving focus. Its
/// accessible name is "Notifications" unless <c>AutomationProperties.Name</c> is set.
/// </para>
/// </summary>
[TemplatePart(Name = PartAnnouncer, Type = typeof(TextBlock))]
public class MaToastHost : ItemsControl
{
    public const string PartAnnouncer = "PART_Announcer";

    public static readonly DependencyProperty OpenCommandProperty = DependencyProperty.Register(
        nameof(OpenCommand), typeof(ICommand), typeof(MaToastHost), new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty DismissCommandProperty = DependencyProperty.Register(
        nameof(DismissCommand), typeof(ICommand), typeof(MaToastHost), new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty TitlePathProperty = DependencyProperty.Register(
        nameof(TitlePath), typeof(string), typeof(MaToastHost), new FrameworkPropertyMetadata("Title"));

    public static readonly DependencyProperty BodyPathProperty = DependencyProperty.Register(
        nameof(BodyPath), typeof(string), typeof(MaToastHost), new FrameworkPropertyMetadata("Body"));

    public static readonly DependencyProperty AccentPathProperty = DependencyProperty.Register(
        nameof(AccentPath), typeof(string), typeof(MaToastHost), new FrameworkPropertyMetadata("Accent"));

    private static readonly DependencyPropertyKey LastAnnouncementPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(LastAnnouncement), typeof(string), typeof(MaToastHost), new FrameworkPropertyMetadata(string.Empty));

    public static readonly DependencyProperty LastAnnouncementProperty = LastAnnouncementPropertyKey.DependencyProperty;

    private TextBlock? _announcer;

    static MaToastHost()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MaToastHost), new FrameworkPropertyMetadata(typeof(MaToastHost)));
        FocusableProperty.OverrideMetadata(typeof(MaToastHost), new FrameworkPropertyMetadata(false));
    }

    public MaToastHost()
    {
        AddHandler(MaToast.DismissedEvent, new RoutedEventHandler(OnToastDismissed));
    }

    /// <summary>Run with the item when a toast's card is clicked (e.g. open what it's about).</summary>
    public ICommand? OpenCommand
    {
        get => (ICommand?)GetValue(OpenCommandProperty);
        set => SetValue(OpenCommandProperty, value);
    }

    /// <summary>Run with the item when a toast's dismiss button is clicked. When null, the host
    /// removes the item itself.</summary>
    public ICommand? DismissCommand
    {
        get => (ICommand?)GetValue(DismissCommandProperty);
        set => SetValue(DismissCommandProperty, value);
    }

    /// <summary>Binding path from an item to its toast's Title. "Title" by default.</summary>
    public string TitlePath
    {
        get => (string)GetValue(TitlePathProperty);
        set => SetValue(TitlePathProperty, value);
    }

    /// <summary>Binding path from an item to its toast's Body. "Body" by default.</summary>
    public string BodyPath
    {
        get => (string)GetValue(BodyPathProperty);
        set => SetValue(BodyPathProperty, value);
    }

    /// <summary>Binding path from an item to its toast's Accent brush. "Accent" by default.</summary>
    public string AccentPath
    {
        get => (string)GetValue(AccentPathProperty);
        set => SetValue(AccentPathProperty, value);
    }

    /// <summary>The text most recently sent to screen readers.</summary>
    public string LastAnnouncement => (string)GetValue(LastAnnouncementProperty);

    /// <summary>Says <paramref name="text"/> through a screen reader without moving focus: the
    /// hidden polite live region takes the text and raises LiveRegionChanged.</summary>
    public void Announce(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        SetValue(LastAnnouncementPropertyKey, text);
        if (_announcer is null) ApplyTemplate();
        if (_announcer is null) return;
        _announcer.Text = text;
        (UIElementAutomationPeer.FromElement(_announcer) ?? UIElementAutomationPeer.CreatePeerForElement(_announcer))
            ?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _announcer = GetTemplateChild(PartAnnouncer) as TextBlock;
        if (_announcer is not null && !string.IsNullOrEmpty(LastAnnouncement)) _announcer.Text = LastAnnouncement;
    }

    protected override bool IsItemItsOwnContainerOverride(object item) => item is MaToast;

    protected override DependencyObject GetContainerForItemOverride() => new MaToast();

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        if (element is not MaToast toast || ReferenceEquals(toast, item)) return;

        Bind(toast, MaToast.TitleProperty, item, TitlePath);
        Bind(toast, MaToast.BodyProperty, item, BodyPath);
        // Only bind the accent when the item has one, so an item without it keeps the style's MaAccent.
        if (Evaluate(item, AccentPath) is Brush) Bind(toast, MaToast.AccentProperty, item, AccentPath);

        toast.SetBinding(MaToast.CommandProperty, new Binding(nameof(OpenCommand)) { Source = this });
        toast.SetCurrentValue(MaToast.CommandParameterProperty, item);
        toast.SetBinding(MaToast.DismissCommandProperty, new Binding(nameof(DismissCommand)) { Source = this });
        toast.SetCurrentValue(MaToast.DismissCommandParameterProperty, item);
    }

    protected override void ClearContainerForItemOverride(DependencyObject element, object item)
    {
        base.ClearContainerForItemOverride(element, item);
        if (element is not MaToast toast || ReferenceEquals(toast, item)) return;
        BindingOperations.ClearAllBindings(toast);
        toast.ClearValue(MaToast.CommandParameterProperty);
        toast.ClearValue(MaToast.DismissCommandParameterProperty);
    }

    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);
        if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems is null) return;
        foreach (var item in e.NewItems) Announce(Describe(item));
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new MaToastHostAutomationPeer(this);

    /// <summary>"{Title}. {Body}" for an item (or just the title when there's no body).</summary>
    private string Describe(object? item)
    {
        string title, body;
        if (item is MaToast toast)
        {
            title = toast.Title;
            body = toast.Body;
        }
        else
        {
            title = Evaluate(item, TitlePath)?.ToString() ?? string.Empty;
            body = Evaluate(item, BodyPath)?.ToString() ?? string.Empty;
        }
        if (string.IsNullOrWhiteSpace(body)) return title;
        if (string.IsNullOrWhiteSpace(title)) return body;
        return $"{title}. {body}";
    }

    private void OnToastDismissed(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not MaToast toast || DismissCommand is not null || toast.DismissCommand is not null) return;
        var item = ItemContainerGenerator.ItemFromContainer(toast);
        if (item == DependencyProperty.UnsetValue) item = toast;
        if (ItemsSource is null)
        {
            if (!Items.Contains(item)) return;
            Items.Remove(item);
        }
        else if (ItemsSource is IList { IsReadOnly: false, IsFixedSize: false } list)
        {
            list.Remove(item);
        }
        else return;
        e.Handled = true;
    }

    private static void Bind(DependencyObject target, DependencyProperty property, object item, string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        BindingOperations.SetBinding(target, property, new Binding(path) { Source = item, Mode = BindingMode.OneWay });
    }

    /// <summary>Reads <paramref name="path"/> off <paramref name="item"/> through a one-off
    /// binding, so any binding path works (nested properties, indexers).</summary>
    private static object? Evaluate(object? item, string? path)
    {
        if (item is null || string.IsNullOrEmpty(path)) return null;
        var probe = new Probe();
        BindingOperations.SetBinding(probe, Probe.ValueProperty,
            new Binding(path) { Source = item, Mode = BindingMode.OneTime, FallbackValue = null });
        var value = probe.GetValue(Probe.ValueProperty);
        BindingOperations.ClearBinding(probe, Probe.ValueProperty);
        return value;
    }

    private sealed class Probe : DependencyObject
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(object), typeof(Probe), new PropertyMetadata(null));
    }

    /// <summary>A named group whose children are the toasts' own peers (each named by its title).</summary>
    private sealed class MaToastHostAutomationPeer(MaToastHost owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        protected override string GetClassNameCore() => nameof(MaToastHost);

        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrEmpty(name) ? "Notifications" : name;
        }
    }
}
