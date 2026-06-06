using MindAttic.Ideas.Abstractions;

namespace MindAttic.Ideas.Plugin.Tooltip;

/// <summary>
/// MindAttic.Ideas.Plugin.Tooltip.V1 — the hover-tooltip capability as a self-contained Plugin (the
/// .idea target of the UiUx Tooltip source). Dropping its tag loads the bundled tooltip css/js (served
/// from /_ideas/Plugin/tooltip/1/), so thereafter ANY element with <c>data-tooltip</c>/<c>data-tt</c>
/// shows a tooltip on hover. Renders no widget of its own (inherits PluginBase's asset-emitting render).
/// </summary>
public sealed class V1 : PluginBase
{
    private const string Mount = "/_ideas/Plugin/tooltip/1";

    public override IReadOnlyList<string> StylesheetUrls { get; } = new[] { Mount + "/tooltip.css" };
    public override IReadOnlyList<string> ScriptUrls { get; } = new[] { Mount + "/tooltip.js" };
}
