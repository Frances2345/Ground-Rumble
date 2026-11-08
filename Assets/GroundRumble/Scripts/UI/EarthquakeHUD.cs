using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GroundRumble
{
    /// <summary>
    /// HUD superior con aviso, duracion, intensidad y controles.
    /// Se construye automaticamente al iniciar si no hay referencias asignadas.
    /// </summary>
    public class EarthquakeHUD : MonoBehaviour
    {
        [SerializeField] private Text statusText;
        [SerializeField] private Text detailsText;
        [SerializeField] private Text controlsText;
        [SerializeField] private Image intensityFill;

        private EarthquakeManager manager;
        private Font font;

        private void Awake()
        {
            if (statusText == null || detailsText == null || controlsText == null)
            {
                BuildUI();
            }
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

            if (keyboard[Key.Minus].wasPressedThisFrame)
            {
                manager.EarthquakeDuration -= 1f;
            }

            if (keyboard[Key.Equals].wasPressedThisFrame)
            {
                manager.EarthquakeDuration += 1f;
            }

            if (keyboard[Key.T].wasPressedThisFrame)
            {
                manager.TriggerEarthquakeNow();
            }
        }

        private void UpdateTexts()
        {
            if (manager.IsShaking)
            {
                statusText.text = $"TERREMOTO   |   Restante: {manager.RemainingDuration:0.0} s";
                statusText.color = new Color(1f, 0.35f, 0.25f);
            }
            else if (manager.IsWarning)
            {
                statusText.text = $"ADVERTENCIA   |   Comienza en: {manager.TimeUntilEarthquake:0.0} s";
                statusText.color = new Color(1f, 0.82f, 0.2f);
            }
            else
            {
                statusText.text = "Sin actividad sismica";
                statusText.color = new Color(0.85f, 0.9f, 1f);
            }

            detailsText.text =
                $"Intensidad: {manager.Intensity:0.00}  [ / ]     " +
                $"Duracion: {manager.EarthquakeDuration:0.0} s  [ - / = ]     " +
                $"Aviso: {manager.WarningDuration:0.0} s";

            controlsText.text =
                "WASD mover   |   Shift correr   |   Ctrl agacharse   |   Espacio saltar   |   1 / 2 subir o bajar piso   |   T iniciar terremoto";
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
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

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

            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasObject.transform, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -16f);
            panelRect.sizeDelta = new Vector2(-32f, 136f);

            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.03f, 0.035f, 0.05f, 0.78f);

            statusText = CreateText(panel.transform, "Estado", -8f, 46f, 32, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.9f, 0.95f, 1f));
            detailsText = CreateText(panel.transform, "Detalles", -54f, 28f, 19, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.78f, 0.82f, 0.9f));
            controlsText = CreateText(panel.transform, "Controles", -86f, 24f, 15, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.62f, 0.66f, 0.74f));

            GameObject barBackground = new GameObject("BarraFondo", typeof(RectTransform), typeof(Image));
            barBackground.transform.SetParent(panel.transform, false);

            RectTransform barRect = barBackground.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(1f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.anchoredPosition = new Vector2(0f, 6f);
            barRect.sizeDelta = new Vector2(-24f, 8f);

            barBackground.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);

            GameObject fill = new GameObject("BarraIntensidad", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(barBackground.transform, false);

            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            intensityFill = fill.GetComponent<Image>();
            intensityFill.type = Image.Type.Filled;
            intensityFill.fillMethod = Image.FillMethod.Horizontal;
            intensityFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            intensityFill.color = new Color(1f, 0.75f, 0.2f);
        }

        private Text CreateText(Transform parent, string name, float y, float height, int fontSize, FontStyle style, TextAnchor alignment, Color color)
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
