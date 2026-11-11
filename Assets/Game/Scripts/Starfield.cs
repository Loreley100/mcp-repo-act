using UnityEngine;

namespace MichiCosmico
{
    /// <summary>Fondo espacial: degradado, nebulosas, un planeta y un campo de estrellas con parpadeo.</summary>
    public class Starfield : MonoBehaviour
    {
        struct Dot
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public float BaseAlpha;
            public float Phase;
            public float Speed;
        }

        Dot[] dots;
        float width;
        float height;

        public void Build(Vector2 bounds, Transform world)
        {
            width = bounds.x + 3f;
            height = bounds.y + 2f;

            // Degradado de fondo (cubre toda la cámara)
            var background = MakeSprite("Fondo", GameArt.Background(), new Color(1f, 1f, 1f, 1f), -60, world);
            var backgroundBounds = GameArt.Background().bounds.size;
            background.transform.localScale = new Vector3(
                (bounds.x * 2f + 4f) / backgroundBounds.x,
                (bounds.y * 2f + 4f) / backgroundBounds.y,
                1f);
            background.transform.position = new Vector3(0f, 0f, 40f);

            // Nebulosas suaves
            var nebulaColors = new[]
            {
                new Color(0.42f, 0.20f, 0.72f, 0.16f),
                new Color(0.16f, 0.32f, 0.72f, 0.14f),
                new Color(0.72f, 0.24f, 0.52f, 0.10f)
            };
            var nebulaPositions = new Vector3[] { new(-6f, 2.5f, 30f), new(5.5f, -2f, 30f), new(0.5f, 3.4f, 30f) };
            var nebulaScales = new Vector3[] { new(16f, 12f, 1f), new(14f, 10f, 1f), new(18f, 8f, 1f) };

            for (int i = 0; i < nebulaColors.Length; i++)
            {
                var nebula = MakeSprite("Nebulosa_" + i, GameArt.Soft(), nebulaColors[i], -55, world);
                nebula.transform.position = nebulaPositions[i];
                nebula.transform.localScale = nebulaScales[i];
            }

            // Planeta lejano con anillo
            var planetGlow = MakeSprite("Planeta_Glow", GameArt.Soft(), new Color(0.55f, 0.35f, 0.95f, 0.28f), -54, world);
            planetGlow.transform.position = new Vector3(-bounds.x + 1.4f, bounds.y - 1.6f, 29f);
            planetGlow.transform.localScale = new Vector3(9.5f, 9.5f, 1f);

            var planet = MakeSprite("Planeta", GameArt.Disc(), new Color(0.36f, 0.22f, 0.62f, 1f), -53, world);
            planet.transform.position = new Vector3(-bounds.x + 1.4f, bounds.y - 1.6f, 28f);
            planet.transform.localScale = new Vector3(4.6f, 4.6f, 1f);

            var planetShadow = MakeSprite("Planeta_Sombra", GameArt.Disc(), new Color(0.10f, 0.05f, 0.20f, 0.55f), -52, world);
            planetShadow.transform.position = new Vector3(-bounds.x + 1.0f, bounds.y - 1.25f, 27f);
            planetShadow.transform.localScale = new Vector3(4.6f, 4.6f, 1f);

            var ring = MakeSprite("Anillo", GameArt.Square(), new Color(0.75f, 0.55f, 1.00f, 0.35f), -52, world);
            ring.transform.position = new Vector3(-bounds.x + 1.4f, bounds.y - 1.6f, 27f);
            ring.transform.rotation = Quaternion.Euler(0f, 0f, -18f);
            ring.transform.localScale = new Vector3(6.2f, 0.28f, 1f);

            // Campo de estrellas
            const int count = 110;
            dots = new Dot[count];
            for (int i = 0; i < count; i++)
            {
                var dot = MakeSprite("Estrella_" + i, GameArt.Disc(), Color.white, -50, transform);
                dot.transform.position = new Vector3(
                    Random.Range(-width, width),
                    Random.Range(-height, height),
                    Random.Range(20f, 26f));
                dot.transform.localScale = Vector3.one * Random.Range(0.035f, 0.13f);

                dots[i] = new Dot
                {
                    Transform = dot.transform,
                    Renderer = dot,
                    BaseAlpha = Random.Range(0.25f, 0.85f),
                    Phase = Random.value * 6.28f,
                    Speed = Random.Range(0.08f, 0.4f)
                };
            }
        }

        void Update()
        {
            if (dots == null) return;
            float time = Time.time;

            for (int i = 0; i < dots.Length; i++)
            {
                ref var dot = ref dots[i];

                var position = dot.Transform.position;
                position.x -= dot.Speed * Time.deltaTime;
                if (position.x < -width) position.x = width;
                dot.Transform.position = position;

                var color = dot.Renderer.color;
                color.a = dot.BaseAlpha * (0.55f + 0.45f * Mathf.Sin(time * 2.2f + dot.Phase));
                dot.Renderer.color = color;
            }
        }

        static SpriteRenderer MakeSprite(string name, Sprite sprite, Color color, int sortingOrder, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            renderer.sharedMaterial = GameArt.Unlit;
            return renderer;
        }
    }
}
