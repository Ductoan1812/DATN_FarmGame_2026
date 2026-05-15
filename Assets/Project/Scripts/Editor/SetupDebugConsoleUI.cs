using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class SetupDebugConsoleUI
{
    [MenuItem("Tools/Setup Debug Console UI")]
    public static void Execute()
    {
        // Tìm GameManager/Debug
        var debug = GameObject.Find("GameManager/Debug");
        if (debug == null) { Debug.LogError("Không tìm thấy GameManager/Debug"); return; }

        // Xóa canvas cũ nếu có
        var oldCanvas = debug.transform.Find("DebugConsole_Canvas");
        if (oldCanvas != null) Object.DestroyImmediate(oldCanvas.gameObject);
        var oldMigratedCanvas = GameObject.Find("Canvas_Debug");
        if (oldMigratedCanvas != null) Object.DestroyImmediate(oldMigratedCanvas);

        var uiRoot = GameObject.Find("UIRoot");
        if (uiRoot == null) uiRoot = new GameObject("UIRoot");

        // ── Canvas ──
        var canvasGO = new GameObject("Canvas_Debug", typeof(RectTransform));
        canvasGO.transform.SetParent(uiRoot.transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Panel ──
        var panel = CreateUI("Panel", canvasGO.transform);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.85f);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(1, 0.45f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // ── ScrollArea ──
        var scrollGO = CreateUI("ScrollArea", panel.transform);
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        var scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0.1f);
        scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMin = new Vector2(10, 0);
        scrollRT.offsetMax = new Vector2(-10, -5);

        // Viewport
        var viewport = CreateUI("Viewport", scrollGO.transform);
        viewport.AddComponent<Image>().color = Color.clear;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        Stretch(viewport);
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        // Content
        var content = CreateUI("Content", viewport.transform);
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0, 1);
        contentRT.offsetMin = Vector2.zero;
        contentRT.offsetMax = Vector2.zero;
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRT;

        // LogText
        var logGO = CreateUI("LogText", content.transform);
        var logTMP = logGO.AddComponent<TextMeshProUGUI>();
        logTMP.fontSize = 16;
        logTMP.color = Color.white;
        logTMP.richText = true;
        logTMP.enableWordWrapping = true;
        logTMP.alignment = TextAlignmentOptions.BottomLeft;
        var logRT = logGO.GetComponent<RectTransform>();
        logRT.anchorMin = Vector2.zero;
        logRT.anchorMax = Vector2.one;
        logRT.offsetMin = new Vector2(5, 0);
        logRT.offsetMax = new Vector2(-5, 0);

        // ── InputField ──
        var inputGO = CreateUI("InputField", panel.transform);
        inputGO.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);
        var inputRT = inputGO.GetComponent<RectTransform>();
        inputRT.anchorMin = new Vector2(0, 0);
        inputRT.anchorMax = new Vector2(1, 0.1f);
        inputRT.offsetMin = new Vector2(5, 5);
        inputRT.offsetMax = new Vector2(-5, 0);

        var tmpInput = inputGO.AddComponent<TMP_InputField>();

        // Text Area
        var textArea = CreateUI("Text Area", inputGO.transform);
        var taRT = textArea.GetComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero;
        taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(10, 0);
        taRT.offsetMax = new Vector2(-10, 0);

        // Placeholder
        var phGO = CreateUI("Placeholder", textArea.transform);
        var phTMP = phGO.AddComponent<TextMeshProUGUI>();
        phTMP.text = "Nhập lệnh... (help để xem danh sách)";
        phTMP.fontSize = 16;
        phTMP.fontStyle = FontStyles.Italic;
        phTMP.color = new Color(1, 1, 1, 0.3f);
        phTMP.enableWordWrapping = false;
        phTMP.alignment = TextAlignmentOptions.MidlineLeft;
        Stretch(phGO);

        // Input Text
        var itGO = CreateUI("Text", textArea.transform);
        var itTMP = itGO.AddComponent<TextMeshProUGUI>();
        itTMP.fontSize = 16;
        itTMP.color = Color.white;
        itTMP.enableWordWrapping = false;
        itTMP.alignment = TextAlignmentOptions.MidlineLeft;
        Stretch(itGO);

        tmpInput.textViewport = taRT;
        tmpInput.textComponent = itTMP;
        tmpInput.placeholder = phTMP;
        tmpInput.fontAsset = itTMP.font;
        tmpInput.pointSize = 16;

        // ── SuggestionsPanel ──
        var sugPanel = CreateUI("SuggestionsPanel", panel.transform);
        sugPanel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        var sugRT = sugPanel.GetComponent<RectTransform>();
        sugRT.anchorMin = new Vector2(0, 0.1f);
        sugRT.anchorMax = new Vector2(0.4f, 0.1f);
        sugRT.pivot = new Vector2(0, 0);
        sugRT.anchoredPosition = new Vector2(5, 2);
        sugRT.sizeDelta = new Vector2(350, 152);

        var vlg = sugPanel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(8, 8, 4, 4);
        vlg.spacing = 2;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        var sugCSF = sugPanel.AddComponent<ContentSizeFitter>();
        sugCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        for (int i = 0; i < 6; i++)
        {
            var sGO = CreateUI($"Sug_{i}", sugPanel.transform);
            var sTMP = sGO.AddComponent<TextMeshProUGUI>();
            sTMP.fontSize = 14;
            sTMP.color = Color.white;
            sTMP.enableWordWrapping = false;
            sTMP.alignment = TextAlignmentOptions.MidlineLeft;
            var le = sGO.AddComponent<LayoutElement>();
            le.preferredHeight = 22;
        }

        sugPanel.SetActive(false);
        panel.SetActive(false);

        // ── Gán DebugConsole refs ──
        var console = debug.GetComponent<DebugConsole>();
        if (console == null) console = debug.AddComponent<DebugConsole>();

        var so = new SerializedObject(console);
        so.FindProperty("panel").objectReferenceValue = panel;
        so.FindProperty("inputField").objectReferenceValue = tmpInput;
        so.FindProperty("logText").objectReferenceValue = logTMP;
        so.FindProperty("scrollRect").objectReferenceValue = scrollRect;
        so.FindProperty("suggestionsPanel").objectReferenceValue = sugPanel;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(debug);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[SetupDebugConsoleUI] Done! UI đã tạo xong và gán refs.");
    }

    private static GameObject CreateUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
