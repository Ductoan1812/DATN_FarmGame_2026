using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SetupHotbarUI
{
    [MenuItem("Tools/Setup Hotbar UI")]
    public static void Execute()
    {
        // ── Canvas ──
        var canvasGO = new GameObject("HUD_Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ══════ HOTBAR ══════
        var hotbarPanel = CreatePanel(canvasGO.transform, "HotbarPanel", new Color(0.12f, 0.1f, 0.08f, 0.85f));
        var hotbarRT = hotbarPanel.GetComponent<RectTransform>();
        hotbarRT.anchorMin = new Vector2(0.5f, 0f);
        hotbarRT.anchorMax = new Vector2(0.5f, 0f);
        hotbarRT.pivot = new Vector2(0.5f, 0f);
        hotbarRT.anchoredPosition = new Vector2(0, 20);
        hotbarRT.sizeDelta = new Vector2(680, 80);

        // Rounded corners via outline
        var hotbarOutline = hotbarPanel.AddComponent<Outline>();
        hotbarOutline.effectColor = new Color(0.55f, 0.4f, 0.2f, 1f);
        hotbarOutline.effectDistance = new Vector2(2, 2);

        // Horizontal layout
        var hlg = hotbarPanel.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6;
        hlg.padding = new RectOffset(8, 8, 8, 8);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // 8 Hotbar slots
        for (int i = 0; i < 8; i++)
        {
            CreateHotbarSlot(hotbarPanel.transform, $"Slot_{i}", i);
        }

        // ══════ SELECTION INDICATOR TEXT ══════
        var slotLabel = new GameObject("SlotLabel");
        slotLabel.transform.SetParent(canvasGO.transform, false);
        var slotLabelRT = slotLabel.AddComponent<RectTransform>();
        slotLabelRT.anchorMin = new Vector2(0.5f, 0f);
        slotLabelRT.anchorMax = new Vector2(0.5f, 0f);
        slotLabelRT.pivot = new Vector2(0.5f, 0f);
        slotLabelRT.anchoredPosition = new Vector2(0, 105);
        slotLabelRT.sizeDelta = new Vector2(300, 30);
        var slotLabelTMP = slotLabel.AddComponent<TextMeshProUGUI>();
        slotLabelTMP.text = "Cuốc";
        slotLabelTMP.fontSize = 18;
        slotLabelTMP.alignment = TextAlignmentOptions.Center;
        slotLabelTMP.color = new Color(1f, 0.95f, 0.8f, 1f);

        // ══════ INVENTORY BUTTON ══════
        var btnGO = new GameObject("Btn_Inventory");
        btnGO.transform.SetParent(canvasGO.transform, false);
        var btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1f, 0f);
        btnRT.anchorMax = new Vector2(1f, 0f);
        btnRT.pivot = new Vector2(1f, 0f);
        btnRT.anchoredPosition = new Vector2(-20, 20);
        btnRT.sizeDelta = new Vector2(60, 60);

        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.18f, 0.15f, 0.12f, 0.9f);
        var btnOutline = btnGO.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0.55f, 0.4f, 0.2f, 1f);
        btnOutline.effectDistance = new Vector2(2, 2);
        btnGO.AddComponent<Button>();

        // Icon text (placeholder)
        var btnIcon = new GameObject("Icon");
        btnIcon.transform.SetParent(btnGO.transform, false);
        var btnIconRT = btnIcon.AddComponent<RectTransform>();
        btnIconRT.anchorMin = Vector2.zero;
        btnIconRT.anchorMax = Vector2.one;
        btnIconRT.sizeDelta = Vector2.zero;
        var btnIconTMP = btnIcon.AddComponent<TextMeshProUGUI>();
        btnIconTMP.text = "▤";
        btnIconTMP.fontSize = 32;
        btnIconTMP.alignment = TextAlignmentOptions.Center;
        btnIconTMP.color = new Color(0.9f, 0.8f, 0.6f, 1f);

        // ══════ INVENTORY PANEL (ẩn mặc định) ══════
        var invPanel = CreatePanel(canvasGO.transform, "InventoryPanel", new Color(0.12f, 0.1f, 0.08f, 0.95f));
        var invRT = invPanel.GetComponent<RectTransform>();
        invRT.anchorMin = new Vector2(0.5f, 0.5f);
        invRT.anchorMax = new Vector2(0.5f, 0.5f);
        invRT.pivot = new Vector2(0.5f, 0.5f);
        invRT.anchoredPosition = Vector2.zero;
        invRT.sizeDelta = new Vector2(520, 420);

        var invOutline = invPanel.AddComponent<Outline>();
        invOutline.effectColor = new Color(0.55f, 0.4f, 0.2f, 1f);
        invOutline.effectDistance = new Vector2(2, 2);

        // Title
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(invPanel.transform, false);
        var titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0, -5);
        titleRT.sizeDelta = new Vector2(0, 40);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "Kho đồ";
        titleTMP.fontSize = 24;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = new Color(1f, 0.9f, 0.65f, 1f);
        titleTMP.fontStyle = FontStyles.Bold;

        // Close button
        var closeBtnGO = new GameObject("Btn_Close");
        closeBtnGO.transform.SetParent(invPanel.transform, false);
        var closeBtnRT = closeBtnGO.AddComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(1f, 1f);
        closeBtnRT.anchorMax = new Vector2(1f, 1f);
        closeBtnRT.pivot = new Vector2(1f, 1f);
        closeBtnRT.anchoredPosition = new Vector2(-8, -8);
        closeBtnRT.sizeDelta = new Vector2(32, 32);
        var closeBtnImg = closeBtnGO.AddComponent<Image>();
        closeBtnImg.color = new Color(0.6f, 0.2f, 0.2f, 1f);
        closeBtnGO.AddComponent<Button>();
        var closeText = new GameObject("Text");
        closeText.transform.SetParent(closeBtnGO.transform, false);
        var closeTextRT = closeText.AddComponent<RectTransform>();
        closeTextRT.anchorMin = Vector2.zero;
        closeTextRT.anchorMax = Vector2.one;
        closeTextRT.sizeDelta = Vector2.zero;
        var closeTextTMP = closeText.AddComponent<TextMeshProUGUI>();
        closeTextTMP.text = "✕";
        closeTextTMP.fontSize = 20;
        closeTextTMP.alignment = TextAlignmentOptions.Center;
        closeTextTMP.color = Color.white;

        // Grid container
        var gridGO = new GameObject("SlotsGrid");
        gridGO.transform.SetParent(invPanel.transform, false);
        var gridRT = gridGO.AddComponent<RectTransform>();
        gridRT.anchorMin = new Vector2(0f, 0f);
        gridRT.anchorMax = new Vector2(1f, 1f);
        gridRT.offsetMin = new Vector2(15, 15);
        gridRT.offsetMax = new Vector2(-15, -50);

        var glg = gridGO.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(60, 60);
        glg.spacing = new Vector2(6, 6);
        glg.padding = new RectOffset(5, 5, 5, 5);
        glg.childAlignment = TextAnchor.UpperLeft;
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 7;

        // 21 inventory slots (3 rows x 7)
        for (int i = 0; i < 21; i++)
        {
            CreateInventorySlot(gridGO.transform, $"InvSlot_{i}");
        }

        // Ẩn inventory panel mặc định
        invPanel.SetActive(false);

        Undo.RegisterCreatedObjectUndo(canvasGO, "Create HUD UI");
        Selection.activeGameObject = canvasGO;
        Debug.Log("[SetupHotbarUI] Đã tạo HUD: Hotbar (8 slot) + Inventory Button + Inventory Panel (21 slot)");
    }

    // ── Helpers ──

    static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    static void CreateHotbarSlot(Transform parent, string name, int index)
    {
        var slotGO = new GameObject(name);
        slotGO.transform.SetParent(parent, false);

        var le = slotGO.AddComponent<LayoutElement>();
        le.preferredWidth = 64;
        le.preferredHeight = 64;

        // Background
        var bg = slotGO.AddComponent<Image>();
        bg.color = new Color(0.22f, 0.18f, 0.14f, 1f);

        // Selection highlight (chỉ slot 0 active mặc định)
        var highlight = new GameObject("Highlight");
        highlight.transform.SetParent(slotGO.transform, false);
        var hlRT = highlight.AddComponent<RectTransform>();
        hlRT.anchorMin = Vector2.zero;
        hlRT.anchorMax = Vector2.one;
        hlRT.sizeDelta = Vector2.zero;
        var hlImg = highlight.AddComponent<Image>();
        hlImg.color = new Color(1f, 0.85f, 0.4f, 0.3f);
        highlight.SetActive(index == 0);

        // Icon
        var icon = new GameObject("Icon");
        icon.transform.SetParent(slotGO.transform, false);
        var iconRT = icon.AddComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.1f, 0.1f);
        iconRT.anchorMax = new Vector2(0.9f, 0.9f);
        iconRT.sizeDelta = Vector2.zero;
        var iconImg = icon.AddComponent<Image>();
        iconImg.color = new Color(1, 1, 1, 0.15f); // placeholder
        iconImg.preserveAspect = true;

        // Amount text
        var amountGO = new GameObject("Amount");
        amountGO.transform.SetParent(slotGO.transform, false);
        var amountRT = amountGO.AddComponent<RectTransform>();
        amountRT.anchorMin = new Vector2(1f, 0f);
        amountRT.anchorMax = new Vector2(1f, 0f);
        amountRT.pivot = new Vector2(1f, 0f);
        amountRT.anchoredPosition = new Vector2(-2, 2);
        amountRT.sizeDelta = new Vector2(30, 20);
        var amountTMP = amountGO.AddComponent<TextMeshProUGUI>();
        amountTMP.text = "";
        amountTMP.fontSize = 14;
        amountTMP.alignment = TextAlignmentOptions.BottomRight;
        amountTMP.color = Color.white;

        // Key hint (1-8)
        var keyGO = new GameObject("KeyHint");
        keyGO.transform.SetParent(slotGO.transform, false);
        var keyRT = keyGO.AddComponent<RectTransform>();
        keyRT.anchorMin = new Vector2(0f, 1f);
        keyRT.anchorMax = new Vector2(0f, 1f);
        keyRT.pivot = new Vector2(0f, 1f);
        keyRT.anchoredPosition = new Vector2(2, -1);
        keyRT.sizeDelta = new Vector2(16, 16);
        var keyTMP = keyGO.AddComponent<TextMeshProUGUI>();
        keyTMP.text = (index + 1).ToString();
        keyTMP.fontSize = 10;
        keyTMP.alignment = TextAlignmentOptions.TopLeft;
        keyTMP.color = new Color(0.6f, 0.55f, 0.45f, 0.7f);
    }

    static void CreateInventorySlot(Transform parent, string name)
    {
        var slotGO = new GameObject(name);
        slotGO.transform.SetParent(parent, false);

        var bg = slotGO.AddComponent<Image>();
        bg.color = new Color(0.22f, 0.18f, 0.14f, 1f);

        // Icon
        var icon = new GameObject("Icon");
        icon.transform.SetParent(slotGO.transform, false);
        var iconRT = icon.AddComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.1f, 0.1f);
        iconRT.anchorMax = new Vector2(0.9f, 0.9f);
        iconRT.sizeDelta = Vector2.zero;
        var iconImg = icon.AddComponent<Image>();
        iconImg.color = new Color(1, 1, 1, 0.15f);
        iconImg.preserveAspect = true;

        // Amount
        var amountGO = new GameObject("Amount");
        amountGO.transform.SetParent(slotGO.transform, false);
        var amountRT = amountGO.AddComponent<RectTransform>();
        amountRT.anchorMin = new Vector2(1f, 0f);
        amountRT.anchorMax = new Vector2(1f, 0f);
        amountRT.pivot = new Vector2(1f, 0f);
        amountRT.anchoredPosition = new Vector2(-2, 2);
        amountRT.sizeDelta = new Vector2(30, 20);
        var amountTMP = amountGO.AddComponent<TextMeshProUGUI>();
        amountTMP.text = "";
        amountTMP.fontSize = 14;
        amountTMP.alignment = TextAlignmentOptions.BottomRight;
        amountTMP.color = Color.white;
    }
}
