using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingPanelUI : MonoBehaviour
{
    private const string AllCategoryId = "all";

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Transform categoryListRoot;
    [SerializeField] private Button categoryButtonTemplate;
    [SerializeField] private Transform recipeListRoot;
    [SerializeField] private CraftingRecipeRowUI recipeRowTemplate;
    [SerializeField] private Image selectedRecipeIcon;
    [SerializeField] private TMP_Text selectedRecipeNameText;
    [SerializeField] private TMP_Text selectedRecipeDescText;
    [SerializeField] private TMP_Text selectedRecipeLevelText;
    [SerializeField] private TMP_Text selectedRecipeOutputText;
    [SerializeField] private Transform ingredientListRoot;
    [SerializeField] private GameObject ingredientSlotTemplate;
    [SerializeField] private Button quantityMinusButton;
    [SerializeField] private Button quantityPlusButton;
    [SerializeField] private Button quantityMaxButton;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Button craftButton;

    private readonly List<GameObject> spawnedRows = new();
    private readonly List<GameObject> spawnedCategories = new();
    private readonly List<GameObject> spawnedIngredients = new();
    private EventBus subscribedBus;
    private CraftingViewData currentView;
    private CraftingRecipeViewData selectedRecipe;
    private string selectedCategoryId = AllCategoryId;
    private int craftTimes = 1;
    private bool listenersRegistered;

    private void OnEnable()
    {
        TrySubscribe();
        RegisterListeners();
        Hide();
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
            subscribedBus.Unsubscribe<CraftingViewPublish>(OnCraftingView);
            subscribedBus.Unsubscribe<CraftingResultPublish>(OnCraftingResult);
            subscribedBus = null;
        }

        UnregisterListeners();
    }

    private void OnCraftingView(CraftingViewPublish e)
    {
        if (e.viewData == null) return;

        currentView = e.viewData;
        Show();
        SetLocalizedText(titleText, "ui.crafting.title");
        SetPlainText(resultText, string.Empty);
        selectedRecipe = FindMatchingRecipe(selectedRecipe) ?? FirstRecipeMatchingCategory() ?? FirstRecipe();
        craftTimes = 1;
        RebuildCategories();
        RebuildRows();
        RefreshDetails();
    }

    private void OnCraftingResult(CraftingResultPublish e)
    {
        if (currentView == null || e.crafter != currentView.Crafter) return;

        SetPlainText(resultText, e.result.Success ? "Chế tạo thành công" : GetFailText(e.result.FailReason));
        if (e.result.Success)
            GameManager.Instance?.CraftingService?.Open(currentView.Crafter, currentView.Station, ExtractRecipes(currentView));
    }

    private void RebuildRows()
    {
        ClearRows();
        if (recipeListRoot == null || recipeRowTemplate == null || currentView?.Recipes == null) return;

        foreach (var recipeView in currentView.Recipes)
        {
            if (recipeView?.Recipe == null) continue;
            if (!IsRecipeInSelectedCategory(recipeView)) continue;
            var row = Instantiate(recipeRowTemplate, recipeListRoot);
            row.gameObject.SetActive(true);
            row.Init(recipeView, OnRecipeSelected, recipeView == selectedRecipe);
            spawnedRows.Add(row.gameObject);
        }
    }

    private void RebuildCategories()
    {
        ClearCategories();
        if (categoryListRoot == null || categoryButtonTemplate == null || currentView?.Recipes == null) return;

        SpawnCategoryButton(AllCategoryId, "Tất cả");

        var seen = new HashSet<string>();
        foreach (var recipeView in currentView.Recipes)
        {
            var category = GetRecipeCategory(recipeView);
            string id = category.ToString();
            if (!seen.Add(id)) continue;
            SpawnCategoryButton(id, GetCategoryLabel(category));
        }
    }

    private void SpawnCategoryButton(string categoryId, string label)
    {
        var button = Instantiate(categoryButtonTemplate, categoryListRoot);
        button.gameObject.SetActive(true);
        SetButtonLabel(button, label);
        SetButtonColor(button, categoryId == selectedCategoryId
            ? new Color(0.58f, 0.36f, 0.13f, 0.98f)
            : new Color(0.28f, 0.17f, 0.08f, 0.95f));
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            selectedCategoryId = categoryId;
            selectedRecipe = FirstRecipeMatchingCategory() ?? FirstRecipe();
            craftTimes = 1;
            RebuildCategories();
            RebuildRows();
            RefreshDetails();
        });
        spawnedCategories.Add(button.gameObject);
    }

    private void OnRecipeSelected(CraftingRecipeViewData recipeView)
    {
        if (currentView == null || recipeView?.Recipe == null) return;
        selectedRecipe = recipeView;
        craftTimes = 1;
        RebuildRows();
        RefreshDetails();
    }

    private void CraftSelectedRecipe()
    {
        if (currentView == null || selectedRecipe?.Recipe == null) return;
        GameManager.Instance?.CraftingService?.TryCraft(currentView.Crafter, selectedRecipe.Recipe, craftTimes);
    }

    private void ChangeCraftTimes(int delta)
    {
        if (selectedRecipe == null) return;
        craftTimes = Mathf.Clamp(craftTimes + delta, 1, GetMaxCraftTimes(selectedRecipe));
        RefreshDetails();
    }

    private void SetMaxCraftTimes()
    {
        if (selectedRecipe == null) return;
        craftTimes = GetMaxCraftTimes(selectedRecipe);
        RefreshDetails();
    }

    private void RefreshDetails()
    {
        ClearIngredients();

        bool hasSelection = selectedRecipe?.Recipe != null;
        var primaryOutput = GetPrimaryOutput(selectedRecipe);
        SetIcon(selectedRecipeIcon, primaryOutput);
        SetLocalizedText(selectedRecipeNameText, hasSelection
            ? (!string.IsNullOrWhiteSpace(selectedRecipe.Recipe.titleKey) ? selectedRecipe.Recipe.titleKey : primaryOutput?.keyName)
            : string.Empty);
        SetLocalizedText(selectedRecipeDescText, primaryOutput?.descKey);
        SetPlainText(selectedRecipeLevelText, hasSelection ? $"Yêu cầu cấp: Lv.{selectedRecipe.Recipe.requiredLevel}" : string.Empty);
        SetPlainText(selectedRecipeOutputText, hasSelection ? BuildOutputText(selectedRecipe, craftTimes) : string.Empty);

        if (hasSelection && selectedRecipe.Ingredients != null)
        {
            foreach (var ingredient in selectedRecipe.Ingredients)
                SpawnIngredientSlot(ingredient, craftTimes);
        }

        int maxTimes = hasSelection ? GetMaxCraftTimes(selectedRecipe) : 1;
        craftTimes = Mathf.Clamp(craftTimes, 1, maxTimes);

        SetPlainText(quantityText, craftTimes.ToString());

        bool canCraft = hasSelection && selectedRecipe.CanCraft && craftTimes <= maxTimes;
        if (craftButton != null)
            craftButton.interactable = canCraft;
        if (quantityMinusButton != null)
            quantityMinusButton.interactable = hasSelection && craftTimes > 1;
        if (quantityPlusButton != null)
            quantityPlusButton.interactable = hasSelection && craftTimes < maxTimes;
        if (quantityMaxButton != null)
            quantityMaxButton.interactable = hasSelection && maxTimes > 1;
    }

    private void Show()
    {
        if (panel != null) panel.SetActive(true);
        else gameObject.SetActive(true);
    }

    private void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void ClearRows()
    {
        foreach (var row in spawnedRows)
            if (row != null) Destroy(row);
        spawnedRows.Clear();
    }

    private void ClearCategories()
    {
        foreach (var item in spawnedCategories)
            if (item != null) Destroy(item);
        spawnedCategories.Clear();
    }

    private void ClearIngredients()
    {
        foreach (var item in spawnedIngredients)
            if (item != null) Destroy(item);
        spawnedIngredients.Clear();
    }

    private void RegisterListeners()
    {
        if (listenersRegistered) return;

        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (quantityMinusButton != null) quantityMinusButton.onClick.AddListener(() => ChangeCraftTimes(-1));
        if (quantityPlusButton != null) quantityPlusButton.onClick.AddListener(() => ChangeCraftTimes(1));
        if (quantityMaxButton != null) quantityMaxButton.onClick.AddListener(SetMaxCraftTimes);
        if (craftButton != null) craftButton.onClick.AddListener(CraftSelectedRecipe);

        listenersRegistered = true;
    }

    private void UnregisterListeners()
    {
        if (!listenersRegistered) return;

        if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
        if (quantityMinusButton != null) quantityMinusButton.onClick.RemoveAllListeners();
        if (quantityPlusButton != null) quantityPlusButton.onClick.RemoveAllListeners();
        if (quantityMaxButton != null) quantityMaxButton.onClick.RemoveAllListeners();
        if (craftButton != null) craftButton.onClick.RemoveListener(CraftSelectedRecipe);

        listenersRegistered = false;
    }

    private void TrySubscribe()
    {
        var bus = GameManager.Instance?.EventBus;
        if (bus == null || bus == subscribedBus) return;

        if (subscribedBus != null)
        {
            subscribedBus.Unsubscribe<CraftingViewPublish>(OnCraftingView);
            subscribedBus.Unsubscribe<CraftingResultPublish>(OnCraftingResult);
        }

        subscribedBus = bus;
        subscribedBus.Subscribe<CraftingViewPublish>(OnCraftingView);
        subscribedBus.Subscribe<CraftingResultPublish>(OnCraftingResult);
    }

    private static List<RecipeData> ExtractRecipes(CraftingViewData viewData)
    {
        var recipes = new List<RecipeData>();
        if (viewData?.Recipes == null) return recipes;
        foreach (var recipeView in viewData.Recipes)
        {
            if (recipeView?.Recipe != null)
                recipes.Add(recipeView.Recipe);
        }
        return recipes;
    }

    private CraftingRecipeViewData FindMatchingRecipe(CraftingRecipeViewData recipeView)
    {
        if (recipeView?.Recipe == null || currentView?.Recipes == null) return null;
        foreach (var candidate in currentView.Recipes)
        {
            if (candidate?.Recipe == recipeView.Recipe)
                return candidate;
        }

        return null;
    }

    private CraftingRecipeViewData FirstRecipe()
    {
        if (currentView?.Recipes == null) return null;
        foreach (var recipeView in currentView.Recipes)
        {
            if (recipeView?.Recipe != null)
                return recipeView;
        }

        return null;
    }

    private CraftingRecipeViewData FirstRecipeMatchingCategory()
    {
        if (currentView?.Recipes == null) return null;
        foreach (var recipeView in currentView.Recipes)
        {
            if (recipeView?.Recipe != null && IsRecipeInSelectedCategory(recipeView))
                return recipeView;
        }

        return null;
    }

    private bool IsRecipeInSelectedCategory(CraftingRecipeViewData recipeView)
    {
        if (selectedCategoryId == AllCategoryId) return true;
        return GetRecipeCategory(recipeView).ToString() == selectedCategoryId;
    }

    private static ItemCategory GetRecipeCategory(CraftingRecipeViewData recipeView)
    {
        var output = GetPrimaryOutput(recipeView);
        if (output == null || output.category == ItemCategory.None)
            return ItemCategory.Misc;

        return output.category;
    }

    private static EntityData GetPrimaryOutput(CraftingRecipeViewData recipeView)
    {
        return recipeView?.Recipe?.outputs != null && recipeView.Recipe.outputs.Count > 0
            ? recipeView.Recipe.outputs[0]?.item
            : null;
    }

    private static string GetCategoryLabel(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Tool => "Công cụ",
            ItemCategory.Seed => "Hạt giống",
            ItemCategory.Crop => "Nông sản",
            ItemCategory.Food => "Đồ ăn",
            ItemCategory.Material => "Nguyên liệu",
            ItemCategory.Weapon => "Vũ khí",
            ItemCategory.Armor => "Áo giáp",
            ItemCategory.Accessory => "Phụ kiện",
            ItemCategory.Placeable => "Trang trí",
            ItemCategory.Consumable => "Tiêu hao",
            ItemCategory.Quest => "Nhiệm vụ",
            _ => "Khác"
        };
    }

    private static string BuildOutputText(CraftingRecipeViewData recipeView, int times)
    {
        if (recipeView?.Recipe?.outputs == null || recipeView.Recipe.outputs.Count == 0)
            return string.Empty;

        var parts = new List<string>();
        foreach (var output in recipeView.Recipe.outputs)
        {
            if (output?.item == null || output.amount <= 0) continue;
            parts.Add($"{Localize(output.item.keyName)} x{output.amount * times}");
        }

        return parts.Count > 0 ? "Nhận: " + string.Join(", ", parts) : string.Empty;
    }

    private int GetMaxCraftTimes(CraftingRecipeViewData recipeView)
    {
        if (recipeView == null || !recipeView.LevelOk)
            return 1;

        int max = 99;
        bool hasIngredient = false;
        foreach (var ingredient in recipeView.Ingredients)
        {
            if (ingredient?.Item == null || ingredient.RequiredAmount <= 0) continue;
            hasIngredient = true;
            max = Mathf.Min(max, ingredient.CurrentAmount / Mathf.Max(1, ingredient.RequiredAmount));
        }

        return Mathf.Max(1, hasIngredient ? max : 1);
    }

    private void SpawnIngredientSlot(CraftingIngredientViewData ingredient, int times)
    {
        if (ingredientListRoot == null || ingredientSlotTemplate == null || ingredient?.Item == null)
            return;

        var slot = Instantiate(ingredientSlotTemplate, ingredientListRoot);
        slot.SetActive(true);
        spawnedIngredients.Add(slot);

        var icon = FindImage(slot.transform, "Icon");
        SetIcon(icon, ingredient.Item);

        var amountText = FindText(slot.transform, "AmountText");
        int required = ingredient.RequiredAmount * Mathf.Max(1, times);
        SetPlainText(amountText, $"{ingredient.CurrentAmount}/{required}");
        if (amountText != null)
            amountText.color = ingredient.CurrentAmount >= required
                ? new Color(0.45f, 0.95f, 0.25f, 1f)
                : new Color(1f, 0.35f, 0.20f, 1f);

        var nameText = FindText(slot.transform, "NameText");
        SetLocalizedText(nameText, ingredient.Item.keyName);
    }

    private static string GetFailText(CraftingFailReason reason)
    {
        return reason switch
        {
            CraftingFailReason.LevelTooLow => "Chưa đủ cấp",
            CraftingFailReason.NotEnoughIngredients => "Thiếu nguyên liệu",
            CraftingFailReason.InventoryFull => "Túi đồ đã đầy",
            CraftingFailReason.ConsumeFailed => "Không thể trừ nguyên liệu",
            CraftingFailReason.OutputFailed => "Không thể nhận vật phẩm",
            _ => reason.ToString()
        };
    }

    private static void SetIcon(Image image, EntityData itemData)
    {
        if (image == null) return;
        image.sprite = itemData != null ? itemData.icon : null;
        image.enabled = image.sprite != null;
        image.preserveAspect = true;
    }

    private static void SetButtonLabel(Button button, string value)
    {
        var text = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
        if (text != null)
            text.text = value ?? string.Empty;
    }

    private static void SetButtonColor(Button button, Color color)
    {
        var image = button != null ? button.GetComponent<Image>() : null;
        if (image != null)
            image.color = color;
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
}
