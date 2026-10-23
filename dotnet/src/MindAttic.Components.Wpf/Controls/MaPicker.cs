using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// A themed drop-down for choosing one of several people or logins (M.COMP-T52), e.g. who a ticket is assigned to.
/// Every option, and the chosen one in the closed box, shows its name, an optional leading <see cref="MaStatusLight"/>
/// and a muted trailing text (e.g. "23% left", "week 97%"). Bind the options' properties by path:
/// <code>
/// &lt;ctl:MaPicker ItemsSource="{Binding People}" SelectedItem="{Binding Assignee}" DisplayMemberPath="Name"
///               StatusMemberPath="Health" TrailingMemberPath="LeftText" Placeholder="Unassigned"
///               AutomationProperties.Name="Assigned to" /&gt;
/// </code>
/// or list <see cref="MaPickerItem"/>s with <c>Status</c> and <c>Trailing</c> set on each.
/// <para>
/// It is a real <see cref="ComboBox"/>, so the keyboard and UI Automation come with it: Tab stops on it once, Up/Down
/// (and Home/End) change the choice while it's closed, Alt+Down or F4 opens it, typing a name's first letters jumps
/// to it, Enter takes the highlighted option and Esc closes without changing; a screen reader sees a combo box with
/// expand/collapse and selection, and options named by their <c>DisplayMemberPath</c>. An option's status words and
/// trailing text are its UI Automation item status ("Good, 23% left"), and the picker's own item status is the
/// chosen option's. The status light is never the only cue: its word is in the item status and its tooltip.
/// </para>
/// </summary>
public class MaPicker : ComboBox
{
    public static readonly DependencyProperty StatusMemberPathProperty = DependencyProperty.Register(
        nameof(StatusMemberPath), typeof(string), typeof(MaPicker), new FrameworkPropertyMetadata(null, OnSelectionPartChanged));

    public static readonly DependencyProperty StatusLabelMemberPathProperty = DependencyProperty.Register(
        nameof(StatusLabelMemberPath), typeof(string), typeof(MaPicker), new FrameworkPropertyMetadata(null, OnSelectionPartChanged));

