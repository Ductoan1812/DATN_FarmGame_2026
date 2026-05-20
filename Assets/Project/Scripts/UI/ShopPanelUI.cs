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

    private readonly List<GameObject> spawnedObjects = new();
    private readonly Dictionary<EntityRuntime, SellCartEntry> sellCart = new();
    private InventoryGridView sellGridView;
    private EventBus subscribedBus;
    private bool listenersRegistered;
    private ShopViewData currentView;
    private ShopItemViewData selectedSellItem;

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
            SetIcon(selectedSellIcon, selectedSellItem?.ItemData);
            selectedSellIcon.enabled = hasSelection && selectedSellItem.ItemData.icon != null;
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
        if (panel != null) panel.SetActive(true);
        else gameObject.SetActive(true);
    }

    private void Hide()
    {
        ClearSpawned();
        sellGridView?.Clear();
        currentView = null;
        selectedSellItem = null;
        sellCart.Clear();

        if (panel != null) panel.SetActive(false);
        else gameObject.SetActive(false);
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
        if (controlsRoot == null)
        {
            var controlsObject = CreateChild("SellQuantityControls", parent);
            controlsRoot = controlsObject.transform;

            var rect = controlsObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.32f, 0f);
            rect.anchorMax = new Vector2(0.78f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(0f, 10f);
            rect.offsetMax = new Vector2(-12f, -10f);

            var layout = GetOrAdd<HorizontalLayoutGroup>(controlsObject);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        selectedSellQuantityText ??= FindText(controlsRoot, "SelectedSellQuantityText");
        sellCartSummaryText ??= FindText(controlsRoot, "SellCartSummaryText");
        sellMinusButton ??= FindButton(controlsRoot, "SellMinusButton");
        sellPlusButton ??= FindButton(controlsRoot, "SellPlusButton");
        sellMaxButton ??= FindButton(controlsRoot, "SellMaxButton");
        sellClearButton ??= FindButton(controlsRoot, "SellClearButton");

        if (selectedSellQuantityText == null)
        {
            selectedSellQuantityText = CreateTemplateLabel("SelectedSellQuantityText", controlsRoot, 22, FontStyles.Bold);
            SetLayoutSize(selectedSellQuantityText.gameObject, 58f, 36f);
        }
        if (sellCartSummaryText == null)
        {
            sellCartSummaryText = CreateTemplateLabel("SellCartSummaryText", controlsRoot, 18, FontStyles.Normal);
            SetLayoutSize(sellCartSummaryText.gameObject, 100f, 36f);
        }
        if (sellMinusButton == null)
            sellMinusButton = CreateCartButton("SellMinusButton", controlsRoot, "-");
        if (sellPlusButton == null)
            sellPlusButton = CreateCartButton("SellPlusButton", controlsRoot, "+");
        if (sellMaxButton == null)
            sellMaxButton = CreateCartButton("SellMaxButton", controlsRoot, "Max");
        if (sellClearButton == null)
            sellClearButton = CreateCartButton("SellClearButton", controlsRoot, "Clear");
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

        var icon = CreateStretchImage("Icon", root.transform, Vector2.zero);
        icon.rectTransform.offsetMin = new Vector2(8f, 8f);
        icon.rectTransform.offsetMax = new Vector2(-8f, -8f);
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

    private static Image CreateStretchImage(string name, Transform parent, Vector2 size)
    {
        var go = CreateChild(name, parent);
        var image = GetOrAdd<Image>(go);
        var rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
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

        image.sprite = itemData != null ? itemData.icon : null;
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
