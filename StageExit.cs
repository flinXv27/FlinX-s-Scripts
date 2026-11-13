using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Put on a door / portal / sign that has a Collider2D on the interact layer.
// Pair it with InteractIndicator if you want the little "E" icon above it.
public class StageExit : MonoBehaviour, IInteractable
{
    [Header("Interact")]
    public string prompt = "Continue";

    [Header("Destination")]
    [Tooltip("true = load the next scene in Build Settings. false = use 'nextSceneName'")]
    public bool useNextBuildIndex = false;
    public string nextSceneName = "Stage2";
    [Tooltip("Used if there is no next scene (e.g. this was the last stage)")]
    public string fallbackScene = "MainMenu";

    [Header("Transition")]
    public ScreenFade screenFade;            // optional, same one your death respawn uses
    public float fadeTime = .5f;

    bool used;

    public string Prompt => prompt;

    public void Interact(PlayerInteractor who)
    {
        if (used) return;
        string target = ResolveTarget();
        if (string.IsNullOrEmpty(target))
        {
            Debug.LogWarning($"{name}: StageExit has no destination scene", this);
            return;
        }
        used = true;
        StartCoroutine(Go(who, target));
    }

    string ResolveTarget()
    {
        if (!useNextBuildIndex) return nextSceneName;

        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next < SceneManager.sceneCountInBuildSettings)
        {
            // convert the build index to a scene name so the save file stores a name like it does now
            string path = SceneUtility.GetScenePathByBuildIndex(next);
            return System.IO.Path.GetFileNameWithoutExtension(path);
        }
        return fallbackScene;
    }

    IEnumerator Go(PlayerInteractor who, string target)
    {
        if (who != null) who.LockPlayer();

        if (screenFade != null) yield return screenFade.FadeTo(1f, fadeTime);

        if (target == fallbackScene && target == "MainMenu") GameSession.Playing = false;
        else GameSession.AdvanceToScene(target);

        Time.timeScale = 1f;
        SceneManager.LoadScene(target);
    }
}
// The Best of the Best - Pradhyot.N 4/11/26 (dont remove this comment, its the signature)
// I'm a diva
// I ain't no fake
// I'm gonna sing wonderful songs
// Vocaloid monopoly!
// I'm an idol
// My hair isn't drills
// Drills... go BRRRRRRRRRR!
// I am the Kasane Territory! - translated lyrics of "Teto Territory" by Kasane Teto