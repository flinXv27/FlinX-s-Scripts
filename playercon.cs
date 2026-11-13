using UnityEngine;
using System.Collections;
// this was made for multiple people as why there are so many comments
// quick warning, this script will house all the player functions and will be crazy hard to debug, if you have any specific problems please contact me and ill try patching it!
// all code is changeable and can be modified to your liking
// this script will have to be well understood to be modified, so please read the comments and understand the code before changing it
// remember, if the variable is public it can be changed within unity itself for ur testing
public class playercon : MonoBehaviour
{
    // some common prefixes i use: (Note: will not be the same across scripts)
    // D = dashing or variables that link to dashing
    // W = wall jumping or holding / or variables that link to that
    // S = animation states
    public float speed = 10f;
    public float Jheight = 10f; 
    float Jbuffer; 
    Rigidbody2D rb; 
    Animator anim;
    SpriteRenderer sr;
    PlayerCombat combat;
    [HideInInspector] public Vector2 move;
    [HideInInspector] public bool Grounded; // checks if player is on the ground
    [HideInInspector] public int attackState;
    [HideInInspector] public bool ParryMovementLocked;
    float knockTimer;
    int currentState;
    public float coyoteTime = 0.12f; // this is the coyote time variable
    float coyoteTimer;
    // if you don't know what coyote time is, its genuinely just Wile E. Coyote (watch the cartoon!)
    public float jumpBufferTime = 0.12f; // this for jump buffering

    static int IdleS = Animator.StringToHash("MukuroStand"); // yeah im not using the animator controller (remove if you are using it)
    static int WalkS = Animator.StringToHash("MukuroRun");
    static int DashS = Animator.StringToHash("MukuroDash");
    static int JumpS = Animator.StringToHash("MukuroJump");
    static int WallSlideS = Animator.StringToHash("MukuroWallSlide");
    static int DashReadyS = Animator.StringToHash("MukuroDashReady");
    static int JumpReadyS = Animator.StringToHash("MukuroJumpReady");
    static int FallS      = Animator.StringToHash("MukuroFall");
    static int IdleLongS  = Animator.StringToHash("MukuroIdle");

    [Header("Wall Slide Variables")]
    public float WSlideSpeed = .5f; // this is the variables for a wall slide, if you want to remove it, comment the code in the FixedUpdate function and add a wall slide animation
    public Transform groundCheck;
    public Transform wallCheckLeft;
    public Transform wallCheckRight;
    public LayerMask groundLayer;
    public LayerMask oneWayLayer;
    public float checkRadius = 0.1f;
    // the last 5 variables above are for checking if the player is on a wall or on the ground
    bool WallSlide; 
    bool OnWall;
    bool touchLeft; 
    bool touchRight;
    bool pushingWall;
    public Vector2 Wjumpf = new Vector2(12f, 14f); // adjust as neccessary, X = horizontal jump force, Y = vertical jump force
    public float WjumpLock = 0.15f;
    float WjumpTimer;

    [Header("Wall Grab & Slide PFX")]
    public ParticleSystem slideParticles;       // assign a particle system (can be a child of the player)

    [Header("Ready / Idle Animations")]
    public float DwindupTime = .15f;       // pause before a dash fires
    public float JwindupTime = .1f;        // crouch before a grounded jump fires
    public bool JwindupLocksMove = true;   // stand still while crouching
    public float FallThreshold = 1f;       // downward speed needed to show the fall anim
    public float IdleWaitTime = 5f;        // seconds of no input before the long idle plays
    [HideInInspector] public bool DashWindup;
    [HideInInspector] public bool JumpWindup;
    float DwindupTimer, JwindupTimer, launchTimer, idleTimer;

