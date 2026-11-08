using System.Collections.Generic;
using UnityEngine;

namespace GroundRumble
{
    /// <summary>
    /// Zona segura alrededor de una columna. Mientras el jugador este dentro
    /// de su radio no recibe dano de los escombros.
    /// </summary>
    public class SafeZone : MonoBehaviour
    {
        private static readonly List<SafeZone> zones = new List<SafeZone>();

        [SerializeField] private float safeRadius = 1.4f;
        [SerializeField] private float heightTolerance = 2.5f;

        public float SafeRadius => safeRadius;
        public float HeightTolerance => heightTolerance;

        public void Configure(float radius, float verticalTolerance)
        {
            safeRadius = Mathf.Max(0.1f, radius);
            heightTolerance = Mathf.Max(0.5f, verticalTolerance);
        }

        private void OnEnable()
        {
            if (!zones.Contains(this))
            {
                zones.Add(this);
            }
        }

        private void OnDisable()
        {
            zones.Remove(this);
        }

        public static bool IsPositionSafe(Vector3 position)
        {
            for (int i = zones.Count - 1; i >= 0; i--)
            {
                SafeZone zone = zones[i];

                if (zone == null)
                {
                    zones.RemoveAt(i);
                    continue;
                }

                Vector3 delta = zone.transform.position - position;

                if (Mathf.Abs(delta.y) > zone.heightTolerance)
                {
                    continue;
                }

                delta.y = 0f;

                if (delta.sqrMagnitude <= zone.safeRadius * zone.safeRadius)
                {
                    return true;
                }
            }

            return false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.45f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, safeRadius);
        }
#endif
    }
}
