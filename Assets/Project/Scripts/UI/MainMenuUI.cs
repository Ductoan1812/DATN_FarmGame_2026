using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main menu for the playable demo. It intentionally keeps save/load orchestration
/// in SaveLoadManager by loading the first gameplay scene and letting boot restore data.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private string firstGameplaySceneName = "FarmScene";

    [Header("Optional Sprites")]
    [SerializeField] private Sprite panelSprite;
    [SerializeField] private Sprite buttonSprite;
    [SerializeField] private Sprite buttonSelectedSprite;

    [Header("Runtime References")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text saveSummaryText;

    private const string MainPanelName = "MainMenuPanel";
    private const string SettingsPanelName = "SettingsPanel";

    private void Awake()
    {
        BuildIfNeeded();
        AutoFindRefs();
        RegisterListeners();
        RefreshSaveState();
    }

#if UNITY_EDITOR
    public void RebuildForEditorPreview()
    {
        BuildIfNeeded();
        AutoFindRefs();
        RefreshSaveState();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    private void OnEnable()
    {
        RefreshSaveState();
    }

    private void OnDestroy()
    {
        if (newGameButton != null) newGameButton.onClick.RemoveListener(OnNewGameClicked);
        if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueClicked);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettingsClicked);
        if (exitButton != null) exitButton.onClick.RemoveListener(OnExitClicked);
        if (closeSettingsButton != null) closeSettingsButton.onClick.RemoveListener(CloseSettings);
    }

    private void RegisterListeners()
    {
        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveListener(OnNewGameClicked);
            newGameButton.onClick.AddListener(OnNewGameClicked);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueClicked);
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OnSettingsClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(OnExitClicked);
            exitButton.onClick.AddListener(OnExitClicked);
        }

        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.RemoveListener(CloseSettings);
            closeSettingsButton.onClick.AddListener(CloseSettings);
        }
    }

    private void OnNewGameClicked()
    {
        SaveLoadManager.DeleteAllSaveData();
        SetStatus("Đang tạo phiên chơi mới...");
        SceneManager.LoadScene(firstGameplaySceneName);
    }

    private void OnContinueClicked()
    {
        if (!SaveLoadManager.HasAnySaveData())
        {
            SetStatus("Chưa có dữ liệu lưu để tiếp tục.");
            RefreshSaveState();
            return;
        }

        SetStatus("Đang tải phiên chơi đã lưu...");
        SceneManager.LoadScene(firstGameplaySceneName);
    }

    private void OnSettingsClicked()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    private void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void OnExitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void RefreshSaveState()
    {
        bool hasSave = SaveLoadManager.HasAnySaveData();
        if (continueButton != null)
            continueButton.interactable = hasSave;

        if (saveSummaryText != null)
            saveSummaryText.text = hasSave ? BuildSaveSummary() : "Chưa có phiên chơi đã lưu.";

        if (!hasSave)
            SetStatus("Chọn New Game để bắt đầu hành trình nông trại.");
    }

    private string BuildSaveSummary()
    {
        string systemPath = Path.Combine(Application.persistentDataPath, SaveLoadManager.SystemSaveFile);
        if (!File.Exists(systemPath))
            return "Đã tìm thấy dữ liệu lưu. Nhấn Continue để tiếp tục.";

        try
        {
            string json = File.ReadAllText(systemPath);
            var data = JsonUtility.FromJson<SystemSaveData>(json);
            if (data == null)
                return "Đã tìm thấy dữ liệu lưu. Nhấn Continue để tiếp tục.";

            string sceneName = string.IsNullOrWhiteSpace(data.lastActiveSceneName)
                ? firstGameplaySceneName
                : data.lastActiveSceneName;
            string dayText = data.time.day > 0 ? $"Ngày {data.time.day}" : "Ngày 1";
            return $"Phiên đã lưu: {sceneName} - {dayText}";
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MainMenuUI] Failed to read save summary: {ex.Message}");
            return "Đã tìm thấy dữ liệu lưu. Nhấn Continue để tiếp tục.";
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void AutoFindRefs()
    {
        newGameButton ??= FindDeepChild(transform, "NewGameButton")?.GetComponent<Button>();
        continueButton ??= FindDeepChild(transform, "ContinueButton")?.GetComponent<Button>();
        settingsButton ??= FindDeepChild(transform, "SettingsButton")?.GetComponent<Button>();
        exitButton ??= FindDeepChild(transform, "ExitButton")?.GetComponent<Button>();
        closeSettingsButton ??= FindDeepChild(transform, "CloseSettingsButton")?.GetComponent<Button>();
        settingsPanel ??= FindDeepChild(transform, SettingsPanelName)?.gameObject;
        statusText ??= FindDeepChild(transform, "StatusText")?.GetComponent<TMP_Text>();
        saveSummaryText ??= FindDeepChild(transform, "SaveSummaryText")?.GetComponent<TMP_Text>();
    }

    private void BuildIfNeeded()
    {
        if (FindDeepChild(transform, MainPanelName) != null)
            return;

        var rootRect = GetComponent<RectTransform>();
        if (rootRect != null)
            Stretch(rootRect);

        var background = CreateImage("Background", transform, new Color(0.10f, 0.16f, 0.20f, 1f), null);
        Stretch(background.GetComponent<RectTransform>());

        var vignette = CreateImage("Vignette", transform, new Color(0.03f, 0.02f, 0.01f, 0.55f), null);
        Stretch(vignette.GetComponent<RectTransform>());

        var panel = CreateImage(MainPanelName, transform, new Color(0.50f, 0.31f, 0.12f, 0.96f), panelSprite);
        Anchor(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(760f, 680f));
        AddOutline(panel, new Color(0.78f, 0.54f, 0.20f, 1f), new Vector2(4f, -4f));

        var title = CreateText("TitleText", panel.transform, "NÔNG TRẠI PHIÊU LƯU", 46f, TextAlignmentOptions.Center, new Color(1f, 0.84f, 0.42f));
        Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(660f, 64f));

        var subtitle = CreateText("SubtitleText", panel.transform, "Trồng trọt - Khai thác - Nhiệm vụ - Phát triển nông trại", 20f, TextAlignmentOptions.Center, new Color(0.95f, 0.84f, 0.62f));
        Anchor(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -126f), new Vector2(650f, 34f));

        var preview = CreateImage("SavePreviewPanel", panel.transform, new Color(0.20f, 0.12f, 0.05f, 0.86f), null);
        Anchor(preview.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -202f), new Vector2(610f, 86f));
        AddOutline(preview, new Color(0.66f, 0.42f, 0.16f, 1f), new Vector2(2f, -2f));

        var saveText = CreateText("SaveSummaryText", preview.transform, "Chưa có phiên chơi đã lưu.", 22f, TextAlignmentOptions.Center, new Color(0.98f, 0.88f, 0.62f));
        Stretch(saveText.rectTransform, new Vector2(20f, 8f), new Vector2(-20f, -8f));

        float y = -315f;
        CreateMenuButton("NewGameButton", panel.transform, "New Game", new Vector2(0f, y));
        CreateMenuButton("ContinueButton", panel.transform, "Continue", new Vector2(0f, y - 74f));
        CreateMenuButton("SettingsButton", panel.transform, "Settings", new Vector2(0f, y - 148f));
        CreateMenuButton("ExitButton", panel.transform, "Exit", new Vector2(0f, y - 222f));

        var status = CreateText("StatusText", panel.transform, string.Empty, 18f, TextAlignmentOptions.Center, new Color(1f, 0.88f, 0.45f));
        Anchor(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(620f, 32f));

        BuildSettingsPanel();
    }

    private void BuildSettingsPanel()
    {
        var panel = CreateImage(SettingsPanelName, transform, new Color(0.22f, 0.13f, 0.06f, 0.96f), panelSprite);
        Anchor(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(560f, 430f));
        AddOutline(panel, new Color(0.78f, 0.54f, 0.20f, 1f), new Vector2(4f, -4f));
        panel.SetActive(false);

        var title = CreateText("SettingsTitleText", panel.transform, "CÀI ĐẶT", 34f, TextAlignmentOptions.Center, new Color(1f, 0.84f, 0.42f));
        Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(500f, 48f));

        CreateTextRow(panel.transform, "Âm lượng tổng", "80%", -128f);
        CreateTextRow(panel.transform, "Nhạc nền", "70%", -182f);
        CreateTextRow(panel.transform, "Hiệu ứng", "85%", -236f);
        CreateTextRow(panel.transform, "Ngôn ngữ", "Tiếng Việt", -290f);

        CreateMenuButton("CloseSettingsButton", panel.transform, "Đóng", new Vector2(0f, -166f), 260f, 56f);
    }

    private void CreateTextRow(Transform parent, string label, string value, float y)
    {
        var row = CreateImage($"Row_{label}", parent, new Color(0.40f, 0.24f, 0.09f, 0.78f), null);
        Anchor(row.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(450f, 42f));

        var labelText = CreateText("Label", row.transform, label, 18f, TextAlignmentOptions.MidlineLeft, new Color(0.95f, 0.84f, 0.62f));
        Stretch(labelText.rectTransform, new Vector2(18f, 0f), new Vector2(-190f, 0f));

        var valueText = CreateText("Value", row.transform, value, 18f, TextAlignmentOptions.MidlineRight, Color.white);
        Stretch(valueText.rectTransform, new Vector2(250f, 0f), new Vector2(-18f, 0f));
    }

    private Button CreateMenuButton(string name, Transform parent, string label, Vector2 position, float width = 420f, float height = 58f)
    {
        var buttonObject = CreateImage(name, parent, new Color(0.34f, 0.20f, 0.08f, 1f), buttonSprite);
        Anchor(buttonObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(width, height));
        AddOutline(buttonObject, new Color(0.78f, 0.54f, 0.20f, 1f), new Vector2(2f, -2f));

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.92f, 0.70f, 1f);
        colors.pressedColor = new Color(0.78f, 0.54f, 0.20f, 1f);
        colors.disabledColor = new Color(0.45f, 0.38f, 0.30f, 0.55f);
        button.colors = colors;

        var text = CreateText("Label", buttonObject.transform, label, 24f, TextAlignmentOptions.Center, new Color(1f, 0.90f, 0.66f));
        Stretch(text.rectTransform);
        return button;
    }

    private static GameObject CreateImage(string name, Transform parent, Color color, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
        }
        return go;
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

    private static void AddOutline(GameObject go, Color color, Vector2 distance)
    {
        var outline = go.GetComponent<Outline>() ?? go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }

    private static void Stretch(RectTransform rect)
    {
        Stretch(rect, Vector2.zero, Vector2.zero);
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static Transform FindDeepChild(Transform root, string targetName)
    {
        if (root == null) return null;
        if (root.name == targetName) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeepChild(root.GetChild(i), targetName);
            if (found != null) return found;
        }

        return null;
    }
}
