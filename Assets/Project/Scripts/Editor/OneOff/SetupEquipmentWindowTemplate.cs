using Assets.HeroEditor4D.Common.Scripts.Enums;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SetupEquipmentWindowTemplate
{
    private static readonly Color Clear = new(1f, 1f, 1f, 0f);
    private static readonly Color Panel = new(0.50f, 0.31f, 0.12f, 0.96f);
    private static readonly Color PanelDark = new(0.20f, 0.12f, 0.05f, 0.96f);
    private static readonly Color PanelMid = new(0.33f, 0.20f, 0.08f, 0.96f);
    private static readonly Color Frame = new(0.78f, 0.54f, 0.20f, 1f);
    private static readonly Color Text = new(1f, 0.90f, 0.68f, 1f);

    [MenuItem("Tools/DATN/One-off Setup/UI/Create Equipment Window Template")]
    public static void Execute()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[SetupEquipmentWindowTemplate] Hãy thoát Play Mode rồi chạy lại tool tạo EquipmentWindow_Template.");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        var windowsRoot = GetWindowsRoot();

        var existing = windowsRoot.Find("EquipmentWindow_Template");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        var window = PanelObject("EquipmentWindow_Template", windowsRoot, Panel);
        var rt = window.GetComponent<RectTransform>();
        Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(1180f, 720f));
        AddOutline(window, Frame, 3f);
        var equipmentUi = window.AddComponent<EquipmentUI>();
        window.SetActive(false);

        BuildHeader(window.transform);
        BuildBody(window.transform);
        AssignEquipmentUIRefs(equipmentUi, window.transform);

        EditorUtility.SetDirty(window);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveOpenScenes();
        Selection.activeGameObject = window;
        Debug.Log("[SetupEquipmentWindowTemplate] Created EquipmentWindow_Template under Canvas_Windows/WindowsRoot.");
    }

    private static void BuildHeader(Transform window)
    {
        var header = PanelObject("Header", window, Clear);
        Stretch(header, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(0f, 86f));

        var title = PanelObject("TitleBanner", header.transform, new Color(0.36f, 0.21f, 0.08f, 1f));
        Anchor(title.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-210f, 0f), new Vector2(390f, 74f));
        AddOutline(title, Frame, 2f);

        var titleIcon = PanelObject("TitleIcon", title.transform, new Color(0.16f, 0.09f, 0.03f, 0.65f));
        Anchor(titleIcon.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(54f, 54f));
        AddOutline(titleIcon, Frame, 1.5f);
        Label("IconText", titleIcon.transform, "E", 30, TextAlignmentOptions.Center, Text, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        Stretch(titleIcon.transform.Find("IconText").gameObject);

        Label("TitleText", title.transform, "EQUIPMENT", 40, TextAlignmentOptions.Center, Text, new Vector2(44f, 0f), new Vector2(300f, 64f), new Vector2(0.5f, 0.5f));

        var close = Button("CloseButton", header.transform, "X", 36, new Color(0.36f, 0.22f, 0.09f, 1f), Text);
        Anchor(close.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-22f, 0f), new Vector2(64f, 64f));
    }

    private static void BuildBody(Transform window)
    {
        var body = PanelObject("Body", window, Clear);
        Stretch(body, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(-44f, -156f));

        var equipmentPanel = PanelObject("EquipmentPanel", body.transform, new Color(0.72f, 0.48f, 0.19f, 0.62f));
        Anchor(equipmentPanel.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.62f, 1f), new Vector2(0f, 0f), Vector2.zero, new Vector2(-14f, 0f));
        AddOutline(equipmentPanel, Frame, 2f);

        BuildEquipmentPanel(equipmentPanel.transform);
        BuildStatsPanel(body.transform);
    }

    private static void BuildEquipmentPanel(Transform parent)
    {
        var preview = PanelObject("CharacterPreviewFrame", parent, PanelDark);
        Anchor(preview.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(280f, 430f));
        AddOutline(preview, Frame, 2f);

        Label("PlayerAvatarPreview", preview.transform, "AVATAR", 34, TextAlignmentOptions.Center, new Color(1f, 0.84f, 0.52f, 0.55f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        Stretch(preview.transform.Find("PlayerAvatarPreview").gameObject);

        CreateSlot(parent, "Slot_Helmet", EquipmentPart.Helmet, "Mũ", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f));
        CreateSlot(parent, "Slot_Armor", EquipmentPart.Armor, "Giáp", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 88f));
        CreateSlot(parent, "Slot_Leggings", EquipmentPart.Leggings, "Quần", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 152f));

        CreateSlot(parent, "Slot_MainHand", EquipmentPart.MeleeWeapon1H, "Tay chính", new Vector2(0f, 0.62f), new Vector2(0f, 0.62f), new Vector2(46f, 0f));
        CreateSlot(parent, "Slot_OffHand", EquipmentPart.Shield, "Tay phụ", new Vector2(1f, 0.62f), new Vector2(1f, 0.62f), new Vector2(-46f, 0f));
        CreateSlot(parent, "Slot_Bracers", EquipmentPart.Bracers, "Tay", new Vector2(0f, 0.34f), new Vector2(0f, 0.34f), new Vector2(46f, 0f));
        CreateSlot(parent, "Slot_Back", EquipmentPart.Back, "Lưng", new Vector2(1f, 0.34f), new Vector2(1f, 0.34f), new Vector2(-46f, 0f));

        CreateSlot(parent, "Slot_Earrings", EquipmentPart.Earrings, "Bông tai", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f, -34f));
        CreateSlot(parent, "Slot_Mask", EquipmentPart.Mask, "Mặt nạ", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-46f, -34f));
        CreateSlot(parent, "Slot_Wings", EquipmentPart.Wings, "Cánh", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-46f, 34f));
        CreateSlot(parent, "Slot_Cape", EquipmentPart.Cape, "Áo choàng", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(46f, 34f));
    }

    private static void BuildStatsPanel(Transform body)
    {
        var panel = PanelObject("StatsPanel", body, new Color(0.29f, 0.17f, 0.06f, 0.98f));
        Anchor(panel.GetComponent<RectTransform>(), new Vector2(0.62f, 0f), new Vector2(1f, 1f), new Vector2(0.62f, 0f), new Vector2(14f, 0f), Vector2.zero);
        AddOutline(panel, Frame, 2f);

        var header = PanelObject("StatsHeader", panel.transform, new Color(0.36f, 0.22f, 0.09f, 1f));
        Anchor(header.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 62f));
        AddOutline(header, Frame, 1.5f);
        Label("HeaderText", header.transform, "CHỈ SỐ NHÂN VẬT", 26, TextAlignmentOptions.Center, Text, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        Stretch(header.transform.Find("HeaderText").gameObject);

        Label("PlayerNameText", panel.transform, "Player", 24, TextAlignmentOptions.Center, new Color(0.42f, 0.82f, 1f, 1f), new Vector2(0f, -76f), new Vector2(0f, 34f), new Vector2(0.5f, 1f));
        var playerNameRt = panel.transform.Find("PlayerNameText").GetComponent<RectTransform>();
        playerNameRt.anchorMin = new Vector2(0f, 1f);
        playerNameRt.anchorMax = new Vector2(1f, 1f);
        playerNameRt.offsetMin = new Vector2(18f, playerNameRt.offsetMin.y);
        playerNameRt.offsetMax = new Vector2(-18f, playerNameRt.offsetMax.y);

        var scrollGo = PanelObject("StatsScrollView", panel.transform, Clear);
        Anchor(scrollGo.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -58f), new Vector2(-34f, -148f));

        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewport = PanelObject("Viewport", scrollGo.transform, new Color(1f, 1f, 1f, 0.01f));
        Stretch(viewport);
        viewport.AddComponent<RectMask2D>();
        scroll.viewport = viewport.GetComponent<RectTransform>();

        var content = PanelObject("Content", viewport.transform, Clear);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 520f);
        scroll.content = contentRt;

        var layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 14, 14);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateStatRow(content.transform, "HP", "320 / 320");
        CreateStatRow(content.transform, "MP", "120 / 120");
        CreateStatRow(content.transform, "EXP", "650 / 1500");
        CreateStatRow(content.transform, "Tấn công", "0");
        CreateStatRow(content.transform, "Phòng thủ", "0");
        CreateStatRow(content.transform, "Tốc độ", "0");
        CreateStatRow(content.transform, "Chí mạng", "0%");
    }

    private static void AssignEquipmentUIRefs(EquipmentUI equipmentUi, Transform window)
    {
        if (equipmentUi == null || window == null)
            return;

        var serialized = new SerializedObject(equipmentUi);
        var playerName = window.Find("Body/StatsPanel/PlayerNameText")?.GetComponent<TMP_Text>();
        var content = window.Find("Body/StatsPanel/StatsScrollView/Viewport/Content");
        var statDatabase = AssetDatabase.LoadAssetAtPath<StatDefinitionDatabase>("Assets/Project/Resources/StatDefinitionDatabase.asset");
        var statRowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Prefabs/UI/attributeInfo.prefab")?.GetComponent<StatRowUI>();

        serialized.FindProperty("playerNameInfo").objectReferenceValue = playerName != null ? playerName.gameObject : null;
        serialized.FindProperty("playerNameText").objectReferenceValue = playerName;
        serialized.FindProperty("statsContent").objectReferenceValue = content;
        serialized.FindProperty("statDatabase").objectReferenceValue = statDatabase;
        serialized.FindProperty("statRowPrefab").objectReferenceValue = statRowPrefab;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateSlot(Transform parent, string name, EquipmentPart part, string label, Vector2 anchor, Vector2 pivot, Vector2 position)
    {
        var slot = PanelObject(name, parent, PanelDark);
        var rt = slot.GetComponent<RectTransform>();
        Anchor(rt, anchor, anchor, pivot, position, new Vector2(104f, 104f));
        AddOutline(slot, new Color(0.43f, 0.26f, 0.10f, 1f), 1.5f);
        var button = slot.AddComponent<Button>();
        button.targetGraphic = slot.GetComponent<Image>();

        var equipmentSlot = slot.AddComponent<EquipmentSlotUI>();

        var empty = PanelObject("EmptyState", slot.transform, new Color(0.10f, 0.06f, 0.02f, 0.72f));
        Stretch(empty, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-12f, -12f));
        Label("Label", empty.transform, label, 17, TextAlignmentOptions.Center, new Color(1f, 0.90f, 0.68f, 0.82f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        Stretch(empty.transform.Find("Label").gameObject);

        var icon = PanelObject("Icon", slot.transform, Clear);
        Stretch(icon, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16f, -16f));
        icon.GetComponent<Image>().raycastTarget = false;

        var serialized = new SerializedObject(equipmentSlot);
        serialized.FindProperty("equipmentPart").enumValueIndex = (int)part;
        serialized.FindProperty("icon").objectReferenceValue = icon.GetComponent<Image>();
        serialized.FindProperty("emptyState").objectReferenceValue = empty;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateStatRow(Transform parent, string name, string value)
    {
        var row = PanelObject($"StatRow_{name}", parent, PanelMid);
        row.AddComponent<LayoutElement>().preferredHeight = 48f;
        AddOutline(row, new Color(0.43f, 0.26f, 0.10f, 1f), 1.2f);

        Label("Name", row.transform, name, 20, TextAlignmentOptions.MidlineLeft, Text, new Vector2(18f, 0f), new Vector2(220f, 42f), new Vector2(0f, 0.5f));
        Label("Value", row.transform, value, 20, TextAlignmentOptions.MidlineRight, Color.white, new Vector2(-18f, 0f), new Vector2(150f, 42f), new Vector2(1f, 0.5f));
        var valueRt = row.transform.Find("Value").GetComponent<RectTransform>();
        valueRt.anchorMin = valueRt.anchorMax = new Vector2(1f, 0.5f);
    }

    private static Transform GetWindowsRoot()
    {
        var uiRoot = GameObject.Find("UIRoot");
        if (uiRoot == null)
            uiRoot = new GameObject("UIRoot");

        var canvasTransform = uiRoot.transform.Find("Canvas_Windows");
        GameObject canvasGo;
        if (canvasTransform == null)
        {
            canvasGo = new GameObject("Canvas_Windows", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(uiRoot.transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }
        else
        {
            canvasGo = canvasTransform.gameObject;
        }

        var root = canvasGo.transform.Find("WindowsRoot");
        if (root != null) return root;

        var rootGo = new GameObject("WindowsRoot", typeof(RectTransform));
        rootGo.transform.SetParent(canvasGo.transform, false);
        Stretch(rootGo);
        return rootGo.transform;
    }

    private static GameObject PanelObject(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = color.a > 0.001f;
        return go;
    }

    private static Button Button(string name, Transform parent, string text, int fontSize, Color backgroundColor, Color textColor)
    {
        var go = PanelObject(name, parent, backgroundColor);
        AddOutline(go, Frame, 1.5f);
        go.AddComponent<Button>();
        var label = Label("Text", go.transform, text, fontSize, TextAlignmentOptions.Center, textColor, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        Stretch(label);
        return go.GetComponent<Button>();
    }

    private static GameObject Label(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment, Color color, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 pivot)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
        return go;
    }

    private static void AddOutline(GameObject go, Color color, float distance)
    {
        var outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(distance, -distance);
    }

    private static void Anchor(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
    }

    private static void Stretch(GameObject go)
    {
        Stretch(go, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
    }

    private static void Stretch(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
        rt.offsetMin = sizeDelta == Vector2.zero ? Vector2.zero : rt.offsetMin;
        rt.offsetMax = sizeDelta == Vector2.zero ? Vector2.zero : rt.offsetMax;
    }
}