    public static readonly DependencyProperty TrailingMemberPathProperty = DependencyProperty.Register(
        nameof(TrailingMemberPath), typeof(string), typeof(MaPicker), new FrameworkPropertyMetadata(null, OnSelectionPartChanged));

    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
        nameof(Placeholder), typeof(string), typeof(MaPicker), new FrameworkPropertyMetadata(null));

    private static readonly DependencyPropertyKey SelectedStatusPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(SelectedStatus), typeof(MaStatus?), typeof(MaPicker), new FrameworkPropertyMetadata(null, OnItemStatusPartChanged));

    /// <summary>The read-only <see cref="SelectedStatus"/> property.</summary>
    public static readonly DependencyProperty SelectedStatusProperty = SelectedStatusPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey SelectedStatusLabelPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(SelectedStatusLabel), typeof(string), typeof(MaPicker), new FrameworkPropertyMetadata(null, OnItemStatusPartChanged));

    /// <summary>The read-only <see cref="SelectedStatusLabel"/> property.</summary>
    public static readonly DependencyProperty SelectedStatusLabelProperty = SelectedStatusLabelPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey SelectedTrailingPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(SelectedTrailing), typeof(string), typeof(MaPicker), new FrameworkPropertyMetadata(null, OnItemStatusPartChanged));

    /// <summary>The read-only <see cref="SelectedTrailing"/> property.</summary>
    public static readonly DependencyProperty SelectedTrailingProperty = SelectedTrailingPropertyKey.DependencyProperty;

    // Binding sinks that follow the chosen option's status, label and trailing text (by path, or an MaPickerItem's own
    // properties) and copy them into the read-only Selected* properties.
    private static readonly DependencyProperty StatusSourceProperty = DependencyProperty.Register(
        "StatusSource", typeof(object), typeof(MaPicker),
        new FrameworkPropertyMetadata(null, (d, e) => d.SetValue(SelectedStatusPropertyKey, ToStatus(e.NewValue))));

    private static readonly DependencyProperty StatusLabelSourceProperty = DependencyProperty.Register(
        "StatusLabelSource", typeof(object), typeof(MaPicker),
        new FrameworkPropertyMetadata(null, (d, e) => d.SetValue(SelectedStatusLabelPropertyKey, e.NewValue?.ToString())));

    private static readonly DependencyProperty TrailingSourceProperty = DependencyProperty.Register(
        "TrailingSource", typeof(object), typeof(MaPicker),
        new FrameworkPropertyMetadata(null, (d, e) => d.SetValue(SelectedTrailingPropertyKey, e.NewValue?.ToString())));

    /// <summary>The template part that draws <see cref="SelectedStatus"/> in the closed box.</summary>
    public const string PartSelectedStatusLight = "PART_SelectedStatusLight";

    /// <summary>The template part that draws <see cref="SelectedTrailing"/> in the closed box.</summary>
    public const string PartSelectedTrailing = "PART_SelectedTrailing";

    /// <summary>The template part that draws <see cref="Placeholder"/> while nothing is chosen.</summary>
    public const string PartPlaceholder = "PART_Placeholder";

    /// <summary>Each option's property holding its <see cref="MaStatus"/> (or a status word such as "Good"); null or
    /// empty (the default) shows no status lights.</summary>
    public string? StatusMemberPath
    {
        get => (string?)GetValue(StatusMemberPathProperty);
        set => SetValue(StatusMemberPathProperty, value);
    }

    /// <summary>Each option's property holding its status in words (e.g. "Signed in"), the light's label; null uses the
    /// status word.</summary>
    public string? StatusLabelMemberPath
    {
        get => (string?)GetValue(StatusLabelMemberPathProperty);
        set => SetValue(StatusLabelMemberPathProperty, value);
    }

    /// <summary>Each option's property holding its muted trailing text (e.g. "23% left"); null shows none.</summary>
    public string? TrailingMemberPath
    {
        get => (string?)GetValue(TrailingMemberPathProperty);
        set => SetValue(TrailingMemberPathProperty, value);
    }

    /// <summary>Muted text in the closed box while nothing is chosen (e.g. "Unassigned"); not part of the accessible
    /// name, so still set <c>AutomationProperties.Name</c>.</summary>
    public string? Placeholder
    {
        get => (string?)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>The chosen option's status, or null.</summary>
    public MaStatus? SelectedStatus => (MaStatus?)GetValue(SelectedStatusProperty);

    /// <summary>The chosen option's status label, or null.</summary>
    public string? SelectedStatusLabel => (string?)GetValue(SelectedStatusLabelProperty);

    /// <summary>The chosen option's trailing text, or null.</summary>
    public string? SelectedTrailing => (string?)GetValue(SelectedTrailingProperty);

    /// <summary>The item status this picker last set on itself, so one the host sets is never overwritten.</summary>
    private string? ownItemStatus;

    protected override DependencyObject GetContainerForItemOverride() => new MaPickerItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is ComboBoxItem;

    /// <summary>Binds a data option's container to the member paths (they win over an ItemContainerStyle's setters), and
    /// names it by its <c>DisplayMemberPath</c> for UI Automation unless it has a name.</summary>
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        if (element is not MaPickerItem container || ReferenceEquals(element, item)) return;
        BindPath(container, MaPickerItem.StatusProperty, StatusMemberPath);
        BindPath(container, MaPickerItem.StatusLabelProperty, StatusLabelMemberPath);
        BindPath(container, MaPickerItem.TrailingProperty, TrailingMemberPath);
        if (container.ReadLocalValue(AutomationProperties.NameProperty) == DependencyProperty.UnsetValue)
        {
            if (!string.IsNullOrEmpty(DisplayMemberPath))
                container.SetBinding(AutomationProperties.NameProperty, new Binding(DisplayMemberPath));
            else if (item is not UIElement && item is not null)
                container.SetValue(AutomationProperties.NameProperty, item.ToString());
        }
    }

    protected override void ClearContainerForItemOverride(DependencyObject element, object item)
    {
        if (element is MaPickerItem container && !ReferenceEquals(element, item))
        {
            BindingOperations.ClearBinding(container, MaPickerItem.StatusProperty);
            BindingOperations.ClearBinding(container, MaPickerItem.StatusLabelProperty);
            BindingOperations.ClearBinding(container, MaPickerItem.TrailingProperty);
        }
        base.ClearContainerForItemOverride(element, item);
    }

    private static void BindPath(FrameworkElement target, DependencyProperty property, string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        target.SetBinding(property, new Binding(path) { Mode = BindingMode.OneWay });
    }

    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        base.OnSelectionChanged(e);
        FollowSelection();
    }

    private static void OnSelectionPartChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((MaPicker)d).FollowSelection();

    /// <summary>Points the Selected* properties at the chosen option: an MaPickerItem's own Status/StatusLabel/Trailing,
    /// or a data option's member paths, following later changes to either.</summary>
    private void FollowSelection()
    {
        var item = SelectedItem;
        var own = item as MaPickerItem;
        Follow(StatusSourceProperty, item, own is not null ? nameof(MaPickerItem.Status) : StatusMemberPath);
        Follow(StatusLabelSourceProperty, item, own is not null ? nameof(MaPickerItem.StatusLabel) : StatusLabelMemberPath);
        Follow(TrailingSourceProperty, item, own is not null ? nameof(MaPickerItem.Trailing) : TrailingMemberPath);
    }

    private void Follow(DependencyProperty sink, object? item, string? path)
    {
        if (item is null || string.IsNullOrEmpty(path))
        {
            BindingOperations.ClearBinding(this, sink);
            ClearValue(sink);
            return;
        }
        SetBinding(sink, new Binding(path) { Source = item, Mode = BindingMode.OneWay });
    }

    /// <summary>A status from an option: an <see cref="MaStatus"/>, or its name as a string; anything else is none.</summary>
    public static MaStatus? ToStatus(object? value) => value switch
    {
        MaStatus s => s,
        string text when Enum.TryParse<MaStatus>(text, true, out var parsed) => parsed,
        _ => null,
    };

    private static void OnItemStatusPartChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var picker = (MaPicker)d;
        picker.ownItemStatus = MaPickerItem.SetOwnItemStatus(picker, picker.ownItemStatus,
            MaPickerItem.Compose(picker.SelectedStatus, picker.SelectedStatusLabel, picker.SelectedTrailing));
    }
}

