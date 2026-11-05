using System.Collections;
using GroundRumble.Player;
using UnityEngine;

namespace GroundRumble.Events
{
    public class EarthquakeSystem : MonoBehaviour
    {
        public static EarthquakeSystem Current { get; private set; }

        [Header("Timing")]
        [Tooltip("Segundos hasta que inicia el primer terremoto (cronómetro en pantalla).")]
        [SerializeField] private float firstShakeDelay = 20f;

        [Tooltip("Segundos de espera entre terremotos una vez terminado el anterior.")]
        [SerializeField] private float shakeInterval = 15f;

        [Header("Shake")]
        [Tooltip("Duración del terremoto en segundos.")]
        [SerializeField] private float shakeDuration = 60f;
        [SerializeField] private float intensity = 0.25f;
        [SerializeField] private float frequency = 20f;

        [Header("Damage")]
        [SerializeField] private float damagePerSecond = 5f;

        public event System.Action ShakeStarted;
        public event System.Action ShakeEnded;

        public bool IsShaking { get; private set; }
        public bool IsPlayerSafe => SafeZone.IsPlayerSafe;

        public float SecondsUntilShake { get; private set; }
        public float ShakeTimeRemaining { get; private set; }
        public float ShakeDuration => shakeDuration;
        public float FirstShakeDelay => firstShakeDelay;

        private Transform cameraTransform;
        private Vector3 cameraBaseLocalPosition;
        private PlayerHealth playerHealth;

        private void Awake()
        {
            Current = this;
            playerHealth = FindAnyObjectByType<PlayerHealth>();
        }

        private void Start()
        {
            cameraTransform = Camera.main != null ? Camera.main.transform : null;
            if (cameraTransform != null)
            {
                cameraBaseLocalPosition = cameraTransform.localPosition;
            }

            StartCoroutine(ShakeLoop());
        }

        private IEnumerator ShakeLoop()
        {
            float delay = Mathf.Max(0f, firstShakeDelay);
            float elapsed = 0f;
            while (elapsed < delay)
            {
                SecondsUntilShake = delay - elapsed;
                elapsed += Time.deltaTime;
                yield return null;
            }
            SecondsUntilShake = 0f;

            while (true)
            {
                yield return StartCoroutine(Shake());

                float wait = Mathf.Max(0f, shakeInterval);
                elapsed = 0f;
                while (elapsed < wait)
                {
                    SecondsUntilShake = wait - elapsed;
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
        }

        private IEnumerator Shake()
        {
            IsShaking = true;
            ShakeTimeRemaining = Mathf.Max(0f, shakeDuration);
            ShakeStarted?.Invoke();

            float time = 0f;
            while (time < shakeDuration)
            {
                SetCameraShake(time);
                ApplyDamage();
                time += Time.deltaTime;
                ShakeTimeRemaining = Mathf.Max(0f, shakeDuration - time);
                yield return null;
            }

            ResetCamera();
            ShakeTimeRemaining = 0f;
            IsShaking = false;
            ShakeEnded?.Invoke();
        }

        private void SetCameraShake(float time)
        {
            if (cameraTransform == null)
            {
                return;
            }
            float ox = Mathf.PerlinNoise(time * frequency, 0f) - 0.5f;
            float oy = Mathf.PerlinNoise(time * frequency, 10f) - 0.5f;
            float oz = Mathf.PerlinNoise(time * frequency, 20f) - 0.5f;
            cameraTransform.localPosition = cameraBaseLocalPosition + new Vector3(ox, oy, oz) * intensity;
        }

        private void ApplyDamage()
        {
            if (playerHealth == null || SafeZone.IsPlayerSafe)
            {
                return;
            }

            playerHealth.TakeDamage(damagePerSecond * Time.deltaTime);
        }

        private void ResetCamera()
        {
            if (cameraTransform != null)
            {
                cameraTransform.localPosition = cameraBaseLocalPosition;
            }
        }
    }
}
