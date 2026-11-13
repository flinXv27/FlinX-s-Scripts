using System.Collections;
using UnityEngine;

public class OneWayPlatform : MonoBehaviour
{
    public float dropTime = .3f;
    Collider2D col;

    void Awake() { col = GetComponent<Collider2D>(); }

    void OnDisable() { if (col != null) col.enabled = true; }

    public void DropThrough()
    {
        if (gameObject.activeInHierarchy) StartCoroutine(Drop());
    }

    IEnumerator Drop()
    {
        col.enabled = false;
        yield return new WaitForSeconds(dropTime);
        col.enabled = true;
    }
}
// made by pradhyot. N