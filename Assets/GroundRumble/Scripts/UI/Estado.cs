using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GroundRumble
{
    public class Estado : MonoBehaviour
    {
        public Image stateImage;
        public Image deathOverlay;
        public TMP_Text deathText;

        [Header("Indicador")]
        [SerializeField] private Color calmColor = new Color(0.2f, 0.9f, 0.35f);
        [SerializeField] private Color quakeColor = new Color(0.95f, 0.15f, 0.1f);
        [SerializeField] private float calmScale = 0.55f;
        [SerializeField] private float minQuakeScale = 0.9f;
        [SerializeField] private float maxQuakeScale = 1.6f;
        [SerializeField] private float scaleLerpSpeed = 6f;
        [SerializeField] private float colorLerpSpeed = 8f;

        [Header("Temporizador")]
        [SerializeField] private float gameDuration = 300f;

        [Header("Muerte")]
        [SerializeField, TextArea(1, 2)] private string deathMessage = "HAZ MUERTO";

        public bool IsGameOver { get; private set; }
        public float GameDuration => gameDuration;
        public float TimeLeft => timeLeft;

        private EarthquakeManager manager;
        private PlayerController player;
        private RectTransform stateRect;
        private float timeLeft;

        private void Awake()
        {
            timeLeft = Mathf.Max(0f, gameDuration);
            ResolveReferences();
            EnsureDeathUI();

            if (deathOverlay != null)
            {
                deathOverlay.gameObject.SetActive(false);
            }

            if (deathText != null && string.IsNullOrEmpty(deathText.text))
            {
                deathText.text = deathMessage;
            }
        }

        public void ConfigureUI(Image state, Image overlay, TMP_Text text)
        {
            stateImage = state;
            deathOverlay = overlay;
            deathText = text;
            ResolveStateRect();

            if (deathText != null && string.IsNullOrEmpty(deathText.text))
            {
                deathText.text = deathMessage;
            }
        }

        private void Update()
        {
            if (manager == null)
            {
                manager = EarthquakeManager.Instance;
            }

            UpdateStateImage();

            if (IsGameOver || gameDuration <= 0f)
            {
                return;
            }

            timeLeft -= Time.deltaTime;

            if (timeLeft <= 0f)
            {
                timeLeft = 0f;
                TriggerDeath();
            }
        }

        private void UpdateStateImage()
        {
            if (stateImage == null)
            {
                return;
            }

            ResolveStateRect();

            bool shaking = manager != null && manager.IsShaking;
            Color targetColor = shaking ? quakeColor : calmColor;
            float targetScale = calmScale;

            if (shaking)
            {
                float total = Mathf.Max(0.01f, manager.CurrentDuration);
                float remaining = Mathf.Clamp01(manager.RemainingShakeDuration / total);
                targetScale = Mathf.Lerp(minQuakeScale, maxQuakeScale, remaining);
            }

            stateImage.color = Color.Lerp(stateImage.color, targetColor, Time.deltaTime * colorLerpSpeed);

            if (stateRect != null)
            {
                stateRect.localScale = Vector3.Lerp(
                    stateRect.localScale,
                    Vector3.one * targetScale,
                    Time.deltaTime * scaleLerpSpeed);
            }
        }

        public void TriggerDeath()
        {
            if (IsGameOver)
            {
                return;
            }

            IsGameOver = true;
            timeLeft = 0f;

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
                if (string.IsNullOrEmpty(deathText.text))
                {
                    deathText.text = deathMessage;
                }

                deathText.gameObject.SetActive(true);
                deathText.transform.SetAsLastSibling();
            }
        }

        private void ResolveReferences()
        {
            if (player == null)
            {
                player = FindAnyObjectByType<PlayerController>();
            }

            if (stateImage == null)
            {
                Transform found = transform.Find("Indicador_Estado");

                if (found != null)
                {
                    stateImage = found.GetComponent<Image>();
                }
            }

            if (deathOverlay == null)
            {
                Transform found = transform.Find("Pantalla_Muerte");

                if (found != null)
                {
                    deathOverlay = found.GetComponent<Image>();
                }
            }

            if (deathText == null && deathOverlay != null)
            {
                deathText = deathOverlay.GetComponentInChildren<TMP_Text>(true);
            }

            ResolveStateRect();
        }

        private void ResolveStateRect()
        {
            if (stateImage != null)
            {
                stateRect = stateImage.rectTransform;
            }
        }

        private void EnsureDeathUI()
        {
            Transform parent = transform;

            if (deathOverlay == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();

                if (canvas != null)
                {
                    parent = canvas.transform;
                }

                GameObject overlayObject = new GameObject("Pantalla_Muerte", typeof(RectTransform), typeof(Image));
                overlayObject.transform.SetParent(parent, false);

                RectTransform rect = overlayObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                deathOverlay = overlayObject.GetComponent<Image>();
                deathOverlay.sprite = UiSpriteFactory.WhiteSprite;
                deathOverlay.color = Color.black;
                deathOverlay.raycastTarget = false;
            }

            if (deathText == null)
            {
                GameObject textObject = new GameObject("Texto_Muerte", typeof(RectTransform));
                textObject.transform.SetParent(deathOverlay.transform, false);

                RectTransform rect = textObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
                text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

                if (text.font == null)
                {
                    text.font = TMP_Settings.defaultFontAsset;
                }

                text.fontSize = 96f;
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.white;
                text.raycastTarget = false;
                text.text = deathMessage;
                deathText = text;
            }
        }
    }
}
