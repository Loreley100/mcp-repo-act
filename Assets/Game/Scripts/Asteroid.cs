using UnityEngine;

namespace MichiCosmico
{
    /// <summary>Meteorito que rebota por la arena; acelera conforme recoges estrellas.</summary>
    public class Asteroid : MonoBehaviour
    {
        public Vector2 Velocity;
        public float Spin;

        Vector2 bounds;
        float speedMultiplier = 1f;
        float radius;

        public void Setup(Vector2 velocity, float spin, Vector2 arenaBounds, float worldRadius)
        {
            Velocity = velocity;
            Spin = spin;
            bounds = arenaBounds;
            radius = worldRadius;
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = multiplier;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var position = (Vector2)transform.position + Velocity * speedMultiplier * dt;

            if (position.x < -bounds.x - radius) { position.x = -bounds.x - radius; Velocity.x = Mathf.Abs(Velocity.x); }
            if (position.x > bounds.x + radius) { position.x = bounds.x + radius; Velocity.x = -Mathf.Abs(Velocity.x); }
            if (position.y < -bounds.y - radius) { position.y = -bounds.y - radius; Velocity.y = Mathf.Abs(Velocity.y); }
            if (position.y > bounds.y + radius) { position.y = bounds.y + radius; Velocity.y = -Mathf.Abs(Velocity.y); }

            transform.position = position;
            transform.Rotate(0f, 0f, Spin * dt);
        }
    }
}
