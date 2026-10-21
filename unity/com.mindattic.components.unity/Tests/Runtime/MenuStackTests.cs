using System.Collections;
using MindAttic.Components.Unity.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MindAttic.Components.Unity.Tests
{
    public class MenuStackTests : UiTestBase
    {
        [UnityTest]
        public IEnumerator Push_pop_and_back_walk_the_screens_and_restore_focus()
        {
            var stack = MenuStack.Create(canvas);
            var main = stack.CreateScreen("Main");
            var settings = stack.CreateScreen("Settings");
            var host = main.AddButton("Host", null, ButtonVariant.Primary);
            var openSettings = main.AddButton("Settings", () => stack.Push(settings));
            var volume = settings.AddRange("Volume", 0, 1, 0.5f);
            MenuScreen changedTo = null;
            stack.ScreenChanged += s => changedTo = s;

            stack.Push(main);
            yield return null;
            Assert.That(stack.Current, Is.SameAs(main));
            Assert.That(main.IsShowing, Is.True);
            Assert.That(settings.IsShowing, Is.False);
            Assert.That(Selected, Is.EqualTo(host.gameObject));
            Assert.That(main.BackButton.gameObject.activeSelf, Is.False, "nothing to go back to");

            MaEventSystem.Select(openSettings.gameObject);
            openSettings.Click();
            yield return null;
            Assert.That(stack.Current, Is.SameAs(settings));
            Assert.That(changedTo, Is.SameAs(settings));
            Assert.That(main.IsShowing, Is.False);
            Assert.That(settings.BackButton.gameObject.activeSelf, Is.True);
            Assert.That(Selected, Is.EqualTo(volume.gameObject), "first item of the new screen");

            Assert.That(MaBackRouter.HandleBack(), Is.True, "Esc/B pops");
            yield return null;
            Assert.That(stack.Current, Is.SameAs(main));
            Assert.That(settings.IsShowing, Is.False);
            Assert.That(Selected, Is.EqualTo(openSettings.gameObject), "focus returns to the button that went deeper");

            Assert.That(MaBackRouter.HandleBack(), Is.False, "a main menu's root stays");
            Assert.That(stack.Current, Is.SameAs(main));
            Assert.That(stack.IsOpen, Is.True);
        }

        [UnityTest]
        public IEnumerator Built_screens_are_destroyed_when_popped_and_back_buttons_pop()
        {
            var stack = MenuStack.Create(canvas);
            var main = stack.CreateScreen("Main");
            main.AddButton("Go", null);
            stack.Push(main);
            var temp = stack.Push("Temporary", s => s.AddButton("Inside", null));
            yield return null;
            Assert.That(stack.Count, Is.EqualTo(2));
            temp.BackButton.Click();
            yield return null;
            Assert.That(stack.Count, Is.EqualTo(1));
            Assert.That(temp == null, Is.True, "a screen built by Push(title, build) is destroyed on pop");
        }

        [UnityTest]
        public IEnumerator A_screen_can_intercept_back()
        {
            var stack = MenuStack.Create(canvas);
            var main = stack.CreateScreen("Main");
            main.AddButton("Join", null);
            var lobby = stack.CreateScreen("Lobby");
            lobby.AddButton("Ready", null);
            int asked = 0;
            lobby.BackHandler = () =>
            {
                asked++;
                return true; // e.g. show "Leave the lobby?" instead
            };
            stack.Push(main);
            stack.Push(lobby);
            yield return null;
            Assert.That(stack.HandleBack(), Is.EqualTo(BackOutcome.Intercepted));
            Assert.That(asked, Is.EqualTo(1));
            Assert.That(stack.Current, Is.SameAs(lobby));
            lobby.BackHandler = null;
            Assert.That(stack.HandleBack(), Is.EqualTo(BackOutcome.Popped));
        }

        [UnityTest]
        public IEnumerator Pause_menu_opens_and_closes_on_back()
        {
            var pause = MenuStack.Create(canvas);
            var root = pause.CreateScreen("Paused");
            var resume = root.AddButton("Resume", () => pause.Close(), ButtonVariant.Primary);
            var options = pause.CreateScreen("Options");
            options.AddToggle("Subtitles", true);
            root.AddButton("Options", () => pause.Push(options));
            pause.ConfigureAsPauseMenu(root);
            int opened = 0, closed = 0;
            pause.Opened += () => opened++;
            pause.Closed += () => closed++;

            Assert.That(pause.IsOpen, Is.False);
            Assert.That(pause.Backdrop, Is.True);
            Assert.That(root.gameObject.activeInHierarchy, Is.False);

            Assert.That(MaBackRouter.HandleBack(), Is.True, "Esc opens the pause menu");
            yield return null;
            Assert.That(pause.IsOpen, Is.True);
            Assert.That(opened, Is.EqualTo(1));
            Assert.That(Selected, Is.EqualTo(resume.gameObject));
            Assert.That(root.BackButton.gameObject.activeSelf, Is.True, "the root can go back (close)");

            pause.Push(options);
            yield return null;
            Assert.That(MaBackRouter.HandleBack(), Is.True);
            Assert.That(pause.Current, Is.SameAs(root));
            Assert.That(MaBackRouter.HandleBack(), Is.True, "back at the root closes it");
            Assert.That(pause.IsOpen, Is.False);
            Assert.That(closed, Is.EqualTo(1));

            Assert.That(MaBackRouter.HandleMenu(), Is.True, "gamepad Start toggles it");
            Assert.That(pause.IsOpen, Is.True);
            resume.Click();
            Assert.That(pause.IsOpen, Is.False);
            Assert.That(closed, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator Closing_a_pause_menu_returns_to_its_root_next_time()
        {
            var pause = MenuStack.Create(canvas);
            var root = pause.CreateScreen("Paused");
            root.AddButton("Resume", null);
            var deep = pause.CreateScreen("Deep");
            deep.AddButton("Something", null);
            pause.ConfigureAsPauseMenu(root);
            pause.Open();
            pause.Push(deep);
            yield return null;
            pause.Close();
            pause.Open();
            yield return null;
            Assert.That(pause.Current, Is.SameAs(root));
            Assert.That(root.IsShowing, Is.True);
            Assert.That(deep.IsShowing, Is.False);
        }

        [UnityTest]
        public IEnumerator Standalone_screen_selects_its_first_item_and_keeps_focus()
        {
            var screen = MenuScreen.Create(canvas, "Solo");
            var title = screen.AddText("Pick one", TextStyle.Label);
            var first = screen.AddButton("First", null);
            screen.AddButton("Second", null);
            yield return null;
            Assert.That(title, Is.Not.Null);
            Assert.That(Selected, Is.EqualTo(first.gameObject));

            MaEventSystem.Select(null); // e.g. a click on empty space
            yield return null;
            yield return null;
            Assert.That(Selected, Is.EqualTo(first.gameObject), "focus comes back for keyboard/gamepad players");

            screen.AddButton("Added later", null);
            yield return null;
            Assert.That(Selected, Is.EqualTo(first.gameObject), "adding items doesn't steal focus");
        }

        [Test]
        public void Validation_reports_broken_menus()
        {
            var screen = MenuScreen.Create(canvas, "Broken");
            screen.AddButton("Same", null);
            screen.AddButton("Same", null);
            var issues = screen.Validate();
            Assert.That(issues, Has.Some.Contains("two buttons"));

            var empty = MenuScreen.Create(canvas, "Empty");
            empty.AddText("Nothing to press");
            Assert.That(empty.Validate(), Has.Some.Contains("stuck"));
            Assert.That(empty.DescribeMenu().Items, Has.Count.EqualTo(1));
        }
    }
}
