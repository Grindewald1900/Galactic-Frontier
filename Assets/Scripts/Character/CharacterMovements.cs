using UnityEngine;

public class CharacterMovements : MonoBehaviour
{
    public float moveSpeed = 0f;
    public float normalSpeed = 6f;
    public float sprintSpeed = 12f;
    public float jumpForce = 50f;
    public float groundCheckRadius = 0.2f;
    public Transform groundCheck;
    public AudioClip stepGrass;
    public AudioClip bgm;
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
        moveSpeed = normalSpeed;
    }

    void Update()
    {
        CheckSpeed();
        CheckRunning();
        CheckJumping();
        CheckShooting();
    }

    private void CheckSpeed()
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            moveSpeed = sprintSpeed;
        }
        else
        {
            moveSpeed = normalSpeed;
        }
    }

    private void CheckJumping()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        animator.SetBool("isJumping", !isGrounded);
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            // rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            rb.AddForce(new Vector2(0f, 800));
        }
    }

    private void CheckRunning()
    {
        float moveX = Input.GetAxis("Horizontal");
        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);
        animator.speed = moveSpeed <= normalSpeed ? 1 : 1.5f;
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
            SoundManager.Instance.PlaySound(SoundManager.SoundType.LOOP_SOUND_EFFECT, stepGrass);
        }
        else
        {
            SoundManager.Instance.PauseSound(SoundManager.SoundType.LOOP_SOUND_EFFECT);
        }
        spriteRenderer.flipX = currentDirection == CharacterDirection.Left;
    }

    private void CheckShooting()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            PlayShootAnimationOnce();
        }
        if (Input.GetButton("Fire1"))
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