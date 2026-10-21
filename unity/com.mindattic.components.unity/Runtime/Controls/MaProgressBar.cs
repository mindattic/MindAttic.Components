using MindAttic.Components.Unity.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// A thin rounded track and its accent fill (the web's <c>.ma-progress</c>): determinate (set
    /// <see cref="Value"/> 0..1) or indeterminate (a segment sweeps across; with reduce motion it rests and pulses).
    /// </summary>
    [AddComponentMenu("MindAttic/UI/Progress Bar")]
    public sealed class MaProgressBar : MaElement
    {
        [SerializeField] Image track;
        [SerializeField] Image fill;
        [SerializeField] float value;
        [SerializeField] bool indeterminate;
        [SerializeField] float height = 10;

        float _shown;

        /// <summary>Creates a bar under <paramref name="parent"/>.</summary>
        public static MaProgressBar Create(Component parent, float value = 0, bool indeterminate = false, float height = 10)
        {
            var rt = MaUi.Rect("Progress Bar", ResolveParent(parent));
            var p = rt.gameObject.AddComponent<MaProgressBar>();
            p.height = height;
            MaUi.Layout(rt.gameObject, minH: height, prefH: height, prefW: 360, flexW: 1);
            p.track = MaUi.Image("Track", rt);
            MaUi.Stretch((RectTransform)p.track.transform);
            p.fill = MaUi.Image("Fill", rt);
            var fr = (RectTransform)p.fill.transform;
            fr.pivot = new Vector2(0, 0.5f);
            fr.offsetMin = fr.offsetMax = Vector2.zero;
            p.value = Mathf.Clamp01(value);
            p._shown = p.value;
            p.indeterminate = indeterminate;
            p.Place();
            p.ApplyCurrentTheme();
            return p;
        }

        /// <summary>Progress, 0..1 (eases towards a new value unless reduce motion is on).</summary>
        public float Value
        {
            get => value;
            set
            {
                this.value = Mathf.Clamp01(value);
                if (!Application.isPlaying || MaTheme.ReduceMotion) _shown = this.value;
                Place();
            }
        }

        /// <summary>No known end: a segment sweeps across instead of a fill.</summary>
        public bool Indeterminate
        {
            get => indeterminate;
            set
            {
                indeterminate = value;
                Place();
            }
        }

        /// <summary>The fill's displayed share (what the bar shows right now).</summary>
        public float DisplayedValue => _shown;

        /// <inheritdoc />
        public override void ApplyTheme(ThemeTokens theme)
        {
            if (track == null) return;
            MaSprites.Apply(track, MaSprites.Rounded(height / 2));
            MaSprites.Apply(fill, MaSprites.Rounded(height / 2));
            track.color = MaUi.WithAlpha(theme.text3, 0.25f);
            fill.color = theme.accent;
        }

        /// <inheritdoc />
        public override MenuItemDefinition Describe() => new MenuItemDefinition(MenuItemKind.Progress, name, false);

        void Update()
        {
            if (fill == null) return;
            if (indeterminate)
            {
                Place();
                return;
            }
            if (Mathf.Approximately(_shown, value)) return;
            float k = Core.Motion.Approach(Time.unscaledDeltaTime, MaTheme.Metrics.DurationBase, MaTheme.ReduceMotion || !Application.isPlaying);
            _shown = Mathf.Abs(_shown - value) < 0.001f ? value : Mathf.Lerp(_shown, value, k);
            Place();
        }

        void Place()
        {
            if (fill == null) return;
            var fr = (RectTransform)fill.transform;
            if (indeterminate)
            {
                const float seg = 0.32f;
                if (MaTheme.ReduceMotion || !Application.isPlaying)
                {
                    fr.anchorMin = new Vector2(0, 0);
                    fr.anchorMax = new Vector2(1, 1);
                    float a = 0.45f + 0.35f * Mathf.Sin(Time.unscaledTime * Mathf.PI);
                    fill.canvasRenderer.SetAlpha(Application.isPlaying ? a : 0.6f);
                }
                else
                {
                    fill.canvasRenderer.SetAlpha(1);
                    float t = Mathf.Repeat(Time.unscaledTime / 1.4f, 1f);
                    float x = Mathf.Lerp(-seg, 1f, Core.Motion.Ease(t));
                    fr.anchorMin = new Vector2(Mathf.Clamp01(x), 0);
                    fr.anchorMax = new Vector2(Mathf.Clamp01(x + seg), 1);
                }
            }
            else
            {
                fill.canvasRenderer.SetAlpha(1);
                fr.anchorMin = new Vector2(0, 0);
                fr.anchorMax = new Vector2(_shown, 1);
            }
            fr.offsetMin = fr.offsetMax = Vector2.zero;
        }
    }
}
