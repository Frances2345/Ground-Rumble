using System;
using System.Collections;
using UnityEngine;

namespace GroundRumble
{
    /// <summary>
    /// Controla el ciclo del terremoto: aviso previo, duracion e intensidad.
    /// </summary>
    public class EarthquakeManager : MonoBehaviour
    {
        public enum QuakeState
        {
            Idle,
            Warning,
            Shaking
        }

        private static EarthquakeManager instance;

        public static EarthquakeManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<EarthquakeManager>();
                }

                return instance;
            }
            private set => instance = value;
        }

        [Header("Configuracion del terremoto")]
        [SerializeField] private float warningDuration = 10f;
        [SerializeField] private float earthquakeDuration = 15f;
        [Range(0f, 1f)]
        [SerializeField] private float intensity = 0.6f;

        [Header("Automatico")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private float autoStartDelay = 3f;
        [SerializeField] private bool repeat = false;
        [SerializeField] private float repeatDelay = 25f;

        public QuakeState State { get; private set; } = QuakeState.Idle;
        public float WarningDuration
        {
            get => warningDuration;
            set => warningDuration = Mathf.Max(0f, value);
        }
        public float EarthquakeDuration
        {
            get => earthquakeDuration;
            set => earthquakeDuration = Mathf.Max(0.1f, value);
        }
        public float Intensity
        {
            get => intensity;
            set => intensity = Mathf.Clamp01(value);
        }

        public bool IsShaking => State == QuakeState.Shaking;
        public bool IsWarning => State == QuakeState.Warning;
        public float TimeUntilEarthquake => State == QuakeState.Warning ? Mathf.Max(0f, warningTimer) : 0f;
        public float RemainingDuration => State == QuakeState.Shaking ? Mathf.Max(0f, earthquakeTimer) : 0f;
        public float TotalDuration => earthquakeDuration;

        public event Action<float> OnWarningStarted;
        public event Action<float, float> OnEarthquakeStarted;
        public event Action OnEarthquakeEnded;
        public event Action<QuakeState> OnStateChanged;

        private float warningTimer;
        private float earthquakeTimer;
        private float repeatTimer;
        private bool hasPlayed;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Start()
        {
            if (autoStart)
            {
                StartCoroutine(AutoStartRoutine());
            }
        }

        private IEnumerator AutoStartRoutine()
        {
            if (autoStartDelay > 0f)
            {
                yield return new WaitForSeconds(autoStartDelay);
            }

            if (!hasPlayed && State == QuakeState.Idle)
            {
                StartSequence();
            }
        }

        private void Update()
        {
            switch (State)
            {
                case QuakeState.Warning:
                    warningTimer -= Time.deltaTime;

                    if (warningTimer <= 0f)
                    {
                        StartShaking();
                    }
                    break;

                case QuakeState.Shaking:
                    earthquakeTimer -= Time.deltaTime;

                    if (earthquakeTimer <= 0f)
                    {
                        EndShaking();
                    }
                    break;

                case QuakeState.Idle:
                    if (repeat && hasPlayed && repeatTimer > 0f)
                    {
                        repeatTimer -= Time.deltaTime;

                        if (repeatTimer <= 0f)
                        {
                            StartSequence();
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Inicia el aviso. Si warningDuration es 0, empieza a temblar de inmediato.
        /// </summary>
        public void StartSequence()
        {
            if (State != QuakeState.Idle)
            {
                return;
            }

            if (warningDuration <= 0f)
            {
                StartShaking();
                return;
            }

            SetState(QuakeState.Warning);
            warningTimer = warningDuration;
            OnWarningStarted?.Invoke(warningDuration);
        }

        /// <summary>
        /// Salta el aviso y comienza a temblar ahora mismo.
        /// </summary>
        public void TriggerEarthquakeNow()
        {
            if (State == QuakeState.Shaking)
            {
                earthquakeTimer = earthquakeDuration;
                return;
            }

            StartShaking();
        }

        /// <summary>
        /// Detiene el terremoto actual y vuelve a Idle.
        /// </summary>
        public void StopEarthquake()
        {
            warningTimer = 0f;

            if (State == QuakeState.Shaking)
            {
                EndShaking();
            }
            else if (State == QuakeState.Warning)
            {
                SetState(QuakeState.Idle);
            }
        }

        private void StartShaking()
        {
            SetState(QuakeState.Shaking);
            earthquakeTimer = earthquakeDuration;
            OnEarthquakeStarted?.Invoke(earthquakeDuration, intensity);
        }

        private void EndShaking()
        {
            earthquakeTimer = 0f;
            hasPlayed = true;
            SetState(QuakeState.Idle);
            OnEarthquakeEnded?.Invoke();

            if (repeat)
            {
                repeatTimer = repeatDelay;
            }
        }

        private void SetState(QuakeState newState)
        {
            if (State == newState)
            {
                return;
            }

            State = newState;
            OnStateChanged?.Invoke(State);
        }
    }
}
