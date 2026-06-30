using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{

    [Header("Scene")]
    [SerializeField] private string firstGameplaySceneName = "FarmScene";

    [Header("Save Slots")]
    [SerializeField] private int saveSlotCount = SaveLoadManager.DefaultSlotCount;
    [SerializeField] private SaveSlotButtonUI saveSlotPrefab;

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject saveSlotPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Transform saveSlotContent;
    [SerializeField] private TMP_Text saveSlotTitleText;

    [Header("Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    [Header("Logo Animation")]
    [SerializeField] private RectTransform logoGame;
    [SerializeField] private float logoMainY = 270f;
    [SerializeField] private float logoPanelY = 420f;
    [SerializeField] private float logoMoveDuration = 0.25f;

    [Header("Loading")]
    [SerializeField] private LoadingScreenView loadingScreenPrefab;

    private Coroutine logoMoveRoutine;
    private LoadingScreenView loadingScreenView;
    private bool isLoadingScene;

    private void Awake()
    {
        AutoFindRefs();
        EnsureSettingsPanelComponent();
        ApplyLocalizedButtonLabels();
        RegisterListeners();
        ShowMainPanel(immediateLogo: true);
        RefreshMainButtons();
    }

#if UNITY_EDITOR
    public void RebuildForEditorPreview()
    {
        AutoFindRefs();
        EnsureSettingsPanelComponent();
        ShowMainPanel(immediateLogo: true);
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    private void Update()
    {
        if (isLoadingScene)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            ShowMainPanel();

        if (mainMenuPanel != null
            && !mainMenuPanel.activeSelf
            && !IsPanelOpen(saveSlotPanel)
            && !IsPanelOpen(settingsPanel))
        {
            ShowMainPanel();
        }
    }

    private void OnDestroy()
    {
        if (newGameButton != null) newGameButton.onClick.RemoveListener(OnNewGameClicked);
        if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueClicked);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettingsClicked);
        if (exitButton != null) exitButton.onClick.RemoveListener(OnExitClicked);
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
    }

    private void OnNewGameClicked()
    {
        // Tự động chọn slot trống đầu tiên — người chơi không cần chọn
        int freeSlot = FindFreeSlotIndex();
        SaveLoadManager.SelectSlot(freeSlot);
        SaveLoadManager.DeleteAllSaveData(freeSlot); // Đảm bảo slot sạch
        StartCoroutine(LoadGameplayScene(firstGameplaySceneName));
    }

    private void OnContinueClicked()
    {
        // Chỉ hiển thị các bản save đã có
        ShowContinuePanel();
    }

    private void OnSettingsClicked()
    {
        if (settingsPanel == null)
            return;

        mainMenuPanel?.SetActive(false);
        saveSlotPanel?.SetActive(false);
        settingsPanel.SetActive(true);
        MoveLogo(logoPanelY, immediate: false);
    }

    private void OnExitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ShowContinuePanel()
    {
        PopulateSaveSlots();

        mainMenuPanel?.SetActive(false);
        settingsPanel?.SetActive(false);
        saveSlotPanel?.SetActive(true);
        MoveLogo(logoPanelY, immediate: false);
    }

    private void ShowMainPanel(bool immediateLogo = false)
    {
        saveSlotPanel?.SetActive(false);
        settingsPanel?.SetActive(false);
        mainMenuPanel?.SetActive(true);
        MoveLogo(logoMainY, immediateLogo);
        RefreshMainButtons();
    }

    private void PopulateSaveSlots()
    {
        if (saveSlotContent == null)
            return;

        for (int i = saveSlotContent.childCount - 1; i >= 0; i--)
            Destroy(saveSlotContent.GetChild(i).gameObject);

        if (saveSlotTitleText != null)
            SetLocalizedText(saveSlotTitleText, LocalizationKeys.UiMainMenuSavesTitle);

        var summaries = SaveLoadManager.GetSaveSlots(saveSlotCount);
        bool foundAny = false;

        foreach (var summary in summaries)
        {
            // Continue chỉ hiển thị slot đã có save
            if (!summary.hasSave)
                continue;

            foundAny = true;
            var slot = CreateSaveSlot(saveSlotContent);
            slot.Bind(summary, interactable: true, OnSaveSlotClicked);
        }

        if (!foundAny && saveSlotTitleText != null)
            SetLocalizedText(saveSlotTitleText, LocalizationKeys.UiMainMenuSavesEmpty);
    }

    private SaveSlotButtonUI CreateSaveSlot(Transform parent)
    {
        if (saveSlotPrefab != null)
            return Instantiate(saveSlotPrefab, parent);

        var root = new GameObject("SlotSave", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(VerticalLayoutGroup));
        root.transform.SetParent(parent, false);
        var rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(550f, 80f);

        var image = root.GetComponent<Image>();
        image.color = new Color(0.78f, 0.53f, 0.24f, 0.92f);

        var layout = root.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.padding = new RectOffset(22, 22, 8, 8);

        CreateSlotText("TitleText", root.transform, 24f, FontStyles.Bold);
        CreateSlotText("DetailText", root.transform, 16f, FontStyles.Normal);
        return root.AddComponent<SaveSlotButtonUI>();
    }

    private void OnSaveSlotClicked(int slotIndex)
    {
        // Continue: load bản save đã chọn
        var summary = SaveLoadManager.BuildSlotSummary(slotIndex);
        if (!summary.hasSave)
            return;

        SaveLoadManager.SelectSlot(slotIndex);

        string targetScene = string.IsNullOrWhiteSpace(summary.lastActiveSceneName)
            ? firstGameplaySceneName
            : summary.lastActiveSceneName;
        StartCoroutine(LoadGameplayScene(targetScene));
    }

    /// <summary>Tìm slot trống đầu tiên. Nếu tất cả đều có save thì dùng slot 1.</summary>
    private int FindFreeSlotIndex()
    {
        var summaries = SaveLoadManager.GetSaveSlots(saveSlotCount);
        foreach (var summary in summaries)
        {
            if (!summary.hasSave)
                return summary.slotIndex;
        }

        // Không có slot trống → dùng slot 1 (ghi đè bản save cũ nhất)
        return 1;
    }

    private IEnumerator LoadGameplayScene(string sceneName)
    {
        if (isLoadingScene)
            yield break;

        isLoadingScene = true;
        ShowLoadingOverlay(sceneName, 0f);

        var op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            isLoadingScene = false;
            loadingScreenView?.HideImmediate();
            yield break;
        }

        op.allowSceneActivation = false;
        while (op.progress < 0.9f)
        {
            ShowLoadingOverlay(sceneName, Mathf.Clamp01(op.progress / 0.9f));
            yield return null;
        }

        ShowLoadingOverlay(sceneName, 1f);
        yield return new WaitForSecondsRealtime(0.12f);
        op.allowSceneActivation = true;
    }

    private void RefreshMainButtons()
    {
        if (continueButton == null)
            return;

        bool hasAnySave = false;
        var summaries = SaveLoadManager.GetSaveSlots(saveSlotCount);
        foreach (var summary in summaries)
        {
            if (!summary.hasSave) continue;
            hasAnySave = true;
            break;
        }

        continueButton.interactable = hasAnySave;
    }

    private void ShowLoadingOverlay(string sceneName, float progress)
    {
        var view = GetOrCreateLoadingScreenView();
        if (view == null)
            return;

        view.Show(sceneName);
        view.SetProgress(progress);
    }

    private LoadingScreenView GetOrCreateLoadingScreenView()
    {
        if (loadingScreenView != null)
            return loadingScreenView;

        loadingScreenView = GetComponentInChildren<LoadingScreenView>(includeInactive: true);
        if (loadingScreenView == null)
            loadingScreenView = LoadingScreenView.InstantiateForParent(transform, loadingScreenPrefab);

        return loadingScreenView;
    }

    private void MoveLogo(float targetY, bool immediate)
    {
        if (logoGame == null)
            return;

        if (logoMoveRoutine != null)
            StopCoroutine(logoMoveRoutine);

        if (immediate || logoMoveDuration <= 0f)
        {
            var pos = logoGame.anchoredPosition;
            pos.y = targetY;
            logoGame.anchoredPosition = pos;
            return;
        }

        logoMoveRoutine = StartCoroutine(MoveLogoRoutine(targetY));
    }

    private IEnumerator MoveLogoRoutine(float targetY)
    {
        Vector2 start = logoGame.anchoredPosition;
        Vector2 end = new Vector2(start.x, targetY);
        float elapsed = 0f;

        while (elapsed < logoMoveDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / logoMoveDuration));
            logoGame.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }

        logoGame.anchoredPosition = end;
        logoMoveRoutine = null;
    }

    private void AutoFindRefs()
    {
        mainMenuPanel ??= FindDeepChild(transform, "MainMenuPanel")?.gameObject;
        saveSlotPanel ??= FindDeepChild(transform, "ContinuePanel")?.gameObject;
        settingsPanel ??= FindDeepChild(transform, "SettingsPanel")?.gameObject
            ?? FindDeepChild(transform, "SettingsWindow")?.gameObject
            ?? FindDeepChild(transform, "Setting Panel")?.gameObject;

        logoGame ??= FindDeepChild(transform, "LogoGame")?.GetComponent<RectTransform>();
        newGameButton ??= FindDeepChild(transform, "NewGameButton")?.GetComponent<Button>();
        continueButton ??= FindDeepChild(transform, "ContinueButton")?.GetComponent<Button>();
        settingsButton ??= FindDeepChild(transform, "SettingsButton")?.GetComponent<Button>();
        exitButton ??= FindDeepChild(transform, "ExitButton")?.GetComponent<Button>();

        if (saveSlotPanel != null && saveSlotContent == null)
            saveSlotContent = FindDeepChild(saveSlotPanel.transform, "Content");

        if (saveSlotPanel != null && saveSlotTitleText == null)
            saveSlotTitleText = FindDeepChild(saveSlotPanel.transform, "TitleName")?.GetComponent<TMP_Text>();
    }

    private void EnsureSettingsPanelComponent()
    {
        if (settingsPanel == null)
            return;

        var settings = settingsPanel.GetComponent<SettingsWindowUI>();
        if (settings == null)
            settings = settingsPanel.AddComponent<SettingsWindowUI>();

        settings.SetCompactMainMenuMode(true);
    }

    private static bool IsPanelOpen(GameObject panel)
    {
        return panel != null && panel.activeSelf;
    }

    private static TMP_Text CreateSlotText(string name, Transform parent, float size, FontStyles style)
    {
        var text = CreateText(name, parent, string.Empty, size, style, TextAlignmentOptions.Left);
        text.enableWordWrapping = false;
        return text;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float size, FontStyles style, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = new Color(1f, 0.90f, 0.66f, 1f);
        return text;
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

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void ApplyLocalizedButtonLabels()
    {
        SetButtonLocalized(newGameButton, LocalizationKeys.UiMainMenuNewGame);
        SetButtonLocalized(continueButton, LocalizationKeys.UiMainMenuContinue);
        SetButtonLocalized(settingsButton, LocalizationKeys.UiMainMenuSettings);
        SetButtonLocalized(exitButton, LocalizationKeys.UiMainMenuExit);
    }

    private static void SetButtonLocalized(Button button, string key)
    {
        if (button == null) return;
        var text = button.GetComponentInChildren<TMP_Text>(true);
        if (text == null) return;
        SetLocalizedText(text, key);
    }

    private static void SetLocalizedText(TMP_Text text, string key)
    {
        if (text == null || string.IsNullOrEmpty(key)) return;
        var localized = text.GetComponent<LocalizedText>() ?? text.gameObject.AddComponent<LocalizedText>();
        localized.SetKey(key);
    }
}
