using UnityEngine;

namespace MichiCosmico
{
    /// <summary>Sacudida de cámara con ruido Perlin, sin dependencias externas.</summary>
    public class CameraShake : MonoBehaviour
    {
        Vector3 basePosition;
        float trauma;

        void Awake()
        {
            basePosition = transform.localPosition;
        }

        public void Add(float amount)
        {
            trauma = Mathf.Clamp01(trauma + amount);
        }

        void LateUpdate()
        {
            if (trauma <= 0f)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, basePosition, 12f * Time.unscaledDeltaTime);
                return;
            }

            float strength = trauma * trauma;
            float time = Time.unscaledTime * 24f;

            var offset = new Vector3(
                (Mathf.PerlinNoise(time, 0.37f) - 0.5f) * 2f,
                (Mathf.PerlinNoise(0.73f, time) - 0.5f) * 2f,
                0f) * (0.55f * strength);

            transform.localPosition = basePosition + offset;
            trauma = Mathf.Max(0f, trauma - Time.unscaledDeltaTime * 1.8f);
        }
    }
}
