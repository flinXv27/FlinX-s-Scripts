using TMPro;
using UnityEngine;

// Put on the player. Casts a short ray/circle in the facing direction, shows a prompt
// when an IInteractable is in front, and talks to it when you press the key.
[RequireComponent(typeof(playercon))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Raycast")]
    public LayerMask interactLayer;          // The NPC layer/ interactibe objects layer
    public float range = 1.5f;
    public float castRadius = .3f;           // circle cast = more forgiving than a thin ray
    public Vector2 originOffset = Vector2.zero;
    public bool aimWithMouse = false;        // false = use the way the player is facing

    [Header("Input")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Prompt UI")]
    public GameObject promptRoot;
    public TMP_Text promptText;

    playercon pc;
    PlayerCombat combat;
    SpriteRenderer sr;
    Rigidbody2D rb;
    Animator anim;
    IInteractable current;

    static readonly int IdleS = Animator.StringToHash("MukuroStand");

    void Awake()
    {
        pc = GetComponent<playercon>();
        combat = GetComponent<PlayerCombat>();
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        SetPrompt(null);
    }

    void Update()
    {
        if (DialogueManager.IsOpen || (combat != null && combat.IsDead))
        {
            SetPrompt(null);
            return;
        }

        // only look for things while standing still enough to talk
        bool canTalk = pc.Grounded && !pc.Dashing && !pc.DashWindup && !pc.ParryMovementLocked;
        current = canTalk ? Find() : null;
        SetPrompt(current);

        if (current != null && Input.GetKeyDown(interactKey))
            current.Interact(this);
    }

    Vector2 AimDir()
    {
        if (aimWithMouse) return pc.GetMouseOrMoveDirection();
        return sr.flipX ? Vector2.right : Vector2.left;   // same convention playercon uses
    }

    IInteractable Find()
    {
        Vector2 origin = (Vector2)transform.position + originOffset;
        RaycastHit2D hit = Physics2D.CircleCast(origin, castRadius, AimDir(), range, interactLayer);
        if (hit.collider == null) return null;
        return hit.collider.GetComponentInParent<IInteractable>();
    }

    void SetPrompt(IInteractable target)
    {
        if (promptRoot == null) return;
        bool show = target != null;
        if (promptRoot.activeSelf != show) promptRoot.SetActive(show);
        if (show && promptText != null) promptText.text = $"[{interactKey}] {target.Prompt}";
    }

    // ---------- called by DialogueManager ----------
    public void LockPlayer()
    {
        rb.linearVelocity = Vector2.zero;
        pc.ParryMovementLocked = false;
        pc.attackState = 0;
        pc.enabled = false;
        if (combat != null) combat.enabled = false;
        if (anim != null && anim.HasState(0, IdleS)) anim.Play(IdleS);
    }
    public bool IsTargeting(GameObject obj)
    {
        return current is Component c && (c.gameObject == obj || c.transform.IsChildOf(obj.transform));
    }

    public void UnlockPlayer()
    {
        pc.enabled = true;
        if (combat != null) combat.enabled = true;
    }

    void OnDrawGizmosSelected() // gizmos are just so cool
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        Vector2 origin = (Vector2)transform.position + originOffset;
        Vector2 dir = Application.isPlaying ? AimDir() : Vector2.right;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin, castRadius);
        Gizmos.DrawWireSphere(origin + dir * range, castRadius);
        Gizmos.DrawLine(origin, origin + dir * range);
    }
}
// this is really cool to write - Pradhyot.N 4/11/26 (dont remove this comment, its the signature)

// Oh, baby, do you know what the patterns mean to show?
// Oh, maybe it's a sign where you've got to go
// Oh
// Can we wander for a spell, and live in parallel?
// I want it to be true, to be like you, my love
// I'll find the point where the two paths meet
// And I'll rectify what I left behind
// Some ersatz human, my synapse fires
// I dance delightful, it feels so right
// To be moving freely with gay abandon
// Oh, inefficient and free from time
// I'm a convoluted amalgamate
// I dance delightful the whole damn night, oh
// (I want you here, right next to me)
// (じゃ〜！) - dance delightful by Jamie P