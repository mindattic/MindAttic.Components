using MindAttic.Components.Unity.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MindAttic.Components.Unity
{
    /// <summary>The text roles every label in the package uses; each picks a size and a colour token.</summary>
    public enum TextStyle
    {
        /// <summary>Menu screen title: largest, bold, <c>text</c>.</summary>
        Title,
        /// <summary>Dialog title or heading: bold, <c>text</c>.</summary>
        Heading,
        /// <summary>Body copy: <c>text</c>.</summary>
        Body,
        /// <summary>Labels next to controls: <c>text2</c>.</summary>
        Label,
        /// <summary>Secondary, de-emphasised copy: <c>text3</c>.</summary>
        Muted,
        /// <summary>Small captions: <c>text3</c>, caption size.</summary>
        Caption,
        /// <summary>A section heading: accent, caption size, upper case, letter-spaced (the web's form-section title).</summary>
        Section,
        /// <summary>The text on a control: control size, <c>text</c>.</summary>
        Control,
        /// <summary>A read-only code: large, bold, letter-spaced, accent.</summary>
        Code,
    }

    /// <summary>Internal building blocks shared by every component's <c>Create</c>.</summary>
    internal static class MaUi
    {
        public const int UiLayer = 5;

        public static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = UiLayer;
            var rt = (RectTransform)go.transform;
            if (parent != null) rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static void Outset(RectTransform rt, float by) => Stretch(rt, -by, -by, -by, -by);

        public static Image Image(string name, Transform parent, bool raycast = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = raycast;
            return img;
        }

        public static TextMeshProUGUI Text(Transform parent, string text, TextStyle style, string name = "Label")
        {
            var rt = Rect(name, parent);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.raycastTarget = false;
            tmp.text = text ?? "";
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            Style(tmp, style, MaTheme.Current);
            return tmp;
        }

        public static void Style(TMP_Text tmp, TextStyle style, ThemeTokens theme)
        {
            if (tmp == null) return;
            var m = theme.Metrics;
            var font = theme.font != null ? theme.font : MaTheme.Font;
            if (font != null && tmp.font != font) tmp.font = font;
            tmp.fontStyle = FontStyles.Normal;
            tmp.characterSpacing = 0;
            switch (style)
            {
                case TextStyle.Title:
                    tmp.fontSize = m.FontTitle; tmp.color = theme.text; tmp.fontStyle = FontStyles.Bold; break;
                case TextStyle.Heading:
                    tmp.fontSize = m.FontHeading; tmp.color = theme.text; tmp.fontStyle = FontStyles.Bold; break;
                case TextStyle.Body:
                    tmp.fontSize = m.FontBody; tmp.color = theme.text; break;
                case TextStyle.Label:
                    tmp.fontSize = m.FontBody; tmp.color = theme.text2; break;
                case TextStyle.Muted:
                    tmp.fontSize = m.FontBody; tmp.color = theme.text3; break;
                case TextStyle.Caption:
                    tmp.fontSize = m.FontCaption; tmp.color = theme.text3; break;
                case TextStyle.Section:
                    tmp.fontSize = m.FontCaption; tmp.color = theme.accent; tmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase; tmp.characterSpacing = 12; break;
                case TextStyle.Control:
                    tmp.fontSize = m.FontControl; tmp.color = theme.text; break;
                case TextStyle.Code:
                    tmp.fontSize = m.FontCode; tmp.color = theme.accent; tmp.fontStyle = FontStyles.Bold; tmp.characterSpacing = 18; break;
            }
        }

        public static LayoutElement Layout(GameObject go, float minW = -1, float minH = -1, float prefW = -1, float prefH = -1, float flexW = -1, float flexH = -1)
        {
            var le = GetOrAdd<LayoutElement>(go);
            le.minWidth = minW;
            le.minHeight = minH;
            le.preferredWidth = prefW;
            le.preferredHeight = prefH;
            le.flexibleWidth = flexW;
            le.flexibleHeight = flexH;
            return le;
        }

        public static HorizontalLayoutGroup HRow(GameObject go, float spacing, RectOffset padding = null, TextAnchor align = TextAnchor.MiddleLeft, bool expandWidth = false)
        {
            var h = GetOrAdd<HorizontalLayoutGroup>(go);
            h.spacing = spacing;
            h.padding = padding ?? new RectOffset();
            h.childAlignment = align;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = expandWidth;
            h.childForceExpandHeight = false;
            return h;
        }

        public static VerticalLayoutGroup VColumn(GameObject go, float spacing, RectOffset padding = null, TextAnchor align = TextAnchor.UpperCenter)
        {
            var v = GetOrAdd<VerticalLayoutGroup>(go);
            v.spacing = spacing;
            v.padding = padding ?? new RectOffset();
            v.childAlignment = align;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        public static RectOffset Pad(float all) => Pad(all, all);

        public static RectOffset Pad(float horizontal, float vertical) =>
            new RectOffset(Mathf.RoundToInt(horizontal), Mathf.RoundToInt(horizontal), Mathf.RoundToInt(vertical), Mathf.RoundToInt(vertical));

        /// <summary>The transform to build under: a Canvas, a component's transform, or the shared overlay's modal layer when null.</summary>
        public static Transform ParentOf(Component parent) => parent != null ? parent.transform : null;

        public static void Destroy(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }

        /// <summary>Sets a colour only when it changed (avoids dirtying the canvas every frame).</summary>
        public static void SetColor(Graphic g, Color c)
        {
            if (g != null && g.color != c) g.color = c;
        }

        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        public static Color ToColor(ThemeColor c) => c.ToColor();
    }
}
