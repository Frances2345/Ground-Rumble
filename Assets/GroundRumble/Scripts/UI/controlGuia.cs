using TMPro;
using UnityEngine;

namespace GroundRumble
{
    public class controlGuia : MonoBehaviour
    {
        public TMP_Text guideText;

        [TextArea(2, 6)]
        [SerializeField] private string defaultGuide =
            "OBJETIVO: llega a la salida antes de que se acabe el tiempo.\n" +
            "RUTA SEGURA: las columnas del centro forman un pasillo protector; cruza por ahi de escalera en escalera.\n" +
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
            if (guideText == null)
            {
                return;
            }

            // Las escenas traen texto de controles hardcodeado que no incluye el objetivo.
            if (string.IsNullOrWhiteSpace(guideText.text) || !guideText.text.Contains("OBJETIVO"))
            {
                guideText.text = defaultGuide;
            }
        }
    }
}
