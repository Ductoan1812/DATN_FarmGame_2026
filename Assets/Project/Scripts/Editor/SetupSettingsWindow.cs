using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

/// <summary>
/// Editor utility: Build nội dung UI cho SettingsWindow trong scene.
/// </summary>
public static class SetupSettingsWindow
{
    public static void Execute()
    {
        // ── Tìm SettingsWindow gốc (kể cả inactive) ──────────────
        GameObject windowGo = null;
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            windowGo = FindDeepGameObject(root.transform, "SettingsWindow");
            if (windowGo != null) break;
        }
        if (windowGo == null)
        {
            Debug.LogError("[SetupSettingsWindow] Không tìm thấy 'SettingsWindow' trong scene.");
            return;
        }

        // Xoá children cũ
        for (int i = windowGo.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(windowGo.transform.GetChild(i).gameObject);

        // ── Background ────────────────────────────────────────────
        var bg = windowGo.GetComponent<Image>() ?? windowGo.AddComponent<Image>();
        var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/UI/Panels/Panel_Menu.png");
        if (panelSprite != null) bg.sprite = panelSprite;
        bg.color = new Color(1f, 1f, 1f, 0.96f);

        var outline = windowGo.GetComponent<Outline>() ?? windowGo.AddComponent<Outline>();
        outline.effectColor    = new Color(0.78f, 0.54f, 0.20f, 1f);
        outline.effectDistance = new Vector2(3f, -3f);

        // ── Gắn SettingsWindowUI ─────────────────────────────────
        var settingsUI = windowGo.GetComponent<SettingsWindowUI>() ?? windowGo.AddComponent<SettingsWindowUI>();

        // ── Root VerticalLayoutGroup ──────────────────────────────
        var rootLayout = windowGo.GetComponent<VerticalLayoutGroup>() ?? windowGo.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding           = new RectOffset(0, 0, 0, 0);
        rootLayout.spacing           = 0f;
        rootLayout.childAlignment    = TextAnchor.UpperLeft;
        rootLayout.childControlWidth  = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth  = true;
        rootLayout.childForceExpandHeight = false;

        // ── Header ────────────────────────────────────────────────
        var header = CreateChild("Header", windowGo.transform);
        AddLayoutElement(header, minH: 60f, prefH: 60f, flexW: 1f);
        var headerImg = header.AddComponent<Image>();
        headerImg.color = new Color(0.30f, 0.17f, 0.07f, 0.95f);
        var headerHL = header.AddComponent<HorizontalLayoutGroup>();
        headerHL.padding          = new RectOffset(20, 12, 0, 0);
        headerHL.spacing          = 8f;
        headerHL.childAlignment   = TextAnchor.MiddleLeft;
        headerHL.childControlWidth  = true;
        headerHL.childControlHeight = true;
        headerHL.childForceExpandWidth  = false;
        headerHL.childForceExpandHeight = true;

        var headerTitle = CreateChild("TitleText", header.transform);
        var headerTitleTMP = headerTitle.AddComponent<TextMeshProUGUI>();
        headerTitleTMP.text      = "Cài đặt";
        headerTitleTMP.fontSize  = 26f;
        headerTitleTMP.fontStyle = FontStyles.Bold;
        headerTitleTMP.color     = new Color(0.98f, 0.88f, 0.55f);
        headerTitleTMP.alignment = TextAlignmentOptions.MidlineLeft;
        AddLayoutElement(headerTitle, flexW: 1f);

        var closeBtnGo = CreateChild("CloseButton", header.transform);
        AddLayoutElement(closeBtnGo, minW: 44f, prefW: 44f, minH: 44f, prefH: 44f);
        var closeBtnImg = closeBtnGo.AddComponent<Image>();
        closeBtnImg.color = new Color(0.70f, 0.22f, 0.13f, 0.95f);
        var closeBtn = closeBtnGo.AddComponent<Button>();
        closeBtn.targetGraphic = closeBtnImg;
        var closeLabelGo = CreateChild("Label", closeBtnGo.transform);
        SetStretchFull(closeLabelGo);
        var closeLabelTMP = closeLabelGo.AddComponent<TextMeshProUGUI>();
        closeLabelTMP.text      = "✕";
        closeLabelTMP.fontSize  = 22f;
        closeLabelTMP.alignment = TextAlignmentOptions.Center;
        closeLabelTMP.color     = Color.white;

        // ── Body ──────────────────────────────────────────────────
        var body = CreateChild("Body", windowGo.transform);
        AddLayoutElement(body, flexH: 1f, flexW: 1f);
        var bodyVL = body.AddComponent<VerticalLayoutGroup>();
        bodyVL.padding           = new RectOffset(40, 40, 24, 24);
        bodyVL.spacing           = 18f;
        bodyVL.childAlignment    = TextAnchor.UpperLeft;
        bodyVL.childControlWidth  = true;
        bodyVL.childControlHeight = true;
        bodyVL.childForceExpandWidth  = true;
        bodyVL.childForceExpandHeight = false;

        // ── Volume Section Label ───────────────────────────────────
        var volSectionLabel = CreateChild("SectionVolume", body.transform);
        AddLayoutElement(volSectionLabel, minH: 32f, prefH: 32f, flexW: 1f);
        var volSectionTMP = volSectionLabel.AddComponent<TextMeshProUGUI>();
        volSectionTMP.text      = "Âm lượng";
        volSectionTMP.fontSize  = 22f;
        volSectionTMP.fontStyle = FontStyles.Bold;
        volSectionTMP.color     = new Color(0.98f, 0.88f, 0.55f);

        // ── 3 Slider Rows ─────────────────────────────────────────
        var (sliderMaster, labelMaster) = CreateSliderRow(body, "Master",  "SliderMaster",  "LabelMasterValue", "Tổng thể");
        var (sliderMusic,  labelMusic)  = CreateSliderRow(body, "Music",   "SliderMusic",   "LabelMusicValue",  "Nhạc nền");
        var (sliderSfx,    labelSfx)    = CreateSliderRow(body, "Sfx",     "SliderSfx",     "LabelSfxValue",    "Hiệu ứng");

        // ── Separator ─────────────────────────────────────────────
        var separator = CreateChild("Separator", body.transform);
        AddLayoutElement(separator, minH: 2f, prefH: 2f, flexW: 1f);
        var sepImg = separator.AddComponent<Image>();
        sepImg.color = new Color(0.60f, 0.40f, 0.15f, 0.40f);

        // ── Language Section ──────────────────────────────────────
        var langLabel = CreateChild("SectionLanguage", body.transform);
        AddLayoutElement(langLabel, minH: 32f, prefH: 32f, flexW: 1f);
        var langLabelTMP = langLabel.AddComponent<TextMeshProUGUI>();
        langLabelTMP.text      = "Ngôn ngữ";
        langLabelTMP.fontSize  = 22f;
        langLabelTMP.fontStyle = FontStyles.Bold;
        langLabelTMP.color     = new Color(0.98f, 0.88f, 0.55f);

        var langRow = CreateChild("LangRow", body.transform);
        AddLayoutElement(langRow, minH: 44f, prefH: 44f, flexW: 1f);
        var langRowHL = langRow.AddComponent<HorizontalLayoutGroup>();
        langRowHL.spacing           = 12f;
        langRowHL.childAlignment    = TextAnchor.MiddleLeft;
        langRowHL.childControlWidth  = true;
        langRowHL.childControlHeight = true;
        langRowHL.childForceExpandWidth  = false;
        langRowHL.childForceExpandHeight = true;

        var langNameGo = CreateChild("LangName", langRow.transform);
        AddLayoutElement(langNameGo, minW: 140f, prefW: 140f);
        var langNameTMP = langNameGo.AddComponent<TextMeshProUGUI>();
        langNameTMP.text      = "Ngôn ngữ";
        langNameTMP.fontSize  = 20f;
        langNameTMP.color     = new Color(0.90f, 0.80f, 0.58f);
        langNameTMP.alignment = TextAlignmentOptions.MidlineLeft;

        var dropdownGo = CreateChild("LanguageDropdown", langRow.transform);
        AddLayoutElement(dropdownGo, minW: 220f, prefW: 220f, minH: 40f, prefH: 40f);
        var dropdownImg = dropdownGo.AddComponent<Image>();
        dropdownImg.color = new Color(0.25f, 0.14f, 0.06f, 0.92f);
        var dropdown = dropdownGo.AddComponent<TMP_Dropdown>();
        dropdown.targetGraphic = dropdownImg;

        // Dropdown template minimal
        var ddLabel = CreateChild("Label", dropdownGo.transform);
        SetStretchFull(ddLabel);
        var ddLabelTMP = ddLabel.AddComponent<TextMeshProUGUI>();
        ddLabelTMP.text      = string.Empty;
        ddLabelTMP.fontSize  = 18f;
        ddLabelTMP.color     = new Color(0.95f, 0.85f, 0.60f);
        ddLabelTMP.alignment = TextAlignmentOptions.MidlineLeft;
        var ddLabelRect = ddLabel.GetComponent<RectTransform>();
        ddLabelRect.offsetMin = new Vector2(10f, 0f);
        ddLabelRect.offsetMax = new Vector2(-30f, 0f);
        dropdown.captionText = ddLabelTMP;

        // Arrow
        var arrowGo = CreateChild("Arrow", dropdownGo.transform);
        var arrowRect = arrowGo.GetComponent<RectTransform>();
        arrowRect.anchorMin        = new Vector2(1f, 0.5f);
        arrowRect.anchorMax        = new Vector2(1f, 0.5f);
        arrowRect.pivot            = new Vector2(1f, 0.5f);
        arrowRect.sizeDelta        = new Vector2(24f, 24f);
        arrowRect.anchoredPosition = new Vector2(-6f, 0f);
        var arrowTMP = arrowGo.AddComponent<TextMeshProUGUI>();
        arrowTMP.text      = "▼";
        arrowTMP.fontSize  = 14f;
        arrowTMP.color     = new Color(0.95f, 0.85f, 0.60f);
        arrowTMP.alignment = TextAlignmentOptions.Center;

        // ── Spacer ─────────────────────────────────────────────────
        var spacer = CreateChild("Spacer", body.transform);
        AddLayoutElement(spacer, flexH: 1f, flexW: 1f);

        // ── Save Button ────────────────────────────────────────────
        var saveBtnGo = CreateChild("SaveButton", body.transform);
        AddLayoutElement(saveBtnGo, minH: 52f, prefH: 52f, flexW: 1f);
        var saveBtnImg = saveBtnGo.AddComponent<Image>();
        saveBtnImg.color = new Color(0.35f, 0.58f, 0.22f, 0.95f);
        var saveBtn = saveBtnGo.AddComponent<Button>();
        saveBtn.targetGraphic = saveBtnImg;
        var saveLabelGo = CreateChild("Label", saveBtnGo.transform);
        SetStretchFull(saveLabelGo);
        var saveLabelTMP = saveLabelGo.AddComponent<TextMeshProUGUI>();
        saveLabelTMP.text      = "Lưu & Đóng";
        saveLabelTMP.fontSize  = 22f;
        saveLabelTMP.fontStyle = FontStyles.Bold;
        saveLabelTMP.color     = Color.white;
        saveLabelTMP.alignment = TextAlignmentOptions.Center;

        // ── Wire refs vào SettingsWindowUI ────────────────────────
        Undo.RegisterCompleteObjectUndo(settingsUI, "Setup SettingsWindow UI");
        var so = new SerializedObject(settingsUI);
        so.FindProperty("sliderMaster").objectReferenceValue     = sliderMaster;
        so.FindProperty("sliderMusic").objectReferenceValue      = sliderMusic;
        so.FindProperty("sliderSfx").objectReferenceValue        = sliderSfx;
        so.FindProperty("labelMasterValue").objectReferenceValue = labelMaster;
        so.FindProperty("labelMusicValue").objectReferenceValue  = labelMusic;
        so.FindProperty("labelSfxValue").objectReferenceValue    = labelSfx;
        so.FindProperty("languageDropdown").objectReferenceValue = dropdown;
        so.FindProperty("saveButton").objectReferenceValue       = saveBtn;
        so.FindProperty("closeButton").objectReferenceValue      = closeBtn;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(windowGo);
        Debug.Log("[SetupSettingsWindow] ✅ SettingsWindow UI đã được build thành công.");
    }

