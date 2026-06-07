using MindAttic.Ideas.Abstractions;

namespace MindAttic.Ideas.Widget.OutfitFont;

/// <summary>
/// MindAttic.Ideas.Widget.OutfitFont.V1 — the Outfit typeface as a self-contained Widget (the .idea
/// target of the UiUx OutfitFont source). A code-only capability activator: it emits ONE stylesheet
/// (base64-embedded @font-face, so no external font files) bundled in this package's wwwroot/ and
/// served under <c>/_ideas/Widget/outfitfont/1/</c>. Dropping it on a page makes the Outfit family
/// available page-wide; it renders no widget of its own (inherits WidgetBase's asset-emitting render).
/// </summary>
public sealed class V1 : WidgetBase
{
    private const string Mount = "/_ideas/Widget/outfitfont/1";

    public override IReadOnlyList<string> StylesheetUrls { get; } = new[] { Mount + "/outfit-font.css" };
}
