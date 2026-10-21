using System;

namespace MindAttic.Components.Unity.Core
{
    /// <summary>An axis-aligned rectangle by its bottom-left corner and size, y up (world units or map pixels).</summary>
    public readonly struct MapRect : IEquatable<MapRect>
    {
        /// <summary>Left edge.</summary>
        public readonly float X;
        /// <summary>Bottom edge.</summary>
        public readonly float Y;
        /// <summary>Width.</summary>
        public readonly float W;
        /// <summary>Height.</summary>
        public readonly float H;

        /// <summary>A rectangle from its bottom-left corner and size.</summary>
        public MapRect(float x, float y, float w, float h)
        {
            X = x;
            Y = y;
            W = w;
            H = h;
        }

        /// <summary>A rectangle from its centre and size.</summary>
        public static MapRect FromCenter(float cx, float cy, float w, float h) => new MapRect(cx - w / 2f, cy - h / 2f, w, h);

        /// <summary>A rectangle spanning two corners in any order.</summary>
        public static MapRect FromCorners(float x0, float y0, float x1, float y1) =>
            new MapRect(Math.Min(x0, x1), Math.Min(y0, y1), Math.Abs(x1 - x0), Math.Abs(y1 - y0));

        /// <summary>Right edge.</summary>
        public float XMax => X + W;
        /// <summary>Top edge.</summary>
        public float YMax => Y + H;
        /// <summary>Centre, across.</summary>
        public float CenterX => X + W / 2f;
        /// <summary>Centre, up.</summary>
        public float CenterY => Y + H / 2f;

        /// <summary>True when it covers no area.</summary>
        public bool IsEmpty => !(W > 0f) || !(H > 0f);

        /// <summary>The smallest rectangle holding both (a zero-size rectangle still counts as a point).</summary>
        public MapRect Union(MapRect other) =>
            FromCorners(Math.Min(X, other.X), Math.Min(Y, other.Y), Math.Max(XMax, other.XMax), Math.Max(YMax, other.YMax));

        /// <summary>Grown by <paramref name="by"/> on every side.</summary>
        public MapRect Inflate(float by) => new MapRect(X - by, Y - by, W + 2 * by, H + 2 * by);

        /// <summary>True when the point lies inside or on the edge.</summary>
        public bool Contains(float x, float y) => x >= X && x <= XMax && y >= Y && y <= YMax;

        /// <inheritdoc />
        public bool Equals(MapRect other) => X == other.X && Y == other.Y && W == other.W && H == other.H;

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is MapRect other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => ((X.GetHashCode() * 31 + Y.GetHashCode()) * 31 + W.GetHashCode()) * 31 + H.GetHashCode();

        /// <inheritdoc />
        public override string ToString() =>
            string.Format(System.Globalization.CultureInfo.InvariantCulture, "({0:0.##}, {1:0.##}) {2:0.##}x{3:0.##}", X, Y, W, H);
    }

    /// <summary>
    /// World to minimap maths for <c>MaMinimap</c>, engine-free: a world rectangle (the content's bounds) is fitted
    /// into a <see cref="Width"/> x <see cref="Height"/> map with <see cref="PaddingPx"/> of margin on every side, at
    /// one uniform scale and centred, so a small level fills the map instead of sitting in empty space. The map's
    /// origin is its bottom-left corner, y up like the world.
    /// </summary>
    public sealed class MinimapFit
    {
        /// <summary>The smallest a feature is drawn, in map pixels, so thin walls and tiny bodies never vanish.</summary>
        public const float MinFeaturePx = 2f;

        /// <exception cref="ArgumentOutOfRangeException">A size is not positive, or the padding is negative.</exception>
        public MinimapFit(float width, float height, float paddingPx = 0f)
        {
            if (!(width > 0f)) throw new ArgumentOutOfRangeException(nameof(width), width, "must be positive");
            if (!(height > 0f)) throw new ArgumentOutOfRangeException(nameof(height), height, "must be positive");
            if (!(paddingPx >= 0f)) throw new ArgumentOutOfRangeException(nameof(paddingPx), paddingPx, "must not be negative");
            Width = width;
            Height = height;
            PaddingPx = Math.Min(paddingPx, Math.Min(width, height) / 2f - 0.5f);
            if (PaddingPx < 0f) PaddingPx = 0f;
            Fit(new MapRect(0, 0, width, height));
        }

