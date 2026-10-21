using MindAttic.Components.Unity.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// A busy ring in the accent (the WPF <c>MaSpinner</c>'s look): one turn every 1.2 s on unscaled time, so it
    /// keeps turning while the game is paused. With reduce motion it stays still and pulses its opacity instead.
    /// </summary>
    [AddComponentMenu("MindAttic/UI/Spinner")]
    public sealed class MaSpinner : MaElement
    {
        [SerializeField] Image ring;
        [SerializeField] float period = 1.2f;

        /// <summary>Creates a spinner <paramref name="size"/> units across under <paramref name="parent"/>.</summary>
        public static MaSpinner Create(Component parent, float size = 48)
        {
            var rt = MaUi.Rect("Spinner", ResolveParent(parent));
            var s = rt.gameObject.AddComponent<MaSpinner>();
            MaUi.Layout(rt.gameObject, size, size, size, size, 0, 0);
            rt.sizeDelta = new Vector2(size, size);
            s.ring = MaUi.Image("Ring", rt);
            MaUi.Stretch((RectTransform)s.ring.transform);
            s.ring.sprite = MaSprites.SpinnerArc;
            s.ring.preserveAspect = true;
            s.ApplyCurrentTheme();
            return s;
        }

        /// <summary>Seconds per turn.</summary>
        public float Period
        {
            get => period;
            set => period = Mathf.Max(0.1f, value);
        }

        /// <summary>The current rotation in degrees (for tests).</summary>
        public float Angle => ring != null ? ring.rectTransform.localEulerAngles.z : 0;

        /// <inheritdoc />
        public override void ApplyTheme(ThemeTokens theme)
        {
            if (ring == null) return;
            ring.sprite = MaSprites.SpinnerArc;
            ring.color = theme.accent;
        }

        /// <inheritdoc />
        public override MenuItemDefinition Describe() => new MenuItemDefinition(MenuItemKind.Progress, "Spinner", false);

        void Update()
        {
            if (ring == null || !Application.isPlaying) return;
            if (MaTheme.ReduceMotion)
            {
                ring.rectTransform.localEulerAngles = Vector3.zero;
                ring.canvasRenderer.SetAlpha(0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2 / (period * 1.5f)));
                return;
            }
            ring.canvasRenderer.SetAlpha(1);
            ring.rectTransform.localEulerAngles = new Vector3(0, 0, -360f * Mathf.Repeat(Time.unscaledTime / period, 1f));
        }
    }
}
