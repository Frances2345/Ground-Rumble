using TMPro;
using UnityEngine;

namespace GroundRumble
{
    public class controlGuia : MonoBehaviour
    {
        public TMP_Text guideText;

        [TextArea(2, 4)]
        [SerializeField] private string defaultGuide =
            "WASD mover   |   Shift correr   |   Ctrl agacharse   |   Espacio saltar   |   " +
            "1 / 2 subir o bajar piso   |   T iniciar terremoto";

        private void Awake()
        {
            ApplyGuide();
        }

        public void ConfigureUI(TMP_Text text)
        {
            guideText = text;
            ApplyGuide();
        }

        private void ApplyGuide()
        {
            if (guideText != null && string.IsNullOrEmpty(guideText.text))
            {
                guideText.text = defaultGuide;
            }
        }
    }
}
