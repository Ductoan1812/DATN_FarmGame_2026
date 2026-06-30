using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanelUI : MonoBehaviour
{
    private const int InfiniteStockMaxQuantity = 99;

    [Header("Root")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private Button closeButton;

    [Header("Tabs")]
    [SerializeField] private Button buyTabButton;
    [SerializeField] private Button sellTabButton;
    [SerializeField] private GameObject buyPage;
    [SerializeField] private GameObject sellPage;

    [Header("Buy Page")]
    [SerializeField] private Transform buyListRoot;
    [SerializeField] private ShopBuyRowUI buyRowTemplate;

    [Header("Sell Page")]
    [SerializeField] private Transform sellInventoryGridRoot;
    [SerializeField] private Button sellSlotTemplate;
    [SerializeField] private Image selectedSellIcon;
    [SerializeField] private TMP_Text selectedSellNameText;
    [SerializeField] private TMP_Text selectedSellHintText;
    [SerializeField] private TMP_Text selectedSellTotalText;
    [SerializeField] private TMP_Text selectedSellQuantityText;
    [SerializeField] private TMP_Text sellCartSummaryText;
    [SerializeField] private Button sellMinusButton;
    [SerializeField] private Button sellPlusButton;
    [SerializeField] private Button sellMaxButton;
    [SerializeField] private Button sellClearButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private int visibleSellSlots = 30;

    private const string ExternalWindowId = "shop";

    private readonly List<GameObject> spawnedObjects = new();
    private readonly List<GameObject> spawnedSellCartRows = new();
    private readonly Dictionary<EntityRuntime, SellCartEntry> sellCart = new();
    private InventoryGridView sellGridView;
    private Transform sellCartListRoot;
    private TMP_Text sellCartEmptyText;
    private EventBus subscribedBus;
    private bool listenersRegistered;
    private ShopViewData currentView;
    private ShopItemViewData selectedSellItem;
    private UIController _uiController;

    private void OnEnable()
    {
        TrySubscribe();
        RegisterListeners();
        Hide();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void Update()
    {
        if (subscribedBus == null)
            TrySubscribe();
    }

    private void OnDisable()
    {
        if (subscribedBus != null)
        {
            subscribedBus.Unsubscribe<ShopViewPublish>(OnShopView);
            subscribedBus = null;
        }

        UnregisterListeners();
        _uiController?.CloseExternalExclusiveWindow(ExternalWindowId);
    }

    private void OnShopView(ShopViewPublish e)
    {
        if (e.viewData == null) return;

        currentView = e.viewData;
        selectedSellItem = null;
        Show();
        SetTitle();
        SetMoney(currentView.CustomerMoney);
        ShowBuyPage();
    }

    private void RegisterListeners()
    {
        if (listenersRegistered) return;

        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (buyTabButton != null) buyTabButton.onClick.AddListener(ShowBuyPage);
        if (sellTabButton != null) sellTabButton.onClick.AddListener(ShowSellPage);
        EnsureSellBasketLayout();
        EnsureSellCartControls();
        if (sellMinusButton != null) sellMinusButton.onClick.AddListener(DecreaseSelectedSellQuantity);
        if (sellPlusButton != null) sellPlusButton.onClick.AddListener(IncreaseSelectedSellQuantity);
        if (sellMaxButton != null) sellMaxButton.onClick.AddListener(MaxSelectedSellQuantity);
        if (sellClearButton != null) sellClearButton.onClick.AddListener(ClearSellCart);
        if (sellButton != null) sellButton.onClick.AddListener(SellCart);

        listenersRegistered = true;
    }

    private void UnregisterListeners()
    {
        if (!listenersRegistered) return;

        if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
        if (buyTabButton != null) buyTabButton.onClick.RemoveListener(ShowBuyPage);
        if (sellTabButton != null) sellTabButton.onClick.RemoveListener(ShowSellPage);
        if (sellMinusButton != null) sellMinusButton.onClick.RemoveListener(DecreaseSelectedSellQuantity);
        if (sellPlusButton != null) sellPlusButton.onClick.RemoveListener(IncreaseSelectedSellQuantity);
        if (sellMaxButton != null) sellMaxButton.onClick.RemoveListener(MaxSelectedSellQuantity);
        if (sellClearButton != null) sellClearButton.onClick.RemoveListener(ClearSellCart);
        if (sellButton != null) sellButton.onClick.RemoveListener(SellCart);

        listenersRegistered = false;
    }

    private void ShowBuyPage()
    {
        if (currentView == null) return;

        if (buyPage != null) buyPage.SetActive(true);
        if (sellPage != null) sellPage.SetActive(false);

        sellGridView?.Clear();
        RebuildBuyList();
    }

    private void ShowSellPage()
    {
        if (currentView == null) return;

        if (buyPage != null) buyPage.SetActive(false);
        if (sellPage != null) sellPage.SetActive(true);

        selectedSellItem = null;
        sellCart.Clear();
        EnsureSellBasketLayout();
        RebuildSellGrid();
        RefreshSellSelection();
    }

    private void RebuildBuyList()
    {
        ClearSpawned();
        if (buyListRoot == null || currentView?.StockItems == null) return;

        if (buyRowTemplate == null) return;

        foreach (var item in currentView.StockItems)
        {
            if (item == null) continue;

            var row = Instantiate(buyRowTemplate, buyListRoot);
            row.gameObject.SetActive(true);
            spawnedObjects.Add(row.gameObject);

            row.Init(item, currentView.CustomerMoney, OnBuyItem);
        }

        ForceRebuild(buyListRoot);
    }

    private void OnBuyItem(ShopItemViewData item, int quantity)
    {
        var result = ShopService.TryBuy(currentView.Customer, currentView.Merchant, item, quantity);
        if (result.Success)
            ShopService.Open(currentView.Customer, currentView.Merchant, currentView.Shop);
        else
            Debug.LogWarning($"[ShopPanelUI] Buy failed: {result.FailReason}.");
    }



    private void RebuildSellGrid()
    {
        if (sellInventoryGridRoot == null) return;

        EnsureSellSlotTemplate();
        if (sellSlotTemplate == null) return;

        EnsureSellGridView();
        var items = new List<InventoryGridItemData>();
        if (currentView?.PlayerSellableItems != null)
        {
            foreach (var item in currentView.PlayerSellableItems)
            {
                if (item == null || !item.Sellable) continue;
                items.Add(new InventoryGridItemData(
                    item.ItemData != null ? item.ItemData.icon : null,
                    item.Amount,
                    item,
                    true));
            }
        }

        sellGridView.Render(items, -1, visibleSellSlots);
    }

    private void RefreshSellSelection()
    {
        bool hasSelection = selectedSellItem?.ItemData != null;
        bool canSellSelection = hasSelection && selectedSellItem != null && selectedSellItem.Sellable && selectedSellItem.Amount > 0;
        int selectedQuantity = GetCartQuantity(selectedSellItem);
        int cartTotal = GetSellCartTotal();
        int cartItemCount = GetSellCartItemCount();

        if (selectedSellIcon != null)
        {
            SetIcon(selectedSellIcon, hasSelection ? selectedSellItem.ItemData : null);
        }

        SetLocalizedText(selectedSellNameText, hasSelection ? selectedSellItem.NameKey : string.Empty);
        if (!hasSelection)
            SetLocalizedText(selectedSellHintText, "ui.shop.sell_empty_hint");
        else if (selectedSellItem.Sellable)
            SetLocalizedText(selectedSellHintText, selectedSellItem.ItemData.descKey);
        else
            SetPlainText(selectedSellHintText, "NPC khong thu mua vat pham nay.");

        SetPlainText(selectedSellQuantityText, hasSelection ? $"{selectedQuantity}/{selectedSellItem.Amount}" : "0/0");
        SetPlainText(sellCartSummaryText, $"Da chon: {cartItemCount} item");
        SetPlainText(selectedSellTotalText, cartTotal.ToString("N0"));
        RebuildSellCartRows();

        if (sellMinusButton != null)
            sellMinusButton.interactable = canSellSelection && selectedQuantity > 0;
        if (sellPlusButton != null)
            sellPlusButton.interactable = canSellSelection && selectedQuantity < selectedSellItem.Amount;
        if (sellMaxButton != null)
            sellMaxButton.interactable = canSellSelection;
        if (sellClearButton != null)
            sellClearButton.interactable = sellCart.Count > 0;
        if (sellButton != null)
            sellButton.interactable = sellCart.Count > 0;
    }

    private void SellCart()
    {
        if (currentView == null || sellCart.Count == 0) return;

        var entries = new List<SellCartEntry>(sellCart.Values);
        foreach (var entry in entries)
        {
            if (entry.Item?.Item == null || entry.Quantity <= 0)
                continue;

            var result = ShopService.TrySell(
                currentView.Customer,
                currentView.Merchant,
                entry.Item.Item,
                entry.Quantity,
                currentView.Shop);

            if (!result.Success)
                Debug.LogWarning($"[ShopPanelUI] Sell failed for '{entry.Item.NameKey}': {result.FailReason}.");
        }

        sellCart.Clear();
        ShopService.Open(currentView.Customer, currentView.Merchant, currentView.Shop);
    }

    private void Show()
    {
        ResolveUIController();
        _uiController?.OpenExternalExclusiveWindow(ExternalWindowId);

        if (panel != null) panel.SetActive(true);
        else gameObject.SetActive(true);
    }

    private void Hide()
    {
        ClearSpawned();
        ClearSellCartRows();
        sellGridView?.Clear();
        currentView = null;
        selectedSellItem = null;
        sellCart.Clear();

        if (panel != null) panel.SetActive(false);
        else gameObject.SetActive(false);

        _uiController?.CloseExternalExclusiveWindow(ExternalWindowId);
    }

    private void ResolveUIController()
    {
        if (_uiController != null) return;
        _uiController = GetComponentInParent<UIController>(true);
        if (_uiController == null)
            _uiController = FindAnyObjectByType<UIController>(FindObjectsInactive.Include);
    }

    private void SetTitle()
    {
        if (titleText == null || currentView == null) return;

        string merchantName = Localize(currentView.Merchant?.entityData?.keyName);
        titleText.text = string.Format(Localize("ui.shop.title"), merchantName);
    }

    private void SetMoney(int money)
    {
        if (moneyText != null)
            moneyText.text = money.ToString("N0");
    }

    private void ClearSpawned()
    {
        foreach (var item in spawnedObjects)
        {
            if (item != null) Destroy(item);
        }
        spawnedObjects.Clear();
    }

    private void TrySubscribe()
    {
        if (subscribedBus != null) return;

        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;

        bus.Subscribe<ShopViewPublish>(OnShopView);
        subscribedBus = bus;
        Debug.Log("[ShopPanelUI] Subscribed to ShopViewPublish.");
    }

    private void EnsureSellGridView()
    {
        if (sellGridView != null) return;

        sellGridView = sellInventoryGridRoot.GetComponent<InventoryGridView>();
        if (sellGridView == null)
            sellGridView = sellInventoryGridRoot.gameObject.AddComponent<InventoryGridView>();

        sellGridView.Configure(
            sellInventoryGridRoot,
            sellSlotTemplate,
            visibleSellSlots,
            showOneAmount: false);
        sellGridView.SetClickHandler((_, gridItem) =>
        {
            selectedSellItem = gridItem.Payload as ShopItemViewData;
            AddSelectedSellQuantity(1);
            RefreshSellSelection();
        });
    }

    private void IncreaseSelectedSellQuantity()
    {
        AddSelectedSellQuantity(1);
    }

    private void DecreaseSelectedSellQuantity()
    {
        if (selectedSellItem?.Item == null) return;
        int quantity = GetCartQuantity(selectedSellItem);
        SetSellCartQuantity(selectedSellItem, quantity - 1);
        RefreshSellSelection();
    }

    private void MaxSelectedSellQuantity()
    {
        if (selectedSellItem?.Item == null) return;
        SetSellCartQuantity(selectedSellItem, selectedSellItem.Amount);
        RefreshSellSelection();
    }

    private void ClearSellCart()
    {
        sellCart.Clear();
        RefreshSellSelection();
    }

    private void AddSelectedSellQuantity(int amount)
    {
        if (selectedSellItem?.Item == null || !selectedSellItem.Sellable)
            return;

        int currentQuantity = GetCartQuantity(selectedSellItem);
        SetSellCartQuantity(selectedSellItem, currentQuantity + amount);
    }

    private void SetSellCartQuantity(ShopItemViewData item, int quantity)
    {
        if (item?.Item == null)
            return;

        quantity = Mathf.Clamp(quantity, 0, item.Amount);
        if (quantity <= 0)
        {
            sellCart.Remove(item.Item);
            return;
        }

        sellCart[item.Item] = new SellCartEntry(item, quantity);
    }

    private int GetCartQuantity(ShopItemViewData item)
    {
        if (item?.Item == null)
            return 0;

        return sellCart.TryGetValue(item.Item, out var entry) ? entry.Quantity : 0;
    }

    private int GetSellCartTotal()
    {
        int total = 0;
        foreach (var entry in sellCart.Values)
        {
            if (entry.Item == null || entry.Quantity <= 0)
                continue;

            total += Mathf.Max(0, entry.Item.SellPrice) * entry.Quantity;
        }

        return total;
    }

    private int GetSellCartItemCount()
    {
        int count = 0;
        foreach (var entry in sellCart.Values)
            count += Mathf.Max(0, entry.Quantity);

        return count;
    }

    private void EnsureSellCartControls()
    {
        if (selectedSellTotalText == null)
            return;

        var parent = selectedSellTotalText.transform.parent;
        if (parent == null)
            return;

        var controlsRoot = parent.Find("SellQuantityControls");
        if (controlsRoot != null)
            controlsRoot.gameObject.SetActive(false);
    }

    private void EnsureSellBasketLayout()
    {
        if (sellPage == null || sellCartListRoot != null)
            return;

        var targetPanel = sellPage.transform.Find("SellTargetPanel");
        if (targetPanel == null)
            return;

        var arrow = sellPage.transform.Find("SellArrowText");
        if (arrow != null)
            arrow.gameObject.SetActive(false);

        var selectedArea = targetPanel.Find("SelectedSellArea");
        if (selectedArea != null)
            selectedArea.gameObject.SetActive(false);

        var totalPanel = selectedSellTotalText != null ? selectedSellTotalText.transform.parent as RectTransform : null;
        if (totalPanel != null)
        {
            totalPanel.anchorMin = new Vector2(0f, 0f);
            totalPanel.anchorMax = new Vector2(1f, 0f);
            totalPanel.pivot = new Vector2(0.5f, 0f);
            totalPanel.anchoredPosition = new Vector2(0f, 18f);
            totalPanel.sizeDelta = new Vector2(-40f, 108f);
        }

        var basketArea = targetPanel.Find("SellBasketArea");
        if (basketArea == null)
        {
            var basketObject = CreateChild("SellBasketArea", targetPanel);
            basketArea = basketObject.transform;
            var basketRect = basketObject.GetComponent<RectTransform>();
            basketRect.anchorMin = new Vector2(0f, 0f);
            basketRect.anchorMax = new Vector2(1f, 1f);
            basketRect.pivot = new Vector2(0.5f, 0.5f);
            basketRect.offsetMin = new Vector2(20f, 142f);
            basketRect.offsetMax = new Vector2(-20f, -64f);

            var basketImage = GetOrAdd<Image>(basketObject);
            basketImage.color = new Color(0.78f, 0.53f, 0.26f, 0.26f);
        }

        var scrollRoot = basketArea.Find("SellCartScrollView");
        if (scrollRoot == null)
            scrollRoot = CreateSellCartScrollView(basketArea);

        sellCartListRoot = scrollRoot.Find("Viewport/Content");
        if (sellCartListRoot == null)
            return;

        sellCartEmptyText = FindText(sellCartListRoot, "SellCartEmptyText");
        if (sellCartEmptyText == null)
        {
            sellCartEmptyText = CreateTemplateLabel("SellCartEmptyText", sellCartListRoot, 21, FontStyles.Normal);
            sellCartEmptyText.alignment = TextAlignmentOptions.Center;
            sellCartEmptyText.enableWordWrapping = true;
            SetLayoutSize(sellCartEmptyText.gameObject, 0f, 78f);
            SetLocalizedText(sellCartEmptyText, "ui.shop.sell_empty_hint");
        }
    }

    private Transform CreateSellCartScrollView(Transform parent)
    {
        var scrollObject = CreateChild("SellCartScrollView", parent);
        SetAnchorStretch(scrollObject.GetComponent<RectTransform>());

        var scrollRect = GetOrAdd<ScrollRect>(scrollObject);
        scrollRect.horizontal = false;

        var viewportObject = CreateChild("Viewport", scrollObject.transform);
        var viewportRect = viewportObject.GetComponent<RectTransform>();
        SetAnchorStretch(viewportRect);
        viewportRect.offsetMin = new Vector2(10f, 10f);
        viewportRect.offsetMax = new Vector2(-10f, -10f);
        GetOrAdd<RectMask2D>(viewportObject);
        var viewportImage = GetOrAdd<Image>(viewportObject);
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);

        var contentObject = CreateChild("Content", viewportObject.transform);
        var contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        var layout = GetOrAdd<VerticalLayoutGroup>(contentObject);
        layout.padding = new RectOffset(0, 8, 0, 0);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = GetOrAdd<ContentSizeFitter>(contentObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        return scrollObject.transform;
    }

    private void RebuildSellCartRows()
    {
        EnsureSellBasketLayout();
        ClearSellCartRows();

        if (sellCartListRoot == null)
            return;

        bool hasEntries = sellCart.Count > 0;
        if (sellCartEmptyText != null)
            sellCartEmptyText.gameObject.SetActive(!hasEntries);

        if (!hasEntries)
        {
            ForceRebuild(sellCartListRoot);
            return;
        }

        foreach (var entry in sellCart.Values)
        {
            if (entry.Item?.ItemData == null || entry.Quantity <= 0)
                continue;

            spawnedSellCartRows.Add(CreateSellCartRow(entry));
        }

        ForceRebuild(sellCartListRoot);
    }

    private GameObject CreateSellCartRow(SellCartEntry entry)
    {
        var row = CreateChild("SellCartRow", sellCartListRoot);
        var background = GetOrAdd<Image>(row);
        background.color = new Color(0.95f, 0.75f, 0.42f, 0.62f);
        SetLayoutSize(row, 0f, 72f);

        var layout = GetOrAdd<HorizontalLayoutGroup>(row);
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var iconFrame = CreateChild("IconFrame", row.transform);
        var iconFrameImage = GetOrAdd<Image>(iconFrame);
        iconFrameImage.color = new Color(0.35f, 0.2f, 0.08f, 0.82f);
        SetLayoutSize(iconFrame, 52f, 52f);
        var icon = CreateStretchImage("Icon", iconFrame.transform, new Vector2(5f, 5f));
        icon.preserveAspect = true;
        SetIcon(icon, entry.Item.ItemData);
        icon.enabled = entry.Item.ItemData.icon != null;

        var itemInfo = CreateChild("ItemInfo", row.transform);
        var itemInfoLayout = GetOrAdd<VerticalLayoutGroup>(itemInfo);
        itemInfoLayout.spacing = 1f;
        itemInfoLayout.childAlignment = TextAnchor.MiddleLeft;
        itemInfoLayout.childControlWidth = true;
        itemInfoLayout.childControlHeight = true;
        itemInfoLayout.childForceExpandWidth = true;
        itemInfoLayout.childForceExpandHeight = false;
        var itemInfoSize = GetOrAdd<LayoutElement>(itemInfo);
        itemInfoSize.minWidth = 126f;
        itemInfoSize.flexibleWidth = 1f;

        var nameText = CreateTemplateLabel("NameText", itemInfo.transform, 18, FontStyles.Bold);
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        SetLocalizedText(nameText, entry.Item.NameKey);

        var priceText = CreateTemplateLabel("PriceText", itemInfo.transform, 15, FontStyles.Normal);
        priceText.overflowMode = TextOverflowModes.Ellipsis;
        SetPlainText(priceText, $"{Mathf.Max(0, entry.Item.SellPrice):N0} x {entry.Quantity} = {Mathf.Max(0, entry.Item.SellPrice) * entry.Quantity:N0}");

        var minusButton = CreateCartButton("MinusButton", row.transform, "-");
        minusButton.onClick.AddListener(() =>
        {
            SetSellCartQuantity(entry.Item, GetCartQuantity(entry.Item) - 1);
            RefreshSellSelection();
        });

        var quantityText = CreateTemplateLabel("QuantityText", row.transform, 18, FontStyles.Bold);
        quantityText.alignment = TextAlignmentOptions.Center;
        SetLayoutSize(quantityText.gameObject, 34f, 38f);
        SetPlainText(quantityText, entry.Quantity.ToString());

        var plusButton = CreateCartButton("PlusButton", row.transform, "+");
        plusButton.interactable = entry.Quantity < entry.Item.Amount;
        plusButton.onClick.AddListener(() =>
        {
            SetSellCartQuantity(entry.Item, GetCartQuantity(entry.Item) + 1);
            RefreshSellSelection();
        });

        var removeButton = CreateCartButton("RemoveButton", row.transform, "X");
        removeButton.onClick.AddListener(() =>
        {
            SetSellCartQuantity(entry.Item, 0);
            RefreshSellSelection();
        });

        return row;
    }

    private void ClearSellCartRows()
    {
        foreach (var row in spawnedSellCartRows)
        {
            if (row != null)
                Destroy(row);
        }

        spawnedSellCartRows.Clear();
    }



    private void EnsureSellSlotTemplate()
    {
        if (sellSlotTemplate != null)
            return;

        sellSlotTemplate = FindButton(transform, "ShopSellSlotTemplate");
        if (sellSlotTemplate != null)
            return;

        if (sellInventoryGridRoot == null)
            return;

        sellSlotTemplate = CreateFallbackSellSlotTemplate(sellInventoryGridRoot);
    }



    private static Button CreateFallbackSellSlotTemplate(Transform parent)
    {
        var root = CreateTemplateButton("ShopSellSlotTemplate", parent);
        root.gameObject.SetActive(false);
        SetButtonSize(root, 76f, 76f);

        var icon = CreateStretchImage("Icon", root.transform, new Vector2(8f, 8f));
        icon.preserveAspect = true;

        var amount = CreateTemplateLabel("AmountText", root.transform, 18, FontStyles.Bold);
        SetAnchorStretch(amount.rectTransform);
        amount.rectTransform.offsetMin = new Vector2(4f, 4f);
        amount.rectTransform.offsetMax = new Vector2(-6f, -4f);
        amount.alignment = TextAlignmentOptions.BottomRight;

        return root;
    }

    private static Button CreateTemplateButton(string name, Transform parent)
    {
        var go = CreateChild(name, parent);
        var image = GetOrAdd<Image>(go);
        image.color = new Color(0.35f, 0.2f, 0.08f, 0.92f);

        var button = GetOrAdd<Button>(go);
        button.targetGraphic = image;
        return button;
    }

    private static Button CreateCartButton(string name, Transform parent, string label)
    {
        var button = CreateTemplateButton(name, parent);
        SetButtonSize(button, label.Length > 1 ? 70f : 44f, 38f);

        var text = CreateTemplateLabel("Label", button.transform, 18, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.96f, 0.84f, 0.62f, 1f);
        text.text = label;
        return button;
    }

    private static TMP_Text CreateTemplateLabel(string name, Transform parent, float size, FontStyles style)
    {
        var go = CreateChild(name, parent);
        var text = GetOrAdd<TextMeshProUGUI>(go);
        text.text = string.Empty;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = new Color(0.2f, 0.1f, 0.03f, 1f);
        text.enableWordWrapping = false;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        SetAnchorStretch(text.rectTransform);

        var layout = GetOrAdd<LayoutElement>(go);
        layout.minHeight = size + 6f;
        layout.preferredHeight = size + 6f;
        layout.flexibleWidth = 1f;

        return text;
    }

    private static Image CreateStretchImage(string name, Transform parent, Vector2 padding)
    {
        var go = CreateChild(name, parent);
        var image = GetOrAdd<Image>(go);
        var rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = padding;
        rect.offsetMax = -padding;
        rect.sizeDelta = Vector2.zero;
        return image;
    }

    private static GameObject CreateChild(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void SetButtonSize(Button button, float width, float height)
    {
        if (button == null) return;

        SetLayoutSize(button.gameObject, width, height);

        var rect = button.GetComponent<RectTransform>();
        if (rect != null)
            rect.sizeDelta = new Vector2(width, height);
    }

    private static void SetLayoutSize(GameObject go, float width, float height)
    {
        if (go == null) return;

        var layout = GetOrAdd<LayoutElement>(go);
        layout.minWidth = width;
        layout.minHeight = height;
        layout.preferredWidth = width;
        layout.preferredHeight = height;
    }

    private static void SetAnchorStretch(RectTransform rect)
    {
        if (rect == null) return;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    private static void SetIcon(Image image, EntityData itemData)
    {
        if (image == null) return;

        image.sprite = itemData?.icon;
        image.enabled = image.sprite != null;
        image.preserveAspect = true;
    }

    private static void SetLocalizedText(TMP_Text text, string key)
    {
        if (text == null) return;

        if (string.IsNullOrWhiteSpace(key))
        {
            text.text = string.Empty;
            return;
        }

        var localized = text.GetComponent<LocalizedText>();
        if (localized == null)
            localized = text.gameObject.AddComponent<LocalizedText>();

        localized.SetKey(key);
    }

    private static void SetPlainText(TMP_Text text, string value)
    {
        if (text == null) return;
        text.text = value ?? string.Empty;
    }

    private static string Localize(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;
        return LocalizationManager.Instance != null ? LocalizationManager.Instance.GetText(key) : key;
    }

    private static TMP_Text FindText(Transform root, string name)
    {
        var child = FindDeepChild(root, name);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static Image FindImage(Transform root, string name)
    {
        var child = FindDeepChild(root, name);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private static Button FindButton(Transform root, string name)
    {
        var child = FindDeepChild(root, name);
        return child != null ? child.GetComponent<Button>() : null;
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

    private static void ForceRebuild(Transform root)
    {
        if (root is not RectTransform rect) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    private readonly struct SellCartEntry
    {
        public readonly ShopItemViewData Item;
        public readonly int Quantity;

        public SellCartEntry(ShopItemViewData item, int quantity)
        {
            Item = item;
            Quantity = quantity;
        }
    }
}
