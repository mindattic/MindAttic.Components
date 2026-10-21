using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MindAttic.Components.Unity.Tests
{
    public class NavigationTests : UiTestBase
    {
        [UnityTest]
        public IEnumerator Menu_screen_wires_explicit_navigation_through_rows()
        {
            var s = MenuScreen.Create(canvas, "Join");
            var a = s.AddButton("Host", null);
            var tb = s.AddTextbox("Join code");
            var row = s.AddRow();
            var cancel = row.AddButton("Cancel", null);
            var join = row.AddButton("Join", null);
            var disabled = s.AddButton("Disabled", null);
            disabled.Interactable = false;
            var toggle = s.AddToggle("Remember me");
            s.RebuildNavigation();

            var nav = a.Button.navigation;
            Assert.That(nav.mode, Is.EqualTo(Navigation.Mode.Explicit));
            Assert.That(nav.selectOnDown, Is.SameAs(tb.InputField));
            Assert.That(nav.selectOnUp, Is.SameAs(toggle.Toggle), "wraps from the top to the bottom");
            Assert.That(nav.selectOnLeft, Is.Null);
            Assert.That(tb.InputField.navigation.selectOnDown, Is.SameAs(cancel.Button), "lands on the row's first cell");
            Assert.That(join.Button.navigation.selectOnUp, Is.SameAs(tb.InputField));
            Assert.That(cancel.Button.navigation.selectOnRight, Is.SameAs(join.Button));
            Assert.That(join.Button.navigation.selectOnLeft, Is.SameAs(cancel.Button));
            Assert.That(join.Button.navigation.selectOnRight, Is.Null, "no horizontal wrap in a menu");
            Assert.That(cancel.Button.navigation.selectOnDown, Is.SameAs(toggle.Toggle), "the disabled button is skipped");
            Assert.That(toggle.Toggle.navigation.selectOnDown, Is.SameAs(a.Button), "wraps from the bottom to the top");

            disabled.Interactable = true;
            yield return null; // rewired on LateUpdate
            Assert.That(cancel.Button.navigation.selectOnDown, Is.SameAs(disabled.Button));

            disabled.Visible = false;
            yield return null;
            Assert.That(cancel.Button.navigation.selectOnDown, Is.SameAs(toggle.Toggle), "hidden items are skipped");
        }

        [UnityTest]
        public IEnumerator Back_button_heads_the_screen_but_is_not_the_first_selection()
        {
            var stack = MenuStack.Create(canvas);
            var main = stack.CreateScreen("Main");
            main.AddButton("Next", null);
            var sub = stack.CreateScreen("Sub");
            var item = sub.AddButton("Item", null);
            stack.Push(main);
            stack.Push(sub);
            yield return null;
            Assert.That(Selected, Is.EqualTo(item.gameObject));
            Assert.That(item.Button.navigation.selectOnUp, Is.SameAs(sub.BackButton.Button));
            Assert.That(sub.BackButton.Button.navigation.selectOnDown, Is.SameAs(item.Button));
        }

        [UnityTest]
        public IEnumerator Roster_rows_and_their_actions_are_navigation_stops()
        {
            var s = MenuScreen.Create(canvas, "Lobby");
            var code = s.AddCode("Join code", "QX7P2K");
            var list = s.AddList(3, "Waiting for players");
            var me = list.AddRow("Ryan", Color.blue, "24 ms");
            me.AddTag("Host", TagTone.Accent);
            int clicked = 0, kicked = 0;
            var sam = list.AddRow("Sam", Color.red, "61 ms", () => clicked++);
            var kick = sam.AddAction(MaIcon.Close, () => kicked++, "Kick");
            var start = s.AddButton("Start", null, Core.ButtonVariant.Primary);
            s.RebuildNavigation();
            yield return null;

            Assert.That(Selected, Is.EqualTo(code.CopyButton.gameObject), "the copy button is the first stop");
            Assert.That(code.CopyButton.Button.navigation.selectOnDown, Is.SameAs(sam.Button), "a plain row is not a stop");
            Assert.That(sam.Button.navigation.selectOnRight, Is.SameAs(kick.Button));
            Assert.That(kick.Button.navigation.selectOnDown, Is.SameAs(start.Button));
            Assert.That(me.Swatch, Is.EqualTo((Color?)Color.blue));
            Assert.That(me.Detail, Is.EqualTo("24 ms"));

            sam.Click();
            kick.Click();
            Assert.That(clicked, Is.EqualTo(1));
            Assert.That(kicked, Is.EqualTo(1));

            list.Remove(me);
            Assert.That(list.Count, Is.EqualTo(1));
            list.Clear();
            Assert.That(list.Count, Is.EqualTo(0));
            yield return null;
            Assert.That(code.CopyButton.Button.navigation.selectOnDown, Is.SameAs(start.Button), "rewired after the roster emptied");
        }

        [UnityTest]
        public IEnumerator Standalone_toolbar_wires_left_right_with_wrap()
        {
            var bar = MaToolbar.Create(canvas);
            var menu = bar.AddIconButton(MaIcon.Menu, null, "Menu");
            bar.AddText("Score 0");
            bar.AddFlexibleSpace();
            var pause = bar.AddIconButton(MaIcon.Pause, null, "Pause");
            yield return null;
            Assert.That(menu.Button.navigation.selectOnRight, Is.SameAs(pause.Button));
            Assert.That(pause.Button.navigation.selectOnRight, Is.SameAs(menu.Button));
            Assert.That(menu.Button.navigation.selectOnDown, Is.Null);
        }
    }
}
