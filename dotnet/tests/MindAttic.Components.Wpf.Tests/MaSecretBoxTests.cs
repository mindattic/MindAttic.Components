using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using MindAttic.Components.Wpf.Controls;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>M.COMP-T155: MaSecretBox keeps a secret masked, reveals it only on a click and briefly, keeps it out of the
/// automation tree, and copies it out of clipboard history and clears it after.</summary>
public sealed class MaSecretBoxTests
{
    private const string Canary = "sk-canary-7f3a9d1e";

    private sealed class FakeClipboard : IMaSecretClipboard
    {
        public string? Text;
        public long Stamp { get; private set; } = 1;
        public int Clears;
        public long Put(string secret) { Text = secret; return ++Stamp; }
        public void Clear() { Text = null; Stamp++; Clears++; }
        public void SomethingElseCopied() { Text = "other"; Stamp++; }
    }

    private static (MaSecretBox Box, FakeClipboard Clipboard) Make(StaHost h, string? secret = Canary)
    {
        var clipboard = new FakeClipboard();
        var box = h.Show(new MaSecretBox { Clipboard = clipboard, Width = 300 });
        AutomationProperties.SetName(box, "Claude key");
        box.SetSecret(secret);
        h.Pump();
        return (box, clipboard);
    }

    private static T Part<T>(MaSecretBox box, string name) where T : DependencyObject =>
        (T)LogicalTreeHelper.FindLogicalNode(box, name);

    [Fact]
    public void It_is_masked_by_default() => StaHost.Run(h =>
    {
        var (box, _) = Make(h);
        var masked = Part<PasswordBox>(box, MaSecretBox.PartMasked);
        var revealed = Part<TextBox>(box, MaSecretBox.PartRevealed);

        Assert.False(box.IsRevealed);
        Assert.True(box.HasValue);
        Assert.Equal(Visibility.Visible, masked.Visibility);
        Assert.Equal(Visibility.Collapsed, revealed.Visibility);
        Assert.Equal("", revealed.Text);
        Assert.Equal(Canary, box.GetSecret());
        Assert.Same(h.Window.FindResource("MaPasswordBox"), masked.Style);
    });

    [Fact]
    public void Reveal_takes_a_click_and_a_second_click_masks_it_and_empties_the_plain_buffer() => StaHost.Run(h =>
    {
        var (box, _) = Make(h);
        var reveal = Part<Button>(box, MaSecretBox.PartReveal);
        var revealed = Part<TextBox>(box, MaSecretBox.PartRevealed);
        var masked = Part<PasswordBox>(box, MaSecretBox.PartMasked);

        h.Click(reveal);
        Assert.True(box.IsRevealed);
        Assert.Equal(Canary, revealed.Text);
        Assert.Equal("", masked.Password);
        Assert.Equal("Hide secret", AutomationProperties.GetName(reveal));

        h.Click(reveal);
        Assert.False(box.IsRevealed);
        Assert.Equal("", revealed.Text);
        Assert.Equal(Canary, box.GetSecret());
        Assert.Equal("Show secret", AutomationProperties.GetName(reveal));
    });

    [Fact]
    public void A_reveal_hides_itself_after_the_timeout() => StaHost.Run(h =>
    {
        var (box, _) = Make(h);
        Assert.Equal(TimeSpan.FromSeconds(30), box.RevealTimeout);
        box.RevealTimeout = TimeSpan.FromMilliseconds(80);

        h.Click(Part<Button>(box, MaSecretBox.PartReveal));
        Assert.True(box.IsRevealed);
        Assert.True(h.PumpUntil(() => !box.IsRevealed));
        Assert.Equal("", Part<TextBox>(box, MaSecretBox.PartRevealed).Text);
        Assert.Equal(Canary, box.GetSecret());
    });

    [Fact]
    public void A_reveal_hides_when_focus_leaves_the_field() => StaHost.Run(h =>
    {
        var (box, _) = Make(h);
        var other = h.Show(new TextBox());
        var masked = Part<PasswordBox>(box, MaSecretBox.PartMasked);
        Assert.True(masked.Focus());

        box.Reveal();
        h.Pump();
        var revealed = Part<TextBox>(box, MaSecretBox.PartRevealed);
        Assert.True(revealed.IsKeyboardFocused);

        Assert.True(other.Focus());
        h.Pump();
        Assert.False(box.IsRevealed);
        Assert.Equal("", revealed.Text);
    });

