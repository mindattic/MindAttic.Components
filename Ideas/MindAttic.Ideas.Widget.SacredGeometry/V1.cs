using MindAttic.Ideas.Abstractions;

namespace MindAttic.Ideas.Widget.SacredGeometry;

/// <summary>
/// MindAttic.Ideas.Widget.SacredGeometry.V1 — the 1024-shape SacredGeometry catalog + renderer as a
/// self-contained Widget (the .idea target of the canonical UiUx <c>Components/SacredGeometry</c>
/// source). A code-only capability activator: it emits the renderer (<c>window.SacredGeometry</c>) plus
/// a tiny auto-init driver, both bundled in this package's wwwroot/ and served under
/// <c>/_ideas/Widget/sacredgeometry/1/</c>. Dropping it on a page makes every
/// <c>&lt;canvas data-sacred-shape="N"&gt;</c> animate shape N live (IntersectionObserver-gated,
/// MutationObserver-aware) — no per-page JS. Renders no widget of its own (inherits WidgetBase's
/// asset-emitting render). The driver loads AFTER the renderer.
/// </summary>
public sealed class V1 : WidgetBase
{
    private const string Mount = "/_ideas/Widget/sacredgeometry/1";

    public override IReadOnlyList<string> ScriptUrls { get; } = new[]
    {
        Mount + "/sacred-geometry.js",        // window.SacredGeometry (catalog + draw)
        Mount + "/sacred-geometry-init.js",   // the canvas[data-sacred-shape] animator
    };
}
