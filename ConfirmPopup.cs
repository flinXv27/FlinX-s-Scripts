using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Generic "Are you sure?" box. root = the dimmed background + box (disabled by default).
public class ConfirmPopup : MonoBehaviour
{
    public GameObject root;
    public TMP_Text messageText;
    public Button yesButton;
    public Button noButton;
    System.Action onYes;

    public bool IsOpen => root != null && root.activeSelf;

    void Awake()
    {
        yesButton.onClick.AddListener(() => { var a = onYes; Close(); a?.Invoke(); });
        noButton.onClick.AddListener(Close);
        root.SetActive(false);
    }

    public void Ask(string message, System.Action yes)
    {
        messageText.text = message;
        onYes = yes;
        root.SetActive(true);
    }

    public void Close()
    {
        onYes = null;
        root.SetActive(false);
    }
}
// you have to add these quite a lot - Pradhyot.N 4/11/26 (dont remove this comment, its the signature)