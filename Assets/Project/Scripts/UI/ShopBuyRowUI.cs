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
    [SerializeField] private Button minusButton;
    [SerializeField] private Button plusButton;
    [SerializeField] private Button actionButton;

    [Header("Sprites")]
    [SerializeField] private Sprite defaultButtonSprite;
    [SerializeField] private Sprite lockedButtonSprite;

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

        SetIcon(item.ItemData);
        SetLocalizedText(nameText, item.NameKey);
        SetLocalizedText(descriptionText, item.ItemData != null ? item.ItemData.descKey : string.Empty);

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
        SetPlainText(priceText, totalPrice.ToString());
        SetPlainText(amountText, quantity.ToString());
        
        bool canAfford = currentCustomerMoney >= totalPrice;
        
        if (actionButton != null)
        {
            actionButton.interactable = canAfford;
            
            Image btnImage = actionButton.GetComponent<Image>();
            if (btnImage != null)
            {
                if (canAfford)
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
        if (icon == null) return;
        icon.sprite = itemData != null ? itemData.icon : null;
        icon.enabled = icon.sprite != null;
        icon.preserveAspect = true;
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
        text.text = value ?? string.Empty;
    }
}
