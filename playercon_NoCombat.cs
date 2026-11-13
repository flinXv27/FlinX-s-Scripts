using UnityEngine;
using System.Collections;
// movement-only version of playercon (all combat / parry / attack / knockback code removed)
// NOTE: Unity requires the class name to match the file name, so this class is playercon_NoCombat
// don't put this on the same object as the original playercon, use one or the other
// remember, if the variable is public it can be changed within unity itself for ur testing
public class playercon_NoCombat : MonoBehaviour
{
    // some common prefixes i use: (Note: will not be the same across scripts)
    // D = dashing or variables that link to dashing
    // W = wall jumping or holding / or variables that link to that
    // S = animation states
    [Header("Feature Toggles")]
    public bool enableWallSlide = true;  // turns wall sliding on / off (wall jumping off a wall still works either way)
    public bool enableDashing = true;    // turns the whole dash on / off (windup, dash, freeze, exit boost)
    public bool enableDashTrail = true;  // turns the dash trail on / off (only shows when dashing is also enabled)

    [Header("Movement")]
    public bool inputLocked = false;     // set this true from any other script (cutscene, menu, etc.) to freeze player input
    public float speed = 10f;
    public float Jheight = 10f;
    float Jbuffer;
    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;
    Collider2D col;          // the player's own collider, used for dropping through one way platforms
    Collider2D dropCollider; // the platform currently being dropped through
    [HideInInspector] public Vector2 move;
    [HideInInspector] public bool Grounded; // checks if player is on the ground
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
    static int FallS = Animator.StringToHash("MukuroFall");
    static int IdleLongS = Animator.StringToHash("MukuroIdle");

    [Header("Wall Slide Variables")]
    public float WSlideSpeed = .5f; // speed of the wall slide
    public Transform groundCheck;
    public Transform wallCheckLeft;
    public Transform wallCheckRight;
    public LayerMask groundLayer;
    public LayerMask oneWayLayer;
    public float dropThroughTime = 0.3f; // how long collision with a one way platform is ignored after down + jump
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
    public bool DashAvailable => enableDashing && CanDash && Dcooler <= 0f;
    public float DashCharge => dashCharge;
    public bool WallSliding => WallSlide;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        defaultGrav = rb.gravityScale;
        SetTrail(false);
    }

    // single place that turns the trail on / off so the toggle is always respected
    void SetTrail(bool on)
    {
        if (dashTrail != null) dashTrail.emitting = on && enableDashTrail;
    }

    // used if dashing gets switched off while a dash is happening
    void CancelDash()
    {
        DashWindup = false;
        Dashing = false;
        Dqueue = false;
        CanDash = true;
        dashCharge = 1f;
        rb.gravityScale = defaultGrav;
        SetTrail(false);
    }

    void Update()
    {
        // no PauseMenu needed: when the game is paused via Time.timeScale = 0 we just skip input
        // (the dash freeze also sets timeScale to 0, so it's let through)
        if (Time.timeScale == 0f && !Dfrozen) return;
        if (inputLocked)
        {
            move = Vector2.zero;
            Jbuffer = 0f;
            Dqueue = false;
            return;
        }

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

        // dash input (ignored if dashing is disabled)
        if (enableDashing && (Input.GetKeyDown("left shift") || Input.GetKeyDown("right shift"))) Dqueue = true;

        // if the toggles get flipped in the inspector mid-game
        if (!enableDashing && (Dashing || DashWindup)) CancelDash();
        if (!enableDashTrail && dashTrail != null && dashTrail.emitting) dashTrail.emitting = false;

        if (WjumpTimer <= 0 && !Dashing)
        {
            if (move.x > 0) sr.flipX = true;
            else if (move.x < 0) sr.flipX = false;
        }

        Collider2D oneWay = null;
        foreach (Collider2D c in Physics2D.OverlapCircleAll(groundCheck.position, checkRadius, oneWayLayer))
        {
            if (c != dropCollider) { oneWay = c; break; } // skip the platform we're currently dropping through
        }
        bool onOneWay = oneWay != null && rb.linearVelocity.y <= 0.01f;   // ignore it while jumping up through it
        Grounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer) || onOneWay;

        // down + jump = drop through
        if (onOneWay && Input.GetKeyDown("space") && (Input.GetKey("s") || Input.GetKey("down")))
        {
            if (col != null)
            {
                StartCoroutine(DropThrough(oneWay));
                Jbuffer = 0f;        // don't also jump
                coyoteTimer = 0f;
                Grounded = false;
            }
        }
        touchLeft = Physics2D.OverlapCircle(wallCheckLeft.position, checkRadius, groundLayer);
        touchRight = Physics2D.OverlapCircle(wallCheckRight.position, checkRadius, groundLayer);
        // the above lines are for checking if the player is grounded or touching a wall you have to use a gameobject with a circle collider as the groundcheck and wallcheck, and set the layer of the ground to "ground" or whatever you want, just make sure to change the layer in the inspector
        OnWall = (touchLeft || touchRight) && !Grounded;

        pushingWall = (touchLeft && move.x < 0) || (touchRight && move.x > 0);
        WallSlide = enableWallSlide && OnWall && !Dashing && !DashWindup && WjumpTimer <= 0f && launchTimer <= 0f && pushingWall;
        UpdateSlideParticles();

        if (Grounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;

        if (launchTimer > 0f) launchTimer -= Time.deltaTime;

        bool idling = Grounded && !Input.anyKey && !Dashing && !DashWindup && !JumpWindup;
        idleTimer = idling ? idleTimer + Time.deltaTime : 0f;

        int newState; // setting the correct animation state based on the player's current action
        if (Dashing) newState = DashS;
        else if (DashWindup) newState = DashReadyS;
        else if (WallSlide) newState = WallSlideS;
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

    IEnumerator DropThrough(Collider2D platform) // allows the player to drop through one way platforms by ignoring collision for a short time
    {
        dropCollider = platform;
        Physics2D.IgnoreCollision(col, platform, true);
        yield return new WaitForSeconds(dropThroughTime);
        if (platform != null) Physics2D.IgnoreCollision(col, platform, false);
        if (dropCollider == platform) dropCollider = null;
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
        WjumpTimer = 0f;
        Jbuffer = 0f;
        if (slideParticles != null) slideParticles.Stop();
        rb.gravityScale = defaultGrav;
        SetTrail(false);
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
        if (!enableDashing) return false;
        if (DashWindup) return false;
        if (DashAvailable) return false;
        CanDash = true;
        dashCharge = 1f;
        Dcooler = 0f;
        return true;
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
        SetTrail(true);
    }

    void EndDash()
    {
        Dashing = false;
        rb.gravityScale = defaultGrav;
        Dboost = Ddir.x * DexitBoost; // keeps a little momentum after the dash ends
        SetTrail(false);
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
        Vector2 vel = rb.linearVelocity;

        if (Dqueue)
        {
            if (enableDashing && !Dashing && !DashWindup && CanDash && Dcooler <= 0)
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
            SetTrail(true);
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
// machinations of the mind - Pradhyot.N 20/10/26 (dont remove this comment, its the signature)
// We're probably just pathetic clones
// There are no save points in life
// If we fall asleep, we slide into the nightmare zone
// Morning, please don't come yet
// Please reset us, we're just pathetic clones
// Holding a controller we're not used to
// We try when we have to, but reality says no
// 	Just let us sleep for now - translated lyrics of "Clone Clone" by Atena