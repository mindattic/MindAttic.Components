using MindAttic.Ideas.Abstractions;

namespace MindAttic.Ideas.Plugin.BackHomeM;

/// <summary>
/// MindAttic.Ideas.Plugin.BackHomeM.V1 — the "back to mindattic.com" corner glyph as a self-contained
/// Plugin. Emits one stylesheet (base64-embedded icon @font-face, no external files) bundled in this
/// package's wwwroot/ and served under <c>/_ideas/Plugin/backhomem/1/</c>, styling the
/// <c>.back-home-m</c> anchor a Theme/page places.
/// </summary>
public sealed class V1 : PluginBase
{
    private const string Mount = "/_ideas/Plugin/backhomem/1";

    public override IReadOnlyList<string> StylesheetUrls { get; } = new[] { Mount + "/back-home-m.css" };
}
