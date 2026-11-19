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

        [Header("Resbalon")]
        [SerializeField] private float slipRollAngle = 150f;
        [SerializeField] private float slipPitchAngle = 22f;
        [SerializeField] private float slipDrop = 0.9f;

        private EarthquakeManager manager;
        private Vector3 originalLocalPosition;
        private Quaternion originalLocalRotation;
        private float noiseSeed;
        private float currentIntensity;
        private float slipTimer;
        private float slipDuration;
        private float slipRollDirection = 1f;

        private void Awake()
        {
            originalLocalPosition = transform.localPosition;
            originalLocalRotation = transform.localRotation;
            noiseSeed = Random.value * 100f;
        }

        public void PlaySlip(float duration)
        {
            slipDuration = Mathf.Max(0.1f, duration);
            slipTimer = slipDuration;
            slipRollDirection = Random.value < 0.5f ? -1f : 1f;
        }

        private void Update()
        {
            if (manager == null)
            {
                manager = EarthquakeManager.Instance;
            }

            float targetIntensity = manager != null && manager.IsShaking ? manager.Intensity : 0f;
            currentIntensity = Mathf.MoveTowards(currentIntensity, targetIntensity, Time.deltaTime * intensitySmoothing);

            if (slipTimer > 0f)
            {
                slipTimer = Mathf.Max(0f, slipTimer - Time.deltaTime);
            }

            float slipProgress = slipDuration > 0f ? 1f - (slipTimer / slipDuration) : 1f;
            float slipEnvelope = slipTimer > 0f ? Mathf.Sin(slipProgress * Mathf.PI) : 0f;

            if (currentIntensity <= 0.001f && slipEnvelope <= 0.0001f)
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
            offset += Vector3.down * (slipDrop * slipEnvelope);

            Quaternion shakeRotation = Quaternion.Euler(
                x * rotationShake * currentIntensity,
                y * rotationShake * currentIntensity,
                z * rotationShake * currentIntensity);

            Quaternion slipRotation = Quaternion.Euler(
                slipPitchAngle * slipEnvelope,
                0f,
                slipRollAngle * slipRollDirection * slipEnvelope);

            transform.localPosition = originalLocalPosition + offset;
            transform.localRotation = originalLocalRotation * slipRotation * shakeRotation;
        }
    }
}
