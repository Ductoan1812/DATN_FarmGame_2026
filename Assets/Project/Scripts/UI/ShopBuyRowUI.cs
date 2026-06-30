using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopBuyRowUI : MonoBehaviour
{
    private const int InfiniteStockMaxQuantity = 99;

    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private TMP_Text requiredLevelText;
    [SerializeField] private Button minusButton;
    [SerializeField] private Button plusButton;
    [SerializeField] private Button actionButton;

    [Header("Sprites")]
    [SerializeField] private Sprite lockedItemIcon;
    [SerializeField] private Sprite defaultButtonSprite;
    [SerializeField] private Sprite lockedButtonSprite;

    private static Sprite generatedLockedIcon;
    private ShopItemViewData currentItem;
    private int currentCustomerMoney = 0;
    private int quantity = 1;
    private int maxQuantity = 1;
    private Action<ShopItemViewData, int> onBuyAction;

    public void Init(ShopItemViewData item, int customerMoney, Action<ShopItemViewData, int> onBuyCallback)
    {
        currentItem = item;
        currentCustomerMoney = customerMoney;
        onBuyAction = onBuyCallback;
        
        quantity = 1;
        maxQuantity = item.InfiniteStock
            ? InfiniteStockMaxQuantity
            : Mathf.Max(1, item.Amount);

        EnsureRequiredLevelText();
        if (item.Unlocked)
        {
            SetIcon(item.ItemData);
            SetLocalizedText(nameText, item.NameKey);
            SetLocalizedText(descriptionText, item.ItemData != null ? item.ItemData.descKey : string.Empty);
            SetPlainText(requiredLevelText, string.Empty);
        }
        else
        {
            SetIcon(GetLockedIcon());
            SetLocalizedText(nameText, "ui.crafting.locked");
            SetPlainText(descriptionText, GetRequiredLevelLabel(item));
            SetPlainText(requiredLevelText, $"Lv.{Mathf.Max(1, item.RequiredLevel)}");
        }

        if (minusButton != null)
        {
            minusButton.onClick.RemoveAllListeners();
            minusButton.onClick.AddListener(OnMinusClicked);
        }

        if (plusButton != null)
        {
            plusButton.onClick.RemoveAllListeners();
            plusButton.onClick.AddListener(OnPlusClicked);
        }

        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnBuyClicked);
        }

        Refresh();
    }

    private void OnMinusClicked()
    {
        quantity = Mathf.Max(1, quantity - 1);
        Refresh();
    }

    private void OnPlusClicked()
    {
        quantity = Mathf.Min(maxQuantity, quantity + 1);
        Refresh();
    }

    private void OnBuyClicked()
    {
        if (currentItem != null && onBuyAction != null)
        {
            onBuyAction.Invoke(currentItem, quantity);
        }
    }

    private void Refresh()
    {
        if (currentItem == null) return;
        
        int totalPrice = currentItem.BuyPrice * quantity;
        bool locked = !currentItem.Unlocked;
        SetPlainText(priceText, locked ? GetRequiredLevelLabel(currentItem) : totalPrice.ToString());
        SetPlainText(amountText, locked ? string.Empty : quantity.ToString());

        bool canAfford = currentCustomerMoney >= totalPrice;
        bool canBuy = currentItem.Unlocked && currentItem.Buyable && canAfford;

        if (minusButton != null)
            minusButton.interactable = currentItem.Unlocked && quantity > 1;

        if (plusButton != null)
            plusButton.interactable = currentItem.Unlocked && quantity < maxQuantity;

        if (actionButton != null)
        {
            actionButton.interactable = canBuy;
            
            Image btnImage = actionButton.GetComponent<Image>();
            if (btnImage != null)
            {
                if (canBuy)
                {
                    if (defaultButtonSprite != null) btnImage.sprite = defaultButtonSprite;
                }
                else
                {
                    if (lockedButtonSprite != null) btnImage.sprite = lockedButtonSprite;
                }
            }
        }
    }

    private void SetIcon(EntityData itemData)
    {
        SetIcon(itemData != null ? itemData.icon : null);
    }

    private void SetIcon(Sprite sprite)
    {
        if (icon == null) return;
        icon.sprite = sprite;
        icon.enabled = icon.sprite != null;
        icon.preserveAspect = true;
    }

    private Sprite GetLockedIcon()
    {
        if (lockedItemIcon != null)
            return lockedItemIcon;

        if (generatedLockedIcon != null)
            return generatedLockedIcon;

        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var clear = new Color(0f, 0f, 0f, 0f);
        var gold = new Color(0.95f, 0.73f, 0.22f, 1f);
        var dark = new Color(0.28f, 0.16f, 0.05f, 1f);
        var pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = clear;

        void FillRect(int xMin, int yMin, int width, int height, Color color)
        {
            for (int y = yMin; y < yMin + height; y++)
            {
                for (int x = xMin; x < xMin + width; x++)
                {
                    if (x >= 0 && x < size && y >= 0 && y < size)
                        pixels[y * size + x] = color;
                }
            }
        }

        FillRect(8, 7, 16, 15, dark);
        FillRect(9, 8, 14, 13, gold);
        FillRect(14, 12, 4, 6, dark);
        FillRect(11, 21, 3, 4, gold);
        FillRect(18, 21, 3, 4, gold);
        FillRect(11, 25, 10, 3, gold);
        FillRect(9, 22, 3, 5, dark);
        FillRect(20, 22, 3, 5, dark);
        FillRect(12, 27, 8, 2, dark);

        texture.SetPixels(pixels);
        texture.Apply();
        generatedLockedIcon = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        generatedLockedIcon.name = "GeneratedShopLockIcon";
        return generatedLockedIcon;
    }

    private void EnsureRequiredLevelText()
    {
        if (requiredLevelText != null)
            return;

        requiredLevelText = FindText(transform, "RequiredLevelText") ?? FindText(transform, "LevelText");
        if (requiredLevelText != null || icon == null)
            return;

        var parent = icon.transform.parent != null ? icon.transform.parent : icon.transform;
        var go = new GameObject("RequiredLevelText", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        requiredLevelText = go.AddComponent<TextMeshProUGUI>();
        requiredLevelText.fontSize = 18f;
        requiredLevelText.fontStyle = FontStyles.Bold;
        requiredLevelText.alignment = TextAlignmentOptions.Center;
        requiredLevelText.color = new Color(1f, 0.87f, 0.38f, 1f);
        requiredLevelText.enableWordWrapping = false;

        var rect = requiredLevelText.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 2f);
        rect.sizeDelta = new Vector2(0f, 22f);
    }

    private static TMP_Text FindText(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root.GetComponent<TMP_Text>();

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindText(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    private static string GetRequiredLevelLabel(ShopItemViewData item)
    {
        int requiredLevel = Mathf.Max(1, item?.RequiredLevel ?? 1);
        return LocalizationManager.Instance != null
            ? LocalizationManager.Instance.GetText("ui.unlock.level_required", requiredLevel)
            : $"Requires Level {requiredLevel}";
    }

    private void SetLocalizedText(TMP_Text text, string key)
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

    private void SetPlainText(TMP_Text text, string value)
    {
        if (text == null) return;
        var localized = text.GetComponent<LocalizedText>();
        if (localized != null)
            localized.SetKey(string.Empty);

        text.text = value ?? string.Empty;
    }
}
