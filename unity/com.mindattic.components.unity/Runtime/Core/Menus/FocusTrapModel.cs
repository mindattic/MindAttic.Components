using System;
using System.Collections.Generic;

namespace MindAttic.Components.Unity.Core
{
    /// <summary>
    /// Nested focus scopes (modals over modals over a menu), engine-free. Entering a scope remembers what had
    /// focus; only the top scope may hold focus; leaving a scope hands back what it should restore, even when
    /// scopes close out of order (the restore target of a scope closed underneath passes to the one above it).
    /// </summary>
    public sealed class FocusTrapModel<TScope, TTarget> where TScope : class where TTarget : class
    {
        readonly List<KeyValuePair<TScope, TTarget>> _scopes = new List<KeyValuePair<TScope, TTarget>>();

        /// <summary>The top (focus-owning) scope, or null.</summary>
        public TScope Top => _scopes.Count > 0 ? _scopes[_scopes.Count - 1].Key : null;

        /// <summary>How many scopes are open.</summary>
        public int Count => _scopes.Count;

        /// <summary>True when <paramref name="scope"/> is open.</summary>
        public bool Contains(TScope scope) => IndexOf(scope) >= 0;

        /// <summary>Opens a scope on top, remembering <paramref name="previousFocus"/> to restore when it closes.</summary>
        public void Enter(TScope scope, TTarget previousFocus)
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            if (Contains(scope)) return;
            _scopes.Add(new KeyValuePair<TScope, TTarget>(scope, previousFocus));
        }

        /// <summary>
        /// Closes a scope. Returns the target to restore focus to when it was the top scope; when a scope below the
        /// top closes, its remembered target is handed to the scope above it and null is returned (focus stays put).
        /// </summary>
        public TTarget Exit(TScope scope)
        {
            int i = IndexOf(scope);
            if (i < 0) return null;
            var restore = _scopes[i].Value;
            _scopes.RemoveAt(i);
            if (i == _scopes.Count) return restore; // was top
            var above = _scopes[i];
            _scopes[i] = new KeyValuePair<TScope, TTarget>(above.Key, restore);
            return null;
        }

        /// <summary>True when focus may sit in <paramref name="owner"/> (no scope is open, or it is the top one).</summary>
        public bool Allows(TScope owner) => _scopes.Count == 0 || ReferenceEquals(owner, Top);

        int IndexOf(TScope scope)
        {
            for (int i = 0; i < _scopes.Count; i++)
            {
                if (ReferenceEquals(_scopes[i].Key, scope)) return i;
            }
            return -1;
        }
    }
}
