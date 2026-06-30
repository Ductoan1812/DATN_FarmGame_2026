using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Quản lý HUD/Menu/Window:
/// - TAB: mở/đóng menu.
/// - Mặc định khi mở menu: mở Backpack window và chọn toggle Backpack.
/// - Mỗi toggle menu map tới đúng window tương ứng.
/// - Mỗi thời điểm chỉ hiển thị 1 functional window.
/// - Khi có window/menu mở, ẩn Hotbar.
/// </summary>
public class UIController : MonoBehaviour
{
    [Serializable]
    public class WindowEntry
    {
        [Tooltip("ID duy nhất để gọi qua API (vd: 'backpack', 'equipment')")]
        public string id;

        [Tooltip("Toggle UI đại diện cho tab này")]
        public Toggle toggle;

        [Tooltip("GameObject sẽ được hiện/ẩn khi tab này được chọn")]
        public GameObject window;
    }

    [Header("Roots")]
    [SerializeField] private GameObject hudRoot;
    [SerializeField] private GameObject windowsRoot;
    [SerializeField] private GameObject menuWindow;
    [SerializeField] private GameObject hotbarRoot;

    [Header("Input")]
    [SerializeField] private KeyCode menuToggleKey = KeyCode.Tab;
    [SerializeField] private bool closeByEscape = true;

    [Header("Window Entries")]
    [SerializeField] private List<WindowEntry> entries = new();

    [Header("Settings")]
    [Tooltip("ID window mặc định khi mở menu")]
    [SerializeField] private string defaultWindowId = "backpack";

    [SerializeField] private bool resetToClosedOnEnable = true;
    [SerializeField] private bool hideHotbarWhenWindowOpen = true;
    [SerializeField] private string backpackWindowId = "backpack";

    [Header("Auto Setup")]
    [SerializeField] private bool autoBindMenuToggles = true;
    [SerializeField] private Transform menuToggleContainer;
    [SerializeField] private bool autoCreateTemplateWindows = true;
    [SerializeField] private Vector2 templateWindowSize = new(1380f, 720f);
    [SerializeField] private Color templateWindowColor = new(0.97f, 0.89f, 0.70f, 0.96f);

    [Header("Button Bridge (Optional)")]
    [SerializeField] private bool autoBindMenuButtons = true;
    [SerializeField] private Button openMenuButton;
    [SerializeField] private List<Button> closeMenuButtons = new();
    [SerializeField] private string openMenuButtonName = "Open";
    [SerializeField] private string closeMenuButtonName = "CloseButton";
    [SerializeField] private bool syncMenuButtonVisibility = true;

    private readonly Dictionary<string, WindowEntry> entryMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ToggleRuntimeBinding> toggleBindings = new();
    private readonly List<ButtonRuntimeBinding> buttonBindings = new();
    private readonly HashSet<string> externalExclusiveWindows = new(StringComparer.OrdinalIgnoreCase);
    private bool initialStateApplied;
    private bool runtimeInitialized;
    private bool hasMenuStateSnapshot;
    private bool menuStateSnapshot;
    private PlayerControler _cachedPlayer;

    /// <summary>Fired khi một window được mở. Truyền id của window đó.</summary>
    public event Action<string> WindowOpened;

    /// <summary>Fired khi một window được đóng. Truyền id của window đó.</summary>
    public event Action<string> WindowClosed;

    // ─── Unity Lifecycle ───────────────────────────────────────────────────

    private void Awake()
    {
        EnsureRuntimeInitialized(forceRebind: true);
        EnsureHudVisible();
        ForceCloseMenuAndWindows();
        CaptureMenuStateSnapshot();
        initialStateApplied = false;
    }

    private void LateUpdate()
    {
        if (initialStateApplied || !Application.isPlaying)
            return;

        EnsureRuntimeInitialized(forceRebind: true);

        // Đảm bảo trạng thái ban đầu luôn đóng menu/window sau khi mọi Start() khác đã chạy.
        ForceCloseMenuAndWindows();
        initialStateApplied = true;
    }

    private void Update()
    {
        if (!runtimeInitialized)
            EnsureRuntimeInitialized(forceRebind: true);

        if (Input.GetKeyDown(menuToggleKey))
            ToggleMenu();

        if (closeByEscape && Input.GetKeyDown(KeyCode.Escape) && IsMenuOpen())
            ForceCloseMenuAndWindows();

        SyncFromExternalMenuActiveState();
    }

