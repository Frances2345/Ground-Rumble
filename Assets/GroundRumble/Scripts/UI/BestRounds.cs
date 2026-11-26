using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace GroundRumble
{
    /// <summary>
    /// Muestra el ranking de partidas ordenado por el tiempo que le quedo al
    /// contador al morir (de mayor a menor). Arrastra aqui un TextMeshProUGUI.
    /// </summary>
    public class BestRounds : MonoBehaviour
    {
        [Header("UI (arrastrar TextMeshProUGUI)")]
        public TMP_Text bestRoundsText;

        [Header("Formato")]
        [SerializeField] private bool showRankNumbers = true;

        private void OnEnable()
        {
            Refresh();
        }

        private void Start()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (bestRoundsText == null)
            {
                return;
            }

            List<RoundRecord> rounds = RoundRecords.GetBest();

            if (rounds.Count == 0)
            {
                bestRoundsText.text = RoundRecords.EmptyMessage;
                return;
            }

            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < rounds.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append('\n');
                }

                if (showRankNumbers)
                {
                    builder.Append(i + 1).Append(". ");
                }

                builder.Append(RoundRecords.FormatLine(rounds[i]));
            }

            bestRoundsText.text = builder.ToString();
        }
    }
}
