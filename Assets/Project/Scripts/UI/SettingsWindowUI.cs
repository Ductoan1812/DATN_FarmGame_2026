using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Quản lý SettingsWindow:
/// - 3 slider âm lượng: Master / Music / SFX (lưu PlayerPrefs)
/// - Dropdown ngôn ngữ (gọi LocalizationManager)
/// - Đổi phím tương tác gameplay
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

    [Header("Input")]
    [SerializeField] private Button interactKeyButton;
    [SerializeField] private TMP_Text interactKeyValueLabel;

    [Header("Buttons")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Navigation")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Header("Optional AudioMixer (để trống nếu chưa có)")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string mixerParamMaster = "VolMaster";
    [SerializeField] private string mixerParamMusic  = "VolMusic";
    [SerializeField] private string mixerParamSfx    = "VolSfx";

    private bool listenersRegistered;
    private bool waitingForInteractKey;

    // ── Lifecycle ────────────────────────────────────────────────

    private void OnEnable()
    {
        EnsureBasicLayout();
        AutoFindRefs();
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
        if (!waitingForInteractKey) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            waitingForInteractKey = false;
            RefreshInteractKeyLabel();
            return;
        }

        if (!Input.anyKeyDown) return;
        if (!TryReadPressedKeyboardKey(out var key)) return;

        GameplayInputSettings.SetInteractKey(key);
        waitingForInteractKey = false;
        RefreshInteractKeyLabel();
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
        interactKeyButton ??= FindDeepChild(transform, "InteractKeyButton")?.GetComponent<Button>();
        interactKeyValueLabel ??= FindDeepChild(transform, "InteractKeyValue")?.GetComponent<TMP_Text>();
        saveButton       ??= FindDeepChild(transform, "SaveButton")?.GetComponent<Button>();
        closeButton      ??= FindDeepChild(transform, "CloseButton")?.GetComponent<Button>();
        backToMenuButton ??= FindDeepChild(transform, "BackToMenuButton")?.GetComponent<Button>();
        quitButton       ??= FindDeepChild(transform, "QuitButton")?.GetComponent<Button>();
    }

    private void EnsureBasicLayout()
    {
        if (FindDeepChild(transform, "SliderMaster") != null
            && FindDeepChild(transform, "InteractKeyButton") != null
            && FindDeepChild(transform, "BackToMenuButton") != null
            && FindDeepChild(transform, "QuitButton") != null)
            return;

        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        var bg = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        bg.color = new Color(0.96f, 0.82f, 0.52f, 0.96f);

        var outline = GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.78f, 0.54f, 0.20f, 1f);
        outline.effectDistance = new Vector2(3f, -3f);

        var header = CreateUiObject("Header", transform);
        SetRect(header, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 72f));
        var headerImage = header.gameObject.AddComponent<Image>();
        headerImage.color = new Color(0.30f, 0.17f, 0.07f, 0.95f);

        var title = CreateText("TitleText", header, "Cài đặt", 28f, TextAlignmentOptions.Center, new Color(0.98f, 0.88f, 0.55f));
        Stretch(title.rectTransform, new Vector2(24f, 0f), new Vector2(-24f, 0f));

        var body = CreateUiObject("Body", transform);
        SetRect(body, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -42f), new Vector2(-96f, -128f));

        CreateSliderRow(body, "Master", "Âm lượng tổng", -38f);
        CreateSliderRow(body, "Music", "Nhạc nền", -118f);
        CreateSliderRow(body, "Sfx", "Hiệu ứng", -198f);
        CreateValueRow(body, "LanguageRow", "Ngôn ngữ", "Tiếng Việt", -278f);
        CreateInteractKeyRow(body, -338f);

        CreateBasicButton("SaveButton", transform, "Lưu", new Vector2(-300f, 34f));
        CreateBasicButton("CloseButton", transform, "Đóng", new Vector2(-100f, 34f));
        CreateBasicButton("BackToMenuButton", transform, "Về menu", new Vector2(100f, 34f));
        CreateBasicButton("QuitButton", transform, "Thoát game", new Vector2(300f, 34f));
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

        RefreshInteractKeyLabel();
    }

    private void RegisterListeners()
    {
        if (listenersRegistered) return;

        if (sliderMaster != null) sliderMaster.onValueChanged.AddListener(OnMasterChanged);
        if (sliderMusic  != null) sliderMusic.onValueChanged.AddListener(OnMusicChanged);
        if (sliderSfx    != null) sliderSfx.onValueChanged.AddListener(OnSfxChanged);
        if (interactKeyButton != null) interactKeyButton.onClick.AddListener(StartInteractKeyRebind);
        if (saveButton   != null) saveButton.onClick.AddListener(Save);
        if (closeButton  != null) closeButton.onClick.AddListener(Close);
        if (backToMenuButton != null) backToMenuButton.onClick.AddListener(BackToMenu);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

        listenersRegistered = true;
    }

    private void UnregisterListeners()
    {
        if (!listenersRegistered) return;

        if (sliderMaster != null) sliderMaster.onValueChanged.RemoveListener(OnMasterChanged);
        if (sliderMusic  != null) sliderMusic.onValueChanged.RemoveListener(OnMusicChanged);
        if (sliderSfx    != null) sliderSfx.onValueChanged.RemoveListener(OnSfxChanged);
        if (interactKeyButton != null) interactKeyButton.onClick.RemoveListener(StartInteractKeyRebind);
        if (saveButton   != null) saveButton.onClick.RemoveListener(Save);
        if (closeButton  != null) closeButton.onClick.RemoveListener(Close);
        if (backToMenuButton != null) backToMenuButton.onClick.RemoveListener(BackToMenu);
        if (quitButton != null) quitButton.onClick.RemoveListener(QuitGame);

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

        GameplayInputSettings.SetInteractKey(GameplayInputSettings.GetInteractKey());
        PlayerPrefs.Save();
        Close();
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

    private void RefreshInteractKeyLabel()
    {
        if (interactKeyValueLabel == null) return;
        var key = GameplayInputSettings.GetInteractKey();
        interactKeyValueLabel.text = GameplayInputSettings.FormatKey(key);
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

    private static void CreateSliderRow(Transform parent, string key, string label, float y)
    {
        var row = CreateUiObject($"{key}Row", parent);
        SetRect(row, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(0f, 56f));

        var labelText = CreateText($"Label{key}", row, label, 20f, TextAlignmentOptions.MidlineLeft, new Color(0.28f, 0.18f, 0.08f));
        SetRect(labelText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(180f, 0f));

        var sliderRoot = CreateUiObject($"Slider{key}", row);
        SetRect(sliderRoot, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(86f, 0f), new Vector2(-260f, 24f));
        var slider = sliderRoot.gameObject.AddComponent<Slider>();

        var background = CreateImage("Background", sliderRoot, new Color(0.24f, 0.14f, 0.06f, 0.92f));
        Stretch(background.rectTransform, Vector2.zero, Vector2.zero);

        var fillArea = CreateUiObject("Fill Area", sliderRoot);
        Stretch(fillArea, new Vector2(8f, 0f), new Vector2(-8f, 0f));

        var fill = CreateImage("Fill", fillArea, new Color(0.86f, 0.55f, 0.18f, 1f));
        Stretch(fill.rectTransform, Vector2.zero, Vector2.zero);

        var handleArea = CreateUiObject("Handle Slide Area", sliderRoot);
        Stretch(handleArea, new Vector2(8f, 0f), new Vector2(-8f, 0f));

        var handle = CreateImage("Handle", handleArea, new Color(1f, 0.86f, 0.45f, 1f));
        SetRect(handle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 32f));

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        var valueText = CreateText($"Label{key}Value", row, "100%", 18f, TextAlignmentOptions.MidlineRight, new Color(0.28f, 0.18f, 0.08f));
        SetRect(valueText.rectTransform, new Vector2(1f, 0f), Vector2.one, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(80f, 0f));
    }

    private static void CreateValueRow(Transform parent, string name, string label, string value, float y)
    {
        var row = CreateUiObject(name, parent);
        SetRect(row, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(0f, 52f));

        var labelText = CreateText("Label", row, label, 20f, TextAlignmentOptions.MidlineLeft, new Color(0.28f, 0.18f, 0.08f));
        SetRect(labelText.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);

        var valueText = CreateText("Value", row, value, 20f, TextAlignmentOptions.MidlineRight, new Color(0.28f, 0.18f, 0.08f));
        SetRect(valueText.rectTransform, new Vector2(0.5f, 0f), Vector2.one, new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);
    }

    private static void CreateInteractKeyRow(Transform parent, float y)
    {
        var row = CreateUiObject("InteractKeyRow", parent);
        SetRect(row, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(0f, 52f));

        var labelText = CreateText("Label", row, "Phím tương tác", 20f, TextAlignmentOptions.MidlineLeft, new Color(0.28f, 0.18f, 0.08f));
        SetRect(labelText.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);

        var buttonImage = CreateImage("InteractKeyButton", row, new Color(0.34f, 0.20f, 0.08f, 1f));
        SetRect(buttonImage.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(160f, 42f));
        var button = buttonImage.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        var valueText = CreateText("InteractKeyValue", buttonImage.transform, "E", 20f, TextAlignmentOptions.Center, new Color(1f, 0.90f, 0.66f));
        Stretch(valueText.rectTransform, Vector2.zero, Vector2.zero);
    }

    private static Button CreateBasicButton(string name, Transform parent, string label, Vector2 position)
    {
        var root = CreateImage(name, parent, new Color(0.34f, 0.20f, 0.08f, 1f));
        SetRect(root.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), position, new Vector2(160f, 48f));
        var button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = root;

        var text = CreateText("Label", root.transform, label, 20f, TextAlignmentOptions.Center, new Color(1f, 0.90f, 0.66f));
        Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
        return button;
    }

    private static RectTransform CreateUiObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float size, TextAlignmentOptions alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = true;
        return text;
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }
}