        /// <summary>The map's width, in map pixels.</summary>
        public float Width { get; }
        /// <summary>The map's height, in map pixels.</summary>
        public float Height { get; }
        /// <summary>The margin kept clear around the fitted bounds, in map pixels.</summary>
        public float PaddingPx { get; }

        /// <summary>The world rectangle last fitted (after a degenerate one was given a size).</summary>
        public MapRect Bounds { get; private set; }
        /// <summary>Map pixels per world unit (the same both ways).</summary>
        public float Scale { get; private set; }
        /// <summary>Map x of world x = 0.</summary>
        public float OffsetX { get; private set; }
        /// <summary>Map y of world y = 0.</summary>
        public float OffsetY { get; private set; }

        /// <summary>
        /// Fits <paramref name="world"/> into the map: the largest uniform scale that keeps it inside the padding,
        /// centred on the other axis. A zero-width or zero-height rectangle (a single point or a line) is given the
        /// other side's size, or 1 world unit, so the scale stays finite.
        /// </summary>
        public void Fit(MapRect world)
        {
            float w = world.W > 0f ? world.W : 0f, h = world.H > 0f ? world.H : 0f;
            if (w <= 0f && h <= 0f) w = h = 1f;
            else if (w <= 0f) w = h;
            else if (h <= 0f) h = w;
            Bounds = MapRect.FromCenter(world.CenterX, world.CenterY, w, h);
            float innerW = Width - 2f * PaddingPx, innerH = Height - 2f * PaddingPx;
            Scale = Math.Min(innerW / w, innerH / h);
            OffsetX = Width / 2f - Bounds.CenterX * Scale;
            OffsetY = Height / 2f - Bounds.CenterY * Scale;
        }

        /// <summary>The map point over world point (<paramref name="wx"/>, <paramref name="wy"/>).</summary>
        public (float X, float Y) ToMap(float wx, float wy) => (OffsetX + wx * Scale, OffsetY + wy * Scale);

        /// <summary>The world point under map point (<paramref name="mx"/>, <paramref name="my"/>) (a click on the map).</summary>
        public (float X, float Y) ToWorld(float mx, float my) => ((mx - OffsetX) / Scale, (my - OffsetY) / Scale);

        /// <summary>A world rectangle on the map, at least <paramref name="minPx"/> each way (grown about its centre), not clipped.</summary>
        public MapRect Feature(MapRect world, float minPx = MinFeaturePx)
        {
            var (x0, y0) = ToMap(world.X, world.Y);
            float w = world.W * Scale, h = world.H * Scale;
            float gw = Math.Max(minPx, w), gh = Math.Max(minPx, h);
            return new MapRect(x0 - (gw - w) / 2f, y0 - (gh - h) / 2f, gw, gh);
        }

        /// <summary>A world rectangle (a camera's view) on the map, clipped to the map's edges.</summary>
        public MapRect Viewport(MapRect world)
        {
            var (x0, y0) = ToMap(world.X, world.Y);
            var (x1, y1) = ToMap(world.XMax, world.YMax);
            return Clip(MapRect.FromCorners(x0, y0, x1, y1));
        }

        /// <summary>The part of a map rectangle on the map (empty when none of it is).</summary>
        public MapRect Clip(MapRect map)
        {
            float l = Math.Max(0f, map.X), r = Math.Min(Width, map.XMax);
            float b = Math.Max(0f, map.Y), t = Math.Min(Height, map.YMax);
            return new MapRect(l, b, Math.Max(0f, r - l), Math.Max(0f, t - b));
        }

        /// <summary>True when the map point lies on the map.</summary>
        public bool OnMap(float mx, float my) => mx >= 0f && mx <= Width && my >= 0f && my <= Height;
    }
}
