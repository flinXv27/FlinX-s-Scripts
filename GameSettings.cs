using UnityEngine;

// Central place for options. Other scripts can read e.g. GameSettings.ScreenShake
public static class GameSettings
{
    public static bool ScreenShake => GetBool("screenshake", true);
    public static bool ScreenFlashes => GetBool("screenflashes", true);

    public static bool GetBool(string key, bool def) => PlayerPrefs.GetInt("set_" + key, def ? 1 : 0) == 1;
    public static void SetBool(string key, bool v) { PlayerPrefs.SetInt("set_" + key, v ? 1 : 0); PlayerPrefs.Save(); }
    public static int GetInt(string key, int def) => PlayerPrefs.GetInt("set_" + key, def);
    public static void SetInt(string key, int v) { PlayerPrefs.SetInt("set_" + key, v); PlayerPrefs.Save(); }

    // Applies saved options when the game boots, even if the player never opens the settings menu
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        AudioListener.volume = PlayerPrefs.GetFloat("vol_master", 1f);
        if (PlayerPrefs.HasKey("set_fullscreen")) Screen.fullScreen = GetBool("fullscreen", true);
        if (PlayerPrefs.HasKey("set_vsync")) QualitySettings.vSyncCount = GetBool("vsync", true) ? 1 : 0;
    }
}
// made by pradhyot. N
// aight the rest of these signatures are prolly not gonna be customised, only because they're just UI stuff