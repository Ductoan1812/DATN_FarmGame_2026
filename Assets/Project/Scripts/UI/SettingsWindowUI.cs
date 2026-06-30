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
    private const string KeyResolutionIndex = "settings_resolution_index";
    private const string KeyWindowMode = "settings_window_mode";

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
    [SerializeField] private Button moveUpKeyButton;
    [SerializeField] private TMP_Text moveUpKeyValueLabel;
    [SerializeField] private Button moveDownKeyButton;
    [SerializeField] private TMP_Text moveDownKeyValueLabel;
    [SerializeField] private Button moveLeftKeyButton;
    [SerializeField] private TMP_Text moveLeftKeyValueLabel;
    [SerializeField] private Button moveRightKeyButton;
    [SerializeField] private TMP_Text moveRightKeyValueLabel;
    [SerializeField] private Button primaryActionKeyButton;
    [SerializeField] private TMP_Text primaryActionKeyValueLabel;
    [SerializeField] private Button secondaryActionKeyButton;
    [SerializeField] private TMP_Text secondaryActionKeyValueLabel;

    [Header("Display")]
    [SerializeField] private Button resolutionButton;
    [SerializeField] private TMP_Text resolutionValueLabel;
    [SerializeField] private Button windowModeButton;
    [SerializeField] private TMP_Text windowModeValueLabel;
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
    private GameplayInputAction? waitingForInputAction;
    private ResolutionOption[] resolutionOptions;

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
        waitingForInputAction = null;
    }

    private void Update()
    {
        if (!waitingForInputAction.HasValue)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            waitingForInputAction = null;
            RefreshInputKeyLabels();
            return;
        }

        if (!Input.anyKeyDown || !TryReadPressedKeyboardKey(out var key))
            return;

        GameplayInputSettings.SetKey(waitingForInputAction.Value, key);
        PlayerPrefs.Save();
        waitingForInputAction = null;
        RefreshInputKeyLabels();
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
        moveUpKeyButton ??= MenuWindowShellUI.FindDeepChild(transform, "MoveUpKeyButton")?.GetComponent<Button>();
        moveUpKeyValueLabel ??= MenuWindowShellUI.FindDeepChild(transform, "MoveUpKeyValue")?.GetComponent<TMP_Text>();
        moveDownKeyButton ??= MenuWindowShellUI.FindDeepChild(transform, "MoveDownKeyButton")?.GetComponent<Button>();
        moveDownKeyValueLabel ??= MenuWindowShellUI.FindDeepChild(transform, "MoveDownKeyValue")?.GetComponent<TMP_Text>();
        moveLeftKeyButton ??= MenuWindowShellUI.FindDeepChild(transform, "MoveLeftKeyButton")?.GetComponent<Button>();
        moveLeftKeyValueLabel ??= MenuWindowShellUI.FindDeepChild(transform, "MoveLeftKeyValue")?.GetComponent<TMP_Text>();
        moveRightKeyButton ??= MenuWindowShellUI.FindDeepChild(transform, "MoveRightKeyButton")?.GetComponent<Button>();
        moveRightKeyValueLabel ??= MenuWindowShellUI.FindDeepChild(transform, "MoveRightKeyValue")?.GetComponent<TMP_Text>();
        primaryActionKeyButton ??= MenuWindowShellUI.FindDeepChild(transform, "PrimaryActionKeyButton")?.GetComponent<Button>();
        primaryActionKeyValueLabel ??= MenuWindowShellUI.FindDeepChild(transform, "PrimaryActionKeyValue")?.GetComponent<TMP_Text>();
        secondaryActionKeyButton ??= MenuWindowShellUI.FindDeepChild(transform, "SecondaryActionKeyButton")?.GetComponent<Button>();
        secondaryActionKeyValueLabel ??= MenuWindowShellUI.FindDeepChild(transform, "SecondaryActionKeyValue")?.GetComponent<TMP_Text>();
        resolutionButton ??= MenuWindowShellUI.FindDeepChild(transform, "ResolutionButton")?.GetComponent<Button>();
        resolutionValueLabel ??= MenuWindowShellUI.FindDeepChild(transform, "ResolutionValue")?.GetComponent<TMP_Text>();
        windowModeButton ??= MenuWindowShellUI.FindDeepChild(transform, "WindowModeButton")?.GetComponent<Button>();
        windowModeValueLabel ??= MenuWindowShellUI.FindDeepChild(transform, "WindowModeValue")?.GetComponent<TMP_Text>();
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
        bool hasRequiredLayout = MenuWindowShellUI.FindDeepChild(transform, "SettingsLayoutV2") != null
            && MenuWindowShellUI.FindDeepChild(transform, "SliderMaster") != null
            && MenuWindowShellUI.FindDeepChild(transform, "MoveUpKeyButton") != null
            && MenuWindowShellUI.FindDeepChild(transform, "MoveDownKeyButton") != null
            && MenuWindowShellUI.FindDeepChild(transform, "MoveLeftKeyButton") != null
            && MenuWindowShellUI.FindDeepChild(transform, "MoveRightKeyButton") != null
            && MenuWindowShellUI.FindDeepChild(transform, "PrimaryActionKeyButton") != null
            && MenuWindowShellUI.FindDeepChild(transform, "SecondaryActionKeyButton") != null
            && MenuWindowShellUI.FindDeepChild(transform, "ResolutionButton") != null
            && MenuWindowShellUI.FindDeepChild(transform, "WindowModeButton") != null
            && MenuWindowShellUI.FindDeepChild(transform, "FullscreenToggle") != null
            && MenuWindowShellUI.FindDeepChild(transform, "VsyncToggle") != null
            && MenuWindowShellUI.FindDeepChild(transform, "TargetFpsButton") != null
            && MenuWindowShellUI.FindDeepChild(transform, "ResetDefaultsButton") != null
            && MenuWindowShellUI.FindDeepChild(transform, "BackToMenuButton") != null
            && MenuWindowShellUI.FindDeepChild(transform, "QuitButton") != null;

        if (hasRequiredLayout)
        {
            SetLocalizedTitle(transform);
            AutoFindRefs();
            ApplyContextualButtonVisibility();
            return;
        }

        Debug.LogWarning($"[SettingsWindowUI] Missing authored layout on '{name}'. No supported layout source was found.");
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
        moveUpKeyButton = null;
        moveUpKeyValueLabel = null;
        moveDownKeyButton = null;
        moveDownKeyValueLabel = null;
        moveLeftKeyButton = null;
        moveLeftKeyValueLabel = null;
        moveRightKeyButton = null;
        moveRightKeyValueLabel = null;
        primaryActionKeyButton = null;
        primaryActionKeyValueLabel = null;
        secondaryActionKeyButton = null;
        secondaryActionKeyValueLabel = null;
        resolutionButton = null;
        resolutionValueLabel = null;
        windowModeButton = null;
        windowModeValueLabel = null;
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

        CacheResolutionOptions();
        int resolutionIndex = PlayerPrefs.GetInt(KeyResolutionIndex, GetClosestResolutionIndex(Screen.width, Screen.height));
        DisplayWindowMode windowMode = GetSavedWindowMode();
        bool fullscreen = windowMode != DisplayWindowMode.Windowed;
        bool vsync = PlayerPrefs.GetInt(KeyVsync, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;
        int fps = PlayerPrefs.GetInt(KeyTargetFps, 60);

        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
        if (vsyncToggle != null)
            vsyncToggle.SetIsOnWithoutNotify(vsync);

        ApplyResolutionAndWindowMode(resolutionIndex, windowMode, persistResolution: false, persistWindowMode: false);
        QualitySettings.vSyncCount = vsync ? 1 : 0;
        ApplyTargetFrameRate(fps, vsync);
        RefreshResolutionLabel();
        RefreshWindowModeLabel();
        RefreshTargetFpsLabel();
        RefreshInputKeyLabels();
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
        if (moveUpKeyButton != null) moveUpKeyButton.onClick.AddListener(StartMoveUpKeyRebind);
        if (moveDownKeyButton != null) moveDownKeyButton.onClick.AddListener(StartMoveDownKeyRebind);
        if (moveLeftKeyButton != null) moveLeftKeyButton.onClick.AddListener(StartMoveLeftKeyRebind);
        if (moveRightKeyButton != null) moveRightKeyButton.onClick.AddListener(StartMoveRightKeyRebind);
        if (primaryActionKeyButton != null) primaryActionKeyButton.onClick.AddListener(StartPrimaryActionKeyRebind);
        if (secondaryActionKeyButton != null) secondaryActionKeyButton.onClick.AddListener(StartSecondaryActionKeyRebind);
        if (resolutionButton != null) resolutionButton.onClick.AddListener(CycleResolution);
        if (windowModeButton != null) windowModeButton.onClick.AddListener(CycleWindowMode);
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
        if (moveUpKeyButton != null) moveUpKeyButton.onClick.RemoveListener(StartMoveUpKeyRebind);
        if (moveDownKeyButton != null) moveDownKeyButton.onClick.RemoveListener(StartMoveDownKeyRebind);
        if (moveLeftKeyButton != null) moveLeftKeyButton.onClick.RemoveListener(StartMoveLeftKeyRebind);
        if (moveRightKeyButton != null) moveRightKeyButton.onClick.RemoveListener(StartMoveRightKeyRebind);
        if (primaryActionKeyButton != null) primaryActionKeyButton.onClick.RemoveListener(StartPrimaryActionKeyRebind);
        if (secondaryActionKeyButton != null) secondaryActionKeyButton.onClick.RemoveListener(StartSecondaryActionKeyRebind);
        if (resolutionButton != null) resolutionButton.onClick.RemoveListener(CycleResolution);
        if (windowModeButton != null) windowModeButton.onClick.RemoveListener(CycleWindowMode);
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
        RefreshWindowModeLabel();
        RefreshTargetFpsLabel();
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
        var mode = value ? DisplayWindowMode.BorderlessFullscreen : DisplayWindowMode.Windowed;
        int resolutionIndex = PlayerPrefs.GetInt(KeyResolutionIndex, GetClosestResolutionIndex(Screen.width, Screen.height));
        ApplyResolutionAndWindowMode(resolutionIndex, mode, persistResolution: false, persistWindowMode: true);
        PlayerPrefs.SetInt(KeyFullscreen, value ? 1 : 0);
        RefreshWindowModeLabel();
        RefreshResolutionLabel();
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
        CacheResolutionOptions();
        PlayerPrefs.SetInt(KeyResolutionIndex, GetClosestResolutionIndex(Screen.currentResolution.width, Screen.currentResolution.height));
        PlayerPrefs.SetInt(KeyWindowMode, (int)DisplayWindowMode.Windowed);
        PlayerPrefs.SetInt(KeyFullscreen, 0);
        PlayerPrefs.SetInt(KeyVsync, 0);
        PlayerPrefs.SetInt(KeyTargetFps, 60);
        GameplayInputSettings.ResetDefaults();
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

    public void RebuildMainMenuAuthoredLayout()
    {
        Transform content = MenuWindowShellUI.FindDeepChild(transform, "Content");
        if (content == null)
        {
            Debug.LogWarning($"[SettingsWindowUI] Cannot rebuild main menu layout on '{name}' because 'Content' was not found.");
            return;
        }

        ClearGeneratedRefs();
        BuildMainMenuSettingsLayout(content);
        SetLocalizedTitle(transform);
        AutoFindRefs();
        ApplyContextualButtonVisibility();
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }

    private void StartInteractKeyRebind()
    {
        StartKeyRebind(GameplayInputAction.SecondaryAction);
    }

    private void StartMoveUpKeyRebind() => StartKeyRebind(GameplayInputAction.MoveUp);
    private void StartMoveDownKeyRebind() => StartKeyRebind(GameplayInputAction.MoveDown);
    private void StartMoveLeftKeyRebind() => StartKeyRebind(GameplayInputAction.MoveLeft);
    private void StartMoveRightKeyRebind() => StartKeyRebind(GameplayInputAction.MoveRight);
    private void StartPrimaryActionKeyRebind() => StartKeyRebind(GameplayInputAction.PrimaryAction);
    private void StartSecondaryActionKeyRebind() => StartKeyRebind(GameplayInputAction.SecondaryAction);

    private void StartKeyRebind(GameplayInputAction action)
    {
        waitingForInputAction = action;
        var label = GetInputValueLabel(action);
        if (label != null)
            label.text = LocalizationManager.Instance != null
                ? LocalizationManager.Instance.GetText(LocalizationKeys.UiSettingsRebindPrompt)
                : "Nhan phim...";
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

    private void CycleResolution()
    {
        CacheResolutionOptions();
        if (resolutionOptions == null || resolutionOptions.Length == 0)
            return;

        int current = PlayerPrefs.GetInt(KeyResolutionIndex, GetClosestResolutionIndex(Screen.width, Screen.height));
        int next = (current + 1) % resolutionOptions.Length;
        ApplyResolutionAndWindowMode(next, GetSavedWindowMode(), persistResolution: true, persistWindowMode: false);
        RefreshResolutionLabel();
        PlayerPrefs.Save();
    }

    private void CycleWindowMode()
    {
        var next = GetSavedWindowMode() switch
        {
            DisplayWindowMode.Windowed => DisplayWindowMode.BorderlessFullscreen,
            DisplayWindowMode.BorderlessFullscreen => DisplayWindowMode.ExclusiveFullscreen,
            _ => DisplayWindowMode.Windowed
        };

        int resolutionIndex = PlayerPrefs.GetInt(KeyResolutionIndex, GetClosestResolutionIndex(Screen.width, Screen.height));
        ApplyResolutionAndWindowMode(resolutionIndex, next, persistResolution: false, persistWindowMode: true);
        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(next != DisplayWindowMode.Windowed);
        RefreshWindowModeLabel();
        RefreshResolutionLabel();
        PlayerPrefs.Save();
    }

    private void RefreshResolutionLabel()
    {
        if (resolutionValueLabel == null)
            return;

        CacheResolutionOptions();
        int index = Mathf.Clamp(PlayerPrefs.GetInt(KeyResolutionIndex, GetClosestResolutionIndex(Screen.width, Screen.height)), 0, Mathf.Max(0, resolutionOptions.Length - 1));
        resolutionValueLabel.text = resolutionOptions != null && resolutionOptions.Length > 0
            ? resolutionOptions[index].Label
            : $"{Screen.width} x {Screen.height}";
    }

    private void RefreshWindowModeLabel()
    {
        if (windowModeValueLabel == null)
            return;

        windowModeValueLabel.text = GetWindowModeDisplayName(GetSavedWindowMode());
    }

    private void RefreshTargetFpsLabel()
    {
        if (targetFpsValueLabel == null) return;
        int fps = PlayerPrefs.GetInt(KeyTargetFps, 60);
        if (fps < 0)
        {
            targetFpsValueLabel.text = LocalizationManager.Instance != null
                ? LocalizationManager.Instance.GetText(LocalizationKeys.UiSettingsFpsUnlimited)
                : "Không giới hạn";
        }
        else
        {
            targetFpsValueLabel.text = $"{fps} FPS";
        }
    }

    private void RefreshLanguageLabel()
    {
        if (languageValueLabel == null) return;
        languageValueLabel.text = PlayerPrefs.GetInt(KeyLang, 0) == 1 ? "English" : "Tiếng Việt";
    }

    private void RefreshInputKeyLabels()
    {
        SetInputKeyLabel(moveUpKeyValueLabel, GameplayInputAction.MoveUp);
        SetInputKeyLabel(moveDownKeyValueLabel, GameplayInputAction.MoveDown);
        SetInputKeyLabel(moveLeftKeyValueLabel, GameplayInputAction.MoveLeft);
        SetInputKeyLabel(moveRightKeyValueLabel, GameplayInputAction.MoveRight);
        SetInputKeyLabel(primaryActionKeyValueLabel, GameplayInputAction.PrimaryAction);
        SetInputKeyLabel(secondaryActionKeyValueLabel, GameplayInputAction.SecondaryAction);
        SetInputKeyLabel(interactKeyValueLabel, GameplayInputAction.SecondaryAction);
    }

    private static void SetInputKeyLabel(TMP_Text label, GameplayInputAction action)
    {
        if (label == null)
            return;

        label.text = GameplayInputSettings.FormatKey(GameplayInputSettings.GetKey(action));
    }

    private TMP_Text GetInputValueLabel(GameplayInputAction action)
    {
        return action switch
        {
            GameplayInputAction.MoveUp => moveUpKeyValueLabel,
            GameplayInputAction.MoveDown => moveDownKeyValueLabel,
            GameplayInputAction.MoveLeft => moveLeftKeyValueLabel,
            GameplayInputAction.MoveRight => moveRightKeyValueLabel,
            GameplayInputAction.PrimaryAction => primaryActionKeyValueLabel,
            GameplayInputAction.SecondaryAction => secondaryActionKeyValueLabel ?? interactKeyValueLabel,
            _ => null
        };
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
                rect.anchoredPosition = new Vector2(-100f, 34f);
        }
    }

    private void BuildMainMenuSettingsLayout(Transform content)
    {
        if (content == null)
            return;

        var contentRect = content as RectTransform;
        if (contentRect != null)
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, 1120f);

        var verticalLayout = content.GetComponent<VerticalLayoutGroup>();
        if (verticalLayout != null)
            verticalLayout.enabled = false;

        var existing = MenuWindowShellUI.FindDeepChild(content, "SettingsLayoutV2");
        if (existing != null)
            DestroyChild(existing.gameObject);

        var layout = CreateUiObject("SettingsLayoutV2", content);
        SetRect(layout, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(-24f, 1120f));

        float y = -12f;
        CreateSliderRow(layout, "Master", LocalizationKeys.UiSettingsAudioMaster, y);
        y -= 64f;
        CreateSliderRow(layout, "Music", LocalizationKeys.UiSettingsAudioMusic, y);
        y -= 64f;
        CreateSliderRow(layout, "Sfx", LocalizationKeys.UiSettingsAudioSfx, y);
        y -= 72f;
        CreateLanguageRow(layout, y);
        y -= 56f;
        CreateInputKeyRow(layout, "MoveUp", LocalizationKeys.UiSettingsMoveUp, "W", y, "MoveUpKeyButton", "MoveUpKeyValue");
        y -= 56f;
        CreateInputKeyRow(layout, "MoveDown", LocalizationKeys.UiSettingsMoveDown, "S", y, "MoveDownKeyButton", "MoveDownKeyValue");
        y -= 56f;
        CreateInputKeyRow(layout, "MoveLeft", LocalizationKeys.UiSettingsMoveLeft, "A", y, "MoveLeftKeyButton", "MoveLeftKeyValue");
        y -= 56f;
        CreateInputKeyRow(layout, "MoveRight", LocalizationKeys.UiSettingsMoveRight, "D", y, "MoveRightKeyButton", "MoveRightKeyValue");
        y -= 56f;
        CreateInputKeyRow(layout, "PrimaryAction", LocalizationKeys.UiSettingsPrimaryAction, "Space", y, "PrimaryActionKeyButton", "PrimaryActionKeyValue");
        y -= 56f;
        CreateInputKeyRow(layout, "SecondaryAction", LocalizationKeys.UiSettingsSecondaryAction, "E", y, "SecondaryActionKeyButton", "SecondaryActionKeyValue");
        y -= 56f;
        CreateButtonValueRow(layout, "Resolution", LocalizationKeys.UiSettingsResolution, "1920 x 1080", y, "ResolutionButton", "ResolutionValue");
        y -= 56f;
        CreateButtonValueRow(layout, "WindowMode", LocalizationKeys.UiSettingsWindowMode, "Windowed", y, "WindowModeButton", "WindowModeValue");
        y -= 56f;
        CreateToggleRow(layout, "Fullscreen", LocalizationKeys.UiSettingsFullscreen, y, true);
        y -= 56f;
        CreateToggleRow(layout, "Vsync", LocalizationKeys.UiSettingsVsync, y, false);
        y -= 56f;
        CreateButtonValueRow(layout, "TargetFps", LocalizationKeys.UiSettingsFps, "60 FPS", y, "TargetFpsButton", "TargetFpsValue");

        CreateResetRow(layout, new Vector2(120f, 34f));
        CreateLocalizedButton("CloseButton", layout, LocalizationKeys.UiSettingsClose, new Vector2(-120f, 34f));
        CreateLocalizedButton("SaveButton", layout, LocalizationKeys.UiSettingsSave, new Vector2(-300f, 34f));
        CreateLocalizedButton("BackToMenuButton", layout, LocalizationKeys.UiSettingsMainMenu, new Vector2(300f, 34f));
        CreateLocalizedButton("QuitButton", layout, LocalizationKeys.UiSettingsQuit, new Vector2(480f, 34f));

        UiTextStyleUtility.ApplyRobotoToChildren(layout);
        RefreshGeneratedToggles(layout);
    }

    private static void RefreshGeneratedToggles(Transform layout)
    {
        if (layout == null)
            return;

        ConfigureGeneratedToggle(layout, "FullscreenToggle");
        ConfigureGeneratedToggle(layout, "VsyncToggle");
    }

    private static void ConfigureGeneratedToggle(Transform layout, string toggleName)
    {
        var toggle = MenuWindowShellUI.FindDeepChild(layout, toggleName)?.GetComponent<Toggle>();
        if (toggle == null)
            return;

        var background = MenuWindowShellUI.FindDeepChild(toggle.transform, "Background")?.GetComponent<Image>();
        var checkmark = MenuWindowShellUI.FindDeepChild(toggle.transform, "Checkmark")?.GetComponent<Image>();
        if (background == null || checkmark == null)
            return;

        toggle.onValueChanged.RemoveListener(UpdateVisual);
        toggle.onValueChanged.AddListener(UpdateVisual);
        UpdateVisual(toggle.isOn);

        void UpdateVisual(bool isOn)
        {
            background.color = isOn ? MenuWindowShellUI.AccentColor : MenuWindowShellUI.SurfaceColor;
            checkmark.enabled = isOn;
            checkmark.color = MenuWindowShellUI.AccentSoftColor;
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

    private static void CreateSliderRow(Transform parent, string key, string labelKey, float y)
    {
        var row = CreateUiObject($"{key}Row", parent);
        SetRect(row, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(0f, 52f));

        var labelText = CreateText($"Label{key}", row, string.Empty, 19f, TextAlignmentOptions.MidlineLeft, MenuWindowShellUI.BodyTextColor);
        SetRect(labelText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(190f, 0f));
        AddLocalizedText(labelText, labelKey);

        var sliderRoot = CreateUiObject($"Slider{key}", row);
        // anchor stretch ngang, offsetMin.x=200 (sau label), offsetMax.x=-90 (trước value text)
        sliderRoot.anchorMin = Vector2.zero;
        sliderRoot.anchorMax = Vector2.one;
        sliderRoot.pivot = new Vector2(0.5f, 0.5f);
        sliderRoot.offsetMin = new Vector2(200f, 6f);
        sliderRoot.offsetMax = new Vector2(-90f, -6f);
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
        CreateButtonValueRow(parent, "Language", LocalizationKeys.UiSettingsLanguage, "Tiếng Việt", y, "LanguageButton", "LanguageValue");
    }

    private static void CreateInteractKeyRow(Transform parent, float y)
    {
        CreateButtonValueRow(parent, "InteractKey", LocalizationKeys.UiSettingsInteractKey, "E", y, "InteractKeyButton", "InteractKeyValue");
    }

    private static void CreateInputKeyRow(
        Transform parent,
        string key,
        string labelKey,
        string value,
        float y,
        string buttonName,
        string valueName)
    {
        CreateButtonValueRow(parent, key, labelKey, value, y, buttonName, valueName);
    }

    private static void CreateButtonValueRow(Transform parent, string key, string labelKey, string value, float y, string buttonName = null, string valueName = null)
    {
        var row = CreateUiObject($"{key}Row", parent);
        SetRect(row, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(0f, 48f));

        var labelText = CreateText($"Label{key}", row, string.Empty, 19f, TextAlignmentOptions.MidlineLeft, MenuWindowShellUI.BodyTextColor);
        SetRect(labelText.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        AddLocalizedText(labelText, labelKey);

        var buttonImage = CreateImage(buttonName ?? $"{key}Button", row, MenuWindowShellUI.SurfaceColor);
        SetRect(buttonImage.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(190f, 40f));
        var button = buttonImage.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        var valueText = CreateText(valueName ?? $"{key}Value", buttonImage.transform, value, 18f, TextAlignmentOptions.Center, MenuWindowShellUI.AccentSoftColor);
        Stretch(valueText.rectTransform, Vector2.zero, Vector2.zero);
    }

    private static void CreateToggleRow(Transform parent, string key, string labelKey, float y, bool defaultValue)
    {
        var row = CreateUiObject($"{key}Row", parent);
        SetRect(row, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(0f, 48f));

        var labelText = CreateText($"Label{key}", row, string.Empty, 19f, TextAlignmentOptions.MidlineLeft, MenuWindowShellUI.BodyTextColor);
        SetRect(labelText.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        AddLocalizedText(labelText, labelKey);

        var toggleRoot = CreateUiObject($"{key}Toggle", row);
        SetRect(toggleRoot, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(70f, 38f));
        var toggle = toggleRoot.gameObject.AddComponent<Toggle>();

        var background = CreateImage("Background", toggleRoot, MenuWindowShellUI.SurfaceColor);
        Stretch(background.rectTransform, Vector2.zero, Vector2.zero);
        background.type = Image.Type.Sliced;

        var checkmark = CreateImage("Checkmark", background.transform, MenuWindowShellUI.AccentSoftColor);
        SetRect(checkmark.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36f, 22f));

        toggle.targetGraphic = background;
        toggle.graphic = checkmark;
        toggle.isOn = defaultValue;
    }

    private static void CreateResetRow(Transform parent, Vector2 position)
    {
        var buttonImage = CreateImage("ResetDefaultsButton", parent, MenuWindowShellUI.SurfaceColor);
        SetRect(buttonImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), position, new Vector2(160f, 48f));
        var button = buttonImage.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        var text = CreateText("Label", buttonImage.transform, string.Empty, 20f, TextAlignmentOptions.Center, MenuWindowShellUI.AccentSoftColor);
        Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
        AddLocalizedText(text, LocalizationKeys.UiSettingsDefault);
    }

    private static Button CreateLocalizedButton(string name, Transform parent, string labelKey, Vector2 position)
    {
        var root = CreateImage(name, parent, MenuWindowShellUI.SurfaceColor);
        SetRect(root.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), position, new Vector2(160f, 48f));
        var button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = root;

        var text = CreateText("Label", root.transform, string.Empty, 20f, TextAlignmentOptions.Center, MenuWindowShellUI.AccentSoftColor);
        Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
        AddLocalizedText(text, labelKey);
        return button;
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

    private static void AddLocalizedText(TMP_Text text, string key)
    {
        if (text == null || string.IsNullOrEmpty(key)) return;
        var localized = text.gameObject.GetComponent<LocalizedText>() ?? text.gameObject.AddComponent<LocalizedText>();
        localized.SetKey(key);
    }

    private static void SetLocalizedTitle(Transform root)
    {
        var titleText = MenuWindowShellUI.FindDeepChild(root, "TitleText")?.GetComponent<TMP_Text>()
            ?? MenuWindowShellUI.FindDeepChild(root, "TitleName")?.GetComponent<TMP_Text>();
        if (titleText != null)
            AddLocalizedText(titleText, LocalizationKeys.UiSettingsTitle);
    }

    private void CacheResolutionOptions()
    {
        if (resolutionOptions != null && resolutionOptions.Length > 0)
            return;

        var options = new List<ResolutionOption>();
        var seen = new HashSet<string>();
        var resolutions = Screen.resolutions;

        for (int i = resolutions.Length - 1; i >= 0; i--)
        {
            var resolution = resolutions[i];
            string key = $"{resolution.width}x{resolution.height}";
            if (!seen.Add(key))
                continue;

            options.Add(new ResolutionOption(resolution.width, resolution.height));
        }

        if (options.Count == 0)
            options.Add(new ResolutionOption(Screen.width, Screen.height));

        resolutionOptions = options.ToArray();
    }

    private int GetClosestResolutionIndex(int width, int height)
    {
        CacheResolutionOptions();
        if (resolutionOptions == null || resolutionOptions.Length == 0)
            return 0;

        int bestIndex = 0;
        int bestScore = int.MaxValue;

        for (int i = 0; i < resolutionOptions.Length; i++)
        {
            int score = Mathf.Abs(resolutionOptions[i].Width - width) + Mathf.Abs(resolutionOptions[i].Height - height);
            if (score >= bestScore)
                continue;

            bestScore = score;
            bestIndex = i;
        }

        return bestIndex;
    }

    private void ApplyResolutionAndWindowMode(
        int index,
        DisplayWindowMode windowMode,
        bool persistResolution,
        bool persistWindowMode)
    {
        CacheResolutionOptions();
        if (resolutionOptions == null || resolutionOptions.Length == 0)
            return;

        index = Mathf.Clamp(index, 0, resolutionOptions.Length - 1);
        var option = resolutionOptions[index];
        Screen.SetResolution(option.Width, option.Height, ToUnityFullScreenMode(windowMode));

        if (persistResolution)
            PlayerPrefs.SetInt(KeyResolutionIndex, index);
        if (persistWindowMode)
        {
            PlayerPrefs.SetInt(KeyWindowMode, (int)windowMode);
            PlayerPrefs.SetInt(KeyFullscreen, windowMode == DisplayWindowMode.Windowed ? 0 : 1);
        }
    }

    private DisplayWindowMode GetSavedWindowMode()
    {
        if (PlayerPrefs.HasKey(KeyWindowMode))
            return (DisplayWindowMode)Mathf.Clamp(
                PlayerPrefs.GetInt(KeyWindowMode, (int)DisplayWindowMode.Windowed),
                0,
                (int)DisplayWindowMode.ExclusiveFullscreen);

        return PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1
            ? DisplayWindowMode.BorderlessFullscreen
            : DisplayWindowMode.Windowed;
    }

    private static FullScreenMode ToUnityFullScreenMode(DisplayWindowMode windowMode)
    {
        return windowMode switch
        {
            DisplayWindowMode.ExclusiveFullscreen => FullScreenMode.ExclusiveFullScreen,
            DisplayWindowMode.BorderlessFullscreen => FullScreenMode.FullScreenWindow,
            _ => FullScreenMode.Windowed
        };
    }

    private string GetWindowModeDisplayName(DisplayWindowMode windowMode)
    {
        string key = windowMode switch
        {
            DisplayWindowMode.ExclusiveFullscreen => LocalizationKeys.UiSettingsWindowModeFullscreen,
            DisplayWindowMode.BorderlessFullscreen => LocalizationKeys.UiSettingsWindowModeBorderless,
            _ => LocalizationKeys.UiSettingsWindowModeWindowed
        };

        return LocalizationManager.Instance != null
            ? LocalizationManager.Instance.GetText(key)
            : windowMode.ToString();
    }

    private enum DisplayWindowMode
    {
        Windowed = 0,
        BorderlessFullscreen = 1,
        ExclusiveFullscreen = 2
    }

    private readonly struct ResolutionOption
    {
        public readonly int Width;
        public readonly int Height;
        public string Label => $"{Width} x {Height}";

        public ResolutionOption(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }
}
