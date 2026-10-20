using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MindAttic.Components.Wpf.Controls;
using MindAttic.Components.Wpf.Mvvm;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T11: the pieces the Orchestrator and Workflow Desktop each kept a copy of: the Mvvm helpers, zoom,
/// persistent tooltips, the WCAG AA palette fixes and the high-contrast brush swap.</summary>
public sealed class SharedPiecesTests
{
    // ---- Mvvm ----

    private sealed class Person : ObservableObject
    {
        private string name = "";
        public string Name { get => name; set { if (Set(ref name, value)) Raise(nameof(Greeting)); } }
        public string Greeting => $"Hi {Name}";
    }

    [Fact]
    public void Set_raises_property_changed_only_when_the_value_changes()
    {
        var person = new Person();
        var raised = new List<string?>();
        ((INotifyPropertyChanged)person).PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        person.Name = "Ada";
        person.Name = "Ada";

        Assert.Equal(["Name", "Greeting"], raised);
    }

    [Fact]
    public void A_relay_command_runs_its_delegate_and_asks_its_predicate()
    {
        var ran = 0;
        var allowed = false;
        var command = new RelayCommand(() => ran++, () => allowed);
        Assert.False(command.CanExecute(null));
        allowed = true;
        Assert.True(command.CanExecute(null));
        command.Execute(null);

        object? seen = null;
        var withParameter = new RelayCommand(p => seen = p);
        Assert.True(withParameter.CanExecute("x"));
        withParameter.Execute("x");

        Assert.Equal(1, ran);
        Assert.Equal("x", seen);
    }

    private sealed class Row(string key) { public string Key { get; } = key; public int Value { get; set; } }

    [Fact]
    public void Reorder_moves_the_same_instances_rather_than_re_adding_them()
    {
        Row a = new("a"), b = new("b"), c = new("c"), d = new("d");
        var target = new ObservableCollection<Row> { a, b, c };
        var actions = new List<string>();
        target.CollectionChanged += (_, e) => actions.Add(e.Action.ToString());

        target.ReorderTo([c, a, d]);

        Assert.Equal([c, a, d], target);
        Assert.DoesNotContain("Reset", actions);
        Assert.Contains("Move", actions);
    }

    [Fact]
    public void Sync_by_key_reuses_surviving_rows_creates_new_ones_drops_the_rest_and_keeps_a_repeated_key_once()
    {
        Row a = new("a"), b = new("b");
        var target = new ObservableCollection<Row> { a, b };

        target.SyncBy([("c", 3), ("a", 1), ("c", 9)], s => s.Item1, r => r.Key,
            s => new Row(s.Item1) { Value = s.Item2 }, (r, s) => r.Value = s.Item2);

        Assert.Equal(["c", "a"], target.Select(r => r.Key));
        Assert.Same(a, target[1]);
        Assert.Equal(1, a.Value);
        Assert.Equal(3, target[0].Value);
    }

    // ---- zoom ----

    [Fact]
    public void Zoom_steps_by_ten_percent_clamps_to_80_to_200_and_reports_only_when_zoomed()
    {
        var saved = new List<double>();
        var zoom = new ZoomLevel(1.3, saved.Add);
        Assert.Equal(1.3, zoom.Scale);
        Assert.Equal("130%", zoom.Text);

        zoom.ResetCommand.Execute(null);
        Assert.Equal("", zoom.Text);
        for (var i = 0; i < 20; i++) zoom.InCommand.Execute(null);
        Assert.Equal(ZoomLevel.Max, zoom.Scale);
        for (var i = 0; i < 20; i++) zoom.OutCommand.Execute(null);
        Assert.Equal(ZoomLevel.Min, zoom.Scale);
        zoom.InCommand.Execute(null);
        Assert.Equal(0.9, zoom.Scale); // rounded, no floating-point drift

        Assert.Equal(1.0, saved[0]); // the initial level isn't a change
        Assert.Equal(0.9, saved[^1]);
        Assert.Equal(saved.Count, saved.Zip(saved.Skip(1)).Count(p => p.First != p.Second) + 1); // only real changes
        Assert.Equal(1, new ZoomLevel(double.NaN).Scale);
        Assert.Equal(ZoomLevel.Max, new ZoomLevel(5).Scale);
    }

    [Fact]
    public void Attach_scales_the_content_and_binds_the_zoom_keys_to_the_window() => Sta(() =>
    {
        var content = new Border { Width = 100, Height = 50 };
        var window = OffScreenWindow.Show(400, 400, content);
        try
        {
            var zoom = new ZoomLevel();
            zoom.Attach(content, window);
            zoom.Scale = 1.5;
            Pump();

            var transform = Assert.IsType<ScaleTransform>(content.LayoutTransform);
            Assert.Equal(1.5, transform.ScaleX);
            Assert.Equal(1.5, transform.ScaleY);
            var keys = window.InputBindings.OfType<KeyBinding>().ToList();
            Assert.Equal(6, keys.Count);
            Assert.All(keys, k => Assert.Equal(ModifierKeys.Control, k.Modifiers));
            Assert.Equal([Key.OemPlus, Key.Add], keys.Where(k => k.Command == zoom.InCommand).Select(k => k.Key));
            Assert.Equal([Key.OemMinus, Key.Subtract], keys.Where(k => k.Command == zoom.OutCommand).Select(k => k.Key));
            Assert.Equal([Key.D0, Key.NumPad0], keys.Where(k => k.Command == zoom.ResetCommand).Select(k => k.Key));
        }
        finally { window.Close(); }
    });