    private void OnEnable()
    {
        EnsureHudVisible();
        EnsureRuntimeInitialized(forceRebind: true);
        if (resetToClosedOnEnable)
            ForceCloseMenuAndWindows();
    }

    private void OnDisable()
    {
        ForceCloseMenuAndWindows();
    }

    private void OnDestroy()
    {
        UnbindToggles();
        UnbindButtons();
    }

    // ─── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Hiện window có id tương ứng, ẩn các window còn lại.
    /// </summary>
    public void Open(string id)
    {
        EnsureRuntimeInitialized();

        if (!entryMap.TryGetValue(id, out _))
        {
            Debug.LogWarning($"[UIController] Không tìm thấy entry với id '{id}'.");
            return;
        }

        if (menuWindow != null && !menuWindow.activeSelf)
            menuWindow.SetActive(true);

        foreach (var entry in entries)
        {
            if (entry?.window == null) continue;

            bool shouldBeActive = string.Equals(entry.id, id, StringComparison.OrdinalIgnoreCase);
            bool wasActive = entry.window.activeSelf;

            if (wasActive != shouldBeActive)
                entry.window.SetActive(shouldBeActive);

            SyncToggle(entry, shouldBeActive);

            if (!wasActive && shouldBeActive) WindowOpened?.Invoke(entry.id);
            else if (wasActive && !shouldBeActive) WindowClosed?.Invoke(entry.id);
        }

        UpdateHotbarVisibility();
        CaptureMenuStateSnapshot();
    }

    /// <summary>
    /// Ẩn window có id tương ứng.
    /// </summary>
    public void Close(string id)
    {
        EnsureRuntimeInitialized();

        if (!entryMap.TryGetValue(id, out var target))
        {
            Debug.LogWarning($"[UIController] Không tìm thấy entry với id '{id}'.");
            return;
        }

        if (target.window == null || !target.window.activeSelf) return;

        target.window.SetActive(false);
        SyncToggle(target, false);
        WindowClosed?.Invoke(target.id);
        UpdateHotbarVisibility();
        CaptureMenuStateSnapshot();
    }

    /// <summary>
    /// Đảo trạng thái window có id tương ứng.
    /// </summary>
    public void Toggle(string id)
    {
        EnsureRuntimeInitialized();

        if (!entryMap.TryGetValue(id, out var target)) return;

        if (target.window != null && target.window.activeSelf)
            Close(id);
        else
            Open(id);
    }

    /// <summary>
    /// Trả về true nếu window đang hiển thị.
    /// </summary>
    public bool IsOpen(string id)
    {
        EnsureRuntimeInitialized();

        if (!entryMap.TryGetValue(id, out var target)) return false;
        return target.window != null && target.window.activeSelf;
    }

    /// <summary>
    /// Ẩn tất cả windows.
    /// </summary>
    public void CloseAll()
    {
        EnsureRuntimeInitialized();

        foreach (var entry in entries)
        {
            if (entry == null) continue;

            bool wasActive = entry.window != null && entry.window.activeSelf;
            if (wasActive)
                entry.window.SetActive(false);

            SyncToggle(entry, false);
            if (wasActive)
                WindowClosed?.Invoke(entry.id);
        }

        UpdateHotbarVisibility();
        CaptureMenuStateSnapshot();
    }

    public void OpenMenu()
    {
        EnsureRuntimeInitialized();

        if (menuWindow != null && !menuWindow.activeSelf)
            menuWindow.SetActive(true);

        if (!TryOpenDefaultWindow())
            UpdateHotbarVisibility();

        CaptureMenuStateSnapshot();
    }

    public void ToggleMenu()
    {
        EnsureRuntimeInitialized();

        if (IsMenuOpen()) ForceCloseMenuAndWindows();
        else OpenMenu();
    }

    public void ForceCloseMenuAndWindows()
    {
        EnsureRuntimeInitialized();

        CloseAll();

        if (menuWindow != null && menuWindow.activeSelf)
            menuWindow.SetActive(false);

        UpdateHotbarVisibility();
        CaptureMenuStateSnapshot();
    }

    /// <summary>
    /// Bridge cho button Open menu nếu scene chưa bind về UIController.
    /// </summary>
    public void OnMenuOpenButtonClicked()
    {
        OpenMenu();
    }

