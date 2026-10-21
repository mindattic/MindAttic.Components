using System;
using MindAttic.Components.Unity.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// An on/off switch with its label: the whole row is the control (click, Enter/A, or Left/Right to set it),
    /// the track fills with the accent when on and the knob slides across. A stock uGUI <see cref="Toggle"/> underneath.
    /// </summary>
    [AddComponentMenu("MindAttic/UI/Toggle")]
    public sealed class MaToggle : MaElement, IMoveHandler
    {
        [SerializeField] Toggle toggle;
        [SerializeField] MaSurface surface;
        [SerializeField] MaStateRelay relay;
        [SerializeField] TextMeshProUGUI label;
        [SerializeField] Image track;
        [SerializeField] Image trackBorder;
        [SerializeField] Image knob;

        float _knob; // 0 off .. 1 on
        bool _hooked;

        const float TrackWidth = 68, TrackHeight = 38, KnobSize = 28;

        /// <summary>Raised with the new value whenever it changes (also <see cref="Toggle.onValueChanged"/>).</summary>
        public event Action<bool> ValueChanged;

        /// <summary>Creates a labelled switch under <paramref name="parent"/>.</summary>
        public static MaToggle Create(Component parent, string label, bool value = false, Action<bool> onChanged = null)
        {
            var m = MaTheme.Metrics;
            var rt = MaUi.Rect("Toggle: " + MaText.Short(label), ResolveParent(parent));
            var go = rt.gameObject;
            var t = go.AddComponent<MaToggle>();
            t.surface = MaSurface.Add(go, m.RadiusSm);
            t.surface.PressScale = false;
            t.toggle = go.AddComponent<Toggle>();
            t.toggle.targetGraphic = t.surface.Fill;
            t.toggle.isOn = value;
            t.relay = MaStateRelay.Attach(t.toggle);

            var content = MaUi.Rect("Content", rt);
            MaUi.Stretch(content);
            MaUi.GetOrAdd<LayoutElement>(content.gameObject).ignoreLayout = true;
            MaUi.HRow(content.gameObject, m.SpaceMd, MaUi.Pad(m.SpaceMd, 0), TextAnchor.MiddleLeft);

            t.label = MaUi.Text(content, label, TextStyle.Control);
            MaUi.Layout(t.label.gameObject, flexW: 1);

            var trackRt = MaUi.Rect("Track", content);
            MaUi.Layout(trackRt.gameObject, TrackWidth, TrackHeight, TrackWidth, TrackHeight, 0, 0);
            t.track = trackRt.gameObject.AddComponent<Image>();
            t.track.raycastTarget = false;
            t.trackBorder = MaUi.Image("Border", trackRt);
            MaUi.Stretch((RectTransform)t.trackBorder.transform);
            t.knob = MaUi.Image("Knob", trackRt);
            var kr = (RectTransform)t.knob.transform;
            kr.anchorMin = kr.anchorMax = new Vector2(0, 0.5f);
            kr.sizeDelta = new Vector2(KnobSize, KnobSize);

            MaUi.Layout(go, minH: m.ControlHeight, prefH: m.ControlHeight, flexW: 1);
            t._knob = value ? 1 : 0;
            if (onChanged != null) t.ValueChanged += onChanged;
            t.Hook();
            t.ApplyCurrentTheme();
            return t;
        }

        /// <summary>On or off. Setting it raises <see cref="ValueChanged"/> when it changes.</summary>
        public bool Value
        {
            get => toggle.isOn;
            set => toggle.isOn = value;
        }

        /// <summary>Sets the value without raising <see cref="ValueChanged"/>.</summary>
        public void SetValueWithoutNotify(bool value)
        {
            toggle.SetIsOnWithoutNotify(value);
            Refresh(false);
        }

        /// <summary>The label text.</summary>
        public string Label
        {
            get => label.text;
            set => label.text = value ?? "";
        }

        /// <summary>Whether it can be changed and focused.</summary>
        public bool Interactable
        {
            get => toggle.interactable;
            set
            {
                if (toggle.interactable == value) return;
                toggle.interactable = value;
                relay.Refresh();
                NotifyNavigationChanged();
            }
        }

        /// <summary>The underlying uGUI toggle.</summary>
        public Toggle Toggle => toggle;

        /// <inheritdoc />
        public override Selectable PrimarySelectable => toggle;

        /// <inheritdoc />
        public override void ApplyTheme(ThemeTokens theme)
        {
            if (surface == null) return;
            surface.SetRadius(theme.Metrics.RadiusSm);
            MaUi.Style(label, TextStyle.Control, theme);
            MaSprites.Apply(track, MaSprites.Rounded(TrackHeight / 2));
            MaSprites.Apply(trackBorder, MaSprites.Ring(TrackHeight / 2, theme.Metrics.BorderWidth));
            MaSprites.Apply(knob, MaSprites.Rounded(KnobSize / 2));
            Refresh(true);
        }

        /// <inheritdoc />
        public override MenuItemDefinition Describe() =>
            new MenuItemDefinition(MenuItemKind.Toggle, Label, toggle != null && toggle.IsInteractable() && gameObject.activeInHierarchy);

        /// <summary>Left turns it off, Right turns it on (gamepad and keyboard).</summary>
        public void OnMove(AxisEventData e)
        {
            if (!toggle.IsInteractable()) return;
            if (e.moveDir == MoveDirection.Left && toggle.isOn) toggle.isOn = false;
            else if (e.moveDir == MoveDirection.Right && !toggle.isOn) toggle.isOn = true;
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
                toggle.onValueChanged.RemoveListener(OnValue);
                _hooked = false;
            }
            base.OnDisable();
        }

        void Hook()
        {
            if (_hooked || relay == null || toggle == null) return;
            relay.Changed += OnState;
            toggle.onValueChanged.AddListener(OnValue);
            _hooked = true;
        }

        void OnState() => Refresh(false);

        void OnValue(bool v)
        {
            Refresh(false);
            ValueChanged?.Invoke(v);
        }

        void Refresh(bool instant)
        {
            if (surface == null || relay == null) return;
            var p = MaTheme.Palette;
            var state = relay.State;
            surface.SetColors(ControlStyles.Row(p, state), relay.ShowFocusRing, false, instant);
            var sw = ControlStyles.Switch(p, toggle.isOn, state);
            MaUi.SetColor(track, sw.Fill.ToColor());
            MaUi.SetColor(trackBorder, sw.Border.ToColor());
            MaUi.SetColor(knob, sw.Text.ToColor());
            MaUi.SetColor(label, (state == ControlState.Disabled ? sw.Text : p.Text).ToColor());
            if (instant || !Application.isPlaying || MaTheme.ReduceMotion) _knob = toggle.isOn ? 1 : 0;
            PlaceKnob();
        }

        void Update()
        {
            float target = toggle != null && toggle.isOn ? 1 : 0;
            if (Mathf.Approximately(_knob, target)) return;
            float k = Motion.Approach(Time.unscaledDeltaTime, MaTheme.Metrics.DurationFast, MaTheme.ReduceMotion || !Application.isPlaying);
            _knob = Mathf.Abs(_knob - target) < 0.01f ? target : Mathf.Lerp(_knob, target, k);
            PlaceKnob();
        }

        void PlaceKnob()
        {
            if (knob == null) return;
            float pad = (TrackHeight - KnobSize) / 2;
            float x = pad + KnobSize / 2 + _knob * (TrackWidth - KnobSize - pad * 2);
            ((RectTransform)knob.transform).anchoredPosition = new Vector2(x, 0);
        }
    }
}
