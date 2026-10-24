using System;
using UnityEngine;

public class ShooterEnemy : EnemyBase
{
    // P/Pro - projectile
    enum State {patrol, chase, aim}
    State state = State.patrol;

    float aimTimer;
    float shotTimer;
    float freezeTimer;

    [Header("spacing")]
    public float PrefRange = 6f;
    public float RangeBuffer = 1f;
    public bool CanRetreat = true;
    public float MinRange = 3.5f;

    [Header("Shooting")]
    public EnemyProjectile PPrefab;
    public float ShotRange = 10f;
    public float AimTime = .5f;
    public float ShotCoolDown = 1.8f;
    public float RecoverTime = .3f;
    public float ProSpeed = 9f;
    public int PDamage = 2;
    public Vector2 MuzzleOffset = new Vector2(0.6f, 0.1f);
    public bool InterruptAim = true;
    public Color AimColor = new Color(1f, .9f, .4f);

    static int IdleS = Animator.StringToHash("ShooterIdle");
    static int WalkS = Animator.StringToHash("ShooterWalk");
    static int ShootS = Animator.StringToHash("ShooterShoot");

    void Reset()
    {
        Pspeed = 3f;
        Aspeed = 4f;
        Drange = 12f;
        LoseInterest = 15f;
        MaxHealth = 2;
    }
    protected override Color GetStateColour() => state == State.aim ? AimColor : baseColor;
    protected override void OnInit()
    {
        if (PPrefab == null) Debug.LogWarning("No Projectile assigned");
    }

    protected override void OnHurt()
    {
        if (state == State.aim && InterruptAim)
        {
            state = State.chase;
            shotTimer = ShotCoolDown * .5f;
        }
        else if (state == State.patrol && player != null)
        {
            state = State.chase;
            NoticePlayer();
        }
    }
    protected override void OnReset()
    {
        state = State.patrol;
        moveDir = facing;
        aimTimer = shotTimer = freezeTimer;
    }
    protected override int GetAnimState()
    {
        if (state == State.aim) return ShootS;
        if(currentSpeed != 0f) return WalkS;
        return IdleS;
    }

    protected override void Think(float dist, bool canSee)
    {
        if (shotTimer > 0f) shotTimer -= Time.deltaTime;
        if (freezeTimer > 0f) freezeTimer -= Time.deltaTime;
        Vector2 pos = transform.position;

        switch (state)
        {
            case State.patrol:
            {
                if (canSee)
                {
                    state = State.chase;
                    NoticePlayer();
                }
                break;
            }
            case State.chase:
            {
                if (!TrackTarget(dist, out bool seeing))
                {
                    state = State.patrol;
                }
                currentSpeed = 0f;
                if (stunTimer > 0f || freezeTimer > 0f) break;

                float dx = lastSeen.x - pos.x;
                FaceTarget(dx);
                float absDx = Mathf.Abs(dx);

                bool wantsMove = false;
                if (absDx > PrefRange + RangeBuffer)
                {
                    moveDir = facing;
                    wantsMove = true;
                }
                else if (CanRetreat && absDx < MinRange)
                {
                    moveDir = -facing;
                    wantsMove = true;
                }
                if (wantsMove && CanMove(moveDir)) currentSpeed = Aspeed;

                if (seeing && shotTimer <= 0f && dist <= ShotRange)
                {
                    currentSpeed = 0f;
                    aimTimer = AimTime;
                    state = State.aim;
                }
                break;
            }

            case State.aim:
            {
                currentSpeed = 0f;
                if (player != null) FaceTarget(player.position.x - pos.x);
                aimTimer -= Time.deltaTime;
                if (aimTimer <= 0f)
                {
                    Shoot();
                    shotTimer = ShotCoolDown;
                    freezeTimer = RecoverTime;
                    state = State.chase;
                }
                break;
            }
        }
    }

    void Shoot()
    {
        if (PPrefab == null || player == null) return;
        Vector2 origin = (Vector2)transform.position + new Vector2(MuzzleOffset.x * facing, MuzzleOffset.y);
        Vector2 dir = ((Vector2)player.position - origin).normalized;
        EnemyProjectile proj = Instantiate(PPrefab, origin, Quaternion.identity);
        proj.Launch(dir, ProSpeed, PDamage, MajorEnemy);
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Vector2 pos = transform.position;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, ShotRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(pos, PrefRange);
        Gizmos.DrawWireSphere(pos, MinRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(pos + new Vector2(MuzzleOffset.x * facing, MuzzleOffset.y), .1f);
    }
}
// wirtten by pradhyot.N 24/10/2026
//Exw L idowhu rq wkh iruhwkrxjkw
//L nhhs rq uxqqlqj olnh d fklfnhq zlwk lwv khdg fxw rii
//Dfkurpdwlf wzlvwhg dhurqdxw
//D elugeudlq, edeb, L grq'w nqrz zkhq L'p vxssrvhg wr vwrs
//Klw ph zlwk wkh iods grzq, fudvk lqwr wkh vriw jurxqg (Irxo!)
//Dlq'w qr ihoorz ihdwkhuv idoolq' iru d iuhdn (Vr zhdn)
//Vr sxw pb qhfn xs rq wkh fkrsslqj eorfn
//D elugeudlq, edeb, L grq'w wklqn pb khdg lv frplq' rii
//Edeb, L grq'w nqrz zkhq L'p vxssrvhg wr vwrs
//Edeb, L grq'w nqrz zkhq L'p vxssrvhg wr vwrs! - elugEudlq eb Mdplh S
// Shift 3