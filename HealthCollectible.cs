using UnityEngine;


public class HealthCollectible : MonoBehaviour
{
    public float speedCollectible = 3.0f;
    public int HealingAmount = 1;
    public float horizontal = 0.2f;
    public bool movingUp = true;

    void FixedUpdate()
    {
       if (movingUp)
       {
           horizontal += 0.2f * Time.deltaTime;
           if (horizontal >= 0.2f)
           {
               movingUp = false;
           }
       }
       else
       {
           horizontal -= 0.2f * Time.deltaTime;
           if (horizontal <= -0.2f)
           {
               movingUp = true;
           }
       }
       Vector2 position = transform.position;
       position.y += horizontal * speedCollectible * Time.deltaTime;
       transform.position = position;
    }
    public void OnTriggerEnter2D(Collider2D other)
    {
        PlayerCombat player = other.GetComponentInParent<PlayerCombat>();
        if (player == null || player.IsDead) return;

        playercon controller = other.GetComponentInParent<playercon>();
        if (controller == null) return;

        bool restoredHealth = player.Heal(HealingAmount);
        bool restoredDash = controller.RestoreDash();
        if (restoredHealth || restoredDash) Destroy(gameObject);
    }
}

// made by pradhyot.N 12/10/2026 