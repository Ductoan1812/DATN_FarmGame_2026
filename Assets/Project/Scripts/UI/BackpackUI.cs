using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FE Backpack — gắn vào UIManager (luôn active, không bao giờ miss event).
/// Cache data từ BackpackSlotChangedPublish và render info panel từ BackpackItemInfoChangedPublish.
/// </summary>
public class BackpackUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private Button btnSort;
    [SerializeField] private Image infoIcon;
    [SerializeField] private TMP_Text infoAmountText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private TMP_Text categoryText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private GameObject rareObject;
    [SerializeField] private Button btnUse;
    [SerializeField] private Button btnSeparate;
    [SerializeField] private TMP_InputField splitInput;
    [SerializeField] private Button btnDrop;
    [SerializeField] private StatListUI statListUI;

    private const int MaxDisplayAmount = 99999;

    private struct SlotData
    {
        public Sprite icon;
        public int amount;
    }

    private SlotData[] _cache;
    private SlotView[] _views;
    private LocalizedText _nameLocalized;
    private LocalizedText _descLocalized;
    private LocalizedText _categoryLocalized;
    private bool _viewsReady;
    private bool _wasActive;
    private bool _dirty;
    private bool _subscribed;
    private int _selectedIndex = -1;

    private void Start()
    {
        CacheInfoRefs();
        TrySubscribe();
        ClearInfoPanel();
    }

    private void OnEnable()
    {
        TrySubscribe();

        if (btnSort != null)
            btnSort.onClick.AddListener(OnSortClicked);
        if (btnSeparate != null)
            btnSeparate.onClick.AddListener(OnSeparateClicked);
        if (btnDrop != null)
            btnDrop.onClick.AddListener(OnDropClicked);
    }

    private void OnDisable()
    {
        if (_subscribed)
        {
            var bus = GameManager.Instance?.EventBus;
            if (bus != null)
            {
                bus.Unsubscribe<BackpackSlotChangedPublish>(OnSlotChanged);
                bus.Unsubscribe<BackpackItemInfoChangedPublish>(OnItemInfoChanged);
            }
            _subscribed = false;
        }

        if (btnSort != null)
            btnSort.onClick.RemoveListener(OnSortClicked);
        if (btnSeparate != null)
            btnSeparate.onClick.RemoveListener(OnSeparateClicked);
        if (btnDrop != null)
            btnDrop.onClick.RemoveListener(OnDropClicked);
    }

    private void TrySubscribe()
    {
        if (_subscribed) return;

        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;

        bus.Subscribe<BackpackSlotChangedPublish>(OnSlotChanged);
        bus.Subscribe<BackpackItemInfoChangedPublish>(OnItemInfoChanged);
        _subscribed = true;
    }

    private void OnSlotChanged(BackpackSlotChangedPublish e)
    {
        EnsureCache(e.index + 1);
        _cache[e.index] = new SlotData { icon = e.icon, amount = e.amount };
        _dirty = true;

        if (IsContainerActive())
        {
            EnsureViews();
            ApplySlot(e.index);
        }
    }

    private void OnItemInfoChanged(BackpackItemInfoChangedPublish e)
    {
        _selectedIndex = e.slotIndex;
        UpdateSelectionVisual();

        if (e.isEmpty)
        {
            ClearInfoPanel();
            return;
        }

        SetIcon(infoIcon, e.icon);
        SetAmountText(infoAmountText, e.amount, true);
        SetLocalizedOrText(_nameLocalized, nameText, e.nameKey);
        SetLocalizedOrText(_descLocalized, descText, e.descKey);
        SetLocalizedOrText(_categoryLocalized, categoryText, e.categoryKey);

        if (priceText != null)
            priceText.text = e.sellPrice > 0 ? e.sellPrice.ToString() : string.Empty;

        if (rareObject != null)
            rareObject.SetActive(false);

        if (btnUse != null)
            btnUse.interactable = true;

        ApplyStats(e.stats);
    }

    private void LateUpdate()
    {
        if (!_subscribed)
            TrySubscribe();

        bool active = IsContainerActive();
        if (active && (!_wasActive || _dirty))
        {
            EnsureViews();
            RefreshAll();
            _dirty = false;
        }

        _wasActive = active;
    }

    private void RefreshAll()
    {
        if (_cache == null || _views == null) return;

        for (int i = 0; i < _cache.Length && i < _views.Length; i++)
            ApplySlot(i);

        UpdateSelectionVisual();
    }

    private void OnSortClicked()
    {
        GameManager.Instance?.EventBus?.Publish(new BackpackSortRequestPublish());
    }

    private void OnSeparateClicked()
    {
        if (_selectedIndex < 0) return;

        int amount = 1;
        if (splitInput != null && !string.IsNullOrEmpty(splitInput.text))
            int.TryParse(splitInput.text, out amount);

        if (amount <= 0) return;

        GameManager.Instance?.EventBus?.Publish(
            new BackpackSplitRequestPublish(_selectedIndex, amount));
    }

    private void OnDropClicked()
    {
        if (_selectedIndex < 0) return;

        GameManager.Instance?.EventBus?.Publish(
            new InventoryDropRequestPublish(InventoryType.Backpack, _selectedIndex));
    }

    private bool IsContainerActive()
    {
        return slotsContainer != null && slotsContainer.gameObject.activeInHierarchy;
    }

    private void EnsureCache(int minSize)
    {
        if (_cache == null)
        {
            _cache = new SlotData[minSize];
            return;
        }

        if (_cache.Length < minSize)
        {
            var old = _cache;
            _cache = new SlotData[minSize];
            old.CopyTo(_cache, 0);
        }
    }

    private void EnsureViews()
    {
        if (_viewsReady || slotsContainer == null) return;

        int count = slotsContainer.childCount;
        _views = new SlotView[count];
        for (int i = 0; i < count; i++)
            _views[i] = new SlotView(slotsContainer.GetChild(i), HandleSlotSelected, i, InventoryType.Backpack);

        _viewsReady = true;
    }

    private void ApplySlot(int index)
    {
        if (_views == null || index < 0 || index >= _views.Length) return;
        if (_cache == null || index >= _cache.Length) return;

        _views[index].SetIcon(_cache[index].icon);
        _views[index].SetAmount(_cache[index].amount);
    }

    private void HandleSlotSelected(int index)
    {
        _selectedIndex = index;
        UpdateSelectionVisual();
        GameManager.Instance?.EventBus?.Publish(new BackpackSlotSelectedRequestPublish(index));
    }

    private void CacheInfoRefs()
    {
        AutoAssignInfoRefs();

        if (nameText != null) _nameLocalized = nameText.GetComponent<LocalizedText>();
        if (descText != null) _descLocalized = descText.GetComponent<LocalizedText>();
        if (categoryText != null) _categoryLocalized = categoryText.GetComponent<LocalizedText>();
    }

    private void AutoAssignInfoRefs()
    {
        var infoRoot = FindDeepChild(transform.root, "Info_Item");
        if (infoRoot == null) return;

        if (infoIcon == null)
        {
            var icon = FindDeepChild(infoRoot, "icon") ?? FindDeepChild(infoRoot, "Icon") ?? FindDeepChild(infoRoot, "Image");
            if (icon != null) infoIcon = icon.GetComponent<Image>();
        }

        if (nameText == null)
        {
            var target = FindDeepChild(infoRoot, "Name_Tmp");
            if (target != null) nameText = target.GetComponent<TMP_Text>();
        }

        if (descText == null)
        {
            var target = FindDeepChild(infoRoot, "desc_tmp");
            if (target != null) descText = target.GetComponent<TMP_Text>();
        }

        if (categoryText == null)
        {
            var target = FindDeepChild(infoRoot, "Category_tmp");
            if (target != null) categoryText = target.GetComponent<TMP_Text>();
        }

        if (priceText == null)
        {
            var panel = FindDeepChild(infoRoot, "Price_panel");
            if (panel != null)
            {
                var value = FindDeepChild(panel, "value") ?? FindDeepChild(panel, "Value");
                if (value != null) priceText = value.GetComponent<TMP_Text>();
            }
        }

        if (btnUse == null)
        {
            var target = FindDeepChild(infoRoot, "Btn_use") ?? FindDeepChild(infoRoot, "Btn_Use");
            if (target != null) btnUse = target.GetComponent<Button>();
        }

        if (statListUI == null)
        {
            var scroll = FindDeepChild(infoRoot, "attribute_SclView");
            if (scroll != null)
            {
                statListUI = scroll.GetComponentInChildren<StatListUI>(true);

                if (statListUI == null)
                {
                    var content = FindDeepChild(scroll, "content") ?? FindDeepChild(scroll, "Content");
                    if (content != null)
                        statListUI = content.GetComponent<StatListUI>();
                }
            }

            if (statListUI == null)
                statListUI = infoRoot.GetComponentInChildren<StatListUI>(true);
        }

        if (btnDrop == null)
        {
            var target = FindDeepChild(infoRoot, "Btn_Drop");
            if (target != null) btnDrop = target.GetComponent<Button>();
        }

        // btn_Separate và Number_inputText nằm trong Button panel (cùng cấp với btn_sort)
        var backpackMenu = FindDeepChild(transform.root, "BackPack_menu");
        if (backpackMenu != null)
        {
            if (btnSeparate == null)
            {
                var target = FindDeepChild(backpackMenu, "btn_Separate");
                if (target != null) btnSeparate = target.GetComponent<Button>();
            }

            if (splitInput == null)
            {
                var target = FindDeepChild(backpackMenu, "Number_inputText");
                if (target != null) splitInput = target.GetComponent<TMP_InputField>();
            }
        }
    }

    private void ApplyStats(StatDisplay[] stats)
    {
        if (statListUI != null)
            statListUI.Show(stats);
    }

    private void UpdateSelectionVisual()
    {
        if (_views == null) return;

        for (int i = 0; i < _views.Length; i++)
            _views[i].SetSelected(i == _selectedIndex);
    }

    private void ClearInfoPanel()
    {
        SetIcon(infoIcon, null);
        SetAmountText(infoAmountText, 0, true);
        SetLocalizedOrText(_nameLocalized, nameText, string.Empty);
        SetLocalizedOrText(_descLocalized, descText, string.Empty);
        SetLocalizedOrText(_categoryLocalized, categoryText, string.Empty);

        if (priceText != null)
            priceText.text = string.Empty;

        if (rareObject != null)
            rareObject.SetActive(false);

        if (btnUse != null)
            btnUse.interactable = false;

        if (statListUI != null)
            statListUI.Clear();
    }

    private static void SetLocalizedOrText(LocalizedText localized, TMP_Text text, string key)
    {
        if (localized != null)
        {
            localized.SetKey(key);
            return;
        }

        if (text != null)
            text.text = key ?? string.Empty;
    }

    private static void SetIcon(Image image, Sprite sprite)
    {
        if (image == null) return;

        image.sprite = sprite;
        image.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
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

    private class SlotView
    {
        private readonly Image _icon;
        private readonly TextMeshProUGUI _amount;
        private readonly GameObject _selected;
        private readonly Toggle _toggle;

        public SlotView(Transform root, Action<int> onSelected, int index, InventoryType invType)
        {
            var iconT = root.Find("icon") ?? root.Find("Icon") ?? root.Find("Image");
            if (iconT != null) _icon = iconT.GetComponent<Image>();

            var amountT = root.Find("Amount") ?? root.Find("Quantity");
            if (amountT != null) _amount = amountT.GetComponent<TextMeshProUGUI>();

            var selectedT = root.Find("Select") ?? root.Find("Selected") ?? root.Find("Highlight");
            if (selectedT != null) _selected = selectedT.gameObject;

            // Drag & drop
            var drag = root.GetComponent<DraggableSlot>();
            if (drag == null) drag = root.gameObject.AddComponent<DraggableSlot>();
            drag.Init(invType, index, _icon);

            _toggle = root.GetComponent<Toggle>();
            if (_toggle != null)
            {
                _toggle.onValueChanged.AddListener(isOn =>
                {
                    if (isOn) onSelected?.Invoke(index);
                });
                return;
            }

            var button = root.GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(() => onSelected?.Invoke(index));
        }

        public void SetIcon(Sprite sprite)
        {
            if (_icon == null) return;
            _icon.sprite = sprite;
            _icon.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        public void SetAmount(int amount)
        {
            if (_amount == null) return;

            if (amount <= 0)
            {
                _amount.text = string.Empty;
                return;
            }

            _amount.text = amount > MaxDisplayAmount
                ? MaxDisplayAmount.ToString()
                : amount.ToString();
        }

        public void SetSelected(bool selected)
        {
            if (_toggle != null)
                _toggle.SetIsOnWithoutNotify(selected);

            if (_selected != null)
                _selected.SetActive(selected);
        }
    }

}
