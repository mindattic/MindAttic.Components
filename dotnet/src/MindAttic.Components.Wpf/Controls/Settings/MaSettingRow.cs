using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>One full-width row of an <see cref="MaSettingsForm"/>: the label, the field's full name (small, selectable
/// for copying), the help text, the editor and, after <see cref="MaSettingsForm.Validate"/>, an error line.</summary>
public sealed class MaSettingRow : Border
{
    private readonly TextBlock _error;
    private DispatcherTimer? _highlightTimer;

    internal MaSettingRow(MaFieldContext context, FrameworkElement editor)
    {
        Context = context;
        Editor = editor;
        Padding = new Thickness(10, 10, 10, 12);
        BorderThickness = new Thickness(0, 0, 0, 1);
        SetResourceReference(BorderBrushProperty, "MaChromeBorder");
        Background = Brushes.Transparent;
        AutomationProperties.SetName(this, context.Label);

        var label = new TextBlock { Text = context.Label, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
        label.SetResourceReference(TextBlock.ForegroundProperty, "MaText");

        var name = new TextBox
        {
            Text = context.FieldName, IsReadOnly = true, IsReadOnlyCaretVisible = false, BorderThickness = new Thickness(0),
            Background = Brushes.Transparent, Padding = new Thickness(0), FontSize = 11, FontFamily = new FontFamily("Cascadia Mono, Consolas"),
            Margin = new Thickness(0, 2, 0, 0), ToolTip = "Full name: select to copy",
        };
        name.SetResourceReference(TextBox.ForegroundProperty, "MaMuted");
        AutomationProperties.SetName(name, "Full name");
        NameBox = name;

        var panel = new StackPanel();
        panel.Children.Add(label);
        panel.Children.Add(name);
        if (!string.IsNullOrWhiteSpace(context.Help))
        {
            var help = new TextBlock { Text = context.Help, TextWrapping = TextWrapping.Wrap, FontSize = 12, Margin = new Thickness(0, 4, 0, 0) };
            help.SetResourceReference(TextBlock.ForegroundProperty, "MaMuted");
            panel.Children.Add(help);
            AutomationProperties.SetHelpText(editor, context.Help);
        }
        editor.Margin = new Thickness(0, 8, 0, 0);
        if (string.IsNullOrEmpty(AutomationProperties.GetName(editor))) AutomationProperties.SetName(editor, context.Label);
        panel.Children.Add(editor);

        _error = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0), Visibility = Visibility.Collapsed };
        _error.SetResourceReference(TextBlock.ForegroundProperty, "MaRec");
        panel.Children.Add(_error);
        Child = panel;
    }

    /// <summary>The field this row edits.</summary>
    public MaFieldContext Context { get; }

    /// <summary>The field's full name.</summary>
    public string FieldName => Context.FieldName;

    /// <summary>The editor built for the field's kind.</summary>
    public FrameworkElement Editor { get; }

    /// <summary>The selectable full-name text.</summary>
    public TextBox NameBox { get; }

    /// <summary>The error shown under the editor, or null for none.</summary>
    public string? Error
    {
        get => _error.Visibility == Visibility.Visible ? _error.Text : null;
        set
        {
            _error.Text = value ?? "";
            _error.Visibility = value is null ? Visibility.Collapsed : Visibility.Visible;
            AutomationProperties.SetItemStatus(this, value ?? "");
        }
    }

    /// <summary>True while the row is lit after <see cref="MaSettingsForm.ScrollTo"/>.</summary>
    public bool IsHighlighted { get; private set; }

    internal void Highlight(TimeSpan duration)
    {
        IsHighlighted = true;
        SetResourceReference(BackgroundProperty, "MaHover");
        _highlightTimer?.Stop();
        _highlightTimer = new DispatcherTimer(duration, DispatcherPriority.Normal, (_, _) => EndHighlight(), Dispatcher);
        _highlightTimer.Start();
    }

    private void EndHighlight()
    {
        _highlightTimer?.Stop();
        _highlightTimer = null;
        IsHighlighted = false;
        Background = Brushes.Transparent;
    }
}
