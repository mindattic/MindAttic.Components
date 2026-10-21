using System.Collections;
using MindAttic.Components.Unity.Core;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MindAttic.Components.Unity.Tests
{
    public class ConstructionTests : UiTestBase
    {
        [UnityTest]
        public IEnumerator Every_component_builds_from_code_on_one_menu_screen()
        {
            var screen = MenuScreen.Create(canvas, "Gallery", "Every control");
            var host = screen.AddButton("Host", null, ButtonVariant.Primary, MaIcon.Play);
            var toggle = screen.AddToggle("Friendly fire", true);
            var textbox = screen.AddTextbox("Join by IP", "127.0.0.1:7777");
            var select = screen.AddSelect("Region", new[] { "Europe", "Americas", "Asia" }, 1);
            var range = screen.AddRange("Volume", 0, 100, 80, null, wholeNumbers: true);
            var progress = screen.AddProgress(0.5f);
            var spinner = screen.AddSpinner("Connecting...");
            var code = screen.AddCode("Join code", "QX7P2K");
            var list = screen.AddList(4);
            list.AddRow("Ryan", new Color(0.42f, 0.64f, 0.91f), "24 ms").AddTag("Host", TagTone.Accent);
            var row = screen.AddRow();
            row.AddButton("Cancel", null);
            row.AddButton("Join", null, ButtonVariant.Primary);
            screen.AddSection("Advanced");
            screen.AddText("Body copy that wraps onto more than one line when it is long enough to need it.");
            var toolbar = MaToolbar.Create(canvas);
            toolbar.AddIconButton(MaIcon.Menu, null, "Menu");
            toolbar.AddText("HUD");
            var tag = MaTag.Create(canvas, "Ready", TagTone.Success);
            var icon = MaIconButton.Create(canvas, MaIcon.Settings, null, "Settings");

            yield return null;
            yield return null;

            Assert.That(host.Label, Is.EqualTo("Host"));
            Assert.That(host.LabelText.font, Is.Not.Null, "TMP font assigned");
            Assert.That(host.Surface.Fill.sprite, Is.Not.Null, "rounded sprite generated");
            Assert.That(host.Icon, Is.EqualTo(MaIcon.Play));
            Assert.That(toggle.Value, Is.True);
            Assert.That(textbox.Placeholder, Is.EqualTo("127.0.0.1:7777"));
            Assert.That(select.Count, Is.EqualTo(3));
            Assert.That(select.Value, Is.EqualTo(1));
            Assert.That(select.SelectedText, Is.EqualTo("Americas"));
            Assert.That(range.Value, Is.EqualTo(80));
            Assert.That(range.GetComponentInParent<MaFormRow>().ValueText.Text, Is.EqualTo("80"));
            Assert.That(progress.Value, Is.EqualTo(0.5f));
            Assert.That(spinner, Is.Not.Null);
            Assert.That(code.Code, Is.EqualTo("QX7P2K"));
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(tag.Text, Is.EqualTo("Ready"));
            Assert.That(icon.Tooltip, Is.EqualTo("Settings"));
            Assert.That(toolbar.GetComponentsInChildren<MaIconButton>().Length, Is.EqualTo(1));

            Assert.That(Selected, Is.EqualTo(host.gameObject), "the first item is selected when the screen shows");
            Assert.That(screen.Validate(), Is.Empty);
            Assert.That(screen.Panel.rect.height, Is.GreaterThan(600), "the card grows to fit its items");
        }

        [Test]
        public void Canvas_is_scaled_from_the_1920x1080_reference()
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920, 1080)));
            Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0.5f));
            Assert.That(canvas.GetComponent<GraphicRaycaster>(), Is.Not.Null);
        }

        [Test]
        public void EventSystem_uses_the_input_module_the_project_has()
        {
            var es = EventSystem.current;
            Assert.That(es, Is.Not.Null);
#if MINDATTIC_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            Assert.That(MaEventSystem.PreferredModule, Is.EqualTo("InputSystemUIInputModule"));
            Assert.That(es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(), Is.Not.Null);
#else
            Assert.That(MaEventSystem.PreferredModule, Is.EqualTo("StandaloneInputModule"));
            Assert.That(es.GetComponent<StandaloneInputModule>(), Is.Not.Null);
#endif
        }

        [Test]
        public void Button_variants_draw_their_web_colours()
        {
            var p = ThemePalette.Dark;
            var primary = MaButton.Create(canvas, "Primary", null, ButtonVariant.Primary);
            var secondary = MaButton.Create(canvas, "Secondary");
            var danger = MaButton.Create(canvas, "Danger", null, ButtonVariant.Danger);
            var ghost = MaButton.Create(canvas, "Ghost", null, ButtonVariant.Ghost);
            AssertColor(p.Accent.ToColor(), primary.Surface.TargetFill, "primary fill");
            AssertColor(p.OnAccent.ToColor(), primary.Surface.TargetText, "primary text");
            AssertColor(p.Bg2.ToColor(), secondary.Surface.TargetFill, "secondary fill");
            AssertColor(p.Border.ToColor(), secondary.Surface.TargetBorder, "secondary border");
            AssertColor(p.Danger.ToColor(), danger.Surface.TargetBorder, "danger border");
            Assert.That(ghost.Surface.TargetFill.a, Is.EqualTo(0).Within(0.01f), "ghost has no fill at rest");

            secondary.Variant = ButtonVariant.Primary;
            AssertColor(p.Accent.ToColor(), secondary.Surface.TargetFill, "variant switch");
        }

        [Test]
        public void Click_runs_handlers_only_while_interactable()
        {
            int clicks = 0;
            var b = MaButton.Create(canvas, "Go", () => clicks++).OnClicked(() => clicks += 10);
            b.Click();
            Assert.That(clicks, Is.EqualTo(11));
            b.Interactable = false;
            b.Click();
            Assert.That(clicks, Is.EqualTo(11));
            Assert.That(b.State, Is.EqualTo(ControlState.Disabled));
            Assert.That(b.Surface.TargetFill.a, Is.LessThan(0.5f), "disabled fades");
        }

        [UnityTest]
        public IEnumerator Hover_eases_unless_reduce_motion_is_on()
        {
            var b = MaButton.Create(canvas, "Hover me");
            var relay = b.GetComponent<MaStateRelay>();
            relay.OnPointerEnter(new PointerEventData(EventSystem.current));
            AssertColor(MaTheme.Palette.Bg3.ToColor(), b.Surface.TargetFill, "hover target");
            Assert.That(b.Surface.Fill.color, Is.Not.EqualTo(b.Surface.TargetFill), "still easing towards hover");
            yield return new WaitForSecondsRealtime(0.6f);
            AssertColor(b.Surface.TargetFill, b.Surface.Fill.color, "settled");

            relay.OnPointerExit(new PointerEventData(EventSystem.current));
            MaTheme.ReduceMotion = true;
            var c = MaButton.Create(canvas, "Instant");
            c.GetComponent<MaStateRelay>().OnPointerEnter(new PointerEventData(EventSystem.current));
            AssertColor(c.Surface.TargetFill, c.Surface.Fill.color, "reduce motion snaps");
            Assert.That(c.transform.localScale, Is.EqualTo(Vector3.one), "no press scale with reduce motion");
        }

        [Test]
        public void Textbox_as_code_uppercases_and_filters()
        {
            var tb = MaTextbox.Create(canvas, "Join code").AsCode(6);
            Assert.That(tb.InputField.characterLimit, Is.EqualTo(6));
            Assert.That(tb.InputField.onValidateInput("", 0, 'q'), Is.EqualTo('Q'));
            Assert.That(tb.InputField.onValidateInput("", 0, '7'), Is.EqualTo('7'));
            Assert.That(tb.InputField.onValidateInput("", 0, '-'), Is.EqualTo('\0'));
            string changed = null;
            tb.Changed += v => changed = v;
            tb.Text = "AB12";
            Assert.That(changed, Is.EqualTo("AB12"));
            Assert.That(tb.InputField.shouldActivateOnSelect, Is.False, "arrows can move past it");
        }

        [UnityTest]
        public IEnumerator Select_toggle_and_range_respond_to_left_right()
        {
            var select = MaSelect.Create(canvas, new[] { "A", "B", "C" });
            int changed = -1;
            select.ValueChanged += v => changed = v;
            select.OnMove(new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Right });
            Assert.That(select.Value, Is.EqualTo(1));
            Assert.That(changed, Is.EqualTo(1));
            select.OnMove(new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Left });
            select.OnMove(new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Left });
            Assert.That(select.Value, Is.EqualTo(0), "clamped at the first option");
            select.WrapWithArrows = true;
            select.OnMove(new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Left });
            Assert.That(select.Value, Is.EqualTo(2), "wraps when asked");

            select.Open(); // created this frame: opens on the next one
            yield return null;
            yield return null;
            Assert.That(select.IsExpanded, Is.True);
            Assert.That(GameObject.Find("Dropdown List"), Is.Not.Null);
            select.Close();
            yield return new WaitForSecondsRealtime(0.4f);

            var toggle = MaToggle.Create(canvas, "Ready", false);
            bool? value = null;
            toggle.ValueChanged += v => value = v;
            toggle.OnMove(new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Right });
            Assert.That(toggle.Value, Is.True);
            Assert.That(value, Is.True);
            toggle.OnMove(new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Left });
            Assert.That(toggle.Value, Is.False);

            var screen = MenuScreen.Create(canvas, "Audio");
            var range = screen.AddRange("Music", 0, 1, 0.5f, null, false, "0%");
            var readout = range.GetComponentInParent<MaFormRow>().ValueText;
            Assert.That(readout.Text, Is.EqualTo("50%"));
            range.Value = 0.25f;
            Assert.That(readout.Text, Is.EqualTo("25%"));
        }

        [UnityTest]
        public IEnumerator Progress_bar_and_spinner_animate_on_unscaled_time()
        {
            float timeScale = Time.timeScale;
            Time.timeScale = 0; // a paused game
            try
            {
                var bar = MaProgressBar.Create(canvas, 0);
                bar.Value = 1;
                var spinner = MaSpinner.Create(canvas);
                float a0 = spinner.Angle;
                yield return new WaitForSecondsRealtime(0.5f);
                Assert.That(bar.DisplayedValue, Is.GreaterThan(0.5f));
                Assert.That(spinner.Angle, Is.Not.EqualTo(a0));
            }
            finally
            {
                Time.timeScale = timeScale;
            }
        }
    }
}
