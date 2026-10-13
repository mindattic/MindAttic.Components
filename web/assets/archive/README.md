# archive/

Assets that are **kept but not part of the served package**: nothing on any MindAttic site references
them, so they are left out of `assets-manifest.json`, the asset layout in [`docs/web/ASSETS.md`](../../../docs/web/ASSETS.md)
and the tests. Paths mirror where each file lived in the old MindAttic.Web.Shared package (fonts now live in
`web/components/<Font>/fonts/`, site files in `web/assets/<site>/`); restoring one means moving it there
and regenerating the manifest.

| Path | What it is |
|---|---|
| `fonts/outfit/outfit.ttf`, `fonts/attic/attic.ttf` | the original TTFs the served `components/OutfitFont/fonts/*.woff2` and `components/AtticFont/fonts/*.woff2` files were made from |
| `mindattic.com/logos/` | brand masters: the MindAttic Interactive wordmark (opaque + transparent) and the transparent M monogram |
| `mindatticcares.com/logos/m-cares-black-red-transparent.png` | 1024px master of the M-Cares logo (the site serves its 300px and 720px versions) |
| `ryandebraal.com/icons/` | the 32 Neko sprite frames and the MindAttic link icon; the page keeps these tiny files inline |

jsDelivr can technically serve any file in the repo, but nothing here is a supported URL: do not link to
`assets/archive/` from a site. Use the package folders instead.
