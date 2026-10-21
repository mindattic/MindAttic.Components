using System.Collections;
using MindAttic.Components.Unity.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace MindAttic.Components.Unity.Tests
{
    public class HudWidgetTests : UiTestBase
    {
        [UnityTest]
        public IEnumerator Status_pill_shows_its_kind_and_pulses_only_while_warning()
        {
            var toolbar = MaToolbar.Create(canvas);
            var pill = toolbar.Add(MaStatusPill.Create(toolbar, "Connecting...", StatusKind.Warn));
            Assert.That(pill.Pulse, Is.True, "a warning pulses by default");
            AssertColor(MaTheme.Current.warning, pill.Dot.color, "warning dot");

            bool dipped = false;
            for (float t = 0; t < MaStatusPill.PulsePeriod * 1.1f; t += Time.unscaledDeltaTime)
            {
                yield return null;
                if (pill.DotOpacity < 0.9f) dipped = true;
            }
            Assert.That(dipped, Is.True, "the dot breathes");

            pill.Set("Live", StatusKind.Ok);
            yield return null;
            Assert.That(pill.Text, Is.EqualTo("Live"));
            Assert.That(pill.Pulse, Is.False);
            Assert.That(pill.DotOpacity, Is.EqualTo(1f));
            AssertColor(MaTheme.Current.success, pill.Dot.color, "ok dot");
            AssertColor(MaTheme.Current.text, pill.LabelText.color, "the label stays in body text");

            pill.Set("Reconnecting...", StatusKind.Warn);
            MaTheme.ReduceMotion = true;
            for (int i = 0; i < 20; i++) yield return null;
            Assert.That(pill.DotOpacity, Is.EqualTo(1f), "reduce motion holds the dot still");

            MaTheme.UseLight();
            pill.Kind = StatusKind.Error;
            AssertColor(ThemePalette.Light.Danger.ToColor(), pill.Dot.color, "restyles live");
            Assert.That(pill.Describe().Label, Is.EqualTo("Reconnecting..."));
        }

        [UnityTest]
        public IEnumerator Banner_fades_in_holds_fades_out_and_queues()
        {
            var banner = MaBanner.Create(canvas);
            Assert.That(banner.IsShowing, Is.False);
            banner.Show("Level 2", "Push every block home", 0.4f);
            banner.Show("Level 3", null, 0.4f);
            Assert.That(banner.IsShowing, Is.True);
            Assert.That(banner.Title.Text, Is.EqualTo("Level 2"));
            Assert.That(banner.Subtitle.Visible, Is.True);
            Assert.That(banner.PendingCount, Is.EqualTo(1));
            Assert.That(banner.Card.GetComponent<CanvasGroup>().blocksRaycasts, Is.False, "never blocks the game");

            float peak = 0;
            float until = Time.unscaledTime + 0.4f + MaTheme.Metrics.DurationBase * 2 + 0.1f;
            while (Time.unscaledTime < until && banner.Title.Text == "Level 2")
            {
                peak = Mathf.Max(peak, banner.Opacity);
                yield return null;
            }
            Assert.That(peak, Is.EqualTo(1f).Within(0.01f), "fully shown while it holds");
            Assert.That(banner.Title.Text, Is.EqualTo("Level 3"), "the next one follows");
            Assert.That(banner.Subtitle.Visible, Is.False, "no subtitle, no line");

            banner.Show("Level 4", "Now", 5f, replace: true);
            Assert.That(banner.Title.Text, Is.EqualTo("Level 4"));
            banner.Clear();
            Assert.That(banner.IsShowing, Is.False);

            MaTheme.ReduceMotion = true;
            banner.Show("Level 5", null, 1f);
            yield return null;
            Assert.That(banner.Opacity, Is.EqualTo(1f), "reduce motion: no fade");
        }

        [UnityTest]
        public IEnumerator Minimap_fits_its_content_with_padding_and_pans_on_click()
        {
            var map = MaMinimap.Create(canvas, 320, 160, 8);
            var rt = map.RectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            // A small level far from the origin of a big world.
            map.AddRect(new MapRect(1000, 2000, 400, 200), Color.gray);
            map.AddRect(MapRect.FromCenter(1100, 2100, 40, 40), Color.red);
            map.AddPoint(9000, 9000, Color.white, 1); // a cursor far away does not rescale the map
            map.SetViewport(new MapRect(1150, 2050, 100, 100));
            map.Apply();

            Assert.That(map.ContentBounds, Is.EqualTo(new MapRect(1000, 2000, 400, 200)));
            var (lx, _) = map.Fit.ToMap(1000, 2000);
            var (rx, _) = map.Fit.ToMap(1400, 2200);
            Assert.That(lx, Is.EqualTo(8).Within(0.01f), "padding on the left");
            Assert.That(rx, Is.EqualTo(152).Within(0.01f), "padding on the right");
            Assert.That(map.PixelAt(2, 80).a, Is.EqualTo(0), "the padding stays clear");
            Color32 red = map.PixelAt(Mathf.RoundToInt(map.Fit.ToMap(1100, 2100).X), Mathf.RoundToInt(map.Fit.ToMap(1100, 2100).Y));
            Assert.That(red.r, Is.EqualTo(255));
            Assert.That(red.g, Is.EqualTo(0));
            Assert.That(map.Image.texture, Is.SameAs(map.Texture));

            Vector2 panned = Vector2.zero;
            int pans = 0;
            map.Panned += w => { panned = w; pans++; };
            yield return null;
            Vector2 centre = RectTransformUtility.WorldToScreenPoint(null, map.Image.rectTransform.TransformPoint(map.Image.rectTransform.rect.center));
            var e = new PointerEventData(EventSystem.current) { position = centre, button = PointerEventData.InputButton.Left };
            map.OnPointerDown(e);
            Assert.That(pans, Is.EqualTo(1));
            Assert.That(panned.x, Is.EqualTo(1200).Within(5f), "the map's centre is the content's centre");
            Assert.That(panned.y, Is.EqualTo(2100).Within(5f));

            map.FitTo(new MapRect(0, 0, 4000, 4000));
            map.Clear();
            map.AddRect(new MapRect(1000, 2000, 400, 200), Color.gray);
            map.Apply();
            Assert.That(map.AutoFit, Is.False);
            Assert.That(map.Fit.Scale, Is.EqualTo(144f / 4000f).Within(1e-4f), "pinned bounds win over the content");
            Assert.That(map.Draws, Is.GreaterThanOrEqualTo(3));
        }

        [UnityTest]
        public IEnumerator Hud_widgets_build_on_a_menu_screen_and_validate()
        {
            var screen = MenuScreen.Create(canvas, "HUD");
            screen.Add(MaStatusPill.Create(null, "Live", StatusKind.Ok));
            screen.Add(MaMinimap.Create(null, 200, 100));
            screen.AddButton("Show banner", null);
            yield return null;
            Assert.That(screen.Validate(), Is.Empty);
        }
    }
}