    [Fact]
    public void A_reveal_hides_when_the_window_deactivates() => StaHost.Run(h =>
    {
        var (box, _) = Make(h);
        box.Reveal();
        Assert.True(box.IsRevealed);

        typeof(Window).GetMethod("OnDeactivated", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(h.Window, [EventArgs.Empty]);
        h.Pump();
        Assert.False(box.IsRevealed);
    });

    [Fact]
    public void Nothing_in_the_automation_tree_holds_the_value_masked_or_revealed() => StaHost.Run(h =>
    {
        var (box, _) = Make(h);
        var peer = UIElementAutomationPeer.CreatePeerForElement(box);
        Assert.Equal("Claude key", peer.GetName());
        Assert.Equal("Set", peer.GetItemStatus());
        Assert.True(peer.IsPassword());
        Assert.Equal(AutomationControlType.Edit, peer.GetAutomationControlType());

        AssertNoCanary(peer);
        box.Reveal();
        h.Pump();
        peer.ResetChildrenCache();
        AssertNoCanary(peer);
        var revealedPeer = UIElementAutomationPeer.CreatePeerForElement(Part<TextBox>(box, MaSecretBox.PartRevealed));
        Assert.True(revealedPeer.IsPassword());
        Assert.Null(revealedPeer.GetPattern(PatternInterface.Value));
        Assert.Null(revealedPeer.GetPattern(PatternInterface.Text));

        box.Clear();
        Assert.Equal("Not set", peer.GetItemStatus());
    });

    private static void AssertNoCanary(AutomationPeer peer)
    {
        Assert.DoesNotContain(Canary, peer.GetName() ?? "");
        Assert.DoesNotContain(Canary, peer.GetItemStatus() ?? "");
        Assert.DoesNotContain(Canary, peer.GetHelpText() ?? "");
        Assert.DoesNotContain(Canary, peer.GetAcceleratorKey() ?? "");
        if (peer.GetPattern(PatternInterface.Value) is IValueProvider value)
        {
            string? text = null;
            try { text = value.Value; } catch (InvalidOperationException) { }
            Assert.DoesNotContain(Canary, text ?? "");
        }
        if (peer.GetPattern(PatternInterface.Text) is ITextProvider textProvider)
        {
            string? text = null;
            try { text = textProvider.DocumentRange.GetText(-1); } catch (InvalidOperationException) { }
            Assert.DoesNotContain(Canary, text ?? "");
        }
        foreach (var child in peer.GetChildren() ?? []) AssertNoCanary(child);
    }

    [Fact]
    public void No_property_of_the_control_exposes_the_value() => StaHost.Run(h =>
    {
        var (box, _) = Make(h);
        foreach (var revealed in new[] { false, true })
        {
            if (revealed) box.Reveal();
            foreach (var prop in typeof(MaSecretBox).GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetIndexParameters().Length == 0))
                Assert.NotEqual(Canary, prop.GetValue(box) as string);
            var locals = box.GetLocalValueEnumerator();
            while (locals.MoveNext()) Assert.NotEqual(Canary, locals.Current.Value as string);
        }
        Assert.DoesNotContain(typeof(MaSecretBox).GetFields(BindingFlags.Static | BindingFlags.Public),
            f => f.GetValue(null) is DependencyProperty dp && dp.PropertyType == typeof(string) && dp.Name is not nameof(MaSecretBox.Placeholder));
    });

    [Fact]
    public void SecretChanged_fires_for_user_edits_only_and_carries_no_value() => StaHost.Run(h =>
    {
        var (box, _) = Make(h, null);
        var args = new List<EventArgs>();
        box.SecretChanged += (_, e) => args.Add(e);

        box.SetSecret("loaded");
        Assert.Empty(args);

        Part<PasswordBox>(box, MaSecretBox.PartMasked).Password = "typed";
        Assert.Single(args);
        Assert.Same(EventArgs.Empty, args[0]);
        Assert.Equal("typed", box.GetSecret());

        box.Reveal();
        Assert.Single(args);
        Part<TextBox>(box, MaSecretBox.PartRevealed).Text = "typed2";
        Assert.Equal(2, args.Count);
        box.Hide();
        Assert.Equal(2, args.Count);
        Assert.Equal("typed2", box.GetSecret());
    });

