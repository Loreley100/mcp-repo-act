using System.Collections.Generic;
using UnityEngine;

namespace MichiCosmico
{
    /// <summary>
    /// Genera por código todas las texturas y sprites del juego: el proyecto no
    /// necesita ningún asset gráfico externo. Un solo material "unlit" para todo.
    /// </summary>
    public static class GameArt
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        static Material unlitMaterial;
        static Font uiFont;

        // ------------------------------------------------------------------ material

        public static Material Unlit
        {
            get
            {
                if (unlitMaterial != null) return unlitMaterial;

                string[] candidates =
                {
                    "Sprites/Default",
                    "Universal Render Pipeline/2D/Sprite-Unlit-Default",
                    "Universal Render Pipeline/Unlit",
                    "UI/Default"
                };

                foreach (var shaderName in candidates)
                {
                    var shader = Shader.Find(shaderName);
                    if (shader == null) continue;
                    unlitMaterial = new Material(shader) { name = "MichiUnlit" };
                    return unlitMaterial;
                }

                return null;
            }
        }

        // ------------------------------------------------------------------ tipografía

        public static Font UIFont
        {
            get
            {
                if (uiFont != null) return uiFont;
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (uiFont == null) uiFont = Font.CreateDynamicFontFromOSFont("Arial", 32);
                return uiFont;
            }
        }

        // ------------------------------------------------------------------ sprites

        public static Sprite Square() => Build("square", 8, p => Color.white);

        public static Sprite Disc() => Build("disc", 128, p => White(InDisc(p, Vector2.zero, 0.92f)));

        public static Sprite Soft() => Build("soft", 128, SoftShader);

        public static Sprite Star() => Build("star", 128, StarShader);

        public static Sprite Spark() => Build("spark", 64, p => White(InStar(p, 0.95f, 0.16f, 4, Mathf.PI * 0.25f)));

        public static Sprite Heart() => Build("heart", 96, HeartShader);

        public static Sprite Cat() => Build("cat", 160, CatShader);

        public static Sprite Panel() => Build("panel", 128, PanelShader, new Vector4(46f, 46f, 46f, 46f));

        public static Sprite Asteroid(int seed) => Build("asteroid_" + seed, 128, AsteroidShader(seed));

        /// <summary>Degradado vertical para el fondo (se escala para cubrir la cámara).</summary>
        public static Sprite Background()
        {
            const string key = "background";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            const int w = 4;
            const int h = 256;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var bottom = new Color(0.16f, 0.06f, 0.26f); // púrpura profundo
            var top = new Color(0.02f, 0.04f, 0.10f);    // azul noche

            for (int y = 0; y < h; y++)
            {
                var c = Color.Lerp(bottom, top, y / (float)(h - 1));
                for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
            }

            tex.Apply(false, false);
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = key;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Cache[key] = sprite;
            return sprite;
        }

        // ------------------------------------------------------------------ generador

        static Sprite Build(string key, int size, System.Func<Vector2, Color> shade, Vector4 border = default)
        {
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color[size * size];
            const int samples = 3;
            const float invSamples = 1f / (samples * samples);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float r = 0f, g = 0f, b = 0f, a = 0f;

                    for (int oy = 0; oy < samples; oy++)
                    {
                        for (int ox = 0; ox < samples; ox++)
                        {
                            float u = ((x + (ox + 0.5f) / samples) / size) * 2f - 1f;
                            float v = ((y + (oy + 0.5f) / samples) / size) * 2f - 1f;
                            var c = shade(new Vector2(u, v));
                            r += c.r * c.a;
                            g += c.g * c.a;
                            b += c.b * c.a;
                            a += c.a;
                        }
                    }

                    r *= invSamples;
                    g *= invSamples;
                    b *= invSamples;
                    a *= invSamples;

                    pixels[y * size + x] = a > 0.0001f
                        ? new Color(r / a, g / a, b / a, a)
                        : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false);

