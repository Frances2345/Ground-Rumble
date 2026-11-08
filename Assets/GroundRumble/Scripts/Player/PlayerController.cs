using UnityEngine;
using UnityEngine.InputSystem;

namespace GroundRumble
{
    /// <summary>
    /// Controlador en primera persona: WASD, agacharse con Ctrl, correr con Shift y salto con Espacio.
    /// La mirada la gestiona <see cref="MouseLook"/>.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour, InputSystem_Actions.IPlayerActions
    {
        [Header("Movimiento")]
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float sprintSpeed = 7.5f;
        [SerializeField] private float crouchSpeed = 2f;
        [SerializeField] private float rotationSpeed = 12f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private bool rotateTowardsMovement = false;

        [Header("Agacharse")]
        [SerializeField] private float standingHeight = 2f;
        [SerializeField] private float crouchingHeight = 1.1f;
        [SerializeField] private Vector3 standingCenter = new Vector3(0f, 1f, 0f);
        [SerializeField] private Vector3 crouchingCenter = new Vector3(0f, 0.55f, 0f);
        [SerializeField] private float crouchCameraHeight = 1.05f;
        [SerializeField] private bool toggleCrouch = false;

        [Header("Referencias")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private PlayerStamina stamina;
        [SerializeField] private PlayerHealth health;

        [Header("Depuracion")]
        [SerializeField] private bool enableFloorTeleport = true;
        [SerializeField] private float floorHeight = 3.2f;

        private CharacterController characterController;
        private InputSystem_Actions inputActions;

        private Vector2 moveInput;
        private Vector3 velocity;
        private float standingCameraHeight;
        private bool sprintHeld;
        private bool isCrouching;
        private bool isSprinting;
        private bool isGrounded;
        private bool controlEnabled = true;

        public bool IsCrouching => isCrouching;
        public bool IsSprinting => isSprinting;
        public bool IsGrounded => isGrounded;
        public bool ControlEnabled => controlEnabled;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            stamina = stamina != null ? stamina : GetComponent<PlayerStamina>();
            health = health != null ? health : GetComponent<PlayerHealth>();

            inputActions = new InputSystem_Actions();

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (cameraPivot != null)
            {
                standingCameraHeight = cameraPivot.localPosition.y;
            }

            characterController.height = standingHeight;
            characterController.center = standingCenter;
        }

        private void OnEnable()
        {
            if (inputActions == null)
            {
                inputActions = new InputSystem_Actions();
            }

            inputActions.Player.Enable();
            inputActions.Player.AddCallbacks(this);
        }

        private void OnDisable()
        {
            inputActions?.Player.Disable();
            inputActions?.Player.RemoveCallbacks(this);
            stamina?.StopSprint();
        }

        private void OnDestroy()
        {
            inputActions?.Dispose();
        }

        public void Configure(Transform cam, Transform pivot)
        {
            cameraTransform = cam;
            cameraPivot = pivot;

            if (cameraPivot != null)
            {
                standingCameraHeight = cameraPivot.localPosition.y;
            }
        }

        private void Update()
        {
            if (!controlEnabled)
            {
                return;
            }

            isGrounded = characterController.isGrounded;

            if (isGrounded && velocity.y < 0f)
            {
                velocity.y = -2f;
            }

            Vector3 moveDirection = Vector3.zero;

            if (moveInput != Vector2.zero)
            {
                Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
                Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;

                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
            }

            bool wantsSprint = sprintHeld
                && !isCrouching
                && moveDirection != Vector3.zero
                && stamina != null
                && (stamina.IsSprinting || stamina.CanStartSprint);

            isSprinting = false;

            if (wantsSprint)
            {
                isSprinting = stamina.ConsumeSprint(Time.deltaTime);
            }
            else if (stamina != null)
            {
                stamina.StopSprint();
            }

            float currentSpeed = walkSpeed;

            if (isCrouching)
            {
                currentSpeed = crouchSpeed;
            }
            else if (isSprinting)
            {
                currentSpeed = sprintSpeed;
            }

            characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

            if (rotateTowardsMovement && moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            velocity.y += gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);

            UpdateCameraHeight();
        }

        private void UpdateCameraHeight()
        {
            if (cameraPivot == null)
            {
                return;
            }

            float targetHeight = isCrouching ? crouchCameraHeight : standingCameraHeight;
            Vector3 localPosition = cameraPivot.localPosition;
            localPosition.y = Mathf.Lerp(localPosition.y, targetHeight, Time.deltaTime * 12f);
            cameraPivot.localPosition = localPosition;
        }

        private void SetCrouching(bool value)
        {
            if (value == isCrouching)
            {
                return;
            }

            if (!value && !CanStandUp())
            {
                return;
            }

            isCrouching = value;

            if (isCrouching)
            {
                characterController.height = crouchingHeight;
                characterController.center = crouchingCenter;
                isSprinting = false;
                stamina?.StopSprint();
            }
            else
            {
                characterController.height = standingHeight;
                characterController.center = standingCenter;
            }
        }

        private bool CanStandUp()
        {
            float radius = characterController.radius * 0.95f;
            Vector3 bottom = transform.position + Vector3.up * (radius + 0.1f);
            Vector3 top = transform.position + Vector3.up * (standingHeight - radius);
            Collider[] overlaps = Physics.OverlapCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore);

            foreach (Collider overlap in overlaps)
            {
                if (overlap.transform == transform || overlap.transform.IsChildOf(transform))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private void TeleportVertically(float amount)
        {
            if (!enableFloorTeleport || amount == 0f)
            {
                return;
            }

            characterController.enabled = false;
            transform.position += Vector3.up * amount;
            characterController.enabled = true;
            velocity.y = 0f;
        }

        public void SetControlEnabled(bool enabled)
        {
            controlEnabled = enabled;

            if (!enabled)
            {
                moveInput = Vector2.zero;
                sprintHeld = false;
                stamina?.StopSprint();
            }
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
        }

        public void OnCrouch(InputAction.CallbackContext context)
        {
            if (toggleCrouch)
            {
                if (context.performed)
                {
                    SetCrouching(!isCrouching);
                }

                return;
            }

            if (context.performed)
            {
                SetCrouching(true);
            }
            else if (context.canceled)
            {
                SetCrouching(false);
            }
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed && isGrounded && !isCrouching && controlEnabled)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        public void OnPrevious(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                TeleportVertically(-floorHeight);
            }
        }

        public void OnNext(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                TeleportVertically(floorHeight);
            }
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                sprintHeld = true;
            }
            else if (context.canceled)
            {
                sprintHeld = false;
            }
        }
    }
}
