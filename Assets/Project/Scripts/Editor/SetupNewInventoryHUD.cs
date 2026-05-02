using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class SetupNewInventoryHUD
{
    [MenuItem("Tools/Setup New Inventory HUD")]
    public static void Execute()
    {
        var scene = EditorSceneManager.GetActiveScene();

        var oldHud = GameObject.Find("_______UI_______________/HUD_Canvas");
        if (oldHud != null)
        {
            oldHud.SetActive(false);
        }

        var oldHudAlt = GameObject.Find("HUD_Canvas");
        if (oldHudAlt != null)
        {
            oldHudAlt.SetActive(false);
        }

        var existing = GameObject.Find("HUD_Canvas_V2");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        var canvasGO = new GameObject("HUD_Canvas_V2", typeof(RectTransform));
        canvasGO.transform.SetParent(null, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Root layers
        var hudRoot = CreatePanel(canvasGO.transform, "HUDRoot", new Color(1, 1, 1, 0f));
        Stretch(hudRoot);

        var topLeft = CreatePanel(hudRoot.transform, "TopLeftPlayerPanel", new Color(0.18f, 0.12f, 0.06f, 0.92f));
        Anchor(topLeft.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(300f, 132f));
        AddFrame(topLeft, new Color(0.6f, 0.42f, 0.16f, 1f));

        var avatar = CreatePanel(topLeft.transform, "AvatarFrame", new Color(0.08f, 0.05f, 0.03f, 0.95f));
        Anchor(avatar.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -12f), new Vector2(74f, 108f));
        AddFrame(avatar, new Color(0.75f, 0.55f, 0.2f, 1f));

        CreateLabel(topLeft.transform, "LevelLabel", "Lv. 12", 18, TextAlignmentOptions.Center, new Color(1f, 0.92f, 0.72f, 1f), new Vector2(12f, -92f), new Vector2(74f, 20f), new Vector2(0f, 1f));

        var stats = CreatePanel(topLeft.transform, "StatsBlock", new Color(1, 1, 1, 0f));
        Anchor(stats.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(110f, 10f), new Vector2(-10f, -10f));

        CreateStatBar(stats.transform, "HPBar", "HP", new Color(0.82f, 0.22f, 0.18f, 1f), new Color(0.45f, 0.06f, 0.05f, 1f), new Vector2(0f, -10f), "320/320");
        CreateStatBar(stats.transform, "EnergyBar", "EN", new Color(0.12f, 0.6f, 0.94f, 1f), new Color(0.02f, 0.24f, 0.4f, 1f), new Vector2(0f, -44f), "120/120");
        CreateStatBar(stats.transform, "ExpBar", "EXP", new Color(0.92f, 0.62f, 0.12f, 1f), new Color(0.28f, 0.16f, 0.03f, 1f), new Vector2(0f, -78f), "650/1500");

        var topCenter = CreatePanel(hudRoot.transform, "TopCenterStatusPanel", new Color(0.18f, 0.12f, 0.06f, 0.92f));
        Anchor(topCenter.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(260f, 118f));
        AddFrame(topCenter, new Color(0.6f, 0.42f, 0.16f, 1f));
        CreateStatusRow(topCenter.transform, "DayRow", "Ngày 5 - Xuân", new Vector2(0f, -10f));
        CreateStatusRow(topCenter.transform, "TimeRow", "09:30 AM", new Vector2(0f, -42f));
        CreateStatusRow(topCenter.transform, "GoldRow", "12,450", new Vector2(0f, -74f));

        var topRight = CreatePanel(hudRoot.transform, "TopRightMapMenuPanel", new Color(0.18f, 0.12f, 0.06f, 0.92f));
        Anchor(topRight.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(286f, 122f));
        AddFrame(topRight, new Color(0.6f, 0.42f, 0.16f, 1f));

        var miniMap = CreatePanel(topRight.transform, "MinimapFrame", new Color(0.08f, 0.05f, 0.03f, 0.95f));
        Anchor(miniMap.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -10f), new Vector2(104f, 102f));
        AddFrame(miniMap, new Color(0.75f, 0.55f, 0.2f, 1f));

        var menuBtn = CreatePanel(topRight.transform, "MenuButton", new Color(0.18f, 0.12f, 0.06f, 1f));
        Anchor(menuBtn.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(52f, 52f));
        AddFrame(menuBtn, new Color(0.75f, 0.55f, 0.2f, 1f));
        CreateLabel(menuBtn.transform, "Icon", "≡", 26, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.65f, 1f), Vector2.zero, new Vector2(52f, 52f), new Vector2(0.5f, 0.5f));

        var inventoryWindow = CreatePanel(hudRoot.transform, "InventoryWindow", new Color(0.22f, 0.14f, 0.06f, 0.96f));
        Anchor(inventoryWindow.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(1100f, 660f));
        AddFrame(inventoryWindow, new Color(0.62f, 0.42f, 0.16f, 1f));

        var title = CreatePanel(inventoryWindow.transform, "TitleBanner", new Color(0.32f, 0.2f, 0.08f, 1f));
        Anchor(title.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 14f), new Vector2(260f, 56f));
        AddFrame(title, new Color(0.7f, 0.5f, 0.2f, 1f));
        CreateLabel(title.transform, "TitleText", "TÚI ĐỒ", 24, TextAlignmentOptions.Center, new Color(1f, 0.93f, 0.75f, 1f), Vector2.zero, new Vector2(260f, 56f), new Vector2(0.5f, 0.5f));

        var leftArea = CreatePanel(inventoryWindow.transform, "InventoryGridPanel", new Color(0.38f, 0.26f, 0.1f, 0.92f));
        Anchor(leftArea.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.67f, 1f), new Vector2(0f, 0f), new Vector2(16f, 16f), new Vector2(-10f, -18f));
        AddFrame(leftArea, new Color(0.72f, 0.5f, 0.2f, 1f));

        var inventoryHeader = CreatePanel(leftArea.transform, "InventoryHeader", new Color(0.24f, 0.16f, 0.06f, 1f));
        Anchor(inventoryHeader.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(12f, -12f), new Vector2(-12f, 56f));
        AddFrame(inventoryHeader, new Color(0.65f, 0.45f, 0.18f, 1f));
        CreateButton(inventoryHeader.transform, "AllButton", "Tất cả", new Vector2(10f, 6f), new Vector2(98f, 38f));
        CreateButton(inventoryHeader.transform, "SortButton", "Sắp xếp nhanh", new Vector2(116f, 6f), new Vector2(136f, 38f));
        CreateButton(inventoryHeader.transform, "CountLabel", "38/60", new Vector2(742f, 6f), new Vector2(78f, 38f), true);

        var grid = CreatePanel(leftArea.transform, "InventoryGrid", new Color(0.18f, 0.12f, 0.05f, 0.85f));
        Anchor(grid.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(12f, 12f), new Vector2(-12f, -78f));
        AddFrame(grid, new Color(0.55f, 0.38f, 0.15f, 1f));
        var gridLayout = grid.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(66f, 66f);
        gridLayout.spacing = new Vector2(6f, 6f);
        gridLayout.padding = new RectOffset(10, 10, 10, 10);
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 8;

        for (int i = 0; i < 48; i++)
        {
            CreateInventorySlot(grid.transform, $"Slot_{i + 1}");
        }

        var detail = CreatePanel(inventoryWindow.transform, "ItemDetailPanel", new Color(0.22f, 0.15f, 0.06f, 0.96f));
        Anchor(detail.GetComponent<RectTransform>(), new Vector2(0.69f, 0f), new Vector2(1f, 1f), new Vector2(0.69f, 0f), new Vector2(8f, 16f), new Vector2(-16f, -16f));
        AddFrame(detail, new Color(0.72f, 0.5f, 0.2f, 1f));
        CreateLabel(detail.transform, "Header", "THÔNG TIN VẬT PHẨM", 20, TextAlignmentOptions.Center, new Color(1f, 0.92f, 0.72f, 1f), new Vector2(16f, -16f), new Vector2(-32f, 30f), new Vector2(0f, 1f));
        CreatePanel(detail.transform, "ItemIconFrame", new Color(0.08f, 0.05f, 0.03f, 0.95f));
        Anchor(detail.transform.Find("ItemIconFrame").GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -56f), new Vector2(104f, 104f));
        AddFrame(detail.transform.Find("ItemIconFrame").gameObject, new Color(0.75f, 0.55f, 0.2f, 1f));
        CreateLabel(detail.transform, "ItemName", "Tên vật phẩm", 20, TextAlignmentOptions.Left, new Color(0.55f, 0.8f, 1f, 1f), new Vector2(136f, -66f), new Vector2(220f, 24f), new Vector2(0f, 1f));
        CreateLabel(detail.transform, "ItemType", "Loại: ...", 16, TextAlignmentOptions.Left, new Color(1f, 0.9f, 0.7f, 1f), new Vector2(136f, -92f), new Vector2(220f, 20f), new Vector2(0f, 1f));
        CreateLabel(detail.transform, "ItemRarity", "Hiếm: ...", 16, TextAlignmentOptions.Left, new Color(1f, 0.9f, 0.7f, 1f), new Vector2(136f, -112f), new Vector2(220f, 20f), new Vector2(0f, 1f));

        var statBox = CreatePanel(detail.transform, "StatsBox", new Color(0.16f, 0.1f, 0.04f, 0.9f));
        Anchor(statBox.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, -16f), new Vector2(-16f, 204f));
        AddFrame(statBox, new Color(0.55f, 0.38f, 0.15f, 1f));
        CreateLabel(statBox.transform, "Stat1", "Tấn công    25", 16, TextAlignmentOptions.Left, new Color(1f, 0.95f, 0.8f, 1f), new Vector2(12f, -8f), new Vector2(280f, 22f), new Vector2(0f, 1f));
        CreateLabel(statBox.transform, "Stat2", "Tỷ lệ chí mạng    5%", 16, TextAlignmentOptions.Left, new Color(1f, 0.95f, 0.8f, 1f), new Vector2(12f, -36f), new Vector2(280f, 22f), new Vector2(0f, 1f));
        CreateLabel(statBox.transform, "Stat3", "Tốc độ đánh    1.20", 16, TextAlignmentOptions.Left, new Color(1f, 0.95f, 0.8f, 1f), new Vector2(12f, -64f), new Vector2(280f, 22f), new Vector2(0f, 1f));
        CreateLabel(statBox.transform, "Description", "Mô tả vật phẩm sẽ hiển thị ở đây.", 15, TextAlignmentOptions.Left, new Color(1f, 0.92f, 0.75f, 1f), new Vector2(12f, -100f), new Vector2(280f, 64f), new Vector2(0f, 1f));

        CreateButton(detail.transform, "EquipButton", "TRANG BỊ", new Vector2(16f, 16f), new Vector2(108f, 42f));
        CreateButton(detail.transform, "CancelButton", "HỦY", new Vector2(132f, 16f), new Vector2(108f, 42f));

        var menu = CreatePanel(hudRoot.transform, "RightSideMenuPanel", new Color(0.22f, 0.14f, 0.06f, 0.96f));
        Anchor(menu.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, -8f), new Vector2(240f, 560f));
        AddFrame(menu, new Color(0.72f, 0.5f, 0.2f, 1f));

        string[] menuItems = { "Túi đồ", "Kho đồ", "Kỹ năng", "Bản đồ", "Nhiệm vụ", "Nhật ký", "Cài đặt" };
        for (int i = 0; i < menuItems.Length; i++)
        {
            CreateMenuItem(menu.transform, $"Menu_{i + 1}", menuItems[i], new Vector2(12f, -16f - (i * 72f)), new Vector2(216f, 60f));
        }

        CreatePanel(hudRoot.transform, "BottomLeftReserved", new Color(1, 1, 1, 0f));
        CreatePanel(hudRoot.transform, "BottomRightReserved", new Color(1, 1, 1, 0f));

        // Hotbar
        var hotbar = CreatePanel(hudRoot.transform, "HotbarPanel", new Color(0.18f, 0.12f, 0.06f, 0.92f));
        Anchor(hotbar.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(940f, 84f));
        AddFrame(hotbar, new Color(0.6f, 0.42f, 0.16f, 1f));
        var hotbarLayout = hotbar.AddComponent<HorizontalLayoutGroup>();
        hotbarLayout.padding = new RectOffset(10, 10, 10, 10);
        hotbarLayout.spacing = 6;
        hotbarLayout.childAlignment = TextAnchor.MiddleCenter;
        hotbarLayout.childForceExpandHeight = false;
        hotbarLayout.childForceExpandWidth = false;
        for (int i = 0; i < 10; i++)
        {
            CreateHotbarSlot(hotbar.transform, $"Slot_{i}", i);
        }

        EditorUtility.SetDirty(canvasGO);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = canvasGO;
        Debug.Log("[SetupNewInventoryHUD] Created HUD_Canvas_V2 and disabled old HUD canvas.");
    }

    static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    static void AddFrame(GameObject go, Color color)
    {
        var outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(2f, -2f);
    }

    static void Anchor(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
    }

    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static GameObject CreateLabel(Transform parent, string name, string text, int size, TextAlignmentOptions alignment, Color color, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 pivot)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        return go;
    }

    static GameObject CreateButton(Transform parent, string name, string text, Vector2 anchoredPosition, Vector2 sizeDelta, bool centered = false)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.34f, 0.22f, 0.08f, 0.95f);
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.72f, 0.5f, 0.2f, 1f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        var txt = CreateLabel(go.transform, "Text", text, 18, centered ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft, new Color(1f, 0.92f, 0.75f, 1f), Vector2.zero, sizeDelta, new Vector2(0.5f, 0.5f));
        txt.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        txt.GetComponent<RectTransform>().anchorMax = Vector2.one;
        txt.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        txt.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        go.AddComponent<Button>();
        return go;
    }

    static void CreateStatBar(Transform parent, string name, string label, Color fillColor, Color bgColor, Vector2 topLeftOffset, string value)
    {
        var bar = CreatePanel(parent, name, bgColor);
        var rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = topLeftOffset;
        rt.sizeDelta = new Vector2(-10f, 30f);
        AddFrame(bar, new Color(0.75f, 0.55f, 0.2f, 1f));

        var barBg = CreatePanel(bar.transform, "BarBackground", new Color(0, 0, 0, 0.25f));
        Stretch(barBg);

        var fill = CreatePanel(bar.transform, "Fill", fillColor);
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(0.7f, 1f);
        fillRT.offsetMin = new Vector2(4f, 4f);
        fillRT.offsetMax = new Vector2(-4f, -4f);

        CreateLabel(bar.transform, "Prefix", label, 16, TextAlignmentOptions.Left, Color.white, new Vector2(12f, -5f), new Vector2(50f, 22f), new Vector2(0f, 1f));
        CreateLabel(bar.transform, "Value", value, 16, TextAlignmentOptions.Right, Color.white, new Vector2(-12f, -5f), new Vector2(110f, 22f), new Vector2(1f, 1f));
    }

    static void CreateStatusRow(Transform parent, string name, string text, Vector2 anchoredPosition)
    {
        var row = CreatePanel(parent, name, new Color(0.28f, 0.18f, 0.08f, 1f));
        Anchor(row.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), anchoredPosition, new Vector2(-18f, 34f));
        AddFrame(row, new Color(0.72f, 0.5f, 0.2f, 1f));
        CreateLabel(row.transform, "Text", text, 18, TextAlignmentOptions.Center, new Color(1f, 0.93f, 0.75f, 1f), Vector2.zero, new Vector2(250f, 32f), new Vector2(0.5f, 0.5f));
    }

    static void CreateInventorySlot(Transform parent, string name)
    {
        var slot = CreatePanel(parent, name, new Color(0.22f, 0.16f, 0.08f, 1f));
        var le = slot.AddComponent<LayoutElement>();
        le.preferredWidth = 66f;
        le.preferredHeight = 66f;
        AddFrame(slot, new Color(0.72f, 0.5f, 0.2f, 1f));

        var icon = CreatePanel(slot.transform, "Icon", new Color(1, 1, 1, 0f));
        Stretch(icon);

        var amount = CreateLabel(slot.transform, "Amount", "", 14, TextAlignmentOptions.BottomRight, Color.white, new Vector2(-5f, 3f), new Vector2(34f, 16f), new Vector2(1f, 0f));
        amount.GetComponent<RectTransform>().anchorMin = new Vector2(1f, 0f);
        amount.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 0f);

        var select = CreatePanel(slot.transform, "Select", new Color(1f, 0.9f, 0.2f, 0.22f));
        Stretch(select);
        select.SetActive(false);
    }

    static void CreateHotbarSlot(Transform parent, string name, int index)
    {
        var slot = CreatePanel(parent, name, new Color(0.2f, 0.14f, 0.06f, 1f));
        var le = slot.AddComponent<LayoutElement>();
        le.preferredWidth = 80f;
        le.preferredHeight = 60f;
        AddFrame(slot, new Color(0.72f, 0.5f, 0.2f, 1f));

        CreateLabel(slot.transform, "SlotNumber", index == 9 ? "0" : (index + 1).ToString(), 14, TextAlignmentOptions.TopLeft, new Color(1f, 0.92f, 0.75f, 1f), new Vector2(6f, -5f), new Vector2(16f, 16f), new Vector2(0f, 1f));
        var icon = CreatePanel(slot.transform, "Icon", new Color(1, 1, 1, 0f));
        var iconRT = icon.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.5f, 0.5f);
        iconRT.anchorMax = new Vector2(0.5f, 0.5f);
        iconRT.pivot = new Vector2(0.5f, 0.5f);
        iconRT.anchoredPosition = new Vector2(0f, 0f);
        iconRT.sizeDelta = new Vector2(28f, 28f);

        CreateLabel(slot.transform, "Amount", "", 13, TextAlignmentOptions.BottomRight, Color.white, new Vector2(-6f, 4f), new Vector2(24f, 16f), new Vector2(1f, 0f));

        var select = CreatePanel(slot.transform, "Select", new Color(1f, 0.9f, 0.2f, 0.2f));
        Stretch(select);
        select.SetActive(index == 0);
    }

    static void CreateMenuItem(Transform parent, string name, string text, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var item = CreatePanel(parent, name, new Color(0.28f, 0.18f, 0.08f, 1f));
        Anchor(item.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), anchoredPosition, sizeDelta);
        AddFrame(item, new Color(0.72f, 0.5f, 0.2f, 1f));

        var icon = CreatePanel(item.transform, "Icon", new Color(1, 1, 1, 0f));
        Anchor(icon.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(38f, 38f));

        CreateLabel(item.transform, "Label", text, 18, TextAlignmentOptions.MidlineLeft, new Color(1f, 0.93f, 0.75f, 1f), new Vector2(66f, -15f), new Vector2(132f, 26f), new Vector2(0f, 1f));
    }
}
