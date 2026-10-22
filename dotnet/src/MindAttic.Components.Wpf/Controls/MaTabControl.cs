using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// A themed tab strip: a <see cref="TabControl"/> whose tabs are <see cref="MaTabItem"/>s, flat
/// labels on the chrome with the selected one lifted onto <c>MaSurface</c> and underlined in its
/// own <see cref="MaTabItem.Accent"/>. Every tab can lead with an <see cref="MaStatusLight"/>
/// (<see cref="MaTabItem.Status"/>) and end with a muted trailing text (<see cref="MaTabItem.Trailing"/>,
/// e.g. "23% left").
/// <code>
/// &lt;ctl:MaTabControl ItemsSource="{Binding People}" SelectedItem="{Binding Current}" DisplayMemberPath="Name"&gt;
///     &lt;ctl:MaTabControl.ItemContainerStyle&gt;
///         &lt;Style TargetType="ctl:MaTabItem" BasedOn="{StaticResource {x:Type ctl:MaTabItem}}"&gt;
///             &lt;Setter Property="Accent" Value="{Binding AccentBrush}" /&gt;
///             &lt;Setter Property="Status" Value="{Binding Health}" /&gt;
///             &lt;Setter Property="Trailing" Value="{Binding LeftText}" /&gt;
///         &lt;/Style&gt;
///     &lt;/ctl:MaTabControl.ItemContainerStyle&gt;
/// &lt;/ctl:MaTabControl&gt;
/// </code>
/// <para>
/// It is a real <see cref="TabControl"/>, so the keyboard and UI Automation come with it: Tab
/// enters the strip once at the selected tab, Left/Right (cycling) and Home/End move the selection,
/// Ctrl+Tab / Ctrl+Shift+Tab switch from anywhere inside, and a screen reader sees a tab list of
/// selectable tab items. A tab's trailing text and status are its UI Automation item status, so
/// they're read after its name. Set <see cref="ShowContent"/> to false for a bare strip whose
/// selection drives content shown elsewhere (e.g. terminal panes). An <c>ItemContainerStyle</c>
/// must be <c>BasedOn</c> the implicit <see cref="MaTabItem"/> style to keep the look.
/// </para>
/// </summary>
public class MaTabControl : TabControl
{
    public static readonly DependencyProperty ShowContentProperty = DependencyProperty.Register(
        nameof(ShowContent), typeof(bool), typeof(MaTabControl),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>The template part that hosts the selected tab's content.</summary>
    public const string PartContentHost = "PART_SelectedContentHost";

    /// <summary>The template part the tabs are laid out in.</summary>
    public const string PartHeaderPanel = "PART_HeaderPanel";

    /// <summary>True (the default) shows the selected tab's content under the strip; false collapses
    /// it, leaving only the strip.</summary>
    public bool ShowContent
    {
        get => (bool)GetValue(ShowContentProperty);
        set => SetValue(ShowContentProperty, value);
    }

    protected override DependencyObject GetContainerForItemOverride() => new MaTabItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is TabItem;
}

/// <summary>
/// One tab of an <see cref="MaTabControl"/>: its <c>Header</c>, led by an optional
/// <see cref="MaStatusLight"/> and followed by an optional trailing text, underlined in its own
/// <see cref="Accent"/> while selected. See <see cref="MaTabControl"/>.
/// </summary>
public class MaTabItem : TabItem
{
    public static readonly DependencyProperty AccentProperty = DependencyProperty.Register(
        nameof(Accent), typeof(Brush), typeof(MaTabItem),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TrailingProperty = DependencyProperty.Register(
        nameof(Trailing), typeof(string), typeof(MaTabItem),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnItemStatusPartChanged));

    public static readonly DependencyProperty TrailingForegroundProperty = DependencyProperty.Register(
        nameof(TrailingForeground), typeof(Brush), typeof(MaTabItem),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
        nameof(Status), typeof(MaStatus?), typeof(MaTabItem),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnItemStatusPartChanged));

    public static readonly DependencyProperty StatusLabelProperty = DependencyProperty.Register(
        nameof(StatusLabel), typeof(string), typeof(MaTabItem),
        new FrameworkPropertyMetadata(null, OnItemStatusPartChanged));

    /// <summary>The template part that draws the selected underline.</summary>
    public const string PartUnderline = "PART_Underline";

    /// <summary>The template part that draws <see cref="Status"/>.</summary>
    public const string PartStatusLight = "PART_StatusLight";

    /// <summary>The template part that draws <see cref="Trailing"/>.</summary>
    public const string PartTrailing = "PART_Trailing";

    /// <summary>The tab's own colour: its underline while selected. <c>MaAccent</c> by default
    /// (from the implicit style), so set it per tab, e.g. one colour per person.</summary>
    public Brush? Accent
    {
        get => (Brush?)GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    /// <summary>Short text after the header (e.g. "23% left"); null or empty (the default) for none.
    /// It is part of the tab's UI Automation item status.</summary>
    public string? Trailing
    {
        get => (string?)GetValue(TrailingProperty);
        set => SetValue(TrailingProperty, value);
    }

    /// <summary>The trailing text's colour: <c>MaMuted</c> by default (from the implicit style); set
    /// e.g. <c>MaBad</c> when a limit is nearly gone.</summary>
    public Brush? TrailingForeground
    {
        get => (Brush?)GetValue(TrailingForegroundProperty);
        set => SetValue(TrailingForegroundProperty, value);
    }

    /// <summary>A status light before the header; null (the default) for none.</summary>
    public MaStatus? Status
    {
        get => (MaStatus?)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    /// <summary>The status light's words (its <see cref="MaStatusLight.Label"/>); null uses the
    /// status word.</summary>
    public string? StatusLabel
    {
        get => (string?)GetValue(StatusLabelProperty);
        set => SetValue(StatusLabelProperty, value);
    }

    /// <summary>The item status this tab last set on itself, so one the host sets is never overwritten.</summary>
    private string? ownItemStatus;

    private static void OnItemStatusPartChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((MaTabItem)d).UpdateItemStatus();

    /// <summary>The status words then the trailing text, comma separated, e.g. "Good, 23% left".</summary>
    internal string ComposeItemStatus()
    {
        var parts = new List<string>(2);
        if (Status is { } status) parts.Add(string.IsNullOrEmpty(StatusLabel) ? MaStatusLight.Describe(status) : StatusLabel!);
        if (!string.IsNullOrEmpty(Trailing)) parts.Add(Trailing!);
        return string.Join(", ", parts);
    }

    private void UpdateItemStatus()
    {
        var local = ReadLocalValue(AutomationProperties.ItemStatusProperty);
        if (local != DependencyProperty.UnsetValue && !Equals(local, ownItemStatus)) return; // the host's own
        ownItemStatus = ComposeItemStatus();
        if (ownItemStatus.Length == 0)
        {
            ownItemStatus = null;
            ClearValue(AutomationProperties.ItemStatusProperty);
        }
        else
        {
            SetValue(AutomationProperties.ItemStatusProperty, ownItemStatus);
        }
    }
}
