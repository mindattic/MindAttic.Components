/* MindAttic.Components - HashPages/hash-pages.js
   One document, several pages: each <section class="ma-page" id="..."> is a page, in document
   order, the first being home. Links with data-page="<id>" switch pages without reloading; the
   URL hash follows (#<id>, nothing for home) so pages can be linked and the back/forward buttons
   work. A hash that names an element inside a page opens that page and scrolls to it.

     var pages = MindAtticHashPages.init({ nav: 'header nav' });
     pages.show('y2k');   pages.pages  // ['home', ..., 'y2k']

   Links inside `nav` get .ma-page-link--active while their page is showing. */
window.MindAtticHashPages = (function () {
  function init(opts) {
    opts = opts || {};
    var pages = Array.prototype.map.call(document.querySelectorAll('.ma-page'), function (s) { return s.id; });
    var home = pages[0];
    var navLinks = document.querySelectorAll('[data-page]');

    function show(name){
      if (pages.indexOf(name) === -1) name = home;
      document.querySelectorAll('.ma-page').forEach(function(s){
        s.classList.toggle('ma-page--active', s.id === name);
      });
      document.querySelectorAll((opts.nav || '') + ' [data-page]').forEach(function(a){
        a.classList.toggle('ma-page-link--active', a.getAttribute('data-page') === name);
      });
      window.scrollTo(0, 0);
    }

    function fromHash(){
      var h = (window.location.hash || '').replace(/^#/, '');
      if (h && pages.indexOf(h) === -1) {
        // In-page anchor (e.g. #sec-budget, #contents): open the page that contains it, then
        // bring the anchor into view (show() resets the scroll position to the top).
        var target = document.getElementById(h);
        var owner = target && target.closest('.ma-page');
        if (owner && !owner.classList.contains('ma-page--active')) {
          show(owner.id);
          target.scrollIntoView();
        } else if (!document.querySelector('.ma-page.ma-page--active')) {
          show(home);
        }
        return;
      }
      show(h || home);
    }

    navLinks.forEach(function(a){
      a.addEventListener('click', function(e){
        e.preventDefault();
        var name = a.getAttribute('data-page');
        // pushState (a history entry per page switch) so back/forward step through pages, as documented above.
        // pushState fires no hashchange, so a popstate handler below re-reads the URL on back/forward.
        var url = name === home ? window.location.pathname + window.location.search : '#' + name;
        var current = window.location.pathname + window.location.search + window.location.hash;
        var next = name === home ? url : window.location.pathname + window.location.search + url;
        if (next !== current) history.pushState(null, '', url);
        show(name);
      });
    });

    window.addEventListener('hashchange', fromHash);
    // Back/forward across pushState entries fires popstate (and hashchange only when the hash differs);
    // fromHash is idempotent, so handling both is harmless.
    window.addEventListener('popstate', fromHash);
    fromHash();
    return { show: show, pages: pages };
  }
  return { init: init };
})();
