using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MindAttic.Components.Unity.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// A dialog over a dimmed backdrop (the web's <c>.ma-modal</c>): title, body, footer buttons. While open it
    /// owns keyboard/gamepad focus (a focus trap: navigation and clicks cannot leave it, and anything that
    /// deselects puts focus back inside), Esc/B cancels it when <see cref="Dismissable"/>, and closing hands focus
    /// back to whatever had it. Ready-made: <see cref="Alert"/>, <see cref="Confirm"/>, <see cref="ConfirmAsync"/>
    /// and <see cref="Busy"/> (a spinner with a Cancel button, e.g. "Connecting...").
    /// </summary>
    [AddComponentMenu("MindAttic/UI/Modal")]
    public sealed class MaModal : MaElement, IMaNavContainer
    {
        [SerializeField] Image backdrop;
        [SerializeField] MaPanel dialog;
        [SerializeField] MaText title;
        [SerializeField] MaIconButton closeButton;
        [SerializeField] RectTransform body;
        [SerializeField] MaRow footer;
        [SerializeField] CanvasGroup group;
        [SerializeField] bool dismissable = true;
        [SerializeField] bool destroyOnClose = true;
        [SerializeField] bool dismissOnBackdropClick;

        readonly List<MaElement> _bodyElements = new List<MaElement>();
        bool _open, _navDirty = true;
        float _anim;
        GameObject _lastInside;

        static readonly FocusTrapModel<MaModal, GameObject> s_trap = new FocusTrapModel<MaModal, GameObject>();
        static readonly List<MaModal> s_open = new List<MaModal>();

        /// <summary>Raised after the modal closes (any way).</summary>
        public event Action Closed;

        /// <summary>What Esc/B, the close button or a backdrop click do before closing (e.g. resolve a confirm as false).</summary>
        public Action OnCancel { get; set; }

        /// <summary>The button focused when the modal opens (default: the first footer button).</summary>
        public Selectable DefaultSelection { get; set; }

        /// <summary>
        /// Creates a closed modal under <paramref name="parent"/> (a canvas or anything on one; null puts it on the
        /// shared overlay, above every menu). Add content, then <see cref="Open"/> it.
        /// </summary>
        public static MaModal Create(string title, Component parent = null)
        {
            var m = MaTheme.Metrics;
            Transform host = parent != null ? (MaCanvas.RootOf(parent.transform) != null ? MaCanvas.RootOf(parent.transform).transform : parent.transform) : MaCanvas.ModalLayer;
            var rt = MaUi.Rect("Modal: " + MaText.Short(title), host);
            MaUi.Stretch(rt);
            var modal = rt.gameObject.AddComponent<MaModal>();
            modal.group = rt.gameObject.AddComponent<CanvasGroup>();

            modal.backdrop = rt.gameObject.AddComponent<Image>();
            modal.backdrop.raycastTarget = true; // blocks everything behind
            var backdropClick = rt.gameObject.AddComponent<MaModalBackdrop>();
            backdropClick.Modal = modal;

            modal.dialog = MaPanel.Create(rt, PanelStyle.Surface, "Dialog");
            var d = modal.dialog.RectTransform;
            d.anchorMin = d.anchorMax = d.pivot = new Vector2(0.5f, 0.5f);
            d.sizeDelta = new Vector2(m.DialogWidth, 0);
            MaUi.VColumn(modal.dialog.gameObject, m.SpaceLg, new RectOffset((int)m.SpaceXl + 4, (int)m.SpaceXl + 4, (int)m.SpaceXl, (int)m.SpaceXl), TextAnchor.UpperLeft);
            var fit = modal.dialog.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var header = MaUi.Rect("Header", modal.dialog.transform);
            MaUi.HRow(header.gameObject, m.SpaceMd, null, TextAnchor.MiddleLeft);
            modal.title = MaText.Create(header, title, TextStyle.Heading);
            modal.title.Tmp.textWrappingMode = TextWrappingModes.Normal;
            MaUi.Layout(modal.title.gameObject, flexW: 1);
            modal.closeButton = MaIconButton.Create(header, MaIcon.Close, modal.Cancel, "Close", ButtonVariant.Ghost);
            modal.closeButton.WithSize(m.IconButtonSize - 8);

            modal.body = MaUi.Rect("Body", modal.dialog.transform);
            MaUi.VColumn(modal.body.gameObject, m.SpaceMd, null, TextAnchor.UpperLeft);

            modal.footer = MaRow.Create(modal.dialog, equalWidths: false);
            modal.footer.name = "Footer";
            modal.footer.Alignment = TextAnchor.MiddleRight;
            modal.footer.gameObject.SetActive(false);

            modal.ApplyCurrentTheme();
            rt.gameObject.SetActive(false);
            return modal;
        }

        /// <summary>An OK-only message. Esc/B or OK closes it and calls <paramref name="onClose"/>.</summary>
        public static MaModal Alert(string title, string message, Action onClose = null, string ok = "OK", Component parent = null)
        {
            var m = Create(title, parent);
            m.AddMessage(message);
            m.AddButton(ok, null, ButtonVariant.Primary);
            m.OnCancel = null;
            if (onClose != null) m.Closed += onClose;
            m.Open();
            return m;
        }

        /// <summary>
        /// A yes/no question. <paramref name="onResult"/> gets true for <paramref name="confirm"/>, false for
        /// <paramref name="cancel"/>, Esc/B or the close button. A <see cref="ButtonVariant.Danger"/> confirm starts
        /// with focus on Cancel, so a stray Enter never destroys anything.
        /// </summary>
        public static MaModal Confirm(string title, string message, Action<bool> onResult, string confirm = "OK", string cancel = "Cancel",
            ButtonVariant confirmVariant = ButtonVariant.Primary, Component parent = null)
        {
            var m = Create(title, parent);
            m.AddMessage(message);
            bool answered = false;
            void Answer(bool v)
            {
                if (answered) return;
                answered = true;
                onResult?.Invoke(v);
            }
            var no = m.AddButton(cancel, () => Answer(false));
            var yes = m.AddButton(confirm, () => Answer(true), confirmVariant);
            m.OnCancel = () => Answer(false);
            m.DefaultSelection = confirmVariant == ButtonVariant.Danger ? no.Button : yes.Button;
            m.Open();
            return m;
        }

        /// <summary><see cref="Confirm"/> as a task: <c>if (await MaModal.ConfirmAsync("Leave?", "...")) Leave();</c></summary>
        public static Task<bool> ConfirmAsync(string title, string message, string confirm = "OK", string cancel = "Cancel",
            ButtonVariant confirmVariant = ButtonVariant.Primary, Component parent = null)
        {
            var tcs = new TaskCompletionSource<bool>();
            Confirm(title, message, v => tcs.TrySetResult(v), confirm, cancel, confirmVariant, parent);
            return tcs.Task;
        }

        /// <summary>
        /// A busy dialog: a spinner and <paramref name="message"/>, plus a Cancel button when
        /// <paramref name="onCancel"/> is given (Esc/B cancels too); without one it can only be closed from code.
        /// Update the text with <see cref="SetMessage"/>; close it with <see cref="Close"/>.
        /// </summary>
        public static MaModal Busy(string title, string message, Action onCancel = null, string cancel = "Cancel", Component parent = null)
        {
            var m = Create(title, parent);
            var row = MaRow.Create(m.body, equalWidths: false);
            row.Alignment = TextAnchor.MiddleLeft;
            var spinner = MaSpinner.Create(row, 44);
            spinner.name = "Spinner";
            var text = row.AddText(message, TextStyle.Body);
            text.Tmp.textWrappingMode = TextWrappingModes.Normal;
            MaUi.Layout(text.gameObject, flexW: 1);
            m._message = text;
            m._bodyElements.Add(row);
            m.closeButton.gameObject.SetActive(onCancel != null);
            if (onCancel != null)
            {
                m.AddButton(cancel, m.Cancel, ButtonVariant.Secondary, closes: false);
                m.OnCancel = onCancel;
                m.Dismissable = true;
            }
            else
            {
                m.Dismissable = false;
            }
            m.Open();
            return m;
        }

        MaText _message;

        /// <summary>The topmost open modal, or null.</summary>
        public static MaModal Top => s_open.Count > 0 ? s_open[s_open.Count - 1] : null;

        /// <summary>How many modals are open.</summary>
        public static int OpenCount => s_open.Count;

        /// <summary>True while it is open.</summary>
        public bool IsOpen => _open;

        /// <summary>Esc/B and the close button cancel it (on by default; a busy dialog without Cancel is not).</summary>
        public bool Dismissable
        {
            get => dismissable;
            set
            {
                dismissable = value;
                if (closeButton != null && !value) closeButton.gameObject.SetActive(false);
            }
        }

        /// <summary>Clicking the dimmed backdrop cancels it (off by default).</summary>
        public bool DismissOnBackdropClick
        {
            get => dismissOnBackdropClick;
            set => dismissOnBackdropClick = value;
        }

        /// <summary>Destroy the modal when it closes (on by default); off keeps it to <see cref="Open"/> again.</summary>
        public bool DestroyOnClose
        {
            get => destroyOnClose;
            set => destroyOnClose = value;
        }

        /// <summary>The title.</summary>
        public string Title
        {
            get => title.Text;
            set => title.Text = value;
        }

        /// <summary>The container body content goes in (a vertical layout).</summary>
        public RectTransform Body => body;

        /// <summary>The dialog card.</summary>
        public MaPanel Dialog => dialog;

        /// <summary>The footer buttons, left to right.</summary>
        public MaRow Footer => footer;

        /// <summary>The close (X) button in the header.</summary>
        public MaIconButton CloseButton => closeButton;

        /// <summary>Width of the dialog in canvas units.</summary>
        public float Width
        {
            get => dialog.RectTransform.sizeDelta.x;
            set => dialog.RectTransform.sizeDelta = new Vector2(value, dialog.RectTransform.sizeDelta.y);
        }

        /// <summary>Adds a paragraph to the body.</summary>
        public MaText AddMessage(string message)
        {
            var t = MaText.Create(body, message, TextStyle.Label);
            t.Tmp.textWrappingMode = TextWrappingModes.Normal;
            if (_message == null) _message = t;
            return t;
        }

        /// <summary>Replaces the first message's text (e.g. a busy dialog's progress line).</summary>
        public void SetMessage(string message)
        {
            if (_message == null) AddMessage(message);
            else _message.Text = message;
        }

        /// <summary>Moves an element into the body (a textbox, a list, a toggle...).</summary>
        public T Add<T>(T element) where T : MaElement
        {
            element.transform.SetParent(body, false);
            _bodyElements.Add(element);
            MarkNavigationDirty();
            return element;
        }

        /// <summary>Adds a footer button; when <paramref name="closes"/> it closes the modal after <paramref name="onClick"/>.</summary>
        public MaButton AddButton(string label, Action onClick, ButtonVariant variant = ButtonVariant.Secondary, bool closes = true)
        {
            footer.gameObject.SetActive(true);
            var b = footer.AddButton(label, null, variant);
            b.LayoutElement.flexibleWidth = 0;
            b.LayoutElement.minWidth = 150;
            b.OnClick.AddListener(() =>
            {
                onClick?.Invoke();
                if (closes) Close();
            });
            MarkNavigationDirty();
            return b;
        }

        /// <summary>Shows the modal and takes focus.</summary>
        public void Open()
        {
            if (_open) return;
            _open = true;
            MaBackRouter.EnsureDriver();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            s_open.Remove(this);
            s_open.Add(this);
            s_trap.Enter(this, MaEventSystem.Selected);
            _anim = MaTheme.ReduceMotion || !Application.isPlaying ? 1 : 0;
            ApplyAnim();
            RebuildNavigation();
            FocusDefault();
        }

        /// <summary>Esc/B, the close button or the backdrop: runs <see cref="OnCancel"/> and closes (when dismissable).</summary>
        public void Cancel()
        {
            if (!_open || !dismissable) return;
            var cancel = OnCancel;
            OnCancel = null;
            cancel?.Invoke();
            Close();
        }

        /// <summary>Closes the modal and gives focus back to what had it before.</summary>
        public void Close()
        {
            if (!_open) return;
            _open = false;
            s_open.Remove(this);
            var restore = s_trap.Exit(this);
            if (restore != null && restore.activeInHierarchy) MaEventSystem.Select(restore);
            else if (restore == null && Top == null && MaNavigation.IsWithin(MaEventSystem.Selected, transform)) MaEventSystem.Select(null);
            Closed?.Invoke();
            if (destroyOnClose) MaUi.Destroy(gameObject);
            else gameObject.SetActive(false);
        }

        /// <summary>Handles a back press: cancels when dismissable. Returns true (a modal always swallows back).</summary>
        public bool HandleBack()
        {
            Cancel();
            return true;
        }

        internal void BackdropClicked()
        {
            if (dismissOnBackdropClick) Cancel();
        }

        /// <inheritdoc />
        public void MarkNavigationDirty() => _navDirty = true;

        /// <summary>Wires navigation: header close button, then the body's rows, then the footer row.</summary>
        public void RebuildNavigation()
        {
            _navDirty = false;
            MaNavigation.Wire(NavRows(), wrapVertical: true, wrapHorizontal: false);
        }

        List<Selectable[]> NavRows()
        {
            var rows = new List<Selectable[]>();
            if (closeButton != null && closeButton.gameObject.activeSelf) rows.Add(new[] { (Selectable)closeButton.Button });
            foreach (Transform child in body)
            {
                if (child.gameObject.activeSelf && MaElement.Of(child) is MaElement el) el.CollectNavRows(rows);
            }
            if (footer.gameObject.activeSelf) footer.CollectNavRows(rows);
            return rows;
        }

        void FocusDefault()
        {
            var target = DefaultSelection != null && MaNavigation.IsNavigable(DefaultSelection) ? DefaultSelection : null;
            if (target == null)
            {
                var rows = new List<Selectable[]>();
                if (footer.gameObject.activeSelf) footer.CollectNavRows(rows);
                target = MaNavigation.First(rows);
            }
            if (target == null)
            {
                var rows = NavRows();
                if (rows.Count > 0 && closeButton != null && rows[0].Length == 1 && rows[0][0] == closeButton.Button) rows.RemoveAt(0);
                target = MaNavigation.First(rows) ?? (closeButton != null && closeButton.gameObject.activeSelf ? closeButton.Button : null);
            }
            _lastInside = target != null ? target.gameObject : null;
            MaEventSystem.Select(_lastInside);
        }

        void LateUpdate()
        {
            if (!_open) return;
            if (_navDirty) RebuildNavigation();
            if (_anim < 1)
            {
                _anim = Mathf.Min(1, _anim + Time.unscaledDeltaTime / Mathf.Max(0.01f, MaTheme.Metrics.DurationBase));
                ApplyAnim();
            }
            if (Top != this) return;

            // The focus trap: keep the selection inside the dialog.
            var sel = MaEventSystem.Selected;
            if (sel != null && MaNavigation.IsWithin(sel, dialog.transform)) _lastInside = sel;
            else if (!MaSelect.AnyExpanded)
            {
                if (_lastInside != null && _lastInside.activeInHierarchy && MaNavigation.IsWithin(_lastInside, dialog.transform))
                    MaEventSystem.Select(_lastInside);
                else FocusDefault();
            }
        }

        void ApplyAnim()
        {
            float e = Motion.Ease(_anim);
            group.alpha = e;
            float s = MaTheme.ReduceMotion ? 1 : Mathf.Lerp(0.96f, 1f, e);
            dialog.transform.localScale = new Vector3(s, s, 1);
        }

        /// <inheritdoc />
        public override void ApplyTheme(ThemeTokens theme)
        {
            if (backdrop == null) return;
            backdrop.color = theme.overlay;
            backdrop.sprite = null;
        }

        /// <inheritdoc />
        protected override void OnDisable()
        {
            base.OnDisable();
            if (_open && !destroyOnClose)
            {
                // Deactivated from outside: treat as closed so the trap doesn't hold a dead scope.
                _open = false;
                s_open.Remove(this);
                s_trap.Exit(this);
            }
        }

        void OnDestroy()
        {
            if (!_open) return;
            _open = false;
            s_open.Remove(this);
            var restore = s_trap.Exit(this);
            if (restore != null && restore.activeInHierarchy) MaEventSystem.Select(restore);
        }

        /// <summary>Closes every open modal (scene teardown, tests).</summary>
        public static void CloseAll()
        {
            for (int i = s_open.Count - 1; i >= 0; i--)
            {
                var m = s_open[i];
                if (m != null) m.Close();
                else s_open.RemoveAt(i);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_open.Clear();
            while (s_trap.Top != null) s_trap.Exit(s_trap.Top);
        }
    }

    /// <summary>Forwards clicks on a modal's dimmed backdrop (not on the dialog) to the modal.</summary>
    [AddComponentMenu("")]
    internal sealed class MaModalBackdrop : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
    {
        public MaModal Modal;

        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData e)
        {
            if (Modal != null && e.pointerCurrentRaycast.gameObject == gameObject) Modal.BackdropClicked();
        }
    }
}
