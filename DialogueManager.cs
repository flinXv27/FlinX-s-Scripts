using System.Collections;
using TMPro;
using UnityEngine;
// this links back to the interactible scripts
// One per scene, on an always-active object (e.g. the Canvas). 'root' is the dialogue box, disabled by default.
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [Header("UI refs")]
    public GameObject root;
    public TMP_Text nameText;
    public TMP_Text bodyText;
    public GameObject continueIndicator;     // little arrow, shown when a line is finished

    [Header("Behaviour")]
    public float charsPerSecond = 40f;
    public bool pauseWorld = true;           // freezes enemies etc. while talking
    public KeyCode advanceKey = KeyCode.E;
    public KeyCode altAdvanceKey = KeyCode.Space;
    public KeyCode skipKey = KeyCode.Escape; // closes the whole conversation

    string[] lines;
    int index;
    bool typing;
    int openedFrame;
    Coroutine typeRoutine;
    PlayerInteractor talker;

    void Awake()
    {
        Instance = this;
        IsOpen = false;
        if (root != null) root.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (IsOpen) { IsOpen = false; Time.timeScale = 1f; }
    }

    public void Begin(string speaker, string[] dialogueLines)
    {
        if (IsOpen || dialogueLines == null || dialogueLines.Length == 0) return;

        talker = FindFirstObjectByType<PlayerInteractor>();
        if (talker != null) talker.LockPlayer();

        lines = dialogueLines;
        index = 0;
        IsOpen = true;
        openedFrame = Time.frameCount;      // so the key press that opened it doesn't also advance it
        if (pauseWorld) Time.timeScale = 0f;

        if (nameText != null) nameText.text = speaker;
        root.SetActive(true);
        ShowLine();
    }

    void Update()
    {
        if (!IsOpen || Time.frameCount == openedFrame) return;

        if (Input.GetKeyDown(skipKey)) { End(); return; }

        bool advance = Input.GetKeyDown(advanceKey) || Input.GetKeyDown(altAdvanceKey) || Input.GetMouseButtonDown(0);
        if (!advance) return;

        if (typing) FinishTyping();          // first press completes the line
        else Next();                         // second press moves on
    }

    void ShowLine()
    {
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(Type(lines[index]));
    }

    IEnumerator Type(string line)
    {
        typing = true;
        if (continueIndicator != null) continueIndicator.SetActive(false);
        bodyText.text = line;
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate();
        int total = bodyText.textInfo.characterCount;

        float shown = 0f;
        while (shown < total)
        {
            shown += charsPerSecond * Time.unscaledDeltaTime;   // unscaled so it works while paused
            bodyText.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
            yield return null;
        }
        FinishTyping();
    }

    void FinishTyping()
    {
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = null;
        typing = false;
        bodyText.maxVisibleCharacters = int.MaxValue;
        if (continueIndicator != null) continueIndicator.SetActive(true);
    }

    void Next()
    {
        index++;
        if (index >= lines.Length) End();
        else ShowLine();
    }

    void End()
    {
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = null;
        typing = false;
        IsOpen = false;
        root.SetActive(false);
        if (pauseWorld) Time.timeScale = 1f;
        if (talker != null) talker.UnlockPlayer();
    }
}
// this actually is quite fun! - Pradhyot.N 4/11/26 (dont remove this comment, its the signature)