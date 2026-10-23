using System.Media;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>
/// A themed stand-in for <see cref="MessageBox"/>: a small MaChrome window with a dark title bar
/// (<see cref="MaWindow.DarkTitleBarProperty"/>), an icon, the message and a row of MaButtons (OK,
/// OK/Cancel, Yes/No or Yes/No/Cancel), returning a <see cref="MessageBoxResult"/>. Use it for a
/// prompt when no app window is open to host an <see cref="MaModal"/>, e.g. during startup or setup.
/// <para>
/// <see cref="Show(string, string, MessageBoxButton, MessageBoxImage, MessageBoxResult)"/> shows
/// it modally and returns the choice. The first button (OK or Yes) is the default (Enter) and is
/// drawn as MaPrimaryButton unless <c>defaultResult</c> names another one. Esc and the title
/// bar's close button answer Cancel when there is a Cancel button, otherwise OK for an OK box and
/// No for a Yes/No box. It merges Theme.xaml itself when the app hasn't, and a box shown before
/// the app has a window of its own neither becomes <see cref="Application.MainWindow"/> nor shuts
/// the app down when it closes, just like the system message box.
/// </para>
/// <para>
/// The window is named by its caption and reads the message as its help text; the icon reads as
/// its severity ("Information", "Warning", "Error", "Question").
/// </para>
/// </summary>
public class MaMessageBox : Window
{
    private readonly MessageBoxResult _escapeResult;
    private readonly List<Button> _buttons = [];

