using UnityEngine;
using System.Collections;
using System;
using UnityEngine.AI;

// this was made for multiple people as why there are so many comments
// quick warning, this script will house all the player functions and will be crazy hard to debug, if you have any specific problems please contact me and ill try patching it!
// all code is changeable and can be modified to your liking
// this script will have to be well understood to be modified, so please read the comments and understand the code before changing it
// remember, if the variable is public it can be changed within unity itself for ur testing
// many debug features are currently commented uncomment if you want to use them, also only use one debug at a time
// honestly i'll add the rest of the comments if enough people want it but im really not bothered right now
public class SimpleEnemy : MonoBehaviour
{
    // some common prefixes i use:
    // D = Detection or things that would link to that
    // A = attacking or things that would lead to that
    // S = animation states
    // J = jumping or something related to that

    enum State {Patrol, Chase, Attack} // this intializes the different states
    State state = State.Patrol;
    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;
    Transform player;

    public float Pspeed = 6f;
    public float Aspeed = 10f;
    float facing = 1f; // 1 = right, -1 = left
    float currentSpeed;
    float turnTimer;
    float ATimer;
    float windupTimer;
    float Jtimer;
    float loseTimer;
    bool Jqueue;
    bool grounded;
    bool ledgeAhead;
    bool wallAhead;
    int currentAnim;
    bool SpriteFacingL = true;
    Vector2 lastSeen; 
  

    [Header("Jumping stuff")]
    public bool CanJump = true;
    public float JCooldown = .4f;
    public float Jheight = 8f;
    public float JifAbove = 1.5f;

    [Header("Tracking a player Downwards")]
    public bool CanDrop = true;
    public float dropIfBelow = 1.5f;
    public float Maxfall = 6f;

    [Header("Ground Checks")]
    public LayerMask groundLayer;
    public float checkRadius = .1f;
    public Vector2 groundCheckOffset = new Vector2(0f, -0.55f);
    public Vector2 ledgeCheckOffset = new Vector2(0.6f, -0.6f);
    public Vector2 wallCheckOffset = new Vector2(0.6f, 0f); 
    public float turnCooldown = 0.2f; // stops the enemy flipping back and forth every frame

    [Header("Detection & attack")]
    public float Drange = 8f;
    public float LoseInterest = 11f;
    public float ARange = 1.2f; 
    public float AHeight = 1f;
    public bool needLOS = true; 
    public float AWindup = .35f;
    public float ACool = 1f;
    public float LoseTime = 2f;
    public float StopX = .5f;
    public int Damage = 1;

    static int IdleS = Animator.StringToHash("officerIdle"); // rename to match your animation clips
    static int WalkS = Animator.StringToHash("officerWalk");
    static int AttackS = Animator.StringToHash("officerAttack");

