using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GroundRumble
{
    /// <summary>
    /// Construye el menu de opciones del terremoto en la escena Opciones.
    /// Permite configurar la magnitud real (escala de Richter) y muestra cuantos
    /// escombros generara cada piso.
    /// </summary>
    public class EarthquakeOptionsMenu : MonoBehaviour
    {
        private static readonly Color Accent = new Color(0.32f, 0.68f, 1f);
        private static readonly Color PanelColor = new Color(0.06f, 0.08f, 0.13f, 0.97f);
        private static readonly Color TextColor = new Color(0.92f, 0.95f, 1f);
        private static readonly Color DimColor = new Color(0.72f, 0.78f, 0.88f);

        private TMP_FontAsset font;
        private Slider magnitudeSlider;
        private TMP_Text magnitudeLabel;
        private TMP_Text debrisLabel;
        private TMP_Text effectsLabel;

        private void Start()
        {
            font = ResolveFont();
            BuildInterface();
            ApplySavedValue();
        }

        private static TMP_FontAsset ResolveFont()
        {
            if (TMP_Settings.defaultFontAsset != null)
            {
                return TMP_Settings.defaultFontAsset;
            }

            return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        private void BuildInterface()
        {
            RectTransform canvasRect = transform as RectTransform;

            if (canvasRect == null)
            {
                return;
            }

            Canvas canvas = GetComponent<Canvas>();

            if (canvas != null)
            {
                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;

                CanvasScaler scaler = GetComponent<CanvasScaler>();

                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920f, 1080f);
                    scaler.matchWidthOrHeight = 0.5f;
                }
            }

            Image backdrop = CreateImage("FondoOpciones", canvasRect, new Color(0.02f, 0.03f, 0.06f, 0.9f));
            Stretch(backdrop.rectTransform);

            Image panel = CreateImage("PanelConfiguracion", canvasRect, PanelColor);
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(820f, 640f);

            CreateTopLabel("Titulo", panelRect, "CONFIGURACIÓN DEL TERREMOTO", 40f, TextAlignmentOptions.Center, TextColor, -60f, 64f);
            CreateTopLabel("Subtitulo", panelRect, "Magnitud en escala de Richter (1.0 - 9.0)", 22f, TextAlignmentOptions.Center, DimColor, -128f, 40f);

            magnitudeLabel = CreateTopLabel("ValorMagnitud", panelRect, string.Empty, 34f, TextAlignmentOptions.Center, Accent, -206f, 54f);

            magnitudeSlider = CreateSlider(panelRect, new Vector2(0f, -284f), new Vector2(660f, 42f));
            magnitudeSlider.onValueChanged.AddListener(OnMagnitudeChanged);

            debrisLabel = CreateTopLabel("ValorEscombros", panelRect, string.Empty, 24f, TextAlignmentOptions.Center, TextColor, -350f, 40f);

            effectsLabel = CreateTopLabel("Descripcion", panelRect, string.Empty, 20f, TextAlignmentOptions.Center, DimColor, -402f, 92f);
            effectsLabel.textWrappingMode = TextWrappingModes.Normal;

            CreateButton("Volver", panelRect, new Vector2(0f, 52f), "Volver al menú");
        }

        private void ApplySavedValue()
        {
            if (magnitudeSlider == null)
            {
                return;
            }

            magnitudeSlider.SetValueWithoutNotify(GameSettings.EarthquakeMagnitude);
            RefreshLabels();
        }

        private void OnMagnitudeChanged(float value)
        {
            GameSettings.EarthquakeMagnitude = value;
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            float magnitude = GameSettings.EarthquakeMagnitude;

            if (magnitudeLabel != null)
            {
                magnitudeLabel.text = $"Magnitud: {magnitude:0.0}  ·  {GameSettings.DescribeMagnitude(magnitude)}";
            }

            if (debrisLabel != null)
            {
                int debris = GameSettings.GetDebrisCount(10);
                debrisLabel.text = $"Escombros por piso: {debris}";
            }

            if (effectsLabel != null)
            {
                effectsLabel.text =
                    "A mayor magnitud, más escombros, más vidrios rotos y una sacudida más fuerte.\n" +
                    "El temblor no te quita vida por sí solo, pero los escombros y los añicos te dañan al golpearte.\n" +
                    "Si te resbalas, quedas aturdido unos segundos y pierdes vida.";
            }
        }

        private Slider CreateSlider(Transform parent, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject sliderObject = new GameObject("SliderMagnitud", typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(parent, false);

            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 1f);
            sliderRect.anchorMax = new Vector2(0.5f, 1f);
            sliderRect.pivot = new Vector2(0.5f, 1f);
            sliderRect.anchoredPosition = anchoredPosition;
            sliderRect.sizeDelta = size;

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = GameSettings.MinMagnitude;
            slider.maxValue = GameSettings.MaxMagnitude;
            slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;

            Image background = CreateImage("Background", sliderRect, new Color(0.16f, 0.19f, 0.26f, 1f));
            background.raycastTarget = true;
            RectTransform backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = new Vector2(0f, 0.32f);
            backgroundRect.anchorMax = new Vector2(1f, 0.68f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderRect, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.32f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.68f);
            fillAreaRect.offsetMin = new Vector2(6f, 0f);
            fillAreaRect.offsetMax = new Vector2(-12f, 0f);

            Image fill = CreateImage("Fill", fillAreaRect, Accent);
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderRect, false);
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = new Vector2(0f, 0f);
            handleAreaRect.anchorMax = new Vector2(1f, 1f);
            handleAreaRect.offsetMin = new Vector2(12f, 0f);
            handleAreaRect.offsetMax = new Vector2(-12f, 0f);

            Image handle = CreateImage("Handle", handleAreaRect, TextColor);
            handle.raycastTarget = true;
            RectTransform handleRect = handle.rectTransform;
            handleRect.anchorMin = new Vector2(0f, 0.12f);
            handleRect.anchorMax = new Vector2(0f, 0.88f);
            handleRect.sizeDelta = new Vector2(24f, 0f);
            handleRect.anchoredPosition = Vector2.zero;

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle;

            return slider;
        }

        private void CreateButton(string name, Transform parent, Vector2 anchoredPosition, string text)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(320f, 58f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = Accent;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => SceneManager.LoadScene("Menu"));

            TextMeshProUGUI label = CreateText("Texto", rect, text, 26f, TextAlignmentOptions.Center, new Color(0.04f, 0.06f, 0.1f));
            Stretch(label.rectTransform);
        }

        private TMP_Text CreateTopLabel(string name, Transform parent, string text, float size, TextAlignmentOptions alignment, Color color, float y, float height)
        {
            TextMeshProUGUI label = CreateText(name, parent, text, size, alignment, color);
            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(760f, height);
            return label;
        }

        private TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, TextAlignmentOptions alignment, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;

            return label;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
