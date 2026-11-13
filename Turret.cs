using UnityEngine;

public class Turret : MonoBehaviour, IDamageable, IResettable
{
    enum State { Idle, Detected, Tracking, Charging, Cooldown, ReturningToRest, ShuttingOff, Disabled }
    State state = State.Idle;

    [Header("Head tracking")]
    public Transform headPivot;                 // child that rotates (do NOT animate this in the Animator)
    public SpriteRenderer headRenderer;
    public float headTurnSpeed = 360f;
    public float headAngleOffset = 0f;          // 0 = art points RIGHT, -90 = UP, 180 = LEFT
    public bool flipHeadWhenAimingLeft = true;
    public bool limitHeadAngle = false;
    public float restAngle = 0f;
    public float maxAngleFromRest = 90f;

    [Header("Detection")]
    public float trackRange = 12f;
    public float shootRange = 8f;
    public bool needLOS = true;
    public LayerMask groundLayer;               // used for LOS + laser only
    public float detectTime = .4f;              // length of the "spotted you" reaction

    [Header("Shooting")]
    public EnemyProjectile projectilePrefab;
    public Transform firePoint;                 // optional, falls back to the head pivot
    public float firePointDistance = .8f;
    public float chargeTime = .6f;
    public bool trackDuringCharge = true;       // false = aim locks when charge starts
    public float fireCooldown = 1.5f;
    public float projectileSpeed = 9f;
    public int damage = 1;
    public bool majorDamage = false;

    [Header("Line of sight (laser)")]
    public bool showLOS = true;
    public LineRenderer laser;                  // optional, one is created automatically if empty
    public Color trackLaserColor = new Color(1f, .2f, .2f, .25f);
    public Color chargeLaserColor = new Color(1f, .2f, .2f, 1f);
    public float trackLaserWidth = .03f;
    public float chargeLaserWidth = .1f;
    public int laserSortingOrder = 5;

    [Header("Animation")]
    public Animator anim;                       // on the body, not on headPivot
    public GameObject disabledSmoke;
    public float shootAnimTime = .3f;
    static int IdleS = Animator.StringToHash("TurretIdle");
    static int ChargeS = Animator.StringToHash("TurretCharge");
    static int ShootS = Animator.StringToHash("TurretShoot");
    static int DisabledS = Animator.StringToHash("TurrentDown");
    static int CooldownS = Animator.StringToHash("TurrentCooldown");
    static int ShutOffS = Animator.StringToHash("TurretDisabled");
    public float shutOffTime = 1f;

    [Header("Health & hit reaction")]
    public int maxHealth = 3;
    public float hitFlashTime = .15f;
    public Color hitColor = Color.red;


    Transform player;
    SpriteRenderer[] renderers;
    Color[] baseColors;
    float timer, flashTimer, shootAnimTimer, aimAngle;
    int health;
    int currentAnim = -1;
    int tint = -1;          // 0 = normal, 1 = charging, 2 = hit flash
    bool laserLit;          // true while the laser is not at its resting look

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        IgnorePlayerCollisions();
        SetDisabledSmoke(false);

        if (anim == null) anim = GetComponentInChildren<Animator>();

        renderers = GetComponentsInChildren<SpriteRenderer>();
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;

