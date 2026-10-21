using System.Collections.Generic;
using MindAttic.Components.Unity.Core;
using NUnit.Framework;

namespace MindAttic.Components.Unity.Core.Tests
{
    public class NavigationGraphTests
    {
        [Test]
        public void Vertical_list_wraps_and_first_is_top()
        {
            var g = NavigationGraph.VerticalList(3);
            Assert.That(g.First, Is.EqualTo(new NavCell(0, 0)));
            Assert.That(g.Move(new NavCell(0, 0), NavDirection.Down), Is.EqualTo(new NavCell(1, 0)));
            Assert.That(g.Move(new NavCell(2, 0), NavDirection.Down), Is.EqualTo(new NavCell(0, 0)));
            Assert.That(g.Move(new NavCell(0, 0), NavDirection.Up), Is.EqualTo(new NavCell(2, 0)));
            Assert.That(g.Move(new NavCell(1, 0), NavDirection.Left), Is.EqualTo(NavCell.None));
        }

        [Test]
        public void Without_wrap_the_ends_go_nowhere()
        {
            var g = NavigationGraph.VerticalList(3, wrap: false);
            Assert.That(g.Move(new NavCell(2, 0), NavDirection.Down), Is.EqualTo(NavCell.None));
            Assert.That(g.Move(new NavCell(0, 0), NavDirection.Up), Is.EqualTo(NavCell.None));
        }

        [Test]
        public void Disabled_cells_and_empty_rows_are_skipped()
        {
            var g = new NavigationGraph(new List<bool[]>
            {
                new[] { false },        // disabled title-level item
                new[] { true },
                new bool[0],            // a label: no cells
                new[] { false },
                new[] { true },
            });
            Assert.That(g.First, Is.EqualTo(new NavCell(1, 0)));
            Assert.That(g.Move(new NavCell(1, 0), NavDirection.Down), Is.EqualTo(new NavCell(4, 0)));
            Assert.That(g.Move(new NavCell(4, 0), NavDirection.Down), Is.EqualTo(new NavCell(1, 0)));
            Assert.That(g.Move(new NavCell(1, 0), NavDirection.Up), Is.EqualTo(new NavCell(4, 0)));
        }

        [Test]
        public void Rows_of_different_widths_land_on_the_nearest_column()
        {
            // A textbox, then a [Cancel | Join] row, then a [code | copy] row, then a full-width button.
            var g = new NavigationGraph(new List<bool[]>
            {
                new[] { true },
                new[] { true, true },
                new[] { true, true },
                new[] { true },
            });
            Assert.That(g.Move(new NavCell(0, 0), NavDirection.Down), Is.EqualTo(new NavCell(1, 0)), "from a single cell, land on the row's first cell");
            Assert.That(g.Move(new NavCell(1, 1), NavDirection.Down), Is.EqualTo(new NavCell(2, 1)), "between wide rows keep the column");
            Assert.That(g.Move(new NavCell(1, 0), NavDirection.Down), Is.EqualTo(new NavCell(2, 0)));
            Assert.That(g.Move(new NavCell(2, 1), NavDirection.Down), Is.EqualTo(new NavCell(3, 0)));
            Assert.That(g.Move(new NavCell(1, 0), NavDirection.Right), Is.EqualTo(new NavCell(1, 1)));
            Assert.That(g.Move(new NavCell(1, 1), NavDirection.Right), Is.EqualTo(NavCell.None));
        }

        [Test]
        public void A_disabled_column_falls_to_its_nearest_enabled_neighbour()
        {
            var g = new NavigationGraph(new List<bool[]>
            {
                new[] { true, true, true },
                new[] { true, false, true },
            }, wrapVertical: false, wrapHorizontal: true);
            Assert.That(g.Move(new NavCell(0, 1), NavDirection.Down), Is.EqualTo(new NavCell(1, 0)), "column 1 is disabled: nearest enabled");
            Assert.That(g.Move(new NavCell(1, 0), NavDirection.Right), Is.EqualTo(new NavCell(1, 2)));
            Assert.That(g.Move(new NavCell(1, 2), NavDirection.Right), Is.EqualTo(new NavCell(1, 0)), "wraps horizontally");
        }

        [Test]
        public void Toolbar_row_moves_horizontally_only()
        {
            var g = NavigationGraph.HorizontalRow(4);
            Assert.That(g.Move(new NavCell(0, 0), NavDirection.Right), Is.EqualTo(new NavCell(0, 1)));
            Assert.That(g.Move(new NavCell(0, 3), NavDirection.Right), Is.EqualTo(NavCell.None));
            Assert.That(g.Move(new NavCell(0, 1), NavDirection.Down), Is.EqualTo(NavCell.None));
        }

