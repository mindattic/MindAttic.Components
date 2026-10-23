using System.IO;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MindAttic.Components.Wpf.Controls;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T53: the light palette, the live switch between it and the dark default, high contrast still
/// winning, and the MaThemeToggle sun/moon button.</summary>
public sealed class MaThemeTests
{
    // ---- the palettes ----

    private static string Source(string relative)
    {
        var root = AppContext.BaseDirectory;
        while (!Path.Exists(Path.Combine(root, ".git")))
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("no repo root above the tests");
        return File.ReadAllText(Path.Combine(root, "dotnet", "src", "MindAttic.Components.Wpf", relative));
    }

    private static List<string> BrushKeys(string xaml) =>
        Regex.Matches(xaml, @"<SolidColorBrush x:Key=""(\w+)""").Select(m => m.Groups[1].Value).ToList();

    [Fact]
    public void The_light_palette_has_every_dark_key_and_no_other_and_the_theme_has_no_hard_coded_colours()
    {
        var theme = Source("Theme.xaml");
        var dark = BrushKeys(theme);
        var light = BrushKeys(Source(Path.Combine("Themes", "Light.xaml")));

        Assert.Equal(dark.Order(), light.Order());
        Assert.Contains("MaChartGrid", light);
        // Outside the palette's own brushes, every colour in a style is a palette key, so it follows the switch.
        var styles = Regex.Replace(theme, @"<SolidColorBrush x:Key=""\w+"" Color=""#[0-9A-Fa-f]+"" />", "");
        styles = Regex.Replace(styles, "<!--.*?-->", "", RegexOptions.Singleline);
        Assert.DoesNotMatch(@"""#[0-9A-Fa-f]{6,8}""", styles);
        Assert.Empty(MaHighContrast.Keys.Except(light));
    }

    [Theory]
    [InlineData(MaThemeMode.Dark)]
    [InlineData(MaThemeMode.Light)]
    public void Both_palettes_meet_the_same_contrast_standard(MaThemeMode mode) => Sta(() =>
    {
        var window = OffScreenWindow.Create();
        try
        {
            MaTheme.Set(window.Resources, mode);
            Assert.Equal(mode, MaTheme.Get(window));
            foreach (var surface in new[] { "MaChrome", "MaSurface" })
            {
                foreach (var text in new[] { "MaText", "MaMuted", "MaRec", "MaBad", "MaLink" })
                    Assert.True(Contrast(window, text, surface) >= 4.5, $"{mode}: {text} on {surface} is {Contrast(window, text, surface):0.00}");
                foreach (var mark in new[] { "MaFieldBorder", "MaFocus", "MaGood", "MaWarn", "MaOther", "MaStar" })
                    Assert.True(Contrast(window, mark, surface) >= 3, $"{mode}: {mark} on {surface} is {Contrast(window, mark, surface):0.00}");
            }
            Assert.True(Contrast(window, "MaControlBorder", "MaChrome") >= 3, $"{mode}: MaControlBorder");
            Assert.True(Contrast(window, "MaChartGrid", "MaChrome") >= 3, $"{mode}: MaChartGrid");
            foreach (var fill in new[] { "MaAccent", "MaAccentHover", "MaTileHover" })
                Assert.True(Contrast(window, "MaOnAccent", fill) >= 4.5, $"{mode}: MaOnAccent on {fill}");
        }
        finally { window.Close(); }
    });

    // ---- switching ----

