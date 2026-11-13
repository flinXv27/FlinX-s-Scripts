using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

// Left column of category buttons, right side shows the matching sub-panel.
// Lives on the always-active MenuManager object so saved options are applied at startup.
public class SettingsMenu : MonoBehaviour
{
    [System.Serializable]
    public class Tab
    {
        public string name;
        public Button button;
        public GameObject panel;
        [TextArea] public string description;   // shown in the bar at the bottom
    }

    [Header("Tabs")]
    public Tab[] tabs;
    public TMP_Text descriptionText;
    public Color selectedColor = new Color(.85f, .25f, .4f);
    public Color normalColor = Color.white;

    [Header("Audio")]
    public AudioMixer mixer;                    // optional, needs exposed params below
    public string musicParam = "MusicVol";
    public string sfxParam = "SFXVol";
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Graphics")] // if you are using them
    public Toggle fullscreenToggle;
    public Toggle vsyncToggle;
    public TMP_Dropdown resolutionDropdown;
    readonly List<Vector2Int> resolutions = new List<Vector2Int>();

    void Awake()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            int idx = i;   // capture a copy for the lambda
            if (tabs[i].button != null) tabs[i].button.onClick.AddListener(() => ShowTab(idx));
        }
    }

    void Start()
    {
        SetupSlider(masterSlider, "vol_master", v => AudioListener.volume = v);
        SetupSlider(musicSlider, "vol_music", v => SetMixer(musicParam, v));
        SetupSlider(sfxSlider, "vol_sfx", v => SetMixer(sfxParam, v));

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = GameSettings.GetBool("fullscreen", Screen.fullScreen);
            fullscreenToggle.onValueChanged.AddListener(on => { Screen.fullScreen = on; GameSettings.SetBool("fullscreen", on); });
        }
        if (vsyncToggle != null)
        {
            vsyncToggle.isOn = GameSettings.GetBool("vsync", QualitySettings.vSyncCount > 0);
            vsyncToggle.onValueChanged.AddListener(on => { QualitySettings.vSyncCount = on ? 1 : 0; GameSettings.SetBool("vsync", on); });
        }
        BuildResolutions();
        ShowTab(0);
    }

    public void ShowTab(int index)
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            bool selected = i == index;
            if (tabs[i].panel != null) tabs[i].panel.SetActive(selected);
            if (tabs[i].button != null && tabs[i].button.targetGraphic != null)
                tabs[i].button.targetGraphic.color = selected ? selectedColor : normalColor;
        }
        if (descriptionText != null && index >= 0 && index < tabs.Length)
            descriptionText.text = tabs[index].description;
    }

    void SetupSlider(Slider s, string prefKey, System.Action<float> apply)
    {
        if (s == null) return;
        s.minValue = 0f; s.maxValue = 1f;
        s.value = PlayerPrefs.GetFloat(prefKey, 1f);
        apply(s.value);
        s.onValueChanged.AddListener(v => { apply(v); PlayerPrefs.SetFloat(prefKey, v); PlayerPrefs.Save(); });
    }

    void SetMixer(string param, float v)
    {
        if (mixer != null) mixer.SetFloat(param, Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f);
    }

    void BuildResolutions()
    {
        if (resolutionDropdown == null) return;
        resolutions.Clear();
        List<string> labels = new List<string>();
        int current = 0;
        foreach (Resolution r in Screen.resolutions)
        {
            Vector2Int size = new Vector2Int(r.width, r.height);
            if (resolutions.Contains(size)) continue;   // skip duplicates that only differ by refresh rate
            resolutions.Add(size);
            labels.Add(r.width + " x " + r.height);
            if (r.width == Screen.width && r.height == Screen.height) current = resolutions.Count - 1;
        }
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(labels);
        resolutionDropdown.value = current;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(i => Screen.SetResolution(resolutions[i].x, resolutions[i].y, Screen.fullScreen));
    }
}
// a simple interface - Pradhyot.N 4/11/26 (dont remove this comment, its the signature)
// this takes a loooooooong time to setup, but it's worth it for the convenience of not having to manually set up each button and panel in the inspector