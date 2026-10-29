using System.Collections;
using GroundRumble.Player;
using UnityEngine;

namespace GroundRumble.Events
{
    public class EarthquakeSystem : MonoBehaviour
    {
        public static EarthquakeSystem Current { get; private set; }

        [Header("Timing")]
        [SerializeField] private float firstShakeDelay = 3f;
        [SerializeField] private float shakeInterval = 12f;

        [Header("Shake")]
        [SerializeField] private float shakeDuration = 2.5f;
        [SerializeField] private float intensity = 0.25f;
        [SerializeField] private float frequency = 20f;

        [Header("Damage")]
        [SerializeField] private float damagePerSecond = 5f;

        public bool IsShaking { get; private set; }
        public bool IsPlayerSafe => SafeZone.IsPlayerSafe;

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

        private System.Collections.IEnumerator ShakeLoop()
        {
            yield return new WaitForSeconds(firstShakeDelay);
            while (true)
            {
                StartCoroutine(Shake());
                yield return new WaitForSeconds(shakeInterval <= 0f ? 1f : shakeInterval);
            }
        }

        private System.Collections.IEnumerator Shake()
        {
            IsShaking = true;
            float time = 0f;
            while (time < shakeDuration)
            {
                SetCameraShake(time);
                ApplyDamage();
                time += Time.deltaTime;
                yield return null;
            }
            ResetCamera();
            IsShaking = false;
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
