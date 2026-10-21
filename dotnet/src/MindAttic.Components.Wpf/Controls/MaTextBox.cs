using System.Windows;
using System.Windows.Controls;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// Attached properties for the <c>MaTextBox</c> and <c>MaTextArea</c> styles.
/// <c>ctl:MaTextBox.Placeholder="Search repos"</c> on a <see cref="TextBox"/> using either style
/// shows that text, muted, while the box is empty. The placeholder is drawn by the style's
/// template: it ignores the mouse (clicks reach the box) and is left out of the box's accessible
/// name, so set <c>AutomationProperties.Name</c> as usual.
/// </summary>
public static class MaTextBox
{
    /// <summary>The muted text shown while the box is empty; empty (none) by default.</summary>
    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.RegisterAttached(
        "Placeholder", typeof(string), typeof(MaTextBox), new FrameworkPropertyMetadata(string.Empty));

    /// <summary>Gets the box's placeholder text.</summary>
    public static string GetPlaceholder(TextBox textBox) => (string)textBox.GetValue(PlaceholderProperty);

    /// <summary>Sets the box's placeholder text.</summary>
    public static void SetPlaceholder(TextBox textBox, string value) => textBox.SetValue(PlaceholderProperty, value);
}
