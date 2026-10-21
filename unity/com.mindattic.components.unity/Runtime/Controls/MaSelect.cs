using System;
using System.Collections.Generic;
using MindAttic.Components.Unity.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// A dropdown (a TextMesh Pro <see cref="TMP_Dropdown"/> built and themed like the web's <c>.ma-select</c>):
    /// Enter/A or a click opens the list, arrows move through it, Esc/B closes it. While closed, Left/Right step
    /// through the options without opening it, which is how most gamepad players will use it.
    /// </summary>
    [AddComponentMenu("MindAttic/UI/Select")]
    public sealed class MaSelect : MaElement, IMoveHandler
    {
        [SerializeField] TMP_Dropdown dropdown;
        [SerializeField] MaSurface surface;
        [SerializeField] MaStateRelay relay;
        [SerializeField] TextMeshProUGUI caption;
        [SerializeField] Image arrow;
        [SerializeField] RectTransform template;
        [SerializeField] Image templateFill;
        [SerializeField] Image templateBorder;
        [SerializeField] MaSelectItem itemTemplate;
        [SerializeField] bool wrapWithArrows;

        bool _hooked;
        const int MaxVisibleItems = 6;

        /// <summary>Raised with the new index whenever the choice changes.</summary>
        public event Action<int> ValueChanged;

        /// <summary>Creates a dropdown with <paramref name="options"/> under <paramref name="parent"/>.</summary>
        public static MaSelect Create(Component parent, IEnumerable<string> options, int index = 0, Action<int> onChanged = null)
        {
            var m = MaTheme.Metrics;
            var rt = MaUi.Rect("Select", ResolveParent(parent));
            var go = rt.gameObject;
            go.SetActive(false); // TMP_Dropdown wants its template before OnEnable
            var s = go.AddComponent<MaSelect>();
            s.surface = MaSurface.Add(go, m.RadiusSm);
            s.surface.PressScale = false;
            MaUi.Layout(go, minH: m.ControlHeight, prefH: m.ControlHeight, prefW: 320, flexW: 1);

            s.caption = MaUi.Text(rt, "", TextStyle.Control, "Caption");
            MaUi.Stretch((RectTransform)s.caption.transform, m.SpaceMd + 2, 0, m.IconSize + m.SpaceMd * 2, 0);
            s.arrow = MaUi.Image("Arrow", rt);
            var ar = (RectTransform)s.arrow.transform;
            ar.anchorMin = ar.anchorMax = new Vector2(1, 0.5f);
            ar.sizeDelta = new Vector2(m.IconSize, m.IconSize);
            ar.anchoredPosition = new Vector2(-m.SpaceMd - m.IconSize / 2, 0);
            s.arrow.sprite = MaIcons.Get(MaIcon.ChevronDown);
            s.surface.AddContent(s.arrow);

            // The list template: a rounded card under the field, a masked viewport and one item row.
            s.template = MaUi.Rect("Template", rt);
            s.template.anchorMin = new Vector2(0, 0);
            s.template.anchorMax = new Vector2(1, 0);
            s.template.pivot = new Vector2(0.5f, 1);
            s.template.anchoredPosition = new Vector2(0, -m.SpaceSm);
            s.template.sizeDelta = new Vector2(0, MaxVisibleItems * m.RowHeight + m.SpaceSm * 2);
            s.templateFill = s.template.gameObject.AddComponent<Image>();
            s.templateBorder = MaUi.Image("Border", s.template);
            MaUi.Stretch((RectTransform)s.templateBorder.transform);
            var scroll = s.template.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = m.RowHeight;

            var viewport = MaUi.Rect("Viewport", s.template);
            MaUi.Stretch(viewport, m.SpaceSm, m.SpaceSm, m.SpaceSm, m.SpaceSm);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = MaUi.Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = new Vector2(0, m.RowHeight);
            scroll.viewport = viewport;
            scroll.content = content;

            var item = MaUi.Rect("Item", content);
            item.anchorMin = new Vector2(0, 0.5f);
            item.anchorMax = new Vector2(1, 0.5f);
            item.sizeDelta = new Vector2(0, m.RowHeight);
            var itemToggle = item.gameObject.AddComponent<Toggle>();
            var itemBg = MaUi.Image("Item Background", item, raycast: true);
            MaUi.Stretch((RectTransform)itemBg.transform);
            var check = MaUi.Image("Item Checkmark", item);
            var cr = (RectTransform)check.transform;
            cr.anchorMin = cr.anchorMax = new Vector2(0, 0.5f);
            cr.sizeDelta = new Vector2(m.IconSize, m.IconSize);
            cr.anchoredPosition = new Vector2(m.SpaceMd + m.IconSize / 2, 0);
            check.sprite = MaIcons.Get(MaIcon.Check);
            var itemLabel = MaUi.Text(item, "Option", TextStyle.Control, "Item Label");
            MaUi.Stretch((RectTransform)itemLabel.transform, m.SpaceMd * 2 + m.IconSize, 0, m.SpaceMd, 0);
            itemToggle.targetGraphic = itemBg;
            itemToggle.graphic = check;
            itemToggle.isOn = true;
            s.itemTemplate = item.gameObject.AddComponent<MaSelectItem>();
            s.itemTemplate.Init(itemToggle, itemBg, check, itemLabel);
            s.template.gameObject.SetActive(false);

            s.dropdown = go.AddComponent<TMP_Dropdown>();
            s.dropdown.targetGraphic = s.surface.Fill;
            s.dropdown.template = s.template;
            s.dropdown.captionText = s.caption;
            s.dropdown.itemText = itemLabel;
            s.relay = MaStateRelay.Attach(s.dropdown);
            s.SetOptions(options, index, notify: false);
            go.SetActive(true);

            if (onChanged != null) s.ValueChanged += onChanged;
            s.Hook();
            s.ApplyCurrentTheme();
            return s;
        }

        /// <summary>The chosen index.</summary>
        public int Value
        {
            get => dropdown.value;
            set => dropdown.value = value;
        }

        /// <summary>The chosen option's text, or null when there are none.</summary>
        public string SelectedText => dropdown.options.Count > 0 ? dropdown.options[dropdown.value].text : null;

        /// <summary>The number of options.</summary>
        public int Count => dropdown.options.Count;

        /// <summary>True while the list is open.</summary>
        public bool IsExpanded => dropdown != null && dropdown.IsExpanded;

        /// <summary>True while any TextMesh Pro dropdown in the scene is open (back/Esc closes it first).</summary>
        public static bool AnyExpanded
        {
            get
            {
                var sel = MaEventSystem.Selected;
                if (sel == null) return false;
                var d = sel.GetComponentInParent<TMP_Dropdown>();
                if (d != null && d.IsExpanded) return true;
                return sel.GetComponentInParent<Canvas>() is Canvas c && c.name == "Dropdown List";
            }
        }

        /// <summary>Left/Right past the ends wrap around (off by default).</summary>
        public bool WrapWithArrows
        {
            get => wrapWithArrows;
            set => wrapWithArrows = value;
        }

        /// <summary>The underlying TextMesh Pro dropdown.</summary>
        public TMP_Dropdown Dropdown => dropdown;

        /// <inheritdoc />
        public override Selectable PrimarySelectable => dropdown;

        /// <summary>Whether it can be changed and focused.</summary>
        public bool Interactable
        {
            get => dropdown.interactable;
            set
            {
                if (dropdown.interactable == value) return;
                dropdown.interactable = value;
                relay.Refresh();
                NotifyNavigationChanged();
            }
        }

        /// <summary>Replaces the options and picks <paramref name="index"/> (clamped).</summary>
        public void SetOptions(IEnumerable<string> options, int index = 0, bool notify = true)
        {
            dropdown.ClearOptions();
            var list = new List<string>();
            if (options != null) list.AddRange(options);
            dropdown.AddOptions(list);
            int i = list.Count == 0 ? 0 : Mathf.Clamp(index, 0, list.Count - 1);
            if (notify) dropdown.value = i;
            else dropdown.SetValueWithoutNotify(i);
            dropdown.RefreshShownValue();
        }

        /// <summary>Sets the index without raising <see cref="ValueChanged"/>.</summary>
        public void SetValueWithoutNotify(int index) => dropdown.SetValueWithoutNotify(index);

        /// <summary>Opens the list (on the next frame if the dropdown was created this frame and hasn't started yet).</summary>
        public void Open()
        {
            if (_started || !Application.isPlaying) dropdown.Show();
            else StartCoroutine(OpenWhenStarted());
        }

        System.Collections.IEnumerator OpenWhenStarted()
        {
            yield return null;
            dropdown.Show();
        }

        bool _started;

        void Start() => _started = true;

        /// <summary>Closes the list.</summary>
        public void Close() => dropdown.Hide();

        /// <summary>Left/Right step the choice while the list is closed.</summary>
        public void OnMove(AxisEventData e)
        {
            if (IsExpanded || !dropdown.IsInteractable() || Count == 0) return;
            int step = e.moveDir == MoveDirection.Left ? -1 : e.moveDir == MoveDirection.Right ? 1 : 0;
            if (step == 0) return;
            int next = dropdown.value + step;
            if (wrapWithArrows) next = (next + Count) % Count;
            next = Mathf.Clamp(next, 0, Count - 1);
            if (next != dropdown.value) dropdown.value = next;
        }

        /// <inheritdoc />
        public override void ApplyTheme(ThemeTokens theme)
        {
            if (surface == null) return;
            var m = theme.Metrics;
            surface.SetRadius(m.RadiusSm);
            MaUi.Style(caption, TextStyle.Control, theme);
            arrow.sprite = MaIcons.Get(MaIcon.ChevronDown);
            MaSprites.Apply(templateFill, MaSprites.Rounded(m.RadiusMd));
            templateFill.color = theme.bg;
            MaSprites.Apply(templateBorder, MaSprites.Ring(m.RadiusMd, m.BorderWidth));
            templateBorder.color = theme.border;
            if (itemTemplate != null) itemTemplate.ApplyTheme(theme);
            dropdown.alphaFadeSpeed = MaTheme.ReduceMotion ? 0f : m.DurationFast;
            Refresh(true);
        }

        /// <inheritdoc />
        public override MenuItemDefinition Describe() =>
            new MenuItemDefinition(MenuItemKind.Select, name, dropdown != null && dropdown.IsInteractable() && gameObject.activeInHierarchy);

        /// <inheritdoc />
        protected override void OnEnable()
        {
            Hook();
            base.OnEnable();
        }

        /// <inheritdoc />
        protected override void OnDisable()
        {
            if (_hooked)
            {
                relay.Changed -= OnState;
                dropdown.onValueChanged.RemoveListener(OnValue);
                _hooked = false;
            }
            base.OnDisable();
        }

        void Hook()
        {
            if (_hooked || relay == null || dropdown == null) return;
            relay.Changed += OnState;
            dropdown.onValueChanged.AddListener(OnValue);
            _hooked = true;
        }

        void OnState() => Refresh(false);
        void OnValue(int v) => ValueChanged?.Invoke(v);

        void Refresh(bool instant)
        {
            if (surface == null || relay == null) return;
            var state = relay.State;
            var colors = ControlStyles.Field(MaTheme.Palette, state, IsExpanded);
            surface.SetColors(colors, relay.ShowFocusRing, false, instant);
            MaUi.SetColor(caption, colors.Text.ToColor());
        }
    }
}
