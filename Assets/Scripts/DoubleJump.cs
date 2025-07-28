using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class DoubleJump : MonoBehaviour
{
    [Header("Configuração de Pulo")]
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private float jumpForce = 10f;

    private CharacterController controller;
    private PlayerMovement playerMovement;
    private PlayerInputActions inputActions;

    private int jumpCount;
    private bool isGrounded;
    private bool jumpPressed;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        inputActions = new PlayerInputActions();

        inputActions.Player.Jump.performed += ctx => jumpPressed = true;
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void Update()
    {
        GroundCheck();

        if (jumpPressed && jumpCount < maxJumps)
        {
            playerMovement.HandleJump(jumpForce);
            jumpCount++;
        }

        jumpPressed = false; // reset no final do frame
    }

    private void GroundCheck()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && playerMovement.VelocityY <= 0f)
        {
            jumpCount = 0;
        }
    }
}
