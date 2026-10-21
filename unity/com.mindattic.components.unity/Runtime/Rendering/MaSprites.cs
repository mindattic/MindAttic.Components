using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// The package's only artwork, generated at runtime (no third-party or imported textures): nine-sliced
    /// rounded rectangles, rings, soft shadows and the spinner arc, antialiased from signed distance fields at
    /// <see cref="Density"/> texels per canvas unit so corners stay crisp on a 4K screen. Cached per size.
    /// </summary>
    public static class MaSprites
    {
        /// <summary>Texels per canvas unit; images using these sprites set <c>pixelsPerUnitMultiplier</c> to it.</summary>
        public const int Density = 2;

        static readonly Dictionary<string, Sprite> s_cache = new Dictionary<string, Sprite>();

        /// <summary>A filled rounded rectangle, sliced, corner radius in canvas units (0 for square).</summary>
        public static Sprite Rounded(float radius) =>
            Get("r" + Quant(radius), () => Build(radius, 0, 0));

        /// <summary>A rounded outline <paramref name="thickness"/> wide, sliced.</summary>
        public static Sprite Ring(float radius, float thickness) =>
            Get("o" + Quant(radius) + "_" + Quant(thickness), () => Build(radius, thickness, 0));

        /// <summary>A soft shadow of a rounded rectangle, blurred over <paramref name="blur"/> units, sliced.</summary>
        public static Sprite Shadow(float radius, float blur) =>
            Get("s" + Quant(radius) + "_" + Quant(blur), () => Build(radius, 0, blur));

        /// <summary>A 270-degree arc with a fading tail, for <see cref="MaSpinner"/>.</summary>
        public static Sprite SpinnerArc => Get("arc", BuildArc);

        /// <summary>Puts a sliced sprite on an image the way every component does.</summary>
        public static void Apply(Image image, Sprite sprite)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.pixelsPerUnitMultiplier = Density;
            image.fillCenter = true;
        }

        static string Quant(float v) => Mathf.RoundToInt(Mathf.Max(0, v) * 4).ToString();

        static Sprite Get(string key, Func<Sprite> make)
        {
            if (s_cache.TryGetValue(key, out var s) && s != null) return s;
            s = make();
            s_cache[key] = s;
            return s;
        }

        static Sprite Build(float radius, float thickness, float blur)
        {
            int r = Mathf.Max(1, Mathf.CeilToInt(radius * Density));
            int t = Mathf.CeilToInt(thickness * Density);
            int b = Mathf.CeilToInt(blur * Density);
            if (t > 0) r = Mathf.Max(r, t + 1);
            int edge = r + b;
            int size = edge * 2 + 2;
            var tex = NewTexture(size, size);
            var px = new Color32[size * size];
            float half = size / 2f;
            float inner = half - b; // half-extent of the shape itself
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = RoundedBox(x + 0.5f - half, y + 0.5f - half, inner, inner, r);
                    float a;
                    if (b > 0)
                        a = 1f - Smooth(-b * 0.35f, b, d); // soft falloff that starts a little inside the edge
                    else if (t > 0)
                        a = Mathf.Clamp01(0.5f - d) - Mathf.Clamp01(0.5f - (d + t));
                    else
                        a = Mathf.Clamp01(0.5f - d);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(edge, edge, edge, edge));
            sprite.name = tex.name = "MaSprite " + (b > 0 ? "shadow" : t > 0 ? "ring" : "rounded") + " r" + radius;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        static Sprite BuildArc()
        {
            const int size = 128;
            var tex = NewTexture(size, size);
            var px = new Color32[size * size];
            float c = size / 2f, outer = size / 2f - 1, thick = size * 0.11f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - c, dy = y + 0.5f - c;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float ring = Mathf.Clamp01(0.5f - Mathf.Abs(dist - (outer - thick / 2)) + thick / 2);
                    float ang = (Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 360f + 90f) % 360f; // 0 at top, clockwise-ish
                    float sweep = ang / 270f;
                    float a = sweep <= 1f ? ring * Mathf.Lerp(0.15f, 1f, sweep) : 0f;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = tex.name = "MaSprite arc";
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        internal static Texture2D NewTexture(int w, int h) =>
            new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };

        // Signed distance from a point to a box of half-extents (hx, hy) with corner radius r (negative inside).
        static float RoundedBox(float px, float py, float hx, float hy, float r)
        {
            float qx = Mathf.Abs(px) - (hx - r);
            float qy = Mathf.Abs(py) - (hy - r);
            float ox = Mathf.Max(qx, 0), oy = Mathf.Max(qy, 0);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
        }

        static float Smooth(float e0, float e1, float x)
        {
            float t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3 - 2 * t);
        }
    }
}
