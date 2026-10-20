// Tag component: tag.js places the hover tip of a .ma-tag pill (tag.css). The page supplies only the tip's
// content (MindAtticTag.init({ tip })) and its inner styling; the component builds the tip on first hover,
// flips it below when there is no room above, keeps it on screen at the left/right edges and moves the
// arrow (--tt-arrow-x) so it still points at the tag. Runs on a scratch page built from this working tree.
import { test, expect } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import { SHARED_ROOT } from '../../lib/paths.mjs';

const TAG = path.join(SHARED_ROOT, 'components', 'Tag');
const CSS = fs.readFileSync(path.join(TAG, 'tag.css'), 'utf8');
const JS = fs.readFileSync(path.join(TAG, 'tag.js'), 'utf8');
const MARGIN = 10;   // tag.js default margin from the viewport edge

// Tags at fixed spots: near the top, in the middle, hard against the left and right edges, plus a tag with
// no tip content. Each tag's wrapper is absolutely positioned so the tag is where the test needs it.
const HTML = `<!doctype html><html><head><meta charset="utf-8"><style>
:root{--text:#111;--bg:#fff;--tag-bg:#eee;--tag-text:#111;--tag-border:#ccc;--tag-hover-bg:#333;--tag-hover-text:#fff;}
html,body{margin:0;height:100%;overflow:hidden;}
.at{position:absolute;}
${CSS}
.page-tip b{color:red;}
</style></head><body>
<div class="at" style="top:4px;left:380px"><span class="ma-tag" id="top" data-tip="Near the top">top</span></div>
<div class="at" style="top:400px;left:380px"><span class="ma-tag" id="mid" data-tip="In the middle">middle</span></div>
<div class="at" style="top:400px;left:2px"><span class="ma-tag" id="left" data-tip="Left edge">left</span></div>
<div class="at" style="top:400px;right:2px"><span class="ma-tag" id="right" data-tip="Right edge">right</span></div>
<div class="at" style="top:300px;left:380px"><span class="ma-tag" id="none">no tip</span></div>
<script>${JS}</script>
<script>
  window.built = [];
  window.shown = [];
  window.tags = MindAtticTag.init({
    tip: function (el) { window.built.push(el.id); return el.dataset.tip ? '<b>' + el.dataset.tip + '</b>' : ''; },
    tipClass: 'page-tip',
    onShow: function (el, tip) { window.shown.push(el.id + ':' + tip.className); },
  });
</script></body></html>`;

test.use({ viewport: { width: 800, height: 600 } });

test.beforeEach(async ({ page }) => {
  const errors = [];
  page.on('pageerror', (e) => errors.push(String(e)));
  await page.setContent(HTML);
  page.errors = errors;
});
test.afterEach(async ({ page }) => { expect(page.errors).toEqual([]); });

/** Hover a tag and measure it, its tip and the tip's arrow (the ::after triangle). */
async function hoverAndMeasure(page, id) {
  await page.hover(`#${id}`);
  return page.evaluate((id) => {
    const tag = document.getElementById(id);
    const tip = tag.querySelector('.ma-tag__tip');
    const box = (r) => ({ left: r.left, right: r.right, top: r.top, bottom: r.bottom, width: r.width });
    const t = box(tag.getBoundingClientRect());
    if (!tip) return { tag: t, tip: null };
    const after = getComputedStyle(tip, '::after');
    const tr = tip.getBoundingClientRect();
    return {
      tag: t,
      tip: box(tr),
      below: tip.classList.contains('ma-tag__tip--below'),
      display: getComputedStyle(tip).display,
      arrowVar: tip.style.getPropertyValue('--tt-arrow-x'),
      // The arrow is centred on its `left` (translateX(-50%)), so this is where it points.
      arrowX: tr.left + parseFloat(after.left),
      vw: document.documentElement.clientWidth,
    };
  }, id);
}

