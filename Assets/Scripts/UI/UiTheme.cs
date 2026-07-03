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

        /// Variante translúcida de un color (chips tonales estilo Material You).
        public static Color Tint(Color c, float alpha) => new Color(c.r, c.g, c.b, alpha);

        public static Font DefaultFont =>
            Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ---- Sprites procedurales (cacheados) ----

        static Sprite roundedRect;
        static Sprite circle;
        static Sprite verticalGradient;

        /// Degradado vertical sutil (más claro arriba) para el fondo del laboratorio.
        public static Sprite VerticalGradient()
        {
            if (verticalGradient != null) return verticalGradient;
            const int h = 128;
            var top = new Color(0.115f, 0.10f, 0.20f);    // violeta profundo más claro
            var bottom = new Color(0.055f, 0.048f, 0.10f); // casi negro
            var tex = NewTex(1, h);
            for (int y = 0; y < h; y++)
                tex.SetPixel(0, y, Color.Lerp(bottom, top, y / (float)(h - 1)));
            tex.Apply();
            verticalGradient = Sprite.Create(tex, new Rect(0, 0, 1, h), new Vector2(0.5f, 0.5f));
            return verticalGradient;
        }

        /// Rect redondeado 64px, borde 9-slice de 24px: sirve para cualquier panel/botón.
        public static Sprite RoundedRect()
        {
            if (roundedRect != null) return roundedRect;
            const int size = 64, radius = 26; // esquinas más suaves (rediseño v2.0)
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

        // ---- Matraz Erlenmeyer procedural ----

        static Sprite flaskGlass;
        static Sprite flaskLiquid;

        /// Medio ancho de la silueta del matraz en cada altura (lienzo 512, centro x=256).
        /// Devuelve <= 0 fuera de la silueta.
        public static float FlaskHalfWidth(int y)
        {
            if (y < 84) return 0;
            if (y < 122)  // base con esquinas suaves
            {
                float k = (y - 84) / 38f;
                return 186f + 10f * Mathf.Sin(k * Mathf.PI * 0.5f) - 10f * (1f - k) * (1f - k) * 4f;
            }
            if (y < 352)  // cuerpo cónico
                return Mathf.Lerp(192f, 46f, (y - 122) / 230f);
            if (y < 468)  // cuello
                return 46f;
            if (y < 492)  // labio
                return 60f;
            return 0;
        }

        static Texture2D FlaskTex(bool liquidOnly)
        {
            const int size = 512;
            const int liquidTop = 240;
            var tex = NewTex(size, size);
            var px = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                float hw = FlaskHalfWidth(y);
                for (int x = 0; x < size; x++)
                {
                    float a = 0f;
                    if (hw > 0)
                    {
                        float dx = Mathf.Abs(x - 256f);
                        a = Mathf.Clamp01(hw - dx + 1f);
                        if (liquidOnly)
                        {
                            // el líquido llena el cuerpo hasta liquidTop, con borde suave
                            float cut = Mathf.Clamp01(liquidTop - y + 1f);
                            a *= cut;
                            // margen interior para que se vea el vidrio alrededor
                            a *= Mathf.Clamp01(hw - dx - 9f + 1f);
                        }
                        else
                        {
                            // marcas de medición en la pared izquierda del cuerpo
                            bool markBand = (y > 150 && y < 158) || (y > 200 && y < 208) || (y > 250 && y < 258);
                            if (markBand)
                            {
                                float innerX = 256f - hw;
                                if (x - innerX > 12 && x - innerX < 52)
                                    a = Mathf.Min(1f, a + 0.55f);
                            }
                        }
                    }
                    byte alpha = (byte)(a * 255);
                    px[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        /// Silueta completa del matraz (vidrio). Tintable con Image.color.
        public static Sprite FlaskGlass()
        {
            if (flaskGlass != null) return flaskGlass;
            flaskGlass = Sprite.Create(FlaskTex(false), new Rect(0, 0, 512, 512), new Vector2(0.5f, 0.5f));
            return flaskGlass;
        }

        /// Solo el líquido interior (hasta ~mitad del cuerpo). Tintable.
        public static Sprite FlaskLiquid()
        {
            if (flaskLiquid != null) return flaskLiquid;
            flaskLiquid = Sprite.Create(FlaskTex(true), new Rect(0, 0, 512, 512), new Vector2(0.5f, 0.5f));
            return flaskLiquid;
        }

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
