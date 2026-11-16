// UNTESTED in Unity. Tiny uGUI helpers (built-in font only, no UI assets needed).
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NextPlay.App
{
    public static class UiKit
    {
        public static Font Font { get { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } }

        public static Canvas MakeCanvas(string name, int sortOrder)
        {
            GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            Canvas c = go.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = sortOrder;
            CanvasScaler cs = go.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(1920, 1080); cs.matchWidthOrHeight = 0.5f;
            return c;
        }

        // Child rect that follows Screen.safeArea (notches / rounded corners).
        public static RectTransform SafeRoot(Canvas canvas)
        {
            GameObject go = new GameObject("SafeArea", typeof(RectTransform)); go.transform.SetParent(canvas.transform, false);
            RectTransform rt = (RectTransform)go.transform;
            Rect sa = Screen.safeArea; float w = Mathf.Max(1, Screen.width), h = Mathf.Max(1, Screen.height);
            rt.anchorMin = new Vector2(sa.xMin / w, sa.yMin / h); rt.anchorMax = new Vector2(sa.xMax / w, sa.yMax / h); rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static RectTransform MakeRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform; rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        public static Image MakeImage(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            RectTransform rt = MakeRect(parent, name, anchorMin, anchorMax, pivot, pos, size);
            Image im = rt.gameObject.AddComponent<Image>(); im.color = color; im.raycastTarget = false; return im;
        }

        public static Text MakeText(Transform parent, string name, string text, int size, Color color, TextAnchor anchor, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 boxSize, bool outline = true)
        {
            RectTransform rt = MakeRect(parent, name, anchorMin, anchorMax, pivot, pos, boxSize);
            Text t = rt.gameObject.AddComponent<Text>();
            t.font = Font; t.text = text; t.fontSize = size; t.color = color; t.alignment = anchor; t.fontStyle = FontStyle.BoldAndItalic;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
            if (outline) { Outline o = rt.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0.02f, 0.05f, 0.2f, 0.95f); o.effectDistance = new Vector2(3, -3); }
            return t;
        }

        public static Button MakeButton(Transform parent, string name, string label, int fontSize, Color bg, Color fg, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size, System.Action onClick)
        {
            RectTransform rt = MakeRect(parent, name, anchorMin, anchorMax, pivot, pos, size);
            Image im = rt.gameObject.AddComponent<Image>(); im.color = bg;
            Button b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = im; if (onClick != null) b.onClick.AddListener(() => onClick());
            Text t = MakeText(rt, "Label", label, fontSize, fg, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, false);
            t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
            return b;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        public static Color Hex(string html, Color fallback) { Color c; return ColorUtility.TryParseHtmlString(html, out c) ? c : fallback; }
    }
}
