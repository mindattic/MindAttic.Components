using System;
using System.Collections.Generic;
using MindAttic.Components.Unity.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// A HUD minimap: an inset panel showing caller-supplied world rectangles and points in their own colours, the
    /// camera's view as an outline, and a click (or drag) on it reported as a world point to pan to. The game owns
    /// what is drawn: each frame (or less often) it calls <see cref="Clear"/>, adds what it wants with
    /// <see cref="AddRect"/> and <see cref="AddPoint"/>, sets <see cref="SetViewport"/>, then <see cref="Apply"/>.
    /// With <see cref="AutoFit"/> (the default) the map fits the bounds of everything added with <c>fit: true</c>
    /// (walls, goals, bodies) plus <see cref="Padding"/>, so a small level fills the map; <see cref="FitTo"/> pins
    /// the world bounds instead. The maths is the engine-free <see cref="MinimapFit"/>; the world is y up.
    /// <code>map.Clear();
    /// foreach (var wall in level.Walls) map.AddRect(MapRect.FromCenter(wall.X, wall.Y, wall.W, wall.H), wallColor);
    /// map.AddPoint(player.x, player.y, playerColor, 2);
    /// map.SetViewport(cameraWorldRect);
    /// map.Apply();
    /// map.Panned += world =&gt; camera.CenterOn(world);</code>
    /// </summary>
    [AddComponentMenu("MindAttic/UI/Minimap")]
    public sealed class MaMinimap : MaElement, IPointerDownHandler, IDragHandler
    {
        /// <summary>Default edge of the map on a 1920x1080 canvas.</summary>
        public const float DefaultSize = 320f;
        /// <summary>Default texels along each edge of the map texture.</summary>
        public const int DefaultResolution = 160;
        /// <summary>Default margin around the fitted content, in texels.</summary>
        public const float DefaultPadding = 8f;

        const float Inset = 4f;

        struct Item
        {
            public MapRect World;
            public Color32 Color;
            public int Radius; // -1 for a rectangle
        }

        [SerializeField] MaPanel panel;
        [SerializeField] RawImage image;
        [SerializeField] int resolution = DefaultResolution;
        [SerializeField] float padding = DefaultPadding;
        [SerializeField] bool autoFit = true;
        [SerializeField] Color backdrop = new Color(0, 0, 0, 0);

        readonly List<Item> _items = new List<Item>();
        MapRect? _contentBounds;
        MapRect? _viewport;
        Color? _viewportColor;
        Color32[] _pixels;
        Texture2D _texture;
        MinimapFit _fit;
        MapRect _fixedBounds;

        /// <summary>
        /// Creates a minimap under <paramref name="parent"/>: <paramref name="size"/> canvas units square, drawn into a
        /// <paramref name="resolution"/>-texel texture with <paramref name="padding"/> texels kept clear around the content.
        /// </summary>
        public static MaMinimap Create(Component parent, float size = DefaultSize, int resolution = DefaultResolution, float padding = DefaultPadding)
        {
            var p = MaPanel.Create(ResolveParent(parent), PanelStyle.Inset, "Minimap");
            var rt = p.RectTransform;
            rt.sizeDelta = new Vector2(size + Inset * 2, size + Inset * 2);
            MaUi.Layout(p.gameObject, size + Inset * 2, size + Inset * 2, size + Inset * 2, size + Inset * 2, 0, 0);
            var m = p.gameObject.AddComponent<MaMinimap>();
            m.panel = p;
            m.resolution = Mathf.Max(8, resolution);
            m.padding = Mathf.Max(0, padding);

            var imageRt = MaUi.Rect("Map", rt);
            MaUi.Stretch(imageRt, Inset, Inset, Inset, Inset);
            MaUi.GetOrAdd<LayoutElement>(imageRt.gameObject).ignoreLayout = true;
            m.image = imageRt.gameObject.AddComponent<RawImage>();
            m.image.raycastTarget = true; // clicks reach OnPointerDown on the panel
            m.EnsureTexture();
            m.ApplyCurrentTheme();
            m.Apply();
            return m;
        }

        /// <summary>Raised with the world point under a click or drag on the map.</summary>
        public event Action<Vector2> Panned;

        /// <summary>The panel behind the map.</summary>
        public MaPanel Panel => panel;

        /// <summary>The map image.</summary>
        public RawImage Image => image;

        /// <summary>The texture the map is drawn into (y up, like the world).</summary>
        public Texture2D Texture
        {
            get
            {
                EnsureTexture();
                return _texture;
            }
        }

        /// <summary>Texels along each edge.</summary>
        public int Resolution => resolution;

        /// <summary>The current world-to-map transform (what the last <see cref="Apply"/> drew with).</summary>
        public MinimapFit Fit
        {
            get
            {
                EnsureTexture();
                return _fit;
            }
        }

        /// <summary>Fit the map to the bounds of the content added with <c>fit: true</c> on each <see cref="Apply"/>.</summary>
        public bool AutoFit
        {
            get => autoFit;
            set => autoFit = value;
        }

        /// <summary>Texels kept clear around the fitted content.</summary>
        public float Padding
        {
            get => padding;
            set
            {
                padding = Mathf.Max(0, value);
                _fit = null;
                EnsureTexture();
            }
        }

        /// <summary>What shows where nothing is drawn (clear by default, so the inset panel shows).</summary>
        public Color Backdrop
        {
            get => backdrop;
            set => backdrop = value;
        }

        /// <summary>The viewport outline's colour (default: the theme's text at 60%).</summary>
        public Color ViewportColor
        {
            get => _viewportColor ?? MaUi.WithAlpha(MaTheme.Current.text, 0.6f);
            set => _viewportColor = value;
        }

        /// <summary>The bounds of everything added with <c>fit: true</c> since <see cref="Clear"/>, or null.</summary>
        public MapRect? ContentBounds => _contentBounds;

        /// <summary>How many rectangles and points are queued for the next <see cref="Apply"/>.</summary>
        public int ItemCount => _items.Count;

        /// <summary>Times the map has been drawn.</summary>
        public int Draws { get; private set; }

        /// <summary>Removes everything drawn (the next <see cref="Apply"/> starts from the backdrop).</summary>
        public void Clear()
        {
            _items.Clear();
            _contentBounds = null;
            _viewport = null;
        }

        /// <summary>Adds a world rectangle; <paramref name="fit"/> includes it in the auto-fit bounds.</summary>
        public MaMinimap AddRect(MapRect world, Color color, bool fit = true)
        {
            _items.Add(new Item { World = world, Color = color, Radius = -1 });
            if (fit) Include(world);
            return this;
        }

        /// <summary>
        /// Adds a world point drawn as a dot <paramref name="radiusPx"/> texels around it; <paramref name="fit"/>
        /// includes it in the auto-fit bounds (off by default, so a wandering cursor does not rescale the map).
        /// </summary>
        public MaMinimap AddPoint(float x, float y, Color color, int radiusPx = 1, bool fit = false)
        {
            _items.Add(new Item { World = new MapRect(x, y, 0, 0), Color = color, Radius = Mathf.Max(0, radiusPx) });
            if (fit) Include(new MapRect(x, y, 0, 0));
            return this;
        }

        /// <summary>Sets the world rectangle drawn as the camera's view (null for none).</summary>
        public MaMinimap SetViewport(MapRect? world)
        {
            _viewport = world;
            return this;
        }

        /// <summary>Pins the world bounds shown (turns <see cref="AutoFit"/> off).</summary>
        public MaMinimap FitTo(MapRect world)
        {
            autoFit = false;
            _fixedBounds = world;
            EnsureTexture();
            _fit.Fit(world);
            return this;
        }

        /// <summary>The world point at a normalised position on the map (0,0 bottom-left .. 1,1 top-right).</summary>
        public Vector2 WorldAt(float u, float v)
        {
            var (x, y) = Fit.ToWorld(u * resolution, v * resolution);
            return new Vector2(x, y);
        }

        /// <summary>Draws everything added since <see cref="Clear"/> into the texture, fitting first when <see cref="AutoFit"/> is on.</summary>
        public void Apply()
        {
            EnsureTexture();
            if (autoFit)
            {
                if (_contentBounds.HasValue) _fit.Fit(_contentBounds.Value);
            }
            else if (!_fixedBounds.IsEmpty) _fit.Fit(_fixedBounds);

            Color32 back = backdrop;
            for (int i = 0; i < _pixels.Length; i++) _pixels[i] = back;
            foreach (var item in _items)
            {
                if (item.Radius < 0) Fill(_fit.Feature(item.World), item.Color);
                else
                {
                    var (mx, my) = _fit.ToMap(item.World.X, item.World.Y);
                    int r = item.Radius;
                    Fill(new MapRect(Mathf.Floor(mx) - r, Mathf.Floor(my) - r, r * 2 + 1, r * 2 + 1), item.Color);
                }
            }
            if (_viewport.HasValue)
            {
                var v = _fit.Viewport(_viewport.Value);
                if (!v.IsEmpty)
                {
                    Color32 c = ViewportColor;
                    Fill(new MapRect(v.X, v.Y, v.W, 1), c);
                    Fill(new MapRect(v.X, v.YMax - 1, v.W, 1), c);
                    Fill(new MapRect(v.X, v.Y, 1, v.H), c);
                    Fill(new MapRect(v.XMax - 1, v.Y, 1, v.H), c);
                }
            }
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
            Draws++;
        }

        /// <summary>The colour of the texel at map (<paramref name="x"/>, <paramref name="y"/>) after the last <see cref="Apply"/>.</summary>
        public Color32 PixelAt(int x, int y)
        {
            EnsureTexture();
            x = Mathf.Clamp(x, 0, resolution - 1);
            y = Mathf.Clamp(y, 0, resolution - 1);
            return _pixels[y * resolution + x];
        }

        /// <inheritdoc />
        public void OnPointerDown(PointerEventData e) => Pan(e);

        /// <inheritdoc />
        public void OnDrag(PointerEventData e) => Pan(e);

        void Pan(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || image == null || Panned == null) return;
            var rt = image.rectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out var local)) return;
            var r = rt.rect;
            float u = Mathf.Clamp01((local.x - r.xMin) / r.width), v = Mathf.Clamp01((local.y - r.yMin) / r.height);
            Panned.Invoke(WorldAt(u, v));
        }

        /// <inheritdoc />
        public override void ApplyTheme(ThemeTokens theme)
        {
            // The panel restyles itself; the map's colours are the caller's.
        }

        /// <inheritdoc />
        public override MenuItemDefinition Describe() => new MenuItemDefinition(MenuItemKind.Custom, name, false);

        void OnDestroy()
        {
            if (_texture != null) MaUi.Destroy(_texture);
        }

        void Include(MapRect world) => _contentBounds = _contentBounds.HasValue ? _contentBounds.Value.Union(world) : world;

        void EnsureTexture()
        {
            if (_texture == null || _texture.width != resolution)
            {
                if (_texture != null) MaUi.Destroy(_texture);
                _texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "Minimap", hideFlags = HideFlags.DontSave,
                };
                _pixels = new Color32[resolution * resolution];
                if (image != null) image.texture = _texture;
            }
            if (_pixels == null || _pixels.Length != resolution * resolution) _pixels = new Color32[resolution * resolution];
            if (_fit == null)
            {
                _fit = new MinimapFit(resolution, resolution, padding);
                _fit.Fit(autoFit && _contentBounds.HasValue ? _contentBounds.Value : _fixedBounds.IsEmpty ? new MapRect(0, 0, resolution, resolution) : _fixedBounds);
            }
            if (image != null && image.texture != _texture) image.texture = _texture;
        }

        void Fill(MapRect r, Color32 color)
        {
            int size = resolution;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(r.X), 0, size), x1 = Mathf.Clamp(Mathf.CeilToInt(r.XMax), 0, size);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(r.Y), 0, size), y1 = Mathf.Clamp(Mathf.CeilToInt(r.YMax), 0, size);
            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++) _pixels[y * size + x] = Blend(_pixels[y * size + x], color);
            }
        }

        static Color32 Blend(Color32 under, Color32 over)
        {
            float a = over.a / 255f;
            if (a >= 1f) return over;
            float ua = under.a / 255f, outA = a + ua * (1 - a);
            if (outA <= 0f) return new Color32(0, 0, 0, 0);
            byte Mix(byte o, byte u) => (byte)Mathf.Clamp(Mathf.RoundToInt((o * a + u * ua * (1 - a)) / outA), 0, 255);
            return new Color32(Mix(over.r, under.r), Mix(over.g, under.g), Mix(over.b, under.b), (byte)Mathf.RoundToInt(outA * 255));
        }
    }
}
