using System.Collections.Generic;
using UnityEngine;

namespace Athanor.UI
{
    /// Paleta (GDD §1) y sprites planos generados por código (placeholders reemplazables).
    public static class UiTheme
    {
        public static readonly Color Background   = Hex("#14121F");
        public static readonly Color Panel        = Hex("#221E33");
        public static readonly Color Card         = Hex("#2E2947");
        public static readonly Color Amber        = Hex("#F2A541");
        public static readonly Color Gold         = Hex("#E8C547");
        public static readonly Color Green        = Hex("#7FB069");
        public static readonly Color Violet       = Hex("#9B72CF");
        public static readonly Color TextMain     = Hex("#F4EFE6");
        public static readonly Color TextDim      = Hex("#A79FBC");

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        public static Font DefaultFont =>
            Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ---- Sprites procedurales (cacheados) ----

        static Sprite roundedRect;
        static Sprite circle;

        /// Rect redondeado 64px, borde 9-slice de 24px: sirve para cualquier panel/botón.
        public static Sprite RoundedRect()
        {
            if (roundedRect != null) return roundedRect;
            const int size = 64, radius = 20;
            var tex = NewTex(size, size);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, new Color(1, 1, 1, RoundedAlpha(x, y, size, size, radius)));
            tex.Apply();
            roundedRect = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect,
                new Vector4(24, 24, 24, 24));
            return roundedRect;
        }

        public static Sprite Circle()
        {
            if (circle != null) return circle;
            const int size = 256;
            var tex = NewTex(size, size);
            float r = size / 2f - 1, cx = size / 2f - 0.5f, cy = size / 2f - 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - d)));
                }
            tex.Apply();
            circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return circle;
        }

        static Texture2D NewTex(int w, int h) =>
            new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };

        // Alfa antialiased para esquinas redondeadas.
        static float RoundedAlpha(int x, int y, int w, int h, int radius)
        {
            float px = Mathf.Min(x, w - 1 - x);
            float py = Mathf.Min(y, h - 1 - y);
            if (px >= radius || py >= radius) return 1;
            float dx = radius - px, dy = radius - py;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Clamp01(radius - d + 1);
        }

        // Colores por elemento (del catálogo) cacheados.
        static readonly Dictionary<string, Color> elementColors = new Dictionary<string, Color>();

        public static Color ElementColor(string hex)
        {
            if (!elementColors.TryGetValue(hex, out var c))
            {
                c = Hex(hex);
                elementColors[hex] = c;
            }
            return c;
        }
    }
}
