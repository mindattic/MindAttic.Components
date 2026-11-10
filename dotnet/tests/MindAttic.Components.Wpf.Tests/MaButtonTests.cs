using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MindAttic.Components.Wpf.Controls;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T174: MaButton, the one button (buttons, chips, tabs, nav items, links, chrome), and MaShapeBorder.</summary>
public sealed class MaButtonTests
{
    private static readonly SolidColorBrush Bg = Frozen(Colors.DarkSlateGray);
    private static readonly SolidColorBrush Fg = Frozen(Colors.White);
    private static readonly SolidColorBrush CheckedBg = Frozen(Colors.Orange);
    private static readonly SolidColorBrush CheckedFg = Frozen(Colors.Black);
    private static readonly SolidColorBrush Accent = Frozen(Colors.Red);

    [Fact]
    public void Resting_state_uses_the_plain_brushes() => StaHost.Run(h =>
    {
        var b = h.Show(Make());
        Assert.Same(Bg, b.EffectiveBackground);
        Assert.Same(Fg, b.EffectiveForeground);
        Assert.Same(Fg, b.Foreground);
        Assert.Equal(1.0, b.Opacity);
    });

    [Fact]
    public void A_plain_button_clicks_and_runs_its_command_but_never_checks() => StaHost.Run(h =>
    {
        object? ran = null;
        var b = h.Show(Make());
        b.Command = new RelayCommand(p => ran = p);
        b.CommandParameter = "hello";
        var clicks = 0;
        b.Click += (_, _) => clicks++;

        Click(b, h);
        Click(b, h);

        Assert.Equal(2, clicks);
        Assert.Equal("hello", ran);
        Assert.NotEqual(true, b.IsChecked);
    });

    [Fact]
    public void A_toggle_checks_and_unchecks_and_wears_the_checked_brushes() => StaHost.Run(h =>
    {
        var b = h.Show(Make(toggle: true));
        Click(b, h);
        Assert.True(b.IsChecked);
        Assert.Same(CheckedBg, b.EffectiveBackground);
        Assert.Same(CheckedFg, b.EffectiveForeground);
        Assert.Same(CheckedFg, b.Foreground);

        Click(b, h);
        Assert.False(b.IsChecked);
        Assert.Same(Bg, b.EffectiveBackground);
        Assert.Same(Fg, b.Foreground);
    });

    [Fact]
    public void Content_text_inherits_the_state_colour() => StaHost.Run(h =>
    {
        var text = new TextBlock { Text = "MECHS" };
        var b = h.Show(Make(toggle: true));
        b.Content = text;
        h.Pump();
        Assert.Equal(Fg, text.Foreground);
        b.IsChecked = true;
        h.Pump();
        Assert.Equal(CheckedFg, text.Foreground);
    });

    [Fact]
    public void Disabled_fades_to_DisabledOpacity_and_back() => StaHost.Run(h =>
    {
        var b = h.Show(Make());
        b.DisabledOpacity = 0.38;
        b.IsEnabled = false;
        h.Pump();
        Assert.Equal(0.38, b.Opacity);
        b.IsEnabled = true;
        h.Pump();
        Assert.Equal(1.0, b.Opacity);
    });

    [Theory]
    [InlineData(MaAccentWhen.Always, false, true)]
    [InlineData(MaAccentWhen.Always, true, true)]
    [InlineData(MaAccentWhen.Checked, false, false)]
    [InlineData(MaAccentWhen.Checked, true, true)]
    [InlineData(MaAccentWhen.Never, true, false)]
    public void The_accent_follows_AccentWhen(MaAccentWhen when, bool isChecked, bool shows) => StaHost.Run(h =>
    {
        var b = h.Show(Make(toggle: true));
        b.AccentBrush = Accent;
        b.AccentEdge = MaAccentEdge.Left;
        b.AccentWhen = when;
        b.IsChecked = isChecked;
        h.Pump();
        Assert.Equal(shows ? Accent : null, b.EffectiveAccent);
    });

    [Fact]
    public void A_group_keeps_exactly_one_checked_and_a_click_on_the_checked_one_keeps_it() => StaHost.Run(h =>
    {
        var a = h.Show(Make(group: "Tabs"));
        var b = h.Show(Make(group: "Tabs"));
        Click(a, h);
        Assert.True(a.IsChecked);

        Click(b, h);
        Assert.True(b.IsChecked);
        Assert.False(a.IsChecked);

        Click(b, h);
        Assert.True(b.IsChecked);
    });

