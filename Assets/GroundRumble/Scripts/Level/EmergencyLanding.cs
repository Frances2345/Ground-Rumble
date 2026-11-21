using UnityEngine;

namespace GroundRumble
{
    /// <summary>
    /// Relleno de la ruta de emergencia. Al entrar, baja al jugador hasta el
    /// rellano del piso destino (que esta en el lado opuesto) con un breve
    /// enfriamiento para evitar cadenas de teletransportes.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class EmergencyLanding : MonoBehaviour
    {
        private EmergencyStairwell owner;
        private Vector3 targetPosition;

        public string RouteName { get; private set; }

        public void Configure(EmergencyStairwell stairwell, Vector3 destination, string routeName)
        {
            owner = stairwell;
            targetPosition = destination;
            RouteName = routeName;

            BoxCollider trigger = GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            // El tamano real lo da la escala del GameObject (2.2 x 2 x 2.2).
            trigger.size = Vector3.one;
        }

        private void Reset()
        {
            BoxCollider trigger = GetComponent<BoxCollider>();
            trigger.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();

            if (player == null || owner == null || !owner.CanUseRoute)
            {
                return;
            }

            player.TeleportToPosition(targetPosition);
            owner.NotifyRouteUsed();
        }
    }
}
