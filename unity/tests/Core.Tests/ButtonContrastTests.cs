using System.Linq;
using MindAttic.Components.Unity.Core;
using NUnit.Framework;

namespace MindAttic.Components.Unity.Core.Tests
{
    public class ButtonContrastTests
    {
        static double LabelContrast(ThemePalette p, ControlColors c, ThemeColor surface) =>
            Contrast.Ratio(c.Text, c.Fill.Over(surface.WithAlpha(1)));

        [TestCase("dark")]
        [TestCase("light")]
        public void The_chosen_yes_after_voting_keeps_its_on_accent_label_at_AA(string theme)
        {
            // The vote overlay's Yes is a Primary button; once the player has voted it is disabled.
            var p = ThemePalette.BuiltIn(theme);
            var chosen = ControlStyles.Button(p, ButtonVariant.Primary, ControlState.Disabled, selected: true);
            Assert.That(chosen.Fill, Is.EqualTo(p.Accent), "still the accent fill, not faded");
            Assert.That(chosen.Text, Is.EqualTo(p.OnAccent));
            Assert.That(LabelContrast(p, chosen, p.Bg), Is.GreaterThanOrEqualTo(ThemeValidator.Text));
        }

        [TestCase("dark")]
        [TestCase("light")]
        public void A_disabled_primary_label_stays_readable_on_its_faded_accent(string theme)
        {
            var p = ThemePalette.BuiltIn(theme);
            var disabled = ControlStyles.Button(p, ButtonVariant.Primary, ControlState.Disabled);
            Assert.That(disabled.Fill.A, Is.EqualTo(ControlStyles.DisabledAlpha).Within(1e-4), "still looks disabled");
            // Before the fix the label was on-accent at 45% over the 45% accent: about 2:1 in both themes.
            var old = ControlStyles.Button(p, ButtonVariant.Primary, ControlState.Normal).Faded(ControlStyles.DisabledAlpha);
            Assert.That(LabelContrast(p, old, p.Bg), Is.LessThan(ControlStyles.DisabledTextContrast), "the bug this guards");
            Assert.That(LabelContrast(p, disabled, p.Bg), Is.GreaterThanOrEqualTo(ControlStyles.DisabledTextContrast));
            Assert.That(LabelContrast(p, disabled, p.Bg2), Is.GreaterThanOrEqualTo(ControlStyles.DisabledTextContrast));
        }

        [TestCase("dark")]
        [TestCase("light")]
        public void Selected_buttons_answer_hover_while_enabled(string theme)
        {
            var p = ThemePalette.BuiltIn(theme);
            Assert.That(ControlStyles.Button(p, ButtonVariant.Primary, ControlState.Hover, true).Fill, Is.EqualTo(p.Accent2));
            Assert.That(ControlStyles.Button(p, ButtonVariant.Secondary, ControlState.Normal, true).Border, Is.EqualTo(p.Accent));
            Assert.That(ControlStyles.Button(p, ButtonVariant.Danger, ControlState.Disabled, true).Fill, Is.EqualTo(p.Danger));
        }

        [Test]
        public void Validator_reports_a_selected_label_that_drops_below_AA()
        {
            Assert.That(ThemeValidator.Validate(ThemePalette.Dark), Is.Empty);
            Assert.That(ThemeValidator.Validate(ThemePalette.Light), Is.Empty);
            var bad = ThemePalette.Dark.With(ThemeTokenNames.OnAccent, ThemeColor.Parse("#8fb4e0")).Renamed("bad");
            var issues = ThemeValidator.Validate(bad);
            Assert.That(issues.Any(i => i.Contains("selected disabled Primary button")), Is.True, string.Join("\n", issues));
        }
    }
}
