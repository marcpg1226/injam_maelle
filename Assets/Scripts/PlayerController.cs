using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class BasePlayer : MonoBehaviour, InputSystem_Actions.IPlayerActions
{
    // -- COMPONENTS
    private InputSystem_Actions inputActions;
    private Rigidbody2D rb;
    public PlayerData data;

    [SerializeField] private Transform groundChecker;

    // -- STATE
    public bool isGround { get; private set; }
    private bool isTouchingWall;
    private int wallSide;  // -1 = pared a la izquierda, 1 = pared a la derecha
    private bool isWallSliding;
    private bool canMoveOnAir = true;
    private bool isFacingRight = true;
    private float wallCoyoteCounter;

    // -- INPUT
    private Vector2 playerMove;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Player.SetCallbacks(this);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable() => inputActions.Player.Enable();
    private void OnDisable() => inputActions.Player.Disable();

    void FixedUpdate()
    {
        GroundDetector();
        WallDetector();
        HandleWallSlide();

        if (canMoveOnAir)
            CalculateMoveSpeed(playerMove);
    }

    // ---------------- INPUT ----------------

    public void OnMove(InputAction.CallbackContext context)
    {
        playerMove = context.ReadValue<Vector2>();

        if (playerMove.x > 0.05f) isFacingRight = true;
        else if (playerMove.x < -0.05f) isFacingRight = false;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (isGround)
        {
            Jump();
        }
        else if (wallCoyoteCounter > 0f)
        {
            WallJump();
        }
    }

    // ---------------- DETECCIÓN ----------------

    private void GroundDetector()
    {
        isGround = Physics2D.OverlapCircle(groundChecker.position, data.detectionRadius, data.groundMask);
    }

    private void WallDetector()
    {
        Vector2 origin = transform.position;

        RaycastHit2D hitRight = Physics2D.Raycast(origin, Vector2.right, data.wallRayLength, data.groundMask);
        RaycastHit2D hitLeft = Physics2D.Raycast(origin, Vector2.left, data.wallRayLength, data.groundMask);

        Debug.DrawRay(origin, Vector2.right * data.wallRayLength, hitRight ? Color.green : Color.red);
        Debug.DrawRay(origin, Vector2.left * data.wallRayLength, hitLeft ? Color.green : Color.red);

        if (hitRight) wallSide = 1;
        else if (hitLeft) wallSide = -1;
        else wallSide = 0;

        isTouchingWall = wallSide != 0 && !isGround;

        if (isTouchingWall)
            wallCoyoteCounter = data.wallCoyoteTime;
        else
            wallCoyoteCounter -= Time.fixedDeltaTime;
    }

    // ---------------- MOVIMIENTO ----------------

    private void CalculateMoveSpeed(Vector2 playerInput)
    {
        float targetSpeed = playerInput.x * data.maxSpeed;
        float speedDif = targetSpeed - rb.linearVelocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? data.acceleration : data.deceleration;
        float movement = speedDif * accelRate;
        rb.AddForce(movement * Vector2.right);
    }

    private void HandleWallSlide()
    {
        bool pushingTowardsWall = Mathf.Sign(playerMove.x) == wallSide && Mathf.Abs(playerMove.x) > 0.05f;
        isWallSliding = isTouchingWall && rb.linearVelocity.y < 0 && pushingTowardsWall;

        if (isWallSliding)
        {
            float clampedY = Mathf.Max(rb.linearVelocity.y, -data.wallSlideSpeed);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, clampedY);
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * data.jumpForce, ForceMode2D.Impulse);
    }

    private void WallJump()
    {
        float jumpDir = -wallSide;
        if (wallSide == 0) jumpDir = isFacingRight ? -1f : 1f;

        wallCoyoteCounter = 0f;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(data.wallJumpForce * jumpDir, data.jumpForce), ForceMode2D.Impulse);

        StopAllCoroutines();
        StartCoroutine(WallJumpRoutine());
    }

    private IEnumerator WallJumpRoutine()
    {
        canMoveOnAir = false;
        yield return new WaitForSeconds(data.afterWallJumpTime);
        canMoveOnAir = true;
    }
}