    [Header("Dash Variables")]
    public float Dspeed = 45f;
    public float Dtime = .08f;
    public float Dcooldown = .4f;
    public float Dfreeze = .05f; 
    public float DexitBoost = 10f;
    public float DexitYBoost = 8f; 
    public float Ddecay = 60f; 
    public float DashRechargeTime = 1f;
    public TrailRenderer dashTrail; // just like celeste it makes copies of the character while dashing
    bool Dfrozen; // checks if the player controls are frozen
    [HideInInspector] public bool Dashing;
    bool Dqueue; // checks if shift was pressed and sees if dashing is allowed
    bool CanDash = true;
    float dashCharge = 1f;
    float Dtimer; // how much time is left on the dash
    float Dboost; // the horizontal momentum you keep after the dash
    float Dcooler; // the cooldown timer 
    Vector2 Ddir; // the direction of the current dash that is normalized so that digonals don't magically giga increase ur speed
    float defaultGrav; // resets the player's gravity back to the amount registered normally
    public bool DashAvailable => CanDash && Dcooler <= 0f;
    public float DashCharge => dashCharge;
    public bool WallSliding => WallSlide;




    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        combat = GetComponent<PlayerCombat>();
        defaultGrav = rb.gravityScale;
        if (dashTrail != null) dashTrail.emitting = false;
    }

    void Update()
    {
        if (PauseMenu.IsPaused) return;
        bool parryReactionActive = combat != null && combat.ParryReactionActive;
        if (parryReactionActive)
        {
            move = Vector2.zero;
            Jbuffer = 0f;
            Dqueue = false;
            JumpWindup = false;
        }
        else
        {
            // basic movement
            move.x = 0;
            if (Input.GetKey("right") || Input.GetKey("d")) move.x = 1;
            else if (Input.GetKey("left") || Input.GetKey("a")) move.x = -1;

            move.y = 0;
            if (Input.GetKey("up") || Input.GetKey("w")) move.y = 1;
            else if (Input.GetKey("down") || Input.GetKey("s")) move.y = -1; // this is for dashing in the direction you are pressing

            // jumping action
            if (Jbuffer > 0) Jbuffer -= Time.deltaTime;
            if (Input.GetKeyDown("space")) Jbuffer = jumpBufferTime;
            //else if (Input.GetKey("down") || Input.GetKey("s")) move.y = -1; if you want to add crouching action, uncomment this line and add crouch animation

            if (Input.GetKeyDown("left shift") || Input.GetKeyDown("right shift")) Dqueue = true; // dash input
        }

        if (WjumpTimer <= 0 && !Dashing && attackState == 0)
        {
            if (move.x > 0) sr.flipX = true;
            else if (move.x < 0) sr.flipX = false;
        }

        if (ParryMovementLocked)
        {
            move = Vector2.zero;
            Jbuffer = 0f;
            Dqueue = false;
            JumpWindup = false;
        }

        Collider2D oneWay = Physics2D.OverlapCircle(groundCheck.position, checkRadius, oneWayLayer);
        bool onOneWay = oneWay != null && rb.linearVelocity.y <= 0.01f;   // ignore it while jumping up through it
        Grounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer) || onOneWay;

        // down + jump = drop through
        if (onOneWay && Input.GetKeyDown("space") && (Input.GetKey("s") || Input.GetKey("down")))
        {
            OneWayPlatform plat = oneWay.GetComponent<OneWayPlatform>();
            if (plat != null)
            {
                plat.DropThrough();
                Jbuffer = 0f;        // don't also jump
                coyoteTimer = 0f;
                Grounded = false;
            }
        }
        touchLeft = Physics2D.OverlapCircle(wallCheckLeft.position, checkRadius, groundLayer);
        touchRight = Physics2D.OverlapCircle(wallCheckRight.position, checkRadius, groundLayer);
        // the above three lines are for checking if the player is grounded or touching a wall you have to use a gameobject with a circle collider as the groundcheck and wallcheck, and set the layer of the ground to "ground" or whatever you want, just make sure to change the layer in the inspector
        OnWall = (touchLeft || touchRight) && !Grounded;

        pushingWall = (touchLeft && move.x < 0) || (touchRight && move.x > 0);
        WallSlide = OnWall && !Dashing && !DashWindup && WjumpTimer <= 0f && knockTimer <= 0f && launchTimer <= 0f && (pushingWall || (ParryMovementLocked && WallSlide));
        UpdateSlideParticles();

        if (Grounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;

        if (launchTimer > 0f) launchTimer -= Time.deltaTime;

        bool idling = Grounded && !Input.anyKey && !Dashing && !DashWindup && !JumpWindup && attackState == 0;
        idleTimer = idling ? idleTimer + Time.deltaTime : 0f;

        int newState;
        if (parryReactionActive) newState = attackState;
        else if (Dashing) newState = DashS;
        else if (DashWindup) newState = DashReadyS;
        else if (WallSlide && (combat == null || !combat.ParryAnimationActive)) newState = WallSlideS;
        else if (attackState != 0) newState = attackState;
        else if (JumpWindup) newState = JumpReadyS;
        else if (launchTimer > 0f) newState = JumpS;   // stops a 1-frame Idle flicker while the ground check clears
        else if (!Grounded) newState = rb.linearVelocity.y < -FallThreshold ? FallS : JumpS;
        else if (move.x == 0) newState = idleTimer >= IdleWaitTime ? IdleLongS : IdleS;
        else newState = WalkS;
        if (newState != currentState)
        {
            anim.Play(newState);
            currentState = newState;
        }
    }
    

    IEnumerator DashFreeze() // this is for a more punchy dash effect by freezing movement
    {
        Dfrozen = true;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(Dfreeze);
        Time.timeScale = 1f;
        Dfrozen = false;
        BeginDashMove();
    }
    public void Knockback(Vector2 vel, float time)
    {
        if (DashWindup)
        {
            DashWindup = false;
            rb.gravityScale = defaultGrav;
            CanDash = true;
            dashCharge = 1f;
        }
        rb.linearVelocity = vel;
        knockTimer = time;
    }
    public void ResetState()
    {
        DashWindup = false;
        JumpWindup = false;
        idleTimer = 0f;
        launchTimer = 0f;
        Dashing = false;
        Dqueue = false;
        CanDash = true;
        dashCharge = 1f;
        Dboost = 0f;
        knockTimer = 0f;
        WjumpTimer = 0f;
        Jbuffer = 0f;
        attackState = 0;
        if (slideParticles != null) slideParticles.Stop();
        rb.gravityScale = defaultGrav;
        if (dashTrail != null) dashTrail.emitting = false;
    }
    public void SetDashCharge(float c)
    {
        dashCharge = Mathf.Clamp01(c);
        CanDash = dashCharge >= 1f;
    }
    void UpdateSlideParticles()
    {
        if (slideParticles == null) return;
        var em = slideParticles.emission;

        if (WallSlide)
        {
            // sit on whichever wall we're touching
            Transform side = touchLeft ? wallCheckLeft : wallCheckRight;
            slideParticles.transform.position = side.position;
            em.enabled = true;
            if (!slideParticles.isPlaying) slideParticles.Play();
        }
        else
        {
            em.enabled = false;   // stops new particles, existing ones fade out naturally
        }
    }
    public bool RestoreDash()
    {
        if (DashWindup) return false;
        if (DashAvailable) return false;
        CanDash = true;
        dashCharge = 1f;
        Dcooler = 0f;
        return true;
    }
    public Vector2 GetMouseOrMoveDirection()
    {
        Camera camera = Camera.main;
        if (camera != null)
        {
            Ray mouseRay = camera.ScreenPointToRay(Input.mousePosition);
            Plane playerPlane = new Plane(Vector3.forward, transform.position);
            if (playerPlane.Raycast(mouseRay, out float distance))
            {
                Vector2 mouseDirection = (Vector2)mouseRay.GetPoint(distance) - (Vector2)transform.position;
                if (mouseDirection != Vector2.zero) return mouseDirection.normalized;
            }
        }

        if (move != Vector2.zero) return move.normalized;
        return sr.flipX ? Vector2.right : Vector2.left;
    }
    public void FaceDirection(Vector2 direction)
    {
        if (direction.x > 0f) sr.flipX = true;
        else if (direction.x < 0f) sr.flipX = false;
    }
    Vector2 GetDashDir() // finds the direction the dash will travel
    {
        if (move != Vector2.zero) return move.normalized;
        return sr.flipX ? Vector2.right : Vector2.left;
    }
 
    void BeginDashMove()
    {
        Ddir = GetDashDir(); // dashes in ur direction
        if (Ddir.x != 0f) sr.flipX = Ddir.x > 0f; 
        if (dashTrail != null) dashTrail.emitting = true;
    }
 
    void EndDash()
    {
        Dashing = false;
        rb.gravityScale = defaultGrav;
        Dboost = Ddir.x * DexitBoost; // keeps a little momentum after the dash ends
        if (dashTrail != null) dashTrail.emitting = false;
    }
    void FixedUpdate() // actual movement 
    {
        if (WjumpTimer > 0) WjumpTimer -= Time.fixedDeltaTime;
        if (Dcooler > 0) Dcooler -= Time.fixedDeltaTime;
        if (Grounded && !Dashing && !DashWindup)
        {
            dashCharge = 1f;
            CanDash = true;
        }
        else if (!CanDash && !DashWindup)
        {
            dashCharge = DashRechargeTime <= 0f
                ? 1f
                : Mathf.MoveTowards(dashCharge, 1f, Time.fixedDeltaTime / DashRechargeTime);
            CanDash = dashCharge >= 1f;
        }
        if (ParryMovementLocked)
        {
            Dqueue = false;
            JumpWindup = false;
            if (DashWindup)
            {
                DashWindup = false;
                CanDash = true;
                dashCharge = 1f;
                rb.gravityScale = defaultGrav;
            }
            rb.linearVelocity = Vector2.zero;
            return;
        }
        if (knockTimer > 0f)
        {
            knockTimer -= Time.fixedDeltaTime;
            Dqueue = false;
            return;
        }
        Vector2 vel = rb.linearVelocity;
        
        if (Dqueue)
        {
            if (!Dashing && !DashWindup && CanDash && Dcooler <= 0)
            {
                JumpWindup = false;
                DashWindup = true;
                DwindupTimer = DwindupTime;
                CanDash = false;
                dashCharge = 0f;
                rb.gravityScale = 0f;
                rb.linearVelocity = Vector2.zero;
                WjumpTimer = 0f;
            }
        Dqueue = false;
        }

        if (DashWindup)
        {
            DwindupTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = Vector2.zero;
            if (DwindupTimer > 0f) return;  // windup finished, actually start the dash

            DashWindup = false;
            Dashing = true;
            Dtimer = Dtime;
            Dcooler = Dcooldown;
            if (dashTrail != null) dashTrail.emitting = true;
            Ddir = GetDashDir(); // direction is read at the end, so you can still aim during the pause
            if (Ddir.x != 0) sr.flipX = Ddir.x > 0f;
            if (Dfreeze > 0f) StartCoroutine(DashFreeze());
            else BeginDashMove();
        }

        if (Dashing)
        {
            if (Dfrozen) return;

            Dtimer -= Time.fixedDeltaTime;
            if (Dtimer <= 0f)
            {
                EndDash();
                vel.x = move.x * speed;
                vel.y = Ddir.y * DexitYBoost;
            }
            else
            {
                rb.linearVelocity = Ddir * Dspeed;
                return;
            }
        }
        else
        {
            if (WjumpTimer <= 0f) vel.x = move.x * speed;
        }
        Dboost = Mathf.MoveTowards(Dboost, 0f, Ddecay * Time.fixedDeltaTime);
        if (WjumpTimer <= 0f) vel.x += Dboost;

        if (JumpWindup)
        {
            JwindupTimer -= Time.fixedDeltaTime;
            if (JwindupLocksMove) vel.x = 0f;
            if (JwindupTimer <= 0f)
            {
                JumpWindup = false;
                vel.y = Jheight;
                coyoteTimer = 0f;
                launchTimer = .1f;
            }
        }
        else if (Jbuffer > 0f)
        {
            if (coyoteTimer > 0f)
            {
                Jbuffer = 0f;
                if (JwindupTime > 0f)
                {
                    JumpWindup = true;
                    JwindupTimer = JwindupTime;
                }
                else
                {
                    vel.y = Jheight;
                    coyoteTimer = 0f;
                    launchTimer = .1f;
                }
            }
        else if (OnWall)
        {
            float awayDir = touchLeft ? 1f : -1f;
            vel = new Vector2(awayDir * Wjumpf.x, Wjumpf.y);
            WjumpTimer = WjumpLock;
            sr.flipX = awayDir > 0f;
            Jbuffer = 0f;
        }
    }
        if (WallSlide && WjumpTimer <= 0f) vel.y = -WSlideSpeed; // wall slide speed
        rb.linearVelocity = vel;
    }
}
// this was actual hell to write - Pradhyot.N 20/10/26 (dont remove this comment, its the signature)
//L dp mxvw d olwwoh eurnhq, iroorz pb hbhv
//Brx sxw brxu ihdu lqvlgh d jluo wkdw'v eurnhq lqvlgh
//Mxvw krsh brxu zlvk iru mrb zloo frph rxw wuxh (Zloo frph rxw wuxh)
//Sxoolqj sdshu rxw pb vzhhwv, L'p sudblqj iru wlsv
//Exw rqob iloohg zlwk eulwwoh sduwv olnh d edj ixoo ri fklsv
//Zkr dp L wr ghqb zkhuh L'p iurp?
//Zkdw L'p iurp? - fkruxv ri Srwhwr fkls eb URPD
// shift 3 