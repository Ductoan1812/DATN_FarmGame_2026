using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class SetupNpcInteractionUI
{
    private const string DialogueChoiceButtonPrefabPath = "Assets/Project/Prefabs/UI/DialogueChoiceButtonTemplate.prefab";
    private const float DialogueOptionMinHeight = 54f;
    private const float DialogueOptionSpacing = 10f;

    private static readonly Color PanelColor = new(0.84f, 0.64f, 0.34f, 0.96f);
    private static readonly Color PanelDarkColor = new(0.28f, 0.17f, 0.08f, 0.95f);
    private static readonly Color ButtonColor = new(0.45f, 0.25f, 0.10f, 0.98f);
    private static readonly Color TextColor = new(0.14f, 0.08f, 0.04f, 1f);
    private static readonly Color LightTextColor = new(0.96f, 0.84f, 0.62f, 1f);

    [MenuItem("Tools/DATN/One-off Setup/UI/Create NPC Interaction UI")]
    public static void Execute()
    {
        var canvas = GetOrCreateCanvas();
        EnsureEventSystem();

        var windowsRoot = GetOrCreateWindowsRoot(canvas.transform);
        var root = GetOrCreateUI("NPCInteractionUI", windowsRoot);
        Stretch(root.GetComponent<RectTransform>());

        var templates = GetOrCreateUI("Templates", root.transform);
        templates.SetActive(false);

        var dialogue = BuildDialoguePanel(root.transform, templates.transform);
        var quest = BuildQuestPanel(root.transform, templates.transform);
        var shop = BuildShopPanel(root.transform, templates.transform);

        BindDialogueUI(root, dialogue);
        BindQuestPanelUI(root, quest);
        BindShopPanelUI(root, shop);

        dialogue.panel.SetActive(false);
        quest.panel.SetActive(false);
        shop.panel.SetActive(false);

        Undo.RegisterFullObjectHierarchyUndo(root, "Create NPC Interaction UI");
        Selection.activeGameObject = root;
        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[SetupNpcInteractionUI] Created NPC DialoguePanel-driven interaction UI and runtime bindings.");
    }

    private static DialogueRefs BuildDialoguePanel(Transform root, Transform templates)
    {
        var panel = GetOrCreatePanel("DialoguePanel", root, PanelColor);
        SetupRect(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(1120f, 250f));
        AddOutline(panel, new Color(0.25f, 0.13f, 0.05f, 1f), new Vector2(4f, -4f));

        var namePlate = GetOrCreatePanel("NamePlate", panel.transform, PanelDarkColor);
        SetupRect(namePlate, new Vector2(0.12f, 1f), new Vector2(0.36f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(0f, 46f));
        AddOutline(namePlate, new Color(0.18f, 0.09f, 0.03f, 1f), new Vector2(3f, -3f));

        var speakerText = GetOrCreateText("SpeakerNameText", namePlate.transform, "NPC", 28, TextAlignmentOptions.Center, LightTextColor);
        Stretch(speakerText.rectTransform, new Vector2(8f, 2f), new Vector2(-8f, -2f));
        EnsureLocalizedText(speakerText);

        var closeButton = CreateButton("CloseButton", panel.transform, "X", 28, new Vector2(46f, 46f), ButtonColor, LightTextColor);
        SetupRect(closeButton.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -12f), new Vector2(46f, 46f));

        var portraitFrame = GetOrCreatePanel("PortraitFrame", panel.transform, new Color(0.95f, 0.75f, 0.42f, 0.55f));
        SetupRect(portraitFrame, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(34f, -12f), new Vector2(190f, -54f));
        AddOutline(portraitFrame, new Color(0.33f, 0.18f, 0.07f, 1f), new Vector2(3f, -3f));

        var portraitImage = GetOrCreateImage("PortraitImage", portraitFrame.transform, new Color(1f, 1f, 1f, 0.22f));
        Stretch(portraitImage.rectTransform, new Vector2(10f, 10f), new Vector2(-10f, -10f));

        var lineText = GetOrCreateText("LineText", panel.transform, "dialogue.line.key", 27, TextAlignmentOptions.MidlineLeft, TextColor);
        SetupRect(lineText.gameObject, new Vector2(0f, 0f), new Vector2(0.69f, 1f), new Vector2(0f, 0.5f), new Vector2(260f, -15f), new Vector2(-280f, -84f));
        lineText.enableWordWrapping = true;
        EnsureLocalizedText(lineText);

        var choicesRoot = GetOrCreateUI("OptionsRoot", panel.transform);
        SetupRect(choicesRoot, new Vector2(0.71f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -70f), new Vector2(-24f, 0f));
        var verticalLayout = GetOrAdd<VerticalLayoutGroup>(choicesRoot);
        verticalLayout.spacing = DialogueOptionSpacing;
        verticalLayout.padding = new RectOffset(0, 0, 0, 0);
        verticalLayout.childAlignment = TextAnchor.UpperCenter;
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = true;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childForceExpandHeight = false;

        var choicesLayoutElement = GetOrAdd<LayoutElement>(choicesRoot);
        choicesLayoutElement.minHeight = 0f;
        choicesLayoutElement.preferredHeight = 0f;

        var contentSizeFitter = GetOrAdd<ContentSizeFitter>(choicesRoot);
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var choiceTemplate = GetDialogueChoiceButtonPrefab(templates);

        return new DialogueRefs(panel, speakerText, lineText, portraitImage, choicesRoot.transform, choiceTemplate, closeButton);
    }

    private static QuestRefs BuildQuestPanel(Transform root, Transform templates)
    {
        var panel = GetOrCreatePanel("QuestPanel", root, PanelColor);
        for (var i = panel.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(panel.transform.GetChild(i).gameObject);
        SetupRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 520f));
        AddOutline(panel, new Color(0.25f, 0.13f, 0.05f, 1f), new Vector2(4f, -4f));

        var titleText = GetOrCreateText("TitleText", panel.transform, "quest.title.key", 32, TextAlignmentOptions.Center, TextColor);
        SetupRect(titleText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(-110f, 48f));
        EnsureLocalizedText(titleText);

        var closeButton = CreateButton("CloseButton", panel.transform, "X", 24, new Vector2(40f, 40f), ButtonColor, LightTextColor);
        SetupRect(closeButton.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -14f), new Vector2(40f, 40f));

        var stateBadge = GetOrCreatePanel("StateBadge", panel.transform, PanelDarkColor);
        SetupRect(stateBadge, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(300f, 42f));
        AddOutline(stateBadge, new Color(0.18f, 0.09f, 0.03f, 1f), new Vector2(2f, -2f));

        var stateText = GetOrCreateText("StateText", stateBadge.transform, "ui.quest.state.not_started", 22, TextAlignmentOptions.Center, LightTextColor);
        Stretch(stateText.rectTransform, new Vector2(10f, 4f), new Vector2(-10f, -4f));
        EnsureLocalizedText(stateText);

        var descriptionText = GetOrCreateText("DescriptionText", panel.transform, "quest.description.key", 23, TextAlignmentOptions.TopLeft, TextColor);
        SetupRect(descriptionText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(-70f, 125f));
        descriptionText.enableWordWrapping = true;
        EnsureLocalizedText(descriptionText);

        var objectivesRoot = GetOrCreatePanel("ObjectivesRoot", panel.transform, new Color(0.95f, 0.75f, 0.42f, 0.28f));
        SetupRect(objectivesRoot, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 88f), new Vector2(-70f, 210f));
        var layout = GetOrAdd<VerticalLayoutGroup>(objectivesRoot);
        layout.spacing = 8f;
        layout.padding = new RectOffset(18, 18, 16, 16);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var objectiveTemplate = GetOrCreateText("QuestObjectiveTextTemplate", templates, "quest.objective.key 0/1", 21, TextAlignmentOptions.Left, TextColor);
        objectiveTemplate.enableWordWrapping = true;
        var layoutElement = GetOrAdd<LayoutElement>(objectiveTemplate.gameObject);
        layoutElement.preferredHeight = 32f;

        return new QuestRefs(panel, titleText, descriptionText, stateText, objectivesRoot.transform, objectiveTemplate, closeButton);
    }

    private static ShopRefs BuildShopPanel(Transform root, Transform templates)
    {
        var panel = GetOrCreatePanel("ShopPanel", root, PanelColor);
        SetupRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1220f, 720f));
        AddOutline(panel, new Color(0.22f, 0.12f, 0.04f, 1f), new Vector2(5f, -5f));

        var header = GetOrCreatePanel("HeaderBar", panel.transform, PanelDarkColor);
        SetupRect(header, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 58f));

        var titleText = GetOrCreateText("TitleText", header.transform, "ui.shop.title", 30, TextAlignmentOptions.Center, LightTextColor);
        Stretch(titleText.rectTransform, new Vector2(70f, 4f), new Vector2(-70f, -4f));
        EnsureLocalizedText(titleText);

        var closeButton = CreateButton("CloseButton", header.transform, "X", 28, new Vector2(50f, 50f), ButtonColor, LightTextColor);
        SetupRect(closeButton.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-8f, -4f), new Vector2(50f, 50f));

        var tabPanel = GetOrCreatePanel("TabPanel", panel.transform, new Color(0.50f, 0.28f, 0.10f, 0.94f));
        SetupRect(tabPanel, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(20f, -36f), new Vector2(170f, -104f));
        AddOutline(tabPanel, new Color(0.20f, 0.10f, 0.03f, 1f), new Vector2(3f, -3f));

        var tabLayout = GetOrAdd<VerticalLayoutGroup>(tabPanel);
        tabLayout.padding = new RectOffset(18, 18, 24, 18);
        tabLayout.spacing = 12f;
        tabLayout.childAlignment = TextAnchor.UpperCenter;
        tabLayout.childControlWidth = true;
        tabLayout.childControlHeight = false;
        tabLayout.childForceExpandWidth = true;
        tabLayout.childForceExpandHeight = false;

        var buyTabButton = CreateButton("BuyTabButton", tabPanel.transform, "ui.shop.buy", 24, new Vector2(132f, 58f), ButtonColor, LightTextColor);
        EnsureLocalizedText(buyTabButton.GetComponentInChildren<TMP_Text>(true));
        var sellTabButton = CreateButton("SellTabButton", tabPanel.transform, "ui.shop.sell", 24, new Vector2(132f, 58f), ButtonColor, LightTextColor);
        EnsureLocalizedText(sellTabButton.GetComponentInChildren<TMP_Text>(true));

        var moneyBox = GetOrCreatePanel("MoneyBox", tabPanel.transform, new Color(0.95f, 0.75f, 0.42f, 0.95f));
        SetupRect(moneyBox, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(-18f, 58f));
        AddOutline(moneyBox, new Color(0.31f, 0.17f, 0.06f, 1f), new Vector2(2f, -2f));
        GetOrAdd<LayoutElement>(moneyBox).ignoreLayout = true;
        var moneyText = GetOrCreateText("MoneyText", moneyBox.transform, "0", 24, TextAlignmentOptions.Center, TextColor);
        Stretch(moneyText.rectTransform, new Vector2(10f, 4f), new Vector2(-10f, -4f));

        var contentPanel = GetOrCreatePanel("ContentPanel", panel.transform, new Color(0.98f, 0.79f, 0.45f, 0.65f));
        SetupRect(contentPanel, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(92f, -36f), new Vector2(-226f, -104f));

        var buyPage = GetOrCreateUI("BuyPage", contentPanel.transform);
        Stretch(buyPage.GetComponent<RectTransform>(), new Vector2(24f, 24f), new Vector2(-24f, -24f));

        var buyListRoot = CreateScrollContent("BuyScrollView", buyPage.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var buyLayout = GetOrAdd<VerticalLayoutGroup>(buyListRoot.gameObject);
        buyLayout.padding = new RectOffset(0, 12, 0, 0);
        buyLayout.spacing = 8f;
        buyLayout.childAlignment = TextAnchor.UpperCenter;
        buyLayout.childControlWidth = true;
        buyLayout.childControlHeight = true;
        buyLayout.childForceExpandWidth = true;
        buyLayout.childForceExpandHeight = false;
        var buyFitter = GetOrAdd<ContentSizeFitter>(buyListRoot.gameObject);
        buyFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        buyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sellPage = GetOrCreateUI("SellPage", contentPanel.transform);
        Stretch(sellPage.GetComponent<RectTransform>(), new Vector2(24f, 24f), new Vector2(-24f, -24f));

        var inventoryPanel = GetOrCreatePanel("PlayerInventoryPanel", sellPage.transform, new Color(0.93f, 0.70f, 0.38f, 0.55f));
        SetupRect(inventoryPanel, new Vector2(0f, 0f), new Vector2(0.48f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-8f, 0f), new Vector2(-18f, 0f));
        AddOutline(inventoryPanel, new Color(0.42f, 0.23f, 0.08f, 1f), new Vector2(2f, -2f));

        var inventoryTitle = GetOrCreateText("InventoryTitleText", inventoryPanel.transform, "ui.shop.your_inventory", 24, TextAlignmentOptions.Center, TextColor);
        SetupRect(inventoryTitle.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(-24f, 38f));
        EnsureLocalizedText(inventoryTitle);

        var sellInventoryGridRoot = CreateScrollContent("SellInventoryScrollView", inventoryPanel.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -26f), new Vector2(-44f, -116f));
        var gridLayout = GetOrAdd<GridLayoutGroup>(sellInventoryGridRoot.gameObject);
        gridLayout.cellSize = new Vector2(76f, 76f);
        gridLayout.spacing = new Vector2(10f, 10f);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 5;
        gridLayout.childAlignment = TextAnchor.UpperCenter;

        var quickButton = CreateButton("QuickSelectButton", inventoryPanel.transform, "ui.shop.quick_select", 21, new Vector2(170f, 50f), ButtonColor, LightTextColor);
        SetupRect(quickButton.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 18f), new Vector2(170f, 50f));
        EnsureLocalizedText(quickButton.GetComponentInChildren<TMP_Text>(true));

        var arrowText = GetOrCreateText("SellArrowText", sellPage.transform, ">", 42, TextAlignmentOptions.Center, TextColor);
        SetupRect(arrowText.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(46f, 80f));

        var sellTargetPanel = GetOrCreatePanel("SellTargetPanel", sellPage.transform, new Color(0.93f, 0.70f, 0.38f, 0.55f));
        SetupRect(sellTargetPanel, new Vector2(0.52f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(8f, 0f), new Vector2(-18f, 0f));
        AddOutline(sellTargetPanel, new Color(0.42f, 0.23f, 0.08f, 1f), new Vector2(2f, -2f));

        var sellTargetTitle = GetOrCreateText("SellTargetTitleText", sellTargetPanel.transform, "ui.shop.sellable_items", 24, TextAlignmentOptions.Center, TextColor);
        SetupRect(sellTargetTitle.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(-24f, 38f));
        EnsureLocalizedText(sellTargetTitle);

        var selectedArea = GetOrCreatePanel("SelectedSellArea", sellTargetPanel.transform, new Color(0.78f, 0.53f, 0.26f, 0.35f));
        SetupRect(selectedArea, new Vector2(0f, 0.28f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -46f), new Vector2(-40f, -88f));
        AddOutline(selectedArea, new Color(0.42f, 0.23f, 0.08f, 1f), new Vector2(2f, -2f));

        var selectedSellIcon = GetOrCreateImage("SelectedSellIcon", selectedArea.transform, new Color(1f, 1f, 1f, 0.18f));
        SetupRect(selectedSellIcon.gameObject, new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96f, 96f));

        var selectedSellNameText = GetOrCreateText("SelectedSellNameText", selectedArea.transform, "", 22, TextAlignmentOptions.Center, TextColor);
        SetupRect(selectedSellNameText.gameObject, new Vector2(0f, 0.30f), new Vector2(1f, 0.30f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-28f, 30f));

        var selectedSellHintText = GetOrCreateText("SelectedSellHintText", selectedArea.transform, "ui.shop.sell_empty_hint", 21, TextAlignmentOptions.Center, TextColor);
        SetupRect(selectedSellHintText.gameObject, new Vector2(0f, 0.16f), new Vector2(1f, 0.16f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-70f, 70f));
        selectedSellHintText.enableWordWrapping = true;
        EnsureLocalizedText(selectedSellHintText);

        var totalPanel = GetOrCreatePanel("SellTotalPanel", sellTargetPanel.transform, new Color(0.95f, 0.75f, 0.42f, 0.45f));
        SetupRect(totalPanel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(-40f, 92f));

        var totalLabel = GetOrCreateText("TotalLabelText", totalPanel.transform, "ui.shop.total", 22, TextAlignmentOptions.Left, TextColor);
        SetupRect(totalLabel.gameObject, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(18f, -10f), new Vector2(160f, 30f));
        EnsureLocalizedText(totalLabel);

        var selectedSellTotalText = GetOrCreateText("SelectedSellTotalText", totalPanel.transform, "0", 25, TextAlignmentOptions.Left, TextColor);
        SetupRect(selectedSellTotalText.gameObject, new Vector2(0f, 0f), new Vector2(0.45f, 0f), new Vector2(0f, 0f), new Vector2(54f, 18f), new Vector2(120f, 34f));

        var sellButton = CreateButton("SellButton", totalPanel.transform, "ui.shop.sell", 23, new Vector2(180f, 54f), ButtonColor, LightTextColor);
        SetupRect(sellButton.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(180f, 54f));
        EnsureLocalizedText(sellButton.GetComponentInChildren<TMP_Text>(true));

        var buyRowTemplate = CreateShopBuyRowTemplate(templates);
        var sellSlotTemplate = CreateShopSellSlotTemplate(templates);

        return new ShopRefs(
            panel,
            titleText,
            moneyText,
            closeButton,
            buyTabButton,
            sellTabButton,
            buyPage,
            sellPage,
            buyListRoot,
            buyRowTemplate,
            sellInventoryGridRoot,
            sellSlotTemplate,
            selectedSellIcon,
            selectedSellNameText,
            selectedSellHintText,
            selectedSellTotalText,
            sellButton);
    }

    private static void BindDialogueUI(GameObject root, DialogueRefs refs)
    {
        var ui = GetOrAdd<DialoguePanelUI>(root);
        var serialized = new SerializedObject(ui);
        serialized.FindProperty("panel").objectReferenceValue = refs.panel;
        serialized.FindProperty("speakerNameText").objectReferenceValue = refs.speakerNameText;
        serialized.FindProperty("lineText").objectReferenceValue = refs.lineText;
        serialized.FindProperty("portraitImage").objectReferenceValue = refs.portraitImage;
        serialized.FindProperty("optionsRoot").objectReferenceValue = refs.choicesRoot;
        serialized.FindProperty("optionButtonPrefab").objectReferenceValue = refs.choiceButtonPrefab;
        serialized.FindProperty("closeButton").objectReferenceValue = refs.closeButton;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(ui);
    }

    private static void BindQuestPanelUI(GameObject root, QuestRefs refs)
    {
        var ui = GetOrAdd<QuestPanelUI>(root);
        var serialized = new SerializedObject(ui);
        serialized.FindProperty("panel").objectReferenceValue = refs.panel;
        serialized.FindProperty("titleText").objectReferenceValue = refs.titleText;
        serialized.FindProperty("descriptionText").objectReferenceValue = refs.descriptionText;
        serialized.FindProperty("stateText").objectReferenceValue = refs.stateText;
        serialized.FindProperty("objectivesRoot").objectReferenceValue = refs.objectivesRoot;
        serialized.FindProperty("objectiveTextPrefab").objectReferenceValue = refs.objectiveTextPrefab;
        serialized.FindProperty("closeButton").objectReferenceValue = refs.closeButton;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(ui);
    }

    private static void BindShopPanelUI(GameObject root, ShopRefs refs)
    {
        var ui = GetOrAdd<ShopPanelUI>(root);
        var serialized = new SerializedObject(ui);
        serialized.FindProperty("panel").objectReferenceValue = refs.panel;
        serialized.FindProperty("titleText").objectReferenceValue = refs.titleText;
        serialized.FindProperty("moneyText").objectReferenceValue = refs.moneyText;
        serialized.FindProperty("closeButton").objectReferenceValue = refs.closeButton;
        serialized.FindProperty("buyTabButton").objectReferenceValue = refs.buyTabButton;
        serialized.FindProperty("sellTabButton").objectReferenceValue = refs.sellTabButton;
        serialized.FindProperty("buyPage").objectReferenceValue = refs.buyPage;
        serialized.FindProperty("sellPage").objectReferenceValue = refs.sellPage;
        serialized.FindProperty("buyListRoot").objectReferenceValue = refs.buyListRoot;
        serialized.FindProperty("buyRowTemplate").objectReferenceValue = refs.buyRowTemplate;
        serialized.FindProperty("sellInventoryGridRoot").objectReferenceValue = refs.sellInventoryGridRoot;
        serialized.FindProperty("sellSlotTemplate").objectReferenceValue = refs.sellSlotTemplate;
        serialized.FindProperty("selectedSellIcon").objectReferenceValue = refs.selectedSellIcon;
        serialized.FindProperty("selectedSellNameText").objectReferenceValue = refs.selectedSellNameText;
        serialized.FindProperty("selectedSellHintText").objectReferenceValue = refs.selectedSellHintText;
        serialized.FindProperty("selectedSellTotalText").objectReferenceValue = refs.selectedSellTotalText;
        serialized.FindProperty("sellButton").objectReferenceValue = refs.sellButton;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(ui);
    }

    private static Canvas GetOrCreateCanvas()
    {
        var canvasGO = GameObject.Find("Canvas_Windows");
        if (canvasGO == null)
            canvasGO = GameObject.Find("HUD_Canvas");
        if (canvasGO == null)
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            canvasGO = canvas != null ? canvas.gameObject : null;
        }

        if (canvasGO == null)
        {
            var uiRoot = GameObject.Find("UIRoot") ?? new GameObject("UIRoot");
            canvasGO = new GameObject("Canvas_Windows", typeof(RectTransform));
            canvasGO.transform.SetParent(uiRoot.transform, false);
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Windows Canvas");
        }

        var canvasComponent = GetOrAdd<Canvas>(canvasGO);
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasComponent.sortingOrder = Mathf.Max(canvasComponent.sortingOrder, 20);

        var scaler = GetOrAdd<CanvasScaler>(canvasGO);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GetOrAdd<GraphicRaycaster>(canvasGO);
        return canvasComponent;
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

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
        Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
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
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        return tmp;
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

    private static Button GetDialogueChoiceButtonPrefab(Transform templates)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialogueChoiceButtonPrefabPath);
        var button = prefab != null ? prefab.GetComponent<Button>() : null;
        if (button != null)
            return button;

        Debug.LogWarning(
            $"[SetupNpcInteractionUI] Khong tim thay Button prefab tai '{DialogueChoiceButtonPrefabPath}'. " +
            "Se tao scene template fallback.");

        var fallback = CreateButton("DialogueChoiceButtonTemplate", templates, "dialogue.choice.key", 21, new Vector2(320f, 54f), ButtonColor, LightTextColor);
        var layoutElement = GetOrAdd<LayoutElement>(fallback.gameObject);
        layoutElement.minHeight = DialogueOptionMinHeight;
        EnsureLocalizedText(fallback.GetComponentInChildren<TMP_Text>(true));
        return fallback;
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

    private static Button CreateShopBuyRowTemplate(Transform templates)
    {
        var row = CreateButton("ShopBuyRowTemplate", templates, "", 18, new Vector2(900f, 84f), new Color(0.96f, 0.76f, 0.42f, 0.85f), TextColor);
        HideGeneratedButtonLabel(row);

        var rowLayoutElement = GetOrAdd<LayoutElement>(row.gameObject);
        rowLayoutElement.minHeight = 78f;
        rowLayoutElement.preferredHeight = 84f;

        var rowLayout = GetOrAdd<HorizontalLayoutGroup>(row.gameObject);
        rowLayout.padding = new RectOffset(18, 12, 8, 8);
        rowLayout.spacing = 14f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        var iconFrame = GetOrCreatePanel("IconFrame", row.transform, new Color(0.50f, 0.30f, 0.12f, 0.88f));
        SetLayoutSize(iconFrame, 64f, 64f);
        var icon = GetOrCreateImage("Icon", iconFrame.transform, Color.white);
        Stretch(icon.rectTransform, new Vector2(8f, 8f), new Vector2(-8f, -8f));

        var textBlock = GetOrCreateUI("TextBlock", row.transform);
        SetLayoutSize(textBlock, 330f, 66f);
        var textLayout = GetOrAdd<VerticalLayoutGroup>(textBlock);
        textLayout.spacing = 2f;
        textLayout.childAlignment = TextAnchor.MiddleLeft;
        textLayout.childControlWidth = true;
        textLayout.childControlHeight = false;
        textLayout.childForceExpandWidth = true;
        textLayout.childForceExpandHeight = false;

        var nameText = GetOrCreateText("NameText", textBlock.transform, "Item Name", 24, TextAlignmentOptions.Left, TextColor);
        SetLayoutSize(nameText.gameObject, 330f, 30f);
        var descriptionText = GetOrCreateText("DescriptionText", textBlock.transform, "Item description", 18, TextAlignmentOptions.Left, TextColor);
        descriptionText.enableWordWrapping = true;
        SetLayoutSize(descriptionText.gameObject, 330f, 30f);

        var priceBlock = GetOrCreateUI("PriceBlock", row.transform);
        SetLayoutSize(priceBlock, 140f, 50f);
        var priceLayout = GetOrAdd<HorizontalLayoutGroup>(priceBlock);
        priceLayout.spacing = 8f;
        priceLayout.childAlignment = TextAnchor.MiddleCenter;
        priceLayout.childControlWidth = false;
        priceLayout.childControlHeight = false;
        var coinIcon = GetOrCreateImage("CoinIcon", priceBlock.transform, new Color(1f, 0.78f, 0.15f, 1f));
        SetLayoutSize(coinIcon.gameObject, 30f, 30f);
        var priceText = GetOrCreateText("PriceText", priceBlock.transform, "0", 24, TextAlignmentOptions.Left, TextColor);
        SetLayoutSize(priceText.gameObject, 80f, 32f);

        var quantityBlock = GetOrCreateUI("QuantityBlock", row.transform);
        SetLayoutSize(quantityBlock, 180f, 54f);
        var quantityLayout = GetOrAdd<HorizontalLayoutGroup>(quantityBlock);
        quantityLayout.spacing = 8f;
        quantityLayout.childAlignment = TextAnchor.MiddleCenter;
        quantityLayout.childControlWidth = false;
        quantityLayout.childControlHeight = false;

        CreateButton("MinusButton", quantityBlock.transform, "-", 28, new Vector2(44f, 44f), ButtonColor, LightTextColor);
        var amountText = GetOrCreateText("AmountText", quantityBlock.transform, "1", 24, TextAlignmentOptions.Center, TextColor);
        SetLayoutSize(amountText.gameObject, 46f, 44f);
        CreateButton("PlusButton", quantityBlock.transform, "+", 28, new Vector2(44f, 44f), ButtonColor, LightTextColor);

        var buyButton = CreateButton("BuyButton", row.transform, "ui.shop.buy", 24, new Vector2(120f, 54f), new Color(0.35f, 0.55f, 0.12f, 1f), LightTextColor);
        EnsureLocalizedText(buyButton.GetComponentInChildren<TMP_Text>(true));

        return row;
    }

    private static Button CreateShopSellSlotTemplate(Transform templates)
    {
        var slot = CreateButton("ShopSellSlotTemplate", templates, "", 16, new Vector2(76f, 76f), new Color(0.26f, 0.15f, 0.06f, 0.95f), LightTextColor);
        HideGeneratedButtonLabel(slot);

        var layoutElement = GetOrAdd<LayoutElement>(slot.gameObject);
        layoutElement.minWidth = 76f;
        layoutElement.minHeight = 76f;
        layoutElement.preferredWidth = 76f;
        layoutElement.preferredHeight = 76f;

        var icon = GetOrCreateImage("Icon", slot.transform, Color.white);
        Stretch(icon.rectTransform, new Vector2(8f, 8f), new Vector2(-8f, -8f));

        var amountText = GetOrCreateText("AmountText", slot.transform, "", 18, TextAlignmentOptions.BottomRight, LightTextColor);
        Stretch(amountText.rectTransform, new Vector2(4f, 4f), new Vector2(-6f, -4f));

        return slot;
    }

    private static void HideGeneratedButtonLabel(Button button)
    {
        var label = button != null ? button.transform.Find("Label") : null;
        if (label != null)
            label.gameObject.SetActive(false);
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

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    private readonly struct DialogueRefs
    {
        public readonly GameObject panel;
        public readonly TMP_Text speakerNameText;
        public readonly TMP_Text lineText;
        public readonly Image portraitImage;
        public readonly Transform choicesRoot;
        public readonly Button choiceButtonPrefab;
        public readonly Button closeButton;

        public DialogueRefs(GameObject panel, TMP_Text speakerNameText, TMP_Text lineText, Image portraitImage, Transform choicesRoot, Button choiceButtonPrefab, Button closeButton)
        {
            this.panel = panel;
            this.speakerNameText = speakerNameText;
            this.lineText = lineText;
            this.portraitImage = portraitImage;
            this.choicesRoot = choicesRoot;
            this.choiceButtonPrefab = choiceButtonPrefab;
            this.closeButton = closeButton;
        }
    }

    private readonly struct QuestRefs
    {
        public readonly GameObject panel;
        public readonly TMP_Text titleText;
        public readonly TMP_Text descriptionText;
        public readonly TMP_Text stateText;
        public readonly Transform objectivesRoot;
        public readonly TMP_Text objectiveTextPrefab;
        public readonly Button closeButton;

        public QuestRefs(GameObject panel, TMP_Text titleText, TMP_Text descriptionText, TMP_Text stateText, Transform objectivesRoot, TMP_Text objectiveTextPrefab, Button closeButton)
        {
            this.panel = panel;
            this.titleText = titleText;
            this.descriptionText = descriptionText;
            this.stateText = stateText;
            this.objectivesRoot = objectivesRoot;
            this.objectiveTextPrefab = objectiveTextPrefab;
            this.closeButton = closeButton;
        }
    }

    private readonly struct ShopRefs
    {
        public readonly GameObject panel;
        public readonly TMP_Text titleText;
        public readonly TMP_Text moneyText;
        public readonly Button closeButton;
        public readonly Button buyTabButton;
        public readonly Button sellTabButton;
        public readonly GameObject buyPage;
        public readonly GameObject sellPage;
        public readonly Transform buyListRoot;
        public readonly Button buyRowTemplate;
        public readonly Transform sellInventoryGridRoot;
        public readonly Button sellSlotTemplate;
        public readonly Image selectedSellIcon;
        public readonly TMP_Text selectedSellNameText;
        public readonly TMP_Text selectedSellHintText;
        public readonly TMP_Text selectedSellTotalText;
        public readonly Button sellButton;

        public ShopRefs(
            GameObject panel,
            TMP_Text titleText,
            TMP_Text moneyText,
            Button closeButton,
            Button buyTabButton,
            Button sellTabButton,
            GameObject buyPage,
            GameObject sellPage,
            Transform buyListRoot,
            Button buyRowTemplate,
            Transform sellInventoryGridRoot,
            Button sellSlotTemplate,
            Image selectedSellIcon,
            TMP_Text selectedSellNameText,
            TMP_Text selectedSellHintText,
            TMP_Text selectedSellTotalText,
            Button sellButton)
        {
            this.panel = panel;
            this.titleText = titleText;
            this.moneyText = moneyText;
            this.closeButton = closeButton;
            this.buyTabButton = buyTabButton;
            this.sellTabButton = sellTabButton;
            this.buyPage = buyPage;
            this.sellPage = sellPage;
            this.buyListRoot = buyListRoot;
            this.buyRowTemplate = buyRowTemplate;
            this.sellInventoryGridRoot = sellInventoryGridRoot;
            this.sellSlotTemplate = sellSlotTemplate;
            this.selectedSellIcon = selectedSellIcon;
            this.selectedSellNameText = selectedSellNameText;
            this.selectedSellHintText = selectedSellHintText;
            this.selectedSellTotalText = selectedSellTotalText;
            this.sellButton = sellButton;
        }
    }
}
