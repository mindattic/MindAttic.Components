using System;
using System.Collections.Generic;
using UnityEngine;

namespace MindAttic.Components.Unity
{
    /// <summary>The built-in icons, drawn at runtime as white line art (tinted by the component that shows them).</summary>
    public enum MaIcon
    {
        /// <summary>No icon.</summary>
        None,
        /// <summary>An X.</summary>
        Close,
        /// <summary>A check mark.</summary>
        Check,
        /// <summary>Two overlapping sheets.</summary>
        Copy,
        /// <summary>A left chevron.</summary>
        ChevronLeft,
        /// <summary>A right chevron.</summary>
        ChevronRight,
        /// <summary>An up chevron.</summary>
        ChevronUp,
        /// <summary>A down chevron (selects).</summary>
        ChevronDown,
        /// <summary>A left arrow.</summary>
        Back,
        /// <summary>A plus.</summary>
        Plus,
        /// <summary>A minus.</summary>
        Minus,
        /// <summary>Three bars.</summary>
        Menu,
        /// <summary>An i in a circle.</summary>
        Info,
        /// <summary>An ! in a circle.</summary>
        Warning,
        /// <summary>A play triangle.</summary>
        Play,
        /// <summary>Two pause bars.</summary>
        Pause,
        /// <summary>A head and shoulders.</summary>
        User,
        /// <summary>A gear.</summary>
        Settings,
        /// <summary>A filled dot.</summary>
        Dot,
        /// <summary>A door with an arrow leaving it.</summary>
        Exit,
    }

    /// <summary>
    /// Sprites for <see cref="MaIcon"/>, drawn once from line segments, circles and a triangle on a 24-unit
    /// design grid (like the web's 24px SVG icons) with antialiased round-capped strokes.
    /// </summary>
    public static class MaIcons
    {
        const int Size = 96;            // texels
        const float Grid = 24f;         // design units
        const float Stroke = 2.2f;      // design units
        static readonly Dictionary<MaIcon, Sprite> s_cache = new Dictionary<MaIcon, Sprite>();

        /// <summary>The sprite for an icon, or null for <see cref="MaIcon.None"/>.</summary>
        public static Sprite Get(MaIcon icon)
        {
            if (icon == MaIcon.None) return null;
            if (s_cache.TryGetValue(icon, out var s) && s != null) return s;
            s = Draw(icon);
            s_cache[icon] = s;
            return s;
        }

