using System.Reflection;
using System.Text.RegularExpressions;

namespace MindAttic.Components.Settings;

/// <summary>Finds and checks the settings tabs an assembly declares.</summary>
public static partial class SettingsTabDiscovery
{
    [GeneratedRegex("^[a-z0-9][a-z0-9.-]{0,63}$")]
    private static partial Regex AppIdPattern();

    /// <summary>Whether <paramref name="appId"/> is a valid tab id (lower-case letters, digits, '.', '-').</summary>
    public static bool IsValidAppId(string? appId) => appId is not null && AppIdPattern().IsMatch(appId);

    /// <summary>Instantiates every tab named by a <see cref="SettingsTabAttribute"/> on the assembly.
    /// Throws <see cref="InvalidOperationException"/> when a declared tab breaks the contract.</summary>
    public static IReadOnlyList<ISettingsTab> FromAssembly(Assembly assembly)
    {
        var tabs = new List<ISettingsTab>();
        foreach (var attr in assembly.GetCustomAttributes<SettingsTabAttribute>())
        {
            if (!typeof(ISettingsTab).IsAssignableFrom(attr.TabType))
                throw new InvalidOperationException($"{attr.TabType.FullName} does not implement ISettingsTab.");
            var tab = (ISettingsTab)(Activator.CreateInstance(attr.TabType)
                ?? throw new InvalidOperationException($"Could not create {attr.TabType.FullName}."));
            Validate(tab);
            tabs.Add(tab);
        }
        return tabs;
    }

    /// <summary>Throws <see cref="InvalidOperationException"/> when the tab breaks the contract:
    /// a bad AppId, an empty Title, or a view type that does not match its Kind.</summary>
    public static void Validate(ISettingsTab tab)
    {
        if (!IsValidAppId(tab.AppId))
            throw new InvalidOperationException($"Settings tab AppId '{tab.AppId}' must be lower-case letters, digits, '.' or '-'.");
        if (string.IsNullOrWhiteSpace(tab.Title))
            throw new InvalidOperationException($"Settings tab '{tab.AppId}' has no Title.");
        var view = tab.ViewType ?? throw new InvalidOperationException($"Settings tab '{tab.AppId}' has no ViewType.");
        switch (tab.Kind)
        {
            case SettingsTabKind.Razor when !Implements(view, "Microsoft.AspNetCore.Components.IComponent"):
                throw new InvalidOperationException($"Settings tab '{tab.AppId}' is Razor but {view.FullName} is not a Razor component.");
            case SettingsTabKind.Wpf when !DerivesFrom(view, "System.Windows.Controls.UserControl"):
                throw new InvalidOperationException($"Settings tab '{tab.AppId}' is Wpf but {view.FullName} is not a UserControl.");
        }
        if (tab.Kind == SettingsTabKind.Wpf && tab.WebReachable)
            throw new InvalidOperationException($"Settings tab '{tab.AppId}' is a WPF view and cannot be web-reachable.");
    }

    // By name, so this contract carries no Razor or WPF dependency.
    private static bool Implements(Type type, string interfaceName) =>
        type.GetInterfaces().Any(i => i.FullName == interfaceName);

    private static bool DerivesFrom(Type type, string baseName)
    {
        for (var t = type.BaseType; t is not null; t = t.BaseType)
            if (t.FullName == baseName) return true;
        return false;
    }
}
