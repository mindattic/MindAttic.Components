using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// A tile with a coloured bar down its left edge (M.COMP-T52): an <c>MaTileButton</c> (a bordered <c>MaSurface</c> card
/// that fills with <c>MaTileHover</c> on hover, the focus ring on keyboard focus) whose 4px bar is drawn in its own
/// <see cref="Accent"/>, e.g. one colour per state or per person. Its content is free (a title over an
/// <c>MaTileSubtitle</c>, a trailing count), padded by <c>Padding</c> inside the bar.
/// <code>
/// &lt;ctl:MaAccentTile Accent="{Binding Accent}" Command="{Binding SelectCommand}" CommandParameter="{Binding}"
///                  ctl:MaTile.IsSelected="{Binding IsSelected}" AutomationProperties.Name="{Binding Name}"&gt;
///     &lt;StackPanel&gt;
///         &lt;TextBlock Text="{Binding Name}" FontWeight="SemiBold" /&gt;
///         &lt;TextBlock Text="{Binding Subtitle}" Style="{StaticResource MaTileSubtitle}" /&gt;
///     &lt;/StackPanel&gt;
/// &lt;/ctl:MaAccentTile&gt;
/// </code>
/// It is a <see cref="Button"/>, so Enter/Space, <c>Command</c> and the button automation come with it. The bar is
/// decoration: name the tile with <c>AutomationProperties.Name</c> and say the state in its content, never by colour
/// alone. <see cref="MaTile.IsSelectedProperty"/> outlines it and reads "Selected" as its item status.
/// </summary>
public class MaAccentTile : Button
{
    public static readonly DependencyProperty AccentProperty = DependencyProperty.Register(
        nameof(Accent), typeof(Brush), typeof(MaAccentTile),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty AccentWidthProperty = DependencyProperty.Register(
        nameof(AccentWidth), typeof(double), typeof(MaAccentTile),
        new FrameworkPropertyMetadata(4d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>The template part that draws <see cref="Accent"/>.</summary>
    public const string PartAccentBar = "PART_AccentBar";

    /// <summary>The left bar's colour: <c>MaAccent</c> by default (from the implicit style).</summary>
    public Brush? Accent
    {
        get => (Brush?)GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    /// <summary>The left bar's width, 4px by default.</summary>
    public double AccentWidth
    {
        get => (double)GetValue(AccentWidthProperty);
        set => SetValue(AccentWidthProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new MaAccentTileAutomationPeer(this);

    private sealed class MaAccentTileAutomationPeer(MaAccentTile owner) : ButtonAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(MaAccentTile);
    }
}