    [Fact]
    public void Dark_is_the_default_and_a_switch_restyles_controls_live_and_back() => Sta(() =>
    {
        var box = new TextBox();
        var tabs = new MaTabControl { Items = { new MaTabItem { Header = "One" } } };
        var light = new MaStatusLight { Status = MaStatus.Good };
        var chart = new MaLineChart { Width = 200, Height = 100 };
        var window = OffScreenWindow.Create(content: new StackPanel { Children = { box, tabs, light, chart } });
        try
        {
            box.Style = (Style)window.FindResource("MaTextBox");
            OffScreenWindow.Show(window);
            Pump();
            Assert.Equal(MaThemeMode.Dark, MaTheme.Get(window.Resources));
            Assert.Equal(MaThemeMode.Dark, MaTheme.Get(box));
            var darkText = ((SolidColorBrush)box.Foreground).Color;
            var darkGood = ((SolidColorBrush)light.Foreground).Color;

            var changes = new List<MaThemeChangedEventArgs>();
            EventHandler<MaThemeChangedEventArgs> onChanged = (_, e) => { if (e.Resources == window.Resources) changes.Add(e); };
            MaTheme.Changed += onChanged;
            try
            {
                MaTheme.Set(window.Resources, MaThemeMode.Light);
                MaTheme.Set(window.Resources, MaThemeMode.Light); // already light: nothing
                Pump();
                Assert.Equal(MaThemeMode.Light, MaTheme.Get(window.Resources));
                Assert.Equal(MaThemeMode.Light, MaTheme.Get(box));
                Assert.Equal(Color.FromRgb(0x1E, 0x1F, 0x24), ((SolidColorBrush)box.Foreground).Color);
                Assert.Equal(Color.FromRgb(0x1B, 0x7A, 0x43), ((SolidColorBrush)light.Foreground).Color);
                Assert.Same(window.FindResource("MaGood"), light.Foreground);
                Assert.Single(changes);
                Assert.Equal(MaThemeMode.Light, changes[0].Theme);

                MaTheme.Set(window.Resources, MaThemeMode.Dark);
                Pump();
                Assert.Equal(darkText, ((SolidColorBrush)box.Foreground).Color);
                Assert.Equal(darkGood, ((SolidColorBrush)light.Foreground).Color);
                Assert.Equal(2, changes.Count);
                Assert.Single(window.Resources.MergedDictionaries); // the light palette is gone, Theme.xaml stays
            }
            finally { MaTheme.Changed -= onChanged; }
        }
        finally { window.Close(); }
    });

