using System;
using MindAttic.Components.Unity.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// A themed button: <see cref="ButtonVariant.Primary"/> (accent fill), <see cref="ButtonVariant.Secondary"/>
    /// (the default raised button), <see cref="ButtonVariant.Danger"/> or <see cref="ButtonVariant.Ghost"/>, with an
    /// optional icon before the label. A stock uGUI <see cref="UnityEngine.UI.Button"/> underneath, so
    /// <see cref="OnClick"/> works in the inspector and from code.
    /// </summary>
    [AddComponentMenu("MindAttic/UI/Button")]
    public class MaButton : MaElement
    {
        [SerializeField] protected Button button;
        [SerializeField] protected MaSurface surface;
        [SerializeField] protected MaStateRelay relay;
        [SerializeField] protected TextMeshProUGUI label;
        [SerializeField] protected Image icon;
        [SerializeField] ButtonVariant variant = ButtonVariant.Secondary;
        [SerializeField] MaIcon iconId = MaIcon.None;
        [SerializeField] Sprite iconSprite;

        /// <summary>Creates a button under <paramref name="parent"/>.</summary>
        public static MaButton Create(Component parent, string label, Action onClick = null,
            ButtonVariant variant = ButtonVariant.Secondary, MaIcon icon = MaIcon.None)
        {
            var b = Build<MaButton>(ResolveParent(parent), label, variant, icon, iconOnly: false);
            if (onClick != null) b.button.onClick.AddListener(() => onClick());
            return b;
        }

        /// <summary>Shared builder for buttons and icon buttons.</summary>
        protected static T Build<T>(Transform parent, string text, ButtonVariant variant, MaIcon iconId, bool iconOnly) where T : MaButton
        {
            var m = MaTheme.Metrics;
            var rt = MaUi.Rect("Button: " + MaText.Short(text), parent);
            var go = rt.gameObject;
            var b = go.AddComponent<T>();
            b.variant = variant;
            b.iconId = iconId;
            b.surface = MaSurface.Add(go, m.RadiusSm);
            b.button = go.AddComponent<Button>();
            b.button.targetGraphic = b.surface.Fill;
            b.relay = MaStateRelay.Attach(b.button);

            var content = MaUi.Rect("Content", rt);
            MaUi.Stretch(content);
            MaUi.HRow(content.gameObject, m.SpaceSm + 4, iconOnly ? new RectOffset() : MaUi.Pad(m.SpaceLg, 0), TextAnchor.MiddleCenter);
            MaUi.GetOrAdd<LayoutElement>(content.gameObject).ignoreLayout = true;

            b.icon = MaUi.Image("Icon", content);
            b.icon.preserveAspect = true;
            MaUi.Layout(b.icon.gameObject, m.IconSize, m.IconSize, m.IconSize, m.IconSize, 0, 0);
            b.surface.AddContent(b.icon);

            if (!iconOnly)
            {
                b.label = MaUi.Text(content, text, TextStyle.Control);
                b.label.alignment = TextAlignmentOptions.Center;
                b.surface.AddContent(b.label);
            }

            // The button sizes itself from its content (label + icon + padding) unless a layout stretches it.
            float minWidth = iconOnly ? m.IconButtonSize : 0;
            float height = iconOnly ? m.IconButtonSize : m.ControlHeight;
            MaUi.Layout(go, minWidth, height, iconOnly ? m.IconButtonSize : -1, height, iconOnly ? 0 : -1, 0);
            b.UpdateIcon();
            b.UpdatePreferredWidth();
            b.Hook();
            b.ApplyCurrentTheme();
            return b;
        }

        /// <summary>The label text.</summary>
        public string Label
        {
            get => label != null ? label.text : "";
            set
            {
                if (label == null) return;
                label.text = value ?? "";
                gameObject.name = "Button: " + MaText.Short(value);
                UpdatePreferredWidth();
            }
        }

        /// <summary>The look.</summary>
        public ButtonVariant Variant
        {
            get => variant;
            set
            {
                variant = value;
                ApplyCurrentTheme();
            }
        }

        /// <summary>The built-in icon shown before the label (<see cref="MaIcon.None"/> for none).</summary>
        public MaIcon Icon
        {
            get => iconId;
            set
            {
                iconId = value;
                iconSprite = null;
                UpdateIcon();
            }
        }

        /// <summary>A custom icon sprite (white artwork is tinted with the text colour); null returns to <see cref="Icon"/>.</summary>
        public Sprite IconSprite
        {
            get => iconSprite;
            set
            {
                iconSprite = value;
                UpdateIcon();
            }
        }

        /// <summary>Whether it can be clicked and focused.</summary>
        public bool Interactable
        {
            get => button.interactable;
            set
            {
                if (button.interactable == value) return;
                button.interactable = value;
                relay.Refresh();
                NotifyNavigationChanged();
            }
        }

        /// <summary>The click event (persistent listeners from the inspector work too).</summary>
        public Button.ButtonClickedEvent OnClick => button.onClick;

        /// <summary>The underlying uGUI button.</summary>
        public Button Button => button;

        /// <summary>The drawn body (fill, border, focus ring).</summary>
        public MaSurface Surface => surface;

        /// <summary>The label component (null on an icon button).</summary>
        public TextMeshProUGUI LabelText => label;

        /// <summary>The visible state.</summary>
        public ControlState State => relay != null ? relay.State : ControlState.Normal;

        /// <inheritdoc />
        public override Selectable PrimarySelectable => button;

        /// <summary>Sets the variant and returns this button.</summary>
        public MaButton WithVariant(ButtonVariant v)
        {
            Variant = v;
            return this;
        }

        /// <summary>Sets a built-in icon and returns this button.</summary>
        public MaButton WithIcon(MaIcon i)
        {
            Icon = i;
            return this;
        }

        /// <summary>Sets a custom icon sprite and returns this button.</summary>
        public MaButton WithIcon(Sprite sprite)
        {
            IconSprite = sprite;
            return this;
        }

        /// <summary>Adds a click handler and returns this button.</summary>
        public MaButton OnClicked(Action action)
        {
            if (action != null) button.onClick.AddListener(() => action());
            return this;
        }

        /// <summary>Fixes the width (canvas units) and returns this button.</summary>
        public MaButton WithWidth(float width)
        {
            LayoutElement.preferredWidth = width;
            LayoutElement.minWidth = width;
            return this;
        }

        /// <summary>Lets the button grow to fill its row and returns it.</summary>
        public MaButton Stretch()
        {
            LayoutElement.flexibleWidth = 1;
            return this;
        }

        /// <summary>Clicks it from code (does nothing when not interactable).</summary>
        public void Click()
        {
            if (button.IsActive() && button.IsInteractable()) button.onClick.Invoke();
        }

        /// <inheritdoc />
        public override void ApplyTheme(ThemeTokens theme)
        {
            if (surface == null) return;
            surface.SetRadius(theme.Metrics.RadiusSm);
            if (label != null) MaUi.Style(label, TextStyle.Control, theme);
            if (label != null && variant == ButtonVariant.Primary) label.fontStyle = FontStyles.Bold;
            UpdateIcon(); // sprites are generated at runtime, so a prefab gets them back here
            Refresh(true);
        }

        /// <inheritdoc />
        public override MenuItemDefinition Describe() =>
            new MenuItemDefinition(MenuItemKind.Button, Label, button != null && button.IsInteractable() && gameObject.activeInHierarchy);

        /// <inheritdoc />
        protected override void OnEnable()
        {
            Hook();
            base.OnEnable();
        }

        /// <inheritdoc />
        protected override void OnDisable()
        {
            if (relay != null) relay.Changed -= OnStateChanged;
            _hooked = false;
            base.OnDisable();
        }

        bool _hooked;

        void Hook()
        {
            if (_hooked || relay == null) return;
            relay.Changed += OnStateChanged;
            _hooked = true;
        }

        void OnStateChanged() => Refresh(false);

        /// <summary>Redraws the current state.</summary>
        protected void Refresh(bool instant)
        {
            if (surface == null || relay == null) return;
            var state = relay.State;
            surface.SetColors(ControlStyles.Button(MaTheme.Palette, variant, state), relay.ShowFocusRing, state == ControlState.Pressed, instant);
        }

        void UpdateIcon()
        {
            if (icon == null) return;
            var sprite = iconSprite != null ? iconSprite : MaIcons.Get(iconId);
            icon.sprite = sprite;
            icon.gameObject.SetActive(sprite != null);
            UpdatePreferredWidth();
        }

        void UpdatePreferredWidth()
        {
            if (label == null) return; // icon buttons are square
            var m = MaTheme.Metrics;
            float w = label.GetPreferredValues(label.text).x + m.SpaceLg * 2;
            if (icon != null && icon.gameObject.activeSelf) w += m.IconSize + m.SpaceSm + 4;
            var le = LayoutElement;
            if (le.minWidth < 0 || le.minWidth != le.preferredWidth) le.preferredWidth = Mathf.Max(w, m.ControlHeight * 2.4f);
        }
    }
}
