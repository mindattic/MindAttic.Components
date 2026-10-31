using System.Diagnostics;
using System.Text.Json;

namespace MindAttic.Components.Settings;

/// <summary>What <see cref="SettingsLauncher.OpenAsync"/> did.</summary>
public enum SettingsOpenResult
{
    /// <summary>A running Desktop came to the front on the app's tab.</summary>
    Activated,

    /// <summary>The Desktop was started at the app's tab.</summary>
    Launched,

    /// <summary>A Desktop is running but has no tab for this app (its tab is not registered).</summary>
    UnknownTab,

    /// <summary>No Desktop is running and its executable could not be found.</summary>
    NotInstalled,
}

/// <summary>
/// The one "open my settings" helper every app's cog calls: brings MindAttic.Vault.Desktop to the
/// front on the app's tab, or launches it there. The Desktop records where it lives in
/// %APPDATA%\MindAttic\Vault\desktop.json each time it starts (<see cref="RecordDesktopLocation"/>),
/// so the helper finds it without configuration; MINDATTIC_VAULT_DESKTOP_EXE overrides that.
/// </summary>
public static class SettingsLauncher
{
    /// <summary>The Desktop's executable file name.</summary>
    public const string DesktopExe = "MindAttic.Vault.Desktop.exe";

    /// <summary>Environment variable naming the Desktop executable explicitly.</summary>
    public const string ExeVariable = "MINDATTIC_VAULT_DESKTOP_EXE";

    private sealed record DesktopLocation(string ExePath);

    /// <summary>%APPDATA%\MindAttic\Vault\desktop.json.</summary>
    public static string LocationFile => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MindAttic", "Vault", "desktop.json");

    /// <summary>Opens the app's tab. Never throws for a missing Desktop; reports it instead.</summary>
    public static async Task<SettingsOpenResult> OpenAsync(string appId, CancellationToken ct = default)
    {
        var answer = await SettingsActivation.TryActivateAsync(appId, TimeSpan.FromSeconds(2), ct: ct);
        if (answer == "ok") return SettingsOpenResult.Activated;
        if (answer is not null) return SettingsOpenResult.UnknownTab;

        var exe = FindDesktop();
        if (exe is null) return SettingsOpenResult.NotInstalled;
        var start = new ProcessStartInfo(exe) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(exe)! };
        start.ArgumentList.Add(SettingsActivation.TabSwitch);
        start.ArgumentList.Add(appId);
        using var process = Process.Start(start);
        return process is null ? SettingsOpenResult.NotInstalled : SettingsOpenResult.Launched;
    }

    /// <summary>The Desktop executable: <see cref="ExeVariable"/>, else the location the Desktop
    /// last recorded, else the usual install folders. Null when none exists.</summary>
    public static string? FindDesktop() => FindDesktop(Environment.GetEnvironmentVariable(ExeVariable), LocationFile);

    /// <summary>Testable core of <see cref="FindDesktop()"/>.</summary>
    public static string? FindDesktop(string? explicitExe, string locationFile)
    {
        if (!string.IsNullOrWhiteSpace(explicitExe) && File.Exists(explicitExe)) return Path.GetFullPath(explicitExe);
        try
        {
            if (File.Exists(locationFile)
                && JsonSerializer.Deserialize<DesktopLocation>(File.ReadAllText(locationFile)) is { ExePath.Length: > 0 } recorded
                && File.Exists(recorded.ExePath)
                && string.Equals(Path.GetFileName(recorded.ExePath), DesktopExe, StringComparison.OrdinalIgnoreCase))
                return recorded.ExePath;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
        }
        foreach (var dir in new[]
                 {
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MindAttic", "Vault.Desktop"),
                     @"C:\Apps\MindAttic.Vault.Desktop",
                 })
        {
            var candidate = Path.Combine(dir, DesktopExe);
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    /// <summary>Called by the Desktop on start so cogs can find it.</summary>
    public static void RecordDesktopLocation(string exePath, string? locationFile = null)
    {
        var file = locationFile ?? LocationFile;
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, JsonSerializer.Serialize(new DesktopLocation(Path.GetFullPath(exePath))));
    }
}
