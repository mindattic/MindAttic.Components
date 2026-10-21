using System.Collections;
using MindAttic.Components.Unity.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MindAttic.Components.Unity.Tests
{
    public class ModalAndFeedbackTests : UiTestBase
    {
        [UnityTest]
        public IEnumerator Confirm_traps_focus_and_back_cancels_and_restores_it()
        {
            var screen = MenuScreen.Create(canvas, "Lobby");
            var leave = screen.AddButton("Leave", null);
            var other = screen.AddButton("Other", null);
            yield return null;
            MaEventSystem.Select(leave.gameObject);

            bool? result = null;
            var modal = MaModal.Confirm("Leave the lobby?", "The others keep playing.", r => result = r, "Leave", "Stay", ButtonVariant.Danger);
            yield return null;
            Assert.That(MaModal.Top, Is.SameAs(modal));
            Assert.That(IsWithin(Selected, modal.Dialog), Is.True, "focus moves into the dialog");
            Assert.That(Selected.GetComponent<MaButton>().Label, Is.EqualTo("Stay"), "a dangerous confirm starts on the safe button");

            MaEventSystem.Select(other.gameObject); // something tries to pull focus out
            yield return null;
            Assert.That(IsWithin(Selected, modal.Dialog), Is.True, "the trap puts it back");

            MaEventSystem.Select(null);
            yield return null;
            Assert.That(IsWithin(Selected, modal.Dialog), Is.True);

            var stay = Selected.GetComponent<MaButton>();
            var nav = stay.Button.navigation;
            Assert.That(nav.selectOnRight.GetComponent<MaButton>().Label, Is.EqualTo("Leave"));
            Assert.That(nav.selectOnUp, Is.SameAs(modal.CloseButton.Button), "navigation stays inside the dialog");
            Assert.That(modal.CloseButton.Button.navigation.selectOnUp, Is.SameAs(stay.Button), "and wraps inside it");

            Assert.That(screen.GetComponentInParent<Canvas>().rootCanvas.sortingOrder, Is.LessThan(MaCanvas.Overlay.sortingOrder));
            Assert.That(MaBackRouter.HandleBack(), Is.True, "Esc/B cancels the modal, not the menu");
            yield return null;
            Assert.That(result, Is.False);
            Assert.That(MaModal.Top, Is.Null);
            Assert.That(Selected, Is.EqualTo(leave.gameObject), "focus returns to where it was");
        }

        [UnityTest]
        public IEnumerator Confirm_button_answers_true_and_alert_calls_back()
        {
            bool? result = null;
            var modal = MaModal.Confirm("Host a game?", "Others can join with your code.", r => result = r, "Host");
            yield return null;
            Assert.That(Selected.GetComponent<MaButton>().Label, Is.EqualTo("Host"), "a normal confirm starts on confirm");
            Selected.GetComponent<MaButton>().Click();
            Assert.That(result, Is.True);
            Assert.That(modal.IsOpen, Is.False);

            int closed = 0;
            MaModal.Alert("Disconnected", "The host left the game.", () => closed++);
            yield return null;
            MaBackRouter.HandleBack();
            Assert.That(closed, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Nested_modals_hand_focus_back_in_order()
        {
            var screen = MenuScreen.Create(canvas, "Menu");
            var b = screen.AddButton("Open", null);
            yield return null;
            var first = MaModal.Alert("First", "...");
            yield return null;
            var inFirst = Selected;
            Assert.That(IsWithin(inFirst, first.Dialog), Is.True);
            var second = MaModal.Confirm("Second", "...", null);
            yield return null;
            Assert.That(IsWithin(Selected, second.Dialog), Is.True);
            Assert.That(MaModal.OpenCount, Is.EqualTo(2));
            second.Close();
            yield return null;
            Assert.That(Selected, Is.EqualTo(inFirst));
            first.Close();
            yield return null;
            Assert.That(Selected, Is.EqualTo(b.gameObject));
        }

        [UnityTest]
        public IEnumerator Busy_dialog_cancels_only_when_it_can()
        {
            bool cancelled = false;
            var busy = MaModal.Busy("Connecting", "Contacting the host...", () => cancelled = true);
            yield return null;
            Assert.That(busy.GetComponentInChildren<MaSpinner>(), Is.Not.Null);
            busy.SetMessage("Retrying (2/3)...");
            Assert.That(busy.Body.GetComponentInChildren<MaText>().Text, Is.EqualTo("Retrying (2/3)..."));
            Assert.That(Selected.GetComponent<MaButton>().Label, Is.EqualTo("Cancel"));
            Selected.GetComponent<MaButton>().Click();
            Assert.That(cancelled, Is.True);
            Assert.That(busy.IsOpen, Is.False);

            var saving = MaModal.Busy("Saving", "Please wait");
            yield return null;
            Assert.That(MaBackRouter.HandleBack(), Is.True, "swallowed");
            Assert.That(saving.IsOpen, Is.True, "no cancel: only code closes it");
            saving.Close();
            Assert.That(MaModal.Top, Is.Null);
        }

        [UnityTest]
        public IEnumerator Toasts_queue_dedupe_and_expire()
        {
            MaTheme.ReduceMotion = true;
            MaToast.Show("Saved", ToastKind.Success);
            yield return null;
            Assert.That(MaToast.OnScreen.Count, Is.EqualTo(1));
            Assert.That(MaToast.OnScreen[0].Message, Is.EqualTo("Saved"));
            MaToast.Show("Saved", ToastKind.Success);
            Assert.That(MaToast.OnScreen.Count, Is.EqualTo(1), "an identical toast restarts instead of stacking");
            MaToast.Show("Two");
            MaToast.Show("Three", ToastKind.Warning, "Heads up");
            MaToast.Show("Four", ToastKind.Error);
            Assert.That(MaToast.OnScreen.Count, Is.EqualTo(3), "three at most");
            MaToast.DismissAll();
            yield return null;
            Assert.That(MaToast.OnScreen.Count, Is.EqualTo(0));

            MaToast.Show("Brief", ToastKind.Info, null, 0.2f);
            yield return null;
            Assert.That(MaToast.OnScreen.Count, Is.EqualTo(1));
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(MaToast.OnScreen.Count, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Toasts_never_take_focus()
        {
            var screen = MenuScreen.Create(canvas, "Menu");
            var b = screen.AddButton("Stay focused", null);
            yield return null;
            MaToast.Show("Player joined");
            yield return null;
            Assert.That(Selected, Is.EqualTo(b.gameObject));
            Assert.That(MaToast.OnScreen[0].GetComponent<UnityEngine.UI.Selectable>(), Is.Null);
        }

        [UnityTest]
        public IEnumerator Code_display_copies_to_the_clipboard()
        {
            var code = MaCodeDisplay.Create(canvas, "Join code", "QX7P2K");
            string copied = null;
            code.Copied += c => copied = c;
            yield return null;
            code.CopyButton.Click();
            Assert.That(copied, Is.EqualTo("QX7P2K"));
            Assert.That(GUIUtility.systemCopyBuffer, Is.EqualTo("QX7P2K"));
            Assert.That(code.CopyButton.Label, Is.EqualTo("Copied"));
            Assert.That(code.CopyButton.Icon, Is.EqualTo(MaIcon.Check));
            Assert.That(MaToast.OnScreen.Count, Is.EqualTo(1));
            code.Code = "";
            Assert.That(code.CopyButton.Interactable, Is.False, "nothing to copy");
        }

        [UnityTest]
        public IEnumerator Tooltip_shows_on_demand_and_on_keyboard_focus()
        {
            var b = MaIconButton.Create(canvas, MaIcon.Copy, null, "Copy the code");
            var tip = b.GetComponent<MaTooltip>();
            Assert.That(tip, Is.Not.Null);
            tip.Show();
            Assert.That(MaTooltip.ShowingText, Is.EqualTo("Copy the code"));
            tip.Hide();
            Assert.That(MaTooltip.ShowingText, Is.Null);

            MaInput.PointerModality = false;
            MaEventSystem.Select(b.gameObject);
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(tip.IsShowing, Is.True);
            MaEventSystem.Select(null);
            Assert.That(tip.IsShowing, Is.False);

            var plain = MaButton.Create(canvas, "Plain").WithTooltip("Fluent tooltip");
            plain.GetComponent<MaTooltip>().Show();
            Assert.That(MaTooltip.ShowingText, Is.EqualTo("Fluent tooltip"));
        }
    }
}
