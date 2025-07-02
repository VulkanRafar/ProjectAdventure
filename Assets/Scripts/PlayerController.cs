using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements.Experimental;

[RequireComponent(typeof(Rigidbody2D), typeof(TouchingDirections), typeof(Damegable))]
public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 7f;
    public float runSpeed = 9f;
    public float airWalkSpeed = 3f;
    private float lockedAirSpeed = 0f;
    private float jumpImpulse = 14;
    public float jumpGravityScale = 3;
    public float fallGravityScale = 6f;
    public float dashSpeed = 22f;
    public float dashDuration = 0.3f;
    public float dashCooldown = 2.5f;
    public float afterImageInterval = 0.05f;
    public float wallSlideSpeed = 0.8f;
    public Vector2 wallJumpForce = new Vector2(12f, 14f);
    public float wallJumpDuration = 0.2f;
    public GameObject afterImagePrefab;
    Vector2 moveInput;
    TouchingDirections touchingDirections;
    Damegable damegable;

    void Start()
    {
        changeEffectObject.SetActive(false); // Matikan efek saat start
    }

    public float CurrentMoveSpeed
    {
        get
        {
            if (!IsMoving || touchingDirections.IsOnWall)
            {
                return 0f;
            }

            if (touchingDirections.IsGrounded)
            {
                return IsRunning ? runSpeed : walkSpeed;
            }
            else
            {
                return lockedAirSpeed > 0 ? lockedAirSpeed : airWalkSpeed;
            }
        }
    }

    [SerializeField]
    private bool _isMoving = false;

    public bool IsMoving
    {
        get
        {
            return _isMoving;
        }
        private set
        {
            _isMoving = value;
            animator.SetBool("isMoving", value);
        }
    }

    [SerializeField]
    private bool _isRunning = false;

    public bool IsRunning
    {
        get
        {
            return _isRunning;
        }
        set
        {
            _isRunning = value;
            animator.SetBool("isRunning", value);
        }
    }

    public bool _isFacingRight = true;

    public bool IsFacingRight
    {
        get { return _isFacingRight; }
        private set
        {
            if (_isFacingRight != value)
            {
                transform.localScale *= new Vector2(-1, 1);
            }

            _isFacingRight = value;
        }
    }

    public bool IsAlive
    {
        get
        {
            return animator.GetBool(AnimationStrings.isAlive);
        }

    }

    public bool LockVelocity
    {
        get
        {
            return animator.GetBool(AnimationStrings.lockVelocity);
        }
    }

    Rigidbody2D rb;
    Animator animator;

    private bool isDashing = false;
    private float dashTimeLeft;
    private float lastDashTime = -Mathf.Infinity;
    private float afterImageTimer;
    private bool isWallSliding = false;
    private bool isWallJumping = false;
    private float wallJumpDirection;

    private IEnumerator PerformDash()
    {
        isDashing = true;
        animator.SetBool("isDashing", true);

        dashTimeLeft = dashDuration;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0;
        rb.linearVelocity = new Vector2((IsFacingRight ? 1 : -1) * dashSpeed, 0f);

        afterImageTimer = 0f;

        while (dashTimeLeft > 0)
        {
            dashTimeLeft -= Time.deltaTime;
            afterImageTimer -= Time.deltaTime;

            if (afterImageTimer <= 0f)
            {
                SpawnAfterImage();
                afterImageTimer = afterImageInterval;
            }

            yield return null;
        }

        isDashing = false;
        animator.SetBool("isDashing", false);
        rb.gravityScale = originalGravity;
    }

    private void SpawnAfterImage()
    {
        GameObject afterImage = Instantiate(afterImagePrefab, transform.position, transform.rotation);
        SpriteRenderer sr = afterImage.GetComponent<SpriteRenderer>();
        SpriteRenderer playerSR = GetComponent<SpriteRenderer>();

        if (sr != null && playerSR != null)
        {
            sr.sprite = playerSR.sprite;
            sr.flipX = playerSR.flipX;
            sr.transform.localScale = transform.localScale;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        touchingDirections = GetComponent<TouchingDirections>();
        damegable = GetComponent<Damegable>();
    }

    // Max Walljumps = ...
    [SerializeField] public int maxWallJumps = 4;
    private int remainingWallJumps;

    private void FixedUpdate()
    {
        if (isDashing)
            return;

        if (!damegable.LockVelocity)
        {
            rb.linearVelocity = new Vector2(moveInput.x * CurrentMoveSpeed, rb.linearVelocity.y);
        }

        // Reset hit jika sudah menyentuh tanah
        if (animator.GetBool("isHit") && touchingDirections.IsGrounded)
        {
            animator.SetBool("isHit", false);
            rb.linearVelocity = Vector2.zero; // opsional, hentikan gerak
        }

        if (touchingDirections.IsGrounded)
        {
            lockedAirSpeed = 0f;
            remainingWallJumps = maxWallJumps;
        }

        // Wall Slide Logic
        if (!touchingDirections.IsGrounded && touchingDirections.IsOnWall && moveInput.x != 0)
        {
            isWallSliding = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
        }
        else
        {
            isWallSliding = false;
        }

        if (!LockVelocity && !isKnockbacked)
            rb.linearVelocity = new Vector2(moveInput.x * CurrentMoveSpeed, rb.linearVelocity.y);

        animator.SetFloat(AnimationStrings.yVelocity, rb.linearVelocity.y);

        // Gravitasi kustom
        if (rb.linearVelocity.y > 0.1f && !touchingDirections.IsGrounded)
        {
            rb.gravityScale = jumpGravityScale;
        }
        else if (rb.linearVelocity.y < -0.1f && !touchingDirections.IsGrounded)
        {
            rb.gravityScale = fallGravityScale;
        }
        else
        {
            rb.gravityScale = 1f;
        }
        
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();

        if (IsAlive)
        {
            IsMoving = moveInput != Vector2.zero;

            SetFacingDirection(moveInput);
        }
        else
        {
            IsMoving = false;
        }
    }

    private void SetFacingDirection(Vector2 moveInput)
    {
        if (moveInput.x > 0 && !_isFacingRight)
        {
            // Facing Right
            IsFacingRight = true;
        }
        else if (moveInput.x < 0 && _isFacingRight)
        {
            // Facing Left
            IsFacingRight = false;
        }
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            IsRunning = true;
        }
        else if (context.canceled)
        {
            IsRunning = false;
        }

    }

    private IEnumerator StopWallJumping()
    {
        yield return new WaitForSeconds(wallJumpDuration);
        isWallJumping = false;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (touchingDirections.IsGrounded)
            {
                animator.SetTrigger(AnimationStrings.jump);
                lockedAirSpeed = IsRunning ? runSpeed : walkSpeed;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpImpulse);
            }
            else if (!touchingDirections.IsGrounded && touchingDirections.IsOnWall)
            {
                isWallJumping = true;
                wallJumpDirection = IsFacingRight ? -1 : 1;

                Vector2 force = new Vector2(wallJumpForce.x * wallJumpDirection, wallJumpForce.y);
                rb.linearVelocity = force;

                // Mengurangi jumlah walljump
                remainingWallJumps--;

                // Optional: Lock input movement untuk sesaat
                StartCoroutine(StopWallJumping());
            }
        }

        if (context.canceled && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }
    }

    private bool isRanged = false;
    [SerializeField] private GameObject changeEffectObject;

    private IEnumerator HideEffectAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        changeEffectObject.SetActive(false);
    }

    public void OnToggleAttackMode(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            // Simpan mode sebelumnya
            bool previousMode = isRanged;

            // Toggle mode
            isRanged = !isRanged;
            animator.SetBool("isRanged", isRanged);
            Debug.Log("Mode Serangan: " + (isRanged ? "Ranged" : "Melee"));

            if (previousMode != isRanged)
            {
                changeEffectObject.SetActive(true);

                // Pastikan SpriteRenderer aktif
                SpriteRenderer sr = changeEffectObject.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.enabled = true;

                // Atur animasi
                Animator effectAnimator = changeEffectObject.GetComponent<Animator>();
                if (effectAnimator != null)
                    effectAnimator.SetTrigger("PlayChangeEffect");

                // Sembunyikan setelah delay
                StartCoroutine(HideEffectAfterDelay(0.6f));
            }
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            animator.SetTrigger(AnimationStrings.attack);
        }
        else
        {
            animator.SetTrigger(AnimationStrings.rangedAttack);
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.started && Time.time >= lastDashTime + dashCooldown)
        {
            lastDashTime = Time.time;
            StartCoroutine(PerformDash());
            animator.SetTrigger(AnimationStrings.isDashing);
        }
    }

    private bool isKnockbacked = false;
    private float knockbackDuration = 0.2f; // waktu terpental

    private IEnumerator RecoverFromKnockback()
    {
        yield return new WaitForSeconds(knockbackDuration);
        isKnockbacked = false;
    }

    public void OnHit(int damage, Vector2 knockback)
    {
        if (isKnockbacked) return; // jangan stack knockback
        isKnockbacked = true;

        rb.linearVelocity = knockback; // langsung atur velocity terpental
        animator.SetTrigger("hit"); // GANTI SetBool → Trigger agar tidak terjebak

        StartCoroutine(RecoverFromKnockback());
    }

    private IEnumerator ResetHitTrigger()
    {
        yield return new WaitForSeconds(0.3f); // tunggu animasi hit selesai
        animator.ResetTrigger(AnimationStrings.hit);
    }
    
    
}