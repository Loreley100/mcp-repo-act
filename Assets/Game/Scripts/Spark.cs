using UnityEngine;

namespace MichiCosmico
{
    /// <summary>Chispa de un solo uso para los bursts de partículas.</summary>
    public class Spark : MonoBehaviour
    {
        Vector2 velocity;
        float life;
        float maxLife;
        float spin;
        float startSize;
        SpriteRenderer spriteRenderer;
        Color color;

        public void Setup(Vector2 initialVelocity, float lifetime, float size, Color tint)
        {
            velocity = initialVelocity;
            life = maxLife = Mathf.Max(0.01f, lifetime);
            startSize = size;
            color = tint;
            spin = Random.Range(-540f, 540f);
            spriteRenderer = GetComponent<SpriteRenderer>();
            transform.localScale = Vector3.one * size;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;

            if (life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += (Vector3)(velocity * dt);
            velocity *= 1f - 3.4f * dt;
            transform.Rotate(0f, 0f, spin * dt);

            float k = life / maxLife;
            if (spriteRenderer != null)
            {
                var c = color;
                c.a *= k;
                spriteRenderer.color = c;
            }

            transform.localScale = Vector3.one * (startSize * (0.35f + 0.65f * k));
        }
    }
}
