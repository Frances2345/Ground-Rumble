using UnityEngine;

namespace GroundRumble.Events
{
    [DisallowMultipleComponent]
    public class SafeZone : MonoBehaviour
    {
        [Header("Safe Area")]
        [SerializeField] private float radius = 1.5f;
        [SerializeField] private float groundOffset = 0.5f;
        [SerializeField] private float checkInterval = 0.1f;

        private static readonly Collider[] OverlapBuffer = new Collider[32];
        private static int activeSafeZones;

        private float nextCheckTime;
        private bool playerInside;

        public static bool IsPlayerSafe => activeSafeZones > 0;
        public bool IsPlayerInside => playerInside;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            activeSafeZones = 0;
        }

        private void OnDisable()
        {
            if (!playerInside)
            {
                return;
            }

            playerInside = false;
            activeSafeZones = Mathf.Max(0, activeSafeZones - 1);
        }

        private void Update()
        {
            if (Time.time < nextCheckTime)
            {
                return;
            }

            nextCheckTime = Time.time + Mathf.Max(0.02f, checkInterval);
            bool inside = CheckPlayerInside();
            if (inside == playerInside)
            {
                return;
            }

            playerInside = inside;
            activeSafeZones = Mathf.Max(0, activeSafeZones + (inside ? 1 : -1));
        }

        private bool CheckPlayerInside()
        {
            int count = Physics.OverlapSphereNonAlloc(
                GetSafeCenter(),
                radius,
                OverlapBuffer,
                ~0,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider hit = OverlapBuffer[i];
                if (hit != null && hit.GetComponentInParent<PlayerController>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private Vector3 GetSafeCenter()
        {
            float halfHeight = Mathf.Abs(transform.lossyScale.y) * 0.5f;
            return transform.position - Vector3.up * halfHeight + Vector3.up * groundOffset;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = GetSafeCenter();
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.3f);
            Gizmos.DrawSphere(center, radius);
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 1f);
            Gizmos.DrawWireSphere(center, radius);
        }
    }
}
