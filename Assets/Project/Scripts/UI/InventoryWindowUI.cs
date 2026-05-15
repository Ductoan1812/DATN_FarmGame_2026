using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class InventoryWindowUI : MonoBehaviour
{
    [Header("Tab Management")]
    [SerializeField] private TabToggle[] tabToggles;
    [SerializeField] private GameObject[] tabPanels;
    [Header("Root Entry Mapping")]
    [SerializeField] private string menuEntryId = "menu";
    [SerializeField] private string backpackEntryId = "backpack";
    [SerializeField] private string equipmentEntryId = "equipment";
    [SerializeField] private string menuWindowName = "MenuWindow";

    private readonly Dictionary<TabType, GameObject> tabPanelMap = new();
    private readonly Dictionary<TabType, TabToggle> tabToggleMap = new();
    private TabType currentActiveTab;
    private bool tabsInitialized;
    private bool rootSubscribed;

    protected virtual void OnEnable()
    {
        TrySubscribeRootController();
    }

    protected virtual void Start()
    {
        InitializeTabSystem();
        TrySubscribeRootController();
        ForceSetInventoryTab();
    }

    protected virtual void OnDestroy()
    {
        UnsubscribeRootController();
        UnsubscribeFromTabEvents();
    }

    public void SetActiveTab(TabType tabType, bool triggerEvent = true)
    {
        if (!tabsInitialized)
            InitializeTabSystem();

        if (currentActiveTab == tabType && tabPanelMap.ContainsKey(tabType))
        {
            if (tabPanelMap[tabType].activeSelf) return;
        }

        bool usedMappedPanel = false;
        HideAllTabPanels();

        if (tabPanelMap.TryGetValue(tabType, out var panel) && panel != null)
        {
            panel.SetActive(true);
            usedMappedPanel = true;
        }

        if (tabToggleMap.TryGetValue(tabType, out var targetToggle) && targetToggle != null && !targetToggle.IsOn)
            targetToggle.SetTabState(true, triggerEvent);

        if (tabToggleMap.TryGetValue(currentActiveTab, out var currentToggle) && currentToggle != null && currentToggle.IsOn)
            currentToggle.SetTabState(false, false);

        currentActiveTab = tabType;

        if (triggerEvent)
        {
            if (!usedMappedPanel)
                ApplyRootEntryForTab(tabType);

            UIRootController.Instance?.NotifyWindowStateChanged();
        }
    }

    private void ForceSetInventoryTab()
    {
        SetActiveTab(TabType.Inventory, false);
        if (tabToggleMap.TryGetValue(TabType.Inventory, out var toggle) && toggle != null)
            toggle.SetTabState(true, false);
    }

    private void TrySubscribeRootController()
    {
        if (rootSubscribed) return;
        if (UIRootController.Instance == null) return;

        UIRootController.Instance.EntryOpened += OnEntryOpened;
        rootSubscribed = true;
    }

    private void UnsubscribeRootController()
    {
        if (!rootSubscribed || UIRootController.Instance == null) return;

        UIRootController.Instance.EntryOpened -= OnEntryOpened;
        rootSubscribed = false;
    }

    private void OnEntryOpened(string id)
    {
        if (string.Equals(id, menuEntryId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(id, backpackEntryId, StringComparison.OrdinalIgnoreCase))
            ForceSetInventoryTab();

        if (string.Equals(id, equipmentEntryId, StringComparison.OrdinalIgnoreCase))
            SyncTabVisualOnly(TabType.InfoPlayer);
    }

    private void InitializeTabSystem()
    {
        if (tabsInitialized) return;

        tabPanelMap.Clear();
        tabToggleMap.Clear();

        AutoBindTabsIfNeeded();

        if (tabToggles != null)
        {
            for (int i = 0; i < tabToggles.Length; i++)
            {
                var toggle = tabToggles[i];
                if (toggle == null) continue;

                tabToggleMap[toggle.TabType] = toggle;

                GameObject panel = ResolvePanelForToggle(i, toggle.TabType);
                if (panel != null)
                    tabPanelMap[toggle.TabType] = panel;

                if (tabPanels != null && i < tabPanels.Length && tabPanels[i] == null)
                    tabPanels[i] = panel;
            }
        }

        SubscribeToTabEvents();
        tabsInitialized = true;
    }

    private void SubscribeToTabEvents()
    {
        if (tabToggles == null) return;
        foreach (var toggle in tabToggles)
        {
            if (toggle == null) continue;
            toggle.OnTabSelected += OnTabSelected;
            toggle.OnTabDeselected += OnTabDeselected;
        }
    }

    private void UnsubscribeFromTabEvents()
    {
        if (tabToggles == null) return;
        foreach (var toggle in tabToggles)
        {
            if (toggle == null) continue;
            toggle.OnTabSelected -= OnTabSelected;
            toggle.OnTabDeselected -= OnTabDeselected;
        }
    }

    private void OnTabSelected(TabType tabType)
    {
        SetActiveTab(tabType, true);
    }

    private void OnTabDeselected(TabType tabType) { }

    private void HideAllTabPanels()
    {
        foreach (var panel in tabPanelMap.Values.Distinct())
        {
            if (panel != null)
                panel.SetActive(false);
        }
    }

    private void AutoBindTabsIfNeeded()
    {
        bool needsToggleBinding = tabToggles == null || tabToggles.Length == 0;
        if (needsToggleBinding)
        {
            tabToggles = GetComponentsInChildren<TabToggle>(true)
                .Where(t => t != null)
                .OrderBy(t => t.transform.GetSiblingIndex())
                .ToArray();

            if (tabToggles.Length == 0)
            {
                var menuWindow = ResolveMenuWindow();
                if (menuWindow != null)
                {
                    tabToggles = menuWindow.GetComponentsInChildren<TabToggle>(true)
                        .Where(t => t != null)
                        .OrderBy(t => t.transform.GetSiblingIndex())
                        .ToArray();
                }
            }
        }

        if (tabToggles == null)
            tabToggles = Array.Empty<TabToggle>();

        if (tabPanels == null || tabPanels.Length != tabToggles.Length)
            tabPanels = new GameObject[tabToggles.Length];
    }

    private GameObject ResolveMenuWindow()
    {
        var root = UIRootController.Instance;
        if (root != null &&
            !string.IsNullOrWhiteSpace(menuEntryId) &&
            root.TryGetEntry(menuEntryId, out var entry) &&
            entry?.Target != null)
        {
            return entry.Target;
        }

        if (!string.IsNullOrWhiteSpace(menuWindowName))
        {
            var byName = GameObject.Find(menuWindowName);
            if (byName != null)
                return byName;
        }

        return null;
    }

    private GameObject ResolvePanelForToggle(int index, TabType tabType)
    {
        if (tabPanels != null && index >= 0 && index < tabPanels.Length && tabPanels[index] != null)
            return tabPanels[index];

        return tabType switch
        {
            TabType.Inventory => FindPanelByNames("BackpackWindow", "InventoryWindow", "BackpackPanel"),
            TabType.InfoPlayer => FindPanelByNames("EquipmentWindow", "EquipmentPanel"),
            TabType.Quest => FindPanelByNames("QuestPanel", "QuestWindow"),
            _ => null
        };
    }

    private GameObject FindPanelByNames(params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            var name = names[i];
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var child = transform.Find(name);
            if (child != null)
                return child.gameObject;

            var menuWindow = ResolveMenuWindow();
            if (menuWindow != null)
            {
                var nested = FindDeepChild(menuWindow.transform, name);
                if (nested != null)
                    return nested.gameObject;
            }

            var global = GameObject.Find(name);
            if (global != null)
                return global;
        }

        return null;
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null || string.IsNullOrWhiteSpace(name))
            return null;

        if (string.Equals(root.name, name, StringComparison.Ordinal))
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeepChild(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    private void ApplyRootEntryForTab(TabType tabType)
    {
        var root = UIRootController.Instance;
        if (root == null)
            return;

        if (tabType == TabType.Inventory)
        {
            if (!string.IsNullOrWhiteSpace(menuEntryId) && root.TryGetEntry(menuEntryId, out _))
                root.Open(menuEntryId);
            else if (!string.IsNullOrWhiteSpace(backpackEntryId) && root.TryGetEntry(backpackEntryId, out _))
                root.Open(backpackEntryId);

            if (!string.IsNullOrWhiteSpace(equipmentEntryId) && root.TryGetEntry(equipmentEntryId, out _))
                root.Close(equipmentEntryId);
            return;
        }

        if (tabType == TabType.InfoPlayer)
        {
            if (!string.IsNullOrWhiteSpace(menuEntryId) && root.TryGetEntry(menuEntryId, out _))
                root.Open(menuEntryId);

            if (!string.IsNullOrWhiteSpace(equipmentEntryId) && root.TryGetEntry(equipmentEntryId, out _))
                root.Open(equipmentEntryId);
        }
    }

    private void SyncTabVisualOnly(TabType activeTab)
    {
        if (!tabsInitialized)
            InitializeTabSystem();

        HideAllTabPanels();
        if (tabPanelMap.TryGetValue(activeTab, out var panel) && panel != null)
            panel.SetActive(true);

        foreach (var pair in tabToggleMap)
            pair.Value?.SetTabState(pair.Key == activeTab, false);

        currentActiveTab = activeTab;
    }
}
