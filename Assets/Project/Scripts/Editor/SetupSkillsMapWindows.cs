using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor utility: Build functional vertical-slice presentation for template Skills/Map windows.
/// </summary>
public static class SetupSkillsMapWindows
{
    private static readonly Color HeaderColor = new(0.30f, 0.17f, 0.07f, 0.95f);
    private static readonly Color GoldText = new(0.98f, 0.88f, 0.55f);
    private static readonly Color BodyText = new(0.92f, 0.80f, 0.58f);
    private static readonly Color MutedText = new(0.72f, 0.61f, 0.43f);
    private static readonly Color PanelTint = new(0.20f, 0.12f, 0.05f, 0.30f);

    [MenuItem("Tools/DATN/UI/Setup Skills And Map Windows")]
    public static void Execute()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Roboto-Bold SDF.asset");

        BuildWindow("SkillsWindow", "Thành thạo", font, BuildSkillsBody);
        BuildWindow("MapWindow", "Bản đồ", font, BuildMapBody);
    }

    private static void BuildWindow(string windowName, string title, TMP_FontAsset font, System.Action<Transform, TMP_FontAsset> buildBody)
    {
        var window = FindDeepGameObject(windowName);
        if (window == null)
        {
            Debug.LogError($"[SetupSkillsMapWindows] Không tìm thấy '{windowName}' trong scene.");
            return;
        }

        AlignWithCoreMenuContent(window);

        for (var i = window.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(window.transform.GetChild(i).gameObject);

        var background = window.GetComponent<Image>() ?? window.AddComponent<Image>();
        var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/UI/Panels/Panel_Menu.png");
        if (panelSprite != null)
            background.sprite = panelSprite;
        background.color = new Color(1f, 1f, 1f, 0.96f);

        var outline = window.GetComponent<Outline>() ?? window.AddComponent<Outline>();
        outline.effectColor = new Color(0.78f, 0.54f, 0.20f, 1f);
        outline.effectDistance = new Vector2(3f, -3f);

        var rootLayout = window.GetComponent<VerticalLayoutGroup>() ?? window.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(0, 0, 0, 0);
        rootLayout.spacing = 0f;
        rootLayout.childAlignment = TextAnchor.UpperLeft;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        var header = CreateChild("Header", window.transform);
        AddLayoutElement(header, minH: 60f, prefH: 60f, flexW: 1f);
        header.AddComponent<Image>().color = HeaderColor;

        var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
        headerLayout.padding = new RectOffset(20, 12, 0, 0);
        headerLayout.spacing = 8f;
        headerLayout.childAlignment = TextAnchor.MiddleCenter;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;

        var titleGo = CreateChild("TitleText", header.transform);
        AddLayoutElement(titleGo, flexW: 1f);
        CreateText(titleGo, title, 26f, GoldText, TextAlignmentOptions.MidlineLeft, font, FontStyles.Bold);

        var closeButtonGo = CreateChild("CloseButton", header.transform);
        AddLayoutElement(closeButtonGo, minW: 44f, prefW: 44f, minH: 44f, prefH: 44f);
        var closeImage = closeButtonGo.AddComponent<Image>();
        closeImage.color = new Color(0.70f, 0.22f, 0.13f, 0.95f);
        var closeButton = closeButtonGo.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;

        var closeLabel = CreateChild("Label", closeButtonGo.transform);
        SetStretchFull(closeLabel);
        CreateText(closeLabel, "X", 20f, Color.white, TextAlignmentOptions.Center, font, FontStyles.Bold);

        var body = CreateChild("Body", window.transform);
        AddLayoutElement(body, flexW: 1f, flexH: 1f);
        buildBody(body.transform, font);

        EditorUtility.SetDirty(window);
        Debug.Log($"[SetupSkillsMapWindows] Built {windowName}.");
    }

    private static void BuildSkillsBody(Transform body, TMP_FontAsset font)
    {
        var layout = body.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(34, 34, 24, 30);
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var summary = CreatePanel("MasterySummary", body, minH: 112f, prefH: 112f);
        var summaryLayout = summary.AddComponent<VerticalLayoutGroup>();
        summaryLayout.padding = new RectOffset(20, 20, 16, 16);
        summaryLayout.spacing = 6f;
        summaryLayout.childAlignment = TextAnchor.UpperLeft;
        summaryLayout.childControlWidth = true;
        summaryLayout.childControlHeight = true;
        summaryLayout.childForceExpandWidth = true;
        summaryLayout.childForceExpandHeight = false;

        CreateTextRow(summary.transform, "Tiến trình kỹ năng", 22f, GoldText, font, FontStyles.Bold);
        CreateTextRow(summary.transform, "Các nhánh thành thạo mở theo vòng chơi nông trại, khai khoáng và chiến đấu.", 18f, BodyText, font);
        CreateTextRow(summary.transform, "Cấp thành thạo hiện tại sẽ hiển thị khi dữ liệu kỹ năng được nối vào window.", 16f, MutedText, font);

        var listHeader = CreateTextContainer("MasteryHeader", body);
        AddLayoutElement(listHeader, minH: 32f, prefH: 32f, flexW: 1f);
        CreateText(listHeader, "Nhánh thành thạo", 22f, GoldText, TextAlignmentOptions.MidlineLeft, font, FontStyles.Bold);

        var list = CreateChild("MasteryRows", body);
        AddLayoutElement(list, flexW: 1f);
        var listLayout = list.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 10f;
        listLayout.childAlignment = TextAnchor.UpperLeft;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;

        CreateMasteryRow(list.transform, "Nông nghiệp", "Trồng trọt và thu hoạch", font);
        CreateMasteryRow(list.transform, "Khai khoáng", "Quặng và tài nguyên tầng mỏ", font);
        CreateMasteryRow(list.transform, "Chiến đấu", "EXP chiến đấu và sức mạnh trang bị", font);
    }

    private static void AlignWithCoreMenuContent(GameObject window)
    {
        var rect = window.GetComponent<RectTransform>();
        if (rect == null) return;

        rect.anchoredPosition = new Vector2(-120f, 30f);
    }

    private static void BuildMapBody(Transform body, TMP_FontAsset font)
    {
        var layout = body.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(34, 34, 24, 30);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        var mapPanel = CreatePanel("MapPreview", body, flexW: 1f, flexH: 1f);
        var mapLayout = mapPanel.AddComponent<VerticalLayoutGroup>();
        mapLayout.padding = new RectOffset(24, 24, 22, 22);
        mapLayout.spacing = 12f;
        mapLayout.childAlignment = TextAnchor.UpperLeft;
        mapLayout.childControlWidth = true;
        mapLayout.childControlHeight = true;
        mapLayout.childForceExpandWidth = true;
        mapLayout.childForceExpandHeight = false;

        CreateTextRow(mapPanel.transform, "Lộ trình vertical slice", 22f, GoldText, font, FontStyles.Bold);

        var route = CreatePanel("RoutePanel", mapPanel.transform, flexW: 1f, flexH: 1f);
        var routeLayout = route.AddComponent<VerticalLayoutGroup>();
        routeLayout.padding = new RectOffset(24, 24, 24, 24);
        routeLayout.spacing = 16f;
        routeLayout.childAlignment = TextAnchor.MiddleCenter;
        routeLayout.childControlWidth = true;
        routeLayout.childControlHeight = true;
        routeLayout.childForceExpandWidth = true;
        routeLayout.childForceExpandHeight = false;

        CreateLocationChip(route.transform, "Nông trại", font);
        CreateRouteLine(route.transform);
        CreateLocationChip(route.transform, "Thị trấn", font);
        CreateRouteLine(route.transform);
        CreateLocationChip(route.transform, "Khu mỏ", font);

        var legend = CreatePanel("MapLegend", body, minW: 340f, prefW: 340f, flexH: 1f);
        var legendLayout = legend.AddComponent<VerticalLayoutGroup>();
        legendLayout.padding = new RectOffset(20, 20, 18, 18);
        legendLayout.spacing = 10f;
        legendLayout.childAlignment = TextAnchor.UpperLeft;
        legendLayout.childControlWidth = true;
        legendLayout.childControlHeight = true;
        legendLayout.childForceExpandWidth = true;
        legendLayout.childForceExpandHeight = false;

        CreateTextRow(legend.transform, "Điểm đến", 22f, GoldText, font, FontStyles.Bold);
        CreateLegendRow(legend.transform, "Farm", "Ruộng, nhà, giường", font);
        CreateLegendRow(legend.transform, "Town", "Shop, craft, quest", font);
        CreateLegendRow(legend.transform, "Mine", "Ore, enemy, drop", font);
    }

    private static void CreateMasteryRow(Transform parent, string title, string detail, TMP_FontAsset font)
    {
        var row = CreatePanel("Mastery_" + title, parent, minH: 84f, prefH: 84f, flexW: 1f);
        var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(18, 18, 12, 12);
        rowLayout.spacing = 16f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        var copy = CreateChild("Copy", row.transform);
        AddLayoutElement(copy, flexW: 1f);
        var copyLayout = copy.AddComponent<VerticalLayoutGroup>();
        copyLayout.spacing = 4f;
        copyLayout.childAlignment = TextAnchor.MiddleLeft;
        copyLayout.childControlWidth = true;
        copyLayout.childControlHeight = true;
        copyLayout.childForceExpandWidth = true;
        copyLayout.childForceExpandHeight = false;
        CreateTextRow(copy.transform, title, 20f, GoldText, font, FontStyles.Bold);
        CreateTextRow(copy.transform, detail, 16f, BodyText, font);

        var state = CreateTextContainer("State", row.transform);
        AddLayoutElement(state, minW: 180f, prefW: 180f, minH: 44f, prefH: 44f);
        state.AddComponent<Image>().color = new Color(0.14f, 0.22f, 0.10f, 0.72f);
        CreateText(state, "Chưa nối dữ liệu", 16f, BodyText, TextAlignmentOptions.Center, font, FontStyles.Bold);
    }

    private static void CreateLegendRow(Transform parent, string title, string detail, TMP_FontAsset font)
    {
        var row = CreatePanel("Legend_" + title, parent, minH: 78f, prefH: 78f, flexW: 1f);
        var rowLayout = row.AddComponent<VerticalLayoutGroup>();
        rowLayout.padding = new RectOffset(14, 14, 10, 10);
        rowLayout.spacing = 3f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = false;
        CreateTextRow(row.transform, title, 19f, GoldText, font, FontStyles.Bold);
        CreateTextRow(row.transform, detail, 16f, BodyText, font);
    }

    private static void CreateLocationChip(Transform parent, string label, TMP_FontAsset font)
    {
        var chip = CreatePanel("Location_" + label, parent, minW: 300f, prefW: 300f, minH: 72f, prefH: 72f);
        chip.GetComponent<LayoutElement>().flexibleWidth = 0f;
        CreateText(chip, label, 24f, GoldText, TextAlignmentOptions.Center, font, FontStyles.Bold);
    }

    private static void CreateRouteLine(Transform parent)
    {
        var line = CreateChild("RouteLine", parent);
        AddLayoutElement(line, minW: 8f, prefW: 8f, minH: 42f, prefH: 42f);
        line.AddComponent<Image>().color = new Color(0.72f, 0.48f, 0.18f, 0.92f);
    }

    private static GameObject CreatePanel(
        string name,
        Transform parent,
        float minW = -1f,
        float prefW = -1f,
        float minH = -1f,
        float prefH = -1f,
        float flexW = -1f,
        float flexH = -1f)
    {
        var panel = CreateChild(name, parent);
        panel.AddComponent<Image>().color = PanelTint;
        AddLayoutElement(panel, minW, prefW, minH, prefH, flexW, flexH);
        return panel;
    }

    private static GameObject CreateTextContainer(string name, Transform parent)
    {
        return CreateChild(name, parent);
    }

    private static void CreateTextRow(Transform parent, string text, float size, Color color, TMP_FontAsset font, FontStyles style = FontStyles.Normal)
    {
        var row = CreateTextContainer("Text", parent);
        AddLayoutElement(row, minH: size + 8f, prefH: size + 8f, flexW: 1f);
        CreateText(row, text, size, color, TextAlignmentOptions.MidlineLeft, font, style);
    }

    private static TextMeshProUGUI CreateText(
        GameObject host,
        string text,
        float size,
        Color color,
        TextAlignmentOptions alignment,
        TMP_FontAsset font,
        FontStyles style = FontStyles.Normal)
    {
        var textHost = host;
        if (host.GetComponent<Graphic>() != null)
        {
            textHost = CreateChild("Label", host.transform);
            SetStretchFull(textHost);
        }

        var label = textHost.AddComponent<TextMeshProUGUI>();
        if (font != null)
            label.font = font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.fontStyle = style;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }

    private static GameObject FindDeepGameObject(string name)
    {
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var found = FindDeep(root.transform, name);
            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name)
            return parent;

        for (var i = 0; i < parent.childCount; i++)
        {
            var found = FindDeep(parent.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    private static GameObject CreateChild(string name, Transform parent)
    {
        var child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child;
    }

    private static void SetStretchFull(GameObject go)
    {
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void AddLayoutElement(
        GameObject go,
        float minW = -1f,
        float prefW = -1f,
        float minH = -1f,
        float prefH = -1f,
        float flexW = -1f,
        float flexH = -1f)
    {
        var layout = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        if (minW >= 0f) layout.minWidth = minW;
        if (prefW >= 0f) layout.preferredWidth = prefW;
        if (minH >= 0f) layout.minHeight = minH;
        if (prefH >= 0f) layout.preferredHeight = prefH;
        if (flexW >= 0f) layout.flexibleWidth = flexW;
        if (flexH >= 0f) layout.flexibleHeight = flexH;
    }
}
