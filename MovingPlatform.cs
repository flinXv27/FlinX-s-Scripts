using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour, IResettable
{
    [Header("Path")]
    public Vector2[] offsets = { new Vector2(4f, 0f) };
    public bool loop = false;                // false = ping-pong (there and back), true = A > B > C > A
    public float speed = 3f;
    public float waitTime = .5f;             // pause at each end

    [Header("Trigger")]
    public bool onlyWhenStoodOn = false;     // true = only moves while something is riding it

    Rigidbody2D rb;
    Collider2D col;
    Vector2 startPos;
    int target = 1;
    int dir = 1;
    float waitTimer;
    readonly HashSet<Rigidbody2D> riders = new HashSet<Rigidbody2D>();
    readonly List<Rigidbody2D> dead = new List<Rigidbody2D>();

    int PointCount => offsets.Length + 1;
    Vector2 Point(int i) => i == 0 ? startPos : startPos + offsets[i - 1];

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        startPos = rb.position;
    }

    void Start() { ResetManager.Register(this); }

    void OnDestroy() { ResetManager.Unregister(this); }

    void OnDisable() { riders.Clear(); }

    void FixedUpdate()
    {
        if (PointCount < 2) return;
        if (onlyWhenStoodOn && riders.Count == 0) return;
        if (waitTimer > 0f) { waitTimer -= Time.fixedDeltaTime; return; }

        Vector2 pos = rb.position;
        Vector2 goal = Point(target);
        Vector2 next = Vector2.MoveTowards(pos, goal, speed * Time.fixedDeltaTime);
        Vector2 delta = next - pos;

        CarryRiders(delta);
        rb.MovePosition(next);

        if (next == goal)
        {
            waitTimer = waitTime;
            Advance();
        }
    }

    void CarryRiders(Vector2 delta)
    {
        dead.Clear();
        foreach (Rigidbody2D r in riders)
        {
            if (r == null) { dead.Add(r); continue; }
            r.position += delta;
        }
        foreach (Rigidbody2D r in dead) riders.Remove(r);
    }

    void Advance()
    {
        if (loop) { target = (target + 1) % PointCount; return; }
        if (target + dir >= PointCount || target + dir < 0) dir = -dir;
        target += dir;
    }

    void OnCollisionStay2D(Collision2D c)
    {
        Rigidbody2D r = c.rigidbody;
        if (r == null || r.bodyType != RigidbodyType2D.Dynamic) return;


        bool above = c.collider.bounds.min.y >= col.bounds.max.y - .15f;
        if (above) riders.Add(r);
        else riders.Remove(r);
    }

    void OnCollisionExit2D(Collision2D c)
    {
        if (c.rigidbody != null) riders.Remove(c.rigidbody);
    }

    public void ResetToStart()
    {
        riders.Clear();
        target = 1;
        dir = 1;
        waitTimer = 0f;
        rb.position = startPos;
        transform.position = startPos;
    }

    void OnDrawGizmosSelected()
    {
        Vector2 origin = Application.isPlaying ? startPos : (Vector2)transform.position;
        Gizmos.color = Color.cyan;
        Vector2 prev = origin;
        Gizmos.DrawWireSphere(prev, .15f);
        for (int i = 0; i < offsets.Length; i++)
        {
            Vector2 p = origin + offsets[i];
            Gizmos.DrawLine(prev, p);
            Gizmos.DrawWireSphere(p, .15f);
            prev = p;
        }
        if (loop && offsets.Length > 0) Gizmos.DrawLine(prev, origin);
    }
}
// made by pradhyot. N