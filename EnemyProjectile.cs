using UnityEngine;

// Put this on a projectile prefab with: Rigidbody2D, a Collider2D set to "Is Trigger", and a sprite.
// Rigidbody2D gets set to Kinematic / no gravity automatically.
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyProjectile : MonoBehaviour, IResettable
{
    public LayerMask groundLayer;   // what destroys the projectile (do NOT include the player or enemy layers)
    public float lifetime = 4f;

    Rigidbody2D rb;
    int damage;
    bool major;
    bool registered;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
    }

    public void Launch(Vector2 dir, float speed, int dmg, bool majorHit)
    {
        damage = dmg;
        major = majorHit;
        rb.linearVelocity = dir * speed;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        ResetManager.Register(this); // so projectiles vanish when the player respawns
        registered = true;
        Destroy(gameObject, lifetime);
    }

    void OnDestroy()
    {
        if (registered) ResetManager.Unregister(this);
    }

    public void ResetToStart()
    {
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerCombat p = other.GetComponentInParent<PlayerCombat>();
        if (p != null)
        {
            if (p.IsDead || p.Immune) return; // flies through a dashing / i-framed player
            p.TakeDamage(damage, transform.position, major);
            Destroy(gameObject);
            return;
        }

        if ((groundLayer.value & (1 << other.gameObject.layer)) != 0) Destroy(gameObject);
    }
}

