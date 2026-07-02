using System;
using System.Collections.Generic;
using UnityEngine;
using Athanor.Domain;

namespace Athanor.UI
{
    /// Iconos vectoriales por elemento, rasterizados con SDFs antialiasados.
    /// Blancos y tintables con Image.color. Reemplazables por PNGs (docs/ASSETS.md).
    public static class ProceduralIcons
    {
        const int Size = 128;
        static readonly Dictionary<ElementId, Sprite> cache = new Dictionary<ElementId, Sprite>();
        static Sprite star, diamond;

        public static Sprite For(ElementId id)
        {
            if (cache.TryGetValue(id, out var s)) return s;
            s = Render(id.ToString(), SdfFor(id));
            cache[id] = s;
            return s;
        }

        /// Estrella de 5 puntas (medallas de logros).
        public static Sprite Star()
        {
            if (star != null) return star;
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float ang = Mathf.PI / 2 + i * Mathf.PI / 5;
                float r = i % 2 == 0 ? 0.82f : 0.36f;
                pts[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
            }
            star = Render("star", p => Poly(p, pts));
            return star;
        }

        /// Rombo (icono genérico arcano).
        public static Sprite Diamond()
        {
            if (diamond != null) return diamond;
            diamond = Render("diamond", p => Poly(p, new[]
            {
                new Vector2(0, 0.8f), new Vector2(-0.6f, 0), new Vector2(0, -0.8f), new Vector2(0.6f, 0),
            }));
            return diamond;
        }

        // ---- Formas por elemento ----

