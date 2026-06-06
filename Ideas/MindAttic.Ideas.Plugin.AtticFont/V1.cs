using MindAttic.Ideas.Abstractions;

namespace MindAttic.Ideas.Plugin.AtticFont;

/// <summary>
/// MindAttic.Ideas.Plugin.AtticFont.V1 — the Attic display typeface as a self-contained Plugin.
/// Emits one stylesheet (base64-embedded @font-face, no external font files) bundled in this package's
/// wwwroot/ and served under <c>/_ideas/Plugin/atticfont/1/</c>. Registers the family and exposes the
/// <c>--font-attic</c> token; a Theme/page applies it (e.g. <c>font-family: var(--font-attic)</c>).
/// </summary>
public sealed class V1 : PluginBase
{
    private const string Mount = "/_ideas/Plugin/atticfont/1";

    public override IReadOnlyList<string> StylesheetUrls { get; } = new[] { Mount + "/attic-font.css" };
}