    // ---- tooltips ----

    [Fact]
    public void Tooltips_stay_open_until_dismissed_once_kept_open() => Sta(() =>
    {
        McToolTips.KeepOpen();
        McToolTips.KeepOpen(); // a second call is harmless

        Assert.Equal(int.MaxValue, ToolTipService.GetShowDuration(new Button()));
        Assert.Equal(int.MaxValue, ToolTipService.GetShowDuration(new TextBlock()));
        var own = new Button();
        ToolTipService.SetShowDuration(own, 3000);
        Assert.Equal(3000, ToolTipService.GetShowDuration(own));
    });

    // ---- WCAG AA palette ----

    [Fact]
    public void A_text_fields_outline_is_3_to_1_and_red_text_is_4_5_to_1_on_every_surface() => Sta(() =>
    {
        var box = new TextBox();
        var window = OffScreenWindow.Create(content: box);
        try
        {
            box.Style = (Style)window.FindResource("McTextBox");
            OffScreenWindow.Show(window);
            Pump();

            Assert.Same(window.FindResource("McFieldBorder"), box.BorderBrush);
            foreach (var surface in new[] { "McChrome", "McSurface" })
            {
                Assert.True(Contrast(window, "McFieldBorder", surface) >= 3, $"McFieldBorder on {surface}");
                Assert.True(Contrast(window, "McRec", surface) >= 4.5, $"McRec on {surface}");
            }
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Every_style_references_the_palette_dynamically_so_an_app_or_high_contrast_can_re_key_it()
    {
        var root = AppContext.BaseDirectory;
        while (!Path.Exists(Path.Combine(root, ".git")))
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("no repo root above the tests");
        var theme = File.ReadAllText(Path.Combine(root, "dotnet", "src", "MindAttic.Components.Wpf", "Theme.xaml"));
        var brushes = Regex.Matches(theme, @"<SolidColorBrush x:Key=""(\w+)""").Select(m => m.Groups[1].Value).ToList();

        Assert.Contains("McFieldBorder", brushes);
        Assert.Contains("McOnAccent", brushes);
        Assert.DoesNotContain(brushes, b => theme.Contains("{StaticResource " + b + "}"));
        Assert.Empty(McHighContrast.Keys.Except(brushes));
    }

    // ---- high contrast ----

    [Fact]
    public void High_contrast_swaps_the_system_colours_into_library_controls_live_and_puts_the_app_overrides_back() => Sta(() =>
    {
        var box = new TextBox();
        var toggle = new ToggleButton { Content = "On", IsChecked = true };
        var window = OffScreenWindow.Create(content: new StackPanel { Children = { box, toggle } });
        try
        {
            box.Style = (Style)window.FindResource("McTextBox");
            toggle.Style = (Style)window.FindResource("McPillToggle");
            var appRed = new SolidColorBrush(Colors.OrangeRed);
            window.Resources["McRec"] = appRed; // an app's own override of a theme key
            OffScreenWindow.Show(window);
            Pump();
            var themeBorder = box.BorderBrush;

            McHighContrast.Apply(window.Resources, on: true, foregrounds: ["AppState"]);
            Pump();
            Assert.Same(SystemColors.WindowTextBrush, box.BorderBrush);
            Assert.Same(SystemColors.HighlightBrush, Pill(toggle).Background);
            Assert.Same(SystemColors.HighlightTextBrush, toggle.Foreground);
            Assert.Same(SystemColors.WindowTextBrush, window.Resources["AppState"]);
            Assert.Same(SystemColors.WindowTextBrush, window.Resources["McRec"]);

            McHighContrast.Apply(window.Resources, on: false, foregrounds: ["AppState"]);
            Pump();
            Assert.Same(themeBorder, box.BorderBrush);
            Assert.Same(window.FindResource("McAccent"), Pill(toggle).Background);
            Assert.Same(appRed, window.Resources["McRec"]);
            Assert.DoesNotContain("AppState", window.Resources.Keys.Cast<object>());
            Assert.DoesNotContain("McText", window.Resources.Keys.Cast<object>()); // the theme's own is back in charge
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Forced_high_contrast_applies_without_following_the_system() => Sta(() =>
    {
        var window = OffScreenWindow.Create();
        try
        {
            using (McHighContrast.Track(window.Resources, window.Dispatcher, force: true))
                Assert.Same(SystemColors.WindowBrush, window.Resources["McChrome"]);
        }
        finally { window.Close(); }
    });

    // ---- harness ----

    private static Border Pill(Control control) => (Border)control.Template.FindName("Bd", control);

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
