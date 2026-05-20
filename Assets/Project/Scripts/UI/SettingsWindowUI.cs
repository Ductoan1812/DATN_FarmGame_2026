using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// Quản lý SettingsWindow:
/// - 3 slider âm lượng: Master / Music / SFX (lưu PlayerPrefs)
/// - Dropdown ngôn ngữ (gọi LocalizationManager)
/// - Nút Save đóng window
/// </summary>
public class SettingsWindowUI : MonoBehaviour
{
    private const string KeyMaster = "vol_master";
    private const string KeyMusic  = "vol_music";
    private const string KeySfx    = "vol_sfx";
    private const string KeyLang   = "settings_language";

    [Header("Volume Sliders")]
    [SerializeField] private Slider sliderMaster;
    [SerializeField] private Slider sliderMusic;
    [SerializeField] private Slider sliderSfx;

    [Header("Volume Value Labels")]
    [SerializeField] private TMP_Text labelMasterValue;
    [SerializeField] private TMP_Text labelMusicValue;
    [SerializeField] private TMP_Text labelSfxValue;

    [Header("Language")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    [Header("Buttons")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button closeButton;

    [Header("Optional AudioMixer (để trống nếu chưa có)")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string mixerParamMaster = "VolMaster";
    [SerializeField] private string mixerParamMusic  = "VolMusic";
    [SerializeField] private string mixerParamSfx    = "VolSfx";

    private bool listenersRegistered;

    // ── Lifecycle ────────────────────────────────────────────────

    private void OnEnable()
    {
        AutoFindRefs();
        LoadSettings();
        RegisterListeners();
    }

    private void OnDisable()
    {
        UnregisterListeners();
    }

    // ── Setup ────────────────────────────────────────────────────

    private void AutoFindRefs()
    {
        sliderMaster     ??= FindDeepChild(transform, "SliderMaster")?.GetComponent<Slider>();
        sliderMusic      ??= FindDeepChild(transform, "SliderMusic")?.GetComponent<Slider>();
        sliderSfx        ??= FindDeepChild(transform, "SliderSfx")?.GetComponent<Slider>();
        labelMasterValue ??= FindDeepChild(transform, "LabelMasterValue")?.GetComponent<TMP_Text>();
        labelMusicValue  ??= FindDeepChild(transform, "LabelMusicValue")?.GetComponent<TMP_Text>();
        labelSfxValue    ??= FindDeepChild(transform, "LabelSfxValue")?.GetComponent<TMP_Text>();
        languageDropdown ??= FindDeepChild(transform, "LanguageDropdown")?.GetComponent<TMP_Dropdown>();
        saveButton       ??= FindDeepChild(transform, "SaveButton")?.GetComponent<Button>();
        closeButton      ??= FindDeepChild(transform, "CloseButton")?.GetComponent<Button>();
    }

    private void LoadSettings()
    {
        float master = PlayerPrefs.GetFloat(KeyMaster, 1f);
        float music  = PlayerPrefs.GetFloat(KeyMusic,  1f);
        float sfx    = PlayerPrefs.GetFloat(KeySfx,    1f);

        SetSlider(sliderMaster, master);
        SetSlider(sliderMusic,  music);
        SetSlider(sliderSfx,    sfx);

        UpdateVolumeLabel(labelMasterValue, master);
        UpdateVolumeLabel(labelMusicValue,  music);
        UpdateVolumeLabel(labelSfxValue,    sfx);

        ApplyVolume(mixerParamMaster, master);
        ApplyVolume(mixerParamMusic,  music);
        ApplyVolume(mixerParamSfx,    sfx);

        // Language dropdown
        if (languageDropdown != null)
        {
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new List<string> { "Tiếng Việt", "English" });

            int savedLang = PlayerPrefs.GetInt(KeyLang, 0);
            languageDropdown.value = Mathf.Clamp(savedLang, 0, languageDropdown.options.Count - 1);
            languageDropdown.RefreshShownValue();
        }
    }

    private void RegisterListeners()
    {
        if (listenersRegistered) return;

        if (sliderMaster != null) sliderMaster.onValueChanged.AddListener(OnMasterChanged);
        if (sliderMusic  != null) sliderMusic.onValueChanged.AddListener(OnMusicChanged);
        if (sliderSfx    != null) sliderSfx.onValueChanged.AddListener(OnSfxChanged);
        if (saveButton   != null) saveButton.onClick.AddListener(Save);
        if (closeButton  != null) closeButton.onClick.AddListener(Close);

        listenersRegistered = true;
    }

    private void UnregisterListeners()
    {
        if (!listenersRegistered) return;

        if (sliderMaster != null) sliderMaster.onValueChanged.RemoveListener(OnMasterChanged);
        if (sliderMusic  != null) sliderMusic.onValueChanged.RemoveListener(OnMusicChanged);
        if (sliderSfx    != null) sliderSfx.onValueChanged.RemoveListener(OnSfxChanged);
        if (saveButton   != null) saveButton.onClick.RemoveListener(Save);
        if (closeButton  != null) closeButton.onClick.RemoveListener(Close);

        listenersRegistered = false;
    }

    // ── Slider Callbacks ─────────────────────────────────────────

    private void OnMasterChanged(float value)
    {
        UpdateVolumeLabel(labelMasterValue, value);
        ApplyVolume(mixerParamMaster, value);
    }

    private void OnMusicChanged(float value)
    {
        UpdateVolumeLabel(labelMusicValue, value);
        ApplyVolume(mixerParamMusic, value);
    }

    private void OnSfxChanged(float value)
    {
        UpdateVolumeLabel(labelSfxValue, value);
        ApplyVolume(mixerParamSfx, value);
    }

    // ── Save / Close ─────────────────────────────────────────────

    private void Save()
    {
        float master = sliderMaster != null ? sliderMaster.value : 1f;
        float music  = sliderMusic  != null ? sliderMusic.value  : 1f;
        float sfx    = sliderSfx    != null ? sliderSfx.value    : 1f;

        PlayerPrefs.SetFloat(KeyMaster, master);
        PlayerPrefs.SetFloat(KeyMusic,  music);
        PlayerPrefs.SetFloat(KeySfx,    sfx);

        if (languageDropdown != null)
        {
            int langIndex = languageDropdown.value;
            PlayerPrefs.SetInt(KeyLang, langIndex);

            Language lang = langIndex == 1 ? Language.En : Language.Vi;
            LocalizationManager.Instance?.SetLanguage(lang);
        }

        PlayerPrefs.Save();
        Close();
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }

    // ── Helpers ───────────────────────────────────────────────────

    private void ApplyVolume(string mixerParam, float linearValue)
    {
        if (audioMixer == null || string.IsNullOrWhiteSpace(mixerParam)) return;

        // Chuyển linear (0–1) sang dB
        float db = linearValue > 0.0001f
            ? Mathf.Log10(linearValue) * 20f
            : -80f;
        audioMixer.SetFloat(mixerParam, db);
    }

    private static void SetSlider(Slider slider, float value)
    {
        if (slider == null) return;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value    = Mathf.Clamp01(value);
    }

    private static void UpdateVolumeLabel(TMP_Text label, float value)
    {
        if (label == null) return;
        label.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeepChild(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
