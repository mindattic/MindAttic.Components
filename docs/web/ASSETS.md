# Runtime assets — the shared jsDelivr package

The `web/` folder of MindAttic.Components (formerly MindAttic.Web.Shared) is also the **shared runtime asset
package** for every MindAttic site. Fonts, images, brand art, theme art and the Cyberspace effect textures
live here once, as plain static files, and every site loads them over jsDelivr instead of embedding them in
its HTML ([BIBLE §2](BIBLE.md#MAU-§2), [MAU-LAW-7](BIBLE.md#MAU-LAW-7)).

## URL pattern

```
https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@<tag>/web/<path>
```

`<tag>` is a whole-number release tag of MindAttic.Components (`V1`, `V2`, …) and **tags are immutable**
([MAU-LAW-6](BIBLE.md#MAU-LAW-6)), so a pinned URL returns the same bytes forever and is cached by browsers
and by the CDN edge indefinitely. Never point a production page at `@main`. The pages pin the newest
whole-number `V<n>` tag of this repo (or the tag the linked deploy passes as `-Tag`).

## Layout

A site's own assets live in `assets/<domain>/`; a file a component or theme cannot work without stays with it.

```
web/
├── assets/
│   ├── mindattic.com/logos/           m-monogram.png, m-icon-16.png, favicon.ico
│   ├── mindatticcares.com/
│   │   ├── logos/  icons/  images/
│   ├── ryandebraal.com/images/        ryan-avatar.png, ryan-portrait.png
│   └── archive/                       kept but not served (see below)
├── components/<Name>/                 component-owned runtime files (CSS/JS and what they cannot work without)
│   ├── OutfitFont/fonts/              outfit-latin.woff2, outfit-latin-ext.woff2
│   ├── AtticFont/fonts/               attic.woff2
│   └── Cyberspace/assets/             circuitboard.00-02.png parallax textures
├── themes/<Name>/                     theme-owned art: Noir's moon, Sakura's and Sunset's 20 backgrounds
└── assets-manifest.json               generated: path, bytes, SHA-256, pixel size of every asset
```

Rule of thumb: **used by one site → `assets/<domain>/<category>/`; owned by a theme → the theme's folder; a
file a component cannot work without (fonts included) stays with the component.** Category folders are named
for what the files are (`logos`, `icons`, `images`), not for the page that uses them. Nothing is ever copied
into two places ([MAU-LAW-5](BIBLE.md#MAU-LAW-5)).

**Only what a site uses is in the package.** Files nothing references — the TTF sources of the web fonts,
brand masters, art a page keeps inline — live in `web/assets/archive/`, at the same relative path they would
have in the package. They stay in the repo but are not in `assets-manifest.json`, not in this layout and not
part of the supported URLs; a test fails if a site links to `assets/archive/`.

## Quality rules

- **Lossy formats keep their full resolution.** A JPEG/WebP is committed byte-for-byte as received;
  it is never downscaled or re-encoded to save space.
- **Lossless recompression is welcome, but must be verified.** A PNG may be re-encoded (for example to
  an 8-bit palette) only if the decoded pixels are identical — the Cyberspace textures went from
  6.2 MB to 1.7 MB this way, pixel-for-pixel identical.
- **Validate fonts before committing them.** Load every woff2 with `fontTools` (or in a browser) —
  a browser silently falls back to a system font when a woff2 is corrupt, which hides the bug.
- **Names:** lowercase kebab-case, descriptive, no spaces; put the variant last
  (`m-cares-black-red-transparent-720.png`).

## Adding or changing an asset

1. Put the file in the right folder, using the naming rules above.
2. `powershell -NoProfile -ExecutionPolicy Bypass -File web\tools\build-asset-manifest.ps1`
3. Commit the asset(s) and `assets-manifest.json` together.
4. Run `MindAttic.Deploy`'s linked deploy (`npm run deploy -- --uiux`): it tags MindAttic.Components with
   the next whole-number tag, pins it in all four sites (commit `Pin MindAttic.Components V<n>` in
   MindAttic.Web), pushes both repos and the tag, checks every asset on the CDN and uploads the sites.
   **Never move or reuse a tag.**

`web\tools\build-asset-manifest.ps1 -Verify` fails if the manifest is stale — use it in CI (the linked
deploy runs it as a preflight gate).

## How a site consumes the package

```html
<!-- in <head>: open the connection early, start the fonts the first paint needs -->
<link rel="preconnect" href="https://cdn.jsdelivr.net" crossorigin>
<link rel="preload" as="font" type="font/woff2" crossorigin
      href="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@V3/web/components/OutfitFont/fonts/outfit-latin.woff2">
```

The `@font-face` rules themselves come from the font component's `*.cdn.css`, spliced into the page's
FONTS marker block by `web/build/build-sites.mjs` with the pinned URL filled in.

- Fonts need `crossorigin` on the `<link rel=preload>`, otherwise the browser fetches them twice.
- Use `fetchpriority="low"` on preloads of decorative images so they never compete with the first paint.
- Add `loading="lazy" decoding="async"` to below-the-fold `<img>` tags.
- Load heavy scripts with `defer` so they never block the first paint.

## Verifying a release

After pushing a tag, check that jsDelivr serves every file at its committed size, for example:

```
curl -sI https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@V3/web/components/AtticFont/fonts/attic.woff2
```

and compare against `assets-manifest.json` (`bytes` / `sha256`). If a brand-new tag 404s for a minute,
retry; `https://purge.jsdelivr.net/gh/mindattic/MindAttic.Components@V3/web/<path>` forces a refresh. The
linked deploy's CDN gate does this check for every asset the sites use.

## Testing

`web/tests/package/` holds a Playwright suite that validates this package and the sites that consume it (see
its `README.md`): the manifest matches the tree, fonts and images decode, lossy files keep their full
resolution and bytes, size budgets hold, every MindAttic.Components URL a site references exists, and each
site loads with no failed requests, no console errors and the right layout at every viewport — in every view
a visitor can reach (hash pages, themes), with desktop-style scrollbars so overflow a Windows visitor would
see is caught. The expected tag is the newest `V<n>` tag of this repo (override with `SHARED_TAG`).
`npm run test:local` runs it against the working tree before a tag is published; `npm run test:live` /
`npm run test:cdn` verify what jsDelivr actually serves.
