using System;

namespace MindAttic.Components.Unity.Core
{
    /// <summary>
    /// Transition timing shared by every component: the web's <c>cubic-bezier(0.4, 0, 0.2, 1)</c> ease and the
    /// reduce-motion rule (with reduce motion on, every transition is instant and nothing slides or scales).
    /// </summary>
    public static class Motion
    {
        /// <summary>The standard ease, <c>cubic-bezier(0.4, 0, 0.2, 1)</c>, at progress <paramref name="t"/> (0..1).</summary>
        public static float Ease(float t) => CubicBezier(0.4f, 0f, 0.2f, 1f, t);

        /// <summary>A transition's duration: <paramref name="seconds"/>, or 0 when <paramref name="reduceMotion"/> is on.</summary>
        public static float Duration(float seconds, bool reduceMotion) => reduceMotion || seconds <= 0 ? 0f : seconds;

        /// <summary>
        /// Eased progress (0..1) of a transition <paramref name="elapsed"/> seconds in. A zero duration (or reduce
        /// motion) is already complete.
        /// </summary>
        public static float Progress(float elapsed, float seconds, bool reduceMotion)
        {
            float d = Duration(seconds, reduceMotion);
            if (d <= 0) return 1f;
            float t = elapsed / d;
            return t <= 0 ? 0f : t >= 1 ? 1f : Ease(t);
        }

        /// <summary>
        /// The fraction of the remaining distance to cover this frame for a smooth exponential approach that is
        /// about 95% done after <paramref name="seconds"/>; 1 (snap) when reduce motion is on or seconds is 0.
        /// </summary>
        public static float Approach(float deltaTime, float seconds, bool reduceMotion)
        {
            float d = Duration(seconds, reduceMotion);
            if (d <= 0 || deltaTime >= d) return 1f;
            if (deltaTime <= 0) return 0f;
            return 1f - (float)Math.Exp(-3.0 * deltaTime / d);
        }

        /// <summary>The lowest opacity a <see cref="Pulse"/> dips to.</summary>
        public const float PulseMin = 0.35f;

        /// <summary>
        /// A soft breathing opacity (1 down to <see cref="PulseMin"/> and back, once per <paramref name="period"/>
        /// seconds) at clock <paramref name="time"/>, for something in progress (a "Connecting..." status dot). With
        /// reduce motion, or a period of 0, it holds at 1.
        /// </summary>
        public static float Pulse(double time, float period, bool reduceMotion)
        {
            if (reduceMotion || !(period > 0)) return 1f;
            double phase = time / period;
            phase -= Math.Floor(phase);
            float wave = (float)(0.5 + 0.5 * Math.Cos(phase * 2 * Math.PI)); // 1 at the start of each period
            return PulseMin + (1f - PulseMin) * wave;
        }

        /// <summary>A CSS <c>cubic-bezier(x1, y1, x2, y2)</c> timing function at <paramref name="t"/>.</summary>
        public static float CubicBezier(float x1, float y1, float x2, float y2, float t)
        {
            if (t <= 0) return 0;
            if (t >= 1) return 1;
            // Solve x(u) = t for u by Newton, falling back to bisection.
            float u = t;
            for (int i = 0; i < 8; i++)
            {
                float x = Bezier(x1, x2, u) - t;
                if (Math.Abs(x) < 1e-6f) return Bezier(y1, y2, u);
                float dx = BezierSlope(x1, x2, u);
                if (Math.Abs(dx) < 1e-6f) break;
                u -= x / dx;
            }
            float lo = 0, hi = 1;
            u = t;
            for (int i = 0; i < 32; i++)
            {
                float x = Bezier(x1, x2, u);
                if (Math.Abs(x - t) < 1e-6f) break;
                if (x < t) lo = u; else hi = u;
                u = (lo + hi) / 2;
            }
            return Bezier(y1, y2, u);
        }

        static float Bezier(float p1, float p2, float u) =>
            3 * (1 - u) * (1 - u) * u * p1 + 3 * (1 - u) * u * u * p2 + u * u * u;

        static float BezierSlope(float p1, float p2, float u) =>
            3 * (1 - u) * (1 - u) * p1 + 6 * (1 - u) * u * (p2 - p1) + 3 * u * u * (1 - p2);
    }
}
