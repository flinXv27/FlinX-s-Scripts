using UnityEngine;


public class HealthCollectible : MonoBehaviour
{
    public float speedCollectible = 3.0f;
    public int HealingAmount = 1;
    public float horizontal = 0.2f;
    public bool movingUp = true;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
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
        PlayerController controller = other.GetComponent<PlayerController>();
        if (controller != null && controller.health < controller.maxHealth)
        {
            controller.ChangeHealth(HealingAmount);
            Destroy(gameObject); 
        }
    }
}

// made by pradhyot.N 12/10/2026 