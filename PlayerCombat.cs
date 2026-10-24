using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[RequireComponent(typeof(playercon))]
public class PlayerCombat : MonoBehaviour, IDamageable
{
    // some common prefixes i use: (Note: will not be the same across scripts)
    // A = Attacking or variables that relate to attacking
    // P = Period, like grace period for example
    // S = animation states
    playercon pc;
    SpriteRenderer sr;
    Rigidbody2D rb;
    Color baseColor;
    Animator anim;
    
    [Header("Player Health")]
    public int maxHealth = 5;
    public int health;
    bool dead;
    public int Health => health; // for other scripts to read
    public bool IsDead => dead;

    [Header("I-frames")]
    public float hitIFrames = 1f;
    public float dashGraceP = .1f;
    public float flashTime = .08f;
    float iTimer;
    float gracePTimer;
    bool wasDashing;
    bool flashing;

    [Header("Attack")]
    public KeyCode Akey = KeyCode.Mouse0;
    public LayerMask enemyLayer;
    public int ADamage = 1;
    public float AReach = 1f;
    public float ARadius = .7f;
    public float ACooldown = .35f;
    public GameObject slash; // to show the attack
    public float slashLifeTime = .15f;
    float ATimer;

    [Header("attack anims")]
    public float AAnimTime = .25f;
    static int AttackS = Animator.StringToHash("MukAttack");
    static int AttackUpS = Animator.StringToHash("MukAttackUp");
    static int AttackDownS = Animator.StringToHash("MukAttackDown");
    int attackAnim;
    float AAnimTimer;

    [Header("Knockback")]
    public Vector2 HitKnockback = new Vector2(8f, 6f); //X means ur pushed sideways and Y is pushed up
    public float  KnockTime = .2f;

    [Header("death & checkpoint")]
    public FailPopup failPopup;
    public float failDelay = 1f;
    public float respawnDelay = 1f;
    public float RespawnIFrames = 1.5f;
    Vector2 checkpoint;
    static bool hasCheckpoint;
    static Vector2 savedCheckpoint;
    static string savedScene;
    public ScreenFade screenFade;
    public float fadeTime = .4f;
    public float blackHold = .15f;


    public bool Immune
    {
        get{return iTimer > 0f || gracePTimer > 0f || pc.Dashing;}
    }
    

    void Start()
    {
        pc = GetComponent<playercon>();
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        baseColor = sr.color;
        health = maxHealth;
        checkpoint = transform.position;
        if(hasCheckpoint && savedScene == SceneManager.GetActiveScene().name)
        {
            checkpoint = savedCheckpoint;
            transform.position = savedCheckpoint;
        }
    }

    void Update()
    {
        if (dead) return;
        if (iTimer > 0f) iTimer -= Time.deltaTime;
        if (ATimer > 0f) ATimer -= Time.deltaTime;
        if (AAnimTimer > 0f) AAnimTimer -= Time.deltaTime;
        bool dashing = pc.Dashing;
        if(wasDashing && !dashing) gracePTimer = dashGraceP;
        wasDashing = dashing;
        if (gracePTimer > 0f) gracePTimer -= Time.deltaTime;
        if (dashing) AAnimTimer = 0f;
        pc.attackState = AAnimTimer > 0f ? attackAnim : 0;
        UpdateFlash();

        if (Input.GetKey(Akey) && ATimer <= 0f && !dashing) attack();
    }
    
    void die(bool majour)
    {
        dead = true;
        sr.color = baseColor;
        pc.attackState = 0;
        pc.enabled = false;
        rb.linearVelocity = Vector2.zero;
        if (majour) Invoke(nameof(goToFailScreen), failDelay);
        else StartCoroutine(RespawnRoutine());
    }
 

    void goToFailScreen()
    {
        if (failPopup != null) failPopup.show();
        else Debug.LogWarning("No fail popup assigned");
    }

    public void SetCheckPoint(Vector2 pos)
    {
        checkpoint = pos;
        hasCheckpoint = true;
        savedCheckpoint = pos;
        savedScene = SceneManager.GetActiveScene().name;
    }

