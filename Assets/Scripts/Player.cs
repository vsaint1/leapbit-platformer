using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {

    private enum PlayerState { Idle, Running, Jumping, DoubleJumping, Falling, WallSliding, Knockback }
    private PlayerState currentState;

    [Header("Movement")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float deceleration = 20f;
    [SerializeField] private float airAcceleration = 8f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float doubleJumpForce = 10f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    private bool canDoubleJump;
    private float coyoteTimer;
    private float jumpBufferTimer;

    [Header("Wall")]
    [SerializeField] private float wallSlideSpeed = 0.5f;
    [SerializeField] private Vector2 wallJumpForce = new Vector2(7f, 14f);
    [SerializeField] private float wallJumpDuration = 0.15f;
    [SerializeField] private float wallCoyoteTime = 0.1f;
    private float wallCoyoteTimer;
    private int lastWallDir;

    [Header("Knockback")]
    [SerializeField] private Vector2 knockbackForce = new Vector2(5F, 30f);
    [SerializeField] private float knockbackDuration = 0.3f;
    [SerializeField] private float invulnerabilityDuration = 1f;

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private Collider2D col;
    private Animator animator;

    private bool isGrounded;
    private bool wasGrounded;
    private bool isTouchingWall;
    private bool isWallSliding;
    private float horizontal;
    private float vertical;

    private int facingDir = 1;
    private int wallDir;
    private bool wallJumping;
    private bool wallJumped;

    private bool isKnockback;
    private bool isInvulnerable;

    void Start() {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        animator = GetComponentInChildren<Animator>();
    }

    void Update() {
        if (isKnockback) return;

        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(KeyCode.T))
            TakeDamage(transform.position - new Vector3(1, 0, 0));

        wasGrounded = isGrounded;
        isGrounded = CheckGrounded();
        isTouchingWall = CheckWall();
        wallDir = GetWallDir();
        isWallSliding = isTouchingWall && !isGrounded && rb.linearVelocity.y <= 0 && !wallJumped;

        UpdateTimers();
        HandleLanding();

        if (Input.GetKeyDown(KeyCode.Space)) jumpBufferTimer = jumpBufferTime;
        if (jumpBufferTimer > 0) HandleJump();

        if (!wallJumping) ApplyMovement();
        ApplyGravityModifier();
        HandleFlip();
        UpdateState();
        HandleAnimations();
    }

    void UpdateTimers() {
        coyoteTimer -= Time.deltaTime;
        jumpBufferTimer -= Time.deltaTime;
        wallCoyoteTimer -= Time.deltaTime;

        if (isGrounded) {
            coyoteTimer = coyoteTime;
        }

        if (isTouchingWall && !isGrounded) {
            wallCoyoteTimer = wallCoyoteTime;
            lastWallDir = wallDir;
        }
    }

    void HandleLanding() {
        if (isGrounded) {
            canDoubleJump = true;
            wallJumped = false;
        }
    }

    void UpdateState() {
        if (isKnockback) {
            SetState(PlayerState.Knockback);
        }
        else if (isWallSliding) {
            SetState(PlayerState.WallSliding);
        }
        else if (!isGrounded && rb.linearVelocity.y < -0.1f) {
            SetState(PlayerState.Falling);
        }
        else if (!isGrounded && currentState == PlayerState.DoubleJumping) {
            SetState(PlayerState.DoubleJumping);
        }
        else if (!isGrounded) {
            SetState(PlayerState.Jumping);
        }
        else if (Mathf.Abs(horizontal) > 0.01f) {
            SetState(PlayerState.Running);
        }
        else {
            SetState(PlayerState.Idle);
        }
    }

    void SetState(PlayerState newState) {
        if (currentState == newState) return;
        currentState = newState;
    }

    void ApplyMovement() {
        if (isWallSliding) {
            const float ySlideModifier = 0.05f;
            float slideSpeed = vertical < 0 ? wallSlideSpeed / ySlideModifier : wallSlideSpeed;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -slideSpeed);
            return;
        }

        float targetX = horizontal * speed;
        float rate = isGrounded
            ? (Mathf.Abs(horizontal) > 0.01f ? acceleration : deceleration)
            : airAcceleration;
        float smoothVX = Mathf.MoveTowards(rb.linearVelocity.x, targetX, rate * Time.deltaTime);
        rb.linearVelocity = new Vector2(smoothVX, rb.linearVelocity.y);
    }

    void ApplyGravityModifier() {
        if (isWallSliding || isGrounded) return;

        if (rb.linearVelocity.y < 0) {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !Input.GetKey(KeyCode.Space)) {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
        }
    }

    void HandleFlip() {
        if (isWallSliding || wallJumping) return;

        if (horizontal > 0 && facingDir != 1) {
            facingDir = 1;
            transform.rotation = Quaternion.identity;
        }
        else if (horizontal < 0 && facingDir != -1) {
            facingDir = -1;
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
    }

    void HandleJump() {
        if (isWallSliding || wallCoyoteTimer > 0) {
            WallJump();
            jumpBufferTimer = 0;
        }
        else if (coyoteTimer > 0) {
            Jump(jumpForce);
            coyoteTimer = 0;
            jumpBufferTimer = 0;
        }
        else if (canDoubleJump) {
            DoubleJump();
            jumpBufferTimer = 0;
        }
    }

    void Jump(float force) {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
        SetState(PlayerState.Jumping);
    }

    void DoubleJump() {
        canDoubleJump = false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, doubleJumpForce);
        SetState(PlayerState.DoubleJumping);
    }

    void WallJump() {
        wallJumped = true;
        canDoubleJump = true;
        wallCoyoteTimer = 0;
        StopCoroutine(nameof(WallJumpRoutine));
        StartCoroutine(nameof(WallJumpRoutine));
    }

    IEnumerator WallJumpRoutine() {
        wallJumping = true;
        SetState(PlayerState.Jumping);

        int jumpDir = wallDir != 0 ? wallDir : lastWallDir;
        facingDir = -jumpDir;
        transform.rotation = facingDir == 1 ? Quaternion.identity : Quaternion.Euler(0, 180, 0);
        rb.linearVelocity = new Vector2(-jumpDir * wallJumpForce.x, wallJumpForce.y);

        yield return new WaitForSeconds(wallJumpDuration);

        wallJumping = false;
    }

    public void TakeDamage(Vector2 sourcePosition) {
        if (isInvulnerable) return;

        Vector2 dir = ((Vector2)transform.position - sourcePosition).normalized;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);

        isKnockback = true;
        isInvulnerable = true;
        SetState(PlayerState.Knockback);
        animator.SetTrigger("KnockBack");
        StartCoroutine(COR_Knockback());
        StartCoroutine(COR_Invulnerability());
    }

    IEnumerator COR_Knockback() {
        yield return new WaitForSeconds(knockbackDuration);
        isKnockback = false;
    }

    IEnumerator COR_Invulnerability() {
        yield return new WaitForSeconds(invulnerabilityDuration);
        isInvulnerable = false;
    }

    void HandleAnimations() {
        float normalizedY = Mathf.Clamp(rb.linearVelocity.y / jumpForce, -1f, 1f);
        animator.SetFloat("Velocity_X", Mathf.Abs(rb.linearVelocity.x));
        animator.SetFloat("Velocity_Y", normalizedY);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsWallSliding", isWallSliding);
    }

    bool CheckGrounded() {
        Vector2 origin = new Vector2(col.bounds.center.x, col.bounds.min.y);
        float distance = 0.1f;
        Debug.DrawRay(origin, Vector2.down * distance, isGrounded ? Color.green : Color.red);
        return Physics2D.Raycast(origin, Vector2.down, distance, groundLayer).collider != null;
    }

    bool CheckWall() {
        Vector2 originR = new Vector2(col.bounds.max.x, col.bounds.center.y);
        Vector2 originL = new Vector2(col.bounds.min.x, col.bounds.center.y);
        float dist = 0.4f;
        bool hitR = Physics2D.Raycast(originR, Vector2.right, dist, groundLayer).collider != null;
        bool hitL = Physics2D.Raycast(originL, Vector2.left, dist, groundLayer).collider != null;
        Debug.DrawRay(originR, Vector2.right * dist, hitR ? Color.green : Color.red);
        Debug.DrawRay(originL, Vector2.left * dist, hitL ? Color.green : Color.red);
        return hitR || hitL;
    }

    int GetWallDir() {
        Vector2 originR = new Vector2(col.bounds.max.x, col.bounds.center.y);
        Vector2 originL = new Vector2(col.bounds.min.x, col.bounds.center.y);
        if (Physics2D.Raycast(originR, Vector2.right, 0.4f, groundLayer).collider != null) return 1;
        if (Physics2D.Raycast(originL, Vector2.left, 0.4f, groundLayer).collider != null) return -1;
        return 0;
    }
}