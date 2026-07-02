using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Athanor.Domain;
using Athanor.UI;

namespace Athanor.EditorTools
{
    /// Hornea todos los assets del juego como PNGs con acabado (volumen, brillo, contorno).
    /// Los archivos van a Assets/Resources/Art/** y el juego los carga con prioridad
    /// sobre las formas procedurales. Reemplazar un asset = sobrescribir su PNG.
    ///   Unity.exe -batchmode -executeMethod Athanor.EditorTools.AssetBaker.Run
    public static class AssetBaker
    {
        const string ArtRoot = "Assets/Resources/Art";

        public static void Run()
        {
            try
            {
                foreach (var def in ElementCatalog.Elements)
                {
                    var color = Hex(def.ColorHex);
                    BakeIcon(ProceduralIcons.SdfOf(def.Id), color, 256,
                             $"{ArtRoot}/Elements/el_{def.Id.ToString().ToLowerInvariant()}.png");
                }

                BakeIcon(ProceduralIcons.StarSdf(), Hex("#E8C547"), 256,
                         $"{ArtRoot}/Achievements/medalla.png");

                BakeFlaskGlass($"{ArtRoot}/Core/matraz_vidrio.png");
                BakeFlaskLiquid($"{ArtRoot}/Core/matraz_liquido.png");
                BakeAppIcon("Assets/Icon/app_icon.png");

                AssetDatabase.Refresh();
                ConfigureImporters();
                Debug.Log("[AssetBaker] LISTO: assets horneados en " + ArtRoot);
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[AssetBaker] Excepción: " + e);
                EditorApplication.Exit(1);
            }
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        /// Texturas de 1024px o más de lado (ilustraciones) — el resto son iconos.
        static bool IsLarge(string assetPath)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            return tex != null && Mathf.Max(tex.width, tex.height) >= 1024;
        }

        // ---- Estilo común: degradado vertical + brillo superior + contorno oscuro ----

        static Color32 Shade(float d, float aa, Color baseCol, Vector2 p)
        {
            float alpha = Mathf.Clamp01(0.5f - d * aa);
            if (alpha <= 0f) return new Color32(0, 0, 0, 0);

            float grad = Mathf.InverseLerp(-1f, 1f, p.y);
            Color body = Color.Lerp(baseCol * 0.74f, Color.Lerp(baseCol, Color.white, 0.22f), grad);

            float hl = Mathf.Clamp01(1f - (p - new Vector2(-0.32f, 0.40f)).magnitude / 0.85f);
            body = Color.Lerp(body, Color.white, hl * 0.16f);

            float edge = Mathf.Clamp01((d + 0.085f) / 0.085f);
            body = Color.Lerp(body, baseCol * 0.42f, edge * edge * 0.85f);

            body.a = alpha;
            return body;
        }

        static void BakeIcon(Func<Vector2, float> sdf, Color baseCol, int size, string path)
        {
            var px = new Color32[size * size];
            float aa = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                    px[y * size + x] = Shade(sdf(p), aa, baseCol, p);
                }
            WritePng(px, size, size, path);
        }

        // ---- Matraz ----

