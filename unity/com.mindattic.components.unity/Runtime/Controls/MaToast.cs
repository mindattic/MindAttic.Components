using System.Collections.Generic;
using MindAttic.Components.Unity.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// A short notification card at the bottom of the screen (the WPF <c>MaToast</c>'s look: a surface card with a
    /// coloured stripe for its <see cref="ToastKind"/>, a bold title and a message). Up to three stack at once,
    /// newer ones wait; each leaves after its time or when clicked. Toasts never take keyboard/gamepad focus.
    /// <code>MaToast.Show("Join code copied", ToastKind.Success);</code>
    /// </summary>
    [AddComponentMenu("")]
    public sealed class MaToast : MaElement, UnityEngine.EventSystems.IPointerClickHandler
    {
        [SerializeField] Image stripe;
        [SerializeField] MaText title;
        [SerializeField] MaText message;
        [SerializeField] CanvasGroup group;

        ToastEntry _entry;
        float _alpha;
        bool _leaving;

        static ToastQueue s_queue;
        static RectTransform s_stack;
        static readonly List<MaToast> s_visible = new List<MaToast>();

        /// <summary>Shows a toast (or restarts an identical one already up); <paramref name="seconds"/> &lt;= 0 uses the theme default.</summary>
        public static ToastEntry Show(string message, ToastKind kind = ToastKind.Info, string title = null, float seconds = 0)
        {
            EnsureHost();
            float d = seconds > 0 ? seconds : MaTheme.Metrics.ToastSeconds;
            return s_queue.Enqueue(message, kind, d, Now, title);
        }

        /// <summary>Removes every toast.</summary>
        public static void DismissAll()
        {
            s_queue?.Clear();
        }

        /// <summary>The toasts on screen, oldest first.</summary>
        public static IReadOnlyList<MaToast> OnScreen => s_visible;

        /// <summary>This toast's entry (message, title, kind, timing).</summary>
        public ToastEntry Entry => _entry;

        /// <summary>The message shown.</summary>
        public string Message => _entry?.Message;

        /// <summary>Removes this toast now.</summary>
        public void Dismiss()
        {
            if (_entry != null) s_queue?.Dismiss(_entry.Id, Now);
        }

        /// <summary>A click dismisses the toast.</summary>
        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData e) => Dismiss();

        static double Now => Time.realtimeSinceStartupAsDouble;

        static void EnsureHost()
        {
            if (s_queue != null && s_stack != null) return;
            s_visible.Clear();
            s_queue = new ToastQueue(3);
            s_queue.Shown += OnShown;
            s_queue.Hidden += OnHidden;
            var m = MaTheme.Metrics;
            s_stack = MaUi.Rect("Toast Stack", MaCanvas.ToastLayer);
            s_stack.anchorMin = s_stack.anchorMax = new Vector2(0.5f, 0);
            s_stack.pivot = new Vector2(0.5f, 0);
            s_stack.anchoredPosition = new Vector2(0, m.SpaceXxl);
            s_stack.sizeDelta = new Vector2(600, 0);
            var v = MaUi.VColumn(s_stack.gameObject, m.SpaceSm + 4, null, TextAnchor.LowerCenter);
            v.childForceExpandWidth = true;
            var fit = s_stack.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            s_stack.gameObject.AddComponent<MaToastTicker>();
        }

        internal static void Tick() => s_queue?.Tick(Now);

        static void OnShown(ToastEntry e)
        {
            var t = Build(e);
            s_visible.Add(t);
        }

        static void OnHidden(ToastEntry e)
        {
            for (int i = 0; i < s_visible.Count; i++)
            {
                var t = s_visible[i];
                if (t == null || t._entry != e) continue;
                s_visible.RemoveAt(i);
                if (t != null) t.Leave();
                return;
            }
        }

        static MaToast Build(ToastEntry e)
        {
            var m = MaTheme.Metrics;
            var panel = MaPanel.Create(s_stack, PanelStyle.Surface, "Toast");
            var t = panel.gameObject.AddComponent<MaToast>();
            t._entry = e;
            t.group = panel.gameObject.AddComponent<CanvasGroup>();
            t.group.interactable = false; // never takes focus
            MaUi.HRow(panel.gameObject, m.SpaceMd, new RectOffset(Mathf.RoundToInt(m.SpaceLg + 6), Mathf.RoundToInt(m.SpaceLg), Mathf.RoundToInt(m.SpaceMd), Mathf.RoundToInt(m.SpaceMd)), TextAnchor.MiddleLeft);

            t.stripe = MaUi.Image("Stripe", panel.transform);
            var sr = (RectTransform)t.stripe.transform;
            sr.anchorMin = new Vector2(0, 0);
            sr.anchorMax = new Vector2(0, 1);
            sr.pivot = new Vector2(0, 0.5f);
            sr.offsetMin = new Vector2(m.SpaceSm, m.SpaceSm + 2);
            sr.offsetMax = new Vector2(m.SpaceSm + 6, -m.SpaceSm - 2);
            MaUi.GetOrAdd<LayoutElement>(t.stripe.gameObject).ignoreLayout = true;

            var icon = MaUi.Image("Icon", panel.transform);
            icon.sprite = MaIcons.Get(e.Kind == ToastKind.Success ? MaIcon.Check : e.Kind == ToastKind.Info ? MaIcon.Info : MaIcon.Warning);
            icon.preserveAspect = true;
            MaUi.Layout(icon.gameObject, m.IconSize, m.IconSize, m.IconSize, m.IconSize, 0, 0);
            icon.name = "Icon";

            var col = MaUi.Rect("Text", panel.transform);
            MaUi.VColumn(col.gameObject, 2, null, TextAnchor.MiddleLeft);
            MaUi.Layout(col.gameObject, flexW: 1);
            t.title = MaText.Create(col, e.Title ?? "", TextStyle.Body);
            t.title.Tmp.fontStyle = FontStyles.Bold;
            t.title.gameObject.SetActive(!string.IsNullOrEmpty(e.Title));
            t.message = MaText.Create(col, e.Message, string.IsNullOrEmpty(e.Title) ? TextStyle.Body : TextStyle.Label);

            // Click anywhere on the card to dismiss (OnPointerClick); it is not a Selectable, so focus stays put.

            t._iconImage = icon;
            t.ApplyCurrentTheme();
            t._alpha = MaTheme.ReduceMotion || !Application.isPlaying ? 1 : 0;
            t.group.alpha = t._alpha;
            panel.transform.SetAsLastSibling();
            return t;
        }

        Image _iconImage;

        /// <inheritdoc />
        public override void ApplyTheme(ThemeTokens theme)
        {
            if (stripe == null || _entry == null) return;
            var c = ControlStyles.Toast(theme.Palette, _entry.Kind).ToColor();
            MaSprites.Apply(stripe, MaSprites.Rounded(3));
            stripe.color = c;
            if (_iconImage != null) _iconImage.color = c;
        }

        void Leave()
        {
            _leaving = true;
            if (MaTheme.ReduceMotion || !Application.isPlaying) MaUi.Destroy(gameObject);
        }

        void Update()
        {
            if (group == null) return;
            float step = Time.unscaledDeltaTime / Mathf.Max(0.01f, MaTheme.Metrics.DurationBase);
            if (_leaving)
            {
                _alpha -= step;
                group.alpha = Mathf.Clamp01(_alpha);
                if (_alpha <= 0) Destroy(gameObject);
            }
            else if (_alpha < 1)
            {
                _alpha = Mathf.Min(1, _alpha + step);
                group.alpha = _alpha;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_queue = null;
            s_stack = null;
            s_visible.Clear();
        }
    }

    /// <summary>Drives the toast clock (one per toast stack).</summary>
    [AddComponentMenu("")]
    internal sealed class MaToastTicker : MonoBehaviour
    {
        void Update() => MaToast.Tick();
    }
}
