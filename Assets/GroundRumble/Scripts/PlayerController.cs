using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, InputSystem_Actions.IPlayerActions
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.5f;

    [Header("Crouch")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchingHeight = 1f;
    [SerializeField] private Vector3 standingCenter = new Vector3(0, 1f, 0);
    [SerializeField] private Vector3 crouchingCenter = new Vector3(0, 0.5f, 0);

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float pitchMin = -85f;
    [SerializeField] private float pitchMax = 85f;

    private CharacterController characterController;
    private InputSystem_Actions inputActions;
    private Transform cameraTransform;

    private Vector2 moveInput;
    private Vector3 velocity;
    private float pitch;
    private bool isSprinting;
    private bool isCrouching;
    private bool isGrounded;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        inputActions = new InputSystem_Actions();
        cameraTransform = Camera.main != null ? Camera.main.transform : null;

        characterController.height = standingHeight;
        characterController.center = standingCenter;

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.identity;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.AddCallbacks(this);
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
        inputActions.Player.RemoveCallbacks(this);
    }

    private void Update()
    {
        isGrounded = characterController.isGrounded;

        if (isGrounded && velocity.y < 0)
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

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        Vector2 look = context.ReadValue<Vector2>();
        transform.Rotate(Vector3.up, look.x * mouseSensitivity, Space.Self);

        pitch = Mathf.Clamp(pitch - look.y * mouseSensitivity, pitchMin, pitchMax);
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
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
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
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
            isSprinting = true;
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
            characterController.height = crouchingHeight;
            characterController.center = crouchingCenter;
            isSprinting = false;
        }
        else
        {
            characterController.height = standingHeight;
            characterController.center = standingCenter;
        }
    }
}
