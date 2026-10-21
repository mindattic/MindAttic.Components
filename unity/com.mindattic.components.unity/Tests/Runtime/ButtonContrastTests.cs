using System.Collections;
using MindAttic.Components.Unity.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MindAttic.Components.Unity.Tests
{
    public class ButtonContrastTests : UiTestBase
    {
        static double Ratio(Color text, Color fill, Color surface)
        {
            var under = Mix(fill, surface);
            return Contrast.Ratio(new ThemeColor(text.r, text.g, text.b, text.a), under);
        }

        static ThemeColor Mix(Color fill, Color surface) =>
            new ThemeColor(fill.r, fill.g, fill.b, fill.a).Over(new ThemeColor(surface.r, surface.g, surface.b, 1));

        [UnityTest]
        public IEnumerator A_vote_answer_chosen_then_disabled_keeps_its_label_readable_in_both_themes()
        {
            foreach (bool dark in new[] { true, false })
            {
                if (dark) MaTheme.UseDark(); else MaTheme.UseLight();
                var row = MaRow.Create(canvas);
                var yes = row.AddButton("Yes", null, ButtonVariant.Primary, MaIcon.Check);
                var no = row.AddButton("No", null, ButtonVariant.Danger, MaIcon.Close);
                yes.Selected = true;           // the player voted Yes
                yes.Interactable = false;
                no.Interactable = false;
                yield return null;
                yes.Surface.Snap();
                no.Surface.Snap();
                var theme = MaTheme.Current;

                AssertColor(theme.accent, yes.Surface.TargetFill, "the chosen Yes keeps its accent fill");
                AssertColor(theme.onAccent, yes.Surface.TargetText, "and its on-accent label");
                Assert.That(Ratio(yes.Surface.TargetText, yes.Surface.TargetFill, theme.bg), Is.GreaterThanOrEqualTo(ThemeValidator.Text),
                    (dark ? "dark" : "light") + ": chosen label at AA");
                Assert.That(Ratio(no.Surface.TargetText, no.Surface.TargetFill, theme.bg), Is.GreaterThanOrEqualTo(ControlStyles.DisabledTextContrast),
                    "the other answer is plainly disabled but still legible");
                Assert.That(yes.Surface.Fill.color.a, Is.EqualTo(1f).Within(0.01f));
                Assert.That(no.Surface.TargetFill.a, Is.LessThan(1f), "the other answer looks disabled");

                yes.Selected = false;
                Assert.That(yes.Surface.TargetFill.a, Is.LessThan(1f), "unselected it fades like any disabled button");
                Assert.That(Ratio(yes.Surface.TargetText, yes.Surface.TargetFill, theme.bg), Is.GreaterThanOrEqualTo(ControlStyles.DisabledTextContrast),
                    "a disabled Primary's label no longer vanishes into its faded accent");
                Object.DestroyImmediate(row.gameObject);
            }
        }
    }
}
