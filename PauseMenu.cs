using UnityEngine;
using UnityEngine.SceneManagement;


public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [Header("Panels")]
    public GameObject pauseRoot;  
    public GameObject pausePanel;  
    public GameObject settingsPanel;    

    [Header("Other")]
    public ConfirmPopup confirm;       
    public SettingsMenu settings; 
    public StatusBarHandler statusBar;
    public KeyCode pauseKey = KeyCode.Escape;
    public string mainMenuScene = "MainMenu";

    PlayerCombat combat;

    void Awake()
    {
        IsPaused = false;
        if (pauseRoot != null) pauseRoot.SetActive(false);
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) combat = p.GetComponent<PlayerCombat>();
        if (confirm == null) Debug.LogWarning("PauseMenu: 'Confirm' is not assigned", this);
    }

    void OnDestroy()
    {
        IsPaused = false;
    }

    void Update()
    {
        if (!Input.GetKeyDown(pauseKey)) return;

        if (IsPaused) Back();
        else if (CanPause()) Pause();
    }

    bool CanPause()
    {
        if (Time.timeScale <= 0f) return false;
        if (combat != null && combat.IsDead) return false;
        return true;
    }

    void Back()
    {
        if (confirm != null && confirm.IsOpen) confirm.Close();
        else if (settingsPanel != null && settingsPanel.activeSelf) ShowPanel(pausePanel);
        else Resume();
    }

    void ShowPanel(GameObject panel)
    {
        if (pausePanel != null) pausePanel.SetActive(panel == pausePanel);
        if (settingsPanel != null) settingsPanel.SetActive(panel == settingsPanel);
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        if (statusBar != null) statusBar.SetVisible(false);
        pauseRoot.SetActive(true);
        ShowPanel(pausePanel);
    }

    public void Resume()
    {
        if (confirm != null && confirm.IsOpen) confirm.Close();
        IsPaused = false;
        Time.timeScale = 1f;
        if (statusBar != null) statusBar.SetVisible(true);
        pauseRoot.SetActive(false);
    }

    public void OpenSettings()
    {
        ShowPanel(settingsPanel);
        if (settings != null) settings.ShowTab(0);
    }

    public void CloseSettings() => ShowPanel(pausePanel);

    public void AskRestart()
    {
        confirm.Ask("Restart from your last checkpoint?\nProgress since then will be lost.", RestartFromCheckpoint);
    }

    public void AskMainMenu()
    {
        confirm.Ask("Return to the main menu?\nProgress since your last checkpoint will be lost.", ToMainMenu);
    }

    void RestartFromCheckpoint()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        GameSession.ResetToLastSave();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void ToMainMenu()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        GameSession.Playing = false; 
        SceneManager.LoadScene(mainMenuScene);
    }
}
// made by pradhyot. N