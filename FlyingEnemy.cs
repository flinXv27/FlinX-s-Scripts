using UnityEngine;

public class FlyingEnemy : EnemyBase
{
    enum State {Patrol, Chase, Aim, WindUp, Swoop, Recover}
    State state = State.Patrol; 

    Vector2 desiredVel;
    float AimTimer;
    float shotTimer;
    float windTimer;
    float swoopTimer;
    float SwoopCooldown; 
    float RecoverTimer;
    float SwoopMaxDist;
    bool hitThisSwoop;
    Vector2 swoopTarget;
    Vector2 swoopDir;
    Vector2 swoopStart;

    [Header("Flying")]
    public float accel = 25f;
    public float BobAmount = .25f;
    public float BobSpeed = 3f;
    public float ObstacleCheckDist = .7f;
    public float PatrolRange = 5f;

    [Header("hovering while chasing")]
    public float HoverDistanceX = 4f;
    public float HoverHeight = 3f;

    [Header("Shooting")]
    public EnemyProjectile PPrefab;
    public float ShotRange = 11f;
    public float aimTime = .5f;
    public float shotCooldown = 2f;
    public float PSpeed = 9f;
    public int PDamage = 1;
    public Vector2 MuzzleOffset = new Vector2(0.4f, -0.2f);
    public bool ShootWhenClose = false; 
    public Color AimColor = new Color(1f, .9f, .4f);
 
    [Header("Swoop")]
    public float SRange = 5f;         
    public float SWindup = .6f;       
    public float WindupBackSpeed = 3f;
    public float SSpeed = 18f;
    public float SOvershoot = 3f;     
    public float SCooldown = 3f;
    public float SRecoverTime = 1.2f; 
    public int SDamage = 1;
    public Vector2 HitBoxHalfSize = new Vector2(.6f, .6f);
    public bool InterruptAttacks = true;  
    public Color WindupColor = new Color(1f, .6f, .2f);
 
    static int FlyS = Animator.StringToHash("flyerFly"); 
    static int ShootS = Animator.StringToHash("flyerShoot");
    static int WindupS = Animator.StringToHash("flyerWindup");
    static int SwoopS = Animator.StringToHash("flyerSwoop");

    void Reset()
    {
        Pspeed = 3f;
        Aspeed = 5f;
        Drange = 12f;
        LoseInterest = 15f;
        MaxHealth = 2;
    }

    protected override bool HasSuperArmour => (state == State.WindUp || state == State.Swoop) && !InterruptAttacks;
 
    protected override Color GetStateColour()
    {
        if (state == State.Aim) return AimColor;
        if (state == State.WindUp) return WindupColor;
        return baseColor;
    }
 
    protected override void OnInit()
    {
        defaultGrav = 0f;      // it flies, so ResetToStart must not give gravity back
        rb.gravityScale = 0f;
        if (PPrefab == null) Debug.LogWarning($"{name}: no projectile prefab assigned", this);
    }
 
    protected override void OnHurt()
    {
        if (state == State.Aim && InterruptAttacks)
        {
            state = State.Chase;
            shotTimer = shotCooldown * .5f;
        }
        else if ((state == State.WindUp || state == State.Swoop) && InterruptAttacks)
        {
            BeginRecover(); // knockback from the base class then sends it flying
        }
        else if (state == State.Patrol && player != null)
        {
            state = State.Chase;
            NoticePlayer();
        }
    }
 
    protected override void OnReset()
    {
        state = State.Patrol;
        desiredVel = Vector2.zero;
        AimTimer = shotTimer = windTimer = swoopTimer = SCooldown = RecoverTimer = 0f;
        hitThisSwoop = false;
    }
 
    protected override int GetAnimState()
    {
        if (state == State.Aim) return ShootS;
        if (state == State.WindUp) return WindupS;
        if (state == State.Swoop) return SwoopS;
        return FlyS;
    }

