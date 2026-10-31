using MindAttic.Components.Settings;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[assembly: SettingsTab(typeof(MindAttic.Components.Settings.Tests.FakeRazorTab))]

namespace Microsoft.AspNetCore.Components
{
    // Stands in for the real IComponent: the contract checks by full name.
    public interface IComponent;
}

namespace MindAttic.Components.Settings.Tests
{
    public sealed class FakeView : Microsoft.AspNetCore.Components.IComponent;

    public sealed class FakeRazorTab : ISettingsTab
    {
        public string AppId { get; init; } = "fake-app";
        public string Title { get; init; } = "Fake";
        public SettingsTabKind Kind { get; init; } = SettingsTabKind.Razor;
        public Type ViewType { get; init; } = typeof(FakeView);
        public void ConfigureServices(IServiceCollection services, SettingsTabHostContext host) { }
    }

    public class SettingsContractTests
    {
        [Fact]
        public void FromAssembly_finds_the_declared_tab()
        {
            var tabs = SettingsTabDiscovery.FromAssembly(typeof(FakeRazorTab).Assembly);
            var tab = Assert.Single(tabs);
            Assert.Equal("fake-app", tab.AppId);
            Assert.False(tab.WebReachable);
        }

        [Theory]
        [InlineData("idiotproof", true)]
        [InlineData("mindattic.vault", true)]
        [InlineData("IdiotProof", false)]
        [InlineData("../evil", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsValidAppId(string? id, bool valid) => Assert.Equal(valid, SettingsTabDiscovery.IsValidAppId(id));

        [Fact]
        public void Validate_rejects_a_view_that_does_not_match_its_kind()
        {
            Assert.Throws<InvalidOperationException>(() => SettingsTabDiscovery.Validate(new FakeRazorTab { ViewType = typeof(string) }));
            Assert.Throws<InvalidOperationException>(() => SettingsTabDiscovery.Validate(new FakeRazorTab { Kind = SettingsTabKind.Wpf }));
            Assert.Throws<InvalidOperationException>(() => SettingsTabDiscovery.Validate(new FakeRazorTab { Title = " " }));
            Assert.Throws<InvalidOperationException>(() => SettingsTabDiscovery.Validate(new FakeRazorTab { AppId = "Bad Id" }));
        }

        [Fact]
        public void Registry_round_trips_and_skips_junk()
        {
            var dir = Path.Combine(Path.GetTempPath(), "ma-settings-" + Guid.NewGuid().ToString("N"));
            try
            {
                var registry = new SettingsTabRegistry(dir);
                registry.Register("zeta", "z.dll");
                registry.Register("alpha", "a.dll");
                registry.Register("alpha", "a2.dll");
                File.WriteAllText(Path.Combine(dir, "broken.json"), "{ not json");
                File.WriteAllText(Path.Combine(dir, "other.json"), """{"AppId":"alpha","AssemblyPath":"x.dll"}""");

                var list = registry.List();
                Assert.Equal(["alpha", "zeta"], list.Select(r => r.AppId));
                Assert.Equal(Path.GetFullPath("a2.dll"), list[0].AssemblyPath);
                Assert.True(registry.Unregister("zeta"));
                Assert.False(registry.Unregister("zeta"));
                Assert.Throws<ArgumentException>(() => registry.Register("..\\x", "x.dll"));
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
            }
        }

        [Theory]
        [InlineData(new[] { "--tab", "idiotproof" }, "idiotproof")]
        [InlineData(new[] { "--tab=prose" }, "prose")]
        [InlineData(new[] { "--tab", "Bad" }, null)]
        [InlineData(new[] { "--other" }, null)]
        public void TabFromArgs(string[] args, string? expected) => Assert.Equal(expected, SettingsActivation.TabFromArgs(args));

        [Fact]
        public async Task Activation_round_trips_over_the_pipe_and_a_second_listener_is_refused()
        {
            var pipe = "MindAttic.Components.Settings.Tests." + Guid.NewGuid().ToString("N");
            var asked = new List<string>();
            await using (new SettingsActivationListener(id => { lock (asked) asked.Add(id); return Task.FromResult(id == "known"); }, pipe))
            {
                Assert.Equal("ok", await SettingsActivation.TryActivateAsync("known", TimeSpan.FromSeconds(5), pipe));
                Assert.Equal("unknown", await SettingsActivation.TryActivateAsync("other", TimeSpan.FromSeconds(5), pipe));
                Assert.Equal("ok", await SettingsActivation.TryActivateAsync("known", TimeSpan.FromSeconds(5), pipe));
                Assert.ThrowsAny<Exception>(() => new SettingsActivationListener(_ => Task.FromResult(true), pipe));
            }
            Assert.Equal(["known", "other", "known"], asked);
            Assert.Null(await SettingsActivation.TryActivateAsync("known", TimeSpan.FromMilliseconds(300), pipe));
        }

        [Fact]
        public void FindDesktop_prefers_the_variable_then_the_recorded_location()
        {
            var dir = Path.Combine(Path.GetTempPath(), "ma-desktop-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var exe = Path.Combine(dir, SettingsLauncher.DesktopExe);
                File.WriteAllText(exe, "");
                var location = Path.Combine(dir, "desktop.json");
                Assert.Equal(exe, SettingsLauncher.FindDesktop(exe, location));

                SettingsLauncher.RecordDesktopLocation(exe, location);
                Assert.Equal(exe, SettingsLauncher.FindDesktop(null, location));

                // A recorded path to some other program is not trusted.
                var other = Path.Combine(dir, "evil.exe");
                File.WriteAllText(other, "");
                SettingsLauncher.RecordDesktopLocation(other, location);
                Assert.NotEqual(other, SettingsLauncher.FindDesktop(null, location));
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }
}
