using System.Collections;
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

        public bool IsShaking { get; private set; }

        private void Awake()
        {
            Current = this;
        }

        private void Start()
        {
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
                time += Time.deltaTime;
                yield return null;
            }
            ResetCamera();
            IsShaking = false;
        }

        private void SetCameraShake(float time)
        {
            Transform cam = Camera.main == null ? null : Camera.main.transform;
            if (cam == null)
            {
                return;
            }
            float ox = Mathf.PerlinNoise(time * frequency, 0f) - 0.5f;
            float oy = Mathf.PerlinNoise(time * frequency, 10f) - 0.5f;
            float oz = Mathf.PerlinNoise(time * frequency, 20f) - 0.5f;
            cam.localPosition = new Vector3(ox, oy, oz) * intensity;
        }

        private void ResetCamera()
        {
            Transform cam = Camera.main == null ? null : Camera.main.transform;
            if (cam != null)
            {
                cam.localPosition = Vector3.zero;
            }
        }
    }
}
