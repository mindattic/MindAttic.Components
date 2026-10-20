using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace MindAttic.Components.Wpf.Tests;

/// <summary>No test pops a window onto the user's screen or takes their keyboard (M.COMP-T39): agents run these tests
/// every few minutes, and DropdownMenuTests showed its window at 0,0 and activated it, so every dotnet test run popped a
/// blank 'testhost' window and stole focus. Every window a test shows goes through <see cref="OffScreenWindow"/>; this
/// scans the test sources for any other way in.</summary>
public sealed class TestWindowHygieneTests
{
    private const string Helper = nameof(OffScreenWindow) + ".cs";

    /// <summary>Every WPF window type the tests could build: System.Windows.Window, its framework subclasses and any
    /// the library adds.</summary>
    private static readonly string[] WindowTypes =
        new[] { typeof(System.Windows.Window).Assembly, typeof(MindAttic.Components.Wpf.Controls.McModal).Assembly }
            .SelectMany(a => { try { return a.GetTypes(); } catch (System.Reflection.ReflectionTypeLoadException e) { return e.Types.OfType<Type>().ToArray(); } })
            .Where(t => typeof(System.Windows.Window).IsAssignableFrom(t))
            .Select(t => t.Name).Distinct().ToArray();

    private static readonly (Regex Pattern, string Why)[] Rules =
    [
        (new(@"(?<!\bOffScreenWindow)\.\s*(Show|ShowDialog|Activate)\s*\(\s*\)"), "shows or activates a window; use OffScreenWindow.Show"),
        (new(@"\bnew\s+(?:[\w.]+\.)?(" + string.Join("|", WindowTypes.Select(Regex.Escape)) + @")\s*(?:[({]|\r?$)"), "builds a window; use OffScreenWindow"),
        (new(@"\b(?:System\.Windows\.)?Window\??\s+\w+\s*(?:=|\([^)]*\)\s*=>)\s*new\s*\("), "builds a window; use OffScreenWindow"),
        (new(@"\b(?:Left|Top)\s*=\s*0\b"), "places a window on-screen; use OffScreenWindow"),
        (new(@"\bShowActivated\s*=\s*true\b"), "activates a window; use OffScreenWindow"),
        (new(@"UseShellExecute\s*=\s*true"), "may launch a GUI process onto the screen"),
    ];

    private static IEnumerable<string> Violations(string file, string source) =>
        source.Split('\n').Select((line, i) => (line, i))
            .Where(l => !l.line.TrimStart().StartsWith("//"))
            .SelectMany(l => Rules.Where(r => r.Pattern.IsMatch(l.line)).Select(r => $"{file}:{l.i + 1}: {r.Why}: {l.line.Trim()}"));

    [Fact]
    public void No_test_shows_a_window_except_off_screen_through_the_shared_helper()
    {
        var root = AppContext.BaseDirectory;
        while (!Path.Exists(Path.Combine(root, ".git")))
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("no repo root above the tests");
        var tests = Path.Combine(root, "dotnet", "tests");
        var files = Directory.EnumerateFiles(tests, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Split(Path.DirectorySeparatorChar).Any(d => d is "bin" or "obj"))
            .Where(f => Path.GetFileName(f) is not (Helper or nameof(TestWindowHygieneTests) + ".cs"))
            .ToList();

        Assert.Contains(files, f => f.EndsWith(nameof(DropdownMenuTests) + ".cs")); // the scan sees the window tests
        Assert.Empty(files.SelectMany(f => Violations(Path.GetRelativePath(root, f), File.ReadAllText(f))));
    }

    [Fact]
    public void The_scan_catches_the_windows_that_popped_up()
    {
        string[] bad =
        [
            "            Width = 400, Height = 300, Left = 0, Top = 0, ShowInTaskbar = false,",
            "        var window = new Window",
            "        var window = new Window { Content = spinner };",
            "        var shell = new System.Windows.Navigation.NavigationWindow();",
            "        Window w = new() { Width = 1 };",
            "    private static Window NewWindow() => new() { Width = 200, ShowActivated = false };",
            "            window.Show();",
            "        dialog.ShowDialog();",
            "            window.Activate();",
            "        var w = OffScreenWindow.Create(); w.ShowActivated = true;",
            "        var psi = new ProcessStartInfo(exe) { UseShellExecute = true };",
        ];
        foreach (var line in bad) Assert.True(Violations("x.cs", line).Any(), line);

        string[] fine =
        [
            "        var window = OffScreenWindow.Show(800, 600, root);",
            "        var window = OffScreenWindow.Create(600, 300, new Grid { Children = { element } });",
            "            OffScreenWindow.Show(window);",
            "        public Window Window { get; }",
            "        Assert.Equal(h.Window.FindResource(\"McText\"), badge.Foreground);",
            "        h.Show(badge);",
            "        Assert.True(now.Left >= Plot.Left);",
            "        var psi = new ProcessStartInfo(\"python\") { UseShellExecute = false, CreateNoWindow = true };",
        ];
        foreach (var line in fine) Assert.False(Violations("x.cs", line).Any(), line);
    }
}
