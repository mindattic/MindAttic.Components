using System;
using System.Globalization;
using MindAttic.Components.Unity.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// A slider in the theme's accent (the web's <c>.ma-range</c>): Left/Right (or the stick) change it while it
    /// is focused, a click or drag sets it. A stock uGUI <see cref="Slider"/> underneath. Attach a readout label
    /// with <see cref="ShowValueIn"/> (a <see cref="MaFormRow"/> does it for you).
    /// </summary>
    [AddComponentMenu("MindAttic/UI/Range")]
    public sealed class MaRange : MaElement
    {
        [SerializeField] Slider slider;
        [SerializeField] MaStateRelay relay;
        [SerializeField] MaSurface handle;
        [SerializeField] Image track;
        [SerializeField] Image fill;
        [SerializeField] TMP_Text valueLabel;
        [SerializeField] string format = "0";

        bool _hooked;
        const float TrackHeight = 10, HandleSize = 30;

        /// <summary>Raised with the new value whenever it changes.</summary>
        public event Action<float> ValueChanged;

        /// <summary>Creates a slider from <paramref name="min"/> to <paramref name="max"/> under <paramref name="parent"/>.</summary>
        public static MaRange Create(Component parent, float min, float max, float value, Action<float> onChanged = null, bool wholeNumbers = false)
        {
            var m = MaTheme.Metrics;
            var rt = MaUi.Rect("Range", ResolveParent(parent));
            var go = rt.gameObject;
            var r = go.AddComponent<MaRange>();
            MaUi.Layout(go, minW: 160, minH: m.ControlHeight, prefW: 360, prefH: m.ControlHeight, flexW: 1);

            r.track = MaUi.Image("Track", rt, raycast: true);
            var tr = (RectTransform)r.track.transform;
            tr.anchorMin = new Vector2(0, 0.5f);
            tr.anchorMax = new Vector2(1, 0.5f);
            tr.sizeDelta = new Vector2(-HandleSize, TrackHeight);

            var fillArea = MaUi.Rect("Fill Area", rt);
            fillArea.anchorMin = new Vector2(0, 0.5f);
            fillArea.anchorMax = new Vector2(1, 0.5f);
            fillArea.sizeDelta = new Vector2(-HandleSize, TrackHeight);
            r.fill = MaUi.Image("Fill", fillArea);
            var fr = (RectTransform)r.fill.transform;
            fr.sizeDelta = Vector2.zero;

            var handleArea = MaUi.Rect("Handle Slide Area", rt);
            handleArea.anchorMin = new Vector2(0, 0.5f);
            handleArea.anchorMax = new Vector2(1, 0.5f);
            handleArea.sizeDelta = new Vector2(-HandleSize, HandleSize);
            var handleRt = MaUi.Rect("Handle", handleArea);
            handleRt.sizeDelta = new Vector2(HandleSize, 0);
            r.handle = MaSurface.Add(handleRt.gameObject, HandleSize / 2);
            var hle = handleRt.gameObject.AddComponent<LayoutElement>();
            hle.ignoreLayout = true;

            r.slider = go.AddComponent<Slider>();
            r.slider.fillRect = fr;
            r.slider.handleRect = handleRt;
            r.slider.targetGraphic = r.handle.Fill;
            r.slider.direction = Slider.Direction.LeftToRight;
            r.slider.minValue = min;
            r.slider.maxValue = max;
            r.slider.wholeNumbers = wholeNumbers;
            r.slider.SetValueWithoutNotify(value);
            r.relay = MaStateRelay.Attach(r.slider);
            r.format = wholeNumbers ? "0" : "0.##";

            if (onChanged != null) r.ValueChanged += onChanged;
            r.Hook();
            r.ApplyCurrentTheme();
            return r;
        }

        /// <summary>The value.</summary>
        public float Value
        {
            get => slider.value;
            set => slider.value = value;
        }

        /// <summary>The lower bound.</summary>
        public float Min
        {
            get => slider.minValue;
            set => slider.minValue = value;
        }

        /// <summary>The upper bound.</summary>
        public float Max
        {
            get => slider.maxValue;
            set => slider.maxValue = value;
        }

        /// <summary>The number format of the readout (e.g. <c>0</c>, <c>0.0</c>, <c>0%</c>).</summary>
        public string Format
        {
            get => format;
            set
            {
                format = value ?? "0";
                UpdateReadout();
            }
        }

        /// <summary>Whether it can be changed and focused.</summary>
        public bool Interactable
        {
            get => slider.interactable;
            set
            {
                if (slider.interactable == value) return;
                slider.interactable = value;
                relay.Refresh();
                NotifyNavigationChanged();
            }
        }

        /// <summary>The underlying uGUI slider.</summary>
        public Slider Slider => slider;

        /// <inheritdoc />
        public override Selectable PrimarySelectable => slider;

        /// <summary>Sets the value without raising <see cref="ValueChanged"/>.</summary>
        public void SetValueWithoutNotify(float v)
        {
            slider.SetValueWithoutNotify(v);
            UpdateReadout();
        }

        /// <summary>Keeps <paramref name="label"/> showing the value in <see cref="Format"/>; returns this slider.</summary>
        public MaRange ShowValueIn(TMP_Text label, string numberFormat = null)
        {
            valueLabel = label;
            if (numberFormat != null) format = numberFormat;
            UpdateReadout();
            return this;
        }

        /// <summary>The readout text for the current value.</summary>
        public string ValueText => slider.value.ToString(format, CultureInfo.InvariantCulture);

        /// <inheritdoc />
        public override void ApplyTheme(ThemeTokens theme)
        {
            if (handle == null) return;
            MaSprites.Apply(track, MaSprites.Rounded(TrackHeight / 2));
            MaSprites.Apply(fill, MaSprites.Rounded(TrackHeight / 2));
            handle.SetRadius(HandleSize / 2);
            Refresh(true);
        }

        /// <inheritdoc />
        public override MenuItemDefinition Describe() =>
            new MenuItemDefinition(MenuItemKind.Range, name, slider != null && slider.IsInteractable() && gameObject.activeInHierarchy);

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
                slider.onValueChanged.RemoveListener(OnValue);
                _hooked = false;
            }
            base.OnDisable();
        }

        void Hook()
        {
            if (_hooked || relay == null || slider == null) return;
            relay.Changed += OnState;
            slider.onValueChanged.AddListener(OnValue);
            _hooked = true;
        }

        void OnState() => Refresh(false);

        void OnValue(float v)
        {
            UpdateReadout();
            ValueChanged?.Invoke(v);
        }

        void UpdateReadout()
        {
            if (valueLabel != null) valueLabel.text = ValueText;
        }

        void Refresh(bool instant)
        {
            if (handle == null || relay == null) return;
            var p = MaTheme.Palette;
            var state = relay.State;
            bool off = state == ControlState.Disabled;
            float a = off ? ControlStyles.DisabledAlpha : 1f;
            MaUi.SetColor(track, p.Bg3.Lerp(p.Border, 0.6f).WithAlpha(a).ToColor());
            MaUi.SetColor(fill, (state == ControlState.Hover ? p.Accent2 : p.Accent).WithAlpha(a).ToColor());
            var knob = state == ControlState.Hover || state == ControlState.Pressed
                ? new ControlColors(p.Accent2, p.Bg, p.Text)
                : new ControlColors(p.Accent, p.Bg, p.Text);
            handle.SetColors(off ? knob.Faded(a) : knob, relay.ShowFocusRing, state == ControlState.Pressed, instant);
            UpdateReadout();
        }
    }
}
