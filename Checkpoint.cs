using UnityEngine;

public class Checkpoint : MonoBehaviour
{
   public Transform spawnPoint;
   public bool healOnActivate = true;
   public SpriteRenderer marker;
   public Color activeColour = Color.green;
   bool activated;
    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerCombat p = other.GetComponentInParent<PlayerCombat>();
        if (p == null) return;
        Vector2 pos = spawnPoint != null ? (Vector2)spawnPoint.position : (Vector2)transform.position;
        p.SetCheckPoint(pos);

        if (!activated)
        {
            activated = true;
            if (healOnActivate) p.Heal(p.maxHealth);
            if(marker != null) marker.color = activeColour;
        }
    }
}