            // píxeles por unidad = tamaño => cada sprite mide 1x1 unidades de mundo.
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                size, 0, SpriteMeshType.FullRect, border);
            sprite.name = key;
            sprite.hideFlags = HideFlags.HideAndDontSave;

            Cache[key] = sprite;
            return sprite;
        }

        // ------------------------------------------------------------------ formas

        static Color White(bool inside) => inside ? Color.white : Color.clear;

        static bool InDisc(Vector2 p, Vector2 center, float radius) =>
            (p - center).sqrMagnitude <= radius * radius;

        static float Side(Vector2 p1, Vector2 p2, Vector2 p3) =>
            (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);

        static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Side(p, a, b);
            float d2 = Side(p, b, c);
            float d3 = Side(p, c, a);
            bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNeg && hasPos);
        }

        static bool InSegment(Vector2 p, Vector2 a, Vector2 b, float halfWidth)
        {
            var ab = b - a;
            float denom = Vector2.Dot(ab, ab);
            float t = denom <= 0f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / denom);
            return (a + ab * t - p).sqrMagnitude <= halfWidth * halfWidth;
        }

        static Vector2 StarVertex(int i, float outer, float inner, int points, float rotation)
        {
            float radius = (i % 2 == 0) ? outer : inner;
            float angle = rotation + i * Mathf.PI / points;
            return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
        }

        static bool InStar(Vector2 p, float outer, float inner, int points, float rotation)
        {
            bool inside = false;
            int n = points * 2;
            var prev = StarVertex(n - 1, outer, inner, points, rotation);

            for (int i = 0; i < n; i++)
            {
                var cur = StarVertex(i, outer, inner, points, rotation);
                if ((cur.y > p.y) != (prev.y > p.y) &&
                    p.x < (prev.x - cur.x) * (p.y - cur.y) / (prev.y - cur.y) + cur.x)
                {
                    inside = !inside;
                }
                prev = cur;
            }

            return inside;
        }

        // ------------------------------------------------------------------ shaders

        static Color SoftShader(Vector2 p)
        {
            float d = Mathf.Clamp01(p.magnitude / 0.98f);
            return new Color(1f, 1f, 1f, Mathf.Pow(1f - d, 2.4f));
        }

        static Color StarShader(Vector2 p)
        {
            if (!InStar(p, 0.95f, 0.42f, 5, Mathf.PI * 0.5f)) return Color.clear;
            float center = Mathf.Clamp01(1f - p.magnitude / 0.55f);
            return Color.Lerp(new Color(1f, 0.92f, 0.62f), Color.white, center * 0.8f);
        }

        static Color HeartShader(Vector2 p)
        {
            bool inside = InDisc(p, new Vector2(-0.26f, 0.18f), 0.36f) ||
                          InDisc(p, new Vector2(0.26f, 0.18f), 0.36f) ||
                          InTriangle(p, new Vector2(-0.60f, 0.10f), new Vector2(0.60f, 0.10f), new Vector2(0f, -0.74f));
            return White(inside);
        }

        static Color PanelShader(Vector2 p)
        {
            const float half = 0.92f;
            const float radius = 0.30f;
            var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - new Vector2(half - radius, half - radius);
            var outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
            float dist = outside.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
            return dist <= 0f ? Color.white : Color.clear;
        }

        static Color CatShader(Vector2 p)
        {
            var outline = new Color(0.17f, 0.09f, 0.25f);
            var fur = new Color(1.00f, 0.83f, 0.93f);
            var ear = new Color(1.00f, 0.47f, 0.70f);
            var eye = new Color(0.14f, 0.08f, 0.20f);
            var blush = new Color(1.00f, 0.42f, 0.63f);
            var nose = new Color(0.86f, 0.24f, 0.47f);

            // Orejas (detrás de la cabeza)
            if (InTriangle(p, new Vector2(-0.80f, 0.18f), new Vector2(-0.58f, 1.00f), new Vector2(-0.04f, 0.40f))) return outline;
            if (InTriangle(p, new Vector2(0.80f, 0.18f), new Vector2(0.58f, 1.00f), new Vector2(0.04f, 0.40f))) return outline;
            if (InTriangle(p, new Vector2(-0.66f, 0.28f), new Vector2(-0.55f, 0.85f), new Vector2(-0.16f, 0.44f))) return ear;
            if (InTriangle(p, new Vector2(0.66f, 0.28f), new Vector2(0.55f, 0.85f), new Vector2(0.16f, 0.44f))) return ear;

            // Cabeza
            var head = new Vector2(0f, -0.08f);
            if (!InDisc(p, head, 0.86f)) return Color.clear;

            Color c = fur;
            if (!InDisc(p, head, 0.735f)) c = outline;

            // Mejillas
            float blushL = Mathf.Clamp01(1f - Vector2.Distance(p, new Vector2(-0.46f, -0.20f)) / 0.24f);
            float blushR = Mathf.Clamp01(1f - Vector2.Distance(p, new Vector2(0.46f, -0.20f)) / 0.24f);
            if (blushL > 0f) c = Color.Lerp(c, blush, blushL * 0.5f);
            if (blushR > 0f) c = Color.Lerp(c, blush, blushR * 0.5f);

            // Bigotes
            if (InSegment(p, new Vector2(-0.42f, -0.26f), new Vector2(-0.94f, -0.20f), 0.018f) ||
                InSegment(p, new Vector2(-0.42f, -0.36f), new Vector2(-0.94f, -0.38f), 0.018f) ||
                InSegment(p, new Vector2(0.42f, -0.26f), new Vector2(0.94f, -0.20f), 0.018f) ||
                InSegment(p, new Vector2(0.42f, -0.36f), new Vector2(0.94f, -0.38f), 0.018f))
                return fur;

            // Ojos
            if (InDisc(p, new Vector2(-0.26f, 0.02f), 0.145f) || InDisc(p, new Vector2(0.26f, 0.02f), 0.145f)) return eye;
            if (InDisc(p, new Vector2(-0.30f, 0.07f), 0.048f) || InDisc(p, new Vector2(0.30f, 0.07f), 0.048f)) return Color.white;

            // Nariz y boquita
            if (InTriangle(p, new Vector2(-0.09f, -0.18f), new Vector2(0.09f, -0.18f), new Vector2(0f, -0.30f))) return nose;
            if (InSegment(p, new Vector2(0f, -0.30f), new Vector2(-0.11f, -0.38f), 0.020f) ||
                InSegment(p, new Vector2(0f, -0.30f), new Vector2(0.11f, -0.38f), 0.020f))
                return outline;

            return c;
        }

        static System.Func<Vector2, Color> AsteroidShader(int seed)
        {
            var rnd = new System.Random(seed * 977 + 13);
            var craters = new List<Vector2>();
            var craterRadius = new List<float>();
            for (int i = 0; i < 4; i++)
            {
                craters.Add(new Vector2((float)rnd.NextDouble() * 1.0f - 0.5f, (float)rnd.NextDouble() * 1.0f - 0.5f));
                craterRadius.Add(0.09f + (float)rnd.NextDouble() * 0.10f);
            }

            float phaseA = (float)rnd.NextDouble() * 6.28f;
            float phaseB = (float)rnd.NextDouble() * 6.28f;

            return p =>
            {
                float angle = Mathf.Atan2(p.y, p.x);
                float radius = 0.72f + 0.10f * Mathf.Sin(3f * angle + phaseA) + 0.05f * Mathf.Sin(7f * angle + phaseB);
                float dist = p.magnitude;
                if (dist > radius) return Color.clear;

                var color = new Color(0.56f, 0.52f, 0.73f);
                if (dist > radius - 0.07f) color = new Color(0.29f, 0.25f, 0.43f);

                for (int i = 0; i < craters.Count; i++)
                {
                    if ((p - craters[i]).sqrMagnitude <= craterRadius[i] * craterRadius[i])
                        color = new Color(0.42f, 0.38f, 0.58f);
                }

                float light = Vector2.Dot(p.normalized, new Vector2(-0.6f, 0.7f)) * 0.5f + 0.5f;
                color = Color.Lerp(color, Color.white, 0.12f * light);

                return new Color(color.r, color.g, color.b, 1f);
            };
        }
    }
}
