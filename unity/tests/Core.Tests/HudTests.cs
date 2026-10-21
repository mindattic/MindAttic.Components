using System;
using System.Collections.Generic;
using MindAttic.Components.Unity.Core;
using NUnit.Framework;

namespace MindAttic.Components.Unity.Core.Tests
{
    public class MinimapFitTests
    {
        const float Tol = 1e-3f;

        [Test]
        public void A_small_level_fills_the_map_inside_the_padding_instead_of_sitting_in_empty_space()
        {
            // A 400x200 level in a 4000-wide world: fitted to its own bounds on a 160 map with 8 px padding.
            var fit = new MinimapFit(160, 160, 8);
            fit.Fit(new MapRect(1000, 2000, 400, 200));

            Assert.That(fit.Scale, Is.EqualTo(144f / 400f).Within(Tol), "the wider side spans the map less the padding");
            var (lx, by) = fit.ToMap(1000, 2000);
            var (rx, ty) = fit.ToMap(1400, 2200);
            Assert.That(lx, Is.EqualTo(8).Within(Tol));
            Assert.That(rx, Is.EqualTo(152).Within(Tol));
            Assert.That((by + ty) / 2, Is.EqualTo(80).Within(Tol), "centred on the short axis");
            Assert.That(ty - by, Is.EqualTo(72).Within(Tol), "one uniform scale: aspect kept");
        }

        [Test]
        public void World_and_map_round_trip()
        {
            var fit = new MinimapFit(200, 120, 10);
            fit.Fit(new MapRect(-500, -300, 1000, 900));
            foreach (var (x, y) in new[] { (0f, 0f), (-500f, -300f), (500f, 600f), (123.4f, -56.7f) })
            {
                var (mx, my) = fit.ToMap(x, y);
                var (wx, wy) = fit.ToWorld(mx, my);
                Assert.That(wx, Is.EqualTo(x).Within(0.01f));
                Assert.That(wy, Is.EqualTo(y).Within(0.01f));
            }
            var (cx, cy) = fit.ToMap(0, 150);
            Assert.That(cx, Is.EqualTo(100).Within(Tol));
            Assert.That(cy, Is.EqualTo(60).Within(Tol));
        }

        [Test]
        public void A_point_or_a_line_still_fits_with_a_finite_scale()
        {
            var fit = new MinimapFit(100, 100);
            fit.Fit(new MapRect(50, 50, 0, 0));
            Assert.That(float.IsInfinity(fit.Scale) || float.IsNaN(fit.Scale), Is.False);
            Assert.That(fit.ToMap(50, 50).X, Is.EqualTo(50).Within(Tol));
            fit.Fit(new MapRect(0, 10, 300, 0));
            Assert.That(fit.Bounds.H, Is.EqualTo(300));
            Assert.That(fit.Scale, Is.EqualTo(100f / 300f).Within(Tol));
        }

        [Test]
        public void Features_keep_a_minimum_size_about_their_centre()
        {
            var fit = new MinimapFit(160, 160);
            fit.Fit(new MapRect(0, 0, 16000, 16000)); // 0.01 px per unit
            var r = fit.Feature(MapRect.FromCenter(8000, 8000, 20, 40));
            Assert.That(r.W, Is.EqualTo(MinimapFit.MinFeaturePx));
            Assert.That(r.H, Is.EqualTo(MinimapFit.MinFeaturePx));
            Assert.That(r.CenterX, Is.EqualTo(80).Within(Tol));
            Assert.That(r.CenterY, Is.EqualTo(80).Within(Tol));
        }

        [Test]
        public void The_viewport_is_clipped_to_the_map()
        {
            var fit = new MinimapFit(100, 100);
            fit.Fit(new MapRect(0, 0, 1000, 1000));
            var v = fit.Viewport(new MapRect(-200, 900, 400, 300));
            Assert.That(v, Is.EqualTo(new MapRect(0, 90, 20, 10)));
            Assert.That(fit.Viewport(new MapRect(5000, 5000, 10, 10)).IsEmpty, Is.True);
        }

        [Test]
        public void Bounds_union_covers_every_feature()
        {
            var a = new MapRect(0, 0, 10, 10);
            var b = MapRect.FromCenter(100, -50, 20, 20);
            var u = a.Union(b);
            Assert.That(u, Is.EqualTo(new MapRect(0, -60, 110, 70)));
            Assert.That(u.Contains(5, 5) && u.Contains(100, -50), Is.True);
            Assert.That(a.Inflate(5), Is.EqualTo(new MapRect(-5, -5, 20, 20)));
        }

