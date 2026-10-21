using System;
using System.Collections.Generic;

namespace MindAttic.Components.Unity.Core
{
    /// <summary>What a back press (Esc, gamepad B, a back button) did.</summary>
    public enum BackOutcome
    {
        /// <summary>Nothing: at the root of a stack that may not close, or an empty closed stack.</summary>
        None,
        /// <summary>The current screen's own back handler took it (e.g. to ask "leave the lobby?").</summary>
        Intercepted,
        /// <summary>The current screen was popped; the one under it is current again.</summary>
        Popped,
        /// <summary>The root was showing and the stack may close (an in-game pause menu), so it closed.</summary>
        Closed,
        /// <summary>The stack was closed and opens on back (an in-game pause menu), so it opened.</summary>
        Opened,
    }

    /// <summary>
    /// The screen stack behind every menu, engine-free: push a screen to go deeper, pop (or press back) to return,
    /// the bottom screen is the root. <see cref="AllowCloseRoot"/> and <see cref="OpenOnBack"/> turn it into an
    /// in-game Esc menu that back opens and closes. Generic over the screen type so the runtime stack and the
    /// tests share it.
    /// </summary>
    public sealed class MenuStackModel<TScreen> where TScreen : class
    {
        readonly List<TScreen> _screens = new List<TScreen>();
        bool _isOpen = true;

        /// <summary>Back at the root closes the stack instead of doing nothing.</summary>
        public bool AllowCloseRoot { get; set; }

        /// <summary>Back while closed opens the stack (on its root).</summary>
        public bool OpenOnBack { get; set; }

        /// <summary>Raised after the current screen changes: (previous, current); either may be null.</summary>
        public event Action<TScreen, TScreen> CurrentChanged;

        /// <summary>Raised after <see cref="IsOpen"/> changes.</summary>
        public event Action<bool> OpenChanged;

        /// <summary>The screens, root first.</summary>
        public IReadOnlyList<TScreen> Screens => _screens;

        /// <summary>How many screens are stacked.</summary>
        public int Count => _screens.Count;

        /// <summary>The top screen, or null.</summary>
        public TScreen Current => _screens.Count > 0 ? _screens[_screens.Count - 1] : null;

        /// <summary>The bottom screen, or null.</summary>
        public TScreen Root => _screens.Count > 0 ? _screens[0] : null;

        /// <summary>Whether the stack is showing. A new model is open.</summary>
        public bool IsOpen => _isOpen;

        /// <summary>True when the current screen has one under it to go back to.</summary>
        public bool CanPop => _screens.Count > 1;

        /// <summary>Pushes a screen on top (opening the stack if it was closed). A screen already in the stack is moved to the top.</summary>
        public void Push(TScreen screen)
        {
            if (screen == null) throw new ArgumentNullException(nameof(screen));
            var previous = Current;
            _screens.Remove(screen);
            _screens.Add(screen);
            SetOpen(true);
            if (!ReferenceEquals(previous, screen)) CurrentChanged?.Invoke(previous, screen);
        }

        /// <summary>Pops the top screen and returns it; refuses (returns null) at the root unless <paramref name="allowRoot"/>.</summary>
        public TScreen Pop(bool allowRoot = false)
        {
            if (_screens.Count == 0 || (_screens.Count == 1 && !allowRoot)) return null;
            var popped = Current;
            _screens.RemoveAt(_screens.Count - 1);
            CurrentChanged?.Invoke(popped, Current);
            return popped;
        }

        /// <summary>Replaces the top screen (or pushes onto an empty stack).</summary>
        public void Replace(TScreen screen)
        {
            if (screen == null) throw new ArgumentNullException(nameof(screen));
            var previous = Current;
            if (_screens.Count > 0) _screens.RemoveAt(_screens.Count - 1);
            _screens.Remove(screen);
            _screens.Add(screen);
            SetOpen(true);
            if (!ReferenceEquals(previous, screen)) CurrentChanged?.Invoke(previous, screen);
        }

        /// <summary>Pops everything above the root; returns the popped screens, top first.</summary>
        public IReadOnlyList<TScreen> PopToRoot()
        {
            var popped = new List<TScreen>();
            if (_screens.Count <= 1) return popped;
            var previous = Current;
            for (int i = _screens.Count - 1; i >= 1; i--)
            {
                popped.Add(_screens[i]);
                _screens.RemoveAt(i);
            }
            CurrentChanged?.Invoke(previous, Current);
            return popped;
        }

        /// <summary>Empties the stack and makes <paramref name="root"/> its only screen.</summary>
        public void SetRoot(TScreen root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            var previous = Current;
            _screens.Clear();
            _screens.Add(root);
            SetOpen(true);
            if (!ReferenceEquals(previous, root)) CurrentChanged?.Invoke(previous, root);
        }

        /// <summary>Removes every screen.</summary>
        public void Clear()
        {
            var previous = Current;
            _screens.Clear();
            if (previous != null) CurrentChanged?.Invoke(previous, null);
        }

        /// <summary>Shows the stack.</summary>
        public void Open() => SetOpen(true);

        /// <summary>Hides the stack (its screens stay).</summary>
        public void Close() => SetOpen(false);

        /// <summary>
        /// Handles a back press. Order: a closed stack opens when <see cref="OpenOnBack"/> (else nothing); the
        /// current screen's <paramref name="intercept"/> may take it; a screen with one under it pops; the root
        /// closes the stack when <see cref="AllowCloseRoot"/>.
        /// </summary>
        public BackOutcome Back(Func<TScreen, bool> intercept = null)
        {
            if (!_isOpen)
            {
                if (OpenOnBack && _screens.Count > 0)
                {
                    SetOpen(true);
                    return BackOutcome.Opened;
                }
                return BackOutcome.None;
            }
            var current = Current;
            if (current == null) return BackOutcome.None;
            if (intercept != null && intercept(current)) return BackOutcome.Intercepted;
            if (_screens.Count > 1)
            {
                Pop();
                return BackOutcome.Popped;
            }
            if (AllowCloseRoot)
            {
                SetOpen(false);
                return BackOutcome.Closed;
            }
            return BackOutcome.None;
        }

        void SetOpen(bool open)
        {
            if (_isOpen == open) return;
            _isOpen = open;
            OpenChanged?.Invoke(open);
        }
    }
}
