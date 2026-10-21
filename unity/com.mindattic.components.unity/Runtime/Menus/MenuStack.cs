using System;
using System.Collections.Generic;
using MindAttic.Components.Unity.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// A stack of <see cref="MenuScreen"/>s, the core of a game's menus: push a screen to go deeper, Esc/B or a
    /// screen's back button pops it, and the screen underneath comes back with its focus where it was. A main
    /// menu's root stays put; an in-game pause menu (<see cref="ConfigureAsPauseMenu"/>) opens on Esc/Start and
    /// closes from its root.
    /// <code>
    /// var stack = MenuStack.Create();
    /// var main = stack.CreateScreen("My Game");
    /// main.AddButton("Multiplayer", () => stack.Push("Multiplayer", s => { s.AddButton("Host", Host); }));
    /// main.AddButton("Quit", Application.Quit, ButtonVariant.Danger);
    /// stack.Push(main);
    /// </code>
    /// </summary>
    [AddComponentMenu("MindAttic/UI/Menu Stack")]
    public sealed class MenuStack : MaElement
    {
        [SerializeField] Image backdrop;
        [SerializeField] RectTransform screens;
        [SerializeField] bool escapeEnabled = true;

        readonly MenuStackModel<MenuScreen> _model = new MenuStackModel<MenuScreen>();
        readonly HashSet<MenuScreen> _destroyOnPop = new HashSet<MenuScreen>();
        bool _hooked;

        /// <summary>Raised after the visible screen changes (the new one, or null).</summary>
        public event Action<MenuScreen> ScreenChanged;

        /// <summary>Raised after the stack opens.</summary>
        public event Action Opened;

        /// <summary>Raised after the stack closes.</summary>
        public event Action Closed;

        /// <summary>
        /// Creates a full-screen stack under <paramref name="parent"/>; with no parent it gets its own MindAttic
        /// canvas (1920x1080 reference, sorting order <paramref name="sortingOrder"/>). A <paramref name="backdrop"/>
        /// dims the game behind it (pause menus).
        /// </summary>
        public static MenuStack Create(Component parent = null, bool backdrop = false, int sortingOrder = 100)
        {
            Transform host = parent != null ? parent.transform : MaCanvas.Create("Menu Canvas", sortingOrder).transform;
            var rt = MaUi.Rect("Menu Stack", host);
            MaUi.Stretch(rt);
            var stack = rt.gameObject.AddComponent<MenuStack>();
            stack.backdrop = MaUi.Image("Backdrop", rt, raycast: true);
            MaUi.Stretch((RectTransform)stack.backdrop.transform);
            stack.backdrop.gameObject.SetActive(backdrop);
            stack.screens = MaUi.Rect("Screens", rt);
            MaUi.Stretch(stack.screens);
            stack.Hook();
            stack.ApplyCurrentTheme();
            MaEventSystem.Ensure();
            return stack;
        }

        /// <summary>
        /// Makes this an in-game pause menu: dimmed backdrop, Esc/B or Start opens it on its root, back from the
        /// root closes it, and it starts closed. Pass the root screen (or push it before calling this, since pushing
        /// opens a stack). Listen to <see cref="Opened"/>/<see cref="Closed"/> to pause the game.
        /// </summary>
        public MenuStack ConfigureAsPauseMenu(MenuScreen root = null)
        {
            if (root != null) SetRoot(root);
            Backdrop = true;
            _model.AllowCloseRoot = true;
            _model.OpenOnBack = true;
            Close();
            return this;
        }

        /// <summary>Creates a screen owned by this stack (hidden until pushed).</summary>
        public MenuScreen CreateScreen(string title, string subtitle = null)
        {
            var s = MenuScreen.Build(screens, title, subtitle);
            s.Stack = this;
            s.gameObject.SetActive(false);
            return s;
        }

        /// <summary>Shows <paramref name="screen"/> on top (moving it under this stack if it was elsewhere).</summary>
        public MenuScreen Push(MenuScreen screen)
        {
            if (screen == null) throw new ArgumentNullException(nameof(screen));
            if (screen.transform.parent != screens) screen.transform.SetParent(screens, false);
            screen.Stack = this;
            _model.Push(screen);
            return screen;
        }

        /// <summary>Creates a screen, lets <paramref name="build"/> fill it, and pushes it; it is destroyed when popped.</summary>
        public MenuScreen Push(string title, Action<MenuScreen> build)
        {
            var s = CreateScreen(title);
            build?.Invoke(s);
            _destroyOnPop.Add(s);
            return Push(s);
        }

        /// <summary>Pops the top screen (never the root) and returns it, or null at the root.</summary>
        public MenuScreen Pop() => _model.Pop();

        /// <summary>Replaces the top screen.</summary>
        public void Replace(MenuScreen screen)
        {
            if (screen.transform.parent != screens) screen.transform.SetParent(screens, false);
            screen.Stack = this;
            _model.Replace(screen);
        }

        /// <summary>Pops everything above the root.</summary>
        public void PopToRoot() => _model.PopToRoot();

        /// <summary>Makes <paramref name="root"/> the only screen.</summary>
        public void SetRoot(MenuScreen root)
        {
            if (root.transform.parent != screens) root.transform.SetParent(screens, false);
            root.Stack = this;
            var old = new List<MenuScreen>(_model.Screens);
            _model.SetRoot(root);
            foreach (var s in old)
            {
                if (s != root && s != null) s.HideInternal();
            }
        }

        /// <summary>Handles a back press as Esc/B would; true when it did something.</summary>
        public bool Back() => HandleBack() != BackOutcome.None;

        /// <summary>Handles a back press and says what happened.</summary>
        public BackOutcome HandleBack() => _model.Back(s => s.BackHandler != null && s.BackHandler());

        /// <summary>The visible screen, or null.</summary>
        public MenuScreen Current => _model.Current;

        /// <summary>The bottom screen, or null.</summary>
        public MenuScreen Root => _model.Root;

        /// <summary>How many screens are stacked.</summary>
        public int Count => _model.Count;

        /// <summary>The screens, root first.</summary>
        public IReadOnlyList<MenuScreen> Screens => _model.Screens;

        /// <summary>True when there is a screen to go back to.</summary>
        public bool CanGoBack => _model.CanPop || (_model.AllowCloseRoot && _model.Count > 0);

        /// <summary>Whether the stack is showing.</summary>
        public bool IsOpen => _model.IsOpen;

        /// <summary>Back at the root closes the stack (pause menus).</summary>
        public bool AllowCloseRoot
        {
            get => _model.AllowCloseRoot;
            set => _model.AllowCloseRoot = value;
        }

        /// <summary>Esc/B or Start while closed opens the stack (pause menus).</summary>
        public bool OpenOnBack
        {
            get => _model.OpenOnBack;
            set => _model.OpenOnBack = value;
        }

        /// <summary>Esc/B reach this stack (on by default; turn off while gameplay owns Esc).</summary>
        public bool EscapeEnabled
        {
            get => escapeEnabled;
            set => escapeEnabled = value;
        }

        /// <summary>Dim the scene behind the menus.</summary>
        public bool Backdrop
        {
            get => backdrop.gameObject.activeSelf;
            set => backdrop.gameObject.SetActive(value);
        }

        /// <summary>Shows the stack (its current screen).</summary>
        public void Open() => _model.Open();

        /// <summary>Hides the stack, keeping its screens.</summary>
        public void Close() => _model.Close();

        /// <summary>Opens a closed stack or closes an open one.</summary>
        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        /// <inheritdoc />
        public override void ApplyTheme(ThemeTokens theme)
        {
            if (backdrop != null) backdrop.color = theme.overlay;
        }

        /// <inheritdoc />
        protected override void OnEnable()
        {
            Hook();
            base.OnEnable();
            MaBackRouter.Register(this);
        }

        /// <inheritdoc />
        protected override void OnDisable()
        {
            MaBackRouter.Unregister(this);
            base.OnDisable();
        }

        void Hook()
        {
            if (_hooked) return;
            _hooked = true;
            _model.CurrentChanged += OnCurrentChanged;
            _model.OpenChanged += OnOpenChanged;
        }

        void OnCurrentChanged(MenuScreen previous, MenuScreen current)
        {
            bool popped = previous != null && !Contains(previous);
            if (previous != null && previous != current) previous.HideInternal();
            if (current != null && _model.IsOpen) current.ShowInternal(restoreFocus: popped, direction: popped ? -1 : 1);
            foreach (var s in _model.Screens) s.UpdateBackButton();
            if (popped && _destroyOnPop.Remove(previous)) MaUi.Destroy(previous.gameObject);
            ScreenChanged?.Invoke(current);
        }

        bool Contains(MenuScreen s)
        {
            foreach (var x in _model.Screens)
            {
                if (x == s) return true;
            }
            return false;
        }

        void OnOpenChanged(bool open)
        {
            screens.gameObject.SetActive(open);
            if (backdrop != null) backdrop.enabled = open;
            if (open)
            {
                var cur = _model.Current;
                if (cur != null) cur.ShowInternal(restoreFocus: false, direction: 1);
                Opened?.Invoke();
            }
            else
            {
                if (_model.Current != null) _model.Current.HideInternal();
                if (MaNavigation.IsWithin(MaEventSystem.Selected, transform)) MaEventSystem.Select(null);
                // Reopening a pause menu starts from its root.
                if (_model.AllowCloseRoot) _model.PopToRoot();
                Closed?.Invoke();
            }
        }
    }
}
