using GroundRumble.Events;
using UnityEngine;

namespace GroundRumble.UI
{
    public class EarthquakeHUD : MonoBehaviour
    {
        [SerializeField] private int fontSize = 28;
        [SerializeField] private float topMargin = 16f;

        private EarthquakeSystem earthquake;
        private GUIStyle labelStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<EarthquakeHUD>() != null)
            {
                return;
            }

            GameObject hud = new GameObject("EarthquakeHUD");
            hud.AddComponent<EarthquakeHUD>();
        }

        private void OnGUI()
        {
            if (earthquake == null)
            {
                earthquake = EarthquakeSystem.Current;
            }

            if (earthquake == null)
            {
                return;
            }

            string text;
            Color color;

            if (earthquake.IsShaking)
            {
                text = "TERREMOTO  " + FormatTime(earthquake.ShakeTimeRemaining);
                color = new Color(1f, 0.32f, 0.28f);
            }
            else if (earthquake.SecondsUntilShake > 0.01f)
            {
                text = "Terremoto en  " + FormatTime(earthquake.SecondsUntilShake);
                color = new Color(1f, 0.82f, 0.25f);
            }
            else
            {
                return;
            }

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
            }

            labelStyle.fontSize = fontSize;
            labelStyle.normal.textColor = color;

            float width = 460f;
            float height = fontSize + 30f;
            Rect rect = new Rect((Screen.width - width) * 0.5f, topMargin, width, height);

            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;

            GUI.Label(rect, text, labelStyle);
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
            int minutes = total / 60;
            int secs = total % 60;
            return $"{minutes:00}:{secs:00}";
        }
    }
}