test('builds the tip lazily on first hover, from the content the page gives, and only once', async ({ page }) => {
  expect(await page.locator('.ma-tag__tip').count()).toBe(0);
  expect(await page.evaluate(() => window.built)).toEqual([]);

  const m = await hoverAndMeasure(page, 'mid');
  expect(m.tip).not.toBeNull();
  expect(m.display).toBe('block');
  const tip = page.locator('#mid > .ma-tag__tip');
  await expect(tip).toHaveClass('ma-tag__tip page-tip');
  await expect(tip.locator('b')).toHaveText('In the middle');
  expect(await page.locator('.ma-tag__tip').count()).toBe(1);   // only the hovered tag has one

  // Away and back: the same tip is reused, the content callback is not asked again, placement runs again.
  await page.mouse.move(5, 590);
  await expect(tip).toBeHidden();
  await page.hover('#mid');
  await expect(tip).toBeVisible();
  expect(await page.locator('#mid .ma-tag__tip').count()).toBe(1);
  expect(await page.evaluate(() => window.built)).toEqual(['mid']);
  expect(await page.evaluate(() => window.shown)).toEqual(['mid:ma-tag__tip page-tip', 'mid:ma-tag__tip page-tip']);

  // A tag the page gives no content gets no tip.
  await page.hover('#none');
  expect(await page.locator('#none .ma-tag__tip').count()).toBe(0);
});

test('sits centred above the tag when there is room, and flips below near the top edge', async ({ page }) => {
  const mid = await hoverAndMeasure(page, 'mid');
  expect(mid.below).toBe(false);
  expect(mid.tip.bottom).toBeLessThan(mid.tag.top);
  expect(Math.abs((mid.tip.left + mid.tip.right) / 2 - (mid.tag.left + mid.tag.right) / 2)).toBeLessThan(1);
  expect(mid.arrowVar).toBe('');   // arrow stays in the middle

  const top = await hoverAndMeasure(page, 'top');
  expect(top.below).toBe(true);
  expect(top.tip.top).toBeGreaterThan(top.tag.bottom);
  expect(top.tip.top).toBeGreaterThanOrEqual(0);
});

for (const edge of ['left', 'right']) {
  test(`stays on screen at the ${edge} edge, with the arrow still pointing at the tag`, async ({ page }) => {
    const m = await hoverAndMeasure(page, edge);
    expect(m.tip.left).toBeGreaterThanOrEqual(MARGIN - 0.5);
    expect(m.tip.right).toBeLessThanOrEqual(m.vw - MARGIN + 0.5);
    if (edge === 'left') expect(m.tip.left).toBeCloseTo(MARGIN, 0);
    else expect(m.tip.right).toBeCloseTo(m.vw - MARGIN, 0);

    // --tt-arrow-x is the tag centre measured from the tip's left edge, and the arrow lands on it.
    const tagCx = (m.tag.left + m.tag.right) / 2;
    expect(m.arrowVar).toMatch(/^-?[\d.]+px$/);
    expect(parseFloat(m.arrowVar)).toBeCloseTo(tagCx - m.tip.left, 0);
    expect(m.arrowX).toBeCloseTo(tagCx, 0);
    expect(m.arrowX).not.toBeCloseTo((m.tip.left + m.tip.right) / 2, 0);   // really moved off-centre
  });
}

test('a tip shifted at an edge is reset to centred when the tag moves clear of it', async ({ page }) => {
  await hoverAndMeasure(page, 'left');
  await page.mouse.move(400, 590);
  await page.evaluate(() => { document.getElementById('left').parentElement.style.left = '380px'; });
  const m = await hoverAndMeasure(page, 'left');
  expect(m.arrowVar).toBe('');
  expect(Math.abs((m.tip.left + m.tip.right) / 2 - (m.tag.left + m.tag.right) / 2)).toBeLessThan(1);
});

test('destroy() stops building and placing tips', async ({ page }) => {
  await page.evaluate(() => window.tags.destroy());
  await page.hover('#mid');
  expect(await page.locator('.ma-tag__tip').count()).toBe(0);
  expect(await page.evaluate(() => window.built)).toEqual([]);
});
