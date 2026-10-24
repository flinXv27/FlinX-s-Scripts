using UnityEngine;

// Shared foundation for every enemy: setup, player lookup, facing, ledge/wall checks, line of sight,
// target tracking, hit flash / knockback / death, animation helper, reset and base gizmos.
//
// To make a new enemy: inherit from this, then implement
//   Think()        - your state machine (called every frame from Update)
//   GetAnimState() - which animation hash to play right now
//   OnReset()      - put your own state/timers back to default
// and optionally override: OnInit, IsImmune, HasSuperArmour, OnHurt, GetStateColour, FixedUpdate.
public abstract class EnemyBase : MonoBehaviour, IDamageable, IResettable
{
    protected Rigidbody2D rb;
    protected Animator anim;
    protected SpriteRenderer sr;
    protected Transform player;
    protected IDamageable playerHealth;
    protected PlayerCombat playerCombat; // used to check if the player is immune before it hits them

    protected float facing = 1f;   // 1 = right, -1 = left
    protected float startFacing;
    protected float moveDir = 1f;  // direction FixedUpdate moves in (usually == facing, shooter backs away with -facing)
    protected float currentSpeed;
    protected float turnTimer;
    protected float loseTimer;
    protected float stunTimer;
    protected float flashTimer;
    protected float defaultGrav;
    protected bool grounded;
    protected bool SpriteFacingL = true;
    protected int currentAnim;
    protected int health;
    protected Vector2 lastSeen;
    protected Vector2 startPos;
    protected Color baseColor;

    // ---------- shared settings ----------
    [Header("Movement")]
    public float Pspeed = 4f;
    public float Aspeed = 6f;
    public float turnCooldown = .2f; // stops the enemy flipping back and forth every frame

    [Header("Detection")]
    public float Drange = 9f;
    public float LoseInterest = 12f;
    public float LoseTime = 2f;
    public bool needLOS = true;

    [Header("Ground Checks")]
    public LayerMask groundLayer;
    public float checkRadius = .1f;
    public Vector2 groundCheckOffset = new Vector2(0f, -0.55f);
    public Vector2 ledgeCheckOffset = new Vector2(0.6f, -0.6f);
    public Vector2 wallCheckOffset = new Vector2(0.6f, 0f);

    [Header("Health & hit reaction")]
    public int MaxHealth = 3;
    public Vector2 KnockbackForce = new Vector2(6f, 4f);
    public float StunTime = .25f;
    public float HitFlashTime = .15f;
    public Color HitColor = Color.red;

    [Header("Major Enemy Check")]
    public bool MajorEnemy = false;

    // ---------- things children must / can provide ----------
    protected abstract void Think(float dist, bool canSee);
    protected abstract int GetAnimState();
    protected abstract void OnReset();

    protected virtual void OnInit() { }
    protected virtual bool IsImmune => false;        // true = TakeDamage does nothing
    protected virtual bool HasSuperArmour => false;  // true = takes damage + flashes but no stun/knockback
    protected virtual void OnHurt() { }              // change state after being hit (interrupt aim, start chasing...)
    protected virtual Color GetStateColour() => baseColor; // telegraph tint for the current state

