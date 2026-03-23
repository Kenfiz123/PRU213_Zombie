using UnityEngine;
using UnityEngine.InputSystem;

// Simple first-person style movement using WASD, Shift to run, Space to jump.
// Attach to a GameObject with a CharacterController component.
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Input (New Input System)")]
    [SerializeField] private InputActionReference moveAction; // Vector2, WASD
    [SerializeField] private InputActionReference runAction;  // Button, hold Shift
    [SerializeField] private InputActionReference jumpAction; // Button, press Space
    [SerializeField] private InputActionReference lookAction; // Vector2, Mouse Delta

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private bool invertMove = true;

    [Header("Stamina (Sprint)")]
    [Tooltip("Script PlayerStamina trên cùng nhân vật.")]
    [SerializeField] private PlayerStamina stamina;
    [Tooltip("Lượng stamina tiêu hao mỗi giây khi giữ nút chạy.")]
    [SerializeField] private float sprintStaminaPerSecond = 15f;
    [Tooltip("Không cho chạy nếu stamina thấp hơn ngưỡng này (tránh tụt về 0 quá gắt).")]
    [SerializeField] private float minStaminaToSprint = 5f;

    [Header("Look")]
    [SerializeField] private Transform cameraPivot; // Usually your Camera transform (or a pivot holding the camera)
    [SerializeField] private float lookSensitivity = 0.12f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private bool lockCursorOnStart = true;
    [SerializeField] private bool unlockCursorWithEscape = true;

    [Header("Dash / Dodge")]
    [Tooltip("Khoảng cách lướt (đơn vị Unity).")]
    [SerializeField] private float dashDistance = 5f;
    [Tooltip("Thời gian hoàn thành cú lướt (giây). Thấp = nhanh hơn.")]
    [SerializeField] private float dashDuration = 0.15f;
    [Tooltip("Cooldown giữa 2 lần dash (giây).")]
    [SerializeField] private float dashCooldown = 3f;
    [Tooltip("Stamina tiêu hao mỗi lần dash.")]
    [SerializeField] private float dashStaminaCost = 20f;
    [Tooltip("Âm thanh khi dash (tùy chọn).")]
    [SerializeField] private AudioClip dashSound;
    [Tooltip("AudioSource phát âm thanh dash.")]
    [SerializeField] private AudioSource dashAudioSource;

    [Header("Jumping & Gravity")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.81f;
    [Tooltip("Extra gravity while going up. >1 makes jump apex faster.")]
    [SerializeField] private float upwardGravityMultiplier = 1.5f;
    [Tooltip("Extra gravity while falling. >1 makes falling faster/snappier.")]
    [SerializeField] private float downwardGravityMultiplier = 2.5f;
    [Tooltip("How long after leaving ground you can still jump.")]
    [SerializeField] private float coyoteTime = 0.12f;
    [Tooltip("How long before landing a jump press is remembered.")]
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private LayerMask groundMask;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float lastGroundedTime;
    private bool jumpUsedSinceGrounded;
    private float pitch;
    private float lastJumpPressedTime;

    // Dash state
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float lastDashTime = -999f;
    private Vector3 dashDirection;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            Debug.LogWarning("PlayerMovement: CharacterController was missing, so one was added automatically.", this);
        }
        if (cameraPivot == null && Camera.main != null)
        {
            cameraPivot = Camera.main.transform;
        }

        if (stamina == null)
        {
            stamina = GetComponent<PlayerStamina>();
        }
    }

    private void Start()
    {
        // Đảm bảo Time.timeScale = 1 khi game bắt đầu (tránh bị stuck ở 0)
        Time.timeScale = 1f;

        if (lockCursorOnStart)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnEnable()
    {
        moveAction?.action.Enable();
        runAction?.action.Enable();
        jumpAction?.action.Enable();
        lookAction?.action.Enable();
    }

    private void OnDisable()
    {
        moveAction?.action.Disable();
        runAction?.action.Disable();
        jumpAction?.action.Disable();
        lookAction?.action.Disable();
    }

    private void Update()
    {
        HandleCursorToggle();
        HandleGroundCheck();
        HandleLook();

        if (isDashing)
        {
            HandleDashMovement();
            return; // Khi đang dash, bỏ qua movement/jump/gravity thường
        }

        HandleDashInput();
        HandleMovement();
        HandleJump();
        ApplyGravity();
    }

    // ═══════════════════════════════════════════════════════════════
    // DASH / DODGE
    // ═══════════════════════════════════════════════════════════════
    private void HandleDashInput()
    {
        // Left Ctrl + đang di chuyển → dash
        if (!Keyboard.current.cKey.wasPressedThisFrame) return;

        // Check cooldown
        if (Time.time - lastDashTime < dashCooldown) return;

        // Lấy hướng di chuyển hiện tại
        Vector2 input = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        if (invertMove) input = -input;

        // Phải có hướng di chuyển mới dash được
        if (input.sqrMagnitude < 0.1f) return;

        // Check stamina
        if (stamina != null && !stamina.TryUseStamina(dashStaminaCost)) return;

        // Bắt đầu dash
        dashDirection = (transform.right * input.x + transform.forward * input.y).normalized;
        isDashing = true;
        dashTimer = 0f;
        lastDashTime = Time.time;

        // Âm thanh
        if (dashSound != null)
        {
            AudioSource src = dashAudioSource != null ? dashAudioSource : GetComponent<AudioSource>();
            if (src != null) src.PlayOneShot(dashSound);
        }
    }

    private void HandleDashMovement()
    {
        dashTimer += Time.deltaTime;

        if (dashTimer >= dashDuration)
        {
            isDashing = false;
            return;
        }

        // Di chuyển nhanh theo hướng dash
        float speed = dashDistance / dashDuration;
        controller.Move(dashDirection * speed * Time.deltaTime);
    }

    /// <summary>Dash cooldown còn lại (0 = sẵn sàng). Dùng cho UI.</summary>
    public float DashCooldownRemaining => Mathf.Max(0f, dashCooldown - (Time.time - lastDashTime));
    /// <summary>Dash đã sẵn sàng chưa.</summary>
    public bool IsDashReady => Time.time - lastDashTime >= dashCooldown;
    /// <summary>Đang dash không.</summary>
    public bool IsDashing => isDashing;

    // ═══════════════════════════════════════════════════════════════
    // CURSOR
    // ═══════════════════════════════════════════════════════════════
    private void HandleCursorToggle()
    {
        if (!unlockCursorWithEscape) return;
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

        bool currentlyLocked = Cursor.lockState == CursorLockMode.Locked;
        Cursor.lockState = currentlyLocked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = !currentlyLocked;
    }

    private void HandleLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        if (lookAction == null) return;

        Vector2 delta = lookAction.action.ReadValue<Vector2>();

        // Yaw: rotate the player around Y axis (left/right).
        float yaw = delta.x * lookSensitivity;
        transform.Rotate(Vector3.up, yaw, Space.Self);

        // Pitch: rotate camera up/down.
        if (cameraPivot != null)
        {
            pitch -= delta.y * lookSensitivity;
            pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

            Vector3 euler = cameraPivot.localEulerAngles;
            cameraPivot.localRotation = Quaternion.Euler(pitch, euler.y, 0f);
        }
    }

    private void HandleGroundCheck()
    {
        if (groundCheck == null)
        {
            // Fallback to transform position if no ground check transform is set.
            groundCheck = transform;
        }

        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask);

        // Remember the last time we were on the ground (for coyote time).
        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            jumpUsedSinceGrounded = false; // Reset jump lock while grounded
        }

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f; // Small downward force keeps the controller grounded.
        }
    }

    private void HandleMovement()
    {
        Vector2 input = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        if (invertMove)
        {
            input = -input;
        }
        float inputX = input.x;
        float inputZ = input.y;

        // Move direction follows the player's facing direction (which is controlled by mouse look).
        Vector3 move = (transform.right * inputX + transform.forward * inputZ).normalized;

        bool wantsToRun = runAction != null && runAction.action.IsPressed();
        bool canUseStamina = stamina != null && stamina.CurrentStamina > minStaminaToSprint;
        bool isRunning = false;

        if (wantsToRun && move.sqrMagnitude > 0.01f && stamina != null)
        {
            // Tiêu stamina liên tục; nếu hết thì không cho chạy nữa
            if (canUseStamina && stamina.UseStaminaContinuous(sprintStaminaPerSecond))
            {
                isRunning = true;
            }
        }

        float speed = isRunning ? runSpeed : walkSpeed;

        controller.Move(move * speed * Time.deltaTime);
    }

    private void HandleJump()
    {
        if (jumpAction != null && jumpAction.action.triggered)
        {
            lastJumpPressedTime = Time.time;
        }

        bool hasBufferedJump = Time.time - lastJumpPressedTime <= jumpBufferTime;
        bool groundedNow = isGrounded;
        bool withinCoyote = Time.time - lastGroundedTime <= coyoteTime;

        // Only allow jump when grounded or within coyote window; ignore spam in air
        if ((groundedNow || withinCoyote) && hasBufferedJump && !jumpUsedSinceGrounded)
        {
            lastJumpPressedTime = float.NegativeInfinity; // consume buffer
            jumpUsedSinceGrounded = true;
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    private void ApplyGravity()
    {
        float multiplier = velocity.y >= 0f ? upwardGravityMultiplier : downwardGravityMultiplier;
        velocity.y += gravity * multiplier * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
