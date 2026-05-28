using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SetupBackpackWindowTemplate
{
    private static readonly Color Clear = new(1f, 1f, 1f, 0f);
    private static readonly Color Panel = new(0.50f, 0.31f, 0.12f, 0.96f);
    private static readonly Color PanelDark = new(0.20f, 0.12f, 0.05f, 0.96f);
    private static readonly Color PanelMid = new(0.33f, 0.20f, 0.08f, 0.96f);
    private static readonly Color Frame = new(0.78f, 0.54f, 0.20f, 1f);
    private static readonly Color Text = new(1f, 0.90f, 0.68f, 1f);

    [MenuItem("Tools/DATN/One-off Setup/UI/Create Backpack Window Template")]
    public static void Execute()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[SetupBackpackWindowTemplate] Hãy thoát Play Mode rồi chạy lại tool tạo BackpackWindow_Template.");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        var windowsRoot = GetWindowsRoot();

        var existing = windowsRoot.Find("BackpackWindow_Template");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        var window = PanelObject("BackpackWindow_Template", windowsRoot, Panel);
        var rt = window.GetComponent<RectTransform>();
        Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(1380f, 720f));
        AddOutline(window, Frame, 3f);
        window.SetActive(false);

        BuildHeader(window.transform);
        BuildContent(window.transform);
        BuildHotbarDock(window.transform);

        EditorUtility.SetDirty(window);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveOpenScenes();
        Selection.activeGameObject = window;
        Debug.Log("[SetupBackpackWindowTemplate] Created BackpackWindow_Template under Canvas_Windows/WindowsRoot.");
    }

    private static void BuildHeader(Transform window)
    {
        var header = PanelObject("Header", window, Clear);
        Stretch(header, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(0f, 88f));

        var title = PanelObject("TitleBanner", header.transform, new Color(0.36f, 0.21f, 0.08f, 1f));
        Anchor(title.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-270f, 0f), new Vector2(380f, 76f));
        AddOutline(title, Frame, 2f);

        var titleIcon = PanelObject("TitleIcon", title.transform, new Color(0.16f, 0.09f, 0.03f, 0.65f));
        Anchor(titleIcon.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(54f, 54f));
        AddOutline(titleIcon, Frame, 1.5f);

        Label("TitleText", title.transform, "BACKPACK", 42, TextAlignmentOptions.Center, Text, Vector2.zero, new Vector2(260f, 64f), new Vector2(0.5f, 0.5f));

        var close = Button("CloseButton", header.transform, "X", 36, new Color(0.36f, 0.22f, 0.09f, 1f), Text);
        Anchor(close.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-22f, 0f), new Vector2(64f, 64f));
    }

    private static void BuildContent(Transform window)
    {
        var body = PanelObject("Body", window, Clear);
        Stretch(body, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(-44f, -180f));

        var gridPanel = PanelObject("GridPanel", body.transform, new Color(0.72f, 0.48f, 0.19f, 0.62f));
        Anchor(gridPanel.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.70f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(-12f, 0f));
        AddOutline(gridPanel, Frame, 2f);

        BuildSlotScroll(gridPanel.transform);
        BuildGridFooter(gridPanel.transform);
        BuildInfoPanel(body.transform);
    }

    private static void BuildSlotScroll(Transform gridPanel)
    {
        var scrollGo = PanelObject("SlotScrollView", gridPanel, Clear);
        Anchor(scrollGo.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 36f), new Vector2(-34f, -110f));

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
        contentRt.sizeDelta = new Vector2(0f, 500f);
        scroll.content = contentRt;

        var grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(86f, 86f);
        grid.spacing = new Vector2(8f, 8f);
        grid.padding = new RectOffset(12, 12, 12, 12);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 8;
        grid.childAlignment = TextAnchor.UpperLeft;

        content.AddComponent<InventoryGridView>();

        for (int i = 0; i < 40; i++)
            Slot(content.transform, $"Slot_toggle{(i == 0 ? string.Empty : $" ({i})")}", i + 1);
    }

    private static void BuildGridFooter(Transform gridPanel)
    {
        var footer = PanelObject("GridFooter", gridPanel, Clear);
        Anchor(footer.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(-34f, 76f));

        var money = PanelObject("MoneyBox", footer.transform, new Color(0.28f, 0.17f, 0.06f, 0.95f));
        Anchor(money.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(130f, 54f));
        AddOutline(money, Frame, 1.5f);
        Label("CoinIcon", money.transform, "O", 24, TextAlignmentOptions.Center, new Color(1f, 0.75f, 0.18f, 1f), new Vector2(18f, 0f), new Vector2(36f, 40f), new Vector2(0f, 0.5f));
        Label("MoneyText", money.transform, "0", 23, TextAlignmentOptions.MidlineLeft, Text, new Vector2(58f, 0f), new Vector2(68f, 40f), new Vector2(0f, 0.5f));

        var sort = Button("SortButton", footer.transform, "Sắp xếp nhanh", 22, new Color(0.36f, 0.22f, 0.08f, 1f), Text);
        Anchor(sort.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(164f, 0f), new Vector2(220f, 54f));

        var prev = Button("PrevPageButton", footer.transform, "<", 30, new Color(0.36f, 0.22f, 0.08f, 1f), Text);
        Anchor(prev.GetComponent<RectTransform>(), new Vector2(0.62f, 0.5f), new Vector2(0.62f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(54f, 54f));

        Label("PageText", footer.transform, "1 / 1", 26, TextAlignmentOptions.Center, Text, Vector2.zero, new Vector2(116f, 54f), new Vector2(0.5f, 0.5f));
        var pageTextRt = footer.transform.Find("PageText").GetComponent<RectTransform>();
        pageTextRt.anchorMin = pageTextRt.anchorMax = new Vector2(0.73f, 0.5f);

        var next = Button("NextPageButton", footer.transform, ">", 30, new Color(0.36f, 0.22f, 0.08f, 1f), Text);
        Anchor(next.GetComponent<RectTransform>(), new Vector2(0.84f, 0.5f), new Vector2(0.84f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(54f, 54f));

        var drop = Button("DropButton", footer.transform, "X", 28, new Color(0.58f, 0.18f, 0.12f, 1f), Color.white);
        Anchor(drop.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(64f, 54f));
    }

    private static void BuildInfoPanel(Transform body)
    {
        var panel = PanelObject("ItemInfoPanel", body, new Color(0.29f, 0.17f, 0.06f, 0.98f));
        Anchor(panel.GetComponent<RectTransform>(), new Vector2(0.70f, 0f), new Vector2(1f, 1f), new Vector2(0.70f, 0f), new Vector2(12f, 0f), new Vector2(0f, 0f));
        AddOutline(panel, Frame, 2f);

        var info = PanelObject("Info_Item", panel.transform, Clear);
        Stretch(info);

        var header = PanelObject("InfoHeader", info.transform, new Color(0.36f, 0.22f, 0.09f, 1f));
        Anchor(header.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, 58f));
        AddOutline(header, Frame, 1.5f);
        Label("HeaderText", header.transform, "THÔNG TIN VẬT PHẨM", 24, TextAlignmentOptions.Center, Text, Vector2.zero, new Vector2(360f, 50f), new Vector2(0.5f, 0.5f));

        var iconFrame = PanelObject("ItemIconFrame", info.transform, PanelDark);
        Anchor(iconFrame.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -78f), new Vector2(104f, 104f));
        AddOutline(iconFrame, Frame, 1.5f);
        var icon = PanelObject("icon", iconFrame.transform, Clear);
        Stretch(icon, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-18f, -18f));

        Label("Name_Tmp", info.transform, "Tên vật phẩm", 22, TextAlignmentOptions.Left, new Color(0.42f, 0.82f, 1f, 1f), new Vector2(144f, -76f), new Vector2(210f, 28f), new Vector2(0f, 1f));
        Label("Category_tmp", info.transform, "Loại", 20, TextAlignmentOptions.Left, new Color(0.40f, 1f, 0.35f, 1f), new Vector2(144f, -108f), new Vector2(210f, 24f), new Vector2(0f, 1f));
        Label("Rarity_tmp", info.transform, "Hiếm", 20, TextAlignmentOptions.Left, new Color(0.85f, 0.45f, 1f, 1f), new Vector2(144f, -138f), new Vector2(210f, 24f), new Vector2(0f, 1f));

        var desc = PanelObject("DescPanel", info.transform, PanelDark);
        Anchor(desc.GetComponent<RectTransform>(), new Vector2(0f, 0.34f), new Vector2(1f, 0.74f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-32f, 0f));
        AddOutline(desc, new Color(0.45f, 0.28f, 0.10f, 1f), 1.5f);
        Label("desc_tmp", desc.transform, "Thông tin mô tả vật phẩm.", 19, TextAlignmentOptions.TopLeft, Text, new Vector2(16f, -14f), new Vector2(320f, 150f), new Vector2(0f, 1f));

        var statScroll = PanelObject("attribute_SclView", info.transform, new Color(0.13f, 0.08f, 0.03f, 0.65f));
        Anchor(statScroll.GetComponent<RectTransform>(), new Vector2(0f, 0.18f), new Vector2(1f, 0.32f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-32f, 0f));
        AddOutline(statScroll, new Color(0.45f, 0.28f, 0.10f, 1f), 1.5f);
        var statContent = PanelObject("content", statScroll.transform, Clear);
        Stretch(statContent);

        var price = PanelObject("Price_panel", info.transform, new Color(0.25f, 0.14f, 0.05f, 0.95f));
        Anchor(price.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(-32f, 58f));
        AddOutline(price, new Color(0.45f, 0.28f, 0.10f, 1f), 1.5f);
        Label("PriceLabel", price.transform, "Giá bán", 20, TextAlignmentOptions.MidlineLeft, Text, new Vector2(16f, 0f), new Vector2(110f, 44f), new Vector2(0f, 0.5f));
        Label("CoinIcon", price.transform, "O", 24, TextAlignmentOptions.Center, new Color(1f, 0.75f, 0.18f, 1f), new Vector2(164f, 0f), new Vector2(42f, 44f), new Vector2(0f, 0.5f));
        Label("value", price.transform, "0", 22, TextAlignmentOptions.MidlineLeft, Text, new Vector2(214f, 0f), new Vector2(100f, 44f), new Vector2(0f, 0.5f));

        var use = Button("Btn_use", info.transform, "Sử dụng", 22, new Color(0.10f, 0.34f, 0.62f, 1f), Color.white);
        Anchor(use.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 24f), new Vector2(150f, 58f));

        var drop = Button("Btn_Drop", info.transform, "Thả", 22, new Color(0.58f, 0.18f, 0.12f, 1f), Color.white);
        Anchor(drop.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 24f), new Vector2(150f, 58f));
    }

    private static void BuildHotbarDock(Transform window)
    {
        var dock = PanelObject("HotbarDock", window, new Color(0.25f, 0.14f, 0.05f, 0.96f));
        Anchor(dock.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -112f), new Vector2(1040f, 92f));
        AddOutline(dock, Frame, 2f);

        var hotbar = PanelObject("Hotbar", dock.transform, Clear);
        Stretch(hotbar, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-24f, -18f));
        var layout = hotbar.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 6, 6);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        for (int i = 0; i < 10; i++)
            HotbarSlot(hotbar.transform, i);
    }

    private static void Slot(Transform parent, string name, int number)
    {
        var slot = PanelObject(name, parent, PanelDark);
        slot.AddComponent<Toggle>();
        var le = slot.AddComponent<LayoutElement>();
        le.preferredWidth = 86f;
        le.preferredHeight = 86f;
        AddOutline(slot, new Color(0.43f, 0.26f, 0.10f, 1f), 1.5f);

        var icon = PanelObject("Icon", slot.transform, Clear);
        Stretch(icon, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-14f, -14f));
        icon.GetComponent<Image>().raycastTarget = false;

        Label("Amount", slot.transform, string.Empty, 17, TextAlignmentOptions.BottomRight, Color.white, new Vector2(-6f, 4f), new Vector2(42f, 22f), new Vector2(1f, 0f));

        var select = PanelObject("Select", slot.transform, new Color(0.35f, 1f, 0.95f, 0.32f));
        Stretch(select);
        select.SetActive(number == 1);
    }

    private static void HotbarSlot(Transform parent, int index)
    {
        var slot = PanelObject(index == 9 ? "0" : (index + 1).ToString(), parent, PanelDark);
        slot.AddComponent<Button>();
        var le = slot.AddComponent<LayoutElement>();
        le.preferredWidth = 78f;
        le.preferredHeight = 72f;
        AddOutline(slot, new Color(0.43f, 0.26f, 0.10f, 1f), 1.5f);

        Label("SlotNumber", slot.transform, index == 9 ? "0" : (index + 1).ToString(), 16, TextAlignmentOptions.TopLeft, Text, new Vector2(8f, -6f), new Vector2(24f, 20f), new Vector2(0f, 1f));

        var icon = PanelObject("Icon", slot.transform, Clear);
        Anchor(icon.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -2f), new Vector2(44f, 44f));
        icon.GetComponent<Image>().raycastTarget = false;

        Label("Amount", slot.transform, string.Empty, 15, TextAlignmentOptions.BottomRight, Color.white, new Vector2(-6f, 5f), new Vector2(34f, 18f), new Vector2(1f, 0f));

        var select = PanelObject("Select", slot.transform, new Color(0.35f, 1f, 0.95f, 0.32f));
        Stretch(select);
        select.SetActive(index == 0);
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
