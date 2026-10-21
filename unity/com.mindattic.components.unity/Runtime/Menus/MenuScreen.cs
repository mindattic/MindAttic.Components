using System;
using System.Collections.Generic;
using MindAttic.Components.Unity.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// One menu page: a centred card with a title (and a back button when it sits on a <see cref="MenuStack"/>
    /// with somewhere to go back to), then a vertical list of controls added with the <c>Add*</c> methods.
    /// Navigation is wired for you (Up/Down through the list, Left/Right inside rows, wrapping top to bottom),
    /// the first control is selected when the screen shows, and the last focused one is selected again when the
    /// player comes back to it.
    /// <code>
    /// var menu = MenuScreen.Create(canvas, "Multiplayer");
    /// menu.AddButton("Host", OnHost, ButtonVariant.Primary);
    /// var code = menu.AddTextbox("Join code", "ABCD").AsCode(6);
    /// menu.AddButton("Join", () => Join(code.Text));
    /// </code>
    /// </summary>
    [AddComponentMenu("MindAttic/UI/Menu Screen")]
    public sealed class MenuScreen : MaElement, IMaNavContainer
    {
        [SerializeField] MaPanel panel;
        [SerializeField] MaText title;
        [SerializeField] MaText subtitle;
        [SerializeField] MaIconButton backButton;
        [SerializeField] Image rule;
        [SerializeField] RectTransform items;
        [SerializeField] CanvasGroup group;
        [SerializeField] bool showBackButton = true;

        bool _navDirty = true, _selectPending;
        float _anim = 1;
        GameObject _lastSelected;

        /// <summary>The stack this screen belongs to, or null for a standalone screen.</summary>
        public MenuStack Stack { get; internal set; }

        /// <summary>Called on a back press before the stack pops; return true to swallow it (e.g. to confirm leaving a lobby).</summary>
        public Func<bool> BackHandler { get; set; }

        /// <summary>Raised each time the screen becomes the visible one.</summary>
        public event Action Shown;

        /// <summary>Raised each time the screen stops being the visible one.</summary>
        public event Action Hidden;

        /// <summary>
        /// Creates a screen under <paramref name="parent"/> (a canvas, or anything on one) and shows it, selecting
        /// its first control once you have added them. To use it on a <see cref="MenuStack"/>, create it with
        /// <see cref="MenuStack.CreateScreen"/> instead (or <see cref="MenuStack.Push(MenuScreen)"/> it).
        /// </summary>
        public static MenuScreen Create(Component parent, string title, string subtitle = null)
        {
            var s = Build(parent != null ? parent.transform : MaCanvas.Create().transform, title, subtitle);
            s._selectPending = true;
            return s;
        }

        internal static MenuScreen Build(Transform parent, string titleText, string subtitleText)
        {
            var m = MaTheme.Metrics;
            var rt = MaUi.Rect("Menu: " + MaText.Short(titleText), parent);
            MaUi.Stretch(rt);
            var s = rt.gameObject.AddComponent<MenuScreen>();
            s.group = rt.gameObject.AddComponent<CanvasGroup>();

            s.panel = MaPanel.Create(rt, PanelStyle.Surface, "Panel");
            var p = s.panel.RectTransform;
            p.anchorMin = p.anchorMax = p.pivot = new Vector2(0.5f, 0.5f);
            p.sizeDelta = new Vector2(m.MenuWidth, 0);
            MaUi.VColumn(s.panel.gameObject, m.SpaceLg - 4, new RectOffset((int)m.SpaceXxl - 8, (int)m.SpaceXxl - 8, (int)m.SpaceXl + 4, (int)m.SpaceXxl - 8), TextAnchor.UpperLeft);
            s.panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var header = MaUi.Rect("Header", s.panel.transform);
            MaUi.HRow(header.gameObject, m.SpaceMd, null, TextAnchor.MiddleLeft);
            s.backButton = MaIconButton.Create(header, MaIcon.ChevronLeft, () => s.GoBack(), "Back", ButtonVariant.Ghost);
            s.backButton.gameObject.SetActive(false);
            s.title = MaText.Create(header, titleText, TextStyle.Title);
            MaUi.Layout(s.title.gameObject, flexW: 1);

            s.subtitle = MaText.Create(s.panel, subtitleText ?? "", TextStyle.Muted);
            s.subtitle.gameObject.SetActive(!string.IsNullOrEmpty(subtitleText));

            s.rule = MaUi.Image("Rule", s.panel.transform);
            MaUi.Layout(s.rule.gameObject, minH: 2, prefH: 2, flexW: 1);

            s.items = MaUi.Rect("Items", s.panel.transform);
            MaUi.VColumn(s.items.gameObject, m.SpaceMd - 2, null, TextAnchor.UpperCenter);

            s.ApplyCurrentTheme();
            return s;
        }

        /// <summary>The title.</summary>
        public string Title
        {
            get => title.Text;
            set
            {
                title.Text = value;
                gameObject.name = "Menu: " + MaText.Short(value);
            }
        }

        /// <summary>The muted line under the title (empty hides it).</summary>
        public string Subtitle
        {
            get => subtitle.Text;
            set
            {
                subtitle.Text = value;
                subtitle.gameObject.SetActive(!string.IsNullOrEmpty(value));
            }
        }

        /// <summary>Show the back button when the stack can go back (on by default).</summary>
        public bool ShowBackButton
        {
            get => showBackButton;
            set
            {
                showBackButton = value;
                UpdateBackButton();
            }
        }

        /// <summary>Width of the card in canvas units (default <see cref="ThemeMetrics.MenuWidth"/>).</summary>
        public float Width
        {
            get => panel.RectTransform.sizeDelta.x;
            set => panel.RectTransform.sizeDelta = new Vector2(value, panel.RectTransform.sizeDelta.y);
        }

        /// <summary>The card; move or anchor it for a left-aligned main menu.</summary>
        public RectTransform Panel => panel.RectTransform;

        /// <summary>The container items are added to (a vertical layout).</summary>
        public RectTransform Items => items;

        /// <summary>The back button in the header.</summary>
        public MaIconButton BackButton => backButton;

        /// <summary>True when this screen is the one showing.</summary>
        public bool IsShowing => gameObject.activeInHierarchy;

        // ---- Adding controls -------------------------------------------------------------------------------

        /// <summary>Adds a full-width button.</summary>
        public MaButton AddButton(string label, Action onClick, ButtonVariant variant = ButtonVariant.Secondary, MaIcon icon = MaIcon.None) =>
            Track(MaButton.Create(items, label, onClick, variant, icon).Stretch());

        /// <summary>Adds a text field with <paramref name="label"/> as its caption.</summary>
        public MaTextbox AddTextbox(string label, string placeholder = "", Action<string> onSubmit = null, Action<string> onChanged = null) =>
            Track(MaTextbox.Create(items, label, placeholder, onSubmit, onChanged));

        /// <summary>Adds an on/off switch.</summary>
        public MaToggle AddToggle(string label, bool value = false, Action<bool> onChanged = null) =>
            Track(MaToggle.Create(items, label, value, onChanged));

        /// <summary>Adds a labelled dropdown.</summary>
        public MaSelect AddSelect(string label, IEnumerable<string> options, int index = 0, Action<int> onChanged = null)
        {
            var select = MaSelect.Create(null, options, index, onChanged);
            Track(MaFormRow.Create(items, label, select));
            return select;
        }

        /// <summary>Adds a labelled slider with a live readout (<paramref name="format"/>, e.g. <c>0</c> or <c>0%</c>).</summary>
        public MaRange AddRange(string label, float min, float max, float value, Action<float> onChanged = null, bool wholeNumbers = false, string format = null)
        {
            var range = MaRange.Create(null, min, max, value, onChanged, wholeNumbers);
            Track(MaFormRow.Create(items, label, range, showValue: true));
            if (format != null) range.Format = format;
            return range;
        }

        /// <summary>Adds a read-only code with a Copy button (a lobby's join code).</summary>
        public MaCodeDisplay AddCode(string label, string code) => Track(MaCodeDisplay.Create(items, label, code));

        /// <summary>Adds a list (a roster, a server browser).</summary>
        public MaList AddList(int maxVisibleRows = 6, string emptyText = "Nothing here yet") => Track(MaList.Create(items, maxVisibleRows, emptyText));

        /// <summary>Adds a progress bar.</summary>
        public MaProgressBar AddProgress(float value = 0, bool indeterminate = false) => Track(MaProgressBar.Create(items, value, indeterminate));

        /// <summary>Adds a spinner with an optional line of text beside it.</summary>
        public MaSpinner AddSpinner(string text = null)
        {
            var row = Track(MaRow.Create(items, equalWidths: false));
            row.Alignment = TextAnchor.MiddleLeft;
            var spinner = MaSpinner.Create(row, 40);
            if (!string.IsNullOrEmpty(text)) MaUi.Layout(row.AddText(text, TextStyle.Label).gameObject, flexW: 1);
            return spinner;
        }

        /// <summary>Adds text.</summary>
        public MaText AddText(string text, TextStyle style = TextStyle.Body) => Track(MaText.Create(items, text, style));

        /// <summary>Adds a section heading (accent, small caps, like the web's form-section title).</summary>
        public MaText AddSection(string title)
        {
            var t = Track(MaText.Create(items, title, TextStyle.Section));
            t.LayoutElement.minHeight = 40;
            return t;
        }

        /// <summary>Adds a row for side-by-side controls (e.g. "Cancel | Join").</summary>
        public MaRow AddRow(bool equalWidths = true) => Track(MaRow.Create(items, equalWidths));

        /// <summary>Adds a label + control row for any element.</summary>
        public MaFormRow AddFormRow(string label, MaElement control, bool showValue = false) => Track(MaFormRow.Create(items, label, control, showValue));

        /// <summary>Adds empty vertical space.</summary>
        public void AddSpace(float height = 16)
        {
            var gap = MaUi.Rect("Space", items);
            MaUi.Layout(gap.gameObject, minH: height, prefH: height);
        }

        /// <summary>Moves any element (or your own prefab's MaElement) into the list.</summary>
        public T Add<T>(T element) where T : MaElement
        {
            element.transform.SetParent(items, false);
            return Track(element);
        }

        /// <summary>Removes every item.</summary>
        public void Clear()
        {
            for (int i = items.childCount - 1; i >= 0; i--) MaUi.Destroy(items.GetChild(i).gameObject);
            MarkNavigationDirty();
        }

        T Track<T>(T element) where T : MaElement
        {
            MarkNavigationDirty();
            if (IsShowing) _selectPending = true; // selects the first item unless focus is already on this screen
            return element;
        }

        // ---- Navigation and focus ---------------------------------------------------------------------------

        /// <inheritdoc />
        public void MarkNavigationDirty() => _navDirty = true;

        /// <summary>The navigation rows: the back button (when shown), then each item's rows.</summary>
        public List<Selectable[]> NavRows()
        {
            var rows = new List<Selectable[]>();
            if (backButton != null && backButton.gameObject.activeSelf) rows.Add(new[] { (Selectable)backButton.Button });
            CollectItemRows(rows);
            return rows;
        }

        void CollectItemRows(List<Selectable[]> rows)
        {
            foreach (Transform child in items)
            {
                if (child.gameObject.activeSelf && MaElement.Of(child) is MaElement el) el.CollectNavRows(rows);
            }
        }

        /// <summary>Wires explicit navigation for every control on the screen now.</summary>
        public void RebuildNavigation()
        {
            _navDirty = false;
            MaNavigation.Wire(NavRows(), wrapVertical: true, wrapHorizontal: false);
        }

        /// <summary>The first focusable item (the back button is skipped), or null.</summary>
        public Selectable FirstSelectable
        {
            get
            {
                var rows = new List<Selectable[]>();
                CollectItemRows(rows);
                return MaNavigation.First(rows) ?? (backButton != null && backButton.gameObject.activeSelf ? backButton.Button : null);
            }
        }

        /// <summary>Rewires navigation and selects the first item.</summary>
        public void SelectFirst()
        {
            RebuildNavigation();
            _selectPending = false;
            var first = FirstSelectable;
            _lastSelected = first != null ? first.gameObject : null;
            if (MaModal.Top == null) MaEventSystem.Select(_lastSelected);
        }

        /// <summary>Rewires navigation and selects the last focused control (or the first).</summary>
        public void Focus()
        {
            RebuildNavigation();
            _selectPending = false;
            if (_lastSelected != null && _lastSelected.activeInHierarchy && MaNavigation.IsWithin(_lastSelected, transform) &&
                _lastSelected.TryGetComponent<Selectable>(out var s) && s.IsInteractable())
            {
                if (MaModal.Top == null) MaEventSystem.Select(_lastSelected);
            }
            else SelectFirst();
        }

        /// <summary>An engine-free description of the screen for <see cref="MenuValidator"/>.</summary>
        public MenuDefinition DescribeMenu()
        {
            var d = new MenuDefinition(Title);
            foreach (Transform child in items)
            {
                if (child.gameObject.activeSelf && MaElement.Of(child) is MaElement el) d.Items.Add(el.Describe());
            }
            return d;
        }

        /// <summary>Checks the screen against the MindAttic menu rules; empty when it passes.</summary>
        public IReadOnlyList<string> Validate() => MenuValidator.Validate(DescribeMenu());

        /// <summary>Asks this screen's stack to go back (the back button does this).</summary>
        public void GoBack()
        {
            if (Stack != null) Stack.Back();
            else BackHandler?.Invoke();
        }

        // ---- Showing --------------------------------------------------------------------------------------

        bool _shown;

        internal void ShowInternal(bool restoreFocus, int direction)
        {
            if (_shown && gameObject.activeInHierarchy) return;
            _shown = true;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            UpdateBackButton();
            _anim = MaTheme.ReduceMotion || !Application.isPlaying ? 1 : 0;
            _animDirection = direction;
            ApplyAnim();
            if (restoreFocus) Focus();
            else SelectFirst();
            Shown?.Invoke();
        }

        internal void HideInternal()
        {
            var sel = MaEventSystem.Selected;
            if (MaNavigation.IsWithin(sel, transform)) _lastSelected = sel;
            bool wasShown = _shown;
            _shown = false;
            gameObject.SetActive(false);
            if (wasShown) Hidden?.Invoke();
        }

        int _animDirection = 1;

        internal void UpdateBackButton()
        {
            if (backButton == null) return;
            bool show = showBackButton && Stack != null && Stack.Current == this && Stack.CanGoBack;
            if (backButton.gameObject.activeSelf != show)
            {
                backButton.gameObject.SetActive(show);
                MarkNavigationDirty();
            }
        }

        void LateUpdate()
        {
            if (!Application.isPlaying) return;
            if (_navDirty) RebuildNavigation();
            if (_selectPending)
            {
                _selectPending = false;
                if (!MaNavigation.IsWithin(MaEventSystem.Selected, transform)) SelectFirst();
            }
            if (_anim < 1)
            {
                _anim = Mathf.Min(1, _anim + Time.unscaledDeltaTime / Mathf.Max(0.01f, MaTheme.Metrics.DurationBase));
                ApplyAnim();
            }

            // Keep focus on the screen for keyboard/gamepad players: if a click on empty space cleared the
            // selection, the next frame puts it back (no modal open, this screen on top).
            if (MaModal.Top != null || (Stack != null && Stack.Current != this)) return;
            var sel = MaEventSystem.Selected;
            if (sel != null && MaNavigation.IsWithin(sel, transform)) _lastSelected = sel;
            else if (sel == null && !MaSelect.AnyExpanded && _lastSelected != null && _lastSelected.activeInHierarchy) MaEventSystem.Select(_lastSelected);
        }

        void ApplyAnim()
        {
            float e = Motion.Ease(_anim);
            group.alpha = e;
            var p = panel.RectTransform;
            p.anchoredPosition = new Vector2(MaTheme.ReduceMotion ? 0 : (1 - e) * 24f * _animDirection, 0);
        }

        /// <inheritdoc />
        public override void ApplyTheme(ThemeTokens theme)
        {
            if (rule == null) return;
            rule.color = theme.border;
        }

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();
            if (Stack == null) Stack = GetComponentInParent<MenuStack>(true);
        }
    }
}
