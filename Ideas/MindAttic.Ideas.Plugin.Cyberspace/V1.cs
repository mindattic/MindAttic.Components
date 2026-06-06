using MindAttic.Ideas.Abstractions;

namespace MindAttic.Ideas.Plugin.Cyberspace;

/// <summary>
/// MindAttic.Ideas.Plugin.Cyberspace.V1 — the animated console-background effects engine as a
/// self-contained Plugin. Bundles its own CSS (the effect-layer styles), the engine JS, the circuitboard
/// textures, and a bootstrap shim that points the engine at those bundled textures — all under
/// <c>/_ideas/Plugin/cyberspace/1/</c>. The Theme renders the effect-layer divs (.cyberspace-sl-fine /
/// .cyberspace-sl-coarse / .console-bg-host) and composes this Plugin; the engine paints into them.
/// circuitboard-srcs.js MUST load before console-bg.js (it reads window.__cyberspaceCircuitboardSrcs at init).
/// </summary>
public sealed class V1 : PluginBase
{
    private const string Mount = "/_ideas/Plugin/cyberspace/1";

    public override IReadOnlyList<string> StylesheetUrls { get; } = new[] { Mount + "/frontpage.css" };

    public override IReadOnlyList<string> ScriptUrls { get; } = new[]
    {
        Mount + "/circuitboard-srcs.js",
        Mount + "/loader.js",
        Mount + "/console-bg.js",
    };
}
