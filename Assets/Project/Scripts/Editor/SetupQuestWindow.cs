using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

/// <summary>
/// Editor utility: Build nội dung UI cho QuestWindow trong scene.
/// </summary>
public static class SetupQuestWindow
{
    public static void Execute()
    {
        // ── Tìm QuestWindow gốc (kể cả inactive) ─────────────────
        GameObject windowGo = null;
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            windowGo = FindDeepGameObject(root.transform, "QuestWindow");
            if (windowGo != null) break;
        }
        if (windowGo == null)
        {
            Debug.LogError("[SetupQuestWindow] Không tìm thấy GameObject 'QuestWindow' trong scene.");
            return;
        }

        // Xoá children cũ (nếu có) trừ khi đã build
        if (windowGo.transform.childCount > 0)
        {
            Debug.Log("[SetupQuestWindow] QuestWindow đã có children — xoá và build lại.");
            for (int i = windowGo.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(windowGo.transform.GetChild(i).gameObject);
        }

        // ── Background sprite ────────────────────────────────────
        var bg = windowGo.GetComponent<Image>();
        if (bg == null) bg = windowGo.AddComponent<Image>();
        var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/UI/Panels/Panel_Menu.png");
        if (panelSprite != null) bg.sprite = panelSprite;
        bg.color = new Color(1f, 1f, 1f, 0.96f);

        var outline = windowGo.GetComponent<Outline>();
        if (outline == null) outline = windowGo.AddComponent<Outline>();
        outline.effectColor    = new Color(0.78f, 0.54f, 0.20f, 1f);
        outline.effectDistance = new Vector2(3f, -3f);

        // ── Gắn QuestLogWindowUI script ──────────────────────────
        var logUI = windowGo.GetComponent<QuestLogWindowUI>();
        if (logUI == null) logUI = windowGo.AddComponent<QuestLogWindowUI>();

        // ── Header ───────────────────────────────────────────────
        var header = CreateChild("Header", windowGo.transform);
        SetStretchH(header, 60f, 1f);
        SetAnchorTop(header, 60f);

        var headerImg = header.AddComponent<Image>();
        headerImg.color = new Color(0.30f, 0.17f, 0.07f, 0.95f);

        var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
        headerLayout.padding          = new RectOffset(20, 12, 0, 0);
        headerLayout.childAlignment   = TextAnchor.MiddleLeft;
        headerLayout.childControlWidth  = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth  = false;
        headerLayout.childForceExpandHeight = true;

        var titleGo = CreateChild("TitleText", header.transform);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text      = "Nhật ký nhiệm vụ";
        titleText.fontSize  = 26f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color     = new Color(0.98f, 0.88f, 0.55f);
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        var titleLE = titleGo.AddComponent<LayoutElement>();
        titleLE.flexibleWidth = 1f;

        // Close Button trong header
        var closeBtnGo = CreateChild("CloseButton", header.transform);
        SetFixedSize(closeBtnGo, 44f, 44f);
        var closeBtnImg = closeBtnGo.AddComponent<Image>();
        closeBtnImg.color = new Color(0.70f, 0.22f, 0.13f, 0.95f);
        var closeBtn = closeBtnGo.AddComponent<Button>();
        closeBtn.targetGraphic = closeBtnImg;
        var closeLabelGo = CreateChild("Label", closeBtnGo.transform);
        var closeLabelText = closeLabelGo.AddComponent<TextMeshProUGUI>();
        closeLabelText.text      = "✕";
        closeLabelText.fontSize  = 22f;
        closeLabelText.alignment = TextAlignmentOptions.Center;
        closeLabelText.color     = Color.white;
        SetStretchFull(closeLabelGo);

        // ── ScrollView Body ───────────────────────────────────────
        var scrollGo = CreateChild("ScrollView", windowGo.transform);
        SetAnchorBody(scrollGo, 60f);

        var scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical   = true;
        scrollRect.scrollSensitivity = 40f;

        // Viewport
        var viewportGo = CreateChild("Viewport", scrollGo.transform);
        SetStretchFull(viewportGo);
        viewportGo.AddComponent<RectMask2D>();
        scrollRect.viewport = viewportGo.GetComponent<RectTransform>();

        // Content
        var contentGo = CreateChild("Content", viewportGo.transform);
        var contentRect = contentGo.GetComponent<RectTransform>();
        contentRect.anchorMin        = new Vector2(0f, 1f);
        contentRect.anchorMax        = new Vector2(1f, 1f);
        contentRect.pivot            = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta        = Vector2.zero;

        var contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding           = new RectOffset(16, 16, 12, 12);
        contentLayout.spacing           = 6f;
        contentLayout.childAlignment    = TextAnchor.UpperLeft;
        contentLayout.childControlWidth  = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth  = true;
        contentLayout.childForceExpandHeight = false;

        var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.content = contentRect;

        // Empty label
        var emptyGo = CreateChild("EmptyLabel", contentGo.transform);
        var emptyText = emptyGo.AddComponent<TextMeshProUGUI>();
        emptyText.text      = "Chưa có nhiệm vụ nào.";
        emptyText.fontSize  = 20f;
        emptyText.color     = new Color(0.75f, 0.65f, 0.45f, 0.80f);
        emptyText.alignment = TextAlignmentOptions.Center;
        var emptyLE = emptyGo.AddComponent<LayoutElement>();
        emptyLE.minHeight       = 60f;
        emptyLE.preferredHeight = 60f;
        emptyLE.flexibleWidth   = 1f;

        // ── Wiring via SerializedObject ───────────────────────────
        Undo.RegisterCompleteObjectUndo(logUI, "Setup QuestWindow UI");
        var so = new SerializedObject(logUI);
        so.FindProperty("questListRoot").objectReferenceValue  = contentGo.transform;
        so.FindProperty("emptyLabel").objectReferenceValue     = emptyGo;
        so.ApplyModifiedProperties();

        // ── Scrollbar (vertical) ──────────────────────────────────
        var sbGo = CreateChild("Scrollbar", scrollGo.transform);
        var sbRect = sbGo.GetComponent<RectTransform>();
        sbRect.anchorMin        = new Vector2(1f, 0f);
        sbRect.anchorMax        = new Vector2(1f, 1f);
        sbRect.pivot            = new Vector2(1f, 0.5f);
        sbRect.sizeDelta        = new Vector2(10f, 0f);
        sbRect.anchoredPosition = Vector2.zero;

        var sbImg = sbGo.AddComponent<Image>();
        sbImg.color = new Color(0.20f, 0.12f, 0.05f, 0.50f);

        var sb = sbGo.AddComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;

        var sbHandleGo = CreateChild("Handle", sbGo.transform);
        var sbHandleImg = sbHandleGo.AddComponent<Image>();
        sbHandleImg.color = new Color(0.75f, 0.50f, 0.20f, 0.85f);
        sb.handleRect   = sbHandleGo.GetComponent<RectTransform>();
        sb.targetGraphic= sbHandleImg;

        scrollRect.verticalScrollbar = sb;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        EditorUtility.SetDirty(windowGo);
        Debug.Log("[SetupQuestWindow] ✅ QuestWindow UI đã được build thành công.");
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
        r.anchorMin        = Vector2.zero;
        r.anchorMax        = Vector2.one;
        r.pivot            = new Vector2(0.5f, 0.5f);
        r.offsetMin        = Vector2.zero;
        r.offsetMax        = Vector2.zero;
    }

