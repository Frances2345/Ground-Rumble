using UnityEngine;
using UnityEngine.InputSystem;

namespace GroundRumble
{
    /// <summary>
    /// Mirada con raton. Rota el jugador en Y y el pivote de camara en X.
    /// </summary>
    public class MouseLook : MonoBehaviour
    {
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float sensitivity = 0.12f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;

        private PlayerController playerController;
        private float pitch;

        public void Configure(Transform pivot)
        {
            cameraPivot = pivot;
        }

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
        }

        private void Update()
        {
            // 1. CONDICIÓN CLAVE: Si el tiempo está detenido (juego pausado), no movemos la cámara.
            if (Time.timeScale == 0f)
            {
                return;
            }

            // 2. Si hacemos clic en la pantalla mientras jugamos, bloqueamos el ratón.
            // (Ya no choca con los botones de la UI porque si está pausado, no pasa de la condición de arriba)
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                LockCursor();
                return;
            }

            // 3. Si el ratón está desbloqueado (como al estar en la pantalla de pausa), evitamos rotar.
            if (Cursor.lockState != CursorLockMode.Locked || Mouse.current == null)
            {
                return;
            }

            if (playerController != null && !playerController.ControlEnabled)
            {
                return;
            }

            // Movimiento normal de cámara
            Vector2 delta = Mouse.current.delta.ReadValue() * sensitivity;

            transform.Rotate(Vector3.up, delta.x, Space.Self);

            pitch = Mathf.Clamp(pitch - delta.y, minPitch, maxPitch);

            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}