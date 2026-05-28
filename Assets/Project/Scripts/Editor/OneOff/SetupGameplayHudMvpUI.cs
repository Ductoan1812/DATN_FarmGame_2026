using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class SetupGameplayHudMvpUI
{
    private static readonly Color PanelColor = new Color(0.08f, 0.06f, 0.04f, 0.72f);
    private static readonly Color SlotColor = new Color(0.13f, 0.10f, 0.07f, 0.88f);
    private static readonly Color BorderColor = new Color(0.95f, 0.78f, 0.38f, 0.95f);

    [MenuItem("Tools/DATN/One-off Setup/UI/Create Gameplay HUD MVP")]
    public static void Execute()
    {
        var canvas = GetOrCreateHudCanvas();
        EnsureEventSystem();

        var hudRoot = GetOrCreateUI("HUDRoot", canvas.transform);
        Stretch(hudRoot.GetComponent<RectTransform>());

        var infoPlayer = BuildPlayerInfoPanel(hudRoot.transform);
        SetupHudStatusMapUI.BuildCanonicalStatusMap(hudRoot.transform);
        BuildHotbar(hudRoot.transform);

        if (infoPlayer.GetComponent<PlayerInfoHUDUI>() == null)
            Undo.AddComponent<PlayerInfoHUDUI>(infoPlayer);

        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[SetupGameplayHudMvpUI] HUD MVP đã được tạo/cập nhật: PlayerInfoHUDUI, HudStatusMapUI, HotbarUI.");
    }

    private static GameObject BuildPlayerInfoPanel(Transform parent)
    {
        var root = GetOrCreatePanel("InfoPlayer", parent, PanelColor);
        ClearChildren(root.transform);
        SetupRect(root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(330f, 156f));

        var layout = GetOrAdd<VerticalLayoutGroup>(root);
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 7f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var level = CreateLabel("LevelLabel", root.transform, "Lv. 1", 22, TextAlignmentOptions.Left, Color.white);
        SetLayoutSize(level.gameObject, -1f, 28f);

        CreateStatBar(root.transform, "HP_info", "HP", "Hp_fill", new Color(0.86f, 0.18f, 0.18f, 1f), "0/0");
        CreateStatBar(root.transform, "Enegy_info", "STA", "Enegy_fill", new Color(0.19f, 0.68f, 0.28f, 1f), "0/0");
        CreateStatBar(root.transform, "Xp_info", "EXP", "Xp_fill", new Color(0.28f, 0.54f, 0.95f, 1f), "0/0");

        return root;
    }

    private static void BuildHotbar(Transform parent)
    {
        var root = GetOrCreatePanel("HotbarRoot", parent, PanelColor);
        ClearChildren(root.transform);
        SetupRect(root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(620f, 76f));

        var layout = GetOrAdd<HorizontalLayoutGroup>(root);
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.spacing = 8f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleCenter;

        for (int i = 0; i < 9; i++)
            CreateHotbarSlot(root.transform, i);

        if (root.GetComponent<HotbarUI>() == null)
            Undo.AddComponent<HotbarUI>(root);
    }

    private static void CreateStatBar(Transform parent, string rootName, string label, string fillName, Color fillColor, string fallbackValue)
    {
        var root = GetOrCreateUI(rootName, parent);
        SetLayoutSize(root, -1f, 30f);

        var bg = GetOrAdd<Image>(root);
        bg.color = new Color(0.03f, 0.025f, 0.02f, 0.92f);
        AddOutline(root, new Color(0f, 0f, 0f, 0.55f), new Vector2(1f, -1f));

        var fill = GetOrCreateUI(fillName, root.transform);
        var fillImage = GetOrAdd<Image>(fill);
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillAmount = 1f;
        Stretch(fill.GetComponent<RectTransform>(), new Vector2(2f, 2f), new Vector2(-2f, -2f));

        var labelText = CreateLabel("Label", root.transform, label, 15, TextAlignmentOptions.Left, Color.white);
        SetupRect(labelText.gameObject, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(58f, 0f));

        var valueText = CreateLabel("Value", root.transform, fallbackValue, 15, TextAlignmentOptions.Right, Color.white);
        Stretch(valueText.rectTransform, new Vector2(70f, 0f), new Vector2(-8f, 0f));
    }

    private static void CreateHotbarSlot(Transform parent, int index)
    {
        var slot = GetOrCreatePanel($"Slot_{index + 1}", parent, SlotColor);
        SetLayoutSize(slot, 58f, 58f);
        AddOutline(slot, BorderColor, new Vector2(1.5f, -1.5f));

        var icon = GetOrCreateUI("Icon", slot.transform);
        var iconImage = GetOrAdd<Image>(icon);
        iconImage.color = new Color(1f, 1f, 1f, 0f);
        Stretch(icon.GetComponent<RectTransform>(), new Vector2(8f, 8f), new Vector2(-8f, -8f));

        var amount = CreateLabel("Amount", slot.transform, string.Empty, 14, TextAlignmentOptions.BottomRight, Color.white);
        Stretch(amount.rectTransform, new Vector2(4f, 4f), new Vector2(-5f, -4f));

        var select = GetOrCreatePanel("Select", slot.transform, new Color(1f, 0.82f, 0.22f, 0.22f));
        var selectImage = GetOrAdd<Image>(select);
        selectImage.raycastTarget = false;
        AddOutline(select, new Color(1f, 0.92f, 0.32f, 1f), new Vector2(2f, -2f));
        Stretch(select.GetComponent<RectTransform>());
        select.SetActive(index == 0);
    }

    private static Canvas GetOrCreateHudCanvas()
    {
        var canvasGo = GameObject.Find("UIRoot/Canvas_HUD") ?? GameObject.Find("Canvas_HUD") ?? GameObject.Find("HUD_Canvas");
        if (canvasGo == null)
        {
            var uiRoot = GameObject.Find("UIRoot") ?? new GameObject("UIRoot");
            Undo.RegisterCreatedObjectUndo(uiRoot, "Create UIRoot");
            canvasGo = new GameObject("Canvas_HUD", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas_HUD");
            canvasGo.transform.SetParent(uiRoot.transform, false);
        }

        var canvas = GetOrAdd<Canvas>(canvasGo);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = GetOrAdd<CanvasScaler>(canvasGo);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GetOrAdd<GraphicRaycaster>(canvasGo);
        Stretch(canvasGo.GetComponent<RectTransform>());
        return canvas;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
            return;

        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
    }

    private static GameObject GetOrCreateUI(string name, Transform parent)
    {
        var child = parent.Find(name);
        if (child != null)
            return child.gameObject;

        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject GetOrCreatePanel(string name, Transform parent, Color color)
    {
        var go = GetOrCreateUI(name, parent);
        var image = GetOrAdd<Image>(go);
        image.color = color;
        return go;
    }

    private static TMP_Text CreateLabel(string name, Transform parent, string value, int size, TextAlignmentOptions alignment, Color color)
    {
        var go = GetOrCreateUI(name, parent);
        var text = GetOrAdd<TextMeshProUGUI>(go);
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
    }

    private static void SetLayoutSize(GameObject go, float width, float height)
    {
        var layout = GetOrAdd<LayoutElement>(go);
        layout.preferredWidth = width;
        layout.preferredHeight = height;
        layout.minWidth = width > 0f ? width : 0f;
        layout.minHeight = height > 0f ? height : 0f;
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
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
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
        if (component != null) return component;

        if (typeof(LayoutGroup).IsAssignableFrom(typeof(T)))
        {
            var existingLayout = go.GetComponent<LayoutGroup>();
            if (existingLayout != null)
            {
                Undo.DestroyObjectImmediate(existingLayout);
            }
        }

        return Undo.AddComponent<T>(go);
    }
}
