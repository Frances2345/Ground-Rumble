using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GroundRumble
{
    public class Timer : MonoBehaviour
    {
        [Header("Temporizador")]
        [SerializeField] private float duration = 300f;
        [SerializeField] private bool autoStart = true;

        [Header("UI (asignar en el inspector)")]
        public TMP_Text timerText;
        public TMP_Text deathText;

        [Header("Muerte")]
        [SerializeField, TextArea(1, 2)] private string deathMessage = "HAS MUERTO";
        [SerializeField] private string gameOverScene = "GameOver";
        [SerializeField] private float delayBeforeGameOver = 2f;

        public float Duration => duration;
        public float TimeLeft { get; private set; }
        public bool IsRunning { get; private set; }
        public bool HasDied { get; private set; }
        public string GameOverScene => gameOverScene;

        private EarthquakeManager manager;
        private PlayerController player;
        private Vida vida;
        private Coroutine gameOverRoutine;

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
            if (HasDied)
            {
                return;
            }

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
            if (HasDied)
            {
                return;
            }

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
            if (HasDied)
            {
                return;
            }

            HasDied = true;
            IsRunning = false;
            UpdateTimerText();

            if (manager == null)
            {
                manager = EarthquakeManager.Instance;
            }

            if (manager != null)
            {
                manager.StopEarthquake();
            }

            if (player == null)
            {
                player = FindAnyObjectByType<PlayerController>();
            }

            if (player != null)
            {
                player.SetControlEnabled(false);
            }

            ShowDeathMessage();

            if (gameOverRoutine != null)
            {
                StopCoroutine(gameOverRoutine);
            }

            gameOverRoutine = StartCoroutine(LoadGameOverRoutine());
        }

        private void ShowDeathMessage()
        {
            if (Estado.Instance != null)
            {
                Estado.Instance.ShowDeath(deathMessage);
                return;
            }

            if (deathText != null)
            {
                deathText.text = deathMessage;
                deathText.gameObject.SetActive(true);
                deathText.transform.SetAsLastSibling();
            }
        }

        private IEnumerator LoadGameOverRoutine()
        {
            if (delayBeforeGameOver > 0f)
            {
                yield return new WaitForSecondsRealtime(delayBeforeGameOver);
            }

            if (string.IsNullOrEmpty(gameOverScene))
            {
                Debug.LogWarning("Ground Rumble: no se asigno la escena de Game Over.");
                yield break;
            }

            if (!Application.CanStreamedLevelBeLoaded(gameOverScene))
            {
                Debug.LogWarning($"Ground Rumble: la escena '{gameOverScene}' no existe o no esta en Build Settings.");
                yield break;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(gameOverScene);
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
            if (timerText == null)
            {
                return;
            }

            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, TimeLeft));
            timerText.text = $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
            timerText.color = TimeLeft <= 30f ? new Color(1f, 0.3f, 0.2f) : Color.white;
        }

    }
}
