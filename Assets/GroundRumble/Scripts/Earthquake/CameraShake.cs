using UnityEngine;

namespace GroundRumble
{
    /// <summary>
    /// Sacude la camara segun la intensidad del terremoto.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        [SerializeField] private float positionShake = 0.07f;
        [SerializeField] private float rotationShake = 1.4f;
        [SerializeField] private float frequency = 18f;
        [SerializeField] private float intensitySmoothing = 3f;

        private EarthquakeManager manager;
        private Vector3 originalLocalPosition;
        private Quaternion originalLocalRotation;
        private float noiseSeed;
        private float currentIntensity;

        private void Awake()
        {
            originalLocalPosition = transform.localPosition;
            originalLocalRotation = transform.localRotation;
            noiseSeed = Random.value * 100f;
        }

        private void Update()
        {
            if (manager == null)
            {
                manager = EarthquakeManager.Instance;
            }

            float targetIntensity = manager != null && manager.IsShaking ? manager.Intensity : 0f;
            currentIntensity = Mathf.MoveTowards(currentIntensity, targetIntensity, Time.deltaTime * intensitySmoothing);

            if (currentIntensity <= 0.001f)
            {
                transform.localPosition = originalLocalPosition;
                transform.localRotation = originalLocalRotation;
                return;
            }

            float time = Time.time * frequency;
            float x = (Mathf.PerlinNoise(noiseSeed, time) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(noiseSeed + 11.3f, time) - 0.5f) * 2f;
            float z = (Mathf.PerlinNoise(noiseSeed + 23.7f, time) - 0.5f) * 2f;

            Vector3 offset = new Vector3(x, y, z) * (positionShake * currentIntensity);
            transform.localPosition = originalLocalPosition + offset;
            transform.localRotation = originalLocalRotation * Quaternion.Euler(
                x * rotationShake * currentIntensity,
                y * rotationShake * currentIntensity,
                z * rotationShake * currentIntensity);
        }
    }
}
