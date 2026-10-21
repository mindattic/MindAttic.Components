using System;
using System.Collections.Generic;

namespace MindAttic.Components.Unity.Core
{
    /// <summary>One banner: a title, an optional subtitle and how long it stays.</summary>
    public sealed class BannerEntry
    {
        /// <summary>Unique, increasing id.</summary>
        public int Id { get; }
        /// <summary>The large line.</summary>
        public string Title { get; }
        /// <summary>The muted line under it (empty for none).</summary>
        public string Subtitle { get; }
        /// <summary>Seconds it is fully shown, between its fade in and fade out.</summary>
        public double Seconds { get; }
        /// <summary>When it started showing (the clock passed to <see cref="BannerQueue.Tick"/>), or NaN while waiting.</summary>
        public double ShownAt { get; internal set; } = double.NaN;

        internal BannerEntry(int id, string title, string subtitle, double seconds)
        {
            Id = id;
            Title = title ?? "";
            Subtitle = subtitle ?? "";
            Seconds = seconds;
        }
    }

    /// <summary>
    /// The banner timeline, engine-free and driven by an injected clock: one banner at a time, each fades in over
    /// <see cref="FadeSeconds"/>, holds for its own seconds and fades out over <see cref="FadeSeconds"/>; later ones
    /// wait their turn (or replace the current one when shown with <c>replace</c>). With reduce motion the fades
    /// are instant.
    /// </summary>
    public sealed class BannerQueue
    {
        readonly Queue<BannerEntry> _pending = new Queue<BannerEntry>();
        int _nextId = 1;

        /// <summary>Creates a queue whose banners fade over <paramref name="fadeSeconds"/>.</summary>
        public BannerQueue(float fadeSeconds = 0.28f)
        {
            FadeSeconds = Math.Max(0f, fadeSeconds);
        }

        /// <summary>Seconds a banner takes to fade in, and again to fade out.</summary>
        public float FadeSeconds { get; set; }

        /// <summary>Fades are instant (the theme's reduce motion).</summary>
        public bool ReduceMotion { get; set; }

        /// <summary>The banner on screen, or null.</summary>
        public BannerEntry Current { get; private set; }

        /// <summary>How many are waiting.</summary>
        public int PendingCount => _pending.Count;

        /// <summary>Raised when a banner starts showing.</summary>
        public event Action<BannerEntry> Shown;

        /// <summary>Raised when a banner has faded out (or was dismissed).</summary>
        public event Action<BannerEntry> Hidden;

        double Fade => Motion.Duration(FadeSeconds, ReduceMotion);

        /// <summary>
        /// Queues a banner for <paramref name="seconds"/> and, if nothing is showing, shows it at <paramref name="now"/>.
        /// <paramref name="replace"/> drops the current banner and anything waiting and shows this one now.
        /// </summary>
        public BannerEntry Enqueue(string title, string subtitle, double seconds, double now, bool replace = false)
        {
            var entry = new BannerEntry(_nextId++, title, subtitle, Math.Max(0, seconds));
            if (replace)
            {
                _pending.Clear();
                HideCurrent();
            }
            _pending.Enqueue(entry);
            Tick(now);
            return entry;
        }

        /// <summary>Retires a finished banner and starts the next one at <paramref name="now"/>.</summary>
        public void Tick(double now)
        {
            if (Current != null && now - Current.ShownAt >= TotalSeconds(Current)) HideCurrent();
            if (Current == null && _pending.Count > 0)
            {
                Current = _pending.Dequeue();
                Current.ShownAt = now;
                Shown?.Invoke(Current);
            }
        }

        /// <summary>
        /// The current banner's opacity at <paramref name="now"/>: 0..1 eased in over the fade, 1 while it holds,
        /// 1..0 over the fade out; 0 when nothing is showing.
        /// </summary>
        public float Opacity(double now)
        {
            if (Current == null) return 0f;
            double t = now - Current.ShownAt, fade = Fade, total = TotalSeconds(Current);
            if (t < 0 || t >= total) return 0f;
            if (fade <= 0) return 1f;
            if (t < fade) return Motion.Ease((float)(t / fade));
            if (t > total - fade) return Motion.Ease((float)((total - t) / fade));
            return 1f;
        }

        /// <summary>Total on-screen time of <paramref name="entry"/>: fade in + hold + fade out.</summary>
        public double TotalSeconds(BannerEntry entry) => entry == null ? 0 : entry.Seconds + 2 * Fade;

        /// <summary>Hides the current banner now and shows the next waiting one.</summary>
        public void Dismiss(double now)
        {
            HideCurrent();
            Tick(now);
        }

        /// <summary>Removes everything.</summary>
        public void Clear()
        {
            _pending.Clear();
            HideCurrent();
        }

        void HideCurrent()
        {
            var e = Current;
            if (e == null) return;
            Current = null;
            Hidden?.Invoke(e);
        }
    }
}