        [Test]
        public void Nothing_enabled_has_no_first()
        {
            var g = new NavigationGraph(new List<bool[]> { new[] { false }, new bool[0] });
            Assert.That(g.First.IsValid, Is.False);
        }
    }

    public class MenuStackModelTests
    {
        sealed class Screen
        {
            public readonly string Name;
            public Screen(string name) { Name = name; }
            public override string ToString() => Name;
        }

        [Test]
        public void Push_pop_and_back_walk_the_stack()
        {
            var main = new Screen("main");
            var join = new Screen("join");
            var m = new MenuStackModel<Screen>();
            var changes = new List<string>();
            m.CurrentChanged += (a, b) => changes.Add((a?.Name ?? "-") + ">" + (b?.Name ?? "-"));

            m.Push(main);
            m.Push(join);
            Assert.That(m.Current, Is.SameAs(join));
            Assert.That(m.Root, Is.SameAs(main));
            Assert.That(m.Count, Is.EqualTo(2));

            Assert.That(m.Back(), Is.EqualTo(BackOutcome.Popped));
            Assert.That(m.Current, Is.SameAs(main));
            Assert.That(m.Back(), Is.EqualTo(BackOutcome.None), "the root of a main menu stays");
            Assert.That(m.IsOpen, Is.True);
            Assert.That(changes, Is.EqualTo(new[] { "->main", "main>join", "join>main" }));
        }

        [Test]
        public void Pop_refuses_the_root_unless_asked()
        {
            var m = new MenuStackModel<Screen>();
            var a = new Screen("a");
            m.Push(a);
            Assert.That(m.Pop(), Is.Null);
            Assert.That(m.Pop(allowRoot: true), Is.SameAs(a));
            Assert.That(m.Count, Is.EqualTo(0));
        }

        [Test]
        public void Pause_menu_opens_and_closes_on_back()
        {
            var pause = new Screen("pause");
            var settings = new Screen("settings");
            var m = new MenuStackModel<Screen> { AllowCloseRoot = true, OpenOnBack = true };
            m.SetRoot(pause);
            m.Close();
            Assert.That(m.Back(), Is.EqualTo(BackOutcome.Opened));
            m.Push(settings);
            Assert.That(m.Back(), Is.EqualTo(BackOutcome.Popped));
            Assert.That(m.Back(), Is.EqualTo(BackOutcome.Closed));
            Assert.That(m.IsOpen, Is.False);
            Assert.That(m.Current, Is.SameAs(pause), "closing keeps the root for next time");
        }

        [Test]
        public void A_screen_can_intercept_back()
        {
            var lobby = new Screen("lobby");
            var m = new MenuStackModel<Screen>();
            m.Push(new Screen("main"));
            m.Push(lobby);
            Assert.That(m.Back(s => s == lobby), Is.EqualTo(BackOutcome.Intercepted));
            Assert.That(m.Current, Is.SameAs(lobby));
        }

        [Test]
        public void Replace_PopToRoot_and_re_push_keep_one_copy()
        {
            var a = new Screen("a");
            var b = new Screen("b");
            var c = new Screen("c");
            var m = new MenuStackModel<Screen>();
            m.Push(a);
            m.Push(b);
            m.Replace(c);
            Assert.That(m.Screens, Is.EqualTo(new[] { a, c }));
            m.Push(b);
            m.Push(c); // moves c to the top rather than stacking a second copy
            Assert.That(m.Screens, Is.EqualTo(new[] { a, b, c }));
            Assert.That(m.PopToRoot(), Is.EqualTo(new[] { c, b }));
            Assert.That(m.Current, Is.SameAs(a));
        }

        [Test]
        public void Closed_stack_without_open_on_back_ignores_back()
        {
            var m = new MenuStackModel<Screen>();
            m.Push(new Screen("a"));
            bool? lastOpen = null;
            m.OpenChanged += o => lastOpen = o;
            m.Close();
            Assert.That(lastOpen, Is.False);
            Assert.That(m.Back(), Is.EqualTo(BackOutcome.None));
            m.Push(new Screen("b"));
            Assert.That(m.IsOpen, Is.True, "pushing opens the stack");
        }
    }

    public class FocusTrapModelTests
    {
        sealed class S { }
        sealed class T { }

