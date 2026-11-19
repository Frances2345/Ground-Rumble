using UnityEngine;

namespace GroundRumble
{
    /// <summary>
    /// Mini seccion de un piso rompible. Cada seccion conoce su area y si puede
    /// romperse (las que estan demasiado cerca de una columna no se rompen).
    /// </summary>
    public class BreakableFloorSection : MonoBehaviour
    {
        private Bounds bounds;
        private bool canBreak = true;
        private bool broken;
        private BreakableFloorSection linkedSection;

        public bool CanBreak => canBreak && !broken;
        public bool IsBroken => broken;

        public void Configure(Bounds worldBounds, bool breakable)
        {
            bounds = worldBounds;
            canBreak = breakable;
            broken = false;
        }

        public void SetLinkedSection(BreakableFloorSection section)
        {
            linkedSection = section;
        }

        public bool ContainsPlayer(Vector3 position)
        {
            if (broken)
            {
                return false;
            }

            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            if (position.x < min.x - 0.25f || position.x > max.x + 0.25f)
            {
                return false;
            }

            if (position.z < min.z - 0.25f || position.z > max.z + 0.25f)
            {
                return false;
            }

            return position.y >= max.y - 0.6f && position.y <= max.y + 1.8f;
        }

        public void BreakOpen()
        {
            if (broken)
            {
                return;
            }

            broken = true;

            if (linkedSection != null)
            {
                linkedSection.BreakOpen();
            }

            Collider sectionCollider = GetComponent<Collider>();

            if (sectionCollider != null)
            {
                sectionCollider.enabled = false;
            }

            Renderer sectionRenderer = GetComponent<Renderer>();

            if (sectionRenderer != null)
            {
                sectionRenderer.enabled = false;
            }
        }
    }
}