        SetupLaser();
        health = maxHealth;
        aimAngle = restAngle;
        ApplyHeadRotation();
        ResetManager.Register(this);
    }

    void IgnorePlayerCollisions()
    {
        if (player == null) return;

        Collider2D[] turretColliders = GetComponentsInChildren<Collider2D>();
        Collider2D[] playerColliders = player.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D turretCollider in turretColliders)
        {
            foreach (Collider2D playerCollider in playerColliders)
            {
                Physics2D.IgnoreCollision(turretCollider, playerCollider);
            }
        }
    }

    void OnDestroy()
    {
        ResetManager.Unregister(this);
    }

    void SetupLaser()
    {
        if (!showLOS) return;
        if (laser == null)
        {
            // fallback so it works with zero setup; assign your own LineRenderer + material for builds
            laser = gameObject.AddComponent<LineRenderer>();
            laser.material = new Material(Shader.Find("Sprites/Default"));
        }
        laser.useWorldSpace = true;
        laser.positionCount = 2;
        laser.sortingOrder = laserSortingOrder;
        SetLaserLook(0f);
        laser.enabled = false;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (state == State.ReturningToRest)
        {
            if (headTurnSpeed > 0f)
                TurnHeadTo(restAngle);
            else
            {
                aimAngle = restAngle;
                ApplyHeadRotation();
            }

            if (Mathf.Approximately(Mathf.DeltaAngle(aimAngle, restAngle), 0f))
            {
                state = State.ShuttingOff;
                timer = 0f;
                UpdateAnimation();
            }
            return;
        }
        if (state == State.ShuttingOff)
        {
            timer += dt;
            if (timer >= shutOffTime)
            {
                state = State.Disabled;
                UpdateAnimation();
                SetDisabledSmoke(true);
                enabled = false;
            }
            return;
        }
        if (player == null) return;
        if (flashTimer > 0f) flashTimer -= dt;
        if (shootAnimTimer > 0f) shootAnimTimer -= dt;

        // cheap distance test first, the raycast only runs when the player is actually in range
        Vector2 origin = HeadPos;
        Vector2 toPlayer = (Vector2)player.position - origin;
        float sqr = toPlayer.sqrMagnitude;
        bool inTrack = false, inShoot = false;
        if (sqr <= trackRange * trackRange && (!needLOS || HasLOS(origin)))
        {
            inTrack = true;
            inShoot = sqr <= shootRange * shootRange;
        }

        switch (state)
        {
            case State.Idle:
                TurnHeadTo(restAngle);
                if (inTrack) { state = State.Detected; timer = 0f; }
                break;

            case State.Detected:
                TurnHeadTo(AngleOf(toPlayer));
                if (!inTrack) state = State.Idle;
                else
                {
                    timer += dt;
                    if (timer >= detectTime) { timer = 0f; state = State.Tracking; }
                }
                break;

            case State.Tracking:
                TurnHeadTo(AngleOf(toPlayer));
                if (!inTrack) state = State.Idle;
                else if (inShoot) { state = State.Charging; timer = 0f; }
                break;

            case State.Charging:
                if (!inShoot) { timer = 0f; state = State.Tracking; break; }
                if (trackDuringCharge) TurnHeadTo(AngleOf(toPlayer));
                timer += dt;
                if (timer >= chargeTime)
                {
                    Shoot();
                    timer = 0f;
                    state = State.Cooldown;
                }
                break;

            case State.Cooldown:
                TurnHeadTo(AngleOf(toPlayer));
                timer += dt;
                if (timer >= fireCooldown)
                {
                    timer = 0f;
                    state = inTrack ? State.Tracking : State.Idle;
                }
                break;
        }

        UpdateLaser();
        UpdateAnimation();
        SetTint(flashTimer > 0f ? 2 : state == State.Charging ? 1 : 0);
    }

    void UpdateAnimation()
    {
        if (anim == null) return;

        int newAnim;
        switch (state)
        {
            case State.Charging:    newAnim = ChargeS; break;
            case State.Cooldown:    newAnim = shootAnimTimer > 0f ? ShootS : CooldownS; break;
            case State.ShuttingOff: newAnim = ShutOffS; break;
            case State.Disabled:    newAnim = DisabledS; break;
            default:                newAnim = IdleS; break;   // Idle, Detected and Tracking all use the idle clip
        }

        if (newAnim == currentAnim || !anim.HasState(0, newAnim)) return;
        anim.Play(newAnim, 0, 0f);
        currentAnim = newAnim;
    }


    void UpdateLaser()
    {
        if (laser == null) return;

        bool show = showLOS && (state == State.Detected || state == State.Tracking || state == State.Charging);
        if (laser.enabled != show) laser.enabled = show;
        if (!show) return;

        float rad = aimAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        Vector2 start = firePoint != null ? (Vector2)firePoint.position : HeadPos;

        RaycastHit2D hit = Physics2D.Raycast(start, dir, trackRange, groundLayer);
        laser.SetPosition(0, start);
        laser.SetPosition(1, hit.collider != null ? hit.point : start + dir * trackRange);

        float t = state == State.Charging ? Mathf.Clamp01(timer / chargeTime) : 0f;
        if (t > 0f || laserLit)
        {
            SetLaserLook(t);
            laserLit = t > 0f;
        }
    }

    void SetLaserLook(float t)
    {
        Color c = Color.Lerp(trackLaserColor, chargeLaserColor, t);
        float w = Mathf.Lerp(trackLaserWidth, chargeLaserWidth, t);
        laser.startColor = c;
        laser.endColor = c;
        laser.startWidth = w;
        laser.endWidth = w;
    }

    Vector2 HeadPos => headPivot != null ? (Vector2)headPivot.position : (Vector2)transform.position;

    float AngleOf(Vector2 dir) => Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

    void TurnHeadTo(float target)
    {
        if (limitHeadAngle)
        {
            float diff = Mathf.DeltaAngle(restAngle, target);
            target = restAngle + Mathf.Clamp(diff, -maxAngleFromRest, maxAngleFromRest);
        }
        float next = Mathf.MoveTowardsAngle(aimAngle, target, headTurnSpeed * Time.deltaTime);
        if (Mathf.Approximately(next, aimAngle)) return; // already on target, skip the transform write
        aimAngle = next;
        ApplyHeadRotation();
    }

    void ApplyHeadRotation()
    {
        if (headPivot != null) headPivot.rotation = Quaternion.Euler(0f, 0f, aimAngle + headAngleOffset);
        if (headRenderer != null && flipHeadWhenAimingLeft)
            headRenderer.flipY = Mathf.Cos(aimAngle * Mathf.Deg2Rad) < 0f;
    }

    bool HasLOS(Vector2 origin) => !Physics2D.Linecast(origin, player.position, groundLayer);

    void Shoot()
    {
        if (projectilePrefab == null) { Debug.LogWarning("Turret has no projectile assigned", this); return; }

        float rad = aimAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        Vector2 spawn = firePoint != null ? (Vector2)firePoint.position : HeadPos + dir * firePointDistance;

        Instantiate(projectilePrefab, spawn, Quaternion.identity).Launch(dir, projectileSpeed, damage, majorDamage);
        shootAnimTimer = shootAnimTime;
    }

    public void TakeDamage(int amount, Vector2 from, bool major = false)
    {
        if (state == State.Disabled || state == State.ReturningToRest || state == State.ShuttingOff) return;

        health -= amount;
        flashTimer = hitFlashTime;
        if (health > 0) return;

        health = 0;
        state = State.ReturningToRest;
        timer = 0f;
        flashTimer = 0f;
        if (laser != null) laser.enabled = false;
        SetTint(0);
    }

    public void ResetToStart()
    {
        enabled = true;
        health = maxHealth;
        state = State.Idle;
        timer = flashTimer = shootAnimTimer = 0f;
        SetDisabledSmoke(false);
        aimAngle = restAngle;
        ApplyHeadRotation();
        if (laser != null) laser.enabled = false;
        currentAnim = 0;     // forces the idle anim to replay
        UpdateAnimation();
        SetTint(0);
    }

    void SetDisabledSmoke(bool visible)
    {
        if (disabledSmoke != null && disabledSmoke.activeSelf != visible)
            disabledSmoke.SetActive(visible);
    }

    void SetTint(int t)
    {
        if (t == tint || renderers == null) return;
        tint = t;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector2 pos = headPivot != null ? (Vector2)headPivot.position : (Vector2)transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, trackRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, shootRange);
    }
}

// holy shit 300 lines?? - pradhyot.N (5/11/26) (its 4 am btw)
// im tired of writing this
// You're tailor-made for this era
// with a vulnerability to your appearance
// Fooled by this truthful acting
// your heart beats so very loudly
// Your extra lives are quickly being lost ;-;
// Your durability worn away
// Dodging matters right in front of you
// You've got your hands full just living! - translated lyrics of "Mesmerizer" by 32ki