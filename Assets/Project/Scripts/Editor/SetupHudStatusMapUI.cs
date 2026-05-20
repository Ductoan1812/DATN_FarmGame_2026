using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class SetupHudStatusMapUI
{
    private const string UiFontAssetPath = "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Roboto-Bold SDF.asset";

    private static readonly Color Panel = new(0.34f, 0.20f, 0.08f, 0.94f);
    private static readonly Color PanelLight = new(0.86f, 0.64f, 0.34f, 0.96f);
    private static readonly Color PanelDark = new(0.18f, 0.10f, 0.04f, 0.96f);
    private static readonly Color Text = new(1f, 0.92f, 0.72f, 1f);
    private static readonly Color Ink = new(0.16f, 0.09f, 0.04f, 1f);
    private static readonly Color Frame = new(0.12f, 0.07f, 0.03f, 1f);

    [MenuItem("Tools/DATN/UI/Create HUD Status Map")]
    public static void Execute()
    {
        var canvas = GetOrCreateHudCanvas();
        EnsureEventSystem();

        var hudRoot = GetOrCreateUI("HUDRoot", canvas.transform);
        Stretch(hudRoot.GetComponent<RectTransform>());

        var root = GetOrCreatePanel("HudStatusMapPanel", hudRoot.transform, new Color(0f, 0f, 0f, 0f));
        ClearChildren(root.transform);
        SetupRect(root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-152f, -24f), new Vector2(548f, 176f));

        var layout = GetOrAdd<HorizontalLayoutGroup>(root);
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperRight;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var statusPanel = BuildStatusPanel(root.transform);
        var minimapPanel = BuildMinimapPanel(root.transform);

        var ui = GetOrAdd<HudStatusMapUI>(root);
        var serialized = new SerializedObject(ui);
        serialized.FindProperty("dateText").objectReferenceValue = statusPanel.dateText;
        serialized.FindProperty("timeText").objectReferenceValue = statusPanel.timeText;
        serialized.FindProperty("moneyText").objectReferenceValue = statusPanel.moneyText;
        serialized.ApplyModifiedProperties();

        EditorUtility.SetDirty(ui);
        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = root;

        Debug.Log($"[SetupHudStatusMapUI] Created HUD status/map UI. Minimap='{minimapPanel.name}'.");
    }

    private static StatusRefs BuildStatusPanel(Transform parent)
    {
        var statusPanel = GetOrCreatePanel("StatusPanel", parent, PanelLight);
        SetLayoutSize(statusPanel, 324f, 168f);
        AddOutline(statusPanel, Frame, new Vector2(4f, -4f));

        var rows = GetOrCreateUI("Rows", statusPanel.transform);
        Stretch(rows.GetComponent<RectTransform>(), new Vector2(12f, 10f), new Vector2(-12f, -10f));

        var rowLayout = GetOrAdd<VerticalLayoutGroup>(rows);
        rowLayout.padding = new RectOffset(0, 0, 0, 0);
        rowLayout.spacing = 8f;
        rowLayout.childAlignment = TextAnchor.UpperCenter;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = false;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = false;

        var dateText = CreateStatusRow(rows.transform, "DateRow", "D", "Ngày 1 - Xuân");
        var timeText = CreateStatusRow(rows.transform, "TimeRow", "T", "06:00 AM");
        var moneyText = CreateStatusRow(rows.transform, "MoneyRow", "$", "500");

        return new StatusRefs(dateText, timeText, moneyText);
    }

    private static GameObject BuildMinimapPanel(Transform parent)
    {
        var panel = GetOrCreatePanel("MiniMapPanel", parent, Panel);
        SetLayoutSize(panel, 210f, 168f);
        AddOutline(panel, Frame, new Vector2(4f, -4f));

        var viewport = GetOrCreatePanel("MapViewport", panel.transform, new Color(0.16f, 0.35f, 0.16f, 1f));
        Stretch(viewport.GetComponent<RectTransform>(), new Vector2(10f, 10f), new Vector2(-10f, -10f));
        AddOutline(viewport, new Color(0.68f, 0.46f, 0.18f, 1f), new Vector2(2f, -2f));

        CreateMapPatch(viewport.transform, "Water", new Color(0.10f, 0.48f, 0.62f, 1f), new Vector2(0.02f, 0.06f), new Vector2(0.42f, 0.56f));
        CreateMapPatch(viewport.transform, "GrassA", new Color(0.28f, 0.58f, 0.20f, 1f), new Vector2(0.32f, 0.02f), new Vector2(0.98f, 0.98f));
        CreateMapPatch(viewport.transform, "Path", new Color(0.72f, 0.52f, 0.26f, 1f), new Vector2(0.34f, 0.36f), new Vector2(1f, 0.58f));
        CreateMapPatch(viewport.transform, "Farm", new Color(0.45f, 0.28f, 0.12f, 1f), new Vector2(0.58f, 0.10f), new Vector2(0.86f, 0.34f));
        CreateMapDot(viewport.transform, "PlayerDot", new Color(0.22f, 0.75f, 1f, 1f), new Vector2(0.52f, 0.50f), 16f);

        var title = CreateLabel("MapLabel", viewport.transform, "Bản đồ", 18, TextAlignmentOptions.Center, Text);
        SetupRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(-16f, 24f));
        return panel;
    }

    private static TMP_Text CreateStatusRow(Transform parent, string name, string icon, string value)
    {
        var row = GetOrCreatePanel(name, parent, new Color(0.98f, 0.78f, 0.43f, 0.48f));
        SetLayoutSize(row, 298f, 46f);

        var layout = GetOrAdd<HorizontalLayoutGroup>(row);
        layout.padding = new RectOffset(12, 12, 4, 4);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var iconText = CreateLabel("Icon", row.transform, icon, 24, TextAlignmentOptions.Center, Ink);
        SetLayoutSize(iconText.gameObject, 38f, 38f);

        var valueText = CreateLabel("ValueText", row.transform, value, 28, TextAlignmentOptions.MidlineLeft, Ink);
        SetLayoutSize(valueText.gameObject, 220f, 38f);
        return valueText;
    }

    private static void CreateMapPatch(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        var patch = GetOrCreatePanel(name, parent, color);
        var rect = patch.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void CreateMapDot(Transform parent, string name, Color color, Vector2 anchor, float size)
    {
        var dot = GetOrCreatePanel(name, parent, color);
        var rect = dot.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(size, size);
        AddOutline(dot, Color.white, new Vector2(1f, -1f));
    }

    private static Canvas GetOrCreateHudCanvas()
    {
        var canvasGO = GameObject.Find("Canvas_HUD") ?? GameObject.Find("HUD_Canvas");
        if (canvasGO == null)
        {
            var uiRoot = GameObject.Find("UIRoot") ?? new GameObject("UIRoot");
            canvasGO = new GameObject("Canvas_HUD", typeof(RectTransform));
            canvasGO.transform.SetParent(uiRoot.transform, false);
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create HUD Canvas");
        }

        var canvas = GetOrAdd<Canvas>(canvasGO);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 10);

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
        image.raycastTarget = false;
        return go;
    }

    private static TMP_Text CreateLabel(string name, Transform parent, string value, int size, TextAlignmentOptions alignment, Color color)
    {
        var go = GetOrCreateUI(name, parent);
        var text = GetOrAdd<TextMeshProUGUI>(go);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiFontAssetPath);
        if (font != null)
            text.font = font;

        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        return text;
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }

    private static void SetLayoutSize(GameObject go, float width, float height)
    {
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);

        var layout = GetOrAdd<LayoutElement>(go);
        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.minHeight = height;
        layout.preferredHeight = height;
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

    private readonly struct StatusRefs
    {
        public readonly TMP_Text dateText;
        public readonly TMP_Text timeText;
        public readonly TMP_Text moneyText;

        public StatusRefs(TMP_Text dateText, TMP_Text timeText, TMP_Text moneyText)
        {
            this.dateText = dateText;
            this.timeText = timeText;
            this.moneyText = moneyText;
        }
    }
}