    protected override void Think(float dist, bool canSee)
    {
        if (shotTimer > 0f) shotTimer -= Time.deltaTime;
        if (SCooldown > 0f) SCooldown -= Time.deltaTime;
        Vector2 pos = transform.position;
 
        switch (state)
        {
            case State.Patrol:
            {
                // drift left/right around the start point, turn at either end or at a wall
                float bob = Mathf.Sin(Time.time * BobSpeed) * BobAmount;
                desiredVel = new Vector2(facing * Pspeed, Mathf.Clamp((startPos.y + bob - pos.y) * 3f, -Pspeed, Pspeed));
                bool atEnd = (facing > 0f && pos.x >= startPos.x + PatrolRange) || (facing < 0f && pos.x <= startPos.x - PatrolRange);
                if (turnTimer <= 0f && (atEnd || HasWallAhead(facing))) Flip();
 
                if (canSee)
                {
                    state = State.Chase;
                    NoticePlayer();
                }
                break;
            }
 
            case State.Chase:
            {
                if (!TrackTarget(dist, out bool seeing))
                {
                    state = State.Patrol;
                    break;
                }
                if (stunTimer > 0f) { desiredVel = Vector2.zero; break; }
 
                FaceTarget(lastSeen.x - pos.x);
 
                if (seeing)
                {
                    // close enough and ready -> swoop
                    if (SwoopCooldown <= 0f && dist <= SRange)
                    {
                        BeginWindup();
                        break;
                    }
                    // otherwise shoot from range
                    bool holdFire = dist <= SRange && !ShootWhenClose;
                    if (shotTimer <= 0f && dist <= ShotRange && !holdFire)
                    {
                        AimTimer = aimTime;
                        desiredVel = Vector2.zero;
                        state = State.Aim;
                        break;
                    }
                }
                desiredVel = HoverVelocity(pos);
                break;
            }
 
            case State.Aim:
            {
                desiredVel = Vector2.zero; // hangs in the air while it lines up the shot
                if (player != null) FaceTarget(player.position.x - pos.x);
                AimTimer -= Time.deltaTime;
                if (AimTimer <= 0f)
                {
                    Fire();
                    shotTimer = shotCooldown;
                    state = State.Chase;
                }
                break;
            }
 
            case State.WindUp:
            {
                FaceTarget(swoopTarget.x - pos.x);
                desiredVel = -swoopDir * WindupBackSpeed; // pulls back slightly, telegraphing the dive
                windTimer -= Time.deltaTime;
                if (windTimer <= 0f) StartSwoop();
                break;
            }
 
            case State.Swoop:
            {
                swoopTimer -= Time.deltaTime;
                SwoopHitCheck();
                if (swoopTimer <= 0f) BeginRecover(); // distance / obstacle stops are checked in FixedUpdate
                break;
            }
 
            case State.Recover:
            {
                // curve back up towards the hover spot (the Accel limit is what makes it a smooth arc)
                if (player != null) lastSeen = player.position;
                desiredVel = HoverVelocity(pos);
                RecoverTimer -= Time.deltaTime;
                bool arrived = desiredVel == Vector2.zero;
                if (arrived || RecoverTimer <= 0f)
                {
                    state = State.Chase;
                    NoticePlayer();
                }
                break;
            }
        }
    }
 