    // ── Factory ───────────────────────────────────────────────────

    private static (Slider slider, TMP_Text valueLabel) CreateSliderRow(
        GameObject parent, string rowName, string sliderName, string valueName, string labelStr)
    {
        var row = CreateChild("Row_" + rowName, parent.transform);
        AddLayoutElement(row, minH: 44f, prefH: 44f, flexW: 1f);
        var rowHL = row.AddComponent<HorizontalLayoutGroup>();
        rowHL.spacing           = 12f;
        rowHL.childAlignment    = TextAnchor.MiddleLeft;
        rowHL.childControlWidth  = true;
        rowHL.childControlHeight = true;
        rowHL.childForceExpandWidth  = false;
        rowHL.childForceExpandHeight = true;

        // Label
        var labelGo = CreateChild("Label_" + rowName, row.transform);
        AddLayoutElement(labelGo, minW: 140f, prefW: 140f);
        var labelTMP = labelGo.AddComponent<TextMeshProUGUI>();
        labelTMP.text      = labelStr;
        labelTMP.fontSize  = 20f;
        labelTMP.color     = new Color(0.90f, 0.80f, 0.58f);
        labelTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // Slider
        var sliderGo = CreateChild(sliderName, row.transform);
        AddLayoutElement(sliderGo, flexW: 1f, minH: 24f);
        var sliderBg = sliderGo.AddComponent<Image>();
        sliderBg.color = new Color(0.20f, 0.11f, 0.04f, 0.75f);
        var slider = sliderGo.AddComponent<Slider>();
        slider.minValue  = 0f;
        slider.maxValue  = 1f;
        slider.value     = 1f;
        slider.direction = Slider.Direction.LeftToRight;

        // Fill area
        var fillArea = CreateChild("Fill Area", sliderGo.transform);
        var fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin        = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax        = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin        = new Vector2(5f, 0f);
        fillAreaRect.offsetMax        = new Vector2(-5f, 0f);

        var fillGo = CreateChild("Fill", fillArea.transform);
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.color = new Color(0.72f, 0.48f, 0.16f, 0.95f);
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin        = Vector2.zero;
        fillRect.anchorMax        = Vector2.one;
        fillRect.offsetMin        = Vector2.zero;
        fillRect.offsetMax        = new Vector2(10f, 0f);
        slider.fillRect = fillRect;

        // Handle
        var handleArea = CreateChild("Handle Slide Area", sliderGo.transform);
        var handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin  = Vector2.zero;
        handleAreaRect.anchorMax  = Vector2.one;
        handleAreaRect.offsetMin  = new Vector2(10f, 0f);
        handleAreaRect.offsetMax  = new Vector2(-10f, 0f);

        var handleGo = CreateChild("Handle", handleArea.transform);
        var handleImg = handleGo.AddComponent<Image>();
        handleImg.color = new Color(0.95f, 0.78f, 0.35f, 1f);
        var handleRect = handleGo.GetComponent<RectTransform>();
        handleRect.anchorMin  = new Vector2(0f, 0f);
        handleRect.anchorMax  = new Vector2(0f, 1f);
        handleRect.sizeDelta  = new Vector2(20f, 0f);
        slider.handleRect      = handleRect;
        slider.targetGraphic   = handleImg;

        // Value Label
        var valueLabelGo = CreateChild(valueName, row.transform);
        AddLayoutElement(valueLabelGo, minW: 56f, prefW: 56f);
        var valueLabelTMP = valueLabelGo.AddComponent<TextMeshProUGUI>();
        valueLabelTMP.text      = "100%";
        valueLabelTMP.fontSize  = 18f;
        valueLabelTMP.fontStyle = FontStyles.Bold;
        valueLabelTMP.color     = new Color(0.98f, 0.88f, 0.55f);
        valueLabelTMP.alignment = TextAlignmentOptions.MidlineRight;

        return (slider, valueLabelTMP);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static GameObject CreateChild(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void SetStretchFull(GameObject go)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin  = Vector2.zero;
        r.anchorMax  = Vector2.one;
        r.pivot      = new Vector2(0.5f, 0.5f);
        r.offsetMin  = Vector2.zero;
        r.offsetMax  = Vector2.zero;
    }

    private static void AddLayoutElement(GameObject go,
        float minW = -1f, float prefW = -1f, float flexW = -1f,
        float minH = -1f, float prefH = -1f, float flexH = -1f)
    {
        var le = go.AddComponent<LayoutElement>();
        if (minW  >= 0f) le.minWidth       = minW;
        if (prefW >= 0f) le.preferredWidth  = prefW;
        if (flexW >= 0f) le.flexibleWidth   = flexW;
        if (minH  >= 0f) le.minHeight       = minH;
        if (prefH >= 0f) le.preferredHeight = prefH;
        if (flexH >= 0f) le.flexibleHeight  = flexH;
    }

    private static GameObject FindDeepGameObject(Transform root, string name)
    {
        if (root.name == name) return root.gameObject;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeepGameObject(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
