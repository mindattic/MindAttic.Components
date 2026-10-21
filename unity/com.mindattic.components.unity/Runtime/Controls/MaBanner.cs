using MindAttic.Components.Unity.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// A timed HUD banner: a card with a large title and an optional muted subtitle that fades in, holds and fades
    /// out ("Level 3", "Round over"). Banners shown while one is up wait their turn (<see cref="BannerQueue"/>), or
    /// replace it with <c>replace: true</c>. It never takes focus or blocks the pointer; with reduce motion the
    /// fades are instant. Centred on its parent by default; move <see cref="MaElement.RectTransform"/> to place it.
    /// <code>var banner = MaBanner.Create(hudCanvas);
    /// banner.Show("Level 2", "Push every block onto its goal");</code>
    /// </summary>
    [AddComponentMenu("MindAttic/UI/Banner")]
    public sealed class MaBanner : MaElement
    {
        /// <summary>Seconds a banner holds when <see cref="Show"/> is given none.</summary>
        public const float DefaultSeconds = 3f;

        [SerializeField] MaPanel card;
        [SerializeField] MaText title;
        [SerializeField] MaText subtitle;
        [SerializeField] CanvasGroup group;

        BannerQueue _queue;

        /// <summary>Creates a banner under <paramref name="parent"/>, <paramref name="width"/> canvas units wide, hidden.</summary>
        public static MaBanner Create(Component parent, float width = 1100f)
        {
            var m = MaTheme.Metrics;
            var rt = MaUi.Rect("Banner", ResolveParent(parent));
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, 0f);
            var b = rt.gameObject.AddComponent<MaBanner>();
            var fitRoot = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            fitRoot.childControlWidth = fitRoot.childControlHeight = true;
            fitRoot.childForceExpandWidth = true;
            fitRoot.childForceExpandHeight = false;
            rt.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            b.card = MaPanel.Create(rt, PanelStyle.Surface, "Card");
            b.group = b.card.gameObject.AddComponent<CanvasGroup>();
            b.group.interactable = false;
            b.group.blocksRaycasts = false;
            var col = MaUi.VColumn(b.card.gameObject, m.SpaceMd, MaUi.Pad(m.SpaceXxl, m.SpaceXl), TextAnchor.UpperCenter);
            col.childForceExpandWidth = true;
            b.title = MaText.Create(b.card, "", TextStyle.Title, TextAlignmentOptions.Center);
            b.subtitle = MaText.Create(b.card, "", TextStyle.Muted, TextAlignmentOptions.Center);
            b.card.gameObject.SetActive(false);
            b.ApplyCurrentTheme();
            return b;
        }

        BannerQueue Queue
        {
            get
            {
                if (_queue != null) return _queue;
                _queue = new BannerQueue(MaTheme.Metrics.DurationBase);
                _queue.Shown += OnShown;
                _queue.Hidden += OnHidden;
                return _queue;
            }
        }

        static double Now => Time.unscaledTimeAsDouble;

        /// <summary>
        /// Shows a banner for <paramref name="seconds"/> (&lt;= 0: <see cref="DefaultSeconds"/>) after any already
        /// waiting, or at once in place of them with <paramref name="replace"/>.
        /// </summary>
        public BannerEntry Show(string title, string subtitle = null, float seconds = 0f, bool replace = false)
        {
            var q = Queue;
            q.FadeSeconds = MaTheme.Metrics.DurationBase;
            q.ReduceMotion = MaTheme.ReduceMotion || !Application.isPlaying;
            var e = q.Enqueue(title, subtitle, seconds > 0 ? seconds : DefaultSeconds, Now, replace);
            Tick();
            return e;
        }

        /// <summary>Hides the banner now and shows the next waiting one.</summary>
        public void Dismiss()
        {
            Queue.Dismiss(Now);
            Tick();
        }

        /// <summary>Hides the banner and drops any waiting.</summary>
        public void Clear()
        {
            Queue.Clear();
            Tick();
        }

        /// <summary>True while a banner is on screen (fading or holding).</summary>
        public bool IsShowing => card != null && card.gameObject.activeSelf;

        /// <summary>The banner on screen, or null.</summary>
        public BannerEntry Current => _queue?.Current;

        /// <summary>How many banners are waiting.</summary>
        public int PendingCount => _queue?.PendingCount ?? 0;

        /// <summary>The card's opacity right now.</summary>
        public float Opacity => group != null && IsShowing ? group.alpha : 0f;

        /// <summary>The card.</summary>
        public MaPanel Card => card;

        /// <summary>The title line.</summary>
        public MaText Title => title;

        /// <summary>The subtitle line (hidden when empty).</summary>
        public MaText Subtitle => subtitle;

        /// <inheritdoc />
        public override void ApplyTheme(ThemeTokens theme)
        {
            // The card and texts restyle themselves.
        }

        /// <inheritdoc />
        public override MenuItemDefinition Describe() => new MenuItemDefinition(MenuItemKind.Text, title != null ? title.Text : name, false);

        void OnShown(BannerEntry e)
        {
            title.Text = e.Title;
            subtitle.Text = e.Subtitle;
            subtitle.Visible = e.Subtitle.Length > 0;
            card.gameObject.SetActive(true);
        }

        void OnHidden(BannerEntry e)
        {
            if (_queue == null || _queue.Current == null) card.gameObject.SetActive(false);
        }

        void Update() => Tick();

        void Tick()
        {
            if (_queue == null || card == null) return;
            _queue.ReduceMotion = MaTheme.ReduceMotion || !Application.isPlaying;
            _queue.Tick(Now);
            if (group != null) group.alpha = _queue.Opacity(Now);
        }
    }
}
