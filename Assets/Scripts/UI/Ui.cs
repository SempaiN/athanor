using UnityEngine;
using UnityEngine.UI;

namespace Athanor.UI
{
    /// Fábrica de controles uGUI creados por código.
    public static class Ui
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static Image Panel(string name, Transform parent, Color color, bool rounded = true)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (rounded)
            {
                img.sprite = UiTheme.RoundedRect();
                img.type = Image.Type.Sliced;
            }
            return img;
        }

        public static Text Label(string name, Transform parent, string text, int size,
                                 Color color, TextAnchor anchor = TextAnchor.MiddleCenter,
                                 FontStyle style = FontStyle.Normal)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = UiTheme.DefaultFont;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button TextButton(string name, Transform parent, Color bg,
                                        out Text label)
        {
            var img = Panel(name, parent, bg);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            img.gameObject.AddComponent<ButtonJuice>();

            var colors = btn.colors;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.6f);
            btn.colors = colors;

            label = Label("Label", img.transform, "", 40, UiTheme.Background,
                          TextAnchor.MiddleCenter, FontStyle.Bold);
            Fill(label.rectTransform);
            return btn;
        }

        /// Ancla el rect al rect completo del padre.
        public static void Fill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// Posiciona con anclas proporcionales al lienzo de referencia 1080×1920.
        public static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }

        /// Ancla a un borde: anchor (0..1) en X e Y, tamaño fijo.
        public static void Anchor(RectTransform rt, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;
        }

        /// Lista vertical con scroll táctil. Devuelve el content donde agregar filas
        /// (fijar su sizeDelta.y al alto total del contenido).
        public static RectTransform ScrollList(string name, Transform parent, out ScrollRect scroll)
        {
            var viewport = Rect(name, parent);
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll = viewport.gameObject.AddComponent<ScrollRect>();

            var content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;

            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;
            scroll.inertia = true;
            return content;
        }

        /// Fila dentro de un ScrollList: ocupa el ancho, alto fijo, apilada desde arriba.
        public static void Row(RectTransform rt, int index, float rowHeight, float margin = 10)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -(index * rowHeight) - margin);
            rt.offsetMin = new Vector2(margin, rt.offsetMin.y);
            rt.offsetMax = new Vector2(-margin, rt.offsetMax.y);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, rowHeight - margin);
        }
    }
}
