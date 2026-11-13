using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Put on a row Button. Clicking flips ON/OFF, like the rows in the Ace Attorney options screen.
[RequireComponent(typeof(Button))]
public class SettingToggle : MonoBehaviour
{
    public string key = "screenshake";
    public bool defaultValue = true;
    public TMP_Text valueText;
    public string onText = "ON";
    public string offText = "OFF";
    bool value;

    void Awake()
    {
        value = GameSettings.GetBool(key, defaultValue);
        GetComponent<Button>().onClick.AddListener(Toggle);
        Refresh();
    }

    public void Toggle()
    {
        value = !value;
        GameSettings.SetBool(key, value);
        Refresh();
    }

    void Refresh()
    {
        if (valueText != null) valueText.text = value ? onText : offText;
    }
}
// a simple interface - Pradhyot.N 4/11/26 (dont remove this comment, its the signature)