using MindAttic.Components.Unity.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// A HUD status pill: a coloured dot and a short label in a rounded outline, for a connection or match state
    /// ("Live", "Connecting...", "Offline"). <see cref="StatusKind"/> picks the dot and outline colour
    /// (<see cref="ControlStyles.Status"/>); the label stays in the body text colour so it reads in any kind. A
    /// <see cref="Pulse"/> makes the dot breathe (the default for <see cref="StatusKind.Warn"/>); with reduce motion
    /// it holds still.
    /// <code>var pill = toolbar.Add(MaStatusPill.Create(toolbar, "Connecting...", StatusKind.Warn));
    /// pill.Set("Live", StatusKind.Ok);</code>
    /// </summary>
    [AddComponentMenu("MindAttic/UI/Status Pill")]
    public sealed class MaStatusPill : MaElement
    {
        /// <summary>Seconds per pulse of the dot.</summary>
        public const float PulsePeriod = 1.2f;

        const float DotSize = 14f;
        const float Height = 40f;

        [SerializeField] Image fill;
        [SerializeField] Image border;
        [SerializeField] Image dot;
        [SerializeField] TextMeshProUGUI label;
        [SerializeField] StatusKind kind;
        [SerializeField] bool pulse;

        /// <summary>
        /// Creates a pill under <paramref name="parent"/>. <paramref name="pulse"/> null pulses only a
        /// <see cref="StatusKind.Warn"/> pill.
        /// </summary>
        public static MaStatusPill Create(Component parent, string text, StatusKind kind = StatusKind.Neutral, bool? pulse = null)
        {
            var m = MaTheme.Metrics;
            var rt = MaUi.Rect("Status: " + MaText.Short(text), ResolveParent(parent));
            var s = rt.gameObject.AddComponent<MaStatusPill>();
            s.kind = kind;
            s.pulse = pulse ?? kind == StatusKind.Warn;
            s.fill = rt.gameObject.AddComponent<Image>();
            s.fill.raycastTarget = false;
            s.border = MaUi.Image("Border", rt);
            MaUi.Stretch((RectTransform)s.border.transform);
            MaUi.GetOrAdd<LayoutElement>(s.border.gameObject).ignoreLayout = true;
            MaUi.HRow(rt.gameObject, m.SpaceSm + 2, MaUi.Pad(m.SpaceMd, m.SpaceXs), TextAnchor.MiddleCenter);
            s.dot = MaUi.Image("Dot", rt);
            MaUi.Layout(s.dot.gameObject, DotSize, DotSize, DotSize, DotSize, 0, 0);
            s.label = MaUi.Text(rt, text, TextStyle.Caption);
            s.label.alignment = TextAlignmentOptions.Left;
            s.label.textWrappingMode = TextWrappingModes.NoWrap;
            MaUi.Layout(rt.gameObject, minH: Height, prefH: Height, flexW: 0);
            s.ApplyCurrentTheme();
            return s;
        }

        /// <summary>The label.</summary>
        public string Text
        {
            get => label != null ? label.text : "";
            set
            {
                if (label == null || label.text == (value ?? "")) return;
                label.text = value ?? "";
                gameObject.name = "Status: " + MaText.Short(value);
            }
        }

        /// <summary>The kind (dot and outline colour).</summary>
        public StatusKind Kind
        {
            get => kind;
            set
            {
                if (kind == value) return;
                kind = value;
                ApplyCurrentTheme();
            }
        }

        /// <summary>The dot breathes (held still with reduce motion).</summary>
        public bool Pulse
        {
            get => pulse;
            set
            {
                pulse = value;
                UpdateDot();
            }
        }

        /// <summary>The dot image.</summary>
        public Image Dot => dot;

        /// <summary>The label component.</summary>
        public TextMeshProUGUI LabelText => label;

        /// <summary>The dot's opacity right now (1 unless pulsing).</summary>
        public float DotOpacity => dot != null ? dot.canvasRenderer.GetAlpha() : 1f;

        /// <summary>
        /// Sets the label and kind in one call (no-op when unchanged, so it is cheap every frame); <paramref name="pulse"/>
        /// null pulses only a <see cref="StatusKind.Warn"/> pill.
        /// </summary>
        public MaStatusPill Set(string text, StatusKind kind, bool? pulse = null)
        {
            Text = text;
            Kind = kind;
            bool p = pulse ?? kind == StatusKind.Warn;
            if (this.pulse != p) Pulse = p;
            return this;
        }

        /// <inheritdoc />
        public override void ApplyTheme(ThemeTokens theme)
        {
            if (fill == null) return;
            var m = theme.Metrics;
            MaSprites.Apply(fill, MaSprites.Rounded(Height / 2));
            MaSprites.Apply(border, MaSprites.Ring(Height / 2, m.BorderWidth));
            MaSprites.Apply(dot, MaSprites.Rounded(DotSize / 2));
            MaUi.Style(label, TextStyle.Caption, theme);
            label.fontStyle = FontStyles.Bold;
            var c = ControlStyles.Status(theme.Palette, kind);
            fill.color = c.Fill.ToColor();
            border.color = c.Border.ToColor();
            label.color = c.Text.ToColor();
            dot.color = ControlStyles.StatusDot(theme.Palette, kind).ToColor();
            UpdateDot();
        }

        /// <inheritdoc />
        public override MenuItemDefinition Describe() => new MenuItemDefinition(MenuItemKind.Text, Text, false);

        void Update() => UpdateDot();

        void UpdateDot()
        {
            if (dot == null) return;
            float a = pulse && Application.isPlaying ? Core.Motion.Pulse(Time.unscaledTimeAsDouble, PulsePeriod, MaTheme.ReduceMotion) : 1f;
            dot.canvasRenderer.SetAlpha(a);
        }
    }
}
