using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class BasePlayer : MonoBehaviour
{
    // -- COMPONENTS
    //private InputSystem_Actions inputActions;
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

    public AudioClip JumpAudio;

    void Awake()
    {
        //inputActions = new InputSystem_Actions();
        //inputActions.Player.SetCallbacks(this);
    }
    private Animator animator;
    public bool CanPlay = true;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    /*private void OnEnable() 
    {
        inputActions.Player.Enable();
    }
    private void OnDisable() 
    {
        inputActions.Player.Disable();
    }*/

    void Update() 
    {
        animator.SetBool("isAereal", !isGround);
    }
    void FixedUpdate()
    {
        if (SceneManager.GetActiveScene().name == "FinalScene")
        {
            return;
        }
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

        if (playerMove.x > 0.05f)
        {
            isFacingRight = true;
            transform.localScale = new Vector2(1, 1);
        }

        else if (playerMove.x < -0.05f)
        {
            isFacingRight = false;
            transform.localScale = new Vector2(-1, 1);
        }
        animator.SetBool("isWalking", Mathf.Abs(playerMove.x) > 0.05f);
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (isGround)
        {
            animator.SetBool("isAereal", false);
            Jump();
        }
        else if (wallCoyoteCounter > 0f)
        {
            WallJump();
        }
        else
            animator.SetBool("isAereal", true);
    }

    public void OnPause(InputAction.CallbackContext context) 
    { 
        if (context.started || !UIManager.Instance.isRebinding)
        {
            if (UIManager.Instance.m_isOpen)
            {
                UIManager.Instance.OnMenuButton();
                return;
            }
            PauseManager.Instance.TogglePause();
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
        AudioManager.Instance.PlaySFX(JumpAudio);
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