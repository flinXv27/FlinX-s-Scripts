using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Put on a row Button. Clicking cycles through options, e.g. Low / Medium / High
[RequireComponent(typeof(Button))]
public class SettingChoice : MonoBehaviour
{
    public string key = "textwindow";
    public string[] options = { "Low", "Medium", "High" };
    public int defaultIndex = 0;
    public TMP_Text valueText;
    int index;

    void Awake()
    {
        index = Mathf.Clamp(GameSettings.GetInt(key, defaultIndex), 0, options.Length - 1);
        GetComponent<Button>().onClick.AddListener(Next);
        Refresh();
    }

    public void Next()
    {
        index = (index + 1) % options.Length;
        GameSettings.SetInt(key, index);
        Refresh();
    }

    void Refresh()
    {
        if (valueText != null && options.Length > 0) valueText.text = options[index];
    }
}
// a basic interface - Pradhyot.N 4/11/26 (dont remove this comment, its the signature)
// who even reads these comments anyway