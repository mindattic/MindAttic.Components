using System.Collections;
using MindAttic.Components.Unity.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MindAttic.Components.Unity.Tests
{
    public class ThemeTests : UiTestBase
    {
        [UnityTest]
        public IEnumerator Switching_theme_restyles_every_live_component()
        {
            var button = MaButton.Create(canvas, "Secondary");
            var panel = MaPanel.Create(canvas);
            var text = MaText.Create(canvas, "Hello");
            var toggle = MaToggle.Create(canvas, "On", true);
            var screen = MenuScreen.Create(canvas, "Menu");
            screen.AddButton("Only", null);
            yield return null;

            var dark = ThemePalette.Dark;
            AssertColor(dark.Bg.ToColor(), panel.Fill.color, "dark panel");
            AssertColor(dark.Text.ToColor(), text.Tmp.color, "dark text");
            AssertColor(dark.Bg2.ToColor(), button.Surface.TargetFill, "dark button");

            MaTheme.UseLight();
            var light = ThemePalette.Light;
            Assert.That(MaTheme.IsDark, Is.False);
            AssertColor(light.Bg.ToColor(), panel.Fill.color, "light panel");
            AssertColor(light.Text.ToColor(), text.Tmp.color, "light text");
            AssertColor(light.Bg2.ToColor(), button.Surface.TargetFill, "light button");
            AssertColor(light.Accent.ToColor(), toggle.transform.Find("Content/Track").GetComponent<UnityEngine.UI.Image>().color, "light switch track");

            MaTheme.Toggle();
            Assert.That(MaTheme.IsDark, Is.True);
            AssertColor(dark.Bg.ToColor(), panel.Fill.color, "back to dark");
        }

        [Test]
        public void Editing_the_current_theme_restyles_live()
        {
            var custom = ThemeTokens.CreateDark();
            MaTheme.Apply(custom);
            var button = MaButton.Create(canvas, "Live");
            custom[ThemeTokenNames.Bg2] = Color.magenta;
            AssertColor(Color.magenta, button.Surface.TargetFill, "edited token");
            Assert.That(custom.Palette.Bg2, Is.EqualTo(ThemeColor.Parse("#ff00ff")));
            Object.DestroyImmediate(custom);
        }

        [Test]
        public void ThemeTokens_read_web_css_with_the_same_token_names()
        {
            var t = ThemeTokens.FromCss(":root{--accent:#ff00ff;--text:#ffffff;--danger:#00ff00}", "dark");
            AssertColor(Color.magenta, t.accent, "accent");
            AssertColor(Color.white, t.text, "text");
            AssertColor(Color.green, t.danger, "unity addition read too");
            AssertColor(ThemePalette.Dark.Bg.ToColor(), t.bg, "missing tokens fall back to the built-in");
            AssertColor(Color.magenta, t["accent"], "indexer by web name");
            StringAssert.Contains("--accent:#ff00ff;", t.ToCss());
            Object.DestroyImmediate(t);
        }

        [Test]
        public void ThemeProvider_applies_its_theme_and_reduce_motion()
        {
            var light = ThemeTokens.CreateLight();
            var go = new GameObject("Provider");
            go.SetActive(false);
            var provider = go.AddComponent<ThemeProvider>();
            provider.Theme = light;
            provider.ReduceMotionSetting = true;
            go.SetActive(true);
            Assert.That(MaTheme.Current, Is.SameAs(light));
            Assert.That(MaTheme.ReduceMotion, Is.True);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(light);
        }

        [Test]
        public void Built_in_themes_pass_the_contrast_rules()
        {
            Assert.That(ThemeValidator.Validate(MaTheme.Dark.Palette), Is.Empty);
            Assert.That(ThemeValidator.Validate(MaTheme.Light.Palette), Is.Empty);
        }
    }
}