    void UpdateFlash()
    {
        if (iTimer > 0f)
        {
            bool visible = Mathf.FloorToInt(iTimer / flashTime) % 2 == 0;
            Color c = baseColor;
            c.a = visible ? 1f : .3f;
            sr.color = c;
            flashing = true;
        }
        else if (flashing)
        {
            sr.color = baseColor;
            flashing = false;
        }
    }

    public void TakeDamage(int amount, Vector2 from, bool majour = false)
    {
        if (dead || Immune) return;
        health = Mathf.Max(0, health - amount);
        Debug.Log("Health: "+ health + "/" + maxHealth);

        if (health == 0)
        {
            die(majour);
            return;
        }
        iTimer = hitIFrames;

        float side = transform.position.x >= from.x ? 1f: -1f;
        pc.Knockback(new Vector2(side * HitKnockback.x, HitKnockback.y), KnockTime);
    }

    public void Kill()
    {
        if (dead) return;
        health = 0;
        die(false);
    }

    public bool Heal(int amount)
    {
        if (dead || amount <= 0 || health >= maxHealth) return false;
        health = Mathf.Min(maxHealth, health + amount);
        return true;
    }

    Vector2 GetADir()
    {
        return pc.GetMouseOrMoveDirection();
    }

    System.Collections.IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        if (screenFade != null) yield return screenFade.FadeTo(1f, fadeTime);
        
        health = maxHealth;
        transform.position = checkpoint;
        rb.linearVelocity = Vector2.zero;
        pc.ResetState();
        ATimer = 0f;
        AAnimTimer = 0f;
        ResetManager.ResetAll();

        yield return new WaitForSeconds(blackHold);

        rb.linearVelocity = Vector2.zero;
        dead = false;
        pc.enabled = true;
        iTimer = RespawnIFrames;
        if (screenFade != null) yield return screenFade.FadeTo(0f, fadeTime);
    }

    int PickAAnim(Vector2 dir)
    {
        if (dir.y > .5f && anim.HasState(0, AttackUpS)) return AttackUpS;
        if (dir.y < -.5f && anim.HasState(0, AttackDownS)) return AttackDownS;
        return AttackS;
    }
    void attack()
    {
        ATimer = ACooldown;
        Vector2 dir = GetADir();
        Vector2 point = (Vector2)transform.position + dir * AReach;

        if (anim != null)
        {
            int hash = PickAAnim(dir);
            if (anim.HasState(0, hash))
            {
                attackAnim = hash;
                AAnimTimer = AAnimTime;
                anim.Play(hash, 0, 0f);
            }
        }
 
        if (slash != null)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            GameObject fx = Instantiate(slash, point, Quaternion.Euler(0f, 0f, angle), transform);
            Destroy(fx, slashLifeTime);
        }
 
        Collider2D[] hits = Physics2D.OverlapCircleAll(point, ARadius, enemyLayer);
        HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>(); // so an enemy with 2 colliders doesn't get hit twice
        foreach (Collider2D h in hits)
        {
            IDamageable target = h.GetComponentInParent<IDamageable>();
            if (target == null || !alreadyHit.Add(target)) continue;
            target.TakeDamage(ADamage, transform.position);
        }
    }
    void OnDrawGizmosSelected() // aren't gizmos just the best?
    {
        Gizmos.color = Color.red;
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Gizmos.DrawWireSphere((Vector2)transform.position + d * AReach, ARadius);
        }
    }
}
// I LOVE CAMEL SCRIPT RAHHHHH -pradhyot.N 23/10/2026
//Vr fdq zh zdqghu iru d vshoo dqg olyh lq sdudooho?
//L zdqw lw wr eh wuxh, wr eh olnh brx
//Pb khduw vlqjv d fkruxv rxw ri wxqh
//Dqg L frxog ohdyh lw rq d vkhoi ru nhhs lw wr pbvhoi
//Exw qrwklqj frxog frqfhdo wkh wklqjv L ihho
//Pb oryh, fdq brx whdfk ph wr eh uhdo? (Fdq brx whdfk ph wr eh uhdo?)
//泣きたくなっても それでも かき集め  - fkruxv ri pdfklqh oryh eb mdplh S
// (shift 3)
