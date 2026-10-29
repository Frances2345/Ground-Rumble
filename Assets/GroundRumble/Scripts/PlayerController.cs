using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour, InputSystem_Actions.IPlayerActions
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float jumpSpeed = 5f;

    [Header("Crouch")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchingHeight = 1f;
    [SerializeField] private float standingEyeHeight = 1.6f;
    [SerializeField] private float crouchingEyeHeight = 0.9f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float pitchMin = -80f;
    [SerializeField] private float pitchMax = 80f;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrain = 25f;
    [SerializeField] private float staminaRegen = 15f;
    [SerializeField] private float minStaminaToSprint = 10f;

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private InputSystem_Actions inputActions;
    private Transform cameraTransform;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isSprinting;
    private bool isCrouching;
    private bool isGrounded;
    private bool jumpRequested;
    private float pitch;
    private float currentStamina;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        inputActions = new InputSystem_Actions();
        cameraTransform = Camera.main != null ? Camera.main.transform : null;

        currentStamina = maxStamina;

        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // No forzar FPS: usar la tasa de refresco del monitor
        Application.targetFrameRate = -1;
        QualitySettings.vSyncCount = 0;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.useGravity = true;

        capsule.height = standingHeight;
        capsule.center = Vector3.zero;
        capsule.radius = 0.5f;

        if (cameraTransform != null)
        {
            cameraTransform.localPosition = new Vector3(0f, standingEyeHeight, 0f);
            cameraTransform.localRotation = Quaternion.identity;
        }
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.AddCallbacks(this);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
        inputActions.Player.RemoveCallbacks(this);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        // Ground check
        isGrounded = Physics.Raycast(transform.position, Vector3.down, (standingHeight * 0.5f) + 0.15f);

        // Look
        if (lookInput.sqrMagnitude > 0f)
        {
            float yaw = lookInput.x * mouseSensitivity;
            float pitchDelta = lookInput.y * mouseSensitivity;

            transform.Rotate(Vector3.up * yaw);
            pitch = Mathf.Clamp(pitch - pitchDelta, pitchMin, pitchMax);

            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        // Stamina
        if (isSprinting && moveInput.sqrMagnitude > 0f && !isCrouching)
        {
            currentStamina = Mathf.Max(0f, currentStamina - staminaDrain * Time.deltaTime);
            if (currentStamina <= 0f)
            {
                isSprinting = false;
            }
        }
        else
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegen * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        Vector3 moveDirection = Vector3.zero;

        if (moveInput != Vector2.zero)
        {
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
        }

        float currentSpeed = walkSpeed;

        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else if (isSprinting && currentStamina > minStaminaToSprint)
        {
            currentSpeed = sprintSpeed;
        }

        Vector3 horizontalVelocity = moveDirection * currentSpeed;
        rb.angularVelocity = Vector3.zero;

        Vector3 velocity = rb.linearVelocity;
        velocity.x = horizontalVelocity.x;
        velocity.z = horizontalVelocity.z;

        if (jumpRequested && isGrounded)
        {
            velocity.y = jumpSpeed;
            jumpRequested = false;
        }

        rb.linearVelocity = velocity;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            lookInput = context.ReadValue<Vector2>();
        }
        else if (context.canceled)
        {
            lookInput = Vector2.zero;
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ToggleCrouch();
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded && !isCrouching)
        {
            jumpRequested = true;
        }
    }

    public void OnPrevious(InputAction.CallbackContext context)
    {
    }

    public void OnNext(InputAction.CallbackContext context)
    {
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (currentStamina > minStaminaToSprint)
            {
                isSprinting = true;
            }
        }
        else if (context.canceled)
        {
            isSprinting = false;
        }
    }

    private void ToggleCrouch()
    {
        isCrouching = !isCrouching;

        if (isCrouching)
        {
            capsule.height = crouchingHeight;
            capsule.center = new Vector3(0f, crouchingHeight * 0.5f, 0f);
            isSprinting = false;

            if (cameraTransform != null)
            {
                cameraTransform.localPosition = new Vector3(0f, crouchingEyeHeight, 0f);
            }
        }
        else
        {
            capsule.height = standingHeight;
            capsule.center = Vector3.zero;

            if (cameraTransform != null)
            {
                cameraTransform.localPosition = new Vector3(0f, standingEyeHeight, 0f);
            }
        }
    }

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public bool IsCrouching => isCrouching;
}