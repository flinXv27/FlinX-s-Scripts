using UnityEngine;


public class ChargerEnemy : EnemyBase
{
    // C = charging or stuff that link to it
    // S = states
    enum State {patrol, Chase, Windup, Charge, Recover}
    State state = State.patrol;
    
    float WindTimer;
    float Ctimer;
    float CoolDownTimer;
    float RecoverTimer;
    bool hitThisCharge;
    Vector2 CDir;

    [Header("Charging")]
    public float CRange = 8f;
    public float AlignTolerance = 1f;
    public bool AllowVert = true;
    public float WindTime = .6f;
    public float CSpeed = 25f;
    public float CTime = .4f;
    public float CCooldown = 1.5f;
    public float RecoverTime = .8f;
    public int CDamage = 1;
    public Vector2 HitBoxHalfSize = new Vector2(.6f, .6f);
    public float CWallDist = .6f;
    public bool StopAtLedge = true;
    public bool InterruptWind = false; // if set to true an attack can reset the charge
    public Color WindColor = new Color(1f, .6f, .2f);

    static int IdleS = Animator.StringToHash("chargerIdle");
    static int WalkS = Animator.StringToHash("chargerWalk");
    static int WindupS = Animator.StringToHash("chargerWindup");
    static int ChargeS = Animator.StringToHash("chargerCharge");

    protected override bool IsImmune => state == State.Charge; // immune whilst dashing
    protected override bool HasSuperArmour => state == State.Windup && !InterruptWind;
    protected override Color GetStateColour() => state == State.Windup ? WindColor : baseColor;

    protected override void OnHurt()
    {
        if(state == State.Windup)
        {
            state = State.Chase;
            CoolDownTimer = CCooldown * .5f;
        }
        else if (state == State.patrol && player != null)
        {
            state = State.Chase;
            NoticePlayer();
        }
    }

    protected override void OnReset()
    {
        state = State.patrol;
        WindTimer = Ctimer = CoolDownTimer = RecoverTimer = 0f;
        hitThisCharge = false;
    }

    protected override int GetAnimState()
    {
        if (state == State.Charge) return ChargeS;
        if (state == State.Windup) return WindupS;
        if (currentSpeed != 0f) return WalkS;
        return IdleS;
    }

    bool TryPickDirection(out Vector2 dir)
    {
        dir = Vector2.zero;
        Vector2 d = (Vector2)player.position - (Vector2)transform.position;
        bool horizontalOK = Mathf.Abs(d.y) <= AlignTolerance && Mathf.Abs(d.x) <= CRange;
        bool VerticalOK = AllowVert && Mathf.Abs(d.x) <= AlignTolerance && Mathf.Abs(d.y) <= CRange;

        if (horizontalOK && (!VerticalOK || Mathf.Abs(d.x) >= Mathf.Abs(d.y))) dir = new Vector2(Mathf.Sign(d.x), 0f);
        else if (VerticalOK) dir = new Vector2(0f, Mathf.Sign(d.y));
        else return false;
        return true;
    }
    bool ShouldStopCharge()
    {
        Vector2 pos = transform.position;
        if (Physics2D.OverlapCircle(pos + CDir * CWallDist, checkRadius, groundLayer)) return true; // wall / ceiling / floor
        if (StopAtLedge && grounded && CDir.x != 0f && !HasGroundAhead(CDir.x)) return true;         // ledge
        return false;
    }
 
    void StartCharge()
    {
        state = State.Charge;
        Ctimer = CTime;
        hitThisCharge = false;
        rb.gravityScale = 0f; // so a sideways charge doesn't droop
    }
 
    void EndCharge()
    {
        state = State.Recover;
        rb.gravityScale = defaultGrav;
        rb.linearVelocity = Vector2.zero;
        CoolDownTimer = CCooldown;
        RecoverTimer = RecoverTime;
    }
 
    void CHitCheck()
    {
        if (hitThisCharge || player == null || playerHealth == null) return;
        Vector2 d = (Vector2)player.position - (Vector2)transform.position;
        if (Mathf.Abs(d.x) > HitBoxHalfSize.x || Mathf.Abs(d.y) > HitBoxHalfSize.y) return;
        if (playerCombat != null && (playerCombat.Immune || playerCombat.IsDead)) return; // player can dash through it
        playerHealth.TakeDamage(CDamage, transform.position, MajorEnemy);
        hitThisCharge = true;
    }

    protected override void Think(float dist, bool canSee)
    {
        if (CoolDownTimer > 0f) CoolDownTimer -= Time.deltaTime;
        Vector2 pos = transform.position;

        switch (state)
        {
            case State.patrol:
            {
                PatrolStep();
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
                    state = State.patrol;
                    break;
                }
                if (stunTimer > 0f)
                {
                    currentSpeed = 0f;
                    break;
                }
                float dx = lastSeen.x - pos.x;
                FaceTarget(dx);
                
                if (seeing && CoolDownTimer <= 0f && TryPickDirection(out Vector2 dir))
                {
                    CDir = dir;
                    WindTimer = WindTime;
                    currentSpeed = 0f;
                    if (CDir.x != 0f)
                    {
                        facing = CDir.x;
                        applyFacing();
                        state = State.Windup;
                        break;
                    }
                    
                }
                bool alignedX = Mathf.Abs(dx) < .5f;
                    currentSpeed = (alignedX || !CanMove(facing)) ? 0f : Aspeed;
                    break;
            }
            case State.Windup:
            {
                currentSpeed = 0f;
                WindTimer -= Time.deltaTime;
                if (WindTimer <= 0f) StartCharge();
                break;
            }
            case State.Charge:
            {
                currentSpeed = 0f;
                Ctimer -= Time.deltaTime;
                CHitCheck();
                if (Ctimer <= 0f) EndCharge();
                break;
            }
            case State.Recover:
            {
                currentSpeed = 0f;
                RecoverTimer -= Time.deltaTime;
                if (RecoverTimer <= 0f)
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
        if (state == State.Charge)
        {
            if (ShouldStopCharge())
            {
                EndCharge();
                return;
            }
            rb.linearVelocity = CDir * CSpeed;
            return;
        }
        base.FixedUpdate();
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Vector2 pos = transform.position;
 
        // charge lanes (the 4 directions it can charge in)
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(pos, new Vector3(CRange * 2f, AlignTolerance * 2f, 0f));
        if (AllowVert) Gizmos.DrawWireCube(pos, new Vector3(AlignTolerance * 2f, CRange * 2f, 0f));
 
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(pos, new Vector3(HitBoxHalfSize.x * 2f, HitBoxHalfSize.y * 2f, 0f));
    }
}
// i love writing comments - pradhyot.N 24/10/26
//D fohyhu idnh lv mxvw dv jrrg dv wkh uhdo wklqj
//Vlqfh d idnh pxvw zrun wr nhhs rq lpsurylqj
//Doo durxqg wkh vrxqg ri brxu yrlfh vxuurxqgv ph
//Folqjlqj wr pb eudlq, wkh ulqjlqj zrq'w jr dzdb
//Pb idloxuhv duh rq glvsodb
//L'p wdnlqj wklv doo wkh zdb
//Wr pb judyh - yhuvh 1, krqhvwob eb Wkruqv
// shift 3