    private static void SetStretchH(GameObject go, float height, float anchorY)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin  = new Vector2(0f, anchorY);
        r.anchorMax  = new Vector2(1f, anchorY);
        r.pivot      = new Vector2(0.5f, 1f);
        r.sizeDelta  = new Vector2(0f, height);
        r.anchoredPosition = Vector2.zero;
    }

    private static void SetAnchorTop(GameObject go, float height)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin        = new Vector2(0f, 1f);
        r.anchorMax        = new Vector2(1f, 1f);
        r.pivot            = new Vector2(0.5f, 1f);
        r.sizeDelta        = new Vector2(0f, height);
        r.anchoredPosition = Vector2.zero;
    }

    private static void SetAnchorBody(GameObject go, float topOffset)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin  = Vector2.zero;
        r.anchorMax  = Vector2.one;
        r.pivot      = new Vector2(0.5f, 0.5f);
        r.offsetMin  = new Vector2(0f, 0f);
        r.offsetMax  = new Vector2(0f, -topOffset);
    }

    private static void SetFixedSize(GameObject go, float w, float h)
    {
        var le = go.AddComponent<LayoutElement>();
        le.minWidth       = w; le.preferredWidth  = w;
        le.minHeight      = h; le.preferredHeight = h;
        le.flexibleWidth  = 0f; le.flexibleHeight = 0f;
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