    void DoAttack()
    {
        if (player == null) return;
        bool inRange = Vector2.Distance(transform.position, player.position) <= ARange + .3f;
        bool inHeight = Mathf.Abs(player.position.y - transform.position.y) <= AHeight + .3f;
        if (inRange && inHeight)
        {
            Debug.Log("Player has Taken damage (placeholder)"); // this will be replaced in the section where the player has health added
        }
    }
    bool HasLOS()
    {
        return !Physics2D.Linecast(transform.position, player.position, groundLayer);
    }
    void FaceTarget(float dx)
    {
        if (Mathf.Abs(dx) > .1f)
        {
            facing = Mathf.Sign(dx);
            applyFacing();
        }
    }
    void Flip()
    {
        facing = -facing;
        turnTimer = turnCooldown;
        applyFacing();
    }
    void applyFacing()
    {
        sr.flipX = SpriteFacingL ? facing > 0f : facing < 0f;
    }
    Vector2 Fwd(Vector2 offset)
    {
        return new Vector2(offset.x * facing, offset.y);
    }
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        applyFacing();
    }

    void Update()
    {
        if (turnTimer > 0f) turnTimer -= Time.deltaTime;
        if (ATimer > 0f) ATimer -= Time.deltaTime;
        if (Jtimer > 0f) Jtimer -= Time.deltaTime;
        Vector2 pos = transform.position;
        grounded = Physics2D.OverlapCircle(pos + groundCheckOffset, checkRadius, groundLayer);
        ledgeAhead = Physics2D.OverlapCircle(pos + Fwd(ledgeCheckOffset), checkRadius, groundLayer);
        wallAhead = Physics2D.OverlapCircle(pos + Fwd(wallCheckOffset), checkRadius, groundLayer);
        float Dist = player != null ? Vector2.Distance(pos, player.position) : Mathf.Infinity;
        bool canSee = Dist <= Drange && (!needLOS || HasLOS());
        bool blocked = !ledgeAhead || wallAhead; 
        //Debug.Log("Dist: " + Dist + " canSee: " + canSee + " LOS: " + (player != null && HasLOS()));
        //Debug.Log("state: " + state + " grounded: " + grounded + " ledgeAhead: " + ledgeAhead + " wallAhead: " + wallAhead); 

        switch (state)
        {
        case State.Patrol:
            currentSpeed = Pspeed;
            if (grounded && blocked && turnTimer <= 0f) Flip();
            if (canSee)state = State.Chase;
        break;

        case State.Chase:
            bool seeing = Dist <= LoseInterest && (!needLOS || HasLOS());
            if(seeing)
            {
                loseTimer = LoseTime;
                lastSeen = player.position;
            }
            else
            {
                loseTimer -= Time.deltaTime;
                if (loseTimer <= 0f)
                {
                    state = State.Patrol;
                    break;    
                }

            }
            float dx = lastSeen.x - pos.x;
            FaceTarget(dx);
            if (seeing && Dist <= ARange)
            {
                currentSpeed = 0f;
                if (ATimer <= 0f)
                {
                    state = State.Attack;
                    windupTimer = AWindup;
                }
            }
            else
            {
                bool playerBelow = seeing && player.position.y < pos.y - dropIfBelow;
                bool safeDrop = false;
                if (CanDrop && playerBelow && grounded && !ledgeAhead)
                {
                    safeDrop = Physics2D.Raycast(pos + Fwd(ledgeCheckOffset), Vector2.down, Maxfall, groundLayer).collider != null;
                }
                bool stopAtLedge = grounded && !ledgeAhead && !safeDrop;
                bool stopAtWall = grounded && wallAhead && CanJump;
                bool alignedX = Mathf.Abs(dx) < StopX;
                currentSpeed = (stopAtLedge || stopAtWall || alignedX) ? 0f : Aspeed;
                bool playerAbove = seeing && JifAbove > 0f && player.position.y - pos.y > JifAbove && Mathf.Abs(dx) <= ARange;
                if (CanJump && grounded && Jtimer <= 0f && (wallAhead || playerAbove))
                {
                    Jqueue = true;
                    Jtimer = JCooldown;
                }
            }
            break;

            case State.Attack:
               currentSpeed = 0f;
               windupTimer -= Time.deltaTime;
               if (windupTimer <= 0f)
                {
                    DoAttack();
                    ATimer = ACool;
                    state = State.Chase;
                }
            break;
        }

        int newAnim; 
        if (state == State.Attack) newAnim = AttackS;
        else if (currentSpeed != 0f) newAnim = WalkS;
        else newAnim = IdleS;
        if (anim != null && newAnim != currentAnim)
        {
            anim.Play(newAnim);
            currentAnim = newAnim;
        }
        //Debug.Log(state);
    }

    void FixedUpdate() // actual movement
    {
        Vector2 vel = rb.linearVelocity;
        vel.x = currentSpeed * facing;
        if (Jqueue)
        {
            vel.y = Jheight;
            Jqueue = false;
        }
        rb.linearVelocity = vel;
    }

    void OnDrawGizmosSelected() // used for debugging in the scene view, basically it shows what the enemy can see
    {
        Vector2 pos = transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, Drange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(pos, LoseInterest);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, ARange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(pos + groundCheckOffset, checkRadius);
        Gizmos.DrawWireSphere(pos + Fwd(ledgeCheckOffset), checkRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pos + Fwd(wallCheckOffset), checkRadius);
    }
}

// teto kept me sane - pradhyot.N 22/10/2026 (this is the script signiture)