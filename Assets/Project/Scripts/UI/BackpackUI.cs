using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// FE Backpack view.
/// Owns UI binding/rendering only; gameplay mutations are requested through EventBus and handled by PlayerBridge.
/// </summary>
public class BackpackUI : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════
    //  Panel Main
    // ══════════════════════════════════════════════════════════

    [Header("Panel Main")]
    [Tooltip("Root chứa toàn bộ slot item. Thường là BackpackWindow/.../Viewport/Content.")]
    [SerializeField] private Transform slotsContainer;

    [Tooltip("Text hiển thị tổng tiền hiện tại.")]
    [SerializeField] private TMP_Text totalMoneyText;

    [FormerlySerializedAs("btnSort")]
    [SerializeField] private Button btnSort;

    [Tooltip("Giảm giá trị trong Split Input.")]
    [SerializeField] private Button btnReturn;

    [Tooltip("Tăng giá trị trong Split Input.")]
    [SerializeField] private Button btnNext;

    [FormerlySerializedAs("splitInput")]
    [SerializeField] private TMP_InputField splitInput;

    [FormerlySerializedAs("btnSeparate")]
    [SerializeField] private Button btnSplit;

    // ══════════════════════════════════════════════════════════
    //  Header InfoItem
    // ══════════════════════════════════════════════════════════

    [Header("Header InfoItem")]
    [FormerlySerializedAs("infoIcon")]
    [SerializeField] private Image itemIcon;

    [FormerlySerializedAs("infoAmountText")]
    [SerializeField] private TMP_Text itemAmountText;

    [FormerlySerializedAs("nameText")]
    [SerializeField] private TMP_Text itemNameText;

    [FormerlySerializedAs("categoryText")]
    [SerializeField] private TMP_Text categoryText;

    [SerializeField] private TMP_Text rareText;

    [FormerlySerializedAs("rareObject")]
    [SerializeField] private GameObject rareObject;

    [FormerlySerializedAs("descText")]
    [SerializeField] private TMP_Text descText;

    [FormerlySerializedAs("priceText")]
    [SerializeField] private TMP_Text priceText;

    [FormerlySerializedAs("btnUse")]
    [SerializeField] private Button btnUse;

    [FormerlySerializedAs("btnDrop")]
    [SerializeField] private Button btnDrop;

    // ══════════════════════════════════════════════════════════
    //  Item Stats
    // ══════════════════════════════════════════════════════════

    [Header("Item Stats")]
    [SerializeField] private StatDefinitionDatabase statDatabase;
    [SerializeField] private Transform statsContent;
    [SerializeField] private StatRowUI statRowPrefab;

    private const int MaxDisplayAmount = 99999;
    private const int DefaultSplitAmount = 1;
    private const float InfoTooltipHideDelay = 0.08f;

    private InventoryGridItemData[] cache;
    private InventoryGridView gridView;
    private RectTransform itemInfoPanelRoot;
    private RectTransform itemInfoBodyRoot;
    private RectTransform itemInfoGuideRoot;
    private RectTransform hoveredSlotRoot;
    private LocalizedText itemNameLocalized;
    private LocalizedText descLocalized;
    private LocalizedText categoryLocalized;
    private readonly List<GameObject> spawnedStatRows = new();
    private bool viewsReady;
    private bool wasActive;
    private bool dirty;
    private bool subscribed;
    private bool refreshRequestedAfterSubscribe;
    private bool infoTooltipPointerInside;
    private int selectedIndex = -1;
    private int hoveredIndex = -1;
    private int selectedAmount;
    private Coroutine hideInfoTooltipRoutine;

    // ══════════════════════════════════════════════════════════
    //  Unity Lifecycle
    // ══════════════════════════════════════════════════════════

    private void Start()
    {
        AutoBindReferences();
        SubscribeEvents();
        ClearInfoPanel();
        SetSplitAmount(DefaultSplitAmount);
        PublishVisualRefreshRequest();
    }

    private void OnEnable()
    {
        AutoBindReferences();
        SubscribeEvents();
        RegisterButtonEvents();
        PublishVisualRefreshRequest();
    }

    private void OnDisable()
    {
        CancelInfoTooltipHide();
        hoveredIndex = -1;
        hoveredSlotRoot = null;
        infoTooltipPointerInside = false;
        SetInfoTooltipVisible(false);
        UnsubscribeEvents();
        UnregisterButtonEvents();
    }

    private void LateUpdate()
    {
        if (!subscribed)
            SubscribeEvents();

        bool active = IsContainerActive();
        if (active && subscribed && cache == null)
            PublishVisualRefreshRequest();

        if (active && (!wasActive || dirty))
        {
            EnsureViews();
            RefreshAllSlots();
            dirty = false;
        }

        wasActive = active;
    }

    // ══════════════════════════════════════════════════════════
    //  Subscribe Events
    // ══════════════════════════════════════════════════════════

    private void SubscribeEvents()
    {
        if (subscribed) return;

        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;

        bus.Subscribe<BackpackSlotChangedPublish>(OnBackpackSlotChanged);
        bus.Subscribe<BackpackItemInfoChangedPublish>(OnBackpackItemInfoChanged);
        bus.Subscribe<StatsChangedPublish>(OnStatsChanged);
        subscribed = true;

        if (!refreshRequestedAfterSubscribe)
        {
            refreshRequestedAfterSubscribe = true;
            PublishVisualRefreshRequest();
        }
    }

    private void UnsubscribeEvents()
    {
        if (!subscribed) return;

        var bus = GameManager.Instance?.EventBus;
        if (bus != null)
        {
            bus.Unsubscribe<BackpackSlotChangedPublish>(OnBackpackSlotChanged);
            bus.Unsubscribe<BackpackItemInfoChangedPublish>(OnBackpackItemInfoChanged);
            bus.Unsubscribe<StatsChangedPublish>(OnStatsChanged);
        }

        subscribed = false;
        refreshRequestedAfterSubscribe = false;
    }

    private void RegisterButtonEvents()
    {
        if (btnSort != null) btnSort.onClick.AddListener(PublishSortRequest);
        if (btnReturn != null) btnReturn.onClick.AddListener(DecreaseSplitAmount);
        if (btnNext != null) btnNext.onClick.AddListener(IncreaseSplitAmount);
        if (btnSplit != null) btnSplit.onClick.AddListener(PublishSplitRequest);
        if (btnDrop != null) btnDrop.onClick.AddListener(PublishDropRequest);
        if (btnUse != null) btnUse.onClick.AddListener(PublishUseRequest);
    }

    private void UnregisterButtonEvents()
    {
        if (btnSort != null) btnSort.onClick.RemoveListener(PublishSortRequest);
        if (btnReturn != null) btnReturn.onClick.RemoveListener(DecreaseSplitAmount);
        if (btnNext != null) btnNext.onClick.RemoveListener(IncreaseSplitAmount);
        if (btnSplit != null) btnSplit.onClick.RemoveListener(PublishSplitRequest);
        if (btnDrop != null) btnDrop.onClick.RemoveListener(PublishDropRequest);
        if (btnUse != null) btnUse.onClick.RemoveListener(PublishUseRequest);
    }

    // ══════════════════════════════════════════════════════════
    //  Event Handlers
    // ══════════════════════════════════════════════════════════

    private void OnBackpackSlotChanged(BackpackSlotChangedPublish e)
    {
        EnsureCache(e.index + 1);
        cache[e.index] = new InventoryGridItemData(e.icon, e.amount, resourceMeter: e.meter);
        dirty = true;

        if (IsContainerActive())
        {
            EnsureViews();
            ApplySlot(e.index);
        }
    }

    private void OnBackpackItemInfoChanged(BackpackItemInfoChangedPublish e)
    {
        if (!e.isPreview)
        {
            selectedIndex = e.slotIndex;
            selectedAmount = e.amount;
            UpdateSelectionVisual();
            UpdateSplitControls();
        }

        if (e.isEmpty)
        {
            ClearInfoPanel();
            SetInfoTooltipVisible(false);
            return;
        }

        SetIcon(itemIcon, e.icon);
        SetAmountText(itemAmountText, e.amount, true);
        SetOptionalLocalizedOrText(itemNameLocalized, itemNameText, e.nameKey, itemNameText?.gameObject);
        SetOptionalLocalizedOrText(descLocalized, descText, e.descKey, GetDescriptionRoot()?.gameObject ?? descText?.gameObject);
        SetOptionalLocalizedOrText(categoryLocalized, categoryText, e.categoryKey, GetFieldRoot(categoryText, "Category"));

        SetOptionalText(priceText, e.sellPrice > 0 ? e.sellPrice.ToString() : string.Empty, GetFieldRoot(priceText, "Price"));

        SetOptionalText(rareText, string.Empty, rareObject != null ? rareObject : rareText?.gameObject);

        if (btnUse != null)
            btnUse.interactable = e.canUse;

        ApplyStats(e.stats);

        if (e.isPreview)
        {
            PositionInfoTooltip(hoveredSlotRoot);
            SetInfoTooltipVisible(true);
        }
    }

    private void OnStatsChanged(StatsChangedPublish e)
    {
        if (e.statType != StatType.Money) return;
        SetTotalMoney(e.newValue);
    }

    // ══════════════════════════════════════════════════════════
    //  Publish Events
    // ══════════════════════════════════════════════════════════

    private void PublishSlotSelectedRequest(int index)
    {
        selectedIndex = index;
        UpdateSelectionVisual();
        GameManager.Instance?.EventBus?.Publish(new BackpackSlotSelectedRequestPublish(index));
    }

    private void PublishSortRequest()
    {
        GameManager.Instance?.EventBus?.Publish(new BackpackSortRequestPublish());
    }

    private void PublishSplitRequest()
    {
        if (selectedIndex < 0) return;

        int amount = GetSplitAmount();
        if (amount <= 0) return;

        GameManager.Instance?.EventBus?.Publish(
            new BackpackSplitRequestPublish(selectedIndex, amount));
    }

    private void PublishDropRequest()
    {
        if (selectedIndex < 0) return;

        GameManager.Instance?.EventBus?.Publish(
            new InventoryDropRequestPublish(InventoryType.Backpack, selectedIndex));
    }

    private void PublishUseRequest()
    {
        if (selectedIndex < 0) return;

        GameManager.Instance?.EventBus?.Publish(
            new InventoryUseRequestPublish(InventoryType.Backpack, selectedIndex));
    }

    private void PublishVisualRefreshRequest()
    {
        GameManager.Instance?.EventBus?.Publish(new InventoryVisualRefreshRequestPublish());
    }

    // ══════════════════════════════════════════════════════════
    //  Render / Grid View
    // ══════════════════════════════════════════════════════════

    private void RefreshAllSlots()
    {
        if (cache == null || gridView == null) return;
        gridView.Render(cache, selectedIndex);
    }

    private void ApplySlot(int index)
    {
        if (cache == null || index < 0 || index >= cache.Length) return;
        gridView?.SetSlot(index, cache[index]);
    }

    private void EnsureViews()
    {
        AutoAssignSlotsContainer();
        if (viewsReady || slotsContainer == null) return;

        gridView = slotsContainer.GetComponent<InventoryGridView>();
        if (gridView == null)
            gridView = slotsContainer.gameObject.AddComponent<InventoryGridView>();

        gridView.Configure(
            slotsContainer,
            dragDropEnabled: true,
            dragType: InventoryType.Backpack);
        gridView.SetClickHandler((index, _) => PublishSlotSelectedRequest(index));
        gridView.SetHoverHandlers(
            (index, _, slotRect) => PublishSlotPreviewRequest(index, slotRect),
            OnSlotPreviewExited);

        viewsReady = true;
    }

    private void UpdateSelectionVisual()
    {
        gridView?.SetSelectedIndex(selectedIndex);
    }

    private void ClearInfoPanel()
    {
        selectedAmount = 0;

        SetIcon(itemIcon, null);
        SetAmountText(itemAmountText, 0, true);
        SetOptionalLocalizedOrText(itemNameLocalized, itemNameText, string.Empty, itemNameText?.gameObject);
        SetOptionalLocalizedOrText(descLocalized, descText, string.Empty, GetDescriptionRoot()?.gameObject ?? descText?.gameObject);
        SetOptionalLocalizedOrText(categoryLocalized, categoryText, string.Empty, GetFieldRoot(categoryText, "Category"));

        SetOptionalText(rareText, string.Empty, rareObject != null ? rareObject : rareText?.gameObject);
        SetOptionalText(priceText, string.Empty, GetFieldRoot(priceText, "Price"));
        if (btnUse != null) btnUse.interactable = false;
        ClearStats();

        UpdateSplitControls();
    }

    private void PublishSlotPreviewRequest(int index, RectTransform slotRect)
    {
        CancelInfoTooltipHide();
        hoveredIndex = index;
        hoveredSlotRoot = slotRect;
        GameManager.Instance?.EventBus?.Publish(new BackpackSlotPreviewRequestPublish(index));
    }

    private void OnSlotPreviewExited(int index)
    {
        if (hoveredIndex == index)
        {
            hoveredIndex = -1;
            hoveredSlotRoot = null;
        }

        ScheduleInfoTooltipHide();
    }

    private void ApplyStats(StatDisplay[] stats)
    {
        ClearStats();

        if (stats == null || stats.Length == 0)
            return;

        AutoAssignStatsRefs();

        if (statDatabase == null || statsContent == null)
            return;

        for (int i = 0; i < stats.Length; i++)
        {
            if (!statDatabase.TryGet(stats[i].statType, out var definition))
                continue;

            var row = CreateStatRow();
            if (row == null)
                continue;

            row.Setup(definition, stats[i].value);
            spawnedStatRows.Add(row.gameObject);
        }

        MoveDescriptionToBottom();
    }

    private void ClearStats()
    {
        for (int i = spawnedStatRows.Count - 1; i >= 0; i--)
        {
            if (spawnedStatRows[i] != null)
                Destroy(spawnedStatRows[i]);
        }

        spawnedStatRows.Clear();
        MoveDescriptionToBottom();
    }

    private StatRowUI CreateStatRow()
    {
        if (statsContent == null)
            return null;

        if (statRowPrefab != null)
        {
            var row = Instantiate(statRowPrefab, statsContent);
            PlaceStatRowBeforeDescription(row.transform);
            row.gameObject.SetActive(true);
            return row;
        }

        var rowObject = new GameObject("StatRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(StatRowUI));
        rowObject.transform.SetParent(statsContent, false);
        PlaceStatRowBeforeDescription(rowObject.transform);

        var layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var nameObject = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObject.transform.SetParent(rowObject.transform, false);
        nameObject.GetComponent<TextMeshProUGUI>().fontSize = 18f;

        var valueObject = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
        valueObject.transform.SetParent(rowObject.transform, false);
        valueObject.GetComponent<TextMeshProUGUI>().fontSize = 18f;

        return rowObject.GetComponent<StatRowUI>();
    }

    private void PlaceStatRowBeforeDescription(Transform row)
    {
        if (row == null || statsContent == null)
            return;

        var descRoot = GetDescriptionRoot();
        if (descRoot == null || descRoot.parent != statsContent)
            return;

        row.SetSiblingIndex(descRoot.GetSiblingIndex());
    }

    private void MoveDescriptionToBottom()
    {
        var descRoot = GetDescriptionRoot();
        if (descRoot == null)
            return;

        descRoot.SetAsLastSibling();
    }

    private Transform GetDescriptionRoot()
    {
        AutoAssignStatsRefs();

        if (statsContent == null)
            return null;

        return FindDirectChild(statsContent, "Desc_info")
            ?? FindDirectChild(statsContent, "Desc_Info")
            ?? FindDeepChild(statsContent, "Desc_info")
            ?? FindDeepChild(statsContent, "Desc_Info");
    }

    private void SetTotalMoney(float value)
    {
        if (totalMoneyText != null)
            totalMoneyText.text = Mathf.FloorToInt(value).ToString();
    }

    // ══════════════════════════════════════════════════════════
    //  Split Controls
    // ══════════════════════════════════════════════════════════

    private void DecreaseSplitAmount()
    {
        SetSplitAmount(GetSplitAmount() - 1);
    }

    private void IncreaseSplitAmount()
    {
        SetSplitAmount(GetSplitAmount() + 1);
    }

    private int GetSplitAmount()
    {
        if (splitInput == null || string.IsNullOrWhiteSpace(splitInput.text))
            return DefaultSplitAmount;

        return int.TryParse(splitInput.text, out int value)
            ? value
            : DefaultSplitAmount;
    }

    private void SetSplitAmount(int value)
    {
        int max = selectedAmount > 1 ? selectedAmount - 1 : 1;
        int clamped = Mathf.Clamp(value, 1, max);

        if (splitInput != null)
            splitInput.SetTextWithoutNotify(clamped.ToString());

        UpdateSplitControls();
    }

    private void UpdateSplitControls()
    {
        bool canSplit = selectedIndex >= 0 && selectedAmount > 1;
        int current = GetSplitAmount();
        int max = selectedAmount > 1 ? selectedAmount - 1 : 1;

        if (btnReturn != null) btnReturn.interactable = canSplit && current > 1;
        if (btnNext != null) btnNext.interactable = canSplit && current < max;
        if (btnSplit != null) btnSplit.interactable = canSplit;
        if (splitInput != null) splitInput.interactable = canSplit;
    }

    // ══════════════════════════════════════════════════════════
    //  Cache / State
    // ══════════════════════════════════════════════════════════

    private bool IsContainerActive()
    {
        AutoAssignSlotsContainer();
        return slotsContainer != null && slotsContainer.gameObject.activeInHierarchy;
    }

    private void EnsureCache(int minSize)
    {
        if (cache == null)
        {
            cache = new InventoryGridItemData[minSize];
            return;
        }

        if (cache.Length >= minSize) return;

        var old = cache;
        cache = new InventoryGridItemData[minSize];
        old.CopyTo(cache, 0);
    }

    // ══════════════════════════════════════════════════════════
    //  Auto Bind References
    // ══════════════════════════════════════════════════════════

    private void AutoBindReferences()
    {
        AutoAssignSlotsContainer();
        AutoAssignMainPanelRefs();
        AutoAssignInfoItemRefs();
        AutoAssignInfoTooltipRefs();
        AutoAssignStatsRefs();

        if (itemNameText != null) itemNameLocalized = itemNameText.GetComponent<LocalizedText>();
        if (descText != null) descLocalized = descText.GetComponent<LocalizedText>();
        if (categoryText != null) categoryLocalized = categoryText.GetComponent<LocalizedText>();
    }

    private void AutoAssignMainPanelRefs()
    {
        var window = ResolveBackpackWindow();
        var root = window != null ? window : transform.root;
        var footer = FindDeepChild(window, "GridFooter") ?? FindDeepChild(root, "BackPack_menu");

        totalMoneyText ??= FindText(root, "total_money")
                        ?? FindText(root, "MoneyText");

        btnSort ??= FindButton(footer, "btn_sort")
                ?? FindButton(footer, "SortButton");

        btnReturn ??= FindButton(footer, "btn_return")
                  ?? FindButton(footer, "PrevPageButton");

        btnNext ??= FindButton(footer, "btn_next")
                ?? FindButton(footer, "NextPageButton");

        splitInput ??= FindInput(footer, "Number_inputText")
                   ?? FindInput(footer, "SplitInput")
                   ?? FindInput(footer, "PageInput");

        btnSplit ??= FindButton(footer, "btnSplit")
                 ?? FindButton(footer, "Btn_Split")
                 ?? FindButton(footer, "btn_Separate")
                 ?? FindButton(footer, "SplitButton");
    }

    private void AutoAssignInfoItemRefs()
    {
        var window = ResolveBackpackWindow();
        var root = window != null ? window : transform.root;
        var infoRoot = FindDeepChild(root, "Info_Item");
        if (infoRoot == null) return;

        itemIcon ??= FindImage(infoRoot, "icon")
                  ?? FindImage(infoRoot, "Icon")
                  ?? FindImage(infoRoot, "Image");

        itemNameText ??= FindText(infoRoot, "itemName")
                      ?? FindText(infoRoot, "Name_Tmp")
                      ?? FindText(infoRoot, "ItemName");

        categoryText ??= FindText(infoRoot, "Category")
                      ?? FindText(infoRoot, "Category_tmp")
                      ?? FindText(infoRoot, "ItemType");

        rareText ??= FindText(infoRoot, "rare")
                  ?? FindText(infoRoot, "Rare")
                  ?? FindText(infoRoot, "Rarity_tmp")
                  ?? FindText(infoRoot, "ItemRarity");

        descText ??= FindText(infoRoot, "descText")
                  ?? FindText(infoRoot, "desc_tmp")
                  ?? FindText(infoRoot, "Description");

        var pricePanel = FindDeepChild(infoRoot, "Price_panel");
        priceText ??= FindText(pricePanel, "value")
                   ?? FindText(pricePanel, "Value")
                   ?? FindText(infoRoot, "priceText");

        btnUse ??= FindButton(infoRoot, "Btn_use")
                ?? FindButton(infoRoot, "Btn_Use")
                ?? FindButton(infoRoot, "UseButton");

        btnDrop ??= FindButton(infoRoot, "Btn_Drop")
                 ?? FindButton(infoRoot, "DropButton");

        AutoAssignStatsRefs();
    }

    private void AutoAssignInfoTooltipRefs()
    {
        if (itemInfoPanelRoot != null && itemInfoBodyRoot != null)
            return;

        var window = ResolveBackpackWindow();
        var body = FindDeepChild(window, "Body");
        var panel = FindDeepChild(body, "ItemInfoPanel");
        if (body is not RectTransform bodyRect || panel is not RectTransform panelRect)
            return;

        itemInfoBodyRoot = bodyRect;
        itemInfoPanelRoot = panelRect;
        var hoverRelay = itemInfoPanelRoot.GetComponent<BackpackInfoTooltipHoverRelay>();
        if (hoverRelay == null)
            hoverRelay = itemInfoPanelRoot.gameObject.AddComponent<BackpackInfoTooltipHoverRelay>();

        hoverRelay.Initialize(this);
        ConfigureInfoTooltipLayout();
    }

    private void ConfigureInfoTooltipLayout()
    {
        if (itemInfoPanelRoot == null || itemInfoBodyRoot == null)
            return;

        var gridPanel = FindDeepChild(itemInfoBodyRoot, "GridPanel") as RectTransform;
        if (gridPanel != null)
        {
            gridPanel.anchorMin = Vector2.zero;
            gridPanel.anchorMax = Vector2.one;
            gridPanel.pivot = new Vector2(0f, 0f);
            gridPanel.offsetMin = Vector2.zero;
            gridPanel.offsetMax = Vector2.zero;
        }

        itemInfoPanelRoot.anchorMin = new Vector2(0f, 1f);
        itemInfoPanelRoot.anchorMax = new Vector2(0f, 1f);
        itemInfoPanelRoot.pivot = new Vector2(0f, 1f);
        itemInfoPanelRoot.sizeDelta = new Vector2(420f, 560f);
        itemInfoPanelRoot.SetAsLastSibling();

        EnsureInfoGuidePanel();
        SetInfoTooltipVisible(false);
    }

    private void EnsureInfoGuidePanel()
    {
        if (itemInfoGuideRoot != null || itemInfoBodyRoot == null)
            return;

        var existing = FindDeepChild(itemInfoBodyRoot, "BackpackGuidePanel") as RectTransform;
        if (existing != null)
        {
            itemInfoGuideRoot = existing;
            return;
        }

        var panel = new GameObject("BackpackGuidePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        panel.transform.SetParent(itemInfoBodyRoot, false);
        itemInfoGuideRoot = panel.GetComponent<RectTransform>();
        itemInfoGuideRoot.anchorMin = new Vector2(0.72f, 0f);
        itemInfoGuideRoot.anchorMax = new Vector2(1f, 1f);
        itemInfoGuideRoot.pivot = new Vector2(1f, 0.5f);
        itemInfoGuideRoot.offsetMin = new Vector2(10f, 12f);
        itemInfoGuideRoot.offsetMax = new Vector2(-14f, -12f);

        var image = panel.GetComponent<Image>();
        image.color = new Color(0.18f, 0.10f, 0.04f, 0.72f);
        image.raycastTarget = false;

        var outline = panel.GetComponent<Outline>();
        outline.effectColor = new Color(0.72f, 0.47f, 0.18f, 0.95f);
        outline.effectDistance = new Vector2(2f, -2f);

        var title = CreateGuideText("GuideTitle", panel.transform, "Thông tin vật phẩm", 24f, TextAlignmentOptions.Center, new Color(1f, 0.86f, 0.50f));
        SetRect(title.rectTransform, Vector2.up, Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(-28f, 42f));

        var body = CreateGuideText(
            "GuideBody",
            panel.transform,
            "Di chuột vào một ô vật phẩm để xem tên, mô tả, giá bán và chỉ số liên quan.\n\nChọn vật phẩm để sử dụng, tách hoặc thả khỏi túi đồ.",
            18f,
            TextAlignmentOptions.TopLeft,
            new Color(0.96f, 0.86f, 0.66f));
        body.enableWordWrapping = true;
        SetRect(body.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -54f), new Vector2(-42f, -120f));

        itemInfoGuideRoot.SetAsLastSibling();
    }

    private void PositionInfoTooltip(RectTransform slotRect)
    {
        if (itemInfoPanelRoot == null || itemInfoBodyRoot == null || slotRect == null)
            return;

        var slotBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(itemInfoBodyRoot, slotRect);
        var bodyRect = itemInfoBodyRoot.rect;
        var panelSize = itemInfoPanelRoot.rect.size;
        if (panelSize.x <= 0f || panelSize.y <= 0f)
            panelSize = itemInfoPanelRoot.sizeDelta;

        var x = slotBounds.max.x + 16f;
        if (x + panelSize.x > bodyRect.xMax - 10f)
            x = slotBounds.min.x - panelSize.x - 16f;

        x = Mathf.Clamp(x, bodyRect.xMin + 10f, bodyRect.xMax - panelSize.x - 10f);

        var y = Mathf.Clamp(
            slotBounds.max.y,
            bodyRect.yMin + panelSize.y + 10f,
            bodyRect.yMax - 10f);

        itemInfoPanelRoot.anchoredPosition = new Vector2(
            x - bodyRect.xMin,
            y - bodyRect.yMax);
    }

    private void SetInfoTooltipVisible(bool visible)
    {
        if (itemInfoPanelRoot != null && itemInfoPanelRoot.gameObject.activeSelf != visible)
            itemInfoPanelRoot.gameObject.SetActive(visible);

        if (itemInfoGuideRoot != null && itemInfoGuideRoot.gameObject.activeSelf == visible)
            itemInfoGuideRoot.gameObject.SetActive(!visible);
    }

    public void OnInfoTooltipPointerEnter()
    {
        infoTooltipPointerInside = true;
        CancelInfoTooltipHide();
    }

    public void OnInfoTooltipPointerExit()
    {
        infoTooltipPointerInside = false;
        if (hoveredIndex < 0)
            ScheduleInfoTooltipHide();
    }

    private void ScheduleInfoTooltipHide()
    {
        CancelInfoTooltipHide();
        hideInfoTooltipRoutine = StartCoroutine(HideInfoTooltipAfterDelay());
    }

    private IEnumerator HideInfoTooltipAfterDelay()
    {
        yield return new WaitForSecondsRealtime(InfoTooltipHideDelay);
        hideInfoTooltipRoutine = null;

        if (hoveredIndex < 0 && !infoTooltipPointerInside)
            SetInfoTooltipVisible(false);
    }

    private void CancelInfoTooltipHide()
    {
        if (hideInfoTooltipRoutine == null)
            return;

        StopCoroutine(hideInfoTooltipRoutine);
        hideInfoTooltipRoutine = null;
    }

    private void AutoAssignStatsRefs()
    {
        statDatabase ??= Resources.Load<StatDefinitionDatabase>("StatDefinitionDatabase");

        var window = ResolveBackpackWindow();
        var root = window != null ? window : transform.root;
        var infoRoot = FindDeepChild(root, "Info_Item");

        if (statsContent == null)
        {
            var statScroll = FindDeepChild(infoRoot, "attribute_SclView");
            var content = FindDeepChild(statScroll, "content")
                       ?? FindDeepChild(statScroll, "Content")
                       ?? FindDeepChild(infoRoot, "StatsContent")
                       ?? FindDeepChild(infoRoot, "AttributeContent");

            statsContent = content;
        }

        if (statRowPrefab == null && statsContent != null)
        {
            statRowPrefab = statsContent.GetComponentInChildren<StatRowUI>(true);

            if (statRowPrefab != null && statRowPrefab.transform.IsChildOf(statsContent))
                statRowPrefab.gameObject.SetActive(false);
        }
    }

    private void AutoAssignSlotsContainer()
    {
        if (slotsContainer != null && IsValidSlotsContainerOwner(slotsContainer))
            return;

        slotsContainer = null;

        var root = transform.root;
        var backpackWindow = ResolveBackpackWindow();
        var backpackPanel = backpackWindow != null
            ? FindDeepChild(backpackWindow, "Backpack_Panel")
            : FindDeepChild(root, "Backpack_Panel");

        var found = FindSlotContainer(backpackPanel)
                 ?? FindSlotContainer(backpackWindow)
                 ?? FindSlotContainer(root);

        if (found != null)
        {
            slotsContainer = found;
            viewsReady = false;
        }

        if (slotsContainer == null)
            Debug.LogWarning("[BackpackUI] Không tìm thấy slotsContainer cho BackpackWindow/BackpackWindow_Template.");
    }

    private Transform ResolveBackpackWindow()
    {
        var owner = FindAncestorBackpackWindow(transform);
        if (owner != null) return owner;

        var root = transform.root;

        var template = FindDeepChild(root, "BackpackWindow_Template");
        if (template != null) return template;

        return FindDeepChild(root, "BackpackWindow");
    }

    private static Transform FindAncestorBackpackWindow(Transform target)
    {
        while (target != null)
        {
            if (target.name.Equals("BackpackWindow_Template", StringComparison.OrdinalIgnoreCase) ||
                target.name.Equals("BackpackWindow", StringComparison.OrdinalIgnoreCase))
            {
                return target;
            }

            target = target.parent;
        }

        return null;
    }

    private bool IsValidSlotsContainerOwner(Transform candidate)
    {
        if (candidate == null) return false;

        var expectedWindow = ResolveBackpackWindow();
        if (expectedWindow == null) return true;

        return candidate == expectedWindow || candidate.IsChildOf(expectedWindow);
    }

    private static bool IsTemplateTransform(Transform target)
    {
        while (target != null)
        {
            if (target.name.IndexOf("_Template", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            target = target.parent;
        }

        return false;
    }

    private static Transform FindSlotContainer(Transform root)
    {
        if (root == null) return null;

        var preferred = FindDeepChild(root, "Content");
        if (LooksLikeSlotContainer(preferred)) return preferred;

        var grid = FindDeepChild(root, "Grid");
        if (LooksLikeSlotContainer(grid)) return grid;

        return FindFirstSlotContainer(root);
    }

    private static Transform FindFirstSlotContainer(Transform root)
    {
        if (root == null) return null;
        if (LooksLikeSlotContainer(root)) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindFirstSlotContainer(root.GetChild(i));
            if (found != null) return found;
        }

        return null;
    }

    private static bool LooksLikeSlotContainer(Transform candidate)
    {
        if (candidate == null || candidate.childCount < 4) return false;

        int slotLike = 0;
        for (int i = 0; i < candidate.childCount; i++)
        {
            var child = candidate.GetChild(i);
            if (child.name.IndexOf("slot", StringComparison.OrdinalIgnoreCase) >= 0 ||
                FindDirectChild(child, "Icon") != null ||
                FindDirectChild(child, "icon") != null ||
                FindDirectChild(child, "Amount") != null)
            {
                slotLike++;
            }
        }

        return slotLike >= 4;
    }

    // ══════════════════════════════════════════════════════════
    //  UI Helpers
    // ══════════════════════════════════════════════════════════

    private static void SetLocalizedOrText(LocalizedText localized, TMP_Text text, string key)
    {
        if (localized != null)
        {
            localized.SetKey(key);
            return;
        }

        if (text != null)
            text.text = LocalizationManager.Instance != null
                ? LocalizationManager.Instance.GetText(key)
                : key ?? string.Empty;
    }

    private static void SetOptionalLocalizedOrText(LocalizedText localized, TMP_Text text, string key, GameObject displayRoot)
    {
        bool hasValue = !string.IsNullOrWhiteSpace(key);

        SetActiveIfNotNull(displayRoot, hasValue);

        if (!hasValue)
        {
            if (localized != null) localized.SetKey(string.Empty);
            if (text != null) text.text = string.Empty;
            return;
        }

        SetLocalizedOrText(localized, text, key);
    }

    private static void SetOptionalText(TMP_Text text, string value, GameObject displayRoot)
    {
        bool hasValue = !string.IsNullOrWhiteSpace(value);

        SetActiveIfNotNull(displayRoot, hasValue);

        if (text != null)
            text.text = hasValue ? value : string.Empty;
    }

    private static void SetActiveIfNotNull(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
            target.SetActive(active);
    }

    private static GameObject GetFieldRoot(TMP_Text text, string parentNameHint)
    {
        if (text == null)
            return null;

        var parent = text.transform.parent;
        if (parent != null &&
            !string.IsNullOrEmpty(parentNameHint) &&
            parent.name.IndexOf(parentNameHint, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return parent.gameObject;
        }

        return text.gameObject;
    }

    private static void SetIcon(Image image, Sprite sprite)
    {
        if (image == null) return;

        image.sprite = sprite;
        image.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        image.enabled = sprite != null;
    }

    private static void SetAmountText(TMP_Text text, int amount, bool showWhenOne)
    {
        if (text == null) return;

        if (amount <= 0 || (!showWhenOne && amount <= 1))
        {
            text.text = string.Empty;
            return;
        }

        text.text = amount > MaxDisplayAmount
            ? MaxDisplayAmount.ToString()
            : amount.ToString();
    }

    private static Button FindButton(Transform root, string name)
    {
        var target = FindDeepChild(root, name);
        return target != null ? target.GetComponent<Button>() : null;
    }

    private static TMP_Text FindText(Transform root, string name)
    {
        var target = FindDeepChild(root, name);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private static TMP_InputField FindInput(Transform root, string name)
    {
        var target = FindDeepChild(root, name);
        return target != null ? target.GetComponent<TMP_InputField>() : null;
    }

    private static Image FindImage(Transform root, string name)
    {
        var target = FindDeepChild(root, name);
        return target != null ? target.GetComponent<Image>() : null;
    }

    private static Transform FindDirectChild(Transform root, string name)
    {
        if (root == null) return null;

        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (string.Equals(child.name, name, StringComparison.OrdinalIgnoreCase))
                return child;
        }

        return null;
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name)) return null;
        if (string.Equals(root.name, name, StringComparison.OrdinalIgnoreCase)) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeepChild(root.GetChild(i), name);
            if (found != null) return found;
        }

        return null;
    }

    private static TextMeshProUGUI CreateGuideText(string name, Transform parent, string value, float size, TextAlignmentOptions alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
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

public sealed class BackpackInfoTooltipHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private BackpackUI owner;

    public void Initialize(BackpackUI target)
    {
        owner = target;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        owner?.OnInfoTooltipPointerEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        owner?.OnInfoTooltipPointerExit();
    }
}
