using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GroundRumble
{
    /// <summary>
    /// HUD superior con aviso, intensidad y controles. La cuenta regresiva usa TextMeshPro.
    /// Las referencias viven en el Canvas de la escena y son editables desde el inspector.
    /// </summary>
    public class EarthquakeHUD : MonoBehaviour
    {
        [SerializeField] private Text statusText;
        [SerializeField] private Text detailsText;
        [SerializeField] private Image intensityFill;
        [SerializeField] private TMP_Text countdownText;

        private EarthquakeManager manager;

        private void Awake()
        {
            if (countdownText == null)
            {
                Transform countdown = transform.Find("Texto_Cuenta_Terremoto");

                if (countdown != null)
                {
                    countdownText = countdown.GetComponent<TMP_Text>();
                }
            }

            if (statusText == null || detailsText == null)
            {
                BuildUI();
            }
        }

        public void ConfigureUI(Text status, Text details, Image intensity, TMP_Text countdown)
        {
            statusText = status;
            detailsText = details;
            intensityFill = intensity;
            countdownText = countdown;
        }

        private void Update()
        {
            if (manager == null)
            {
                manager = EarthquakeManager.Instance;
            }

            if (manager == null)
            {
                if (statusText != null)
                {
                    statusText.text = "Sin EarthquakeManager en la escena";
                }

                return;
            }

            HandleAdjustmentKeys();
            UpdateTexts();
            UpdateIntensityBar();
        }

        private void HandleAdjustmentKeys()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            if (keyboard[Key.LeftBracket].wasPressedThisFrame)
            {
                manager.Intensity -= 0.05f;
            }

            if (keyboard[Key.RightBracket].wasPressedThisFrame)
            {
                manager.Intensity += 0.05f;
            }

            if (keyboard[Key.T].wasPressedThisFrame)
            {
                manager.TriggerEarthquakeNow();
            }
        }

        private void UpdateTexts()
        {
            if (statusText != null)
            {
                if (manager.IsShaking || manager.IsWarning)
                {
                    statusText.text = string.Empty;
                }
                else
                {
                    statusText.text = "Sin actividad sismica";
                    statusText.color = new Color(0.85f, 0.9f, 1f);
                }
            }

            if (countdownText != null)
            {
                if (manager.IsWarning)
                {
                    // Cuenta regresiva entera; nunca se muestra el tiempo restante del sismo.
                    countdownText.text = $"TERREMOTO ESCAPA\n{manager.WarningSecondsLeft}";
                    countdownText.color = new Color(1f, 0.82f, 0.2f);
                }
                else if (manager.IsShaking)
                {
                    countdownText.text = "TERREMOTO ESCAPA";
                    countdownText.color = new Color(1f, 0.35f, 0.25f);
                }
                else
                {
                    countdownText.text = string.Empty;
                }
            }

            if (detailsText != null)
            {
                detailsText.text =
                    $"Intensidad: {manager.Intensity:0.00}  [ / ]     |     " +
                    "Las columnas verdes te protegen";
            }

        }

        private void UpdateIntensityBar()
        {
            if (intensityFill == null)
            {
                return;
            }

            float intensity = manager.IsShaking || manager.IsWarning ? manager.Intensity : manager.Intensity * 0.35f;
            intensityFill.fillAmount = Mathf.Clamp01(intensity);
            intensityFill.color = Color.Lerp(new Color(1f, 0.85f, 0.2f), new Color(1f, 0.2f, 0.15f), intensity);
        }

        private void BuildUI()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            GameObject canvasObject = new GameObject("Earthquake_HUD");
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            countdownText = CreateCountdownText(canvasObject.transform);

            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasObject.transform, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -16f);
            panelRect.sizeDelta = new Vector2(-32f, 150f);

            Image panelImage = panel.GetComponent<Image>();
            panelImage.sprite = UiSpriteFactory.WhiteSprite;
            panelImage.color = new Color(0.03f, 0.035f, 0.05f, 0.78f);

            statusText = CreateText(panel.transform, "Texto_Estado", -8f, 42f, 30, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.9f, 0.95f, 1f), font);
            detailsText = CreateText(panel.transform, "Texto_Detalles", -52f, 26f, 19, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.78f, 0.82f, 0.9f), font);

            GameObject barBackground = new GameObject("Barra_Terremoto_Fondo", typeof(RectTransform), typeof(Image));
            barBackground.transform.SetParent(panel.transform, false);

            RectTransform barRect = barBackground.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(1f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.anchoredPosition = new Vector2(0f, 10f);
            barRect.sizeDelta = new Vector2(-24f, 8f);

            Image barImage = barBackground.GetComponent<Image>();
            barImage.sprite = UiSpriteFactory.WhiteSprite;
            barImage.color = new Color(1f, 1f, 1f, 0.12f);

            GameObject fill = new GameObject("Barra_Terremoto_Relleno", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(barBackground.transform, false);

            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            intensityFill = fill.GetComponent<Image>();
            intensityFill.sprite = UiSpriteFactory.WhiteSprite;
            intensityFill.type = Image.Type.Filled;
            intensityFill.fillMethod = Image.FillMethod.Horizontal;
            intensityFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            intensityFill.color = new Color(1f, 0.75f, 0.2f);
        }

        private TMP_Text CreateCountdownText(Transform parent)
        {
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            if (font == null)
            {
                font = TMP_Settings.defaultFontAsset;
            }

            if (font == null)
            {
                return null;
            }

            GameObject textObject = new GameObject("Texto_Cuenta_Terremoto", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 160f);
            rect.sizeDelta = new Vector2(1200f, 260f);

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = 64f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.35f, 0.25f);
            text.raycastTarget = false;
            text.text = string.Empty;

            return text;
        }

        private Text CreateText(Transform parent, string name, float y, float height, int fontSize, FontStyle style, TextAnchor alignment, Color color, Font font)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(-24f, height);

            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            Shadow shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(1f, -1f);

            return text;
        }
    }
}
