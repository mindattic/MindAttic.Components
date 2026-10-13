# HashPages

Several pages in one document. Each `<section class="ma-page" id="...">` is a page (document order,
first is home); links with `data-page="<id>"` switch between them without a reload, and the URL hash
follows (`#<id>`, empty for home) so pages are linkable. A hash naming an element inside a page
(`#sec-budget`) opens that page and scrolls to it.

```html
<nav><a href="#" data-page="home">Home</a> <a href="#faq" data-page="faq">FAQ</a></nav>
<section class="ma-page" id="home">...</section>
<section class="ma-page" id="faq">...</section>
<script>var site = MindAtticHashPages.init({ nav: 'nav' });</script>
```

`init` returns `{ show(id), pages }` for scripted navigation (e.g. a capture-demo tour). Links inside
`nav` get `.ma-page-link--active` for the page showing; style that in the page's CSS.
Consumers: mindatticcares.com.