    protected override void FixedUpdate()
    {
        if (state == State.Swoop)
        {
            if (ShouldStopSwoop()) { BeginRecover(); return; }
            rb.linearVelocity = swoopDir * SSpeed;
            return;
        }
        if (stunTimer > 0f) return; // let the knockback play out
 
        Vector2 v = AvoidObstacles(desiredVel);
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, v, accel * Time.fixedDeltaTime);
    }
 
    // ---------- movement helpers ----------
 
    // velocity that takes it towards its hover spot above / beside the player, slowing as it arrives
    Vector2 HoverVelocity(Vector2 pos)
    {
        float side = pos.x >= lastSeen.x ? 1f : -1f; // stays on whichever side it's already on
        float bob = Mathf.Sin(Time.time * BobSpeed) * BobAmount;
        Vector2 target = lastSeen + new Vector2(side * HoverDistanceX, HoverHeight + bob);
        Vector2 to = target - pos;
        if (to.magnitude < .25f) return Vector2.zero;
        return to.normalized * Mathf.Min(Aspeed, to.magnitude * 3f);
    }
 
    // zero out any part of the velocity that is pushing into ground / walls
    Vector2 AvoidObstacles(Vector2 v)
    {
        Vector2 pos = transform.position;
        if (v.x != 0f && Physics2D.OverlapCircle(pos + new Vector2(Mathf.Sign(v.x) * ObstacleCheckDist, 0f), checkRadius, groundLayer)) v.x = 0f;
        if (v.y != 0f && Physics2D.OverlapCircle(pos + new Vector2(0f, Mathf.Sign(v.y) * ObstacleCheckDist), checkRadius, groundLayer)) v.y = 0f;
        return v;
    }
 
    // ---------- swoop ----------
 
    void BeginWindup()
    {
        swoopTarget = player.position; // locked now so the player can dodge it
        Vector2 d = swoopTarget - (Vector2)transform.position;
        swoopDir = d.sqrMagnitude > .01f ? d.normalized : Vector2.down;
        windTimer = SWindup;
        desiredVel = Vector2.zero;
        state = State.WindUp;
    }
 
    void StartSwoop()
    {
        Vector2 pos = transform.position;
        Vector2 d = swoopTarget - pos;
        swoopDir = d.sqrMagnitude > .01f ? d.normalized : Vector2.down;
        swoopStart = pos;
        SwoopMaxDist = d.magnitude + SOvershoot;
        swoopTimer = SwoopMaxDist / SSpeed + .2f;
        hitThisSwoop = false;
        state = State.Swoop;
    }
 
    // checked every physics step (before moving) so a fast dive can't skip through the ground
    bool ShouldStopSwoop()
    {
        Vector2 pos = transform.position;
        if (Vector2.Distance(pos, swoopStart) >= SwoopMaxDist) return true;
        if (Physics2D.OverlapCircle(pos + swoopDir * ObstacleCheckDist, checkRadius, groundLayer)) return true;
        return false;
    }
 
    // ends the dive but keeps the current velocity, so Accel bends it into a curve back up
    void BeginRecover()
    {
        state = State.Recover;
        SCooldown = SwoopCooldown;
        RecoverTimer = SRecoverTime;
    }
 
    void SwoopHitCheck()
    {
        if (hitThisSwoop || player == null || playerHealth == null) return;
        Vector2 d = (Vector2)player.position - (Vector2)transform.position;
        if (Mathf.Abs(d.x) > HitBoxHalfSize.x || Mathf.Abs(d.y) > HitBoxHalfSize.y) return;
        if (playerCombat != null && (playerCombat.Immune || playerCombat.IsDead)) return; // player can dash through it
        playerHealth.TakeDamage(SDamage, transform.position, MajorEnemy);
        hitThisSwoop = true;
    }
 
    // ---------- shooting ----------
 
    void Fire()
    {
        if (PPrefab == null || player == null) return;
        Vector2 origin = (Vector2)transform.position + new Vector2(MuzzleOffset.x * facing, MuzzleOffset.y);
        Vector2 dir = ((Vector2)player.position - origin).normalized;
        EnemyProjectile proj = Instantiate(PPrefab, origin, Quaternion.identity);
        proj.Launch(dir, PSpeed, PDamage, MajorEnemy);
    }
 
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Vector2 pos = transform.position;
        Vector2 home = Application.isPlaying ? startPos : pos;
 
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, ShotRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(pos, SRange);
        Gizmos.DrawWireCube(pos, new Vector3(HitBoxHalfSize.x * 2f, HitBoxHalfSize.y * 2f, 0f));
 
        Gizmos.color = Color.blue; // patrol range
        Gizmos.DrawLine(home + Vector2.left * PatrolRange, home + Vector2.right * PatrolRange);
 
        Gizmos.color = Color.white; // hover spots relative to the player's side
        Gizmos.DrawWireSphere(pos + new Vector2(MuzzleOffset.x * facing, MuzzleOffset.y), .1f);
        Gizmos.DrawLine(pos, pos + new Vector2(ObstacleCheckDist, 0f));
        Gizmos.DrawLine(pos, pos + new Vector2(-ObstacleCheckDist, 0f));
        Gizmos.DrawLine(pos, pos + new Vector2(0f, ObstacleCheckDist));
        Gizmos.DrawLine(pos, pos + new Vector2(0f, -ObstacleCheckDist));
    }

  
}
