using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Quản lý SettingsWindow: âm lượng, ngôn ngữ, phím tương tác, hiển thị và điều hướng.
/// </summary>
public class SettingsWindowUI : MonoBehaviour
{
    private const string KeyMaster = "vol_master";
    private const string KeyMusic = "vol_music";
    private const string KeySfx = "vol_sfx";
    private const string KeyLang = "settings_language";
    private const string KeyFullscreen = "settings_fullscreen";
    private const string KeyVsync = "settings_vsync";
    private const string KeyTargetFps = "settings_target_fps";

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
    [SerializeField] private Button languageButton;
    [SerializeField] private TMP_Text languageValueLabel;

    [Header("Input")]
    [SerializeField] private Button interactKeyButton;
    [SerializeField] private TMP_Text interactKeyValueLabel;

    [Header("Display")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Toggle vsyncToggle;
    [SerializeField] private Button targetFpsButton;
    [SerializeField] private TMP_Text targetFpsValueLabel;
    [SerializeField] private Button resetDefaultsButton;

    [Header("Buttons")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Navigation")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";
    [SerializeField] private bool compactMainMenuMode;

    [Header("Optional AudioMixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string mixerParamMaster = "VolMaster";
    [SerializeField] private string mixerParamMusic = "VolMusic";
    [SerializeField] private string mixerParamSfx = "VolSfx";

    private bool listenersRegistered;
    private bool waitingForInteractKey;

    private void OnEnable()
    {
        EnsureBasicLayout();
        AutoFindRefs();
        ApplyContextualButtonVisibility();
        LoadSettings();
        RegisterListeners();
    }

    private void OnDisable()
    {
        UnregisterListeners();
        waitingForInteractKey = false;
    }

    private void Update()
    {
        if (!waitingForInteractKey)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            waitingForInteractKey = false;
            RefreshInteractKeyLabel();
            return;
        }

        if (!Input.anyKeyDown || !TryReadPressedKeyboardKey(out var key))
            return;

        GameplayInputSettings.SetInteractKey(key);
        PlayerPrefs.Save();
        waitingForInteractKey = false;
        RefreshInteractKeyLabel();
    }

    private void AutoFindRefs()
    {
        sliderMaster ??= MenuWindowShellUI.FindDeepChild(transform, "SliderMaster")?.GetComponent<Slider>();
        sliderMusic ??= MenuWindowShellUI.FindDeepChild(transform, "SliderMusic")?.GetComponent<Slider>();
        sliderSfx ??= MenuWindowShellUI.FindDeepChild(transform, "SliderSfx")?.GetComponent<Slider>();
        labelMasterValue ??= MenuWindowShellUI.FindDeepChild(transform, "LabelMasterValue")?.GetComponent<TMP_Text>();
        labelMusicValue ??= MenuWindowShellUI.FindDeepChild(transform, "LabelMusicValue")?.GetComponent<TMP_Text>();
        labelSfxValue ??= MenuWindowShellUI.FindDeepChild(transform, "LabelSfxValue")?.GetComponent<TMP_Text>();
        languageDropdown ??= MenuWindowShellUI.FindDeepChild(transform, "LanguageDropdown")?.GetComponent<TMP_Dropdown>();
        languageButton ??= MenuWindowShellUI.FindDeepChild(transform, "LanguageButton")?.GetComponent<Button>();
        languageValueLabel ??= MenuWindowShellUI.FindDeepChild(transform, "LanguageValue")?.GetComponent<TMP_Text>();
        interactKeyButton ??= MenuWindowShellUI.FindDeepChild(transform, "InteractKeyButton")?.GetComponent<Button>();
        interactKeyValueLabel ??= MenuWindowShellUI.FindDeepChild(transform, "InteractKeyValue")?.GetComponent<TMP_Text>();
        fullscreenToggle ??= MenuWindowShellUI.FindDeepChild(transform, "FullscreenToggle")?.GetComponent<Toggle>();
        vsyncToggle ??= MenuWindowShellUI.FindDeepChild(transform, "VsyncToggle")?.GetComponent<Toggle>();
        targetFpsButton ??= MenuWindowShellUI.FindDeepChild(transform, "TargetFpsButton")?.GetComponent<Button>();
        targetFpsValueLabel ??= MenuWindowShellUI.FindDeepChild(transform, "TargetFpsValue")?.GetComponent<TMP_Text>();
        resetDefaultsButton ??= MenuWindowShellUI.FindDeepChild(transform, "ResetDefaultsButton")?.GetComponent<Button>();
        saveButton ??= MenuWindowShellUI.FindDeepChild(transform, "SaveButton")?.GetComponent<Button>();
        closeButton ??= MenuWindowShellUI.FindDeepChild(transform, "CloseButton")?.GetComponent<Button>();
        backToMenuButton ??= MenuWindowShellUI.FindDeepChild(transform, "BackToMenuButton")?.GetComponent<Button>();
        quitButton ??= MenuWindowShellUI.FindDeepChild(transform, "QuitButton")?.GetComponent<Button>();
        UiTextStyleUtility.ApplyRobotoToChildren(transform);
    }

    private void EnsureBasicLayout()
    {
        bool hasRequiredLayout = MenuWindowShellUI.FindDeepChild(transform, "SliderMaster") != null
            && MenuWindowShellUI.FindDeepChild(transform, "InteractKeyButton") != null
            && MenuWindowShellUI.FindDeepChild(transform, "FullscreenToggle") != null
            && MenuWindowShellUI.FindDeepChild(transform, "VsyncToggle") != null
            && MenuWindowShellUI.FindDeepChild(transform, "TargetFpsButton") != null
            && MenuWindowShellUI.FindDeepChild(transform, "ResetDefaultsButton") != null
            && MenuWindowShellUI.FindDeepChild(transform, "BackToMenuButton") != null
            && MenuWindowShellUI.FindDeepChild(transform, "QuitButton") != null;

        if (hasRequiredLayout)
        {
            AutoFindRefs();
            ApplyContextualButtonVisibility();
            return;
        }

        ClearGeneratedRefs();

        MenuWindowShellUI.ClearChildren(transform);
        var body = MenuWindowShellUI.BuildShell(transform, "Cài đặt", new Vector2(0f, -42f), new Vector2(-96f, -128f));
        var bodyFrame = MenuWindowShellUI.CreateImage("SettingsBodyFrame", body, new Color(0.14f, 0.08f, 0.03f, 0.12f));
        MenuWindowShellUI.Stretch(bodyFrame.rectTransform, new Vector2(18f, 18f), new Vector2(-18f, -18f));

        CreateSliderRow(bodyFrame.transform, "Master", "Âm lượng tổng", -34f);
        CreateSliderRow(bodyFrame.transform, "Music", "Nhạc nền", -104f);
        CreateSliderRow(bodyFrame.transform, "Sfx", "Hiệu ứng", -174f);
        CreateLanguageRow(bodyFrame.transform, -244f);
        CreateInteractKeyRow(bodyFrame.transform, -304f);
        CreateToggleRow(bodyFrame.transform, "Fullscreen", "Toàn màn hình", -364f, true);
        CreateToggleRow(bodyFrame.transform, "Vsync", "Đồng bộ khung hình", -424f, false);
        CreateButtonValueRow(bodyFrame.transform, "TargetFps", "FPS mục tiêu", "60 FPS", -484f);
        CreateResetRow(bodyFrame.transform, -544f);

        CreateBasicButton("SaveButton", transform, "Lưu", new Vector2(-300f, 34f));
        CreateBasicButton("CloseButton", transform, "Đóng", new Vector2(-100f, 34f));
        CreateBasicButton("BackToMenuButton", transform, "Về menu", new Vector2(100f, 34f));
        CreateBasicButton("QuitButton", transform, "Thoát game", new Vector2(300f, 34f));
        AutoFindRefs();
        ApplyContextualButtonVisibility();
    }

    private void ClearGeneratedRefs()
    {
        sliderMaster = null;
        sliderMusic = null;
        sliderSfx = null;
        labelMasterValue = null;
        labelMusicValue = null;
        labelSfxValue = null;
        languageDropdown = null;
        languageButton = null;
        languageValueLabel = null;
        interactKeyButton = null;
        interactKeyValueLabel = null;
        fullscreenToggle = null;
        vsyncToggle = null;
        targetFpsButton = null;
        targetFpsValueLabel = null;
        resetDefaultsButton = null;
        saveButton = null;
        closeButton = null;
        backToMenuButton = null;
        quitButton = null;
    }

    private void LoadSettings()
    {
        float master = PlayerPrefs.GetFloat(KeyMaster, 1f);
        float music = PlayerPrefs.GetFloat(KeyMusic, 1f);
        float sfx = PlayerPrefs.GetFloat(KeySfx, 1f);

        SetSlider(sliderMaster, master);
        SetSlider(sliderMusic, music);
        SetSlider(sliderSfx, sfx);

        UpdateVolumeLabel(labelMasterValue, master);
        UpdateVolumeLabel(labelMusicValue, music);
        UpdateVolumeLabel(labelSfxValue, sfx);

        ApplyVolume(mixerParamMaster, master);
        ApplyVolume(mixerParamMusic, music);
        ApplyVolume(mixerParamSfx, sfx);

        int savedLang = PlayerPrefs.GetInt(KeyLang, 0);
        if (languageDropdown != null)
        {
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new List<string> { "Tiếng Việt", "English" });
            languageDropdown.SetValueWithoutNotify(Mathf.Clamp(savedLang, 0, languageDropdown.options.Count - 1));
            languageDropdown.RefreshShownValue();
        }

        RefreshLanguageLabel();

        bool fullscreen = PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;
        bool vsync = PlayerPrefs.GetInt(KeyVsync, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;
        int fps = PlayerPrefs.GetInt(KeyTargetFps, 60);

        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
        if (vsyncToggle != null)
            vsyncToggle.SetIsOnWithoutNotify(vsync);

        Screen.fullScreen = fullscreen;
        QualitySettings.vSyncCount = vsync ? 1 : 0;
        ApplyTargetFrameRate(fps, vsync);
        RefreshTargetFpsLabel();
        RefreshInteractKeyLabel();
    }

    private void RegisterListeners()
    {
        if (listenersRegistered) return;

        if (sliderMaster != null) sliderMaster.onValueChanged.AddListener(OnMasterChanged);
        if (sliderMusic != null) sliderMusic.onValueChanged.AddListener(OnMusicChanged);
        if (sliderSfx != null) sliderSfx.onValueChanged.AddListener(OnSfxChanged);
        if (languageDropdown != null) languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        if (languageButton != null) languageButton.onClick.AddListener(CycleLanguage);
        if (interactKeyButton != null) interactKeyButton.onClick.AddListener(StartInteractKeyRebind);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        if (vsyncToggle != null) vsyncToggle.onValueChanged.AddListener(OnVsyncChanged);
        if (targetFpsButton != null) targetFpsButton.onClick.AddListener(CycleTargetFps);
        if (resetDefaultsButton != null) resetDefaultsButton.onClick.AddListener(ResetDefaults);
        if (saveButton != null) saveButton.onClick.AddListener(Save);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (backToMenuButton != null) backToMenuButton.onClick.AddListener(BackToMenu);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

        listenersRegistered = true;
    }

    private void UnregisterListeners()
    {
        if (!listenersRegistered) return;

        if (sliderMaster != null) sliderMaster.onValueChanged.RemoveListener(OnMasterChanged);
        if (sliderMusic != null) sliderMusic.onValueChanged.RemoveListener(OnMusicChanged);
        if (sliderSfx != null) sliderSfx.onValueChanged.RemoveListener(OnSfxChanged);
        if (languageDropdown != null) languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
        if (languageButton != null) languageButton.onClick.RemoveListener(CycleLanguage);
        if (interactKeyButton != null) interactKeyButton.onClick.RemoveListener(StartInteractKeyRebind);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
        if (vsyncToggle != null) vsyncToggle.onValueChanged.RemoveListener(OnVsyncChanged);
        if (targetFpsButton != null) targetFpsButton.onClick.RemoveListener(CycleTargetFps);
        if (resetDefaultsButton != null) resetDefaultsButton.onClick.RemoveListener(ResetDefaults);
        if (saveButton != null) saveButton.onClick.RemoveListener(Save);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        if (backToMenuButton != null) backToMenuButton.onClick.RemoveListener(BackToMenu);
        if (quitButton != null) quitButton.onClick.RemoveListener(QuitGame);

        listenersRegistered = false;
    }

    private void OnMasterChanged(float value)
    {
        UpdateVolumeLabel(labelMasterValue, value);
        ApplyVolume(mixerParamMaster, value);
        PlayerPrefs.SetFloat(KeyMaster, value);
        PlayerPrefs.Save();
    }

    private void OnMusicChanged(float value)
    {
        UpdateVolumeLabel(labelMusicValue, value);
        ApplyVolume(mixerParamMusic, value);
        PlayerPrefs.SetFloat(KeyMusic, value);
        PlayerPrefs.Save();
    }

    private void OnSfxChanged(float value)
    {
        UpdateVolumeLabel(labelSfxValue, value);
        ApplyVolume(mixerParamSfx, value);
        PlayerPrefs.SetFloat(KeySfx, value);
        PlayerPrefs.Save();
    }

    private void OnLanguageChanged(int langIndex)
    {
        PlayerPrefs.SetInt(KeyLang, langIndex);
        var lang = langIndex == 1 ? Language.En : Language.Vi;
        LocalizationManager.Instance?.SetLanguage(lang);
        RefreshLanguageLabel();
        PlayerPrefs.Save();
    }

    private void CycleLanguage()
    {
        int current = PlayerPrefs.GetInt(KeyLang, 0);
        OnLanguageChanged(current == 0 ? 1 : 0);
        if (languageDropdown != null)
            languageDropdown.SetValueWithoutNotify(PlayerPrefs.GetInt(KeyLang, 0));
    }

    private void OnFullscreenChanged(bool value)
    {
        Screen.fullScreen = value;
        PlayerPrefs.SetInt(KeyFullscreen, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnVsyncChanged(bool value)
    {
        QualitySettings.vSyncCount = value ? 1 : 0;
        PlayerPrefs.SetInt(KeyVsync, value ? 1 : 0);
        ApplyTargetFrameRate(PlayerPrefs.GetInt(KeyTargetFps, 60), value);
        PlayerPrefs.Save();
    }

    private void CycleTargetFps()
    {
        int current = PlayerPrefs.GetInt(KeyTargetFps, 60);
        int next = current switch
        {
            30 => 60,
            60 => 120,
            120 => -1,
            _ => 30
        };

        PlayerPrefs.SetInt(KeyTargetFps, next);
        ApplyTargetFrameRate(next, vsyncToggle != null && vsyncToggle.isOn);
        RefreshTargetFpsLabel();
        PlayerPrefs.Save();
    }

    private void ResetDefaults()
    {
        PlayerPrefs.SetFloat(KeyMaster, 1f);
        PlayerPrefs.SetFloat(KeyMusic, 1f);
        PlayerPrefs.SetFloat(KeySfx, 1f);
        PlayerPrefs.SetInt(KeyLang, 0);
        PlayerPrefs.SetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0);
        PlayerPrefs.SetInt(KeyVsync, 0);
        PlayerPrefs.SetInt(KeyTargetFps, 60);
        GameplayInputSettings.SetInteractKey(KeyCode.E);
        PlayerPrefs.Save();
        LoadSettings();
    }

    private void Save()
    {
        if (languageDropdown != null)
            OnLanguageChanged(languageDropdown.value);

        PlayerPrefs.Save();
        Close();
    }

    public void SetCompactMainMenuMode(bool value)
    {
        compactMainMenuMode = value;
        AutoFindRefs();
        ApplyContextualButtonVisibility();
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }

    private void StartInteractKeyRebind()
    {
        waitingForInteractKey = true;
        if (interactKeyValueLabel != null)
            interactKeyValueLabel.text = "Nhấn phím...";
    }

    private void BackToMenu()
    {
        Time.timeScale = 1f;
        if (string.Equals(SceneManager.GetActiveScene().name, mainMenuSceneName, System.StringComparison.Ordinal))
        {
            Close();
            return;
        }

        GameManager.PrepareReturnToMainMenu();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ApplyVolume(string mixerParam, float linearValue)
    {
        if (audioMixer == null || string.IsNullOrWhiteSpace(mixerParam)) return;

        float db = linearValue > 0.0001f ? Mathf.Log10(linearValue) * 20f : -80f;
        audioMixer.SetFloat(mixerParam, db);
    }

    private static void SetSlider(Slider slider, float value)
    {
        if (slider == null) return;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = Mathf.Clamp01(value);
    }

    private static void DestroyChild(GameObject child)
    {
        if (child == null)
            return;

        if (Application.isPlaying)
            Destroy(child);
        else
            DestroyImmediate(child);
    }

    private static void UpdateVolumeLabel(TMP_Text label, float value)
    {
        if (label == null) return;
        label.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private static void ApplyTargetFrameRate(int fps, bool vsyncEnabled)
    {
        Application.targetFrameRate = vsyncEnabled ? -1 : fps;
    }

    private void RefreshTargetFpsLabel()
    {
        if (targetFpsValueLabel == null) return;
        int fps = PlayerPrefs.GetInt(KeyTargetFps, 60);
        targetFpsValueLabel.text = fps < 0 ? "Không giới hạn" : $"{fps} FPS";
    }

    private void RefreshLanguageLabel()
    {
        if (languageValueLabel == null) return;
        languageValueLabel.text = PlayerPrefs.GetInt(KeyLang, 0) == 1 ? "English" : "Tiếng Việt";
    }

    private void RefreshInteractKeyLabel()
    {
        if (interactKeyValueLabel == null) return;
        var key = GameplayInputSettings.GetInteractKey();
        interactKeyValueLabel.text = GameplayInputSettings.FormatKey(key);
    }

    private void ApplyContextualButtonVisibility()
    {
        bool mainMenuContext = compactMainMenuMode
            || string.Equals(SceneManager.GetActiveScene().name, mainMenuSceneName, System.StringComparison.Ordinal);

        if (!mainMenuContext)
        {
            if (saveButton != null) saveButton.gameObject.SetActive(true);
            if (backToMenuButton != null) backToMenuButton.gameObject.SetActive(true);
            if (quitButton != null) quitButton.gameObject.SetActive(true);
            return;
        }

        if (saveButton != null) saveButton.gameObject.SetActive(false);
        if (backToMenuButton != null) backToMenuButton.gameObject.SetActive(false);
        if (quitButton != null) quitButton.gameObject.SetActive(false);

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(true);
            var rect = closeButton.GetComponent<RectTransform>();
            if (rect != null)
                rect.anchoredPosition = new Vector2(0f, 34f);
        }
    }

    private static bool TryReadPressedKeyboardKey(out KeyCode key)
    {
        foreach (KeyCode candidate in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (!IsAllowedRebindKey(candidate)) continue;
            if (!Input.GetKeyDown(candidate)) continue;

            key = candidate;
            return true;
        }

        key = KeyCode.None;
        return false;
    }

    private static bool IsAllowedRebindKey(KeyCode key)
    {
        if (key == KeyCode.None || key == KeyCode.Escape || key == KeyCode.Tab)
            return false;

        string name = key.ToString();
        return !name.StartsWith("Mouse") && !name.StartsWith("Joystick");
    }

    private static void CreateSliderRow(Transform parent, string key, string label, float y)
    {
        var row = CreateUiObject($"{key}Row", parent);
        SetRect(row, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(0f, 52f));

        var labelText = CreateText($"Label{key}", row, label, 19f, TextAlignmentOptions.MidlineLeft, MenuWindowShellUI.BodyTextColor);
        SetRect(labelText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(190f, 0f));

        var sliderRoot = CreateUiObject($"Slider{key}", row);
        SetRect(sliderRoot, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(102f, 0f), new Vector2(-286f, 22f));
        var slider = sliderRoot.gameObject.AddComponent<Slider>();

        var background = CreateImage("Background", sliderRoot, MenuWindowShellUI.SurfaceAltColor);
        Stretch(background.rectTransform, Vector2.zero, Vector2.zero);

        var fillArea = CreateUiObject("Fill Area", sliderRoot);
        Stretch(fillArea, new Vector2(8f, 0f), new Vector2(-8f, 0f));

        var fill = CreateImage("Fill", fillArea, MenuWindowShellUI.AccentColor);
        Stretch(fill.rectTransform, Vector2.zero, Vector2.zero);

        var handleArea = CreateUiObject("Handle Slide Area", sliderRoot);
        Stretch(handleArea, new Vector2(8f, 0f), new Vector2(-8f, 0f));

        var handle = CreateImage("Handle", handleArea, MenuWindowShellUI.AccentSoftColor);
        SetRect(handle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 32f));

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        var valueText = CreateText($"Label{key}Value", row, "100%", 18f, TextAlignmentOptions.MidlineRight, MenuWindowShellUI.BodyTextColor);
        SetRect(valueText.rectTransform, new Vector2(1f, 0f), Vector2.one, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(80f, 0f));
    }

    private static void CreateLanguageRow(Transform parent, float y)
    {
        CreateButtonValueRow(parent, "Language", "Ngôn ngữ", "Tiếng Việt", y, "LanguageButton", "LanguageValue");
    }

    private static void CreateInteractKeyRow(Transform parent, float y)
    {
        CreateButtonValueRow(parent, "InteractKey", "Phím tương tác", "E", y, "InteractKeyButton", "InteractKeyValue");
    }

    private static void CreateButtonValueRow(Transform parent, string key, string label, string value, float y, string buttonName = null, string valueName = null)
    {
        var row = CreateUiObject($"{key}Row", parent);
        SetRect(row, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(0f, 48f));

        var labelText = CreateText($"Label{key}", row, label, 19f, TextAlignmentOptions.MidlineLeft, MenuWindowShellUI.BodyTextColor);
        SetRect(labelText.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);

        var buttonImage = CreateImage(buttonName ?? $"{key}Button", row, MenuWindowShellUI.SurfaceColor);
        SetRect(buttonImage.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(190f, 40f));
        var button = buttonImage.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        var valueText = CreateText(valueName ?? $"{key}Value", buttonImage.transform, value, 18f, TextAlignmentOptions.Center, MenuWindowShellUI.AccentSoftColor);
        Stretch(valueText.rectTransform, Vector2.zero, Vector2.zero);
    }

    private static void CreateToggleRow(Transform parent, string key, string label, float y, bool defaultValue)
    {
        var row = CreateUiObject($"{key}Row", parent);
        SetRect(row, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(0f, 48f));

        var labelText = CreateText($"Label{key}", row, label, 19f, TextAlignmentOptions.MidlineLeft, MenuWindowShellUI.BodyTextColor);
        SetRect(labelText.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);

        var toggleRoot = CreateUiObject($"{key}Toggle", row);
        SetRect(toggleRoot, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(70f, 38f));
        var toggle = toggleRoot.gameObject.AddComponent<Toggle>();

        var background = CreateImage("Background", toggleRoot, MenuWindowShellUI.SurfaceColor);
        Stretch(background.rectTransform, Vector2.zero, Vector2.zero);

        var checkmark = CreateImage("Checkmark", background.transform, MenuWindowShellUI.AccentSoftColor);
        SetRect(checkmark.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36f, 22f));

        toggle.targetGraphic = background;
        toggle.graphic = checkmark;
        toggle.isOn = defaultValue;
    }

    private static void CreateResetRow(Transform parent, float y)
    {
        var buttonImage = CreateImage("ResetDefaultsButton", parent, MenuWindowShellUI.SurfaceColor);
        SetRect(buttonImage.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, y), new Vector2(190f, 42f));
        var button = buttonImage.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        var text = CreateText("Label", buttonImage.transform, "Mặc định", 18f, TextAlignmentOptions.Center, MenuWindowShellUI.AccentSoftColor);
        Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
    }

    private static Button CreateBasicButton(string name, Transform parent, string label, Vector2 position)
    {
        var root = CreateImage(name, parent, MenuWindowShellUI.SurfaceColor);
        SetRect(root.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), position, new Vector2(160f, 48f));
        var button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = root;

        var text = CreateText("Label", root.transform, label, 20f, TextAlignmentOptions.Center, MenuWindowShellUI.AccentSoftColor);
        Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
        return button;
    }

    private static RectTransform CreateUiObject(string name, Transform parent)
    {
        return MenuWindowShellUI.CreateUiObject(name, parent);
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        return MenuWindowShellUI.CreateImage(name, parent, color);
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float size, TextAlignmentOptions alignment, Color color)
    {
        return MenuWindowShellUI.CreateText(name, parent, value, size, alignment, color);
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        MenuWindowShellUI.Stretch(rect, offsetMin, offsetMax);
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        MenuWindowShellUI.SetRect(rect, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
    }
}
