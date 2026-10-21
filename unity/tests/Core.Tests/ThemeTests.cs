using System.IO;
using System.Linq;
using MindAttic.Components.Unity.Core;
using NUnit.Framework;

namespace MindAttic.Components.Unity.Core.Tests
{
    public class ThemeColorTests
    {
        [TestCase("#fff", 255, 255, 255, 255)]
        [TestCase("#0d0f12", 13, 15, 18, 255)]
        [TestCase("#6BA3E8", 107, 163, 232, 255)]
        [TestCase("#11223380", 17, 34, 51, 128)]
        [TestCase("#f008", 255, 0, 0, 136)]
        [TestCase("rgba(0,0,0,0.7)", 0, 0, 0, 179)]
        [TestCase("rgb(27, 79, 140)", 27, 79, 140, 255)]
        [TestCase("rgba(128 128 128 / 20%)", 128, 128, 128, 51)]
        [TestCase("white", 255, 255, 255, 255)]
        [TestCase("transparent", 0, 0, 0, 0)]
        public void Parses_css_colours(string css, int r, int g, int b, int a)
        {
            Assert.That(ThemeColor.Parse(css), Is.EqualTo(ThemeColor.FromBytes(r, g, b, a)));
        }

        [TestCase("")]
        [TestCase("#12")]
        [TestCase("#zzzzzz")]
        [TestCase("linear-gradient(180deg, #15181e 0%, #0b0e14 100%)")]
        [TestCase("var(--accent)")]
        [TestCase(null)]
        public void Rejects_what_is_not_a_plain_colour(string css)
        {
            Assert.That(ThemeColor.TryParse(css, out _), Is.False);
        }

        [Test]
        public void Hex_round_trips()
        {
            Assert.That(ThemeColor.Parse("#6ba3e8").ToHex(), Is.EqualTo("#6ba3e8"));
            Assert.That(ThemeColor.Parse("rgba(0,0,0,0.5)").ToHex(), Is.EqualTo("#00000080"));
        }

        [Test]
        public void Lerp_and_over_composite()
        {
            var black = ThemeColor.Black;
            Assert.That(black.Lerp(ThemeColor.White, 0.5f).ToHex(), Is.EqualTo("#808080"));
            Assert.That(ThemeColor.White.WithAlpha(0.5f).Over(black).ToHex(), Is.EqualTo("#808080"));
        }
    }

