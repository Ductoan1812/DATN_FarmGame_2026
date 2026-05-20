using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class SetupCraftingPanelUITemplate
{
    private const string UiFontAssetPath = "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Roboto-Bold SDF.asset";

    private static readonly Color PanelColor = new(0.84f, 0.64f, 0.34f, 0.96f);
    private static readonly Color PanelDarkColor = new(0.28f, 0.17f, 0.08f, 0.95f);
    private static readonly Color ButtonColor = new(0.45f, 0.25f, 0.10f, 0.98f);
    private static readonly Color TextColor = new(0.14f, 0.08f, 0.04f, 1f);
    private static readonly Color LightTextColor = new(0.96f, 0.84f, 0.62f, 1f);

    [MenuItem("Tools/DATN/UI/Create Crafting Panel UI")]
    public static void Execute()
    {
        var canvas = GetOrCreateCanvas();
        EnsureEventSystem();

        var windowsRoot = GetOrCreateWindowsRoot(canvas.transform);
        var root = GetOrCreateUI("NPCInteractionUI", windowsRoot);
        Stretch(root.GetComponent<RectTransform>());

        var templates = GetOrCreateUI("Templates", root.transform);
        templates.SetActive(false);

        var refs = BuildCraftingPanel(root.transform, templates.transform);
        BindCraftingPanelUI(root, refs);

        refs.panel.SetActive(false);

        Undo.RegisterFullObjectHierarchyUndo(root, "Create Crafting Panel UI");
        Selection.activeGameObject = root;
        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[SetupCraftingPanelUITemplate] Created CraftingPanel UI and bound CraftingPanelUI.");
    }

    private static CraftingRefs BuildCraftingPanel(Transform root, Transform templates)
    {
        var panel = GetOrCreatePanel("CraftingPanel", root, PanelColor);
        ClearChildren(panel.transform);
        SetupRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 640f));
        AddOutline(panel, new Color(0.22f, 0.12f, 0.04f, 1f), new Vector2(5f, -5f));

        var header = GetOrCreatePanel("Header", panel.transform, PanelDarkColor);
        SetupRect(header, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 48f));

        var headerTitle = GetOrCreateText("HeaderTitleText", header.transform, "CHẾ TẠO", 22, TextAlignmentOptions.Center, LightTextColor);
        Stretch(headerTitle.rectTransform, new Vector2(70f, 4f), new Vector2(-70f, -4f));

        var closeButton = CreateButton("CloseButton", header.transform, "X", 26, new Vector2(42f, 42f), ButtonColor, LightTextColor);
        SetupRect(closeButton.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(42f, 42f));

        var body = GetOrCreateUI("Body", panel.transform);
        SetupRect(body, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(-28f, -84f));
        var bodyLayout = GetOrAdd<HorizontalLayoutGroup>(body);
        bodyLayout.padding = new RectOffset(0, 0, 0, 0);
        bodyLayout.spacing = 12f;
        bodyLayout.childAlignment = TextAnchor.MiddleCenter;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = false;
        bodyLayout.childForceExpandHeight = true;

        var categoryPanel = GetOrCreatePanel("CategoryPanel", body.transform, new Color(0.23f, 0.15f, 0.07f, 0.96f));
        SetLayoutSize(categoryPanel, 150f, 1f);
        AddOutline(categoryPanel, new Color(0.12f, 0.07f, 0.03f, 1f), new Vector2(3f, -3f));

        var categoryTitle = GetOrCreateText("CategoryTitleText", categoryPanel.transform, "Bộ lọc", 18, TextAlignmentOptions.Center, LightTextColor);
        SetupRect(categoryTitle.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-18f, 28f));

        var categoryRoot = GetOrCreateUI("CategoryListRoot", categoryPanel.transform);
        SetupRect(categoryRoot, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(-18f, -60f));
        var categoryLayout = GetOrAdd<VerticalLayoutGroup>(categoryRoot);
        categoryLayout.padding = new RectOffset(8, 8, 8, 8);
        categoryLayout.spacing = 8f;
        categoryLayout.childAlignment = TextAnchor.UpperCenter;
        categoryLayout.childControlWidth = true;
        categoryLayout.childControlHeight = false;
        categoryLayout.childForceExpandWidth = true;
        categoryLayout.childForceExpandHeight = false;

        var listPanel = GetOrCreatePanel("RecipeListPanel", body.transform, new Color(0.30f, 0.19f, 0.08f, 0.96f));
        SetLayoutSize(listPanel, 420f, 1f);
        AddOutline(listPanel, new Color(0.12f, 0.07f, 0.03f, 1f), new Vector2(3f, -3f));

        var titleText = GetOrCreateText("TitleText", listPanel.transform, "DANH SÁCH CRAFTING", 18, TextAlignmentOptions.Center, LightTextColor);
        SetupRect(titleText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-36f, 28f));
        EnsureLocalizedText(titleText);

        var listRoot = CreateScrollContent("RecipeScrollView", listPanel.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(-28f, -68f));
        var listLayout = GetOrAdd<VerticalLayoutGroup>(listRoot.gameObject);
        listLayout.padding = new RectOffset(0, 12, 0, 0);
        listLayout.spacing = 8f;
        listLayout.childAlignment = TextAnchor.UpperCenter;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;

        var fitter = GetOrAdd<ContentSizeFitter>(listRoot.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var detailPanel = GetOrCreatePanel("CraftingDetailPanel", body.transform, new Color(0.85f, 0.62f, 0.32f, 0.96f));
        var detailLayoutElement = GetOrAdd<LayoutElement>(detailPanel);
        detailLayoutElement.minWidth = 560f;
        detailLayoutElement.preferredWidth = 560f;
        detailLayoutElement.flexibleWidth = 1f;
        detailLayoutElement.flexibleHeight = 1f;
        AddOutline(detailPanel, new Color(0.22f, 0.12f, 0.04f, 1f), new Vector2(4f, -4f));

        var detailTitle = GetOrCreateText("DetailTitleText", detailPanel.transform, "THÔNG TIN CHẾ TẠO", 18, TextAlignmentOptions.Center, LightTextColor);
        SetupRect(detailTitle.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(-36f, 28f));

        var previewBox = GetOrCreatePanel("PreviewBox", detailPanel.transform, new Color(0.96f, 0.77f, 0.43f, 0.64f));
        SetupRect(previewBox, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(-42f, 138f));

        var selectedIcon = GetOrCreateImage("SelectedRecipeIcon", previewBox.transform, new Color(1f, 1f, 1f, 0.18f));
        SetupRect(selectedIcon.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(98f, 98f));

        var selectedName = GetOrCreateText("SelectedRecipeNameText", previewBox.transform, "recipe.name", 24, TextAlignmentOptions.Left, TextColor);
        SetupRect(selectedName.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(150f, -18f), new Vector2(-170f, 32f));
        EnsureLocalizedText(selectedName);

        var selectedDesc = GetOrCreateText("SelectedRecipeDescText", previewBox.transform, "recipe.desc", 18, TextAlignmentOptions.TopLeft, TextColor);
        selectedDesc.enableWordWrapping = true;
        SetupRect(selectedDesc.gameObject, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(150f, -52f), new Vector2(-170f, -72f));
        EnsureLocalizedText(selectedDesc);

        var selectedLevel = GetOrCreateText("SelectedRecipeLevelText", previewBox.transform, "Yêu cầu cấp: Lv.1", 18, TextAlignmentOptions.Left, TextColor);
        SetupRect(selectedLevel.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(150f, 12f), new Vector2(-170f, 28f));

        var ingredientTitle = GetOrCreateText("IngredientTitleText", detailPanel.transform, "NGUYÊN LIỆU CẦN THIẾT", 18, TextAlignmentOptions.Center, LightTextColor);
        SetupRect(ingredientTitle.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -252f), new Vector2(-48f, 28f));

        var ingredientRoot = GetOrCreateUI("IngredientListRoot", detailPanel.transform);
        SetupRect(ingredientRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -324f), new Vector2(-58f, 112f));
        var ingredientLayout = GetOrAdd<HorizontalLayoutGroup>(ingredientRoot);
        ingredientLayout.padding = new RectOffset(0, 0, 0, 0);
        ingredientLayout.spacing = 12f;
        ingredientLayout.childAlignment = TextAnchor.MiddleCenter;
        ingredientLayout.childControlWidth = false;
        ingredientLayout.childControlHeight = false;
        ingredientLayout.childForceExpandWidth = false;
        ingredientLayout.childForceExpandHeight = false;

        var outputText = GetOrCreateText("SelectedRecipeOutputText", detailPanel.transform, "", 20, TextAlignmentOptions.Center, TextColor);
        SetupRect(outputText.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 152f), new Vector2(-48f, 30f));

        var controlsPanel = GetOrCreatePanel("CraftControlsPanel", detailPanel.transform, new Color(0.72f, 0.46f, 0.20f, 0.42f));
        SetupRect(controlsPanel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(-58f, 70f));

        var controlsLayout = GetOrAdd<HorizontalLayoutGroup>(controlsPanel);
        controlsLayout.padding = new RectOffset(14, 14, 8, 8);
        controlsLayout.spacing = 10f;
        controlsLayout.childAlignment = TextAnchor.MiddleCenter;
        controlsLayout.childControlWidth = false;
        controlsLayout.childControlHeight = false;
        controlsLayout.childForceExpandWidth = false;
        controlsLayout.childForceExpandHeight = false;

        var minusButton = CreateButton("QuantityMinusButton", controlsPanel.transform, "-", 24, new Vector2(48f, 46f), ButtonColor, LightTextColor);
        var quantityText = GetOrCreateText("QuantityText", controlsPanel.transform, "1", 24, TextAlignmentOptions.Center, TextColor);
        SetLayoutSize(quantityText.gameObject, 52f, 46f);
        var plusButton = CreateButton("QuantityPlusButton", controlsPanel.transform, "+", 24, new Vector2(48f, 46f), ButtonColor, LightTextColor);
        var maxButton = CreateButton("QuantityMaxButton", controlsPanel.transform, "MAX", 18, new Vector2(64f, 46f), ButtonColor, LightTextColor);
        var craftButton = CreateButton("CraftButton", controlsPanel.transform, "CHẾ TẠO", 22, new Vector2(174f, 52f), new Color(0.35f, 0.55f, 0.12f, 1f), LightTextColor);

        var resultBox = GetOrCreatePanel("ResultBox", detailPanel.transform, new Color(0.22f, 0.12f, 0.04f, 0.86f));
        SetupRect(resultBox, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(-58f, 34f));
        var resultText = GetOrCreateText("ResultText", resultBox.transform, "", 17, TextAlignmentOptions.Center, LightTextColor);
        Stretch(resultText.rectTransform, new Vector2(12f, 4f), new Vector2(-12f, -4f));

        var categoryTemplate = CreateCategoryButtonTemplate(templates);
        var rowTemplate = CreateCraftingRecipeRowTemplate(templates);
        var ingredientTemplate = CreateIngredientSlotTemplate(templates);

        return new CraftingRefs(
            panel,
            titleText,
            resultText,
            closeButton,
            categoryRoot.transform,
            categoryTemplate,
            listRoot,
            rowTemplate,
            selectedIcon,
            selectedName,
            selectedDesc,
            selectedLevel,
            outputText,
            ingredientRoot.transform,
            ingredientTemplate,
            minusButton,
            plusButton,
            maxButton,
            quantityText,
            craftButton);
    }

    private static CraftingRecipeRowUI CreateCraftingRecipeRowTemplate(Transform templates)
    {
        var row = GetOrCreatePanel("CraftingRecipeRowTemplate", templates, new Color(0.96f, 0.76f, 0.42f, 0.86f));
        ClearChildren(row.transform);
        row.SetActive(false);
        SetLayoutSize(row, 360f, 72f);
        AddOutline(row, new Color(0.42f, 0.23f, 0.08f, 1f), new Vector2(2f, -2f));
        var rowButton = GetOrAdd<Button>(row);
        rowButton.targetGraphic = row.GetComponent<Image>();

        var layout = GetOrAdd<HorizontalLayoutGroup>(row);
        layout.padding = new RectOffset(10, 8, 8, 8);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var iconFrame = GetOrCreatePanel("IconFrame", row.transform, new Color(0.50f, 0.30f, 0.12f, 0.88f));
        SetLayoutSize(iconFrame, 52f, 52f);
        var icon = GetOrCreateImage("Icon", iconFrame.transform, Color.white);
        Stretch(icon.rectTransform, new Vector2(8f, 8f), new Vector2(-8f, -8f));

        var textBlock = GetOrCreateUI("TextBlock", row.transform);
        SetLayoutSize(textBlock, 190f, 58f);
        var textLayout = GetOrAdd<VerticalLayoutGroup>(textBlock);
        textLayout.spacing = 2f;
        textLayout.childAlignment = TextAnchor.MiddleLeft;
        textLayout.childControlWidth = true;
        textLayout.childControlHeight = false;
        textLayout.childForceExpandWidth = true;
        textLayout.childForceExpandHeight = false;

        var nameText = GetOrCreateText("NameText", textBlock.transform, "recipe.name", 18, TextAlignmentOptions.Left, LightTextColor);
        SetLayoutSize(nameText.gameObject, 190f, 24f);
        EnsureLocalizedText(nameText);

        var ingredientText = GetOrCreateText("IngredientText", textBlock.transform, "ingredient 0/1", 15, TextAlignmentOptions.Left, new Color(0.45f, 0.95f, 0.25f, 1f));
        ingredientText.enableWordWrapping = true;
        SetLayoutSize(ingredientText.gameObject, 190f, 32f);

        var levelText = GetOrCreateText("LevelText", row.transform, "Lv 1", 16, TextAlignmentOptions.Center, LightTextColor);
        SetLayoutSize(levelText.gameObject, 54f, 42f);

        var craftButton = CreateButton("CraftButton", row.transform, "Chọn", 15, new Vector2(58f, 42f), ButtonColor, LightTextColor);

        var rowUI = GetOrAdd<CraftingRecipeRowUI>(row);
        var serialized = new SerializedObject(rowUI);
        serialized.FindProperty("icon").objectReferenceValue = icon;
        serialized.FindProperty("nameText").objectReferenceValue = nameText;
        serialized.FindProperty("ingredientText").objectReferenceValue = ingredientText;
        serialized.FindProperty("levelText").objectReferenceValue = levelText;
        serialized.FindProperty("rowButton").objectReferenceValue = rowButton;
        serialized.FindProperty("craftButton").objectReferenceValue = craftButton;
        serialized.FindProperty("background").objectReferenceValue = row.GetComponent<Image>();
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(rowUI);

        return rowUI;
    }

    private static Button CreateCategoryButtonTemplate(Transform templates)
    {
        var button = CreateButton("CraftingCategoryButtonTemplate", templates, "Tất cả", 17, new Vector2(118f, 42f), ButtonColor, LightTextColor);
        button.gameObject.SetActive(false);
        return button;
    }

    private static GameObject CreateIngredientSlotTemplate(Transform templates)
    {
        var slot = GetOrCreatePanel("CraftingIngredientSlotTemplate", templates, new Color(0.33f, 0.20f, 0.09f, 0.92f));
        ClearChildren(slot.transform);
        slot.SetActive(false);
        SetLayoutSize(slot, 92f, 104f);
        AddOutline(slot, new Color(0.16f, 0.08f, 0.03f, 1f), new Vector2(2f, -2f));

        var icon = GetOrCreateImage("Icon", slot.transform, Color.white);
        SetupRect(icon.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(52f, 52f));

        var amountText = GetOrCreateText("AmountText", slot.transform, "0/0", 16, TextAlignmentOptions.Center, new Color(0.45f, 0.95f, 0.25f, 1f));
        SetupRect(amountText.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 32f), new Vector2(-10f, 22f));

        var nameText = GetOrCreateText("NameText", slot.transform, "Item", 13, TextAlignmentOptions.Center, LightTextColor);
        nameText.enableWordWrapping = true;
        SetupRect(nameText.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 7f), new Vector2(-10f, 24f));
        EnsureLocalizedText(nameText);
        return slot;
    }

    private static void BindCraftingPanelUI(GameObject root, CraftingRefs refs)
    {
        var ui = GetOrAdd<CraftingPanelUI>(root);
        var serialized = new SerializedObject(ui);
        serialized.FindProperty("panel").objectReferenceValue = refs.panel;
        serialized.FindProperty("titleText").objectReferenceValue = refs.titleText;
        serialized.FindProperty("resultText").objectReferenceValue = refs.resultText;
        serialized.FindProperty("closeButton").objectReferenceValue = refs.closeButton;
        serialized.FindProperty("categoryListRoot").objectReferenceValue = refs.categoryListRoot;
        serialized.FindProperty("categoryButtonTemplate").objectReferenceValue = refs.categoryButtonTemplate;
        serialized.FindProperty("recipeListRoot").objectReferenceValue = refs.recipeListRoot;
        serialized.FindProperty("recipeRowTemplate").objectReferenceValue = refs.recipeRowTemplate;
        serialized.FindProperty("selectedRecipeIcon").objectReferenceValue = refs.selectedRecipeIcon;
        serialized.FindProperty("selectedRecipeNameText").objectReferenceValue = refs.selectedRecipeNameText;
        serialized.FindProperty("selectedRecipeDescText").objectReferenceValue = refs.selectedRecipeDescText;
        serialized.FindProperty("selectedRecipeLevelText").objectReferenceValue = refs.selectedRecipeLevelText;
        serialized.FindProperty("selectedRecipeOutputText").objectReferenceValue = refs.selectedRecipeOutputText;
        serialized.FindProperty("ingredientListRoot").objectReferenceValue = refs.ingredientListRoot;
        serialized.FindProperty("ingredientSlotTemplate").objectReferenceValue = refs.ingredientSlotTemplate;
        serialized.FindProperty("quantityMinusButton").objectReferenceValue = refs.quantityMinusButton;
        serialized.FindProperty("quantityPlusButton").objectReferenceValue = refs.quantityPlusButton;
        serialized.FindProperty("quantityMaxButton").objectReferenceValue = refs.quantityMaxButton;
        serialized.FindProperty("quantityText").objectReferenceValue = refs.quantityText;
        serialized.FindProperty("craftButton").objectReferenceValue = refs.craftButton;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(ui);
    }

    private static Canvas GetOrCreateCanvas()
    {
        var canvasGO = GameObject.Find("Canvas_Windows") ?? GameObject.Find("HUD_Canvas");
        if (canvasGO == null)
        {
            var existingCanvas = Object.FindFirstObjectByType<Canvas>();
            canvasGO = existingCanvas != null ? existingCanvas.gameObject : null;
        }

        if (canvasGO == null)
        {
            var uiRoot = GameObject.Find("UIRoot") ?? new GameObject("UIRoot");
            canvasGO = new GameObject("Canvas_Windows", typeof(RectTransform));
            canvasGO.transform.SetParent(uiRoot.transform, false);
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Windows Canvas");
        }

        var canvas = GetOrAdd<Canvas>(canvasGO);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 20);

        var scaler = GetOrAdd<CanvasScaler>(canvasGO);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GetOrAdd<GraphicRaycaster>(canvasGO);
        return canvas;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
        Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
    }

    private static Transform GetOrCreateWindowsRoot(Transform canvas)
    {
        var root = canvas.Find("WindowsRoot");
        if (root != null) return root;

        var go = new GameObject("WindowsRoot", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create WindowsRoot");
        go.transform.SetParent(canvas, false);
        Stretch(go.GetComponent<RectTransform>());
        return go.transform;
    }

    private static Transform CreateScrollContent(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var scroll = GetOrCreatePanel(name, parent, new Color(0f, 0f, 0f, 0f));
        SetupRect(scroll, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);

        var scrollRect = GetOrAdd<ScrollRect>(scroll);
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;

        var viewport = GetOrCreatePanel("Viewport", scroll.transform, new Color(1f, 1f, 1f, 0.01f));
        Stretch(viewport.GetComponent<RectTransform>());
        var mask = GetOrAdd<Mask>(viewport);
        mask.showMaskGraphic = false;

        var content = GetOrCreateUI("Content", viewport.transform);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = contentRect;
        return content.transform;
    }

    private static Button CreateButton(string name, Transform parent, string text, int fontSize, Vector2 size, Color backgroundColor, Color textColor)
    {
        var go = GetOrCreatePanel(name, parent, backgroundColor);
        SetupRect(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        AddOutline(go, new Color(0.18f, 0.09f, 0.03f, 1f), new Vector2(2f, -2f));

        var button = GetOrAdd<Button>(go);
        var layoutElement = GetOrAdd<LayoutElement>(go);
        layoutElement.minWidth = size.x;
        layoutElement.minHeight = size.y;
        layoutElement.preferredWidth = size.x;
        layoutElement.preferredHeight = size.y;

        var label = GetOrCreateText("Label", go.transform, text, fontSize, TextAlignmentOptions.Center, textColor);
        Stretch(label.rectTransform, new Vector2(12f, 4f), new Vector2(-12f, -4f));
        return button;
    }

    private static GameObject GetOrCreateUI(string name, Transform parent)
    {
        var child = parent.Find(name);
        if (child != null)
            return child.gameObject;

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return go;
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }

    private static GameObject GetOrCreatePanel(string name, Transform parent, Color color)
    {
        var go = GetOrCreateUI(name, parent);
        var image = GetOrAdd<Image>(go);
        image.color = color;
        image.raycastTarget = true;
        return go;
    }

    private static Image GetOrCreateImage(string name, Transform parent, Color color)
    {
        var go = GetOrCreateUI(name, parent);
        var image = GetOrAdd<Image>(go);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI GetOrCreateText(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment, Color color)
    {
        var go = GetOrCreateUI(name, parent);
        var tmp = GetOrAdd<TextMeshProUGUI>(go);
        var font = LoadUiFont();
        tmp.text = text;
        if (font != null)
            tmp.font = font;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    private static TMP_FontAsset LoadUiFont()
    {
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiFontAssetPath);
    }

    private static void EnsureLocalizedText(TMP_Text text)
    {
        if (text == null)
            return;

        var localized = GetOrAdd<LocalizedText>(text.gameObject);
        var serialized = new SerializedObject(localized);
        serialized.FindProperty("targetText").objectReferenceValue = text;
        serialized.FindProperty("localizationKey").stringValue = text.text;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(localized);
    }

    private static void SetLayoutSize(GameObject go, float width, float height)
    {
        var layoutElement = GetOrAdd<LayoutElement>(go);
        layoutElement.minWidth = width;
        layoutElement.minHeight = height;
        layoutElement.preferredWidth = width;
        layoutElement.preferredHeight = height;
    }

    private static void SetupRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        SetupRect(go.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
    }

    private static void SetupRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void Stretch(RectTransform rect)
    {
        Stretch(rect, Vector2.zero, Vector2.zero);
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void AddOutline(GameObject go, Color color, Vector2 distance)
    {
        var outline = GetOrAdd<Outline>(go);
        outline.effectColor = color;
        outline.effectDistance = distance;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    private readonly struct CraftingRefs
    {
        public readonly GameObject panel;
        public readonly TMP_Text titleText;
        public readonly TMP_Text resultText;
        public readonly Button closeButton;
        public readonly Transform categoryListRoot;
        public readonly Button categoryButtonTemplate;
        public readonly Transform recipeListRoot;
        public readonly CraftingRecipeRowUI recipeRowTemplate;
        public readonly Image selectedRecipeIcon;
        public readonly TMP_Text selectedRecipeNameText;
        public readonly TMP_Text selectedRecipeDescText;
        public readonly TMP_Text selectedRecipeLevelText;
        public readonly TMP_Text selectedRecipeOutputText;
        public readonly Transform ingredientListRoot;
        public readonly GameObject ingredientSlotTemplate;
        public readonly Button quantityMinusButton;
        public readonly Button quantityPlusButton;
        public readonly Button quantityMaxButton;
        public readonly TMP_Text quantityText;
        public readonly Button craftButton;

        public CraftingRefs(
            GameObject panel,
            TMP_Text titleText,
            TMP_Text resultText,
            Button closeButton,
            Transform categoryListRoot,
            Button categoryButtonTemplate,
            Transform recipeListRoot,
            CraftingRecipeRowUI recipeRowTemplate,
            Image selectedRecipeIcon,
            TMP_Text selectedRecipeNameText,
            TMP_Text selectedRecipeDescText,
            TMP_Text selectedRecipeLevelText,
            TMP_Text selectedRecipeOutputText,
            Transform ingredientListRoot,
            GameObject ingredientSlotTemplate,
            Button quantityMinusButton,
            Button quantityPlusButton,
            Button quantityMaxButton,
            TMP_Text quantityText,
            Button craftButton)
        {
            this.panel = panel;
            this.titleText = titleText;
            this.resultText = resultText;
            this.closeButton = closeButton;
            this.categoryListRoot = categoryListRoot;
            this.categoryButtonTemplate = categoryButtonTemplate;
            this.recipeListRoot = recipeListRoot;
            this.recipeRowTemplate = recipeRowTemplate;
            this.selectedRecipeIcon = selectedRecipeIcon;
            this.selectedRecipeNameText = selectedRecipeNameText;
            this.selectedRecipeDescText = selectedRecipeDescText;
            this.selectedRecipeLevelText = selectedRecipeLevelText;
            this.selectedRecipeOutputText = selectedRecipeOutputText;
            this.ingredientListRoot = ingredientListRoot;
            this.ingredientSlotTemplate = ingredientSlotTemplate;
            this.quantityMinusButton = quantityMinusButton;
            this.quantityPlusButton = quantityPlusButton;
            this.quantityMaxButton = quantityMaxButton;
            this.quantityText = quantityText;
            this.craftButton = craftButton;
        }
    }
}
