using System;
using MindAttic.Components.Unity.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// A single-line text field (a TextMesh Pro <see cref="TMP_InputField"/>) with an optional caption above it,
    /// the web's outlined field look: the border lights in the accent on hover and while editing. Reached by
    /// keyboard/gamepad like any control; Enter/A starts editing (so arrows can still move past it), Enter
    /// submits, Esc cancels the edit without leaving the menu.
    /// </summary>
    [AddComponentMenu("MindAttic/UI/Textbox")]
    public sealed class MaTextbox : MaElement
    {
        [SerializeField] TMP_InputField input;
        [SerializeField] MaSurface surface;
        [SerializeField] MaStateRelay relay;
        [SerializeField] TextMeshProUGUI caption;
        [SerializeField] TextMeshProUGUI text;
        [SerializeField] TextMeshProUGUI placeholder;
        [SerializeField] bool uppercase;

        bool _hooked, _editing;
        static int s_lastEditEndFrame = -10;

        /// <summary>Raised with the text when the player presses Enter (or the field loses focus with a change).</summary>
        public event Action<string> Submitted;

        /// <summary>Raised with the text on every edit.</summary>
        public event Action<string> Changed;

        /// <summary>Creates a field (with <paramref name="label"/> as its caption when not empty) under <paramref name="parent"/>.</summary>
        public static MaTextbox Create(Component parent, string label, string placeholder = "",
            Action<string> onSubmit = null, Action<string> onChanged = null)
        {
            var m = MaTheme.Metrics;
            var root = MaUi.Rect("Textbox: " + MaText.Short(label), ResolveParent(parent));
            var t = root.gameObject.AddComponent<MaTextbox>();
            MaUi.VColumn(root.gameObject, m.SpaceSm, null, TextAnchor.UpperLeft);
            MaUi.Layout(root.gameObject, flexW: 1);

            t.caption = MaUi.Text(root, label, TextStyle.Label, "Caption");
            t.caption.gameObject.SetActive(!string.IsNullOrEmpty(label));

            var field = MaUi.Rect("Field", root);
            field.gameObject.SetActive(false); // TMP_InputField wants its parts before OnEnable
            MaUi.Layout(field.gameObject, minH: m.ControlHeight, prefH: m.ControlHeight, flexW: 1);
            t.surface = MaSurface.Add(field.gameObject, m.RadiusSm);
            t.surface.PressScale = false;

            var area = MaUi.Rect("Text Area", field);
            MaUi.Stretch(area, m.SpaceMd + 2, 6, m.SpaceMd + 2, 6);
            area.gameObject.AddComponent<RectMask2D>();

            t.placeholder = MaUi.Text(area, placeholder, TextStyle.Muted, "Placeholder");
            MaUi.Stretch((RectTransform)t.placeholder.transform);
            t.placeholder.fontStyle = FontStyles.Italic;
            t.text = MaUi.Text(area, "", TextStyle.Control, "Text");
            MaUi.Stretch((RectTransform)t.text.transform);
            t.text.overflowMode = TextOverflowModes.Overflow;

            t.input = field.gameObject.AddComponent<TMP_InputField>();
            t.input.textViewport = area;
            t.input.textComponent = t.text;
            t.input.placeholder = t.placeholder;
            t.input.targetGraphic = t.surface.Fill;
            t.input.lineType = TMP_InputField.LineType.SingleLine;
            t.input.shouldActivateOnSelect = false;
            t.input.onFocusSelectAll = true;
            t.input.richText = false;
            t.input.caretWidth = 2;
            t.input.customCaretColor = true;
            t.relay = MaStateRelay.Attach(t.input);
            field.gameObject.SetActive(true);

            if (onSubmit != null) t.Submitted += onSubmit;
            if (onChanged != null) t.Changed += onChanged;
            t.Hook();
            t.ApplyCurrentTheme();
            return t;
        }

        /// <summary>True while any MindAttic text field is being edited, or one stopped this frame (back/Esc is the field's then).</summary>
        public static bool AnyEditing
        {
            get
            {
                if (Time.frameCount - s_lastEditEndFrame <= 1) return true;
                var sel = MaEventSystem.Selected;
                return sel != null && sel.TryGetComponent<TMP_InputField>(out var f) && f.isFocused;
            }
        }

        /// <summary>The text.</summary>
        public string Text
        {
            get => input.text;
            set => input.text = value ?? "";
        }

        /// <summary>The placeholder shown while empty.</summary>
        public string Placeholder
        {
            get => placeholder.text;
            set => placeholder.text = value ?? "";
        }

        /// <summary>The caption above the field (empty hides it).</summary>
        public string Label
        {
            get => caption.text;
            set
            {
                caption.text = value ?? "";
                caption.gameObject.SetActive(!string.IsNullOrEmpty(value));
            }
        }

        /// <summary>Whether it can be edited and focused.</summary>
        public bool Interactable
        {
            get => input.interactable;
            set
            {
                if (input.interactable == value) return;
                input.interactable = value;
                relay.Refresh();
                NotifyNavigationChanged();
            }
        }

        /// <summary>Read-only: selectable and copyable, not editable.</summary>
        public bool ReadOnly
        {
            get => input.readOnly;
            set => input.readOnly = value;
        }

        /// <summary>True while the player is typing in it.</summary>
        public bool IsEditing => input != null && input.isFocused;

        /// <summary>The underlying TextMesh Pro input field.</summary>
        public TMP_InputField InputField => input;

        /// <inheritdoc />
        public override Selectable PrimarySelectable => input;

        /// <summary>Sets the content type (standard, integer, email, password, ...) and returns this field.</summary>
        public MaTextbox WithContentType(TMP_InputField.ContentType type)
        {
            input.contentType = type;
            input.ForceLabelUpdate();
            return this;
        }

        /// <summary>Limits the length and returns this field.</summary>
        public MaTextbox WithCharacterLimit(int limit)
        {
            input.characterLimit = Mathf.Max(0, limit);
            return this;
        }

        /// <summary>
        /// Makes it a join-code field: letters and digits only, typed in upper case, at most
        /// <paramref name="length"/> characters (0 for no limit). Returns this field.
        /// </summary>
        public MaTextbox AsCode(int length = 0)
        {
            uppercase = true;
            input.characterLimit = Mathf.Max(0, length);
            input.onValidateInput = ValidateCode;
            return this;
        }

        /// <summary>Selects the field and starts editing.</summary>
        public void Focus()
        {
            MaEventSystem.Select(input.gameObject);
            input.ActivateInputField();
        }

        /// <inheritdoc />
        public override void ApplyTheme(ThemeTokens theme)
        {
            if (surface == null) return;
            var m = theme.Metrics;
            surface.SetRadius(m.RadiusSm);
            MaUi.Style(caption, TextStyle.Label, theme);
            MaUi.Style(text, TextStyle.Control, theme);
            MaUi.Style(placeholder, TextStyle.Muted, theme);
            placeholder.fontSize = m.FontControl;
            placeholder.fontStyle = FontStyles.Italic;
            input.caretColor = theme.accent;
            input.selectionColor = MaUi.WithAlpha(theme.accent, 0.35f);
            input.fontAsset = text.font;
            Refresh(true);
        }

        /// <inheritdoc />
        public override MenuItemDefinition Describe()
        {
            string label = !string.IsNullOrEmpty(Label) ? Label : Placeholder;
            return new MenuItemDefinition(MenuItemKind.Textbox, label, input != null && input.IsInteractable() && gameObject.activeInHierarchy);
        }

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
                input.onValueChanged.RemoveListener(OnValue);
                input.onSubmit.RemoveListener(OnSubmit);
                input.onEndEdit.RemoveListener(OnEndEdit);
                _hooked = false;
            }
            base.OnDisable();
        }

        void Hook()
        {
            if (_hooked || relay == null || input == null) return;
            relay.Changed += OnState;
            input.onValueChanged.AddListener(OnValue);
            input.onSubmit.AddListener(OnSubmit);
            input.onEndEdit.AddListener(OnEndEdit);
            if (uppercase) input.onValidateInput = ValidateCode;
            _hooked = true;
        }

        void Update()
        {
            bool editing = input != null && input.isFocused;
            if (editing != _editing)
            {
                _editing = editing;
                Refresh(false);
            }
        }

        void OnState() => Refresh(false);
        void OnValue(string v) => Changed?.Invoke(v);
        void OnSubmit(string v) => Submitted?.Invoke(v);
        void OnEndEdit(string v) => s_lastEditEndFrame = Time.frameCount;

        char ValidateCode(string current, int index, char c)
        {
            if (!char.IsLetterOrDigit(c) || c > 127) return '\0';
            return char.ToUpperInvariant(c);
        }

        void Refresh(bool instant)
        {
            if (surface == null || relay == null) return;
            var state = relay.State;
            bool editing = input != null && input.isFocused;
            var colors = ControlStyles.Field(MaTheme.Palette, state, editing);
            surface.SetColors(colors, relay.ShowFocusRing && !editing, false, instant);
            MaUi.SetColor(text, colors.Text.ToColor());
        }
    }
}
