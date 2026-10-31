using System.Text.Json;

namespace MindAttic.Components.Settings;

/// <summary>One registered settings tab: which app, and the assembly that declares its tab.</summary>
/// <param name="AppId">The tab's <see cref="ISettingsTab.AppId"/>.</param>
/// <param name="AssemblyPath">Full path to the assembly carrying <see cref="SettingsTabAttribute"/>,
/// in a directory that also holds its dependencies (the app's own output or install folder).</param>
public sealed record SettingsTabRegistration(string AppId, string AssemblyPath);

/// <summary>
/// The per-user list of installed settings tabs, one JSON file per app under
/// %APPDATA%\MindAttic\Vault\SettingsTabs. An app registers its tab (on start or install) and
/// MindAttic.Vault.Desktop reads the folder, so adding an app never changes the Desktop's source.
/// Holds paths only, never settings or secrets.
/// </summary>
public sealed class SettingsTabRegistry(string directory)
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    /// <summary>The registry for the current Windows user.</summary>
    public static SettingsTabRegistry ForCurrentUser() => new(DefaultDirectory);

    /// <summary>%APPDATA%\MindAttic\Vault\SettingsTabs.</summary>
    public static string DefaultDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MindAttic", "Vault", "SettingsTabs");

    /// <summary>The folder the registration files live in.</summary>
    public string Directory { get; } = directory;

    /// <summary>Records (or replaces) an app's tab registration.</summary>
    public void Register(string appId, string assemblyPath)
    {
        if (!SettingsTabDiscovery.IsValidAppId(appId))
            throw new ArgumentException($"'{appId}' is not a valid settings tab id.", nameof(appId));
        var full = Path.GetFullPath(assemblyPath);
        System.IO.Directory.CreateDirectory(Directory);
        var file = FileFor(appId);
        var temp = file + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(new SettingsTabRegistration(appId, full), Json));
        File.Move(temp, file, overwrite: true);
    }

    /// <summary>Removes an app's registration; false when it had none.</summary>
    public bool Unregister(string appId)
    {
        if (!SettingsTabDiscovery.IsValidAppId(appId)) return false;
        var file = FileFor(appId);
        if (!File.Exists(file)) return false;
        File.Delete(file);
        return true;
    }

    /// <summary>Every readable registration, ordered by AppId. A malformed or mismatched file is skipped.</summary>
    public IReadOnlyList<SettingsTabRegistration> List()
    {
        if (!System.IO.Directory.Exists(Directory)) return [];
        var result = new List<SettingsTabRegistration>();
        foreach (var file in System.IO.Directory.EnumerateFiles(Directory, "*.json"))
        {
            try
            {
                var reg = JsonSerializer.Deserialize<SettingsTabRegistration>(File.ReadAllText(file));
                if (reg is null || !SettingsTabDiscovery.IsValidAppId(reg.AppId) || string.IsNullOrWhiteSpace(reg.AssemblyPath)) continue;
                if (!string.Equals(Path.GetFileNameWithoutExtension(file), reg.AppId, StringComparison.Ordinal)) continue;
                result.Add(reg);
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
            }
        }
        return result.OrderBy(r => r.AppId, StringComparer.Ordinal).ToList();
    }

    private string FileFor(string appId) => Path.Combine(Directory, appId + ".json");
}
