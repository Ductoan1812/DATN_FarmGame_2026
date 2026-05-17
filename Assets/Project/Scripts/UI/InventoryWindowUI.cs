using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý tab system bên trong MenuWindow.
/// 
/// Kết hợp với UIController: khi tab được chọn, UIController.Open(id) sẽ
/// được gọi để hiện panel tương ứng.
/// 
/// Cách sử dụng:
/// - Đặt component này trên MenuWindow (hoặc object cha chứa TabToggle).
/// - Assign UIController reference (component trên cùng object hoặc drag trong Inspector).
/// - Kéo thả các TabToggle vào tabToggles theo thứ tự.
/// - Đặt tabToggles rỗng để auto-bind từ children.
/// </summary>
public class InventoryWindowUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIController uiController;
    [SerializeField] private GameObject menuWindowRef;

    [Header("Tab Toggles")]
    [Tooltip("Để trống để auto-bind từ children")]
    [SerializeField] private TabToggle[] tabToggles;

    [Header("Legacy Mapping (Optional)")]
    [SerializeField] private List<TabEntryMapping> tabEntryMappings = new();

    private readonly Dictionary<TabType, TabToggle> tabToggleMap = new();
    private TabType currentActiveTab;
    private bool tabsInitialized;

    // ─── Unity Lifecycle ────────────────────────────────────────────────

    protected virtual void Awake()
    {
        ResolveUIControllerReference();
    }

    protected virtual void Start()
    {
        ResolveUIControllerReference();
        AutoBindFromChildren();
        BuildTabMaps();
        // Mặc định chọn tab Inventory khi MenuWindow mở
        SetActiveTabSilent(TabType.Inventory);
    }

    protected virtual void OnEnable()
    {
        // Mỗi khi MenuWindow được bật, reset về tab mặc định
        if (tabsInitialized)
            SetActiveTab(TabType.Inventory);
    }

    protected virtual void OnDestroy()
    {
        UnsubscribeTabEvents();
    }

    // ─── Public API ─────────────────────────────────────────────────────

    /// <summary>
    /// Chọn tab và mở window tương ứng qua UIController.
    /// </summary>
    public void SetActiveTab(TabType tabType)
    {
        if (!tabsInitialized) BuildTabMaps();
        ApplyTabSwitch(tabType);
    }

    // ─── Private: Tab Switch ─────────────────────────────────────────────

    private void ApplyTabSwitch(TabType newTab)
    {
        var prevTab = currentActiveTab;
        currentActiveTab = newTab;

        // Sync toggle visuals
        SyncToggles(newTab, prevTab);

        // Thông báo UIController mở window tương ứng
        NotifyUIController(newTab);
    }

    private void SetActiveTabSilent(TabType tabType)
    {
        currentActiveTab = tabType;

        // Sync toggles không fire event
        foreach (var pair in tabToggleMap)
            pair.Value?.SetTabState(pair.Key == tabType, false);
    }

    private void SyncToggles(TabType activeTab, TabType prevTab)
    {
        if (tabToggleMap.TryGetValue(activeTab, out var newToggle) && newToggle != null && !newToggle.IsOn)
            newToggle.SetTabState(true, false);

        if (prevTab != activeTab && tabToggleMap.TryGetValue(prevTab, out var oldToggle) && oldToggle != null && oldToggle.IsOn)
            oldToggle.SetTabState(false, false);
    }

    /// <summary>
    /// Lấy id window tương ứng với TabType rồi gọi UIController.Open(id).
    /// Quy tắc mapping: dùng tên tab viết thường.
    /// Ví dụ: Inventory → "backpack", InfoPlayer → "equipment".
    /// </summary>
    private void NotifyUIController(TabType tabType)
    {
        if (uiController == null) return;

        var id = GetWindowIdForTab(tabType);
        if (!string.IsNullOrWhiteSpace(id))
            uiController.Open(id);
    }

    /// <summary>
    /// Mapping từ TabType → window id trong UIController.
    /// Override method này trong subclass nếu cần thay đổi mapping.
    /// </summary>
    protected virtual string GetWindowIdForTab(TabType tabType)
    {
        for (int i = 0; i < tabEntryMappings.Count; i++)
        {
            var mapping = tabEntryMappings[i];
            if (mapping == null) continue;
            if (mapping.tabType != tabType) continue;
            if (!string.IsNullOrWhiteSpace(mapping.rootEntryId))
                return mapping.rootEntryId;
        }

        return tabType switch
        {
            TabType.Inventory  => "backpack",
            TabType.InfoPlayer => "equipment",
            TabType.Quest      => "quest",
            TabType.Shop       => "shop",
            TabType.Skills     => "skills",
            TabType.Map        => "map",
            TabType.Settings   => "settings",
            _                  => null
        };
    }

    private void ResolveUIControllerReference()
    {
        if (uiController != null) return;

        uiController = GetComponent<UIController>();
        if (uiController != null) return;

        uiController = GetComponentInParent<UIController>(true);
        if (uiController != null) return;

        if (menuWindowRef != null)
        {
            uiController = menuWindowRef.GetComponent<UIController>();
            if (uiController == null)
                uiController = menuWindowRef.GetComponentInParent<UIController>(true);
            if (uiController != null) return;
        }

        uiController = FindAnyObjectByType<UIController>(FindObjectsInactive.Include);
    }

    // ─── Private: Initialization ─────────────────────────────────────────

    private void AutoBindFromChildren()
    {
        if (tabToggles != null && tabToggles.Length > 0) return;

        var found = new List<TabToggle>();
        CollectTabToggles(transform, found);
        found.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
        tabToggles = found.ToArray();
    }

    private static void CollectTabToggles(Transform root, List<TabToggle> result)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            var tt = child.GetComponent<TabToggle>();
            if (tt != null)
            {
                result.Add(tt);
                continue; // không đi sâu vào con của TabToggle
            }
            // Tránh đi vào object có InventoryWindowUI riêng
            if (child.GetComponent<InventoryWindowUI>() == null)
                CollectTabToggles(child, result);
        }
    }

    private void BuildTabMaps()
    {
        if (tabsInitialized) return;

        tabToggleMap.Clear();

        foreach (var toggle in tabToggles)
        {
            if (toggle == null) continue;
            tabToggleMap[toggle.TabType] = toggle;
        }

        SubscribeTabEvents();
        tabsInitialized = true;
    }

    private void SubscribeTabEvents()
    {
        if (tabToggles == null) return;
        foreach (var toggle in tabToggles)
        {
            if (toggle == null) continue;
            toggle.OnTabSelected += OnTabSelected;
        }
    }

    private void UnsubscribeTabEvents()
    {
        if (tabToggles == null) return;
        foreach (var toggle in tabToggles)
        {
            if (toggle == null) continue;
            toggle.OnTabSelected -= OnTabSelected;
        }
    }

    // ─── Tab Event Handler ───────────────────────────────────────────────

    private void OnTabSelected(TabType tabType)
    {
        if (!tabsInitialized) BuildTabMaps();
        ApplyTabSwitch(tabType);
    }
}

// ─── Supporting Types ────────────────────────────────────────────────────

[System.Serializable]
public class TabEntryMapping
{
    public TabType tabType;
    public string rootEntryId;
}
