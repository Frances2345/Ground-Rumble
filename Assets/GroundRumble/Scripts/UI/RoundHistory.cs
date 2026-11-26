using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;

namespace GroundRumble
{
    [Serializable]
    public class RoundRecord
    {
        public float magnitude;
        public float remainingSeconds;
        public string playedAt;
    }

    [Serializable]
    public class RoundRecordList
    {
        public List<RoundRecord> rounds = new List<RoundRecord>();
    }

    /// <summary>
    /// Guarda las ultimas 5 partidas en PlayerPrefs. Cada registro conserva la
    /// magnitud elegida y el tiempo que le quedaba al contador al morir.
    /// </summary>
    public static class RoundRecords
    {
        public const int MaxRecords = 5;
        public const string EmptyMessage = "aun no se ha registrado una partida";

        private const string StorageKey = "GroundRumble.RoundRecords.v1";

        public static void AddRound(float magnitude, float remainingSeconds)
        {
            List<RoundRecord> rounds = Load();

            rounds.Add(new RoundRecord
            {
                magnitude = Mathf.Clamp(magnitude, GameSettings.MinMagnitude, GameSettings.MaxMagnitude),
                remainingSeconds = Mathf.Max(0f, remainingSeconds),
                playedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            });

            SortByRecent(rounds);
            Trim(rounds);
            Save(rounds);
        }

        public static List<RoundRecord> GetRecent()
        {
            List<RoundRecord> rounds = Load();
            SortByRecent(rounds);
            return rounds;
        }

        public static List<RoundRecord> GetBest()
        {
            List<RoundRecord> rounds = Load();
            rounds.Sort((a, b) =>
            {
                int byTime = b.remainingSeconds.CompareTo(a.remainingSeconds);

                return byTime != 0 ? byTime : CompareDates(b, a);
            });

            return rounds;
        }

        public static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.RoundToInt(seconds));

            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        public static string FormatLine(RoundRecord record)
        {
            return $"Magnitud {record.magnitude:0.0} ({FormatTime(record.remainingSeconds)})";
        }

        private static void SortByRecent(List<RoundRecord> rounds)
        {
            rounds.Sort((a, b) => CompareDates(b, a));
        }

        private static int CompareDates(RoundRecord a, RoundRecord b)
        {
            string first = a.playedAt ?? string.Empty;
            string second = b.playedAt ?? string.Empty;

            return string.CompareOrdinal(first, second);
        }

        private static void Trim(List<RoundRecord> rounds)
        {
            if (rounds.Count > MaxRecords)
            {
                rounds.RemoveRange(MaxRecords, rounds.Count - MaxRecords);
            }
        }

        private static List<RoundRecord> Load()
        {
            string json = PlayerPrefs.GetString(StorageKey, string.Empty);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<RoundRecord>();
            }

            try
            {
                RoundRecordList data = JsonUtility.FromJson<RoundRecordList>(json);

                if (data == null || data.rounds == null)
                {
                    return new List<RoundRecord>();
                }

                List<RoundRecord> rounds = data.rounds.FindAll(record => record != null);
                Trim(rounds);

                return rounds;
            }
            catch (Exception)
            {
                return new List<RoundRecord>();
            }
        }

        private static void Save(List<RoundRecord> rounds)
        {
            RoundRecordList data = new RoundRecordList { rounds = rounds };
            PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Muestra las 5 partidas mas recientes. Arrastra aqui un TextMeshProUGUI.
    /// </summary>
    public class RoundHistory : MonoBehaviour
    {
        [Header("UI (arrastrar TextMeshProUGUI)")]
        public TMP_Text historyText;

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
            if (historyText == null)
            {
                return;
            }

            List<RoundRecord> rounds = RoundRecords.GetRecent();

            if (rounds.Count == 0)
            {
                historyText.text = RoundRecords.EmptyMessage;
                return;
            }

            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < rounds.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(RoundRecords.FormatLine(rounds[i]));
            }

            historyText.text = builder.ToString();
        }
    }
}
