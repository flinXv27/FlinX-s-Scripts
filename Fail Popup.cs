using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FailPopup : MonoBehaviour
{
    [Header("UI refs")]
    public GameObject popupRoot;
    public RectTransform card;
    public TMP_Text titleText;

    [Header("content")]
    public string[] FailTitles = {"Slaughtered", "Fail", "Retry Mission?"};
    public KeyCode retrykey = KeyCode.R;

    [Header("animation")]
    public float showTime = .5f;
    public float slidefrom = 400f;

    CanvasGroup group;
    StatusBarHandler statusBar;
    Vector2 restPos;
    bool shown;

    void Awake()
    {
        if (popupRoot == null || card == null)
        {
            Debug.LogWarning("Popup Failed to load");
        }
        group = popupRoot.GetComponent<CanvasGroup>();
        if (group == null) group = popupRoot.AddComponent<CanvasGroup>();
        restPos = card.anchoredPosition;
        popupRoot.SetActive(false);
    }

    void Update()
    {
        if (shown && Input.GetKeyDown(retrykey)) Retry();
    }

    public void Show()
    {
        if (statusBar != null) statusBar.SetVisible(false);
        if (shown || popupRoot == null || card == null) return;
        shown = true;
        if (titleText != null && FailTitles.Length > 0) titleText.text = FailTitles[Random.Range(0, FailTitles.Length)];
        Time.timeScale = 0f;
        popupRoot.SetActive(true);
        StartCoroutine(Animate());
    }
    IEnumerator Animate()
    {
        group.alpha = 0f;
        group.interactable = false;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / showTime;
            float e = Mathf.Clamp01(t);
            group.alpha = e;
            card.anchoredPosition = restPos + Vector2.up * slidefrom * (1f - EaseOutBack(e));
            yield return null;
        }
        group.alpha = 1f;
        card.anchoredPosition = restPos;
        group.interactable = true;
    }
    float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x -1f, 2f);
    }
    public void Retry()
    {
        Time.timeScale = 1f;
        GameSession.ResetToLastSave();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
// AHHHHHHHHHHHHHHHHH - pradhyot.N 23/10/2026
// medicine, i dont actually know any of the lyrics for it tho