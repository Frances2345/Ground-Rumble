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
        [SerializeField] private bool lockCursorOnStart = true;

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

        private void Start()
        {
            if (lockCursorOnStart)
            {
                LockCursor();
            }
        }

        private void OnDisable()
        {
            UnlockCursor();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                UnlockCursor();
                return;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                LockCursor();
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked || Mouse.current == null)
            {
                return;
            }

            if (playerController != null && !playerController.ControlEnabled)
            {
                return;
            }

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

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
