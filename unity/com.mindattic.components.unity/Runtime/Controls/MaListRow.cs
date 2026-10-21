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
    /// One row of a <see cref="MaList"/>: an optional colour swatch, the text, optional tags, an optional detail
    /// on the right and optional trailing icon buttons. With a click handler the whole row is a button.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class MaListRow : MaElement, ISelectHandler
    {
        [SerializeField] MaSurface surface;
        [SerializeField] Button button;
        [SerializeField] MaStateRelay relay;
        [SerializeField] Image swatch;
        [SerializeField] TextMeshProUGUI label;
        [SerializeField] TextMeshProUGUI detail;
        [SerializeField] RectTransform tags;
        [SerializeField] RectTransform actions;

        readonly List<MaIconButton> _actions = new List<MaIconButton>();
        bool _hooked;
        const float SwatchSize = 22;

        internal MaList Owner { get; set; }

        internal static MaListRow Create(Transform parent, string text, Color? swatchColor, string detailText, Action onClick)
        {
            var m = MaTheme.Metrics;
            var rt = MaUi.Rect("Row: " + MaText.Short(text), parent);
            var go = rt.gameObject;
            var r = go.AddComponent<MaListRow>();
            r.surface = MaSurface.Add(go, m.RadiusSm, focusable: onClick != null);
            r.surface.PressScale = false;
            MaUi.Layout(go, minH: m.RowHeight, prefH: m.RowHeight, flexW: 1);

            var content = MaUi.Rect("Content", rt);
            MaUi.Stretch(content);
            MaUi.GetOrAdd<LayoutElement>(content.gameObject).ignoreLayout = true;
            MaUi.HRow(content.gameObject, m.SpaceMd, MaUi.Pad(m.SpaceMd, 0), TextAnchor.MiddleLeft);

            r.swatch = MaUi.Image("Swatch", content);
            MaUi.Layout(r.swatch.gameObject, SwatchSize, SwatchSize, SwatchSize, SwatchSize, 0, 0);
            r.label = MaUi.Text(content, text, TextStyle.Body);
            MaUi.Layout(r.label.gameObject, flexW: 1);
            r.tags = MaUi.Rect("Tags", content);
            MaUi.HRow(r.tags.gameObject, m.SpaceSm, null, TextAnchor.MiddleRight);
            r.detail = MaUi.Text(content, detailText, TextStyle.Caption, "Detail");
            r.detail.alignment = TextAlignmentOptions.MidlineRight;
            r.actions = MaUi.Rect("Actions", content);
            MaUi.HRow(r.actions.gameObject, m.SpaceXs, null, TextAnchor.MiddleRight);

            if (onClick != null)
            {
                r.button = go.AddComponent<Button>();
                r.button.targetGraphic = r.surface.Fill;
                r.button.onClick.AddListener(() => onClick());
                r.relay = MaStateRelay.Attach(r.button);
            }
            r.Swatch = swatchColor;
            r.Detail = detailText;
            r.Hook();
            r.ApplyCurrentTheme();
            return r;
        }

        /// <summary>The main text.</summary>
        public string Text
        {
            get => label.text;
            set
            {
                label.text = value ?? "";
                gameObject.name = "Row: " + MaText.Short(value);
            }
        }

        /// <summary>The right-aligned detail (ping, score, status); null or empty hides it.</summary>
        public string Detail
        {
            get => detail.text;
            set
            {
                detail.text = value ?? "";
                detail.gameObject.SetActive(!string.IsNullOrEmpty(value));
            }
        }

        /// <summary>The colour swatch before the text (a player's colour); null hides it.</summary>
        public Color? Swatch
        {
            get => swatch.gameObject.activeSelf ? swatch.color : (Color?)null;
            set
            {
                swatch.gameObject.SetActive(value.HasValue);
                if (value.HasValue) swatch.color = value.Value;
            }
        }

        /// <summary>True when the whole row is clickable.</summary>
        public bool Clickable => button != null;

        /// <summary>The row's button (null when not clickable).</summary>
        public Button Button => button;

        /// <summary>The trailing action buttons.</summary>
        public IReadOnlyList<MaIconButton> Actions => _actions;

        /// <summary>Adds a tag pill after the text (e.g. "Host", "You", "Ready").</summary>
        public MaTag AddTag(string text, TagTone tone = TagTone.Neutral) => MaTag.Create(tags, text, tone);

        /// <summary>Removes every tag.</summary>
        public void ClearTags()
        {
            for (int i = tags.childCount - 1; i >= 0; i--) MaUi.Destroy(tags.GetChild(i).gameObject);
        }

        /// <summary>Adds a trailing icon button (kick, mute, delete) that is its own navigation stop.</summary>
        public MaIconButton AddAction(MaIcon icon, Action onClick, string tooltip = null, ButtonVariant variant = ButtonVariant.Ghost)
        {
            var b = MaIconButton.Create(actions, icon, onClick, tooltip, variant).WithSize(MaTheme.Metrics.RowHeight - 12);
            _actions.Add(b);
            NotifyNavigationChanged();
            return b;
        }

        /// <summary>Clicks the row from code.</summary>
        public void Click()
        {
            if (button != null && button.IsInteractable()) button.onClick.Invoke();
        }

        /// <inheritdoc />
        public override Selectable PrimarySelectable => button;

        /// <summary>The row's button (if clickable) followed by its action buttons, as one navigation row.</summary>
        public override void CollectNavRows(List<Selectable[]> rows)
        {
            var cells = new List<Selectable>();
            if (button != null) cells.Add(button);
            foreach (var a in _actions)
            {
                if (a != null && a.gameObject.activeSelf) cells.Add(a.Button);
            }
            if (cells.Count > 0) rows.Add(cells.ToArray());
        }

        /// <summary>Keeps the row in view when it takes focus.</summary>
        public void OnSelect(BaseEventData e)
        {
            if (Owner != null) Owner.ScrollIntoView(RectTransform);
        }

        /// <inheritdoc />
        public override void ApplyTheme(ThemeTokens theme)
        {
            if (surface == null) return;
            MaSprites.Apply(swatch, MaSprites.Rounded(SwatchSize / 2));
            MaUi.Style(label, TextStyle.Body, theme);
            MaUi.Style(detail, TextStyle.Caption, theme);
            Refresh(true);
        }

        /// <inheritdoc />
        public override MenuItemDefinition Describe() => new MenuItemDefinition(MenuItemKind.Button, Text, button != null && button.IsInteractable());

        /// <inheritdoc />
        protected override void OnEnable()
        {
            Hook();
            base.OnEnable();
        }

        /// <inheritdoc />
        protected override void OnDisable()
        {
            if (_hooked && relay != null) relay.Changed -= OnState;
            _hooked = false;
            base.OnDisable();
        }

        void Hook()
        {
            if (_hooked || relay == null) return;
            relay.Changed += OnState;
            _hooked = true;
        }

        void OnState() => Refresh(false);

        void Refresh(bool instant)
        {
            if (surface == null) return;
            var state = relay != null ? relay.State : ControlState.Normal;
            var c = ControlStyles.Row(MaTheme.Palette, state);
            surface.SetColors(c, relay != null && relay.ShowFocusRing, false, instant);
            MaUi.SetColor(label, (state == ControlState.Hover && Clickable ? MaTheme.Palette.Accent : MaTheme.Palette.Text).ToColor());
        }
    }
}