    public class CssTokenParserTests
    {
        static string ThemesDir
        {
            get
            {
                var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
                while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "web", "themes"))) dir = dir.Parent;
                Assert.That(dir, Is.Not.Null, "web/themes not found above the test directory");
                return Path.Combine(dir.FullName, "web", "themes");
            }
        }

        [Test]
        public void Built_in_dark_matches_web_dark_css_token_for_token()
        {
            string css = File.ReadAllText(Path.Combine(ThemesDir, "Dark", "dark.css"));
            var tokens = CssTokenParser.ReadTheme(css, "dark");
            foreach (string name in ThemeTokenNames.Web)
            {
                Assert.That(tokens.ContainsKey(name), name + " missing from dark.css");
                Assert.That(ThemeColor.Parse(tokens[name]), Is.EqualTo(ThemePalette.Dark[name]), name);
            }
            Assert.That(tokens[ThemeTokenNames.Font], Does.Contain("Outfit"));
        }

        [Test]
        public void Built_in_light_matches_web_light_css_token_for_token()
        {
            string css = File.ReadAllText(Path.Combine(ThemesDir, "Light", "light.css"));
            var tokens = CssTokenParser.ReadTheme(css, "light");
            foreach (string name in ThemeTokenNames.Web)
            {
                Assert.That(tokens.ContainsKey(name), name + " missing from light.css");
                Assert.That(ThemeColor.Parse(tokens[name]), Is.EqualTo(ThemePalette.Light[name]), name);
            }
        }

        [Test]
        public void Every_web_theme_with_a_palette_parses_into_a_full_palette()
        {
            int parsed = 0;
            foreach (string file in Directory.GetFiles(ThemesDir, "*.css", SearchOption.AllDirectories))
            {
                string css = File.ReadAllText(file);
                foreach (string name in CssTokenParser.ThemeNames(css))
                {
                    var tokens = CssTokenParser.ReadTheme(css, name);
                    if (!ThemeTokenNames.Web.All(tokens.ContainsKey)) continue; // e.g. a theme that only adds effects
                    var palette = ThemePalette.FromTokens(name, tokens);
                    Assert.That(palette.Colors.Count(), Is.EqualTo(ThemeTokenNames.All.Count), file + " " + name);
                    parsed++;
                }
            }
            Assert.That(parsed, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void Reads_root_and_data_theme_rules_and_skips_comments_media_and_ordinary_properties()
        {
            const string css = @"/* palette */
:root { --bg:#000; --accent: #123456 ; --font-resume:'Outfit',system-ui,sans-serif; color: red; }
[data-theme=""neon""] { --bg: #111111; --accent: rgba(10, 20, 30, 0.5); }
html[data-theme='neon'] body { background: linear-gradient(180deg, #000 0%, #fff 100%); }
@media (prefers-reduced-motion: reduce) { :root { --accent:#ffffff; } }
html[data-theme=""neon""] { --text:#eeeeee; }";
            var dark = CssTokenParser.ReadTheme(css, "dark");
            Assert.That(dark["bg"], Is.EqualTo("#000"));
            Assert.That(dark["accent"], Is.EqualTo("#123456"), "@media block must not override");
            Assert.That(dark.ContainsKey("color"), Is.False);
            Assert.That(dark["font-resume"], Is.EqualTo("'Outfit',system-ui,sans-serif"));

            var neon = CssTokenParser.ReadTheme(css, "NEON");
            Assert.That(neon["bg"], Is.EqualTo("#111111"));
            Assert.That(neon["text"], Is.EqualTo("#eeeeee"));
            Assert.That(CssTokenParser.ThemeNames(css), Is.EquivalentTo(new[] { "dark", "neon" }));
        }

        [Test]
        public void Missing_tokens_fall_back_to_the_built_in_of_matching_darkness()
        {
            var light = ThemePalette.FromCss("[data-theme=\"paper\"]{--bg:#fffff0;--accent:#004488;}", "paper");
            Assert.That(light.Name, Is.EqualTo("paper"));
            Assert.That(light.Bg, Is.EqualTo(ThemeColor.Parse("#fffff0")));
            Assert.That(light.Accent, Is.EqualTo(ThemeColor.Parse("#004488")));
            Assert.That(light.Text, Is.EqualTo(ThemePalette.Light.Text));
            Assert.That(light.IsDark, Is.False);

            var dark = ThemePalette.FromCss("[data-theme=\"cave\"]{--bg:#050505;}", "cave");
            Assert.That(dark.Text, Is.EqualTo(ThemePalette.Dark.Text));
            Assert.That(dark.IsDark, Is.True);
        }

        [Test]
        public void ToCss_round_trips_through_the_parser()
        {
            foreach (var p in new[] { ThemePalette.Dark, ThemePalette.Light })
            {
                var back = ThemePalette.FromCss(p.ToCss(), p.Name);
                foreach (var kv in p.Colors) Assert.That(back[kv.Key], Is.EqualTo(kv.Value), p.Name + " " + kv.Key);
            }
        }

        [Test]
        public void With_changes_one_token_and_leaves_the_original()
        {
            var red = ThemePalette.Dark.With(ThemeTokenNames.Accent, ThemeColor.Parse("#ff0000"));
            Assert.That(red.Accent, Is.EqualTo(ThemeColor.Parse("#ff0000")));
            Assert.That(ThemePalette.Dark.Accent, Is.EqualTo(ThemeColor.Parse("#6ba3e8")));
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => ThemePalette.Dark.With("nope", ThemeColor.Black));
        }
    }

    public class ContrastTests
    {
        [Test]
        public void Black_on_white_is_21_to_1()
        {
            Assert.That(Contrast.Ratio(ThemeColor.Black, ThemeColor.White), Is.EqualTo(21).Within(0.01));
            Assert.That(Contrast.Ratio(ThemeColor.White, ThemeColor.White), Is.EqualTo(1).Within(0.01));
        }

        [Test]
        public void Both_built_in_themes_pass_validation()
        {
            Assert.That(ThemeValidator.Validate(ThemePalette.Dark), Is.Empty);
            Assert.That(ThemeValidator.Validate(ThemePalette.Light), Is.Empty);
        }

        [Test]
        public void A_low_contrast_palette_is_reported()
        {
            var bad = ThemePalette.Dark.With(ThemeTokenNames.Text, ThemeColor.Parse("#222222"));
            var issues = ThemeValidator.Validate(bad);
            Assert.That(issues, Has.Some.Contains("--text on --bg"));
        }
    }

    public class ControlStyleTests
    {
        [Test]
        public void Secondary_mirrors_ma_tool_btn()
        {
            var p = ThemePalette.Dark;
            var rest = ControlStyles.Button(p, ButtonVariant.Secondary, ControlState.Normal);
            Assert.That(rest.Fill, Is.EqualTo(p.Bg2));
            Assert.That(rest.Border, Is.EqualTo(p.Border));
            Assert.That(rest.Text, Is.EqualTo(p.Text2));
            var hover = ControlStyles.Button(p, ButtonVariant.Secondary, ControlState.Hover);
            Assert.That(hover.Fill, Is.EqualTo(p.Bg3));
            Assert.That(hover.Border, Is.EqualTo(p.Accent));
            Assert.That(hover.Text, Is.EqualTo(p.Text));
        }

        [Test]
        public void Primary_is_an_accent_fill_with_readable_text_in_both_themes()
        {
            foreach (var p in new[] { ThemePalette.Dark, ThemePalette.Light })
            {
                foreach (var s in new[] { ControlState.Normal, ControlState.Hover, ControlState.Pressed })
                {
                    var c = ControlStyles.Button(p, ButtonVariant.Primary, s);
                    Assert.That(Contrast.Ratio(c.Text, c.Fill), Is.GreaterThanOrEqualTo(4.5), p.Name + " " + s);
                }
            }
        }

        [Test]
        public void Every_variant_keeps_text_readable_in_every_enabled_state()
        {
            foreach (var p in new[] { ThemePalette.Dark, ThemePalette.Light })
            foreach (ButtonVariant v in System.Enum.GetValues(typeof(ButtonVariant)))
            foreach (var s in new[] { ControlState.Normal, ControlState.Hover, ControlState.Pressed })
            {
                var c = ControlStyles.Button(p, v, s);
                var fill = c.Fill.Over(p.Bg); // a ghost button's fill is see-through
                Assert.That(Contrast.Ratio(c.Text, fill), Is.GreaterThanOrEqualTo(4.5), p.Name + " " + v + " " + s);
            }
        }

        [Test]
        public void Disabled_fades_every_colour()
        {
            var normal = ControlStyles.Button(ThemePalette.Dark, ButtonVariant.Primary, ControlState.Normal);
            var off = ControlStyles.Button(ThemePalette.Dark, ButtonVariant.Primary, ControlState.Disabled);
            Assert.That(off.Fill.A, Is.EqualTo(normal.Fill.A * ControlStyles.DisabledAlpha).Within(0.01));
            Assert.That(off.Border.A, Is.EqualTo(normal.Border.A * ControlStyles.DisabledAlpha).Within(0.01));
            // The label fades too, but only as far as it still reads at 3:1 on the faded fill (M.COMP-T47).
            Assert.That(off.Text.A, Is.GreaterThanOrEqualTo(normal.Text.A * ControlStyles.DisabledAlpha - 0.01).And.LessThan(1f));
        }

        [Test]
        public void Field_border_lights_in_the_accent_on_hover_and_while_editing()
        {
            var p = ThemePalette.Light;
            Assert.That(ControlStyles.Field(p, ControlState.Normal, false).Border, Is.EqualTo(p.Border));
            Assert.That(ControlStyles.Field(p, ControlState.Hover, false).Border, Is.EqualTo(p.Accent));
            Assert.That(ControlStyles.Field(p, ControlState.Normal, true).Border, Is.EqualTo(p.Accent));
        }

        [Test]
        public void Toast_kinds_map_to_status_tokens()
        {
            var p = ThemePalette.Dark;
            Assert.That(ControlStyles.Toast(p, ToastKind.Info), Is.EqualTo(p.Accent));
            Assert.That(ControlStyles.Toast(p, ToastKind.Success), Is.EqualTo(p.Success));
            Assert.That(ControlStyles.Toast(p, ToastKind.Warning), Is.EqualTo(p.Warning));
            Assert.That(ControlStyles.Toast(p, ToastKind.Error), Is.EqualTo(p.Danger));
        }
    }

    public class MotionTests
    {
        [Test]
        public void Ease_matches_css_standard_curve_endpoints_and_midpoint()
        {
            Assert.That(Motion.Ease(0), Is.EqualTo(0));
            Assert.That(Motion.Ease(1), Is.EqualTo(1));
            // cubic-bezier(0.4,0,0.2,1) at x=0.5 is about 0.774
            Assert.That(Motion.Ease(0.5f), Is.EqualTo(0.774f).Within(0.01f));
            float prev = 0;
            for (int i = 1; i <= 20; i++)
            {
                float v = Motion.Ease(i / 20f);
                Assert.That(v, Is.GreaterThanOrEqualTo(prev));
                prev = v;
            }
        }

        [Test]
        public void Reduce_motion_makes_everything_instant()
        {
            Assert.That(Motion.Duration(0.28f, true), Is.EqualTo(0));
            Assert.That(Motion.Progress(0, 0.28f, true), Is.EqualTo(1));
            Assert.That(Motion.Approach(0.001f, 0.15f, true), Is.EqualTo(1));
            Assert.That(Motion.Progress(0, 0.28f, false), Is.EqualTo(0));
            Assert.That(Motion.Progress(0.28f, 0.28f, false), Is.EqualTo(1));
            Assert.That(Motion.Approach(0.016f, 0.15f, false), Is.InRange(0.01f, 0.99f));
        }
    }
}
