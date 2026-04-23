using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Quản lý đóng/mở menu và chuyển tab.
/// Gắn lên Menu GameObject.
///
/// - controllerPanel: HUD (HealthBar + Hotbar) — luôn hiện khi chơi.
/// - menuPanel: Window (Inventory, InfoPlayer...) — ẩn/hiện khi mở menu.
/// - openButton: nút mở menu.
/// - Phím I hoặc Tab: toggle menu.
/// </summary>
public class UISystem : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject controllerPanel;
    [SerializeField] private GameObject menuPanel;

    [Header("Control Buttons")]
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    [Header("Tab Management")]
    [SerializeField] private TabToggle[] tabToggles;
    [SerializeField] private GameObject[] tabPanels;

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.I;

    private Dictionary<TabType, GameObject> tabPanelMap;
    private Dictionary<TabType, TabToggle> tabToggleMap;
    private TabType currentActiveTab;
    private bool isMenuOpen;

    private void Start()
    {
        ShowController();
        InitializeTabSystem();

        if (openButton != null) openButton.onClick.AddListener(ShowMenu);
        if (closeButton != null) closeButton.onClick.AddListener(ShowController);

        OnTabSelected(TabType.Inventory);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (isMenuOpen) ShowController();
            else ShowMenu();
        }
    }

    private void OnDestroy()
    {
        if (openButton != null) openButton.onClick.RemoveListener(ShowMenu);
        if (closeButton != null) closeButton.onClick.RemoveListener(ShowController);
        UnsubscribeFromTabEvents();
    }

    // ── Show/Hide ─────────────────────────────────────────────────────────────

    public void ShowController()
    {
        isMenuOpen = false;
        if (controllerPanel != null) controllerPanel.SetActive(true);
        if (menuPanel != null) menuPanel.SetActive(false);
        if (openButton != null) openButton.gameObject.SetActive(true);
        if (closeButton != null) closeButton.gameObject.SetActive(false);

        // Bật lại input player
        var player = FindAnyObjectByType<PlayerControler>();
        if (player != null) player.InputEnabled = true;
    }

    public void ShowMenu()
    {
        isMenuOpen = true;
        if (controllerPanel != null) controllerPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(true);
        if (openButton != null) openButton.gameObject.SetActive(false);
        if (closeButton != null) closeButton.gameObject.SetActive(true);

        // Tắt input player khi menu mở
        var player = FindAnyObjectByType<PlayerControler>();
        if (player != null) player.InputEnabled = false;

        ForceSetInventoryTab();
    }

    private void ForceSetInventoryTab()
    {
        SetActiveTab(TabType.Inventory, false);
        if (tabToggleMap != null && tabToggleMap.TryGetValue(TabType.Inventory, out var toggle) && toggle != null)
            toggle.SetTabState(true, false);
    }

    // ── Tab System ────────────────────────────────────────────────────────────

    private void InitializeTabSystem()
    {
        tabPanelMap = new Dictionary<TabType, GameObject>();
        tabToggleMap = new Dictionary<TabType, TabToggle>();

        if (tabToggles != null && tabPanels != null)
        {
            int count = Mathf.Min(tabToggles.Length, tabPanels.Length);
            for (int i = 0; i < count; i++)
            {
                if (tabToggles[i] != null && tabPanels[i] != null)
                {
                    tabPanelMap[tabToggles[i].TabType] = tabPanels[i];
                    tabToggleMap[tabToggles[i].TabType] = tabToggles[i];
                }
            }
        }

        SubscribeToTabEvents();
    }

    private void SubscribeToTabEvents()
    {
        if (tabToggles == null) return;
        foreach (var t in tabToggles)
        {
            if (t != null)
            {
                t.OnTabSelected += OnTabSelected;
                t.OnTabDeselected += OnTabDeselected;
            }
        }
    }

    private void UnsubscribeFromTabEvents()
    {
        if (tabToggles == null) return;
        foreach (var t in tabToggles)
        {
            if (t != null)
            {
                t.OnTabSelected -= OnTabSelected;
                t.OnTabDeselected -= OnTabDeselected;
            }
        }
    }

    private void OnTabSelected(TabType tabType)
    {
        SetActiveTab(tabType, true);
    }

    private void OnTabDeselected(TabType tabType) { }

    public void SetActiveTab(TabType tabType, bool triggerEvent = true)
    {
        if (currentActiveTab == tabType && tabPanelMap.ContainsKey(tabType))
        {
            // Đã active → skip
            if (tabPanelMap[tabType].activeSelf) return;
        }

        HideAllTabPanels();

        if (tabPanelMap.TryGetValue(tabType, out var panel) && panel != null)
            panel.SetActive(true);

        if (tabToggleMap.TryGetValue(tabType, out var targetToggle) && targetToggle != null && !targetToggle.IsOn)
            targetToggle.SetTabState(true, triggerEvent);

        if (tabToggleMap.TryGetValue(currentActiveTab, out var currentToggle) && currentToggle != null && currentToggle.IsOn)
            currentToggle.SetTabState(false, false);

        currentActiveTab = tabType;
    }

    private void HideAllTabPanels()
    {
        if (tabPanels == null) return;
        foreach (var panel in tabPanels)
            if (panel != null) panel.SetActive(false);
    }
}
