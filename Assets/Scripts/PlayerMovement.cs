using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform groundCheck;

    [Header("Movimentação")]
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -20f;

    [Header("Ground Check")]
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool gravityEnabled = true;

    // Input
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpInput;
    private bool runInput;

    private PlayerInputActions inputActions;

    // Expor velocidade vertical para outros scripts
    public float VelocityY => velocity.y;

    private void Awake()
    {
        inputActions = new PlayerInputActions();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        inputActions.Player.Jump.performed += ctx => jumpInput = true;
        inputActions.Player.Run.performed += ctx => runInput = true;
        inputActions.Player.Run.canceled += ctx => runInput = false;
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        GroundCheck();
        Move();
        ApplyGravity();
        HandleJump();
        RotateCamera(); // Opcional: também pode ir em script separado
    }

    private void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }
    }

    private void Move()
    {
        Vector3 direction = transform.right * moveInput.x + transform.forward * moveInput.y;
        float currentSpeed = runInput ? runSpeed : walkSpeed;
        controller.Move(direction * currentSpeed * Time.deltaTime);
    }

    private void HandleJump()
    {
        if (jumpInput && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        jumpInput = false; // reset após uso
    }

    private void ApplyGravity()
    {
        if (!gravityEnabled) return;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void RotateCamera()
    {
        float mouseX = lookInput.x * Time.deltaTime * 100f;
        float mouseY = lookInput.y * Time.deltaTime * 100f;

        // Clamping
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private float xRotation = 0f;

    // Acesso externo
    public void DisableGravity() => gravityEnabled = false;
    public void EnableGravity() => gravityEnabled = true;
}
