using UnityEngine;
using UnityEngine.InputSystem;

public class BasePlayer : MonoBehaviour, InputSystem_Actions.IPlayerActions
{
    // -- COMPONENTS
    private InputSystem_Actions inputActions;
    private Rigidbody2D rb;
    public PlayerData data;
    private bool isFacingRight;
    bool canWallJump;
    RaycastHit2D hit;

    public bool isGround { get; set; }
    [SerializeField] private Transform groundChecker;

    // -- VARIABLES
    Vector2 playerMove;

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

    // Update is called once per frame
    void FixedUpdate()
    {
        CalculateMoveSpeed(playerMove);
        GroundDetector();
    }

    void Update()
    {
        Vector2 dir = GetPlayerDirection().normalized; 
        hit = Physics2D.Raycast(transform.position, dir, data.wallRayLength, data.groundMask); 
        // Debug.DrawRay usa (Origen, Dirección * Longitud, Color)
        Debug.DrawRay(transform.position, dir * data.wallRayLength, Color.red);
    } 

    public void OnMove(InputAction.CallbackContext context)
    {
        playerMove = context.ReadValue<Vector2>();
    }

    private void GroundDetector()
    {
        isGround = Physics2D.OverlapCircle(groundChecker.position, data.detectionRadius, data.groundMask);
        
    }


    private void CalculateMoveSpeed(Vector2 playerInput)
    {
        float targetSpeed = playerInput.x * data.maxSpeed;
        float speedDif = targetSpeed - rb.linearVelocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? data.acceleration : data.deceleration;
        float movement = speedDif * accelRate;
        rb.AddForce(movement * Vector2.right.normalized);
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (isGround && context.performed)
        {
            
            Jump();
            return;
        }
        if (canWallJump) 
        {
            
            WallJump(GetPlayerDirection().x);
            Debug.Log($"WallJumping: " + GetPlayerDirection().x + "Isground: " + isGround);
        }
    }

    private void WallJump(float inputXDir)
    {
        rb.linearVelocity = Vector2.zero;
        float jumpDir = Mathf.Sign(inputXDir);
        rb.AddForce(new Vector2(data.wallJumpForce * jumpDir, data.jumpForce), ForceMode2D.Impulse);
    }

    private void Jump() 
    {
        rb.AddForce(Vector2.up.normalized * data.jumpForce, ForceMode2D.Impulse);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        canWallJump = !isGround && rb.linearVelocityY < 0 && collision.collider.CompareTag("Ground");
        //if (!isGround && rb.linearVelocityY < 0 && collision.collider.CompareTag("Ground")) Debug.Log("CanWallJump");
    }

    public Vector2 GetPlayerDirection()
    {
            if (playerMove.x > 0.05f) isFacingRight = true;
        else if (playerMove.x < -0.05f) isFacingRight = false;

        return isFacingRight ? Vector2.right : Vector2.left;
    }




}
