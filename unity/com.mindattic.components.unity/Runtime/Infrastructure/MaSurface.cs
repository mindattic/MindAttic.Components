using System.Collections.Generic;
using MindAttic.Components.Unity.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// The drawn body of a control: a rounded fill (on this GameObject, so it is also the raycast target), a
    /// hairline border, an accent focus ring just outside, and the text/icons tinted with the state's text colour.
    /// Colours ease towards their targets over the theme's fast duration (instant with reduce motion), and a
    /// press shrinks the control slightly.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class MaSurface : MonoBehaviour
    {
        [SerializeField] Image fill;
        [SerializeField] Image border;
        [SerializeField] Image focusRing;
        [SerializeField] List<Graphic> content = new List<Graphic>();
        [SerializeField] float radius = 12;
        [SerializeField] bool pressScale = true;

        Color _fill, _border, _text, _ring;
        Color _fillTarget, _borderTarget, _textTarget, _ringTarget;
        float _scale = 1, _scaleTarget = 1;
        bool _animating;

        /// <summary>The fill image (the raycast target and the selectable's target graphic).</summary>
        public Image Fill => fill;
        /// <summary>The border image.</summary>
        public Image Border => border;
        /// <summary>The focus ring image.</summary>
        public Image FocusRing => focusRing;
        /// <summary>Text and icons tinted with the state's text colour.</summary>
        public IReadOnlyList<Graphic> Content => content;
        /// <summary>The corner radius in canvas units.</summary>
        public float Radius => radius;
        /// <summary>The fill colour currently targeted (what the control will settle on).</summary>
        public Color TargetFill => _fillTarget;
        /// <summary>The border colour currently targeted.</summary>
        public Color TargetBorder => _borderTarget;
        /// <summary>The text colour currently targeted.</summary>
        public Color TargetText => _textTarget;
        /// <summary>True when the focus ring is (becoming) visible.</summary>
        public bool RingVisible => _ringTarget.a > 0.5f;

        /// <summary>Shrink to 97% while pressed.</summary>
        public bool PressScale
        {
            get => pressScale;
            set => pressScale = value;
        }

        /// <summary>Builds the fill, border and focus ring on <paramref name="go"/>.</summary>
        public static MaSurface Add(GameObject go, float radius, bool focusable = true)
        {
            var s = MaUi.GetOrAdd<MaSurface>(go);
            s.fill = MaUi.GetOrAdd<Image>(go);
            s.fill.raycastTarget = true;
            s.border = MaUi.Image("Border", go.transform);
            MaUi.Stretch((RectTransform)s.border.transform);
            s.border.transform.SetAsFirstSibling();
            MaUi.GetOrAdd<LayoutElement>(s.border.gameObject).ignoreLayout = true;
            if (focusable)
            {
                s.focusRing = MaUi.Image("Focus Ring", go.transform);
                s.focusRing.transform.SetSiblingIndex(1);
                s.focusRing.color = Color.clear;
                MaUi.GetOrAdd<LayoutElement>(s.focusRing.gameObject).ignoreLayout = true;
            }
            s.radius = radius;
            s.ApplyShape(MaTheme.Metrics);
            return s;
        }

        /// <summary>Adds a graphic to tint with the text colour.</summary>
        public void AddContent(Graphic g)
        {
            if (g != null && !content.Contains(g)) content.Add(g);
            if (g != null) g.color = _textTarget;
        }

        /// <summary>Stops tinting a graphic.</summary>
        public void RemoveContent(Graphic g) => content.Remove(g);

        /// <summary>Changes the corner radius.</summary>
        public void SetRadius(float r)
        {
            radius = r;
            ApplyShape(MaTheme.Metrics);
        }

        /// <summary>Rebuilds sprites for the current radius and the theme's border/focus widths.</summary>
        public void ApplyShape(ThemeMetrics m)
        {
            MaSprites.Apply(fill, MaSprites.Rounded(radius));
            MaSprites.Apply(border, MaSprites.Ring(radius, m.BorderWidth));
            if (focusRing != null)
            {
                float outset = m.FocusRingOffset + m.FocusRingWidth;
                MaUi.Outset((RectTransform)focusRing.transform, outset);
                MaSprites.Apply(focusRing, MaSprites.Ring(radius + outset, m.FocusRingWidth));
            }
        }

        /// <summary>Targets a state's colours; <paramref name="instant"/> skips the transition.</summary>
        public void SetColors(ControlColors c, bool focusRingOn, bool pressed, bool instant = false)
        {
            _fillTarget = c.Fill.ToColor();
            _borderTarget = c.Border.ToColor();
            _textTarget = c.Text.ToColor();
            var accent = ControlStyles.FocusRing(MaTheme.Palette).ToColor();
            _ringTarget = focusRingOn ? accent : MaUi.WithAlpha(accent, 0);
            _scaleTarget = pressed && pressScale && !MaTheme.ReduceMotion ? 0.97f : 1f;
            if (instant || !Application.isPlaying || MaTheme.ReduceMotion || !isActiveAndEnabled) Snap();
            else _animating = true;
        }

        /// <summary>Jumps to the target colours now.</summary>
        public void Snap()
        {
            _fill = _fillTarget;
            _border = _borderTarget;
            _text = _textTarget;
            _ring = _ringTarget;
            _scale = _scaleTarget;
            _animating = false;
            Push();
        }

        void Update()
        {
            if (!_animating) return;
            float k = Motion.Approach(Time.unscaledDeltaTime, MaTheme.Metrics.DurationFast, MaTheme.ReduceMotion);
            _fill = Color.Lerp(_fill, _fillTarget, k);
            _border = Color.Lerp(_border, _borderTarget, k);
            _text = Color.Lerp(_text, _textTarget, k);
            _ring = Color.Lerp(_ring, _ringTarget, k);
            _scale = Mathf.Lerp(_scale, _scaleTarget, k);
            if (Close(_fill, _fillTarget) && Close(_border, _borderTarget) && Close(_text, _textTarget) && Close(_ring, _ringTarget) &&
                Mathf.Abs(_scale - _scaleTarget) < 0.001f)
            {
                Snap();
                return;
            }
            Push();
        }

        static bool Close(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.004f && Mathf.Abs(a.g - b.g) < 0.004f && Mathf.Abs(a.b - b.b) < 0.004f && Mathf.Abs(a.a - b.a) < 0.004f;

        void Push()
        {
            MaUi.SetColor(fill, _fill);
            MaUi.SetColor(border, _border);
            MaUi.SetColor(focusRing, _ring);
            for (int i = 0; i < content.Count; i++) MaUi.SetColor(content[i], _text);
            var s = new Vector3(_scale, _scale, 1);
            if (transform.localScale != s) transform.localScale = s;
        }
    }
}
