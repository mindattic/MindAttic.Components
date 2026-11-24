using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// A titled content container (M.COMP-T221): an optional <see cref="Title"/> header in the <c>MaSectionLabel</c>
/// look, a 1px light-gray (<c>MaMuted</c>) rule under it, and the content set in by <see cref="ContentInset"/>
/// (the shared <c>MaPanelInset</c> resource by default), so every panel's content starts at the same left pixel.
/// <code>
/// &lt;ctl:MaPanel Title="Sessions"&gt;
///     &lt;StackPanel&gt;
///         &lt;TextBlock Text="..." /&gt;
///         &lt;ctl:MaPanel Title="Idle"&gt;...&lt;/ctl:MaPanel&gt;
///     &lt;/StackPanel&gt;
/// &lt;/ctl:MaPanel&gt;
/// </code>
/// <para>
/// Panels nest: a panel inside another (<see cref="IsNested"/>) draws a smaller sub-header and takes the
/// <c>MaPanelNestedInset</c> instead, whose left edge is 0, so its header and content line up with the parent's other
/// content rather than doubling the inset; a host that wants the extra step sets <see cref="ContentInset"/> itself.
/// With no <see cref="Title"/> the header row (and its rule) collapses and the panel is just the inset content.
/// <see cref="ShowDivider"/> false hides the rule alone. The panel is a UI Automation Group named by its title.
/// </para>
/// <para>
/// Its look is the implicit <c>MaPanel</c> style in Theme.xaml; the header row (<see cref="PartHeader"/>) keeps
/// leading columns free for a drag grip and an expand/collapse chevron.
/// </para>
/// </summary>
public class MaPanel : ContentControl
{
    /// <summary>The template part holding the header row and its rule; collapsed while <see cref="Title"/> is null.</summary>
    public const string PartHeader = "PART_Header";

    /// <summary>The template part that presents the <see cref="Title"/>.</summary>
    public const string PartTitle = "PART_Title";

    /// <summary>The template part that draws the rule under the header.</summary>
    public const string PartDivider = "PART_Divider";

    /// <summary>The template part that presents the content, set in by <see cref="ContentInset"/>.</summary>
    public const string PartContent = "PART_Content";

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(object), typeof(MaPanel),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty TitleTemplateProperty = DependencyProperty.Register(
        nameof(TitleTemplate), typeof(DataTemplate), typeof(MaPanel),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty ShowDividerProperty = DependencyProperty.Register(
        nameof(ShowDivider), typeof(bool), typeof(MaPanel),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty ContentInsetProperty = DependencyProperty.Register(
        nameof(ContentInset), typeof(Thickness), typeof(MaPanel),
        new FrameworkPropertyMetadata(default(Thickness), FrameworkPropertyMetadataOptions.AffectsMeasure));

    private static readonly DependencyPropertyKey IsNestedPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsNested), typeof(bool), typeof(MaPanel), new FrameworkPropertyMetadata(false));

    /// <summary>The read-only <see cref="IsNested"/> property.</summary>
    public static readonly DependencyProperty IsNestedProperty = IsNestedPropertyKey.DependencyProperty;

    static MaPanel()
    {
        // A container, not a stop in the tab order: its content takes focus.
        FocusableProperty.OverrideMetadata(typeof(MaPanel), new FrameworkPropertyMetadata(false));
        IsTabStopProperty.OverrideMetadata(typeof(MaPanel), new FrameworkPropertyMetadata(false));
    }

    public MaPanel()
    {
        Loaded += (_, _) => UpdateIsNested();
    }

    /// <summary>The header (usually a string); null (the default) collapses the header row and its rule.</summary>
    public object? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>How a non-string <see cref="Title"/> is drawn.</summary>
    public DataTemplate? TitleTemplate
    {
        get => (DataTemplate?)GetValue(TitleTemplateProperty);
        set => SetValue(TitleTemplateProperty, value);
    }

    /// <summary>Whether the 1px light-gray rule under the header shows; true by default.</summary>
    public bool ShowDivider
    {
        get => (bool)GetValue(ShowDividerProperty);
        set => SetValue(ShowDividerProperty, value);
    }

    /// <summary>The space around the content: the <c>MaPanelInset</c> resource by default, <c>MaPanelNestedInset</c>
    /// (no left step) while <see cref="IsNested"/>. A local value wins over both.</summary>
    public Thickness ContentInset
    {
        get => (Thickness)GetValue(ContentInsetProperty);
        set => SetValue(ContentInsetProperty, value);
    }

    /// <summary>True while this panel sits inside another <see cref="MaPanel"/>.</summary>
    public bool IsNested => (bool)GetValue(IsNestedProperty);

    protected override void OnVisualParentChanged(DependencyObject oldParent)
    {
        base.OnVisualParentChanged(oldParent);
        UpdateIsNested();
    }

    private void UpdateIsNested()
    {
        var nested = false;
        for (var node = ParentOf(this); node is not null && !nested; node = ParentOf(node))
            nested = node is MaPanel;
        SetValue(IsNestedPropertyKey, nested);
    }

    private static DependencyObject? ParentOf(DependencyObject node) =>
        (node is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(node) : null)
        ?? LogicalTreeHelper.GetParent(node);

    protected override AutomationPeer OnCreateAutomationPeer() => new MaPanelAutomationPeer(this);

    /// <summary>A panel is a Group named by its title (unless <c>AutomationProperties.Name</c> is set).</summary>
    private sealed class MaPanelAutomationPeer(MaPanel owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(MaPanel);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            if (!string.IsNullOrEmpty(name)) return name;
            return ((MaPanel)Owner).Title switch
            {
                null => string.Empty,
                string s => s,
                UIElement => string.Empty,
                var other => other.ToString() ?? string.Empty,
            };
        }
    }
}