        static void BakeFlaskGlass(string path)
        {
            const int size = 512;
            var glass = Hex("#F6BE68");
            var px = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                float hw = UiTheme.FlaskHalfWidth(y);
                for (int x = 0; x < size; x++)
                {
                    if (hw <= 0) { px[y * size + x] = new Color32(0, 0, 0, 0); continue; }
                    float dx = Mathf.Abs(x - 256f);
                    float a = Mathf.Clamp01(hw - dx + 1f);
                    if (a <= 0) { px[y * size + x] = new Color32(0, 0, 0, 0); continue; }

                    float grad = Mathf.InverseLerp(84f, 492f, y);
                    Color body = Color.Lerp(glass * 0.85f, Color.Lerp(glass, Color.white, 0.25f), grad);
                    float baseAlpha = 0.50f;

                    // contorno del vidrio más presente
                    float edge = Mathf.Clamp01((dx - (hw - 7f)) / 7f);
                    body = Color.Lerp(body, glass * 0.55f, edge * 0.9f);
                    baseAlpha = Mathf.Lerp(baseAlpha, 0.9f, edge);

                    // marcas de medición
                    bool markBand = (y > 150 && y < 158) || (y > 200 && y < 208) || (y > 250 && y < 258);
                    float innerX = 256f - hw;
                    if (markBand && x - innerX > 12 && x - innerX < 52)
                        baseAlpha = Mathf.Min(1f, baseAlpha + 0.4f);

                    body.a = baseAlpha * a;
                    px[y * size + x] = body;
                }
            }
            WritePng(px, size, size, path);
        }

        static void BakeFlaskLiquid(string path)
        {
            const int size = 512;
            const int liquidTop = 240;
            var dark = Hex("#6E46A8");
            var light = Hex("#A97FE0");
            var px = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                float hw = UiTheme.FlaskHalfWidth(y);
                for (int x = 0; x < size; x++)
                {
                    float a = 0f;
                    if (hw > 0)
                    {
                        float dx = Mathf.Abs(x - 256f);
                        a = Mathf.Clamp01(hw - dx + 1f);
                        a *= Mathf.Clamp01(liquidTop - y + 1f);
                        a *= Mathf.Clamp01(hw - dx - 9f + 1f);
                    }
                    if (a <= 0f) { px[y * size + x] = new Color32(0, 0, 0, 0); continue; }

                    float grad = Mathf.InverseLerp(84f, liquidTop, y);
                    Color body = Color.Lerp(dark, light, grad);
                    if (y > liquidTop - 10) body = Color.Lerp(body, Color.white, 0.25f); // superficie
                    body.a = a;
                    px[y * size + x] = body;
                }
            }
            WritePng(px, size, size, path);
        }

        // ---- Ícono de la app (matraz sobre fondo redondeado oscuro con anillo) ----

        static void BakeAppIcon(string path)
        {
            const int size = 512;
            var bg = Hex("#14121F");
            var ring = Hex("#F2A541");
            var glass = Hex("#F6BE68");
            var liquid = Hex("#9B72CF");
            var px = new Color32[size * size];

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);

                    // fondo redondeado
                    var q = new Vector2(Mathf.Abs(p.x) - 0.82f, Mathf.Abs(p.y) - 0.82f);
                    float rd = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude
                               + Mathf.Min(Mathf.Max(q.x, q.y), 0) - 0.16f;
                    float bgA = Mathf.Clamp01(0.5f - rd * (size / 2f));
                    Color col = bg; col.a = bgA;

                    // anillo tenue
                    float ringD = Mathf.Abs(p.magnitude - 0.72f) - 0.02f;
                    float ringA = Mathf.Clamp01(0.5f - ringD * (size / 2f)) * 0.35f;
                    col = Blend(col, ring, ringA * bgA);

                    // matraz centrado (mapea el lienzo del matraz 512 a escala 0.62)
                    float fx = 256f + p.x / 0.62f * 256f;
                    float fy = 256f + (p.y + 0.06f) / 0.62f * 256f;
                    if (fy >= 0 && fy < 512)
                    {
                        float hw = UiTheme.FlaskHalfWidth((int)fy);
                        if (hw > 0)
                        {
                            float dxp = Mathf.Abs(fx - 256f);
                            float fa = Mathf.Clamp01(hw - dxp + 1f);
                            if (fa > 0)
                            {
                                bool isLiquid = fy < 240 && dxp < hw - 9f;
                                var part = isLiquid ? liquid : glass;
                                float alpha = isLiquid ? 1f : 0.8f;
                                col = Blend(col, part, fa * alpha * bgA);
                            }
                        }
                    }
                    px[y * size + x] = col;
                }
            WritePng(px, size, size, path);
        }

        static Color Blend(Color baseCol, Color top, float a)
        {
            var outc = Color.Lerp(baseCol, top, Mathf.Clamp01(a));
            outc.a = Mathf.Max(baseCol.a, a);
            return outc;
        }

        // ---- IO ----

        static void WritePng(Color32[] px, int w, int h, string path)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        /// Deja todo Art/** como Sprite sin compresión (llamado también antes de cada build,
        /// para que cualquier PNG nuevo soltado a mano quede bien importado).
        public static void ConfigureImporters()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                var imp = (TextureImporter)AssetImporter.GetAtPath(p);
                if (imp == null) continue;
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                // Iconos chicos: sin compresión (nitidez). Ilustraciones grandes: comprimidas
                // en alta calidad para no inflar el APK (~8 MB una sola a 1080x1920 sin comprimir).
                imp.textureCompression = imp.maxTextureSize >= 1024 && IsLarge(p)
                    ? TextureImporterCompression.CompressedHQ
                    : TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }
        }
    }
}
