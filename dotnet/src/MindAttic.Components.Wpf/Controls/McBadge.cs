using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// A badge (chip): a small outlined pill holding a word, optionally led by an icon-font
/// <see cref="Glyph"/>, in a state colour. Set <see cref="Control.Foreground"/> to the state
/// colour: the glyph, the text and the outline all follow it (the outline unless
/// <see cref="Control.BorderBrush"/> is set too), so a state is shown as a word and a glyph as
/// well as a colour (WCAG 1.4.1).
/// <code>
/// &lt;ctl:McBadge Glyph="{Binding StateGlyph}" Content="{Binding StateLabel}" Foreground="{Binding StateBrush}" /&gt;
/// &lt;ctl:McBadge Style="{StaticResource McTag}" Content="{Binding Type}" /&gt;
/// </code>
/// <para>
/// Its look is the implicit <c>McBadge</c> style in Theme.xaml (a rounded pill, 12px semibold);
/// the <c>McTag</c> style is the plain variant: a squarer, quieter label with a neutral
/// <c>McControlBorder</c> outline and normal-weight 11px text, for a type or category tag. The
/// glyph is decoration: it's hidden from UI Automation, so a screen reader reads only the word.
/// </para>
/// </summary>
public class McBadge : ContentControl
{
    /// <summary>Segoe MDL2 Assets, the icon font the rest of the theme uses.</summary>
    public static readonly FontFamily DefaultGlyphFontFamily = new("Segoe MDL2 Assets");

    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
        nameof(Glyph), typeof(string), typeof(McBadge),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty GlyphFontFamilyProperty = DependencyProperty.Register(
        nameof(GlyphFontFamily), typeof(FontFamily), typeof(McBadge),
        new FrameworkPropertyMetadata(DefaultGlyphFontFamily, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty GlyphFontSizeProperty = DependencyProperty.Register(
        nameof(GlyphFontSize), typeof(double), typeof(McBadge),
        new FrameworkPropertyMetadata(10d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
        nameof(CornerRadius), typeof(CornerRadius), typeof(McBadge),
        new FrameworkPropertyMetadata(new CornerRadius(10), FrameworkPropertyMetadataOptions.AffectsRender));

    static McBadge()
    {
        // A badge is a label, not a stop in the tab order.
        FocusableProperty.OverrideMetadata(typeof(McBadge), new FrameworkPropertyMetadata(false));
        IsTabStopProperty.OverrideMetadata(typeof(McBadge), new FrameworkPropertyMetadata(false));
    }

    /// <summary>The icon-font character drawn before the content (e.g. <c>"\uE768"</c>, or
    /// <c>&amp;#xE768;</c> in XAML); empty (the default) for none, which collapses the glyph and
    /// its gap.</summary>
    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    /// <summary>The font the <see cref="Glyph"/> is drawn in; Segoe MDL2 Assets by default.</summary>
    public FontFamily GlyphFontFamily
    {
        get => (FontFamily)GetValue(GlyphFontFamilyProperty);
        set => SetValue(GlyphFontFamilyProperty, value);
    }

    /// <summary>The <see cref="Glyph"/>'s size; 10 by default.</summary>
    public double GlyphFontSize
    {
        get => (double)GetValue(GlyphFontSizeProperty);
        set => SetValue(GlyphFontSizeProperty, value);
    }

    /// <summary>The outline's corner radius: 10 (a pill) by default, 3 in <c>McTag</c>.</summary>
    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    /// <summary>The template part that draws the <see cref="Glyph"/>.</summary>
    public const string PartGlyph = "PART_Glyph";

    protected override AutomationPeer OnCreateAutomationPeer() => new McBadgeAutomationPeer(this);

    /// <summary>A badge reads as text: its name is its content (unless
    /// <c>AutomationProperties.Name</c> is set), and the decorative glyph is left out of its
    /// children so a screen reader never announces a private-use icon character.</summary>
    private sealed class McBadgeAutomationPeer(McBadge owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(McBadge);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            if (!string.IsNullOrEmpty(name)) return name;
            var badge = (McBadge)Owner;
            return badge.Content switch
            {
                null => string.Empty,
                string s => s,
                UIElement => string.Empty,
                var other => other.ToString() ?? string.Empty,
            };
        }

        protected override List<AutomationPeer>? GetChildrenCore()
        {
            var children = base.GetChildrenCore();
            var badge = (McBadge)Owner;
            var glyph = badge.Template?.FindName(PartGlyph, badge) as UIElement;
            if (children is null || glyph is null) return children;
            children.RemoveAll(p => p is UIElementAutomationPeer u && ReferenceEquals(u.Owner, glyph));
            return children;
        }
    }
}
