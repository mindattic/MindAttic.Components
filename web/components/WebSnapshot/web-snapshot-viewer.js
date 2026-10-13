// WebSnapshot viewer — loads a captured snapshot into a `.web-snapshot > img`.
// Two ways to feed an element:
//   A) Fetch mode  — set `data-src` to a .b64.txt URL. The viewer fetches the
//      data URI and assigns it to the inner <img>.
//   B) Inline mode — leave `data-src` off. Set `<img src>` yourself (e.g.,
//      inline base64). No network.
//
// Markup:
//   <div class="web-snapshot" style="width:150px;height:225px"
//        data-src="path/to/ryandebraal.b64.txt">
//     <img alt="ryandebraal.com">
//   </div>
//
// Public API:
//   WebSnapshot.attach(el, opts?)
//   WebSnapshot.refresh(el)
//   WebSnapshot.autoInit(root?)  — rescan; call after DOM mutations

(function (global) {
  function readOpts(el) {
    return { src: el.dataset.src || null };
  }

  function attach(el, opts) {
    if (!el) return undefined;
    // Already attached: reconcile a changed data-src (autoInit() is documented
    // to be re-run after DOM mutations, and a reused node may carry a new src).
    if (el.__webSnapshotState) {
      const state = el.__webSnapshotState;
      const nextSrc = (opts && 'src' in opts) ? opts.src : readOpts(el).src;
      if (nextSrc && nextSrc !== state.opts.src) {
        state.opts.src = nextSrc;
        refresh(el).catch(err => console.warn('[WebSnapshot] refresh failed:', err));
      }
      return state;
    }

    const merged = Object.assign({}, readOpts(el), opts || {});
    let img = el.querySelector('img');
    if (!img) {
      img = document.createElement('img');
      el.appendChild(img);
    }

    const state = { el, img, opts: merged };
    el.__webSnapshotState = state;

    if (merged.src) {
      refresh(el).catch(err => console.warn('[WebSnapshot] refresh failed:', err));
    }
    return state;
  }

  async function refresh(el) {
    const state = el && el.__webSnapshotState;
    if (!state) throw new Error('WebSnapshot.refresh: element not attached');
    const src = state.opts.src;
    if (!src) return;

    const bust = '_t=' + Date.now();
    const url = src + (src.includes('?') ? '&' : '?') + bust;
    const res = await fetch(url, { cache: 'no-store' });
    if (!res.ok) throw new Error(`Fetch ${src} failed: ${res.status}`);
    const payload = (await res.text()).trim();
    // Guard against a 200 that's actually an HTML error page or anything else
    // non-image — assigning it would just yield a silently broken <img>.
    if (!/^(data:image\/|https?:\/\/)/i.test(payload)) {
      throw new Error(`Fetch ${src} returned a non-image payload`);
    }
    state.img.src = payload;
  }

  function autoInit(root) {
    (root || document).querySelectorAll('.web-snapshot').forEach(el => attach(el));
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => autoInit(), { once: true });
  } else {
    autoInit();
  }

  global.WebSnapshot = { attach, refresh, autoInit };
})(window);