    // ---------- lifecycle ----------
    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        defaultGrav = rb.gravityScale;
        baseColor = sr.color;
        health = MaxHealth;
        applyFacing();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerHealth = p.GetComponent<IDamageable>();
            playerCombat = p.GetComponent<PlayerCombat>();
            if (playerHealth == null) Debug.LogError($"Player '{p.name}' must have a component implementing IDamageable.", p);
            IgnorePlayerCollisions();
        }
        IgnoreEnemyCollisions();
        startPos = transform.position;
        startFacing = facing;
        ResetManager.Register(this);
        OnInit();
    }

    protected virtual void OnDestroy()
    {
        ResetManager.Unregister(this);
    }

    protected virtual void Update()
    {
        if (stunTimer > 0f) stunTimer -= Time.deltaTime;
        if (turnTimer > 0f) turnTimer -= Time.deltaTime;
        UpdateColour();

        Vector2 pos = transform.position;
        grounded = Physics2D.OverlapCircle(pos + groundCheckOffset, checkRadius, groundLayer);

        float dist = player != null ? Vector2.Distance(pos, player.position) : Mathf.Infinity;
        bool canSee = dist <= Drange && (!needLOS || HasLOS());

        Think(dist, canSee);
        PlayAnim(GetAnimState());
    }

    protected virtual void FixedUpdate() // actual movement
    {
        if (stunTimer > 0f) return; // let the knockback play out
        Vector2 vel = rb.linearVelocity;
        vel.x = currentSpeed * moveDir;
        rb.linearVelocity = vel;
    }

    // ---------- shared behaviour ----------

    // walk forward, turn around at ledges and walls
    protected void PatrolStep()
    {
        currentSpeed = Pspeed;
        moveDir = facing;
        if (grounded && !CanMove(facing) && turnTimer <= 0f) Flip();
    }

    // call every frame while chasing. Updates lastSeen / the lose-interest timer.
    // returns false once the enemy has given up (go back to patrol)
    protected bool TrackTarget(float dist, out bool seeing)
    {
        seeing = player != null && dist <= LoseInterest && (!needLOS || HasLOS());
        if (seeing)
        {
            loseTimer = LoseTime;
            lastSeen = player.position;
            return true;
        }
        loseTimer -= Time.deltaTime;
        return loseTimer > 0f;
    }

    // start tracking the player (used when first spotting them or when hit while patrolling)
    protected void NoticePlayer()
    {
        loseTimer = LoseTime;
        if (player != null) lastSeen = player.position;
    }

    public void TakeDamage(int amount, Vector2 from, bool major = false)
    {
        if (IsImmune) return;

        health -= amount;
        if (health <= 0)
        {
            gameObject.SetActive(false);
            return;
        }
        flashTimer = HitFlashTime;
        if (HasSuperArmour) return;

        stunTimer = StunTime;
        OnHurt();
        float side = transform.position.x >= from.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(side * KnockbackForce.x, KnockbackForce.y);
    }

    public void ResetToStart()
    {
        gameObject.SetActive(true);
        IgnorePlayerCollisions();
        IgnoreEnemyCollisions();
        transform.position = startPos;
        rb.gravityScale = defaultGrav;
        rb.linearVelocity = Vector2.zero;
        health = MaxHealth;
        facing = startFacing;
        applyFacing();
        currentSpeed = 0f;
        turnTimer = loseTimer = stunTimer = flashTimer = 0f;
        sr.color = baseColor;
        currentAnim = 0;
        OnReset();
    }

    // ---------- helpers ----------
    void UpdateColour()
    {
        Color target;
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            target = HitColor;
        }
        else target = GetStateColour();
        if (sr.color != target) sr.color = target;
    }

    protected void PlayAnim(int hash)
    {
        if (anim == null || hash == currentAnim || !anim.HasState(0, hash)) return; // missing clips are skipped quietly
        anim.Play(hash);
        currentAnim = hash;
    }

    // is there ground just in front of the feet in that direction?
    protected bool HasGroundAhead(float dir)
    {
        Vector2 pos = transform.position;
        return Physics2D.OverlapCircle(pos + new Vector2(ledgeCheckOffset.x * dir, ledgeCheckOffset.y), checkRadius, groundLayer);
    }

    protected bool HasWallAhead(float dir)
    {
        Vector2 pos = transform.position;
        return Physics2D.OverlapCircle(pos + new Vector2(wallCheckOffset.x * dir, wallCheckOffset.y), checkRadius, groundLayer);
    }

    // true if there's ground to walk onto and no wall in the way in that direction
    protected bool CanMove(float dir)
    {
        if (!grounded) return true;
        return HasGroundAhead(dir) && !HasWallAhead(dir);
    }

    protected bool HasLOS()
    {
        bool clear = !Physics2D.Linecast(transform.position, player.position, groundLayer);
        Debug.DrawLine(transform.position, player.position, clear ? Color.green : Color.red); // shows enemy LOS
        return clear;
    }

    protected void FaceTarget(float dx)
    {
        if (Mathf.Abs(dx) > .1f)
        {
            facing = Mathf.Sign(dx);
            applyFacing();
        }
    }

    protected void Flip()
    {
        facing = -facing;
        turnTimer = turnCooldown;
        applyFacing();
    }

    protected void applyFacing()
    {
        moveDir = facing;
        sr.flipX = SpriteFacingL ? facing > 0f : facing < 0f;
    }

    protected void IgnorePlayerCollisions()
    {
        if (player == null) return;
        Collider2D[] enemyColliders = GetComponentsInChildren<Collider2D>();
        Collider2D[] playerColliders = player.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D e in enemyColliders)
            foreach (Collider2D p in playerColliders)
                Physics2D.IgnoreCollision(e, p);
    }

    void IgnoreEnemyCollisions()
    {
        Collider2D[] enemyColliders = GetComponentsInChildren<Collider2D>();
        foreach (EnemyBase otherEnemy in FindObjectsByType<EnemyBase>())
        {
            if (otherEnemy == this) continue;

            Collider2D[] otherColliders = otherEnemy.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D enemyCollider in enemyColliders)
                foreach (Collider2D otherCollider in otherColliders)
                    Physics2D.IgnoreCollision(enemyCollider, otherCollider);
        }

        foreach (SimpleEnemy otherEnemy in FindObjectsByType<SimpleEnemy>())
        {
            Collider2D[] otherColliders = otherEnemy.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D enemyCollider in enemyColliders)
                foreach (Collider2D otherCollider in otherColliders)
                    Physics2D.IgnoreCollision(enemyCollider, otherCollider);
        }
    }

    protected virtual void OnDrawGizmosSelected() // children call base.OnDrawGizmosSelected() then add their own
    {
        Vector2 pos = transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, Drange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(pos, LoseInterest);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(pos + groundCheckOffset, checkRadius);
        Gizmos.DrawWireSphere(pos + new Vector2(ledgeCheckOffset.x * facing, ledgeCheckOffset.y), checkRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pos + new Vector2(wallCheckOffset.x * facing, wallCheckOffset.y), checkRadius);
    }
}
