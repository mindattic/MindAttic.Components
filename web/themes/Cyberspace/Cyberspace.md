# Cyberspace theme

Dark cyberpunk landing-page chrome. Composes with the **Cyberspace** Component (effects engine), the **OutfitFont** + **AtticFont** Components, and the **BackHomeM** Component into a single README-style project page.

> **No current consumer.** No page loads this theme today (each repo's GitHub README is its project page). The files are served on the CDN for any page that wants the composition.

## What this theme includes

| Layer | Source |
|---|---|
| Page chrome (`.hero`, `.readme`, `.btn`, layout) | `theme.css` (this folder) |
| The three Cyberspace-effects fixed-position divs | `body-prelude.html` (this folder) |
| The Cyberspace effects engine (animated console background) | `Components/Cyberspace/*.js` |
| Outfit + Attic typography | `Components/OutfitFont/`, `Components/AtticFont/` |
| Back-to-mindattic.com glyph | `Components/BackHomeM/` |

## Layout the theme expects

```html
<body>
  <!-- theme body-prelude.html goes here -->
  <a class="back-home-m" href="https://mindattic.com/"></a>

  <div class="page">
    <header class="hero">
      <h1 class="project-name" id="<slug>">Title</h1>
      <p class="tagline">One-line tagline</p>
      <div class="btn-row">
        <a class="btn btn-primary">Open</a>
        <a class="btn btn-secondary">GitHub</a>
      </div>
    </header>
    <article class="readme">
      <!-- rendered README HTML -->
    </article>
  </div>

  <!-- theme script tags (deps.json) go here -->
</body>
```

## Direct (non-theme) consumption

Prose consumes the **Cyberspace Component directly** from its Blazor `wwwroot` — not via this theme. If you want only the effects engine without the page chrome, depend on `Components/Cyberspace/` rather than `Themes/Cyberspace/`.

## Related

- Component layer: [`Components/Cyberspace/`](../../Components/Cyberspace/Cyberspace.md) — the raw effects engine.