    [Fact]
    public void The_light_palette_goes_just_after_the_theme_so_an_app_dictionary_merged_later_still_wins() => Sta(() =>
    {
        var window = OffScreenWindow.Create();
        try
        {
            var brand = new SolidColorBrush(Colors.OrangeRed);
            window.Resources.MergedDictionaries.Add(new ResourceDictionary { ["MaAccent"] = brand });
            MaTheme.Set(window.Resources, MaThemeMode.Light);

            Assert.Equal(3, window.Resources.MergedDictionaries.Count);
            Assert.EndsWith("Themes/Light.xaml", window.Resources.MergedDictionaries[1].Source.OriginalString);
            Assert.Same(brand, window.FindResource("MaAccent"));
            Assert.Equal(Color.FromRgb(0xF3, 0xF4, 0xF7), ((SolidColorBrush)window.FindResource("MaChrome")).Color);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void High_contrast_wins_over_the_light_theme_and_the_light_theme_returns_after_it() => Sta(() =>
    {
        var box = new TextBox();
        var window = OffScreenWindow.Create(content: box);
        try
        {
            box.Style = (Style)window.FindResource("MaTextBox");
            OffScreenWindow.Show(window);
            MaTheme.Set(window.Resources, MaThemeMode.Light);
            Pump();
            var lightBorder = box.BorderBrush;

            MaHighContrast.Apply(window.Resources, on: true);
            MaTheme.Set(window.Resources, MaThemeMode.Dark);
            MaTheme.Set(window.Resources, MaThemeMode.Light);
            Pump();
            Assert.Same(SystemColors.WindowTextBrush, box.BorderBrush);
            Assert.Same(SystemColors.WindowBrush, window.FindResource("MaChrome"));

            MaHighContrast.Apply(window.Resources, on: false);
            Pump();
            Assert.Equal(((SolidColorBrush)lightBorder).Color, ((SolidColorBrush)box.BorderBrush).Color);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void A_saved_setting_parses_to_a_theme_with_dark_as_the_fallback()
    {
        Assert.Equal(MaThemeMode.Light, MaTheme.Parse("light"));
        Assert.Equal(MaThemeMode.Light, MaTheme.Parse(" Light "));
        Assert.Equal(MaThemeMode.Dark, MaTheme.Parse("Dark"));
        Assert.Equal(MaThemeMode.Dark, MaTheme.Parse(null));
        Assert.Equal(MaThemeMode.Dark, MaTheme.Parse("sepia"));
        Assert.Equal(MaThemeMode.Dark, MaTheme.Parse("7"));
        Assert.Equal(MaThemeMode.Light, MaTheme.Opposite(MaThemeMode.Dark));
    }

    [Fact]
    public void A_themed_title_bar_follows_the_theme() => Sta(() =>
    {
        var window = OffScreenWindow.Create();
        try
        {
            MaWindow.SetThemedTitleBar(window, true);
            Assert.True(MaWindow.GetDarkTitleBar(window));
            MaTheme.Set(window.Resources, MaThemeMode.Light);
            Pump();
            Assert.False(MaWindow.GetDarkTitleBar(window));
            MaTheme.Set(window.Resources, MaThemeMode.Dark);
            Pump();
            Assert.True(MaWindow.GetDarkTitleBar(window));
        }
        finally { window.Close(); }
    });

    // ---- MaThemeToggle ----

    [Fact]
    public void The_toggle_shows_a_sun_in_dark_mode_and_a_moon_in_light_mode_and_is_named_for_the_theme_it_switches_to() => Sta(() =>
    {
        var toggle = new MaThemeToggle();
        var window = OffScreenWindow.Show(content: toggle);
        try
        {
            Pump();
            Assert.Equal(MaThemeMode.Dark, toggle.Theme);
            Assert.Equal(MaThemeMode.Light, toggle.TargetTheme);
            Assert.Equal(MaThemeToggle.SunGlyph, toggle.Glyph);
            Assert.Equal(MaThemeToggle.SunGlyph, toggle.Content);
            Assert.Equal("Switch to light theme", toggle.ToolTip);
            Assert.True(toggle.Focusable);
            Assert.True(toggle.IsTabStop);
            var peer = UIElementAutomationPeer.CreatePeerForElement(toggle);
            Assert.Equal("Switch to light theme", peer.GetName());
            Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
            Assert.Equal(nameof(MaThemeToggle), peer.GetClassName());

            MaTheme.Set(window.Resources, MaThemeMode.Light);
            Pump();
            Assert.Equal(MaThemeMode.Light, toggle.Theme);
            Assert.Equal(MaThemeToggle.MoonGlyph, toggle.Glyph);
            Assert.Equal("Switch to dark theme", peer.GetName());
            Assert.Equal("Switch to dark theme", toggle.ToolTip);

            AutomationProperties.SetName(toggle, "Theme");
            Assert.Equal("Theme", peer.GetName());
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Invoking_the_toggle_switches_the_theme_raises_ThemeChanged_and_runs_its_command() => Sta(() =>
    {
        var toggle = new MaThemeToggle();
        var label = new TextBlock();
        var host = new StackPanel { Children = { toggle, label } };
        var window = OffScreenWindow.Show(content: host);
        try
        {
            label.SetResourceReference(TextBlock.ForegroundProperty, "MaText");
            var seen = new List<MaThemeMode>();
            host.AddHandler(MaThemeToggle.ThemeChangedEvent, new EventHandler<MaThemeChangedEventArgs>((_, e) => seen.Add(e.Theme)));
            var ran = 0;
            toggle.Command = new Mvvm.RelayCommand(() => ran++);
            Pump();

            var invoke = (IInvokeProvider)UIElementAutomationPeer.CreatePeerForElement(toggle).GetPattern(PatternInterface.Invoke);
            invoke.Invoke();
            Pump();
            Assert.Equal(MaThemeMode.Light, MaTheme.Get(window.Resources)); // the window's dictionary carries the theme
            Assert.Equal(MaThemeMode.Light, toggle.Theme);
            Assert.Equal(Color.FromRgb(0x1E, 0x1F, 0x24), ((SolidColorBrush)label.Foreground).Color);
            Assert.Equal([MaThemeMode.Light], seen);
            Assert.Equal(1, ran);

            invoke.Invoke();
            Pump();
            Assert.Equal(MaThemeMode.Dark, MaTheme.Get(window.Resources));
            Assert.Equal(MaThemeToggle.SunGlyph, toggle.Glyph);
            Assert.Equal([MaThemeMode.Light, MaThemeMode.Dark], seen);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void The_toggle_answers_the_keyboard() => Sta(() =>
    {
        var toggle = new MaThemeToggle();
        var window = OffScreenWindow.Show(content: toggle);
        try
        {
            Pump();
            toggle.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(toggle), 0, Key.Enter)
            {
                RoutedEvent = Keyboard.KeyDownEvent,
            });
            Pump();
            Assert.Equal(MaThemeMode.Light, toggle.Theme);
            Assert.Equal(MaThemeMode.Light, MaTheme.Get(window.Resources));
        }
        finally { window.Close(); }
    });

    // ---- harness ----

    private static double Contrast(FrameworkElement scope, string a, string b)
    {
        static double Luminance(Color c)
        {
            static double Channel(byte v) { var s = v / 255.0; return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4); }
            return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
        }
        var (la, lb) = (Luminance(((SolidColorBrush)scope.FindResource(a)).Color), Luminance(((SolidColorBrush)scope.FindResource(b)).Color));
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ContextIdle, () => { });

    private static void Sta(Action test)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try { test(); }
            catch (Exception ex) { failure = ExceptionDispatchInfo.Capture(ex); }
            finally { Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}
