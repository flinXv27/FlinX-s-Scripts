using UnityEngine;
// a completely optional script that spawns afterimages of the player while dashing, for a cool effect. It is not necessary for the game to function, and can be removed if desired. It is also not very optimized, so it may cause performance issues on lower-end devices. It is recommended to use this script only on the player character, and not on other objects. It is also recommended to use this script only on 2D games, as it may not work correctly in 3D games.
public class AfterImageSpawner : MonoBehaviour
{
    [Header("Look")]
    public Color ghostColor = new Color(0.4f, 0.8f, 1f, 0.6f); // tint + starting alpha
    public float fadeTime = 0.25f;

    [Header("Spacing")]
    public float spawnDistance = 0.5f; // a new ghost every X units travelled

    playercon pc;
    SpriteRenderer sr;
    Vector2 lastSpawnPos;
    bool wasDashing;

    void Awake()
    {
        pc = GetComponent<playercon>();
        sr = GetComponent<SpriteRenderer>();
    }

    // LateUpdate so the animator has already set this frame's sprite
    void LateUpdate()
    {
        if (!pc.Dashing)
        {
            wasDashing = false;
            return;
        }

        Vector2 pos = transform.position;
        if (!wasDashing)
        {
            wasDashing = true;
            SpawnGhost();            // ghost at the dash's starting point
            lastSpawnPos = pos;
            return;
        }

        if (Vector2.Distance(pos, lastSpawnPos) >= spawnDistance)
        {
            SpawnGhost();
            lastSpawnPos = pos;
        }
    }

    void SpawnGhost()
    {
        GameObject g = new GameObject("AfterImage");
        g.transform.SetPositionAndRotation(transform.position, transform.rotation);
        g.transform.localScale = transform.lossyScale;

        SpriteRenderer gsr = g.AddComponent<SpriteRenderer>();
        gsr.sprite = sr.sprite;
        gsr.flipX = sr.flipX;
        gsr.flipY = sr.flipY;
        gsr.color = ghostColor;
        gsr.sharedMaterial = sr.sharedMaterial;
        gsr.sortingLayerID = sr.sortingLayerID;
        gsr.sortingOrder = sr.sortingOrder - 1; // behind the player

        g.AddComponent<AfterImageFade>().Init(fadeTime);
    }
}

public class AfterImageFade : MonoBehaviour
{
    SpriteRenderer sr;
    float duration, t;
    float startAlpha;

    public void Init(float fadeTime)
    {
        sr = GetComponent<SpriteRenderer>();
        duration = Mathf.Max(fadeTime, 0.01f);
        startAlpha = sr.color.a;
    }

    void Update()
    {
        t += Time.deltaTime / duration;
        Color c = sr.color;
        c.a = Mathf.Lerp(startAlpha, 0f, t);
        sr.color = c;
        if (t >= 1f) Destroy(gameObject);
    }
}
// well it was easy to write ig - Pradhyot.N 4/11/26 (dont remove this comment, its the signature)