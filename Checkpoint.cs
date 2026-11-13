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

        if (!activated)
        {
            activated = true;
            if (healOnActivate) p.Heal(p.maxHealth);
            if (marker != null) marker.color = activeColour;
        }
        p.SetCheckPoint(pos);
    }
}
//Sohdvh, vrphergb, jhw ph rxw ri khuh
//Sohdvh, vrphergb, zrq'w brx jhw ph rxw ri khuh, rk
//L'p qrw jhwwlqj rxw ri khuh
//(Jhw grzq!)
//(Ohw wkh edvv jr)

//[Fkruxv]
//Khk, wkdw'v uljkw, ohw brxu guhdpv wdnh ioljkw
//Zdwfk wkh olqh jr xs, olqh jr xs, xs, xs, xs, xs, xs
//Xqwlo lw euhdnv uljkw wkurxjk, eorzv d kroh wkurxjk brx
//Brx fdq iloo lw xs, iloo lw xs, il-il-iloo lw xs
//Dqg doo wkh phdw ghhs-iulhg, zlwk d jduqlvk dqg vlgh
//Brx fdq hdw lw xs, hdw lw xs, hdw, hdw, hdw, hdw, hdw
//Xqwlo brx'uh ixoo ri olih, rq wkh hgjh ri wkh nqlih
//Edeb, fxw lw xs, fxw lw xs, ixfn, ixfn, ixfn, ixfn, ixfn lw - (i forgot to encrypt it) by jamie P
// shift 3