    /// <summary>Builds the box without showing it; <see cref="Show(string, string, MessageBoxButton,
    /// MessageBoxImage, MessageBoxResult)"/> is the usual entry point.</summary>
    public MaMessageBox(string message, string caption = "", MessageBoxButton buttons = MessageBoxButton.OK,
        MessageBoxImage image = MessageBoxImage.None, MessageBoxResult defaultResult = MessageBoxResult.None)
    {
        Message = message ?? string.Empty;
        Image = image;
        Title = caption ?? string.Empty;

        if (TryFindResource("MaChrome") is null)
            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/MindAttic.Components.Wpf;component/Theme.xaml"),
            });

        MaWindow.SetThemedTitleBar(this, true);
        SetResourceReference(BackgroundProperty, "MaChrome");
        SetResourceReference(ForegroundProperty, "MaText");
        FontSize = 13;
        SizeToContent = SizeToContent.WidthAndHeight;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        MinWidth = 360;
        MaxWidth = 560;
        AutomationProperties.SetHelpText(this, Message);

        var results = buttons switch
        {
            MessageBoxButton.OKCancel => new[] { MessageBoxResult.OK, MessageBoxResult.Cancel },
            MessageBoxButton.YesNo => [MessageBoxResult.Yes, MessageBoxResult.No],
            MessageBoxButton.YesNoCancel => [MessageBoxResult.Yes, MessageBoxResult.No, MessageBoxResult.Cancel],
            _ => [MessageBoxResult.OK],
        };
        _escapeResult = results.Contains(MessageBoxResult.Cancel) ? MessageBoxResult.Cancel
            : results.Contains(MessageBoxResult.No) ? MessageBoxResult.No
            : MessageBoxResult.OK;
        DefaultResult = results.Contains(defaultResult) ? defaultResult : results[0];
        Result = _escapeResult;

        var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        foreach (var result in results)
        {
            var isDefault = result == DefaultResult;
            var button = new Button
            {
                Content = Label(result),
                Tag = result,
                IsDefault = isDefault,
                MinWidth = 84,
                Margin = new Thickness(_buttons.Count == 0 ? 0 : 8, 0, 0, 0),
            };
            button.SetResourceReference(StyleProperty, isDefault ? "MaPrimaryButton" : "MaButton");
            button.FocusVisualStyle = null; // the MaButton styles draw their own focus border
            AutomationProperties.SetName(button, result.ToString());
            button.Click += (_, _) => Choose(result);
            _buttons.Add(button);
            row.Children.Add(button);
        }

        var text = new TextBlock
        {
            Text = Message,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var body = new DockPanel { Margin = new Thickness(20, 20, 20, 20), LastChildFill = true };
        if (Glyph(image) is { } glyph)
        {
            var icon = new TextBlock
            {
                Text = glyph.Glyph,
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = 28,
                Margin = new Thickness(0, 0, 16, 0),
                VerticalAlignment = VerticalAlignment.Top,
            };
            icon.SetResourceReference(TextBlock.ForegroundProperty, glyph.Brush);
            AutomationProperties.SetName(icon, glyph.Name);
            DockPanel.SetDock(icon, Dock.Left);
            body.Children.Add(icon);
            IconElement = icon;
        }
        body.Children.Add(text);

        var footer = new Border { BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(20, 12, 20, 12), Child = row };
        footer.SetResourceReference(Border.BorderBrushProperty, "MaChromeBorder");
        footer.SetResourceReference(Border.BackgroundProperty, "MaSurface");
        DockPanel.SetDock(footer, Dock.Bottom);

        var root = new DockPanel { LastChildFill = true };
        root.Children.Add(footer);
        root.Children.Add(body);
        Content = root;

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Escape) return;
            e.Handled = true;
            Choose(_escapeResult);
        };
        // Logical focus only: it becomes keyboard focus when the window activates, so building or
        // showing the box without activation never takes the user's keyboard.
        FocusManager.SetFocusedElement(this, _buttons.First(b => (MessageBoxResult)b.Tag == DefaultResult));
    }

    /// <summary>The message shown in the box.</summary>
    public string Message { get; }

    /// <summary>The icon shown beside the message.</summary>
    public MessageBoxImage Image { get; }

    /// <summary>The button Enter presses and that has focus when the box opens.</summary>
    public MessageBoxResult DefaultResult { get; }

    /// <summary>The answer: the button that was clicked, or the Esc/close answer until one is.</summary>
    public MessageBoxResult Result { get; private set; }

    /// <summary>The buttons, in order (each one's <c>Tag</c> is its <see cref="MessageBoxResult"/>).</summary>
    public IReadOnlyList<Button> Buttons => _buttons;

    /// <summary>The icon element, or null when <see cref="Image"/> is <see cref="MessageBoxImage.None"/>.</summary>
    public FrameworkElement? IconElement { get; }

    private void Choose(MessageBoxResult result)
    {
        Result = result;
        Close();
    }

    /// <summary>Shows a themed message box modally and returns the button chosen.</summary>
    public static MessageBoxResult Show(string message, string caption = "", MessageBoxButton buttons = MessageBoxButton.OK,
        MessageBoxImage image = MessageBoxImage.None, MessageBoxResult defaultResult = MessageBoxResult.None) =>
        Show(null, message, caption, buttons, image, defaultResult);

    /// <summary>Shows a themed message box modally, centred on <paramref name="owner"/> when one
    /// is given, and returns the button chosen.</summary>
    public static MessageBoxResult Show(Window? owner, string message, string caption = "",
        MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None,
        MessageBoxResult defaultResult = MessageBoxResult.None)
    {
        var app = Application.Current;
        if (app is not null && !app.Dispatcher.CheckAccess())
            return app.Dispatcher.Invoke(() => Show(owner, message, caption, buttons, image, defaultResult));

        // A Window created before the app has one becomes its MainWindow, and closing the app's
        // only window ends it (OnLastWindowClose / OnMainWindowClose). A prompt shown during
        // startup must do neither, so both are held for the box's lifetime.
        var mainWindow = app?.CheckAccess() == true ? app.MainWindow : null;
        var shutdownMode = app?.CheckAccess() == true ? app.ShutdownMode : (ShutdownMode?)null;
        if (shutdownMode is not null) app!.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        try
        {
            var box = new MaMessageBox(message, caption, buttons, image, defaultResult);
            if (owner is not null)
            {
                box.Owner = owner;
                box.ShowInTaskbar = false;
                box.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            if (shutdownMode is not null && ReferenceEquals(app!.MainWindow, box)) app.MainWindow = mainWindow;
            Sound(image)?.Play();
            box.ShowDialog();
            return box.Result;
        }
        finally
        {
            if (shutdownMode is not null)
            {
                if (app!.MainWindow is MaMessageBox) app.MainWindow = mainWindow;
                app.ShutdownMode = shutdownMode.Value;
            }
        }
    }

    private static string Label(MessageBoxResult result) => result switch
    {
        MessageBoxResult.Yes => "_Yes",
        MessageBoxResult.No => "_No",
        MessageBoxResult.Cancel => "Cancel",
        _ => "OK",
    };

    // MessageBoxImage has aliases (Asterisk = Information, Exclamation = Warning, Hand = Stop =
    // Error), so the spoken name is spelled out rather than taken from Enum.ToString().
    private static (string Glyph, string Brush, string Name)? Glyph(MessageBoxImage image) => image switch
    {
        MessageBoxImage.Information => ("", "MaFocus", "Information"),
        MessageBoxImage.Warning => ("", "MaWarn", "Warning"),
        MessageBoxImage.Error => ("", "MaBad", "Error"),
        MessageBoxImage.Question => ("", "MaFocus", "Question"),
        _ => null,
    };

    private static SystemSound? Sound(MessageBoxImage image) => image switch
    {
        MessageBoxImage.Information => SystemSounds.Asterisk,
        MessageBoxImage.Warning => SystemSounds.Exclamation,
        MessageBoxImage.Error => SystemSounds.Hand,
        MessageBoxImage.Question => SystemSounds.Question,
        _ => null,
    };
}
