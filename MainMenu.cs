using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    enum SlotMode { New, Load }

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject slotPanel;
    public GameObject settingsPanel;
    public GameObject extrasPanel;

    [Header("Main buttons")]
    public Button loadGameButton;

    [Header("Slots")]
    public TMP_Text slotTitle;
    public SlotButton[] slotButtons;       // one per slot (3)

    [Header("Other")]
    public ConfirmPopup confirm;
    public SettingsMenu settings;
    public string firstGameScene = "SampleScene";

    SlotMode mode;
    GameObject current;

    void Start()
    {
        if (confirm == null) Debug.LogError("MainMenu: 'Confirm' is not assigned in the Inspector", this);
        if (settings == null) Debug.LogWarning("MainMenu: 'Settings' is not assigned", this);
        Time.timeScale = 1f;
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int idx = i;
            slotButtons[i].button.onClick.AddListener(() => SelectSlot(idx));
            if (slotButtons[i].deleteButton != null) slotButtons[i].deleteButton.onClick.AddListener(() => AskDelete(idx));
        }
        ShowMain();
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (confirm.IsOpen) confirm.Close();
        else if (current != mainPanel) ShowMain();
    }

    // ---------- panel switching ----------
    void Show(GameObject panel)
    {
        mainPanel.SetActive(panel == mainPanel);
        slotPanel.SetActive(panel == slotPanel);
        settingsPanel.SetActive(panel == settingsPanel);
        extrasPanel.SetActive(panel == extrasPanel);
        current = panel;
    }

    public void ShowMain()
    {
        loadGameButton.interactable = SaveSystem.AnySave();
        Show(mainPanel);
    }

    public void OpenNewGame()  { OpenSlots(SlotMode.New);  }
    public void OpenLoadGame() { OpenSlots(SlotMode.Load); }
    public void OpenSettings() { Show(settingsPanel); if (settings != null) settings.ShowTab(0); }
    public void OpenExtras()   { Show(extrasPanel); }

    void OpenSlots(SlotMode m)
    {
        mode = m;
        slotTitle.text = m == SlotMode.New ? "New Game - choose a slot" : "Load Game - choose a slot";
        RefreshSlots();
        Show(slotPanel);
    }

    void RefreshSlots()
    {
        for (int i = 0; i < slotButtons.Length; i++)
            slotButtons[i].Refresh(i, mode == SlotMode.Load);
    }

    // ---------- slot actions ----------
    void SelectSlot(int i)
    {
        bool hasSave = SaveSystem.Exists(i);
        if (mode == SlotMode.New)
        {
            string msg = hasSave
                ? $"Slot {i + 1} already has a save.\nOverwrite it and start a new game?"
                : $"Start a new game in Slot {i + 1}?";
            confirm.Ask(msg, () => StartNew(i));
        }
        else if (hasSave)
        {
            confirm.Ask($"Load Slot {i + 1}?", () => LoadSlot(i));
        }
    }
    void AskDelete(int i)
    {
        if (!SaveSystem.Exists(i)) return;
        confirm.Ask($"Delete Slot {i + 1}?\nThis can't be undone.", () => DeleteSlot(i));
    }
    void DeleteSlot(int i)
    {
        SaveSystem.Delete(i);
        if (GameSession.CurrentSlot == i) GameSession.Clear();
        RefreshSlots();
    }

    void StartNew(int slot)
    {
        GameSession.NewGame(slot, firstGameScene);
        SceneManager.LoadScene(firstGameScene);
    }

    void LoadSlot(int slot)
    {
        if (!GameSession.LoadGame(slot)) { RefreshSlots(); return; }
        string scene = string.IsNullOrEmpty(GameSession.Current.scene) ? firstGameScene : GameSession.Current.scene;
        SceneManager.LoadScene(scene);
    }

    public void Quit()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
// im actually so done