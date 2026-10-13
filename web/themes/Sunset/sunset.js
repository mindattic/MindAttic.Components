// 20 sunset background variations — #1 is the original geometric render,
// the rest are pointillist (Seurat-style) variations with dot rasterization.
// Each is a 1024×720 JPEG hosted in MindAttic.Components and served by jsDelivr.
const SUNSET_BGS = bgSet('sunset', 20);
const sunsetSeen = [];

function startSunset() {
  const idx = pickUnusedIdx(SUNSET_BGS, sunsetSeen, 'sunset');
  document.body.style.setProperty('--sunset-bg', `url("${SUNSET_BGS[idx]}")`);
  document.body.dataset.sunsetBg = String(idx + 1);
  window.currentSunsetBg = idx + 1;
  console.log(`[sunset] background #${idx + 1} of ${SUNSET_BGS.length} (${sunsetSeen.length} of ${SUNSET_BGS.length} this cycle)`);
}