        static Sprite Draw(MaIcon icon)
        {
            var shapes = Shapes(icon);
            var tex = MaSprites.NewTexture(Size, Size);
            var px = new Color32[Size * Size];
            float k = Grid / Size; // design units per texel
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    // Design grid has y pointing down; textures have y up.
                    var p = new Vector2((x + 0.5f) * k, Grid - (y + 0.5f) * k);
                    float a = 0;
                    foreach (var s in shapes) a = Mathf.Max(a, s(p, k));
                    px[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = tex.name = "MaIcon " + icon;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        // A shape returns its coverage (0..1) at p, given k design units per texel for antialiasing.
        delegate float Shape(Vector2 p, float k);

        static Shape Line(float x1, float y1, float x2, float y2, float w = Stroke) => (p, k) =>
        {
            var a = new Vector2(x1, y1);
            var ab = new Vector2(x2, y2) - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            float d = (p - (a + ab * t)).magnitude - w / 2;
            return Mathf.Clamp01(0.5f - d / k);
        };

        static Shape Poly(params float[] xy)
        {
            var parts = new List<Shape>();
            for (int i = 0; i + 3 < xy.Length; i += 2) parts.Add(Line(xy[i], xy[i + 1], xy[i + 2], xy[i + 3]));
            return (p, k) =>
            {
                float a = 0;
                foreach (var s in parts) a = Mathf.Max(a, s(p, k));
                return a;
            };
        }

        static Shape Disc(float cx, float cy, float r) => (p, k) =>
            Mathf.Clamp01(0.5f - ((p - new Vector2(cx, cy)).magnitude - r) / k);

        static Shape Circle(float cx, float cy, float r, float w = Stroke) => (p, k) =>
            Mathf.Clamp01(0.5f - (Mathf.Abs((p - new Vector2(cx, cy)).magnitude - r) - w / 2) / k);

        static Shape Box(float x1, float y1, float x2, float y2, float r, float w = Stroke) => (p, k) =>
        {
            float cx = (x1 + x2) / 2, cy = (y1 + y2) / 2, hx = (x2 - x1) / 2, hy = (y2 - y1) / 2;
            float qx = Mathf.Abs(p.x - cx) - (hx - r), qy = Mathf.Abs(p.y - cy) - (hy - r);
            float d = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
            return w <= 0 ? Mathf.Clamp01(0.5f - d / k) : Mathf.Clamp01(0.5f - (Mathf.Abs(d) - w / 2) / k);
        };

        static Shape Triangle(float ax, float ay, float bx, float by, float cx, float cy) => (p, k) =>
        {
            // Signed distance as the max of the three edge half-plane distances (good enough for a convex icon).
            float E(float x1, float y1, float x2, float y2)
            {
                var n = new Vector2(y2 - y1, -(x2 - x1)).normalized;
                return Vector2.Dot(p - new Vector2(x1, y1), n);
            }
            float s = Mathf.Sign((bx - ax) * (cy - ay) - (by - ay) * (cx - ax));
            float d = Mathf.Max(s * E(ax, ay, bx, by), Mathf.Max(s * E(bx, by, cx, cy), s * E(cx, cy, ax, ay)));
            return Mathf.Clamp01(0.5f - d / k);
        };

        static Shape[] Shapes(MaIcon icon)
        {
            switch (icon)
            {
                case MaIcon.Close: return new[] { Line(6, 6, 18, 18), Line(18, 6, 6, 18) };
                case MaIcon.Check: return new[] { Poly(5, 12.5f, 10, 17.5f, 19, 7) };
                case MaIcon.Copy: return new[] { Box(8.5f, 8.5f, 19.5f, 19.5f, 2), Poly(5, 15.5f, 5, 6, 6.5f, 4.5f, 15.5f, 4.5f) };
                case MaIcon.ChevronLeft: return new[] { Poly(15, 5, 8, 12, 15, 19) };
                case MaIcon.ChevronRight: return new[] { Poly(9, 5, 16, 12, 9, 19) };
                case MaIcon.ChevronUp: return new[] { Poly(5, 15, 12, 8, 19, 15) };
                case MaIcon.ChevronDown: return new[] { Poly(5, 9, 12, 16, 19, 9) };
                case MaIcon.Back: return new[] { Line(19, 12, 5.5f, 12), Poly(11, 6, 5, 12, 11, 18) };
                case MaIcon.Plus: return new[] { Line(12, 5, 12, 19), Line(5, 12, 19, 12) };
                case MaIcon.Minus: return new[] { Line(5, 12, 19, 12) };
                case MaIcon.Menu: return new[] { Line(4.5f, 7, 19.5f, 7), Line(4.5f, 12, 19.5f, 12), Line(4.5f, 17, 19.5f, 17) };
                case MaIcon.Info: return new[] { Circle(12, 12, 9.5f), Line(12, 11, 12, 16.5f), Disc(12, 7.6f, 1.35f) };
                case MaIcon.Warning: return new[] { Circle(12, 12, 9.5f), Line(12, 7, 12, 12.8f), Disc(12, 16.4f, 1.35f) };
                case MaIcon.Play: return new[] { Triangle(8, 5, 19, 12, 8, 19) };
                case MaIcon.Pause: return new[] { Line(8.5f, 5.5f, 8.5f, 18.5f, 3.2f), Line(15.5f, 5.5f, 15.5f, 18.5f, 3.2f) };
                case MaIcon.User: return new[] { Circle(12, 8, 3.8f), Box(4.5f, 15, 19.5f, 27, 6) };
                case MaIcon.Settings: return Gear();
                case MaIcon.Dot: return new[] { Disc(12, 12, 6) };
                case MaIcon.Exit: return new[] { Poly(13, 4.5f, 5, 4.5f, 5, 19.5f, 13, 19.5f), Line(10, 12, 20, 12), Poly(16.5f, 8, 20.5f, 12, 16.5f, 16) };
                default: return new Shape[0];
            }
        }

        static Shape[] Gear()
        {
            var shapes = new List<Shape> { Circle(12, 12, 5.2f, 2.4f), Disc(12, 12, 1.6f) };
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                shapes.Add(Line(12 + c * 7.2f, 12 + s * 7.2f, 12 + c * 9.6f, 12 + s * 9.6f, 3f));
            }
            return shapes.ToArray();
        }
    }
}
