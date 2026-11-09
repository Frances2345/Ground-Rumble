using TMPro;
using UnityEngine;

namespace GroundRumble
{
    public class Timer : MonoBehaviour
    {
        [Header("Temporizador")]
        [SerializeField] private float duration = 300f;
        [SerializeField] private bool autoStart = true;

        [Header("UI (asignar en el inspector)")]
        public TMP_Text timerText;

        public float Duration => duration;
        public float TimeLeft { get; private set; }
        public bool IsRunning { get; private set; }
        public bool HasDied { get; private set; }

        private EarthquakeManager manager;
        private PlayerController player;
        private Vida vida;

        private void Awake()
        {
            TimeLeft = Mathf.Max(0f, duration);
            ResolveReferences();
            UpdateTimerText();
        }

        private void Start()
        {
            ResolveReferences();

            if (vida != null)
            {
                vida.RespawnOnDeath = false;
                vida.OnDied += HandlePlayerDied;
            }

            IsRunning = autoStart;
        }

        private void OnDestroy()
        {
            if (vida != null)
            {
                vida.OnDied -= HandlePlayerDied;
            }
        }

        private void Update()
        {
            if (!IsRunning || HasDied)
            {
                return;
            }

            TimeLeft -= Time.deltaTime;

            if (TimeLeft <= 0f)
            {
                TimeLeft = 0f;
                UpdateTimerText();
                HandleTimeUp();
                return;
            }

            UpdateTimerText();
        }

        public void StartTimer()
        {
            if (HasDied) return;

            TimeLeft = Mathf.Max(0f, duration);
            IsRunning = true;
            UpdateTimerText();
        }

        public void Pause()
        {
            IsRunning = false;
        }

        public void Resume()
        {
            if (!HasDied)
            {
                IsRunning = true;
            }
        }

        public void AddTime(float seconds)
        {
            TimeLeft = Mathf.Max(0f, TimeLeft + seconds);
            UpdateTimerText();
        }

        public void KillPlayerImmediately()
        {
            HandleTimeUp();
        }

        private void HandleTimeUp()
        {
            if (HasDied) return;

            ResolveReferences();
            IsRunning = false;

            if (vida != null)
            {
                vida.RespawnOnDeath = false;
                vida.Kill();
            }
            else
            {
                HandlePlayerDied();
            }
        }

        private void HandlePlayerDied()
        {
            if (HasDied) return;

            HasDied = true;
            IsRunning = false;
            UpdateTimerText();

            // Dispara la muerte centralizada en Estado -> Options
            if (Estado.Instance != null)
            {
                Estado.Instance.TriggerDeath();
            }
            else
            {
                Options options = FindAnyObjectByType<Options>();
                if (options != null)
                {
                    options.TriggerDeathSequence();
                }
            }
        }

        private void ResolveReferences()
        {
            if (player == null)
            {
                player = FindAnyObjectByType<PlayerController>();
            }

            if (vida == null)
            {
                vida = player != null ? player.GetComponent<Vida>() : FindAnyObjectByType<Vida>();
            }

            if (manager == null)
            {
                manager = EarthquakeManager.Instance;
            }
        }

        private void UpdateTimerText()
        {
            if (timerText == null) return;

            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, TimeLeft));
            timerText.text = $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
            timerText.color = TimeLeft <= 30f ? new Color(1f, 0.3f, 0.2f) : Color.white;
        }
    }
}