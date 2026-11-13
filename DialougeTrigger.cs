using UnityEngine;

public class DialogueTrigger : MonoBehaviour, IInteractable
{
    public string speakerName = "???";
    public string prompt = "Talk";
    [TextArea(2, 5)] public string[] lines;
    [Tooltip("Optional: said instead of 'lines' on every talk after the first")]
    [TextArea(2, 5)] public string[] repeatLines;

    bool talkedBefore;

    public string Prompt => prompt;

    public void Interact(PlayerInteractor who)
    {
        string[] use = (talkedBefore && repeatLines != null && repeatLines.Length > 0) ? repeatLines : lines;
        talkedBefore = true;
        if (use == null || use.Length == 0) return;
        DialogueManager.Instance.Begin(speakerName, use);
    }
}
// written by Pradhyot.N 4/11/26 (dont remove this comment, its the signature)