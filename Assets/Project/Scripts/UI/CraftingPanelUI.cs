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
        EnsureBasicLayout();
        AutoFindRefs();
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

        AutoFindRefs();
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

    private void AutoFindRefs()
    {
        panel ??= gameObject;
        titleText ??= FindText(transform, "TitleText");
        resultText ??= FindText(transform, "ResultText");
        closeButton ??= FindButton(transform, "CloseButton");
        categoryListRoot ??= FindDeepChild(transform, "CategoryListRoot")
                          ?? FindDeepChild(transform, "CategoryContent");
        categoryButtonTemplate ??= FindButton(transform, "CategoryButtonTemplate");
        recipeListRoot ??= FindDeepChild(transform, "RecipeListRoot")
                        ?? FindDeepChild(transform, "RecipeContent");
        recipeRowTemplate ??= FindDeepChild(transform, "RecipeRowTemplate")?.GetComponent<CraftingRecipeRowUI>();
        selectedRecipeIcon ??= FindImage(transform, "SelectedRecipeIcon");
        selectedRecipeNameText ??= FindText(transform, "SelectedRecipeNameText");
        selectedRecipeDescText ??= FindText(transform, "SelectedRecipeDescText");
        selectedRecipeLevelText ??= FindText(transform, "SelectedRecipeLevelText");
        selectedRecipeOutputText ??= FindText(transform, "SelectedRecipeOutputText");
        ingredientListRoot ??= FindDeepChild(transform, "IngredientListRoot")
                           ?? FindDeepChild(transform, "IngredientContent");
        ingredientSlotTemplate ??= FindDeepChild(transform, "IngredientSlotTemplate")?.gameObject;
        quantityMinusButton ??= FindButton(transform, "QuantityMinusButton");
        quantityPlusButton ??= FindButton(transform, "QuantityPlusButton");
        quantityMaxButton ??= FindButton(transform, "QuantityMaxButton");
        quantityText ??= FindText(transform, "QuantityText");
        craftButton ??= FindButton(transform, "CraftButton");
    }

    private void EnsureBasicLayout()
    {
        AutoFindRefs();
        if (titleText != null &&
            recipeListRoot != null &&
            recipeRowTemplate != null &&
            selectedRecipeNameText != null &&
            ingredientListRoot != null &&
            ingredientSlotTemplate != null &&
            craftButton != null)
        {
            return;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        var background = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        background.color = new Color(0.95f, 0.78f, 0.48f, 0.96f);

        var outline = GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.78f, 0.54f, 0.20f, 1f);
        outline.effectDistance = new Vector2(3f, -3f);

        var header = CreateUiObject("Header", transform);
        SetRect(header, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 72f));
        CreateImage("HeaderBackground", header, new Color(0.30f, 0.17f, 0.07f, 0.95f), stretch: true);

        titleText = CreateText("TitleText", header, "Chế tạo vật phẩm", 28f, TextAlignmentOptions.Center, new Color(0.98f, 0.88f, 0.55f));
        Stretch(titleText.rectTransform, new Vector2(90f, 0f), new Vector2(-90f, 0f));
        closeButton = CreateBasicButton("CloseButton", header, "X", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-42f, 0f), new Vector2(48f, 42f));

        var body = CreateUiObject("Body", transform);
        Stretch(body, new Vector2(28f, 28f), new Vector2(-28f, -92f));

        var categoryPanel = CreatePanel("CategoryPanel", body, new Vector2(0f, 0f), new Vector2(0.18f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(-12f, 0f));
        categoryListRoot = CreateUiObject("CategoryListRoot", categoryPanel);
        Stretch((RectTransform)categoryListRoot, new Vector2(10f, 10f), new Vector2(-10f, -10f));
        ConfigureVerticalList(categoryListRoot, 8f, new RectOffset(6, 6, 6, 6));
        categoryButtonTemplate = CreateBasicButton("CategoryButtonTemplate", categoryListRoot, "Tất cả", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0f, 46f));
        categoryButtonTemplate.gameObject.SetActive(false);

        var recipePanel = CreatePanel("RecipePanel", body, new Vector2(0.19f, 0f), new Vector2(0.54f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(-8f, 0f));
        recipeListRoot = CreateUiObject("RecipeListRoot", recipePanel);
        Stretch((RectTransform)recipeListRoot, new Vector2(12f, 12f), new Vector2(-12f, -12f));
        ConfigureVerticalList(recipeListRoot, 10f, new RectOffset(4, 4, 4, 4));
        recipeRowTemplate = CreateRecipeRowTemplate(recipeListRoot);
        recipeRowTemplate.gameObject.SetActive(false);

        var detailPanel = CreatePanel("DetailPanel", body, new Vector2(0.55f, 0f), Vector2.one, new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);

        selectedRecipeIcon = CreateImage("SelectedRecipeIcon", detailPanel, new Color(0.22f, 0.12f, 0.04f, 0.86f), stretch: false);
        SetRect(selectedRecipeIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(86f, 86f));

        selectedRecipeNameText = CreateText("SelectedRecipeNameText", detailPanel, "Chọn công thức", 26f, TextAlignmentOptions.MidlineLeft, new Color(0.28f, 0.16f, 0.06f));
        SetRect(selectedRecipeNameText.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f), new Vector2(126f, -24f), new Vector2(-150f, 48f));

        selectedRecipeDescText = CreateText("SelectedRecipeDescText", detailPanel, "Chọn một công thức ở danh sách bên trái để xem nguyên liệu cần thiết.", 18f, TextAlignmentOptions.TopLeft, new Color(0.34f, 0.22f, 0.10f));
        SetRect(selectedRecipeDescText.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f), new Vector2(24f, -118f), new Vector2(-48f, 82f));

        selectedRecipeLevelText = CreateText("SelectedRecipeLevelText", detailPanel, string.Empty, 18f, TextAlignmentOptions.MidlineLeft, new Color(0.42f, 0.24f, 0.08f));
        SetRect(selectedRecipeLevelText.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f), new Vector2(24f, -208f), new Vector2(-48f, 32f));

        selectedRecipeOutputText = CreateText("SelectedRecipeOutputText", detailPanel, string.Empty, 18f, TextAlignmentOptions.MidlineLeft, new Color(0.42f, 0.24f, 0.08f));
        SetRect(selectedRecipeOutputText.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f), new Vector2(24f, -246f), new Vector2(-48f, 32f));

        var ingredientTitle = CreateText("IngredientTitle", detailPanel, "Nguyên liệu", 20f, TextAlignmentOptions.MidlineLeft, new Color(0.28f, 0.16f, 0.06f));
        SetRect(ingredientTitle.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f), new Vector2(24f, -302f), new Vector2(-48f, 34f));

        ingredientListRoot = CreateUiObject("IngredientListRoot", detailPanel);
        SetRect(ingredientListRoot as RectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f), new Vector2(24f, -344f), new Vector2(-48f, 150f));
        ConfigureVerticalList(ingredientListRoot, 6f, new RectOffset(0, 0, 0, 0));
        ingredientSlotTemplate = CreateIngredientSlotTemplate(ingredientListRoot);
        ingredientSlotTemplate.SetActive(false);

        var quantityPanel = CreateUiObject("QuantityPanel", detailPanel);
        SetRect(quantityPanel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 78f), new Vector2(-48f, 46f));
        quantityMinusButton = CreateBasicButton("QuantityMinusButton", quantityPanel, "-", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(44f, 40f));
        quantityText = CreateText("QuantityText", quantityPanel, "1", 22f, TextAlignmentOptions.Center, new Color(0.28f, 0.16f, 0.06f));
        SetRect(quantityText.rectTransform, new Vector2(0f, 0f), Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-220f, 0f));
        quantityPlusButton = CreateBasicButton("QuantityPlusButton", quantityPanel, "+", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-88f, 0f), new Vector2(44f, 40f));
        quantityMaxButton = CreateBasicButton("QuantityMaxButton", quantityPanel, "Max", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-28f, 0f), new Vector2(60f, 40f));

        resultText = CreateText("ResultText", detailPanel, string.Empty, 18f, TextAlignmentOptions.Center, new Color(0.42f, 0.24f, 0.08f));
        SetRect(resultText.rectTransform, new Vector2(0f, 0f), Vector2.right, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(-230f, 30f));

        craftButton = CreateBasicButton("CraftButton", detailPanel, "Chế tạo", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-92f, 30f), new Vector2(160f, 50f));
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
        image.sprite = itemData?.icon;
        image.enabled = true;
        image.color = image.sprite != null ? Color.white : new Color(0.20f, 0.11f, 0.04f, 0.60f);
        image.preserveAspect = image.sprite != null;
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

    private static RectTransform CreateUiObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static RectTransform CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var panel = CreateUiObject(name, parent);
        SetRect(panel, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
        var image = panel.gameObject.AddComponent<Image>();
        image.color = new Color(0.93f, 0.68f, 0.36f, 0.42f);
        var outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.54f, 0.32f, 0.12f, 0.65f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        return panel;
    }

    private static Image CreateImage(string name, Transform parent, Color color, bool stretch)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.preserveAspect = true;
        if (stretch)
            Stretch(image.rectTransform, Vector2.zero, Vector2.zero);
        return image;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float size, TextAlignmentOptions alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = true;
        return text;
    }

    private static Button CreateBasicButton(string name, Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        SetRect(rect, anchorMin, anchorMax, new Vector2(0.5f, 0.5f), anchoredPosition, sizeDelta);

        var image = go.GetComponent<Image>();
        image.color = new Color(0.34f, 0.20f, 0.08f, 1f);

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;

        var text = CreateText("Label", go.transform, label, 18f, TextAlignmentOptions.Center, new Color(1f, 0.90f, 0.66f));
        Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
        return button;
    }

    private static CraftingRecipeRowUI CreateRecipeRowTemplate(Transform parent)
    {
        var row = new GameObject("RecipeRowTemplate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CraftingRecipeRowUI));
        row.transform.SetParent(parent, false);
        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        SetLayoutSize(row, 0f, 78f);

        var iconFrame = CreateUiObject("IconFrame", row.transform);
        SetLayoutSize(iconFrame.gameObject, 56f, 56f);
        var iconBg = iconFrame.gameObject.AddComponent<Image>();
        iconBg.color = new Color(0.20f, 0.11f, 0.04f, 0.80f);
        var icon = CreateImage("Icon", iconFrame, Color.white, stretch: true);
        icon.rectTransform.offsetMin = new Vector2(6f, 6f);
        icon.rectTransform.offsetMax = new Vector2(-6f, -6f);

        var textStack = CreateUiObject("TextStack", row.transform);
        var textLayout = textStack.gameObject.AddComponent<VerticalLayoutGroup>();
        textLayout.spacing = 2f;
        textLayout.childAlignment = TextAnchor.MiddleLeft;
        textLayout.childControlWidth = true;
        textLayout.childControlHeight = true;
        textLayout.childForceExpandWidth = true;
        textLayout.childForceExpandHeight = false;
        var textSize = textStack.gameObject.AddComponent<LayoutElement>();
        textSize.flexibleWidth = 1f;
        CreateText("NameText", textStack, string.Empty, 18f, TextAlignmentOptions.MidlineLeft, new Color(0.96f, 0.86f, 0.62f));
        CreateText("IngredientText", textStack, string.Empty, 14f, TextAlignmentOptions.MidlineLeft, new Color(0.78f, 0.92f, 0.42f));

        var levelText = CreateText("LevelText", row.transform, string.Empty, 16f, TextAlignmentOptions.Center, new Color(1f, 0.78f, 0.25f));
        SetLayoutSize(levelText.gameObject, 56f, 38f);
        var chooseButton = CreateBasicButton("CraftButton", row.transform, "Chọn", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(66f, 38f));
        SetLayoutSize(chooseButton.gameObject, 66f, 38f);
        return row.GetComponent<CraftingRecipeRowUI>();
    }

    private static GameObject CreateIngredientSlotTemplate(Transform parent)
    {
        var row = new GameObject("IngredientSlotTemplate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        row.transform.SetParent(parent, false);
        row.GetComponent<Image>().color = new Color(0.34f, 0.20f, 0.08f, 0.72f);
        SetLayoutSize(row, 0f, 48f);
        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 6, 6);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var icon = CreateImage("Icon", row.transform, Color.white, stretch: false);
        SetLayoutSize(icon.gameObject, 36f, 36f);
        var name = CreateText("NameText", row.transform, string.Empty, 16f, TextAlignmentOptions.MidlineLeft, new Color(0.96f, 0.86f, 0.62f));
        name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var amount = CreateText("AmountText", row.transform, string.Empty, 16f, TextAlignmentOptions.MidlineRight, Color.white);
        SetLayoutSize(amount.gameObject, 82f, 32f);
        return row;
    }

    private static void ConfigureVerticalList(Transform root, float spacing, RectOffset padding)
    {
        var layout = root.GetComponent<VerticalLayoutGroup>() ?? root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = padding;
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static void SetLayoutSize(GameObject go, float width, float height)
    {
        var layout = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.minHeight = height;
        layout.preferredHeight = height;
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
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
