using System.Collections.Generic;
using MindAttic.Components.Unity.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>Something that lays out focusable rows and rewires their navigation when they change (screens, modals, toolbars).</summary>
    public interface IMaNavContainer
    {
        /// <summary>Rewire navigation on the next frame (or immediately via the container's own rebuild).</summary>
        void MarkNavigationDirty();
    }

    /// <summary>
    /// The base of every MindAttic component: it restyles itself from <see cref="MaTheme.Current"/> whenever the
    /// theme changes (also in the editor), reports its focusable controls as navigation rows to the screen or
    /// dialog it sits in, and describes itself for menu validation.
    /// </summary>
    [ExecuteAlways]
    public abstract class MaElement : MonoBehaviour
    {
        bool _subscribed;

        /// <summary>This element's RectTransform.</summary>
        public RectTransform RectTransform => (RectTransform)transform;

        /// <summary>The LayoutElement that sizes it inside its parent's layout (added on first use).</summary>
        public LayoutElement LayoutElement => MaUi.GetOrAdd<LayoutElement>(gameObject);

        /// <summary>Shown or hidden; hiding takes it out of keyboard/gamepad navigation.</summary>
        public bool Visible
        {
            get => gameObject.activeSelf;
            set
            {
                if (gameObject.activeSelf == value) return;
                gameObject.SetActive(value);
                NotifyNavigationChanged();
            }
        }

        /// <summary>The control that takes focus, or null for static elements.</summary>
        public virtual Selectable PrimarySelectable => null;

        /// <summary>Restyles the element for <paramref name="theme"/>. Called on enable and on every theme change.</summary>
        public abstract void ApplyTheme(ThemeTokens theme);

        /// <summary>
        /// Adds this element's focusable controls to <paramref name="rows"/>, one array per navigation row (a
        /// single control by default; a list adds a row per entry, a button row one row of many).
        /// </summary>
        public virtual void CollectNavRows(List<Selectable[]> rows)
        {
            var s = PrimarySelectable;
            if (s != null && s.gameObject.activeInHierarchy) rows.Add(new[] { s });
        }

        /// <summary>An engine-free description of the element for <see cref="MenuValidator"/>.</summary>
        public virtual MenuItemDefinition Describe()
        {
            var s = PrimarySelectable;
            return new MenuItemDefinition(MenuItemKind.Custom, name, s != null && s.IsInteractable());
        }

        /// <summary>Tells the enclosing screen/dialog/toolbar to rewire navigation.</summary>
        protected virtual void NotifyNavigationChanged()
        {
            var parent = transform.parent;
            if (parent == null) return;
            var c = parent.GetComponentInParent<IMaNavContainer>(true);
            c?.MarkNavigationDirty();
        }

        /// <inheritdoc cref="MonoBehaviour" />
        protected virtual void OnEnable()
        {
            if (!_subscribed)
            {
                MaTheme.Changed += HandleTheme;
                _subscribed = true;
            }
            ApplyTheme(MaTheme.Current);
        }

        /// <inheritdoc cref="MonoBehaviour" />
        protected virtual void OnDisable()
        {
            if (_subscribed)
            {
                MaTheme.Changed -= HandleTheme;
                _subscribed = false;
            }
        }

        void HandleTheme(ThemeTokens theme)
        {
            if (this != null) ApplyTheme(theme);
        }

        /// <summary>Applies the current theme now (used by every Create, so a just-built element is styled before its first frame).</summary>
        protected void ApplyCurrentTheme() => ApplyTheme(MaTheme.Current);

        /// <summary>
        /// The most specific MindAttic element on a GameObject (the last one added: a list or toolbar is built on a
        /// panel, so the GameObject carries both), or null.
        /// </summary>
        public static MaElement Of(Component c)
        {
            if (c == null) return null;
            var all = c.GetComponents<MaElement>();
            return all.Length == 0 ? null : all[all.Length - 1];
        }

        internal static Transform ResolveParent(Component parent) => parent != null ? parent.transform : MaCanvas.ModalLayer;
    }
}
