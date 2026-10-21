using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>
    /// Canvases set up the MindAttic way: screen-space overlay, scaled from a 1920x1080 reference (matching
    /// width and height equally) so every size in the package means the same on any screen, with a raycaster
    /// and an EventSystem. <see cref="Overlay"/> is the shared top layer modals, toasts and tooltips live on.
    /// </summary>
    public static class MaCanvas
    {
        /// <summary>The reference resolution every MindAttic canvas is laid out for.</summary>
        public static readonly Vector2 ReferenceResolution = new Vector2(1920, 1080);

        /// <summary>Sorting order of <see cref="Overlay"/> (below TextMesh Pro dropdown lists at 30000).</summary>
        public const int OverlaySortingOrder = 29000;

        static Canvas s_overlay;
        static RectTransform s_modalLayer, s_toastLayer, s_tooltipLayer;

        /// <summary>Creates a configured overlay canvas (and an EventSystem if the scene has none).</summary>
        public static Canvas Create(string name = "MindAttic UI", int sortingOrder = 0, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = MaUi.UiLayer;
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            Configure(canvas);
            MaEventSystem.Ensure();
            return canvas;
        }

        /// <summary>Adds (or sets up) a CanvasScaler at the 1920x1080 reference and a GraphicRaycaster on a root canvas.</summary>
        public static void Configure(Canvas canvas)
        {
            if (canvas == null) return;
            canvas.pixelPerfect = false;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
            if (canvas.isRootCanvas || canvas.transform.parent == null)
            {
                var scaler = MaUi.GetOrAdd<CanvasScaler>(canvas.gameObject);
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = ReferenceResolution;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                scaler.referencePixelsPerUnit = 100;
            }
            if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        /// <summary>The shared top canvas (created on first use; kept across scene loads in Play Mode).</summary>
        public static Canvas Overlay
        {
            get
            {
                if (s_overlay == null)
                {
                    s_overlay = Create("MindAttic Overlay", OverlaySortingOrder);
                    if (Application.isPlaying) Object.DontDestroyOnLoad(s_overlay.gameObject);
                    else s_overlay.gameObject.hideFlags = HideFlags.DontSave;
                    s_modalLayer = Layer("Modals");
                    s_toastLayer = Layer("Toasts");
                    s_tooltipLayer = Layer("Tooltips");
                }
                return s_overlay;
            }
        }

        /// <summary>The full-screen layer modals open on.</summary>
        public static RectTransform ModalLayer { get { _ = Overlay; return s_modalLayer; } }

        /// <summary>The full-screen layer toasts stack on (above modals).</summary>
        public static RectTransform ToastLayer { get { _ = Overlay; return s_toastLayer; } }

        /// <summary>The full-screen layer tooltips float on (above everything).</summary>
        public static RectTransform TooltipLayer { get { _ = Overlay; return s_tooltipLayer; } }

        /// <summary>The root canvas a component under <paramref name="t"/> draws on, or null.</summary>
        public static Canvas RootOf(Transform t)
        {
            var c = t != null ? t.GetComponentInParent<Canvas>(true) : null;
            return c != null ? c.rootCanvas : null;
        }

        /// <summary>Destroys the shared overlay (tests, or a game tearing its UI down).</summary>
        public static void DestroyOverlay()
        {
            if (s_overlay != null) MaUi.Destroy(s_overlay.gameObject);
            s_overlay = null;
        }

        static RectTransform Layer(string name)
        {
            var rt = MaUi.Rect(name, s_overlay.transform);
            MaUi.Stretch(rt);
            return rt;
        }
    }
}
