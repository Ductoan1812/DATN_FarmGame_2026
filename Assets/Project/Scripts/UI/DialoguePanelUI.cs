using System.Collections.Generic;
using DialogueGraphTool;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DialoguePanelUI : MonoBehaviour
{
    private const float DefaultOptionSpacing = 10f;

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text lineText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private string portraitResourcesPath = "Portraits/";
    [SerializeField] private string defaultInteractionLineKey = "ui.dialogue.default_interaction_line";
    [FormerlySerializedAs("choicesRoot")]
    [SerializeField] private Transform optionsRoot;
    [FormerlySerializedAs("choiceButtonPrefab")]
    [SerializeField] private Button optionButtonPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private string externalWindowId = "dialogue";

    [Header("Dynamic Layout")]
    [SerializeField] private float panelMinHeight = 250f;
    [SerializeField] private float panelMaxHeight = 420f;
    [SerializeField] private float panelHeightPaddingFromOptions = 92f;
    [SerializeField] private float optionMinHeight = 54f;
    [SerializeField] private float optionsTopPadding = 70f;
    [SerializeField] private float optionSpacing = DefaultOptionSpacing;
    [SerializeField] private ScrollRect optionsScrollRect;
    [SerializeField] private RectTransform optionsContentRect;
    [SerializeField] private Scrollbar optionsScrollbar;

    private readonly List<Button> spawnedChoices = new();
    private EventBus subscribedBus;
    private bool closeListenerRegistered;
    private RectTransform panelRect;
    private RectTransform optionsRootRect;
    private VerticalLayoutGroup optionsLayout;
    private LayoutElement panelLayoutElement;
    private LayoutElement optionsRootLayoutElement;
    private LayoutElement optionsContentLayoutElement;
    private UIController uiController;

    private void Awake()
    {
        EnsureBasicLayout();
        CacheLayoutReferences();
        ConfigureDynamicLayout();
        ResolveUIController();
    }

    private void OnEnable()
    {
        EnsureBasicLayout();
        CacheLayoutReferences();
        ConfigureDynamicLayout();
        ResolveUIController();
        TrySubscribe();

        if (closeButton != null && !closeListenerRegistered)
        {
            closeButton.onClick.AddListener(Hide);
            closeListenerRegistered = true;
        }

        Hide();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void Update()
    {
        if (uiController == null)
            ResolveUIController();

        if (subscribedBus == null)
            TrySubscribe();
    }

    private void OnDisable()
    {
        if (subscribedBus != null)
        {
            subscribedBus.Unsubscribe<InteractionOptionsReadyPublish>(OnInteractionOptionsReady);
            subscribedBus.Unsubscribe<DialogueViewPublish>(OnDialogueView);
            subscribedBus.Unsubscribe<DialoguePortraitNodePublish>(OnPortraitNode);
            subscribedBus.Unsubscribe<ShopViewPublish>(OnShopView);
            subscribedBus = null;
        }

        if (closeButton != null && closeListenerRegistered)
        {
            closeButton.onClick.RemoveListener(Hide);
            closeListenerRegistered = false;
        }

        uiController?.CloseExternalExclusiveWindow(externalWindowId);
    }

    private void OnInteractionOptionsReady(InteractionOptionsReadyPublish e)
    {
        Debug.Log($"[DialoguePanelUI] Received {e.options?.Count ?? 0} root interaction option(s).");
        if (e.options == null || e.options.Count == 0) return;

        ClearOptions();
        Show();

        string speakerKey = e.target?.entityData?.keyName;
        string lineKey = e.target?.entityData?.descKey;
        if (string.IsNullOrWhiteSpace(lineKey))
            lineKey = defaultInteractionLineKey;

        SetLocalizedText(speakerNameText, speakerKey);
        SetLocalizedText(lineText, lineKey);

        foreach (var option in e.options)
        {
            if (option == null) continue;
            SpawnOption(option.TextKey, option.Execute);
        }

        SpawnOption("ui.dialogue.goodbye", Hide);
        RefreshLayoutSizes();
    }

    private void OnDialogueView(DialogueViewPublish e)
    {
        Debug.Log($"[DialoguePanelUI] Received dialogue view: graph='{e.viewData?.GraphId}', line='{e.viewData?.LineKey}'.");
        if (e.viewData == null) return;

        ClearOptions();
        Show();

        SetLocalizedText(speakerNameText, e.viewData.SpeakerNameKey);
        SetLocalizedText(lineText, e.viewData.LineKey);
        SetPortrait(e.viewData.PortraitKey);

        if (e.viewData.Choices != null)
        {
            foreach (var choice in e.viewData.Choices)
            {
                if (choice == null) continue;
                SpawnOption(choice.TextKey, () =>
                {
                    if (choice.Execute == null) Hide();
                    else choice.Execute.Invoke();
                });
            }
        }

        RefreshLayoutSizes();
    }

    private void OnPortraitNode(DialoguePortraitNodePublish e)
    {
        SetPortrait(e.portraitKey);
    }

    private void OnShopView(ShopViewPublish e)
    {
        Hide();
    }

    private void Show()
    {
        ResolveUIController();
        uiController?.OpenExternalExclusiveWindow(externalWindowId);

        if (panel != null) panel.SetActive(true);
        else gameObject.SetActive(true);
    }

    private void Hide()
    {
        ClearOptions();
        ResetLayoutToMinimum();

        if (panel != null) panel.SetActive(false);
        else gameObject.SetActive(false);

        uiController?.CloseExternalExclusiveWindow(externalWindowId);
    }

    private void ClearOptions()
    {
        foreach (var button in spawnedChoices)
        {
            if (button == null)
                continue;

            var go = button.gameObject;
            go.SetActive(false);
            var optionsParent = GetOptionsContentParent();
            if (optionsParent != null && button.transform.parent == optionsParent)
                button.transform.SetParent(null, false);

            Destroy(go);
        }
        spawnedChoices.Clear();
    }

    private void SpawnOption(string textKey, System.Action execute)
    {
        if (optionButtonPrefab == null || optionsRoot == null) return;

        var button = Instantiate(optionButtonPrefab, GetOptionsContentParent());
        button.gameObject.SetActive(true);
        EnsureOptionLayout(button);
        spawnedChoices.Add(button);

        SetLocalizedText(button.GetComponentInChildren<TMP_Text>(true), textKey);
        button.onClick.AddListener(() => execute?.Invoke());
    }

    private void CacheLayoutReferences()
    {
        AutoFindRefs();
        panelRect = panel != null ? panel.GetComponent<RectTransform>() : GetComponent<RectTransform>();
        optionsRootRect = optionsRoot as RectTransform;
    }

    private void AutoFindRefs()
    {
        panel ??= gameObject;
        speakerNameText ??= FindText(transform, "SpeakerNameText");
        lineText ??= FindText(transform, "LineText");
        portraitImage ??= FindImage(transform, "PortraitImage");
        optionsRoot ??= FindDeepChild(transform, "OptionsRoot")
                     ?? FindDeepChild(transform, "ChoicesRoot");
        optionButtonPrefab ??= FindButton(transform, "OptionButtonTemplate")
                            ?? FindButton(transform, "ChoiceButtonTemplate");
        closeButton ??= FindButton(transform, "CloseButton");
    }

    private void EnsureBasicLayout()
    {
        AutoFindRefs();
        if (speakerNameText != null && lineText != null && optionsRoot != null && optionButtonPrefab != null)
            return;

        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        panel = gameObject;
        var bg = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        bg.color = new Color(0.28f, 0.15f, 0.06f, 0.92f);

        var outline = GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.78f, 0.54f, 0.20f, 1f);
        outline.effectDistance = new Vector2(3f, -3f);

        portraitImage = CreateImage("PortraitImage", transform, new Color(0.14f, 0.08f, 0.04f, 0.88f));
        SetRect(portraitImage.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -24f), new Vector2(128f, 128f));

        speakerNameText = CreateText("SpeakerNameText", transform, "NPC", 24f, TextAlignmentOptions.MidlineLeft, new Color(1f, 0.86f, 0.50f));
        SetRect(speakerNameText.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f), new Vector2(184f, -22f), new Vector2(-250f, 36f));

        lineText = CreateText("LineText", transform, "Hãy chọn một hành động để tiếp tục tương tác.", 20f, TextAlignmentOptions.TopLeft, new Color(0.96f, 0.86f, 0.66f));
        lineText.enableWordWrapping = true;
        SetRect(lineText.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f), new Vector2(184f, -66f), new Vector2(-250f, 74f));

        closeButton = CreateDialogueButton("CloseButton", transform, "X");
        SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), Vector2.one, new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(46f, 40f));

        var options = new GameObject("OptionsRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        options.transform.SetParent(transform, false);
        optionsRoot = options.transform;
        var optionsRect = (RectTransform)optionsRoot;
        SetRect(optionsRect, new Vector2(0f, 0f), Vector2.one, new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(-56f, 122f));
        options.GetComponent<Image>().color = new Color(0.18f, 0.10f, 0.04f, 0.35f);

        optionButtonPrefab = CreateDialogueButton("OptionButtonTemplate", optionsRoot, "Lựa chọn");
        optionButtonPrefab.gameObject.SetActive(false);
    }

    private void ConfigureDynamicLayout()
    {
        if (panelRect != null)
        {
            panelLayoutElement = GetOrAdd<LayoutElement>(panelRect.gameObject);
            panelLayoutElement.minHeight = Mathf.Max(panelLayoutElement.minHeight, panelMinHeight);
        }

        if (optionsRootRect == null)
            return;

        EnsureOptionsScrollLayout();
        if (optionsContentRect == null)
            return;

        optionsLayout = GetOrAdd<VerticalLayoutGroup>(optionsContentRect.gameObject);
        optionsLayout.spacing = optionSpacing;
        optionsLayout.childAlignment = TextAnchor.UpperCenter;
        optionsLayout.childControlWidth = true;
        optionsLayout.childControlHeight = true;
        optionsLayout.childForceExpandWidth = true;
        optionsLayout.childForceExpandHeight = false;

        optionsRootLayoutElement = GetOrAdd<LayoutElement>(optionsRootRect.gameObject);
        optionsRootLayoutElement.minHeight = Mathf.Max(0f, optionsRootLayoutElement.minHeight);

        optionsContentLayoutElement = GetOrAdd<LayoutElement>(optionsContentRect.gameObject);

        var fitter = GetOrAdd<ContentSizeFitter>(optionsContentRect.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        optionsRootRect.anchorMin = new Vector2(optionsRootRect.anchorMin.x, 1f);
        optionsRootRect.anchorMax = new Vector2(optionsRootRect.anchorMax.x, 1f);
        optionsRootRect.pivot = new Vector2(optionsRootRect.pivot.x, 1f);
        optionsRootRect.anchoredPosition = new Vector2(optionsRootRect.anchoredPosition.x, -optionsTopPadding);
    }

    private void EnsureOptionsScrollLayout()
    {
        if (optionsRootRect == null)
            return;

        var oldLayout = optionsRootRect.GetComponent<VerticalLayoutGroup>();
        if (oldLayout != null)
            oldLayout.enabled = false;

        var oldFitter = optionsRootRect.GetComponent<ContentSizeFitter>();
        if (oldFitter != null)
            oldFitter.enabled = false;

        var mask = GetOrAdd<RectMask2D>(optionsRootRect.gameObject);
        mask.enabled = true;

        optionsScrollRect = optionsScrollRect != null
            ? optionsScrollRect
            : GetOrAdd<ScrollRect>(optionsRootRect.gameObject);
        optionsScrollRect.horizontal = false;
        optionsScrollRect.vertical = true;
        optionsScrollRect.movementType = ScrollRect.MovementType.Clamped;
        optionsScrollRect.inertia = true;

        if (optionsContentRect == null)
        {
            var content = optionsRootRect.Find("OptionsContent") as RectTransform;
            if (content == null)
            {
                var contentObject = new GameObject("OptionsContent", typeof(RectTransform));
                contentObject.transform.SetParent(optionsRootRect, false);
                content = contentObject.GetComponent<RectTransform>();
            }

            optionsContentRect = content;
        }

        optionsContentRect.anchorMin = new Vector2(0f, 1f);
        optionsContentRect.anchorMax = new Vector2(1f, 1f);
        optionsContentRect.pivot = new Vector2(0.5f, 1f);
        optionsContentRect.anchoredPosition = Vector2.zero;
        optionsContentRect.offsetMin = new Vector2(0f, optionsContentRect.offsetMin.y);
        optionsContentRect.offsetMax = new Vector2(0f, optionsContentRect.offsetMax.y);

        for (int i = optionsRootRect.childCount - 1; i >= 0; i--)
        {
            var child = optionsRootRect.GetChild(i);
            if (child == optionsContentRect || child.name == "OptionsScrollbar")
                continue;

            child.SetParent(optionsContentRect, false);
        }

        optionsScrollbar = optionsScrollbar != null
            ? optionsScrollbar
            : FindOrCreateOptionsScrollbar();

        optionsScrollRect.viewport = optionsRootRect;
        optionsScrollRect.content = optionsContentRect;
        optionsScrollRect.verticalScrollbar = optionsScrollbar;
        optionsScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        optionsScrollRect.verticalScrollbarSpacing = 4f;
    }

    private void EnsureOptionLayout(Button button)
    {
        if (button == null)
            return;

        var layoutElement = GetOrAdd<LayoutElement>(button.gameObject);
        layoutElement.minHeight = Mathf.Max(layoutElement.minHeight, optionMinHeight);

        if (layoutElement.preferredHeight > 0f && layoutElement.preferredHeight < optionMinHeight)
            layoutElement.preferredHeight = optionMinHeight;
    }

    private void RefreshLayoutSizes()
    {
        if (panelRect == null || optionsRootRect == null)
            return;

        Canvas.ForceUpdateCanvases();

        float contentHeight = CalculateOptionsContentHeight();
        float maxOptionsHeight = Mathf.Max(0f, panelMaxHeight - panelHeightPaddingFromOptions);
        float optionsHeight = Mathf.Min(contentHeight, maxOptionsHeight);
        if (optionsRootLayoutElement != null)
        {
            optionsRootLayoutElement.minHeight = optionsHeight;
            optionsRootLayoutElement.preferredHeight = optionsHeight;
        }

        if (optionsContentLayoutElement != null)
        {
            optionsContentLayoutElement.minHeight = contentHeight;
            optionsContentLayoutElement.preferredHeight = contentHeight;
        }

        optionsRootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, optionsHeight);
        if (optionsContentRect != null)
            optionsContentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);

        float panelHeight = Mathf.Clamp(contentHeight + panelHeightPaddingFromOptions, panelMinHeight, panelMaxHeight);
        if (panelLayoutElement != null)
        {
            panelLayoutElement.minHeight = panelHeight;
            panelLayoutElement.preferredHeight = panelHeight;
        }

        panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelHeight);
        bool needsScrollbar = contentHeight > optionsHeight + 1f;
        if (optionsScrollbar != null)
            optionsScrollbar.gameObject.SetActive(needsScrollbar);

        if (optionsContentRect != null)
        {
            float rightOffset = needsScrollbar ? -16f : 0f;
            optionsContentRect.offsetMin = new Vector2(0f, optionsContentRect.offsetMin.y);
            optionsContentRect.offsetMax = new Vector2(rightOffset, optionsContentRect.offsetMax.y);
        }

        if (optionsScrollRect != null)
            optionsScrollRect.verticalNormalizedPosition = 1f;

        if (optionsContentRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(optionsContentRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(optionsRootRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
    }

    private void ResetLayoutToMinimum()
    {
        if (panelRect == null || optionsRootRect == null)
            return;

        if (optionsRootLayoutElement != null)
        {
            optionsRootLayoutElement.minHeight = 0f;
            optionsRootLayoutElement.preferredHeight = 0f;
        }

        if (optionsContentLayoutElement != null)
        {
            optionsContentLayoutElement.minHeight = 0f;
            optionsContentLayoutElement.preferredHeight = 0f;
        }

        optionsRootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);
        if (optionsContentRect != null)
            optionsContentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);

        if (panelLayoutElement != null)
        {
            panelLayoutElement.minHeight = panelMinHeight;
            panelLayoutElement.preferredHeight = panelMinHeight;
        }

        panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelMinHeight);
        if (optionsScrollbar != null)
            optionsScrollbar.gameObject.SetActive(false);

        if (optionsContentRect != null)
        {
            optionsContentRect.offsetMin = new Vector2(0f, optionsContentRect.offsetMin.y);
            optionsContentRect.offsetMax = new Vector2(0f, optionsContentRect.offsetMax.y);
        }

        if (optionsContentRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(optionsContentRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(optionsRootRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
    }

    private float CalculateOptionsContentHeight()
    {
        var content = optionsContentRect != null ? optionsContentRect : optionsRootRect;
        if (content == null)
            return 0f;

        float totalHeight = 0f;
        int activeChildCount = 0;

        if (optionsLayout != null)
            totalHeight += optionsLayout.padding.top + optionsLayout.padding.bottom;

        for (int i = 0; i < content.childCount; i++)
        {
            var child = content.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf)
                continue;

            float childHeight = Mathf.Max(
                optionMinHeight,
                LayoutUtility.GetMinHeight(child),
                LayoutUtility.GetPreferredHeight(child));

            totalHeight += childHeight;
            activeChildCount++;
        }

        if (activeChildCount > 1)
            totalHeight += (activeChildCount - 1) * (optionsLayout != null ? optionsLayout.spacing : optionSpacing);

        return totalHeight;
    }

    private Transform GetOptionsContentParent()
    {
        return optionsContentRect != null ? optionsContentRect : optionsRoot;
    }

    private Scrollbar FindOrCreateOptionsScrollbar()
    {
        var existing = optionsRootRect.Find("OptionsScrollbar");
        if (existing != null && existing.TryGetComponent(out Scrollbar existingScrollbar))
            return existingScrollbar;

        var scrollbarObject = new GameObject("OptionsScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbarObject.transform.SetParent(optionsRootRect, false);

        var rect = scrollbarObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(12f, 0f);
        rect.anchoredPosition = Vector2.zero;

        var background = scrollbarObject.GetComponent<Image>();
        background.color = new Color(0.18f, 0.10f, 0.04f, 0.35f);

        var slidingArea = new GameObject("SlidingArea", typeof(RectTransform));
        slidingArea.transform.SetParent(scrollbarObject.transform, false);
        SetStretch(slidingArea.GetComponent<RectTransform>(), new Vector2(2f, 2f), new Vector2(-2f, -2f));

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(slidingArea.transform, false);
        SetStretch(handle.GetComponent<RectTransform>());

        var handleImage = handle.GetComponent<Image>();
        handleImage.color = new Color(0.77f, 0.52f, 0.22f, 0.92f);

        var scrollbar = scrollbarObject.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handle.GetComponent<RectTransform>();
        return scrollbar;
    }

    private static void SetStretch(RectTransform rect)
    {
        SetStretch(rect, Vector2.zero, Vector2.zero);
    }

    private static void SetStretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rect == null) return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        var component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static void SetLocalizedText(TMP_Text text, string key)
    {
        if (text == null) return;

        var localized = text.GetComponent<LocalizedText>();
        if (localized == null)
            localized = text.gameObject.AddComponent<LocalizedText>();

        localized.SetKey(key);
    }

    private void SetPortrait(string portraitKey)
    {
        if (portraitImage == null || string.IsNullOrWhiteSpace(portraitKey)) return;

        string path = string.IsNullOrWhiteSpace(portraitResourcesPath)
            ? portraitKey
            : portraitResourcesPath.TrimEnd('/') + "/" + portraitKey;

        var sprite = Resources.Load<Sprite>(path);
        if (sprite == null)
        {
            Debug.LogWarning($"[DialoguePanelUI] Portrait sprite not found at Resources/{path}.");
            return;
        }

        portraitImage.sprite = sprite;
        portraitImage.enabled = true;
    }

    private void TrySubscribe()
    {
        if (subscribedBus != null) return;

        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;

        bus.Subscribe<InteractionOptionsReadyPublish>(OnInteractionOptionsReady);
        bus.Subscribe<DialogueViewPublish>(OnDialogueView);
        bus.Subscribe<DialoguePortraitNodePublish>(OnPortraitNode);
        bus.Subscribe<ShopViewPublish>(OnShopView);
        subscribedBus = bus;
        Debug.Log("[DialoguePanelUI] Subscribed to NPC interaction dialogue events.");
    }

    private void ResolveUIController()
    {
        if (uiController != null) return;

        uiController = GetComponent<UIController>();
        if (uiController != null) return;

        uiController = GetComponentInParent<UIController>(true);
        if (uiController != null) return;

        uiController = FindAnyObjectByType<UIController>(FindObjectsInactive.Include);
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float size, TextAlignmentOptions alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.color = color;
        return text;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.preserveAspect = true;
        return image;
    }

    private static Button CreateDialogueButton(string name, Transform parent, string label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = new Color(0.34f, 0.20f, 0.08f, 1f);
        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        var text = CreateText("Label", go.transform, label, 18f, TextAlignmentOptions.Center, new Color(1f, 0.90f, 0.66f));
        SetStretch(text.rectTransform);
        return button;
    }

    private static TMP_Text FindText(Transform root, string name)
    {
        var child = FindDeepChild(root, name);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static Image FindImage(Transform root, string name)
    {
        var child = FindDeepChild(root, name);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private static Button FindButton(Transform root, string name)
    {
        var child = FindDeepChild(root, name);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name)) return null;
        if (root.name == name) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeepChild(root.GetChild(i), name);
            if (found != null) return found;
        }

        return null;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }
}
