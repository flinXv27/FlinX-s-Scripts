using UnityEngine;

// Put on an interactable object. Shows a sprite (e.g. a little "E" icon) above it when the player is near.
// Make the sprite a child object with a SpriteRenderer and drag its SpriteRenderer into 'icon'.
public class InteractIndicator : MonoBehaviour
{
    [Header("Sprite")]
    public SpriteRenderer icon;
    public Vector2 offset = new Vector2(0f, 1.2f);   // where it floats relative to this object

    [Header("Showing")]
    public float showDistance = 3f;
    public float fadeSpeed = 8f;
    [Tooltip("true = only appears when the player's interact raycast is actually pointing at this object")]
    public bool onlyWhenTargeted = false;

    [Header("Bobbing")]
    public float bobAmount = .08f;
    public float bobSpeed = 3f;

    Transform player;
    PlayerInteractor interactor;
    float alpha;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            interactor = p.GetComponent<PlayerInteractor>();
        }
        if (icon == null) { Debug.LogWarning($"{name}: InteractIndicator has no icon assigned", this); enabled = false; return; }
        SetAlpha(0f);
    }

    void Update()
    {
        if (player == null) return;

        bool show = !DialogueManager.IsOpen && IsNear();
        if (show && onlyWhenTargeted)
            show = interactor != null && interactor.IsTargeting(gameObject);

        alpha = Mathf.MoveTowards(alpha, show ? 1f : 0f, fadeSpeed * Time.unscaledDeltaTime);
        SetAlpha(alpha);

        if (alpha > 0f)
        {
            float bob = Mathf.Sin(Time.unscaledTime * bobSpeed) * bobAmount;
            icon.transform.position = (Vector2)transform.position + offset + Vector2.up * bob;
        }
    }

    bool IsNear()
    {
        return ((Vector2)player.position - (Vector2)transform.position).sqrMagnitude <= showDistance * showDistance;
    }

    void SetAlpha(float a)
    {
        Color c = icon.color;
        c.a = a;
        icon.color = c;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, showDistance);
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere((Vector2)transform.position + offset, .1f);
    }
}
// made by pradhyot. N