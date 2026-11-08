using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GroundRumble
{
    public class Estado : MonoBehaviour
    {
        public static Estado Instance { get; private set; }

        [Header("Estado del terremoto (asignar en el inspector)")]
        public TMP_Text stateText;
        public Image stateImage;
        public Image stateFill;

        [Header("Muerte (asignar en el inspector)")]
        public Image deathOverlay;
        public TMP_Text deathText;

        [Header("Indicador")]
        [SerializeField] private Color calmColor = new Color(0.2f, 0.9f, 0.35f);
        [SerializeField] private Color quakeColor = new Color(0.95f, 0.15f, 0.1f);
        [SerializeField, Range(0f, 1f)] private float calmScale = 0.55f;
        [SerializeField, Range(0f, 1f)] private float minQuakeScale = 0.9f;
        [SerializeField, Range(0f, 1f)] private float maxQuakeScale = 1f;
        [SerializeField] private float scaleLerpSpeed = 6f;
        [SerializeField] private float colorLerpSpeed = 8f;
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField, Range(0f, 1f)] private float pulseAmount = 0.05f;

        [Header("Textos de estado")]
        [SerializeField] private string calmMessage = "Por ahora todo es calma";
        [SerializeField] private string quakeMessage = "TERREMOTO EN PROGRESO";

        [Header("Mensaje de muerte")]
        [SerializeField, TextArea(1, 2)] private string deathMessage = "HAZ MUERTO";

        public bool IsGameOver { get; private set; }
        public string DeathMessage => deathMessage;

        private EarthquakeManager manager;
        private PlayerController player;
        private RectTransform stateRect;
        private Vector3 stateBaseScale = Vector3.one;
        private bool hasStateBaseScale;

        private void Awake()
        {
            Instance = this;
            player = FindAnyObjectByType<PlayerController>();
            ResolveStateRect();

            if (deathOverlay != null)
            {
                deathOverlay.gameObject.SetActive(false);
            }

            if (deathText != null && string.IsNullOrEmpty(deathText.text))
            {
                deathText.text = deathMessage;
            }

            UpdateStateText();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void ConfigureUI(Image state, Image overlay, TMP_Text text)
        {
            stateImage = state;
            deathOverlay = overlay;
            deathText = text;
            ResolveStateRect();

            if (deathOverlay != null)
            {
                deathOverlay.gameObject.SetActive(false);
            }

            if (deathText != null && string.IsNullOrEmpty(deathText.text))
            {
                deathText.text = deathMessage;
            }
        }

        public void ConfigureStatusUI(TMP_Text text, Image fill)
        {
            stateText = text;
            stateFill = fill;
        }

        private void Update()
        {
            if (manager == null)
            {
                manager = EarthquakeManager.Instance;
            }

            ResolveStateRect();
            UpdateStateImage();
            UpdateStateText();
            UpdateStateFill();
        }

        private void UpdateStateImage()
        {
            if (stateImage == null)
            {
                return;
            }

            bool shaking = manager != null && manager.IsShaking;
            Color targetColor = shaking ? quakeColor : calmColor;
            float targetScale = Mathf.Clamp01(calmScale);

            if (shaking)
            {
                float total = Mathf.Max(0.01f, manager.CurrentDuration);
                float remaining = Mathf.Clamp01(manager.RemainingShakeDuration / total);
                float minFactor = Mathf.Clamp01(Mathf.Min(minQuakeScale, maxQuakeScale));
                float maxFactor = Mathf.Clamp01(Mathf.Max(minQuakeScale, maxQuakeScale));
                targetScale = Mathf.Lerp(minFactor, maxFactor, remaining);

                if (pulseAmount > 0f)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.Max(0.01f, pulseSpeed));
                    targetScale *= 1f - Mathf.Clamp01(pulseAmount) * pulse;
                }
            }

            targetScale = Mathf.Clamp01(targetScale);
            stateImage.color = Color.Lerp(stateImage.color, targetColor, Time.deltaTime * colorLerpSpeed);

            if (stateRect != null)
            {
                Vector3 targetLocalScale = stateBaseScale * targetScale;
                stateRect.localScale = Vector3.Lerp(
                    stateRect.localScale,
                    targetLocalScale,
                    Time.deltaTime * scaleLerpSpeed);
            }
        }

        private void UpdateStateText()
        {
            if (stateText == null)
            {
                return;
            }

            bool shaking = manager != null && manager.IsShaking;
            stateText.text = shaking ? quakeMessage : calmMessage;
            stateText.color = Color.Lerp(
                stateText.color,
                shaking ? quakeColor : calmColor,
                Time.deltaTime * colorLerpSpeed);
        }

        private void UpdateStateFill()
        {
            if (stateFill == null)
            {
                return;
            }

            bool shaking = manager != null && manager.IsShaking;
            float target = 1f;

            if (shaking)
            {
                float total = Mathf.Max(0.01f, manager.CurrentDuration);
                target = Mathf.Clamp01(manager.RemainingShakeDuration / total);
            }

            stateFill.fillAmount = Mathf.Lerp(stateFill.fillAmount, target, Time.deltaTime * colorLerpSpeed);
            stateFill.color = Color.Lerp(
                stateFill.color,
                shaking ? quakeColor : calmColor,
                Time.deltaTime * colorLerpSpeed);
        }

        public void ShowDeath()
        {
            ShowDeath(deathMessage);
        }

        public void ShowDeath(string message)
        {
            if (IsGameOver)
            {
                return;
            }

            IsGameOver = true;

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

            if (deathOverlay != null)
            {
                deathOverlay.color = Color.black;
                deathOverlay.raycastTarget = false;
                deathOverlay.gameObject.SetActive(true);
                deathOverlay.transform.SetAsLastSibling();
            }

            if (deathText != null)
            {
                deathText.text = string.IsNullOrEmpty(message) ? deathMessage : message;
                deathText.gameObject.SetActive(true);
                deathText.transform.SetAsLastSibling();
            }
        }

        public void TriggerDeath()
        {
            ShowDeath(deathMessage);
        }

        private void ResolveStateRect()
        {
            if (stateImage == null)
            {
                stateRect = null;
                hasStateBaseScale = false;
                return;
            }

            RectTransform rect = stateImage.rectTransform;

            if (rect == stateRect && hasStateBaseScale)
            {
                return;
            }

            stateRect = rect;
            stateBaseScale = rect.localScale;
            hasStateBaseScale = true;
        }
    }
}
