using UnityEngine;

namespace GroundRumble
{
    /// <summary>
    /// Ajustes persistentes del juego. La magnitud del terremoto usa la escala
    /// real de Richter (1.0 - 9.0) y se guarda entre escenas con PlayerPrefs.
    /// </summary>
    public static class GameSettings
    {
        public const float MinMagnitude = 1f;
        public const float MaxMagnitude = 9f;
        public const float DefaultMagnitude = 5.5f;
        public const float FloorHeight = 3.96f;

        private const string MagnitudeKey = "GroundRumble.EarthquakeMagnitude";

        private static float? cachedMagnitude;

        public static float EarthquakeMagnitude
        {
            get
            {
                if (!cachedMagnitude.HasValue)
                {
                    cachedMagnitude = Mathf.Clamp(
                        PlayerPrefs.GetFloat(MagnitudeKey, DefaultMagnitude),
                        MinMagnitude,
                        MaxMagnitude);
                }

                return cachedMagnitude.Value;
            }
            set
            {
                cachedMagnitude = Mathf.Clamp(value, MinMagnitude, MaxMagnitude);
                PlayerPrefs.SetFloat(MagnitudeKey, cachedMagnitude.Value);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Intensidad normalizada (0 - 1) que consumen el terremoto, la camara y los escombros.
        /// </summary>
        public static float NormalizedIntensity =>
            Mathf.InverseLerp(MinMagnitude, MaxMagnitude, EarthquakeMagnitude);

        public static int GetDebrisCount(int baseCount)
        {
            return GetDebrisCount(baseCount, NormalizedIntensity);
        }

        /// <summary>
        /// Convierte la intensidad en cantidad de escombros. A magnitud minima cae
        /// la mitad del valor base y a magnitud maxima el triple y medio.
        /// </summary>
        public static int GetDebrisCount(int baseCount, float intensity)
        {
            float t = Mathf.Clamp01(intensity);
            float multiplier = Mathf.Lerp(0.5f, 3.5f, t);
            return Mathf.Max(0, Mathf.RoundToInt(Mathf.Max(0, baseCount) * multiplier));
        }

        public static string DescribeMagnitude(float magnitude)
        {
            if (magnitude < 3f) return "Leve";
            if (magnitude < 4f) return "Menor";
            if (magnitude < 5f) return "Moderado";
            if (magnitude < 6f) return "Fuerte";
            if (magnitude < 7f) return "Muy fuerte";
            if (magnitude < 8f) return "Mayor";
            return "Destructivo";
        }
    }
}