    [Fact]
    public void A_group_is_scoped_to_its_own_window() => StaHost.Run(h =>
    {
        var here = h.Show(Make(group: "Game"));
        var otherRoot = new StackPanel();
        var there = Make(group: "Game");
        otherRoot.Children.Add(there);
        var other = OffScreenWindow.Show(OffScreenWindow.Create(content: otherRoot));
        try
        {
            h.Pump();
            there.IsChecked = true;
            here.IsChecked = true;
            h.Pump();
            Assert.True(here.IsChecked);
            Assert.True(there.IsChecked);
        }
        finally
        {
            other.Close();
        }
    });

    [Fact]
    public void A_group_in_a_tree_not_yet_shown_leaves_shown_windows_alone() => StaHost.Run(h =>
    {
        var shown = h.Show(Make(group: "Nav"));
        shown.IsChecked = true;
        var detached = new StackPanel();
        var a = Make(group: "Nav");
        var b = Make(group: "Nav");
        detached.Children.Add(a);
        detached.Children.Add(b);

        a.IsChecked = true;
        b.IsChecked = true;

        Assert.True(shown.IsChecked);
        Assert.False(a.IsChecked);
        Assert.True(b.IsChecked);
    });

    [Fact]
    public void Groups_on_different_ui_threads_never_touch_each_other()
    {
        MaButton? first = null;
        StaHost.Run(h =>
        {
            first = h.Show(Make(group: "Shared"));
            first.IsChecked = true;
        });

        // A second UI thread checking a same-named group must not reach the first thread's button.
        StaHost.Run(h =>
        {
            var second = h.Show(Make(group: "Shared"));
            second.IsChecked = true;
            Assert.True(second.IsChecked);
        });

        GC.KeepAlive(first);
    }

    [Fact]
    public void IsDefault_and_IsCancel_register_Enter_and_Escape_like_a_stock_Button() => StaHost.Run(h =>
    {
        // Pressing the keys needs an activated window with keyboard focus, which test windows never are
        // (OffScreenWindow); so check the access-key registration both controls rely on, side by side.
        const string Enter = "\r";
        const string Escape = "\u001b";
        var ok = h.Show(Make());
        var source = PresentationSource.FromVisual(h.Window);
        var cancel = h.Show(Make());
        Assert.False(AccessKeyManager.IsKeyRegistered(source, Enter));
        Assert.False(AccessKeyManager.IsKeyRegistered(source, Escape));

        ok.IsDefault = true;
        cancel.IsCancel = true;
        Assert.True(AccessKeyManager.IsKeyRegistered(source, Enter));
        Assert.True(AccessKeyManager.IsKeyRegistered(source, Escape));

        ok.IsDefault = false;
        cancel.IsCancel = false;
        Assert.False(AccessKeyManager.IsKeyRegistered(source, Enter));
        Assert.False(AccessKeyManager.IsKeyRegistered(source, Escape));

        var stock = h.Show(new Button { IsDefault = true });
        Assert.True(AccessKeyManager.IsKeyRegistered(source, Enter), "the stock Button registers the same way");
        stock.IsDefault = false;
    });

    [Theory]
    [InlineData(MaShape.Rectangle)]
    [InlineData(MaShape.Rounded)]
    [InlineData(MaShape.Pill)]
    [InlineData(MaShape.Chamfer)]
    [InlineData(MaShape.Trapezoid)]
    [InlineData(MaShape.Parallelogram)]
    public void Every_shape_fills_its_middle(MaShape shape) => StaHost.Run(h =>
    {
        var b = h.Show(Make());
        b.Shape = shape;
        b.Width = 120;
        b.Height = 36;
        h.Pump();
        var pixels = Render(b, 120, 36);
        Assert.Equal(Bg.Color, pixels[18 * 120 + 60]);
    });

    [Theory]
    [InlineData(MaShape.Chamfer)]
    [InlineData(MaShape.Pill)]
    public void Cut_and_round_shapes_leave_the_corner_empty(MaShape shape) => StaHost.Run(h =>
    {
        var b = h.Show(Make());
        b.Shape = shape;
        b.Chamfer = 10;
        b.Width = 120;
        b.Height = 36;
        h.Pump();
        var pixels = Render(b, 120, 36);
        Assert.Equal(0, pixels[0].A);
    });

    [Fact]
    public void Plain_buttons_are_buttons_to_automation_and_toggles_also_toggle() => StaHost.Run(h =>
    {
        var plain = UIElementAutomationPeer.CreatePeerForElement(h.Show(Make()));
        var toggle = UIElementAutomationPeer.CreatePeerForElement(h.Show(Make(toggle: true)));
        var tab = UIElementAutomationPeer.CreatePeerForElement(h.Show(Make(group: "Peers")));
        Assert.NotNull(plain.GetPattern(PatternInterface.Invoke));
        Assert.Null(plain.GetPattern(PatternInterface.Toggle));
        Assert.NotNull(toggle.GetPattern(PatternInterface.Toggle));
        Assert.NotNull(tab.GetPattern(PatternInterface.Toggle));
        Assert.Equal(AutomationControlType.Button, plain.GetAutomationControlType());
    });

