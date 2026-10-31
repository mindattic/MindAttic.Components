using Microsoft.Extensions.DependencyInjection;

namespace MindAttic.Components.Settings;

/// <summary>What a settings tab's view is built with, which decides which hosts can show it.</summary>
public enum SettingsTabKind
{
    /// <summary>A portable Razor component: no @page, render mode or auth lookup. Hostable by
    /// MindAttic.Vault.Desktop (BlazorWebView) and, when the tab opts in, MindAttic.Vault.Web.</summary>
    Razor,

    /// <summary>A native WPF UserControl with a public parameterless constructor. Desktop only.</summary>
    Wpf,
}

/// <summary>
/// One app's settings tab. An app's settings package implements this once and marks it with
/// <c>[assembly: SettingsTab(typeof(MyTab))]</c>; hosts discover it by scanning that assembly.
/// The implementation needs a public parameterless constructor.
/// </summary>
public interface ISettingsTab
{
    /// <summary>Stable id of the app, e.g. "idiotproof". Lower case; used in the pipe protocol,
    /// the registry file name and the Desktop's --tab argument.</summary>
    string AppId { get; }

    /// <summary>The tab's caption, e.g. "IdiotProof".</summary>
    string Title { get; }

    /// <summary>Whether <see cref="ViewType"/> is a Razor component or a WPF UserControl.</summary>
    SettingsTabKind Kind { get; }

    /// <summary>The view: a type implementing Microsoft.AspNetCore.Components.IComponent for
    /// <see cref="SettingsTabKind.Razor"/>, or deriving from System.Windows.Controls.UserControl for
    /// <see cref="SettingsTabKind.Wpf"/>.</summary>
    Type ViewType { get; }

    /// <summary>Whether a web host may show this tab. Off unless the app opts in; Desktop ignores it.</summary>
    bool WebReachable => false;

    /// <summary>Registers the services the view injects into the host's container (the tab's own
    /// DI graph: repositories, Vault access, ...). Called once per host before the view is built.</summary>
    void ConfigureServices(IServiceCollection services, SettingsTabHostContext host);

    /// <summary>Parameters passed to a Razor view (e.g. the signed-in user), resolved from the
    /// host's built container. Ignored for WPF views, which take what they need through DI.</summary>
    Task<IReadOnlyDictionary<string, object?>> ParametersAsync(IServiceProvider services, SettingsTabHostContext host) =>
        Task.FromResult<IReadOnlyDictionary<string, object?>>(new Dictionary<string, object?>());

    /// <summary>Full paths of stylesheets a Razor view needs (its app's CSS, usually under
    /// <see cref="SettingsTabHostContext.ContentRoot"/>). A host without the app's web root, such as
    /// a BlazorWebView, inlines them into the page; missing files are skipped. Ignored for WPF views.</summary>
    IReadOnlyList<string> Stylesheets(SettingsTabHostContext host) => [];
}

/// <summary>What a host tells a tab about where it is running.</summary>
/// <param name="HostName">"MindAttic.Vault.Desktop", "MindAttic.Vault.Web" or the app's own host.</param>
/// <param name="UserName">The signed-in user's name (on Desktop: the Windows account).</param>
/// <param name="ContentRoot">The directory the tab's assembly was loaded from.</param>
public sealed record SettingsTabHostContext(string HostName, string UserName, string ContentRoot);

/// <summary>Marks the <see cref="ISettingsTab"/> implementation an assembly carries.</summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class SettingsTabAttribute(Type tabType) : Attribute
{
    /// <summary>The type implementing <see cref="ISettingsTab"/>.</summary>
    public Type TabType { get; } = tabType;
}
