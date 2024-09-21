using System;
using UnityEngine;

public class CharacterMovements : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool isGrounded;
    private bool isJumping;
    private bool isRunning;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        isJumping = !isGrounded;
        isRunning = moveX != 0 && isGrounded;
        spriteRenderer.flipX = moveX < 0;
        animator.SetBool("isJumping", isJumping);
        animator.SetBool("isRunning", isRunning);
        animator.SetFloat("xSpeed", Math.Abs(moveX));


        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump();
        }
        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);
    }

    void Jump()
    {

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

}
