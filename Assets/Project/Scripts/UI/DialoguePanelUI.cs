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

    [Header("Dynamic Layout")]
    [SerializeField] private float panelMinHeight = 250f;
    [SerializeField] private float panelHeightPaddingFromOptions = 92f;
    [SerializeField] private float optionMinHeight = 54f;
    [SerializeField] private float optionsTopPadding = 70f;
    [SerializeField] private float optionSpacing = DefaultOptionSpacing;

    private readonly List<Button> spawnedChoices = new();
    private EventBus subscribedBus;
    private bool closeListenerRegistered;
    private RectTransform panelRect;
    private RectTransform optionsRootRect;
    private VerticalLayoutGroup optionsLayout;
    private LayoutElement panelLayoutElement;
    private LayoutElement optionsRootLayoutElement;

    private void Awake()
    {
        CacheLayoutReferences();
        ConfigureDynamicLayout();
    }

    private void OnEnable()
    {
        CacheLayoutReferences();
        ConfigureDynamicLayout();
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
        if (TryOpenViaRoot("dialogue")) return;

        if (panel != null) panel.SetActive(true);
        else gameObject.SetActive(true);
        UIRootController.Instance?.NotifyWindowStateChanged();
    }

    private void Hide()
    {
        ClearOptions();
        ResetLayoutToMinimum();
        if (TryCloseViaRoot("dialogue")) return;

        if (panel != null) panel.SetActive(false);
        else gameObject.SetActive(false);
        UIRootController.Instance?.NotifyWindowStateChanged();
    }

    private bool TryOpenViaRoot(string id)
    {
        var root = UIRootController.Instance;
        if (root == null || !root.TryGetEntry(id, out _)) return false;

        root.Open(id);
        return true;
    }

    private bool TryCloseViaRoot(string id)
    {
        var root = UIRootController.Instance;
        if (root == null || !root.TryGetEntry(id, out _)) return false;

        root.Close(id);
        return true;
    }

    private void ClearOptions()
    {
        foreach (var button in spawnedChoices)
        {
            if (button == null)
                continue;

            var go = button.gameObject;
            go.SetActive(false);
            if (optionsRoot != null && button.transform.parent == optionsRoot)
                button.transform.SetParent(null, false);

            Destroy(go);
        }
        spawnedChoices.Clear();
    }

    private void SpawnOption(string textKey, System.Action execute)
    {
        if (optionButtonPrefab == null || optionsRoot == null) return;

        var button = Instantiate(optionButtonPrefab, optionsRoot);
        button.gameObject.SetActive(true);
        EnsureOptionLayout(button);
        spawnedChoices.Add(button);

        SetLocalizedText(button.GetComponentInChildren<TMP_Text>(true), textKey);
        button.onClick.AddListener(() => execute?.Invoke());
    }

    private void CacheLayoutReferences()
    {
        panelRect = panel != null ? panel.GetComponent<RectTransform>() : GetComponent<RectTransform>();
        optionsRootRect = optionsRoot as RectTransform;
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

        optionsLayout = GetOrAdd<VerticalLayoutGroup>(optionsRootRect.gameObject);
        optionsLayout.spacing = optionSpacing;
        optionsLayout.childAlignment = TextAnchor.UpperCenter;
        optionsLayout.childControlWidth = true;
        optionsLayout.childControlHeight = true;
        optionsLayout.childForceExpandWidth = true;
        optionsLayout.childForceExpandHeight = false;

        optionsRootLayoutElement = GetOrAdd<LayoutElement>(optionsRootRect.gameObject);
        optionsRootLayoutElement.minHeight = Mathf.Max(0f, optionsRootLayoutElement.minHeight);

        var fitter = GetOrAdd<ContentSizeFitter>(optionsRootRect.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        optionsRootRect.anchorMin = new Vector2(optionsRootRect.anchorMin.x, 1f);
        optionsRootRect.anchorMax = new Vector2(optionsRootRect.anchorMax.x, 1f);
        optionsRootRect.pivot = new Vector2(optionsRootRect.pivot.x, 1f);
        optionsRootRect.anchoredPosition = new Vector2(optionsRootRect.anchoredPosition.x, -optionsTopPadding);
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

        float optionsHeight = CalculateOptionsContentHeight();
        if (optionsRootLayoutElement != null)
        {
            optionsRootLayoutElement.minHeight = optionsHeight;
            optionsRootLayoutElement.preferredHeight = optionsHeight;
        }

        optionsRootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, optionsHeight);

        float panelHeight = Mathf.Max(panelMinHeight, optionsHeight + panelHeightPaddingFromOptions);
        if (panelLayoutElement != null)
        {
            panelLayoutElement.minHeight = panelHeight;
            panelLayoutElement.preferredHeight = panelHeight;
        }

        panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelHeight);

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

        optionsRootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);

        if (panelLayoutElement != null)
        {
            panelLayoutElement.minHeight = panelMinHeight;
            panelLayoutElement.preferredHeight = panelMinHeight;
        }

        panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelMinHeight);
        LayoutRebuilder.ForceRebuildLayoutImmediate(optionsRootRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
    }

    private float CalculateOptionsContentHeight()
    {
        if (optionsRootRect == null)
            return 0f;

        float totalHeight = 0f;
        int activeChildCount = 0;

        if (optionsLayout != null)
            totalHeight += optionsLayout.padding.top + optionsLayout.padding.bottom;

        for (int i = 0; i < optionsRootRect.childCount; i++)
        {
            var child = optionsRootRect.GetChild(i) as RectTransform;
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
}
