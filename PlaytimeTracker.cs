using UnityEngine;

// Creates itself at startup and survives scene loads. No scene setup needed.
// Uses scaled time, so time spent paused or on the fail popup isn't counted.
public class PlaytimeTracker : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        GameObject go = new GameObject("PlaytimeTracker");
        DontDestroyOnLoad(go);
        go.AddComponent<PlaytimeTracker>();
    }

    void Update()
    {
        if (GameSession.Playing) GameSession.PlaySeconds += Time.deltaTime;
    }
}
// a simple library - Pradhyot.N 4/11/26 (dont remove this comment, its the signature)
// i don't think i should put the chorus's of songs in just libaries