        [Test]
        public void Nested_scopes_restore_in_order()
        {
            var menuButton = new T();
            var modalButton = new T();
            var m1 = new S();
            var m2 = new S();
            var trap = new FocusTrapModel<S, T>();
            trap.Enter(m1, menuButton);
            trap.Enter(m2, modalButton);
            Assert.That(trap.Top, Is.SameAs(m2));
            Assert.That(trap.Allows(m1), Is.False);
            Assert.That(trap.Exit(m2), Is.SameAs(modalButton));
            Assert.That(trap.Allows(m1), Is.True);
            Assert.That(trap.Exit(m1), Is.SameAs(menuButton));
            Assert.That(trap.Count, Is.EqualTo(0));
        }

        [Test]
        public void Closing_a_scope_underneath_hands_its_restore_target_up()
        {
            var menuButton = new T();
            var m1 = new S();
            var m2 = new S();
            var trap = new FocusTrapModel<S, T>();
            trap.Enter(m1, menuButton);
            trap.Enter(m2, new T());
            Assert.That(trap.Exit(m1), Is.Null, "focus stays in the top modal");
            Assert.That(trap.Exit(m2), Is.SameAs(menuButton), "the menu gets focus back, not the closed modal's button");
        }
    }

    public class MenuValidatorTests
    {
        [Test]
        public void A_good_menu_passes()
        {
            var menu = new MenuDefinition("Multiplayer")
                .Add(MenuItemKind.Button, "Host")
                .Add(MenuItemKind.Textbox, "Join code")
                .Add(MenuItemKind.Button, "Join")
                .Add(MenuItemKind.Text, "", focusable: false);
            Assert.That(MenuValidator.Validate(menu), Is.Empty);
        }

        [Test]
        public void Broken_rules_are_each_reported()
        {
            var menu = new MenuDefinition("")
                .Add(MenuItemKind.Button, "Join", focusable: false)
                .Add(MenuItemKind.Button, "join", focusable: false)
                .Add(MenuItemKind.Toggle, " ", focusable: false);
            var issues = MenuValidator.Validate(menu);
            Assert.That(issues, Has.Some.Contains("no title"));
            Assert.That(issues, Has.Some.Contains("two buttons"));
            Assert.That(issues, Has.Some.Contains("has no label"));
            Assert.That(issues, Has.Some.Contains("stuck"));
        }
    }

    public class ToastQueueTests
    {
        [Test]
        public void Shows_up_to_max_then_queues_and_expires_on_the_clock()
        {
            var q = new ToastQueue(maxVisible: 2);
            var shown = new List<int>();
            var hidden = new List<int>();
            q.Shown += e => shown.Add(e.Id);
            q.Hidden += e => hidden.Add(e.Id);

            var a = q.Enqueue("a", ToastKind.Info, 3, now: 0);
            var b = q.Enqueue("b", ToastKind.Success, 3, now: 1);
            var c = q.Enqueue("c", ToastKind.Error, 3, now: 1);
            Assert.That(q.Visible, Is.EqualTo(new[] { a, b }));
            Assert.That(q.PendingCount, Is.EqualTo(1));

            q.Tick(2.9);
            Assert.That(hidden, Is.Empty);
            q.Tick(3.0);
            Assert.That(hidden, Is.EqualTo(new[] { a.Id }));
            Assert.That(q.Visible, Is.EqualTo(new[] { b, c }));
            Assert.That(c.ShownAt, Is.EqualTo(3.0));
            q.Tick(10);
            Assert.That(q.Visible, Is.Empty);
            Assert.That(shown, Is.EqualTo(new[] { a.Id, b.Id, c.Id }));
        }

        [Test]
        public void Duplicate_restarts_the_timer_and_dismiss_frees_room()
        {
            var q = new ToastQueue(1);
            var a = q.Enqueue("Copied", ToastKind.Success, 2, 0);
            var again = q.Enqueue("Copied", ToastKind.Success, 2, 1.5);
            Assert.That(again, Is.SameAs(a));
            q.Tick(3);
            Assert.That(q.Visible, Has.Count.EqualTo(1), "timer restarted at 1.5");
            var b = q.Enqueue("Next", ToastKind.Info, 0, 3);
            Assert.That(q.PendingCount, Is.EqualTo(1));
            Assert.That(q.Dismiss(a.Id, 3.1), Is.True);
            Assert.That(q.Visible, Is.EqualTo(new[] { b }));
            q.Tick(1000);
            Assert.That(q.Visible, Is.EqualTo(new[] { b }), "duration 0 stays until dismissed");
            Assert.That(q.Dismiss(999, 0), Is.False);
            q.Clear();
            Assert.That(q.Visible, Is.Empty);
        }
    }
}
