using UnityEngine;

public class playercon1 : MonoBehaviour
{
    public float speed = 10f;
    public float Jheight = 10f;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;

    public Transform groundCheck;
    public LayerMask groundLayer;
    public float checkRadius = 0.1f;

    static int IdleS = Animator.StringToHash("MukIdle");
    static int WalkS = Animator.StringToHash("MukWalk");
    static int JumpS = Animator.StringToHash("MukJump");

    float Jbuffer;
    float coyoteTimer;
    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;
    Vector2 move;
    bool Grounded;
    int currentState;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        move.x = 0;
        if (Input.GetKey("right") || Input.GetKey("d")) move.x = 1;
        else if (Input.GetKey("left") || Input.GetKey("a")) move.x = -1;

        if (move.x > 0) sr.flipX = true;
        else if (move.x < 0) sr.flipX = false;

        if (Jbuffer > 0) Jbuffer -= Time.deltaTime;
        if (Input.GetKeyDown("space")) Jbuffer = jumpBufferTime;

        Grounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        if (Grounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;

        int newState;
        if (!Grounded) newState = JumpS;
        else if (move.x == 0) newState = IdleS;
        else newState = WalkS;

        if (newState != currentState)
        {
            anim.Play(newState);
            currentState = newState;
        }
    }

    void FixedUpdate()
    {
        Vector2 vel = rb.linearVelocity;
        vel.x = move.x * speed;

        if (Jbuffer > 0f && coyoteTimer > 0f)
        {
            vel.y = Jheight;
            Jbuffer = 0f;
            coyoteTimer = 0f;
        }

        rb.linearVelocity = vel;
    }
}

// made by pradhyot.N 21/10/2026 