        [Test]
        public void Rejects_a_map_without_size()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MinimapFit(0, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MinimapFit(10, 10, -1));
        }
    }

    public class BannerQueueTests
    {
        [Test]
        public void A_banner_fades_in_holds_and_fades_out()
        {
            var q = new BannerQueue(0.5f);
            var e = q.Enqueue("Level 2", "Push the blocks home", 3, now: 10);
            Assert.That(q.Current, Is.SameAs(e));
            Assert.That(q.Opacity(10), Is.EqualTo(0).Within(1e-4));
            Assert.That(q.Opacity(10.25), Is.GreaterThan(0).And.LessThan(1));
            Assert.That(q.Opacity(11), Is.EqualTo(1));
            Assert.That(q.Opacity(13.75), Is.GreaterThan(0).And.LessThan(1));
            Assert.That(q.TotalSeconds(e), Is.EqualTo(4));
            q.Tick(14);
            Assert.That(q.Current, Is.Null);
            Assert.That(q.Opacity(14), Is.EqualTo(0));
        }

        [Test]
        public void Later_banners_wait_their_turn_unless_they_replace()
        {
            var q = new BannerQueue(0);
            var shown = new List<string>();
            var hidden = new List<string>();
            q.Shown += b => shown.Add(b.Title);
            q.Hidden += b => hidden.Add(b.Title);
            q.Enqueue("One", null, 2, 0);
            q.Enqueue("Two", "", 2, 0.5);
            Assert.That(q.Current.Title, Is.EqualTo("One"));
            Assert.That(q.PendingCount, Is.EqualTo(1));
            q.Tick(2);
            Assert.That(q.Current.Title, Is.EqualTo("Two"));
            q.Enqueue("Three", null, 2, 2.5, replace: true);
            Assert.That(q.Current.Title, Is.EqualTo("Three"));
            Assert.That(shown, Is.EqualTo(new[] { "One", "Two", "Three" }));
            Assert.That(hidden, Is.EqualTo(new[] { "One", "Two" }));
            q.Dismiss(3);
            Assert.That(q.Current, Is.Null);
        }

        [Test]
        public void Reduce_motion_makes_the_fade_instant()
        {
            var q = new BannerQueue(0.5f) { ReduceMotion = true };
            q.Enqueue("Level 1", null, 2, 0);
            Assert.That(q.Opacity(0), Is.EqualTo(1));
            Assert.That(q.Opacity(1.99), Is.EqualTo(1));
            Assert.That(q.Opacity(2), Is.EqualTo(0));
        }
    }

    public class StatusTests
    {
        [Test]
        public void Pulse_breathes_between_its_floor_and_full_and_holds_with_reduce_motion()
        {
            Assert.That(Motion.Pulse(0, 1.2f, false), Is.EqualTo(1).Within(1e-4));
            Assert.That(Motion.Pulse(0.6, 1.2f, false), Is.EqualTo(Motion.PulseMin).Within(1e-4));
            Assert.That(Motion.Pulse(1.2, 1.2f, false), Is.EqualTo(1).Within(1e-4));
            for (double t = 0; t < 3; t += 0.07)
                Assert.That(Motion.Pulse(t, 1.2f, false), Is.InRange(Motion.PulseMin - 1e-4, 1 + 1e-4));
            Assert.That(Motion.Pulse(0.6, 1.2f, true), Is.EqualTo(1));
            Assert.That(Motion.Pulse(0.6, 0, false), Is.EqualTo(1));
        }

        [TestCase("dark")]
        [TestCase("light")]
        public void Status_pill_reads_in_both_themes(string theme)
        {
            var p = ThemePalette.BuiltIn(theme);
            foreach (StatusKind kind in Enum.GetValues(typeof(StatusKind)))
            {
                var c = ControlStyles.Status(p, kind);
                Assert.That(Contrast.Ratio(c.Text, c.Fill), Is.GreaterThanOrEqualTo(ThemeValidator.Text), kind + " label");
                Assert.That(Contrast.Ratio(ControlStyles.StatusDot(p, kind), c.Fill), Is.GreaterThanOrEqualTo(ThemeValidator.NonText), kind + " dot");
            }
            Assert.That(ControlStyles.StatusDot(p, StatusKind.Ok), Is.EqualTo(p.Success));
            Assert.That(ControlStyles.StatusDot(p, StatusKind.Error), Is.EqualTo(p.Danger));
        }
    }
}
