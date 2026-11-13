using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlotButton : MonoBehaviour
{
    public Button button;
    public Button deleteButton;
    public TMP_Text label;

    public void Refresh(int index, bool loadMode)
    {
        SaveData d = SaveSystem.Load(index);
        if (deleteButton != null) deleteButton.interactable = d != null;
        if (d == null)
        {
            label.text = $"Slot {index + 1}\nEmpty";
            button.interactable = !loadMode;   // can't load an empty slot
            return;
        }
        string detail = d.hasCheckpoint ? $"{d.scene}  |  HP {d.health}/{d.maxHealth}" : $"{d.scene}  |  New game";
        label.text = $"Slot {index + 1}\n{detail}\n{d.savedAt}";
        button.interactable = true;
    }

    static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        int h = total / 3600;
        int m = (total % 3600) / 60;
        int s = total % 60;
        return $"{h}:{m:00}:{s:00}";
    }
}
// you know this actually is really cool, but convuluted - Pradhyot.N 5/11/26 (dont remove this comment, its the signature)