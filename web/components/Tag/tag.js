/* MindAttic.Components - Tag/tag.js
   Places the hover tip of a .ma-tag pill (see tag.css). One delegated listener on the document
   serves every tag, present and future, so a page that re-renders its tags never re-wires them.

   On mouseenter of a .ma-tag:
     1. Lazy build: when the tag has no .ma-tag__tip yet, tip(tagEl) is asked for the tip's content
        (an HTML string or a Node; nothing/'' = this tag has no tip) and it is wrapped in
        <span class="ma-tag__tip [tipClass]"> inside the tag. A tip already in the markup is used as is.
     2. Flip below: the tip sits centered above the tag; with less than its height + margin above
        the tag it gets .ma-tag__tip--below.
     3. Keep on screen: when the centered tip would cross the left or right viewport edge (minus
        margin), it is shifted to the nearest position that fits, and --tt-arrow-x moves the arrow
        so it still points at the tag's center.

     var tags = MindAtticTag.init({
       tip: function (tagEl) { return SKILLS[tagEl.dataset.skill] ? '<b>...</b>' : ''; },
       tipClass: 'tag-tooltip',            // extra class(es) for the page's inner styling
       onShow: function (tagEl, tipEl) {}  // after each placement (e.g. animate something inside)
     });
     tags.place(tagEl);   tags.destroy();

   The page supplies only the tip's content and its inner styling. */
window.MindAtticTag = (function () {
  'use strict';

  function init(opts) {
    opts = opts || {};
    var selector = opts.selector || '.ma-tag';
    var margin = opts.margin != null ? opts.margin : 10;
    // Fallback size (the tip's CSS width, a typical height) when the tip cannot be measured.
    var fallbackW = opts.width || 280;
    var fallbackH = opts.height || 170;

    function build(tagEl) {
      var tip = tagEl.querySelector('.ma-tag__tip');
      if (tip || typeof opts.tip !== 'function') return tip;
      var content = opts.tip(tagEl);
      if (content == null || content === '') return null;
      tip = document.createElement('span');
      tip.className = 'ma-tag__tip' + (opts.tipClass ? ' ' + opts.tipClass : '');
      if (typeof content === 'string') tip.innerHTML = content; else tip.appendChild(content);
      tagEl.appendChild(tip);
      return tip;
    }

    function place(tagEl) {
      var tip = build(tagEl);
      if (!tip) return null;
      // Back to the CSS default: centered above, arrow in the middle.
      tip.classList.remove('ma-tag__tip--below');
      tip.style.left = '';
      tip.style.transform = '';
      tip.style.removeProperty('--tt-arrow-x');

      var r = tagEl.getBoundingClientRect();
      var w = tip.offsetWidth || fallbackW;
      var h = tip.offsetHeight || fallbackH;
      var vw = document.documentElement.clientWidth || window.innerWidth;

      if (r.top < h + margin) tip.classList.add('ma-tag__tip--below');

      var tagCx = r.left + r.width / 2;
      var idealLeft = tagCx - w / 2;
      if (idealLeft < margin || idealLeft + w > vw - margin) {
        var left = Math.max(margin, Math.min(idealLeft, vw - margin - w));
        // The tip is positioned against the tag's padding box (.ma-tag is position:relative).
        tip.style.left = (left - (r.left + tagEl.clientLeft)) + 'px';
        tip.style.transform = 'none';
        tip.style.setProperty('--tt-arrow-x', (tagCx - left) + 'px');
      }
      if (typeof opts.onShow === 'function') opts.onShow(tagEl, tip);
      return tip;
    }

    function onEnter(e) {
      var t = e.target;
      if (!t || !t.closest) return;
      var tagEl = t.closest(selector);
      // mouseenter does not bubble, so the capture-phase listener sees one event per element
      // entered; act only on the tag itself, not on every child the pointer crosses.
      if (tagEl && tagEl === t) place(tagEl);
    }

    document.addEventListener('mouseenter', onEnter, true);
    return {
      place: place,
      destroy: function () { document.removeEventListener('mouseenter', onEnter, true); },
    };
  }

  return { init: init };
})();