        static Func<Vector2, float> SdfFor(ElementId id) => id switch
        {
            // Básicos
            ElementId.Tierra => p => Min(
                Poly(p, new[] { new Vector2(-0.9f, -0.55f), new Vector2(-0.15f, 0.62f), new Vector2(0.5f, -0.55f) }),
                Poly(p, new[] { new Vector2(0.05f, -0.55f), new Vector2(0.5f, 0.28f), new Vector2(0.9f, -0.55f) })),
            ElementId.Agua => Drop(0.5f, 0.85f),
            ElementId.Fuego => p => Min(
                Circle(p, new Vector2(0, -0.32f), 0.44f),
                Poly(p, new[] { new Vector2(0, 0.9f), new Vector2(-0.46f, -0.1f), new Vector2(0.46f, -0.1f) })),
            ElementId.Aire => p => Min(
                Capsule(p, new Vector2(-0.7f, 0.4f), new Vector2(0.5f, 0.4f), 0.13f),
                Min(Capsule(p, new Vector2(-0.5f, 0f), new Vector2(0.7f, 0f), 0.13f),
                    Capsule(p, new Vector2(-0.7f, -0.4f), new Vector2(0.3f, -0.4f), 0.13f))),
            // Compuestos
            ElementId.Barro => p => Min(
                Min(Circle(p, new Vector2(-0.3f, -0.15f), 0.42f), Circle(p, new Vector2(0.28f, -0.22f), 0.38f)),
                Min(Circle(p, new Vector2(0, 0.15f), 0.4f),
                    Capsule(p, new Vector2(-0.45f, -0.42f), new Vector2(0.45f, -0.42f), 0.2f))),
            ElementId.Lava => p => Min(
                Circle(p, new Vector2(0, 0.2f), 0.55f),
                Min(Capsule(p, new Vector2(-0.32f, -0.1f), new Vector2(-0.32f, -0.58f), 0.16f),
                    Capsule(p, new Vector2(0.26f, -0.1f), new Vector2(0.26f, -0.42f), 0.16f))),
            ElementId.Polvo => p => Min(
                Min(Circle(p, new Vector2(-0.45f, 0.3f), 0.2f), Circle(p, new Vector2(0.15f, 0.45f), 0.16f)),
                Min(Min(Circle(p, new Vector2(0.5f, 0.1f), 0.18f), Circle(p, new Vector2(-0.1f, -0.05f), 0.24f)),
                    Min(Circle(p, new Vector2(-0.5f, -0.4f), 0.17f), Circle(p, new Vector2(0.25f, -0.45f), 0.21f)))),
            ElementId.Vapor => p => Min(
                Min(Circle(p, new Vector2(-0.36f, -0.02f), 0.34f), Circle(p, new Vector2(0.02f, 0.2f), 0.42f)),
                Min(Circle(p, new Vector2(0.38f, -0.02f), 0.34f),
                    Capsule(p, new Vector2(-0.36f, -0.2f), new Vector2(0.38f, -0.2f), 0.3f))),
            ElementId.Niebla => p => Min(
                Capsule(p, new Vector2(-0.75f, 0.35f), new Vector2(0.75f, 0.35f), 0.16f),
                Min(Capsule(p, new Vector2(-0.75f, -0.05f), new Vector2(0.75f, -0.05f), 0.16f),
                    Capsule(p, new Vector2(-0.75f, -0.45f), new Vector2(0.75f, -0.45f), 0.16f))),
            ElementId.Energia => p => Poly(p, new[]
            {
                new Vector2(0.28f, 0.9f), new Vector2(-0.45f, 0.02f), new Vector2(-0.04f, 0.02f),
                new Vector2(-0.28f, -0.9f), new Vector2(0.45f, -0.02f), new Vector2(0.04f, -0.02f),
            }),
            // Materiales
            ElementId.Piedra => p => Poly(p, new[]
            {
                new Vector2(-0.6f, -0.55f), new Vector2(-0.75f, 0.1f), new Vector2(-0.2f, 0.65f),
                new Vector2(0.55f, 0.45f), new Vector2(0.7f, -0.35f), new Vector2(0.15f, -0.6f),
            }),
            ElementId.Metal => p => Min(
                Poly(p, new[] { new Vector2(-0.75f, -0.5f), new Vector2(-0.55f, 0.05f), new Vector2(0.55f, 0.05f), new Vector2(0.75f, -0.5f) }),
                Poly(p, new[] { new Vector2(-0.45f, 0.12f), new Vector2(-0.3f, 0.55f), new Vector2(0.3f, 0.55f), new Vector2(0.45f, 0.12f) })),
            ElementId.Cristal => p => Poly(p, new[]
            {
                new Vector2(0, 0.8f), new Vector2(-0.6f, 0.15f), new Vector2(-0.35f, -0.7f),
                new Vector2(0.35f, -0.7f), new Vector2(0.6f, 0.15f),
            }),
            ElementId.Vida => p => Min(
                Mathf.Max(Circle(p, new Vector2(-0.22f, -0.22f), 0.72f), Circle(p, new Vector2(0.22f, 0.22f), 0.72f)),
                Capsule(p, new Vector2(0.3f, 0.3f), new Vector2(0.6f, 0.6f), 0.06f)),
            // Tria Prima (símbolos alquímicos estilizados)
            ElementId.Sal => p => Mathf.Max(
                RoundRect(p, 0.55f, 0.55f, 0.1f),
                -Capsule(p, new Vector2(-0.55f, 0f), new Vector2(0.55f, 0f), 0.055f)),
            ElementId.Mercurio => p => Min(
                Circle(p, new Vector2(0.05f, -0.12f), 0.5f),
                Circle(p, new Vector2(-0.42f, 0.38f), 0.2f)),
            ElementId.Azufre => p => Min(
                Poly(p, new[] { new Vector2(0, 0.78f), new Vector2(-0.6f, -0.08f), new Vector2(0.6f, -0.08f) }),
                Min(Capsule(p, new Vector2(0, -0.08f), new Vector2(0, -0.68f), 0.09f),
                    Capsule(p, new Vector2(-0.3f, -0.42f), new Vector2(0.3f, -0.42f), 0.09f))),
            // Nobles
            ElementId.Oro => p => Min(
                Circle(p, Vector2.zero, 0.3f),
                Mathf.Abs(Circle(p, Vector2.zero, 0.58f)) - 0.1f),
            ElementId.Eter => p => Min(
                Circle(p, Vector2.zero, 0.42f),
                Mathf.Abs(Ellipse(p, 0.78f, 0.32f)) - 0.075f),
            // Culminación
            ElementId.PiedraFilosofal => p =>
            {
                var hex = new Vector2[6];
                for (int i = 0; i < 6; i++)
                {
                    float ang = Mathf.PI / 2 + i * Mathf.PI / 3;
                    hex[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 0.72f;
                }
                return Poly(p, hex);
            },
            _ => p => Circle(p, Vector2.zero, 0.6f),
        };

        static Func<Vector2, float> Drop(float r, float tipY) => p => Min(
            Circle(p, new Vector2(0, -0.25f), r),
            Poly(p, new[] { new Vector2(0, tipY), new Vector2(-0.42f, 0.1f), new Vector2(0.42f, 0.1f) }));

        // ---- Primitivas SDF (espacio normalizado [-1,1], y hacia arriba) ----

        static float Min(float a, float b) => Mathf.Min(a, b);

        static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;

        static float Ellipse(Vector2 p, float rx, float ry)
        {
            // aproximación suficiente para anillos decorativos
            var q = new Vector2(p.x / rx, p.y / ry);
            return (q.magnitude - 1f) * Mathf.Min(rx, ry);
        }

        static float Capsule(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - r;
        }

        static float RoundRect(Vector2 p, float hw, float hh, float r)
        {
            var q = new Vector2(Mathf.Abs(p.x) - hw + r, Mathf.Abs(p.y) - hh + r);
            return new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude
                   + Mathf.Min(Mathf.Max(q.x, q.y), 0) - r;
        }

        static float Poly(Vector2 p, Vector2[] v)
        {
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                Vector2 e = v[j] - v[i];
                Vector2 w = p - v[i];
                Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c0 = p.y >= v[i].y, c1 = p.y < v[j].y, c2 = e.x * w.y > e.y * w.x;
                if ((c0 && c1 && c2) || (!c0 && !c1 && !c2)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        static Sprite Render(string name, Func<Vector2, float> sdf)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var px = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    var p = new Vector2((x + 0.5f) / Size * 2f - 1f, (y + 0.5f) / Size * 2f - 1f);
                    float a = Mathf.Clamp01(0.5f - sdf(p) * (Size / 2f));
                    px[y * Size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f));
        }
    }
}
