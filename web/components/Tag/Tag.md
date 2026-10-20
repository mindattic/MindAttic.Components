# Tag

A pill (`.ma-tag`) with an optional hover tip (`.ma-tag__tip`). `tag.css` styles both from the theme
tokens; `tag.js` builds and places the tip, so the page supplies only the tip's content and its inner
styling.

```html
<span class="ma-tag" data-skill="C#">C#</span>
<script>
  var tags = MindAtticTag.init({
    tip: function (tagEl) { return describe(tagEl.dataset.skill); },  // HTML string or Node; '' = no tip
    tipClass: 'tag-tooltip',                                         // optional extra class(es)
    onShow: function (tagEl, tipEl) {}                               // optional, after each placement
  });
</script>
```

On mouseenter of a tag (one delegated capture listener on the document, so tags rendered later need
no wiring):

- **Lazy build.** The first hover asks `tip(tagEl)` for the content and wraps it in
  `<span class="ma-tag__tip [tipClass]">` inside the tag; later hovers reuse it. A tip already in the
  markup is placed as is.
- **Flip below.** The tip sits centred above the tag; with less than its height + `margin` (default
  10px) above the tag it gets `.ma-tag__tip--below`.
- **Keep on screen.** When the centred tip would cross the left or right viewport edge (less
  `margin`), it is shifted to the nearest position that fits and `--tt-arrow-x` is set so the arrow
  still points at the tag's centre. A later hover clear of the edges resets it to centred.

Options: `selector` (default `.ma-tag`), `margin` (10), `width` / `height` (280 / 170, used only when the
tip cannot be measured). `init` returns `{ place(tagEl), destroy() }`.

The site build splices `tag.js` in with `tag.css` (the `TAG` block's `js` in `sites.json`).
Consumers: ryandebraal.com. Tests: `web/tests/package/specs/components/tag.spec.mjs`.
