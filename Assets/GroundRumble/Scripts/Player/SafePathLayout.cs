using System.Collections.Generic;
using UnityEngine;

namespace GroundRumble
{
    /// <summary>
    /// Distribucion de columnas por TODO el piso: bordes, esquinas e interior,
    /// con una forma aleatoria pero reproducible (misma semilla por piso).
    /// Se dejan libres las dos esquinas donde estan las escaleras de emergencia.
    /// </summary>
    public static class SafePathLayout
    {
        private const int ColumnCount = 18;
        private const float MinSeparation = 3.1f;
        private const int SeedBase = 1337;
        private const int SeedStep = 7919;

        public static Vector2[] GetPositions(int floorNumber, float width, float depth)
        {
            var rng = new System.Random(SeedBase + floorNumber * SeedStep);
            var positions = new List<Vector2>(ColumnCount);

            // Margen desde la pared para que la columna no se corte con el muro.
            float halfW = Mathf.Max(2f, width * 0.5f - 2.4f);
            float halfD = Mathf.Max(2f, depth * 0.5f - 2.4f);

            // Esquinas de las escaleras: se dejan despejadas.
            Vector2 eastLanding = new Vector2(width * 0.5f - 3.2f, -depth * 0.5f + 3.4f);
            Vector2 westLanding = new Vector2(-width * 0.5f + 3.2f, depth * 0.5f - 3.4f);

            int attempts = 0;

            while (positions.Count < ColumnCount && attempts < 3000)
            {
                attempts++;

                float x;
                float z;

                // La mayoria cae cerca de los limites del piso, el resto al interior.
                if (rng.NextDouble() < 0.62)
                {
                    switch (rng.Next(4))
                    {
                        case 0:
                            x = Range(rng, -halfW, halfW);
                            z = halfD;
                            break;
                        case 1:
                            x = Range(rng, -halfW, halfW);
                            z = -halfD;
                            break;
                        case 2:
                            x = halfW;
                            z = Range(rng, -halfD, halfD);
                            break;
                        default:
                            x = -halfW;
                            z = Range(rng, -halfD, halfD);
                            break;
                    }
                }
                else
                {
                    x = Range(rng, -halfW, halfW);
                    z = Range(rng, -halfD, halfD);
                }

                x = Mathf.Clamp(x + Range(rng, -0.9f, 0.9f), -halfW, halfW);
                z = Mathf.Clamp(z + Range(rng, -0.9f, 0.9f), -halfD, halfD);

                Vector2 candidate = new Vector2(x, z);

                if ((candidate - eastLanding).sqrMagnitude < 12.25f ||
                    (candidate - westLanding).sqrMagnitude < 12.25f)
                {
                    continue;
                }

                bool tooClose = false;

                foreach (Vector2 existing in positions)
                {
                    if ((candidate - existing).sqrMagnitude < MinSeparation * MinSeparation)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    positions.Add(candidate);
                }
            }

            return positions.ToArray();
        }

        private static float Range(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