    /// <summary>
    /// Bridge cho button Close menu/window nếu scene chưa bind về UIController.
    /// </summary>
    public void OnMenuCloseButtonClicked()
    {
        ForceCloseMenuAndWindows();
    }

    /// <summary>
    /// Mở một external window (vd: dialogue) theo chế độ exclusive:
    /// đóng toàn bộ menu + functional windows trước khi hiển thị external window.
    /// </summary>
    public void OpenExternalExclusiveWindow(string externalWindowId)
    {
        EnsureRuntimeInitialized();

        if (string.IsNullOrWhiteSpace(externalWindowId))
            return;

        ForceCloseMenuAndWindows();
        externalExclusiveWindows.Add(externalWindowId);
        UpdateHotbarVisibility();
    }

    /// <summary>
    /// Đánh dấu external window đã đóng để khôi phục hiển thị HUD/Hotbar đúng trạng thái.
    /// </summary>
    public void CloseExternalExclusiveWindow(string externalWindowId)
    {
        EnsureRuntimeInitialized();

        if (string.IsNullOrWhiteSpace(externalWindowId))
            return;

        if (externalExclusiveWindows.Remove(externalWindowId))
            UpdateHotbarVisibility();
    }

    [ContextMenu("Setup Missing Menu Windows")]
    private void SetupMissingMenuWindows()
    {
        EnsureRuntimeInitialized(forceRebind: true);
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        if (!Application.isPlaying)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    // ─── Private ───────────────────────────────────────────────────────────

    private void BuildLookup()
    {
        entryMap.Clear();
        foreach (var entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.id)) continue;
            if (entryMap.ContainsKey(entry.id))
            {
                Debug.LogWarning($"[UIController] Duplicate entry id '{entry.id}' — bỏ qua.");
                continue;
            }
            entryMap[entry.id] = entry;
        }
    }

    private void EnsureRuntimeInitialized(bool forceRebind = false)
    {
        ResolveReferences();

        if (autoBindMenuToggles)
            EnsureMenuToggleEntries();

        BuildLookup();

        if (forceRebind || toggleBindings.Count == 0)
            BindToggles();

        if (forceRebind || buttonBindings.Count == 0)
            BindButtons();

        runtimeInitialized = true;
    }

    private void BindToggles()
    {
        UnbindToggles();

        foreach (var entry in entries)
        {
            if (entry?.toggle == null) continue;
            var capturedId = entry.id;
            UnityAction<bool> listener = isOn =>
            {
                if (isOn) Open(capturedId);
            };

            entry.toggle.onValueChanged.AddListener(listener);
            toggleBindings.Add(new ToggleRuntimeBinding(entry.toggle, listener));
        }
    }

    private void UnbindToggles()
    {
        for (int i = 0; i < toggleBindings.Count; i++)
        {
            var binding = toggleBindings[i];
            if (binding?.toggle != null && binding.listener != null)
                binding.toggle.onValueChanged.RemoveListener(binding.listener);
        }

        toggleBindings.Clear();
    }

    private void BindButtons()
    {
        UnbindButtons();

        if (!autoBindMenuButtons)
            return;

        ResolveMenuButtons();

        if (openMenuButton != null)
        {
            UnityAction openListener = OnMenuOpenButtonClicked;
            openMenuButton.onClick.AddListener(openListener);
            buttonBindings.Add(new ButtonRuntimeBinding(openMenuButton, openListener));
        }

        if (closeMenuButtons == null || closeMenuButtons.Count == 0)
        {
            UpdateMenuButtonVisibility();
            return;
        }

        for (int i = 0; i < closeMenuButtons.Count; i++)
        {
            var closeButton = closeMenuButtons[i];
            if (closeButton == null) continue;

            UnityAction closeListener = OnMenuCloseButtonClicked;
            closeButton.onClick.AddListener(closeListener);
            buttonBindings.Add(new ButtonRuntimeBinding(closeButton, closeListener));
        }

        UpdateMenuButtonVisibility();
    }

    private void UnbindButtons()
    {
        for (int i = 0; i < buttonBindings.Count; i++)
        {
            var binding = buttonBindings[i];
            if (binding?.button != null && binding.listener != null)
                binding.button.onClick.RemoveListener(binding.listener);
        }

        buttonBindings.Clear();
    }

    private void ResolveReferences()
    {
        windowsRoot ??= gameObject;

        if (menuWindow == null)
            menuWindow = FindSceneObject("UIRoot/Canvas_Windows/WindowsRoot/MenuWindow");

        if (hudRoot == null)
            hudRoot = FindSceneObject("UIRoot/Canvas_HUD/HUDRoot");

        if (hotbarRoot == null)
            hotbarRoot = FindSceneObject("UIRoot/Canvas_HUD/HUDRoot/Menu/ConTroller/Hotbar");
    }

    private void ResolveMenuButtons()
    {
        if (openMenuButton == null)
        {
            if (hudRoot != null)
                openMenuButton = FindButtonByName(hudRoot.transform, openMenuButtonName);

            if (openMenuButton == null)
                openMenuButton = FindButtonByName(transform.root, openMenuButtonName);
        }

        if (closeMenuButtons == null)
            closeMenuButtons = new List<Button>();

        closeMenuButtons.RemoveAll(button => button == null);
        if (closeMenuButtons.Count > 0)
            return;

        var collected = new List<Button>();

        if (menuWindow != null)
            CollectButtonsByName(menuWindow.transform, closeMenuButtonName, collected);

        for (int i = 0; i < entries.Count; i++)
        {
            var window = entries[i]?.window;
            if (window == null) continue;
            CollectButtonsByName(window.transform, closeMenuButtonName, collected);
        }

        for (int i = 0; i < collected.Count; i++)
        {
            var button = collected[i];
            if (button == null || closeMenuButtons.Contains(button)) continue;
            closeMenuButtons.Add(button);
        }
    }

    private void EnsureMenuToggleEntries()
    {
        var toggles = ResolveMenuToggles();
        if (toggles.Count == 0) return;

        for (int i = 0; i < toggles.Count; i++)
        {
            var toggle = toggles[i];
            if (toggle == null) continue;

            string id = ResolveIdFromToggle(toggle);
            if (string.IsNullOrWhiteSpace(id)) continue;

            if (TryGetEntryById(id, out var existing))
            {
                if (existing.toggle == null)
                    existing.toggle = toggle;

                if (existing.window == null)
                    existing.window = FindOrCreateWindowForId(id);
                continue;
            }

            entries.Add(new WindowEntry
            {
                id = id,
                toggle = toggle,
                window = FindOrCreateWindowForId(id)
            });
        }
    }

    private List<Toggle> ResolveMenuToggles()
    {
        var result = new List<Toggle>();
        Transform sourceRoot = menuToggleContainer;

        if (sourceRoot == null && menuWindow != null)
            sourceRoot = menuWindow.transform;

        if (sourceRoot == null)
            return result;

        var tabToggles = sourceRoot.GetComponentsInChildren<TabToggle>(true);
        if (tabToggles != null && tabToggles.Length > 0)
        {
            for (int i = 0; i < tabToggles.Length; i++)
            {
                var t = tabToggles[i].GetComponent<Toggle>();
                if (t != null && t.gameObject.activeSelf) result.Add(t);
            }

            result.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            return result;
        }

        var plainToggles = sourceRoot.GetComponentsInChildren<Toggle>(true);
        if (plainToggles == null) return result;

        for (int i = 0; i < plainToggles.Length; i++)
        {
            if (plainToggles[i] != null && plainToggles[i].gameObject.activeSelf)
                result.Add(plainToggles[i]);
        }

        result.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
        return result;
    }

    private string ResolveIdFromToggle(Toggle toggle)
    {
        if (toggle == null) return null;

        var tabToggle = toggle.GetComponent<TabToggle>();
        if (tabToggle != null)
            return ResolveIdFromTabType(tabToggle.TabType);

        string name = toggle.name.ToLowerInvariant();
        if (name.Contains("backpack") || name.Contains("inventory")) return "backpack";
        if (name.Contains("equipment") || name.Contains("info")) return "equipment";
        if (name.Contains("skill")) return "skills";
        if (name.Contains("quest")) return "quest";
        if (name.Contains("map")) return "map";
        if (name.Contains("setting")) return "settings";
        if (name.Contains("shop")) return "shop";

        return null;
    }

    private static string ResolveIdFromTabType(TabType tabType)
    {
        return tabType switch
        {
            TabType.Inventory => "backpack",
            TabType.InfoPlayer => "equipment",
            TabType.Skills => "skills",
            TabType.Quest => "quest",
            TabType.Map => "map",
            TabType.Settings => "settings",
            TabType.Shop => "shop",
            _ => null
        };
    }

    private bool TryOpenDefaultWindow()
    {
        if (!string.IsNullOrWhiteSpace(defaultWindowId) && entryMap.ContainsKey(defaultWindowId))
        {
            Open(defaultWindowId);
            return true;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.id)) continue;
            if (entry.window == null) continue;

            Open(entry.id);
            return true;
        }

        return false;
    }

    private void EnsureHudVisible()
    {
        if (hudRoot != null && !hudRoot.activeSelf)
            hudRoot.SetActive(true);
    }

    private void UpdateHotbarVisibility()
    {
        UpdateMenuButtonVisibility();

        bool hasExternalWindowOpen = externalExclusiveWindows.Count > 0;
        bool isBackpackOpen = IsEntryWindowOpen(backpackWindowId);
        bool anyFunctionalWindowOpen = IsAnyFunctionalWindowOpen();
        bool menuOpen = IsMenuOpen();

        // Block player input khi có bất kỳ UI nào đang mở
        bool isUIBlocking = menuOpen || anyFunctionalWindowOpen || hasExternalWindowOpen;
        UpdatePlayerInput(!isUIBlocking);

        if (hotbarRoot == null)
            return;

        bool showHotbar;
        if (!hideHotbarWhenWindowOpen)
        {
            showHotbar = !hasExternalWindowOpen;
        }
        else
        {
            bool gameplayState = !menuOpen && !anyFunctionalWindowOpen && !hasExternalWindowOpen;
            bool backpackState = isBackpackOpen && !hasExternalWindowOpen;
            showHotbar = gameplayState || backpackState;
        }

        if (hotbarRoot.activeSelf != showHotbar)
            hotbarRoot.SetActive(showHotbar);
    }

    private void UpdatePlayerInput(bool enabled)
    {
        if (_cachedPlayer == null)
            _cachedPlayer = FindAnyObjectByType<PlayerControler>();

        if (_cachedPlayer != null)
            _cachedPlayer.InputEnabled = enabled;
    }

    private void UpdateMenuButtonVisibility()
    {
        if (!syncMenuButtonVisibility)
            return;

        bool menuOpen = IsMenuOpen();

        if (openMenuButton != null && openMenuButton.gameObject.activeSelf == menuOpen)
            openMenuButton.gameObject.SetActive(!menuOpen);

        if (closeMenuButtons == null)
            return;

        for (int i = 0; i < closeMenuButtons.Count; i++)
        {
            var closeButton = closeMenuButtons[i];
            if (closeButton == null) continue;

            var closeObject = closeButton.gameObject;
            if (closeObject.activeSelf != menuOpen)
                closeObject.SetActive(menuOpen);
        }
    }

    private bool IsMenuOpen()
    {
        return menuWindow != null && menuWindow.activeSelf;
    }

    private bool IsAnyFunctionalWindowOpen()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry?.window == null || !entry.window.activeSelf) continue;
            return true;
        }

        return false;
    }

    private bool IsEntryWindowOpen(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        if (!entryMap.TryGetValue(id, out var entry))
            return false;

        return entry?.window != null && entry.window.activeSelf;
    }

    private bool TryGetEntryById(string id, out WindowEntry entry)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            var item = entries[i];
            if (item == null || string.IsNullOrWhiteSpace(item.id)) continue;
            if (!string.Equals(item.id, id, StringComparison.OrdinalIgnoreCase)) continue;
            entry = item;
            return true;
        }

        entry = null;
        return false;
    }

    private GameObject FindOrCreateWindowForId(string id)
    {
        string windowName = ToWindowName(id);

        if (windowsRoot != null)
        {
            var child = windowsRoot.transform.Find(windowName);
            if (child != null) return child.gameObject;
        }

        var byName = GameObject.Find(windowName);
        if (byName != null) return byName;

        if (!autoCreateTemplateWindows || windowsRoot == null)
            return null;

        return CreateTemplateWindow(windowName, id);
    }

    private GameObject CreateTemplateWindow(string windowName, string id)
    {
        var go = new GameObject(windowName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        go.transform.SetParent(windowsRoot.transform, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = templateWindowSize;

        var image = go.GetComponent<Image>();
        image.color = templateWindowColor;
        image.raycastTarget = true;

        var outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(0.43f, 0.26f, 0.10f, 1f);
        outline.effectDistance = new Vector2(3f, -3f);
        outline.useGraphicAlpha = true;

        var labelGo = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);

        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 1f);
        labelRect.anchorMax = new Vector2(0.5f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -28f);
        labelRect.sizeDelta = new Vector2(templateWindowSize.x - 80f, 56f);

        var label = labelGo.GetComponent<TextMeshProUGUI>();
        label.text = $"{ToDisplayName(id)} (Template)";
        label.fontSize = 38f;
        label.color = new Color(0.2f, 0.12f, 0.05f, 1f);
        label.alignment = TextAlignmentOptions.Center;

        go.SetActive(false);
        return go;
    }

    private static string ToWindowName(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return "Window";

        return id.ToLowerInvariant() switch
        {
            "backpack" => "BackpackWindow",
            "equipment" => "EquipmentWindow",
            "quest" => "QuestWindow",
            "map" => "MapWindow",
            "skills" => "SkillsWindow",
            "settings" => "SettingsWindow",
            "shop" => "ShopWindow",
            _ => $"{char.ToUpperInvariant(id[0])}{id.Substring(1)}Window"
        };
    }

    private static string ToDisplayName(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return "Window";
        return $"{char.ToUpperInvariant(id[0])}{id.Substring(1).ToLowerInvariant()}";
    }

    private static GameObject FindSceneObject(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        var segments = path.Split('/');
        if (segments.Length == 0) return null;

        var root = GameObject.Find(segments[0]);
        if (root == null) return null;

        Transform current = root.transform;
        for (int i = 1; i < segments.Length; i++)
        {
            current = current.Find(segments[i]);
            if (current == null) return null;
        }

        return current.gameObject;
    }

    private static Button FindButtonByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
            return null;

        var buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            if (button == null) continue;
            if (!string.Equals(button.name, targetName, StringComparison.OrdinalIgnoreCase)) continue;
            return button;
        }

        return null;
    }

    private static void CollectButtonsByName(Transform root, string targetName, List<Button> result)
    {
        if (root == null || result == null || string.IsNullOrWhiteSpace(targetName))
            return;

        var buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            if (button == null) continue;
            if (!string.Equals(button.name, targetName, StringComparison.OrdinalIgnoreCase)) continue;
            result.Add(button);
        }
    }

    private void SyncFromExternalMenuActiveState()
    {
        if (menuWindow == null)
        {
            hasMenuStateSnapshot = false;
            return;
        }

        bool currentState = menuWindow.activeSelf;
        if (!hasMenuStateSnapshot)
        {
            hasMenuStateSnapshot = true;
            menuStateSnapshot = currentState;
            return;
        }

        if (menuStateSnapshot == currentState)
            return;

        menuStateSnapshot = currentState;

        if (currentState)
        {
            if (!TryOpenDefaultWindow())
                UpdateHotbarVisibility();
        }
        else
        {
            CloseAll();
            UpdateHotbarVisibility();
        }
    }

    private void CaptureMenuStateSnapshot()
    {
        if (menuWindow == null)
        {
            hasMenuStateSnapshot = false;
            return;
        }

        hasMenuStateSnapshot = true;
        menuStateSnapshot = menuWindow.activeSelf;
    }

    /// <summary>
    /// Sync toggle state mà không trigger lại onValueChanged.
    /// </summary>
    private static void SyncToggle(WindowEntry entry, bool isOn)
    {
        var t = entry.toggle;
        if (t == null || t.isOn == isOn) return;

        var tabToggle = t.GetComponent<TabToggle>();
        if (tabToggle != null)
            tabToggle.SetTabState(isOn, false);
        else
            t.SetIsOnWithoutNotify(isOn);
    }

    private sealed class ToggleRuntimeBinding
    {
        public readonly Toggle toggle;
        public readonly UnityAction<bool> listener;

        public ToggleRuntimeBinding(Toggle toggle, UnityAction<bool> listener)
        {
            this.toggle = toggle;
            this.listener = listener;
        }
    }

    private sealed class ButtonRuntimeBinding
    {
        public readonly Button button;
        public readonly UnityAction listener;

        public ButtonRuntimeBinding(Button button, UnityAction listener)
        {
            this.button = button;
            this.listener = listener;
        }
    }
}
