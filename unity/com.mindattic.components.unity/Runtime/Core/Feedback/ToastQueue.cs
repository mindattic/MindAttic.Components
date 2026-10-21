using System;
using System.Collections.Generic;

namespace MindAttic.Components.Unity.Core
{
    /// <summary>What a toast is about; picks its stripe colour (<see cref="ControlStyles.Toast"/>).</summary>
    public enum ToastKind
    {
        /// <summary>Neutral news (accent).</summary>
        Info,
        /// <summary>Something worked (success green).</summary>
        Success,
        /// <summary>Worth a look (warning amber).</summary>
        Warning,
        /// <summary>Something failed (danger red).</summary>
        Error,
    }

    /// <summary>One queued or visible toast.</summary>
    public sealed class ToastEntry
    {
        /// <summary>Unique, increasing id.</summary>
        public int Id { get; }
        /// <summary>The message line.</summary>
        public string Message { get; }
        /// <summary>Optional bold title above the message.</summary>
        public string Title { get; }
        /// <summary>The kind.</summary>
        public ToastKind Kind { get; }
        /// <summary>Seconds it stays visible once shown; 0 or less means until dismissed.</summary>
        public double Duration { get; }
        /// <summary>When it was shown (the clock passed to <see cref="ToastQueue.Tick"/>), or NaN while pending.</summary>
        public double ShownAt { get; internal set; } = double.NaN;

        internal ToastEntry(int id, string message, string title, ToastKind kind, double duration)
        {
            Id = id;
            Message = message;
            Title = title;
            Kind = kind;
            Duration = duration;
        }

        /// <summary>True when it has been shown and its time is up at <paramref name="now"/>.</summary>
        public bool ExpiredAt(double now) => !double.IsNaN(ShownAt) && Duration > 0 && now - ShownAt >= Duration;
    }

    /// <summary>
    /// The toast timeline, engine-free and driven by an injected clock: at most <see cref="MaxVisible"/> toasts are up
    /// at once, newer ones wait their turn, each leaves when its duration has passed or it is dismissed, and an
    /// identical message already up is not shown twice (its timer restarts instead).
    /// </summary>
    public sealed class ToastQueue
    {
        readonly List<ToastEntry> _visible = new List<ToastEntry>();
        readonly Queue<ToastEntry> _pending = new Queue<ToastEntry>();
        int _nextId = 1;

        /// <summary>How many toasts may be up at once (at least 1).</summary>
        public int MaxVisible { get; }

        /// <summary>Raised when a toast becomes visible.</summary>
        public event Action<ToastEntry> Shown;
        /// <summary>Raised when a visible toast leaves (expired or dismissed).</summary>
        public event Action<ToastEntry> Hidden;

        /// <summary>Creates a queue.</summary>
        public ToastQueue(int maxVisible = 3)
        {
            MaxVisible = Math.Max(1, maxVisible);
        }

        /// <summary>The toasts up now, oldest first.</summary>
        public IReadOnlyList<ToastEntry> Visible => _visible;

        /// <summary>How many are waiting for room.</summary>
        public int PendingCount => _pending.Count;

        /// <summary>
        /// Queues a toast and, if there is room, shows it at <paramref name="now"/>. Returns the entry, or the
        /// already-visible duplicate (same message, title and kind) whose timer was restarted.
        /// </summary>
        public ToastEntry Enqueue(string message, ToastKind kind, double duration, double now, string title = null)
        {
            foreach (var v in _visible)
            {
                if (v.Message == message && v.Title == title && v.Kind == kind)
                {
                    v.ShownAt = now;
                    return v;
                }
            }
            var entry = new ToastEntry(_nextId++, message ?? "", title, kind, duration);
            _pending.Enqueue(entry);
            Tick(now);
            return entry;
        }

        /// <summary>Retires expired toasts and promotes waiting ones at <paramref name="now"/>.</summary>
        public void Tick(double now)
        {
            for (int i = _visible.Count - 1; i >= 0; i--)
            {
                if (_visible[i].ExpiredAt(now)) Remove(i);
            }
            while (_visible.Count < MaxVisible && _pending.Count > 0)
            {
                var next = _pending.Dequeue();
                next.ShownAt = now;
                _visible.Add(next);
                Shown?.Invoke(next);
            }
        }

        /// <summary>Removes a visible or pending toast; returns false when the id is unknown.</summary>
        public bool Dismiss(int id, double now)
        {
            for (int i = 0; i < _visible.Count; i++)
            {
                if (_visible[i].Id == id)
                {
                    Remove(i);
                    Tick(now);
                    return true;
                }
            }
            int before = _pending.Count;
            var keep = new List<ToastEntry>(_pending);
            keep.RemoveAll(e => e.Id == id);
            if (keep.Count == before) return false;
            _pending.Clear();
            foreach (var e in keep) _pending.Enqueue(e);
            return true;
        }

        /// <summary>Removes everything.</summary>
        public void Clear()
        {
            _pending.Clear();
            for (int i = _visible.Count - 1; i >= 0; i--) Remove(i);
        }

        void Remove(int index)
        {
            var e = _visible[index];
            _visible.RemoveAt(index);
            Hidden?.Invoke(e);
        }
    }
}