    [Fact]
    public void Copy_puts_it_on_the_clipboard_and_clears_it_after_the_timeout() => StaHost.Run(h =>
    {
        var (box, clipboard) = Make(h);
        Assert.Equal(TimeSpan.FromSeconds(30), box.ClipboardTimeout);
        box.ClipboardTimeout = TimeSpan.FromMilliseconds(80);

        h.Click(Part<Button>(box, MaSecretBox.PartCopy));
        Assert.Equal(Canary, clipboard.Text);
        Assert.True(h.PumpUntil(() => clipboard.Text is null));
        Assert.Equal(1, clipboard.Clears);
    });

    [Fact]
    public void Copy_is_not_cleared_when_something_else_was_copied_since() => StaHost.Run(h =>
    {
        var (box, clipboard) = Make(h);
        box.ClipboardTimeout = TimeSpan.FromMilliseconds(50);
        Assert.True(box.CopyToClipboard());
        clipboard.SomethingElseCopied();

        h.PumpUntil(() => false, TimeSpan.FromMilliseconds(250));
        Assert.Equal("other", clipboard.Text);
        Assert.Equal(0, clipboard.Clears);
    });

    [Fact]
    public void Copy_is_disabled_with_no_value() => StaHost.Run(h =>
    {
        var (box, clipboard) = Make(h, null);
        Assert.False(box.HasValue);
        Assert.False(Part<Button>(box, MaSecretBox.PartCopy).IsEnabled);
        Assert.False(box.CopyToClipboard());
        Assert.Null(clipboard.Text);
        Assert.Equal(Visibility.Visible, Part<TextBlock>(box, MaSecretBox.PartPlaceholder).Visibility);
    });

    [Fact]
    public void Ctrl_C_in_the_revealed_text_goes_through_the_safe_copy_and_cut_is_blocked() => StaHost.Run(h =>
    {
        var (box, clipboard) = Make(h);
        box.Reveal();
        var revealed = Part<TextBox>(box, MaSecretBox.PartRevealed);
        revealed.SelectAll();

        ApplicationCommands.Copy.Execute(null, revealed);
        Assert.Equal(Canary, clipboard.Text);
        Assert.False(ApplicationCommands.Cut.CanExecute(null, revealed));
        Assert.False(revealed.IsUndoEnabled);
    });

    [Fact]
    public void A_copy_carries_the_formats_that_keep_it_out_of_clipboard_history_and_cloud_sync() => StaHost.Run(_ =>
    {
        var data = MaSecretClipboard.CreateDataObject(Canary);
        Assert.Equal(Canary, data.GetData(DataFormats.UnicodeText));
        Assert.True(data.GetDataPresent(MaSecretClipboard.ExcludeFromMonitorProcessingFormat));
        foreach (var format in new[] { MaSecretClipboard.CanIncludeInClipboardHistoryFormat, MaSecretClipboard.CanUploadToCloudClipboardFormat })
        {
            var stream = Assert.IsType<MemoryStream>(data.GetData(format));
            Assert.Equal(new byte[] { 0, 0, 0, 0 }, stream.ToArray());
        }
        Assert.Equal("ExcludeClipboardContentFromMonitorProcessing", MaSecretClipboard.ExcludeFromMonitorProcessingFormat);
        Assert.Equal("CanIncludeInClipboardHistory", MaSecretClipboard.CanIncludeInClipboardHistoryFormat);
        Assert.Equal("CanUploadToCloudClipboard", MaSecretClipboard.CanUploadToCloudClipboardFormat);
    });

    [Fact]
    public void Unloading_masks_it() => StaHost.Run(h =>
    {
        var (box, _) = Make(h);
        box.Reveal();
        ((Panel)box.Parent).Children.Remove(box);
        h.Pump();
        Assert.False(box.IsRevealed);
    });
}
