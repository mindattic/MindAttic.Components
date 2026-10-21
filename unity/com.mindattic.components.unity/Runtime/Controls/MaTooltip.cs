using MindAttic.Components.Unity.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// A tooltip on any control: after a short delay on hover, and at once on keyboard/gamepad focus, one shared
    /// bubble (inverted colours, like the web's tag tip) shows above the control, or below when there is no room,
    /// kept on screen. It never takes focus or blocks the pointer. Attach with <see cref="Attach"/> or
    /// <see cref="MaTooltipExtensions.WithTooltip{T}"/>.
    /// </summary>
    [AddComponentMenu("MindAttic/UI/Tooltip")]
    [DisallowMultipleComponent]
    public sealed class MaTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] [TextArea] string text;

        static RectTransform s_bubble;
        static Image s_fill;
        static TextMeshProUGUI s_label;
        static CanvasGroup s_group;
        static MaTooltip s_owner;

        float _showAt = -1;
        float _alpha;

        /// <summary>Adds (or updates) a tooltip on <paramref name="target"/>'s GameObject.</summary>
        public static MaTooltip Attach(Component target, string text)
        {
            var go = target is MaElement el && el.PrimarySelectable != null ? el.PrimarySelectable.gameObject : target.gameObject;
            var t = MaUi.GetOrAdd<MaTooltip>(go);
            t.Text = text;
            return t;
        }

        /// <summary>The tooltip text.</summary>
        public string Text
        {
            get => text;
            set
            {
                text = value ?? "";
                if (s_owner == this && s_label != null) s_label.text = text;
            }
        }

        /// <summary>True while this tooltip's bubble is showing.</summary>
        public bool IsShowing => s_owner == this && s_bubble != null && s_bubble.gameObject.activeSelf;

        /// <summary>The shared bubble's text while it is showing (for tests), else null.</summary>
        public static string ShowingText => s_owner != null && s_bubble != null && s_bubble.gameObject.activeSelf ? s_label.text : null;

        /// <inheritdoc />
        public void OnPointerEnter(PointerEventData e) => Schedule(MaTheme.Metrics.TooltipDelay);
        /// <inheritdoc />
        public void OnPointerExit(PointerEventData e) => Hide();
        /// <inheritdoc />
        public void OnSelect(BaseEventData e)
        {
            if (!MaInput.PointerModality) Schedule(0.15f);
        }
        /// <inheritdoc />
        public void OnDeselect(BaseEventData e) => Hide();

        /// <summary>Shows the bubble now (also for tests).</summary>
        public void Show()
        {
            if (string.IsNullOrEmpty(text)) return;
            EnsureBubble();
            s_owner = this;
            s_label.text = text;
            Style(MaTheme.Current);
            s_bubble.gameObject.SetActive(true);
            s_bubble.SetAsLastSibling();
            LayoutRebuilder.ForceRebuildLayoutImmediate(s_bubble);
            Position();
            _alpha = MaTheme.ReduceMotion || !Application.isPlaying ? 1 : 0;
            s_group.alpha = _alpha;
        }

        /// <summary>Hides the bubble if it is this tooltip's.</summary>
        public void Hide()
        {
            _showAt = -1;
            if (s_owner != this) return;
            s_owner = null;
            if (s_bubble != null) s_bubble.gameObject.SetActive(false);
        }

        void Schedule(float delay)
        {
            if (!isActiveAndEnabled) return;
            _showAt = Time.unscaledTime + delay;
        }

        void OnDisable() => Hide();

        void Update()
        {
            if (_showAt >= 0 && Time.unscaledTime >= _showAt)
            {
                _showAt = -1;
                Show();
            }
            if (s_owner == this && s_group != null && _alpha < 1)
            {
                _alpha = Mathf.MoveTowards(_alpha, 1, Time.unscaledDeltaTime / 0.14f);
                s_group.alpha = _alpha;
            }
        }

        void Position()
        {
            var layer = MaCanvas.TooltipLayer;
            var target = (RectTransform)transform;
            var canvas = MaCanvas.RootOf(target);
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var corners = new Vector3[4];
            target.GetWorldCorners(corners); // bl, tl, tr, br
            Vector2 top = RectTransformUtility.WorldToScreenPoint(cam, (corners[1] + corners[2]) / 2);
            Vector2 bottom = RectTransformUtility.WorldToScreenPoint(cam, (corners[0] + corners[3]) / 2);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, top, null, out var topLocal);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, bottom, null, out var bottomLocal);

            var size = s_bubble.rect.size;
            var bounds = layer.rect;
            const float gap = 12;
            bool above = topLocal.y + gap + size.y <= bounds.yMax;
            float y = above ? topLocal.y + gap : bottomLocal.y - gap - size.y;
            float x = Mathf.Clamp(topLocal.x - size.x / 2, bounds.xMin + 8, bounds.xMax - 8 - size.x);
            s_bubble.anchoredPosition = new Vector2(x, y);
        }

        static void EnsureBubble()
        {
            if (s_bubble != null) return;
            var m = MaTheme.Metrics;
            s_bubble = MaUi.Rect("Tooltip", MaCanvas.TooltipLayer);
            s_bubble.anchorMin = s_bubble.anchorMax = new Vector2(0.5f, 0.5f);
            s_bubble.pivot = Vector2.zero;
            s_fill = s_bubble.gameObject.AddComponent<Image>();
            s_fill.raycastTarget = false;
            s_group = s_bubble.gameObject.AddComponent<CanvasGroup>();
            s_group.blocksRaycasts = false;
            s_group.interactable = false;
            MaUi.HRow(s_bubble.gameObject, 0, MaUi.Pad(m.SpaceMd, m.SpaceSm + 2));
            var fitter = s_bubble.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            s_label = MaUi.Text(s_bubble, "", TextStyle.Caption, "Text");
            s_label.textWrappingMode = TextWrappingModes.Normal;
            var le = MaUi.Layout(s_label.gameObject);
            le.preferredWidth = -1;
            s_bubble.gameObject.SetActive(false);
        }

        static void Style(ThemeTokens theme)
        {
            MaSprites.Apply(s_fill, MaSprites.Rounded(theme.Metrics.RadiusSm));
            s_fill.color = theme.text;
            MaUi.Style(s_label, TextStyle.Caption, theme);
            s_label.color = theme.bg;
            s_label.fontSize = theme.Metrics.FontCaption + 2;
            // Wrap long tips at 560 units (the web's 280px max width).
            float natural = s_label.GetPreferredValues(s_label.text).x;
            MaUi.Layout(s_label.gameObject).preferredWidth = Mathf.Min(natural, 560);
        }
    }

    /// <summary>Fluent tooltip helper for every MindAttic element.</summary>
    public static class MaTooltipExtensions
    {
        /// <summary>Adds a tooltip to the element's control and returns the element.</summary>
        public static T WithTooltip<T>(this T element, string text) where T : MaElement
        {
            MaTooltip.Attach(element, text);
            return element;
        }
    }
}
