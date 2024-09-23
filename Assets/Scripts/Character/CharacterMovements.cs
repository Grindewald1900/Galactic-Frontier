using UnityEngine;

public class CharacterMovements : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float groundCheckRadius = 0.2f;
    public Transform groundCheck;
    public AudioClip stepGrass;
    public CharacterDirection currentDirection = CharacterDirection.Right;
    public LayerMask groundLayer;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool isGrounded;
    private bool isRunning;
    private bool isShooting;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        CheckRunning();
        CheckJumping();
        CheckShooting();
    }

    private void CheckJumping()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        animator.SetBool("isJumping", !isGrounded);
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

    }
    private void CheckRunning()
    {
        float moveX = Input.GetAxis("Horizontal");
        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);
        if (moveX < 0)
        {
            currentDirection = CharacterDirection.Left;
        }
        else if (moveX > 0)
        {
            currentDirection = CharacterDirection.Right;
        }
        isRunning = moveX != 0 && isGrounded;
        animator.SetBool("isRunning", isRunning);

        if (isRunning)
        {
            SoundManager.instance.PlaySound(stepGrass);
        }
        spriteRenderer.flipX = currentDirection == CharacterDirection.Left;
    }

    private void CheckShooting()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PlayShootAnimationOnce();
        }
        if (Input.GetMouseButton(0))
        {
            PlayShootAnimationLoop();
        }
        else
        {
            StopShootAnimation();
        }
    }

    void PlayShootAnimationOnce()
    {
        if (!isShooting)
        {
            if (isRunning)
            {
                animator.Play("Run_shoot_Animation");
            }
            else
            {
                animator.Play("Shooting_Animation");
            }
        }
    }


    void PlayShootAnimationLoop()
    {
        if (!isShooting)
        {
            animator.SetBool("isShooting", true);
            isShooting = true;
        }
    }


    public void StopShootAnimation()
    {
        animator.SetBool("isShooting", false);
        isShooting = false;
    }

    public enum CharacterDirection
    {
        Up,
        Down,
        Left,
        Right
    }
}