/// <summary>
/// One option of an <see cref="MaPicker"/>: its content (the name), led by an optional <see cref="MaStatusLight"/>
/// (<see cref="Status"/>) and followed by a muted <see cref="Trailing"/> text. The highlighted option tints
/// <c>MaHover</c>, the keyboard-focused one is outlined in <c>MaFocus</c> and the chosen one is SemiBold with an
/// <c>MaAccent</c> bar at its left edge (a shape and a weight, not colour alone).
/// </summary>
public class MaPickerItem : ComboBoxItem
{
    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
        nameof(Status), typeof(MaStatus?), typeof(MaPickerItem),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnItemStatusPartChanged));

    public static readonly DependencyProperty StatusLabelProperty = DependencyProperty.Register(
        nameof(StatusLabel), typeof(string), typeof(MaPickerItem), new FrameworkPropertyMetadata(null, OnItemStatusPartChanged));

    public static readonly DependencyProperty TrailingProperty = DependencyProperty.Register(
        nameof(Trailing), typeof(string), typeof(MaPickerItem),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnItemStatusPartChanged));

    public static readonly DependencyProperty TrailingForegroundProperty = DependencyProperty.Register(
        nameof(TrailingForeground), typeof(Brush), typeof(MaPickerItem),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>The template part that draws <see cref="Status"/>.</summary>
    public const string PartStatusLight = "PART_StatusLight";

    /// <summary>The template part that draws <see cref="Trailing"/>.</summary>
    public const string PartTrailing = "PART_Trailing";

    /// <summary>The template part that marks the chosen option.</summary>
    public const string PartSelectedBar = "PART_SelectedBar";

    /// <summary>A status light before the name; null (the default) for none.</summary>
    public MaStatus? Status
    {
        get => (MaStatus?)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    /// <summary>The status light's words; null uses the status word.</summary>
    public string? StatusLabel
    {
        get => (string?)GetValue(StatusLabelProperty);
        set => SetValue(StatusLabelProperty, value);
    }

    /// <summary>Short muted text after the name (e.g. "23% left"); null or empty for none.</summary>
    public string? Trailing
    {
        get => (string?)GetValue(TrailingProperty);
        set => SetValue(TrailingProperty, value);
    }

    /// <summary>The trailing text's colour: <c>MaMuted</c> by default (from the implicit style).</summary>
    public Brush? TrailingForeground
    {
        get => (Brush?)GetValue(TrailingForegroundProperty);
        set => SetValue(TrailingForegroundProperty, value);
    }

    private string? ownItemStatus;

    private static void OnItemStatusPartChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var item = (MaPickerItem)d;
        item.ownItemStatus = SetOwnItemStatus(item, item.ownItemStatus, Compose(item.Status, item.StatusLabel, item.Trailing));
    }

    /// <summary>The status words (the label, or the status word) then the trailing text, comma separated.</summary>
    internal static string Compose(MaStatus? status, string? label, string? trailing)
    {
        var parts = new List<string>(2);
        if (status is { } s) parts.Add(string.IsNullOrEmpty(label) ? MaStatusLight.Describe(s) : label!);
        if (!string.IsNullOrEmpty(trailing)) parts.Add(trailing!);
        return string.Join(", ", parts);
    }

    /// <summary>Sets <paramref name="value"/> as <paramref name="target"/>'s item status unless the host set its own;
    /// returns what it set (null when it cleared or left it).</summary>
    internal static string? SetOwnItemStatus(DependencyObject target, string? previous, string value)
    {
        var local = target.ReadLocalValue(AutomationProperties.ItemStatusProperty);
        if (local != DependencyProperty.UnsetValue && !Equals(local, previous)) return previous; // the host's own
        if (value.Length == 0)
        {
            target.ClearValue(AutomationProperties.ItemStatusProperty);
            return null;
        }
        target.SetValue(AutomationProperties.ItemStatusProperty, value);
        return value;
    }
}
