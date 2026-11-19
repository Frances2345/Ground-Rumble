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

        [Header("Caida")]
        [SerializeField] private float fallRollAngle = 82f;
        [SerializeField] private float fallPitchAngle = 18f;
        [SerializeField] private float fallDrop = 1.1f;

        private EarthquakeManager manager;
        private Vector3 originalLocalPosition;
        private Quaternion originalLocalRotation;
        private float noiseSeed;
        private float currentIntensity;
        private float slipTimer;
        private float slipDuration;
        private float slipRollDirection = 1f;
        private float fallTimer;
        private float fallDuration;
        private float fallRollDirection = 1f;
        private float fallPitchDirection = 1f;
        private float warningTimer;

        public static CameraShake Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            originalLocalPosition = transform.localPosition;
            originalLocalRotation = transform.localRotation;
            noiseSeed = Random.value * 100f;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void PlaySlip(float duration)
        {
            slipDuration = Mathf.Max(0.1f, duration);
            slipTimer = slipDuration;
            slipRollDirection = Random.value < 0.5f ? -1f : 1f;
        }

        public void PlayFallStun(float duration)
        {
            fallDuration = Mathf.Max(0.1f, duration);
            fallTimer = fallDuration;
            fallRollDirection = Random.value < 0.5f ? -1f : 1f;
            fallPitchDirection = Random.value < 0.5f ? -1f : 1f;
        }

        public void SetFloorWarning(float duration)
        {
            warningTimer = Mathf.Max(warningTimer, duration);
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

            if (fallTimer > 0f)
            {
                fallTimer = Mathf.Max(0f, fallTimer - Time.deltaTime);
            }

            if (warningTimer > 0f)
            {
                warningTimer = Mathf.Max(0f, warningTimer - Time.deltaTime);
            }

            float slipProgress = slipDuration > 0f ? 1f - (slipTimer / slipDuration) : 1f;
            float slipEnvelope = slipTimer > 0f ? Mathf.Sin(slipProgress * Mathf.PI) : 0f;

            float fallEnvelope = 0f;

            if (fallTimer > 0f)
            {
                float fallProgress = 1f - (fallTimer / fallDuration);
                float fadeIn = Mathf.Clamp01(fallProgress / 0.12f);
                float fadeOut = Mathf.Clamp01((1f - fallProgress) / 0.18f);
                fallEnvelope = Mathf.Min(fadeIn, fadeOut);
            }

            if (currentIntensity <= 0.001f && slipEnvelope <= 0.0001f && fallEnvelope <= 0.0001f && warningTimer <= 0f)
            {
                transform.localPosition = originalLocalPosition;
                transform.localRotation = originalLocalRotation;
                return;
            }

            float shakeIntensity = currentIntensity;

            if (warningTimer > 0f)
            {
                shakeIntensity = Mathf.Max(shakeIntensity, 0.3f) * 4f;
            }

            float time = Time.time * frequency;
            float x = (Mathf.PerlinNoise(noiseSeed, time) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(noiseSeed + 11.3f, time) - 0.5f) * 2f;
            float z = (Mathf.PerlinNoise(noiseSeed + 23.7f, time) - 0.5f) * 2f;

            Vector3 offset = new Vector3(x, y, z) * (positionShake * shakeIntensity);
            offset += Vector3.down * ((slipDrop * slipEnvelope) + (fallDrop * fallEnvelope));

            Quaternion shakeRotation = Quaternion.Euler(
                x * rotationShake * shakeIntensity,
                y * rotationShake * shakeIntensity,
                z * rotationShake * shakeIntensity);

            Quaternion slipRotation = Quaternion.Euler(
                slipPitchAngle * slipEnvelope,
                0f,
                slipRollAngle * slipRollDirection * slipEnvelope);

            Quaternion fallRotation = Quaternion.Euler(
                fallPitchAngle * fallPitchDirection * fallEnvelope,
                0f,
                fallRollAngle * fallRollDirection * fallEnvelope);

            transform.localPosition = originalLocalPosition + offset;
            transform.localRotation = originalLocalRotation * fallRotation * slipRotation * shakeRotation;
        }
    }
}
