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
    [SerializeField] private Button buyRowTemplate;

    [Header("Sell Page")]
    [SerializeField] private Transform sellInventoryGridRoot;
    [SerializeField] private Button sellSlotTemplate;
    [SerializeField] private Image selectedSellIcon;
    [SerializeField] private TMP_Text selectedSellNameText;
    [SerializeField] private TMP_Text selectedSellHintText;
    [SerializeField] private TMP_Text selectedSellTotalText;
    [SerializeField] private Button sellButton;
    [SerializeField] private int visibleSellSlots = 30;

    private readonly List<GameObject> spawnedObjects = new();
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
        if (sellButton != null) sellButton.onClick.AddListener(SellSelectedItem);

        listenersRegistered = true;
    }

    private void UnregisterListeners()
    {
        if (!listenersRegistered) return;

        if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
        if (buyTabButton != null) buyTabButton.onClick.RemoveListener(ShowBuyPage);
        if (sellTabButton != null) sellTabButton.onClick.RemoveListener(ShowSellPage);
        if (sellButton != null) sellButton.onClick.RemoveListener(SellSelectedItem);

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
        RebuildSellGrid();
        RefreshSellSelection();
    }

    private void RebuildBuyList()
    {
        ClearSpawned();
        if (buyListRoot == null || currentView?.StockItems == null) return;

        EnsureBuyRowTemplate();
        if (buyRowTemplate == null) return;

        foreach (var item in currentView.StockItems)
        {
            if (item == null || !item.Buyable) continue;

            var row = Instantiate(buyRowTemplate, buyListRoot);
            row.gameObject.SetActive(true);
            spawnedObjects.Add(row.gameObject);

            BindBuyRow(row.gameObject, item);
        }

        ForceRebuild(buyListRoot);
    }

    private void BindBuyRow(GameObject row, ShopItemViewData item)
    {
        int quantity = 1;
        int maxQuantity = item.InfiniteStock
            ? InfiniteStockMaxQuantity
            : Mathf.Max(1, item.Amount);

        var icon = FindImage(row.transform, "Icon");
        var nameText = FindText(row.transform, "NameText");
        var descriptionText = FindText(row.transform, "DescriptionText");
        var priceText = FindText(row.transform, "PriceText");
        var amountText = FindText(row.transform, "AmountText");
        var minusButton = FindButton(row.transform, "MinusButton");
        var plusButton = FindButton(row.transform, "PlusButton");
        var actionButton = FindButton(row.transform, "BuyButton");

        SetIcon(icon, item.ItemData);
        SetLocalizedText(nameText, item.NameKey);
        SetLocalizedText(descriptionText, item.ItemData != null ? item.ItemData.descKey : string.Empty);

        void Refresh()
        {
            SetPlainText(priceText, (item.BuyPrice * quantity).ToString());
            SetPlainText(amountText, quantity.ToString());
        }

        if (minusButton != null)
            minusButton.onClick.AddListener(() =>
            {
                quantity = Mathf.Max(1, quantity - 1);
                Refresh();
            });

        if (plusButton != null)
            plusButton.onClick.AddListener(() =>
            {
                quantity = Mathf.Min(maxQuantity, quantity + 1);
                Refresh();
            });

        if (actionButton != null)
            actionButton.onClick.AddListener(() =>
            {
                var result = ShopService.TryBuy(currentView.Customer, currentView.Merchant, item, quantity);
                if (result.Success)
                    ShopService.Open(currentView.Customer, currentView.Merchant, currentView.Shop);
                else
                    Debug.LogWarning($"[ShopPanelUI] Buy failed: {result.FailReason}.");
            });

        Refresh();
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

        SetPlainText(selectedSellTotalText, hasSelection ? selectedSellItem.SellPrice.ToString() : "0");

        if (sellButton != null)
            sellButton.interactable = canSellSelection;
    }

    private void SellSelectedItem()
    {
        if (currentView == null || selectedSellItem?.Item == null) return;
        if (!selectedSellItem.Sellable) return;

        var result = ShopService.TrySell(
            currentView.Customer,
            currentView.Merchant,
            selectedSellItem.Item,
            1,
            currentView.Shop);

        if (result.Success)
            ShopService.Open(currentView.Customer, currentView.Merchant, currentView.Shop);
        else
            Debug.LogWarning($"[ShopPanelUI] Sell failed: {result.FailReason}.");
    }

    private void Show()
    {
        if (TryOpenViaRoot("shop")) return;

        if (panel != null) panel.SetActive(true);
        else gameObject.SetActive(true);
        UIRootController.Instance?.NotifyWindowStateChanged();
    }

    private void Hide()
    {
        ClearSpawned();
        sellGridView?.Clear();
        currentView = null;
        selectedSellItem = null;
        if (TryCloseViaRoot("shop")) return;

        if (panel != null) panel.SetActive(false);
        else gameObject.SetActive(false);
        UIRootController.Instance?.NotifyWindowStateChanged();
    }

    private bool TryOpenViaRoot(string id)
    {
        var root = UIRootController.Instance;
        if (root == null || !root.TryGetEntry(id, out _)) return false;

        root.Open(id);
        return true;
    }

    private bool TryCloseViaRoot(string id)
    {
        var root = UIRootController.Instance;
        if (root == null || !root.TryGetEntry(id, out _)) return false;

        root.Close(id);
        return true;
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
            RefreshSellSelection();
        });
    }

    private void EnsureBuyRowTemplate()
    {
        if (buyRowTemplate != null)
            return;

        buyRowTemplate = FindButton(transform, "ShopBuyRowTemplate");
        if (buyRowTemplate != null)
            return;

        if (buyListRoot == null)
            return;

        buyRowTemplate = CreateFallbackBuyRowTemplate(buyListRoot);
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

    private static Button CreateFallbackBuyRowTemplate(Transform parent)
    {
        var root = CreateTemplateButton("ShopBuyRowTemplate", parent);
        root.gameObject.SetActive(false);

        var rootLayout = GetOrAdd<HorizontalLayoutGroup>(root.gameObject);
        rootLayout.spacing = 10f;
        rootLayout.padding = new RectOffset(12, 12, 8, 8);
        rootLayout.childAlignment = TextAnchor.MiddleLeft;
        rootLayout.childControlWidth = false;
        rootLayout.childControlHeight = false;
        rootLayout.childForceExpandWidth = false;
        rootLayout.childForceExpandHeight = false;

        var rootSize = GetOrAdd<LayoutElement>(root.gameObject);
        rootSize.minHeight = 86f;
        rootSize.preferredHeight = 86f;
        rootSize.flexibleWidth = 1f;

        var icon = CreateStretchImage("Icon", root.transform, new Vector2(66f, 66f));
        icon.preserveAspect = true;

        var textBlock = CreateChild("TextBlock", root.transform);
        var textLayout = GetOrAdd<VerticalLayoutGroup>(textBlock);
        textLayout.spacing = 4f;
        textLayout.childAlignment = TextAnchor.MiddleLeft;
        textLayout.childControlWidth = true;
        textLayout.childControlHeight = false;
        textLayout.childForceExpandWidth = true;
        textLayout.childForceExpandHeight = false;
        var textSize = GetOrAdd<LayoutElement>(textBlock);
        textSize.minWidth = 250f;
        textSize.flexibleWidth = 1f;

        CreateTemplateLabel("NameText", textBlock.transform, 28, FontStyles.Bold);
        CreateTemplateLabel("DescriptionText", textBlock.transform, 22, FontStyles.Normal);

        var priceText = CreateTemplateLabel("PriceText", root.transform, 24, FontStyles.Normal);
        var priceSize = GetOrAdd<LayoutElement>(priceText.gameObject);
        priceSize.minWidth = 100f;
        priceSize.preferredWidth = 100f;

        var minus = CreateTemplateButton("MinusButton", root.transform);
        CreateTemplateLabel("Label", minus.transform, 24, FontStyles.Bold).text = "-";
        SetButtonSize(minus, 48f, 48f);

        var amount = CreateTemplateLabel("AmountText", root.transform, 24, FontStyles.Bold);
        var amountSize = GetOrAdd<LayoutElement>(amount.gameObject);
        amountSize.minWidth = 42f;
        amountSize.preferredWidth = 42f;
        amount.text = "1";

        var plus = CreateTemplateButton("PlusButton", root.transform);
        CreateTemplateLabel("Label", plus.transform, 24, FontStyles.Bold).text = "+";
        SetButtonSize(plus, 48f, 48f);

        var buyButton = CreateTemplateButton("BuyButton", root.transform);
        CreateTemplateLabel("Label", buyButton.transform, 24, FontStyles.Bold).text = "Mua";
        SetButtonSize(buyButton, 124f, 56f);

        return root;
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

        var layout = GetOrAdd<LayoutElement>(button.gameObject);
        layout.minWidth = width;
        layout.minHeight = height;
        layout.preferredWidth = width;
        layout.preferredHeight = height;

        var rect = button.GetComponent<RectTransform>();
        if (rect != null)
            rect.sizeDelta = new Vector2(width, height);
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
}