    [Fact]
    public void Shape_None_draws_nothing_but_still_takes_clicks() => StaHost.Run(h =>
    {
        var b = h.Show(Make());
        b.Shape = MaShape.None;
        b.Width = 120;
        b.Height = 36;
        h.Pump();
        var pixels = Render(b, 120, 36);
        Assert.All(pixels, p => Assert.Equal(0, p.A));
        Assert.Same(b, HitTest(h, b, new Point(60, 18)));
    });

    [Fact]
    public void The_shape_border_draws_its_accent_on_the_chosen_edge() => StaHost.Run(h =>
    {
        var border = h.Show(new MaShapeBorder
        {
            Shape = MaShape.Rectangle, Fill = Bg, AccentBrush = Accent, AccentEdge = MaAccentEdge.Left, AccentThickness = 4,
            Width = 100, Height = 30, HorizontalAlignment = HorizontalAlignment.Left,
        });
        h.Pump();
        var pixels = Render(border, 100, 30);
        Assert.Equal(Accent.Color, pixels[15 * 100 + 1]);
        Assert.Equal(Bg.Color, pixels[15 * 100 + 50]);
    });

    [Fact]
    public void MaModal_works_without_Theme_xaml_and_is_skinned_by_properties() => StaHost.Run(h =>
    {
        var root = new Grid();
        var window = OffScreenWindow.Create(500, 400, root);
        window.Resources.MergedDictionaries.Clear();
        var backdrop = Frozen(Color.FromArgb(0xC0, 0, 0, 0));
        var modal = new MaModal { Title = "Mech", Content = new TextBlock { Text = "picture" }, BackdropBrush = backdrop, CornerRadius = new CornerRadius(0) };
        root.Children.Add(modal);
        OffScreenWindow.Show(window);
        try
        {
            modal.IsOpen = true;
            h.Pump();
            modal.ApplyTemplate();
            var part = (Border)modal.Template.FindName(MaModal.PartBackdrop, modal);
            var card = (Border)modal.Template.FindName(MaModal.PartCard, modal);
            var close = (MaButton)modal.Template.FindName(MaModal.PartCloseButton, modal);
            Assert.Same(backdrop, part.Background);
            Assert.Equal(new CornerRadius(0), card.CornerRadius);

            ((IInvokeProvider)UIElementAutomationPeer.CreatePeerForElement(close).GetPattern(PatternInterface.Invoke)!).Invoke();
            h.Pump();
            Assert.False(modal.IsOpen);
        }
        finally
        {
            window.Close();
        }
    });

    private static MaButton Make(bool toggle = false, string? group = null) => new()
    {
        Background = Bg, Foreground = Fg, CheckedBackground = CheckedBg, CheckedForeground = CheckedFg,
        IsToggle = toggle, GroupName = group, Width = 80, Height = 30, HorizontalAlignment = HorizontalAlignment.Left,
    };

    private static void Click(MaButton button, StaHost h)
    {
        ((IInvokeProvider)UIElementAutomationPeer.CreatePeerForElement(button).GetPattern(PatternInterface.Invoke)!).Invoke();
        h.Pump();
    }

    private static object? HitTest(StaHost h, UIElement element, Point at)
    {
        var result = VisualTreeHelper.HitTest(element, at)?.VisualHit;
        for (var node = result as DependencyObject; node is not null; node = VisualTreeHelper.GetParent(node))
        {
            if (node is MaButton b) return b;
        }

        return null;
    }

    private static Color[] Render(FrameworkElement element, int width, int height)
    {
        // Through a VisualBrush, so the element's offset inside its parent doesn't shift the picture.
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(new VisualBrush(element) { Stretch = Stretch.None, AlignmentX = AlignmentX.Left, AlignmentY = AlignmentY.Top },
                null, new Rect(0, 0, width, height));
        }

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var raw = new byte[width * height * 4];
        bitmap.CopyPixels(raw, width * 4, 0);
        var colors = new Color[width * height];
        for (var i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.FromArgb(raw[i * 4 + 3], raw[i * 4 + 2], raw[i * 4 + 1], raw[i * 4]);
        }

        return colors;
    }

    private static SolidColorBrush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private sealed class RelayCommand(Action<object?> run) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => run(parameter);
    }
}
