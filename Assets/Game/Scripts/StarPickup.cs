using UnityEngine;

namespace MichiCosmico
{
    /// <summary>Estrella coleccionable: flota, gira y brilla.</summary>
    public class StarPickup : MonoBehaviour
    {
        Vector3 basePosition;
        float phase;
        SpriteRenderer glow;

        void Start()
        {
            basePosition = transform.position;
            phase = Random.value * 6.28f;

            var glowChild = transform.Find("Glow");
            if (glowChild != null) glow = glowChild.GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            float t = Time.time + phase;

            transform.position = basePosition + new Vector3(0f, Mathf.Sin(t * 2.1f) * 0.14f, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, t * 42f);
            transform.localScale = Vector3.one * (1f + Mathf.Sin(t * 3.4f) * 0.09f);

            if (glow != null)
            {
                var color = glow.color;
                color.a = 0.30f + 0.22f * Mathf.Sin(t * 3.4f);
                glow.color = color;
                glow.transform.localScale = Vector3.one * (2.1f + 0.25f * Mathf.Sin(t * 3.4f));
            }
        }
    }
}
