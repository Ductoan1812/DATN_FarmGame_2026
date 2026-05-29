using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenUI : MonoBehaviour
{
    private const float FadeDuration = 0.22f;
    private const int OverlaySortingOrder = 5000;

    private CanvasGroup canvasGroup;
    private GameObject panel;
    private Transform uiRoot;
    private Transform canvasOverlayRoot;
    private TMP_Text titleText;
    private TMP_Text sceneText;
    private TMP_Text progressText;
    private Image progressFill;
    private EventBus subscribedBus;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        BuildIfNeeded();
        SetVisible(false, immediate: true);
    }

    private void OnEnable()
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
        if (subscribedBus == null)
            return;

        subscribedBus.Unsubscribe<LoadingScreenShowPublish>(OnShow);
        subscribedBus.Unsubscribe<LoadingScreenProgressPublish>(OnProgress);
        subscribedBus.Unsubscribe<LoadingScreenHidePublish>(OnHide);
        subscribedBus = null;
    }

    private void TrySubscribe()
    {
        if (subscribedBus != null)
            return;

        var bus = GameManager.Instance?.EventBus;
        if (bus == null)
            return;

        bus.Subscribe<LoadingScreenShowPublish>(OnShow);
        bus.Subscribe<LoadingScreenProgressPublish>(OnProgress);
        bus.Subscribe<LoadingScreenHidePublish>(OnHide);
        subscribedBus = bus;
    }

    private void OnShow(LoadingScreenShowPublish e)
    {
        BuildIfNeeded();
        panel.transform.SetAsLastSibling();
        string sceneName = string.IsNullOrWhiteSpace(e.targetSceneName) ? "Scene" : e.targetSceneName.Trim();
        SetPlainText(titleText, "LOADING");
        SetPlainText(sceneText, sceneName);
        SetProgress(0f);
        SetVisible(true, immediate: false);
    }

    private void OnProgress(LoadingScreenProgressPublish e)
    {
        SetProgress(e.progress);
    }

    private void OnHide(LoadingScreenHidePublish e)
    {
        SetProgress(1f);
        SetVisible(false, immediate: false);
    }

    private void BuildIfNeeded()
    {
        if (panel != null)
            return;

        canvasOverlayRoot = GetOrCreateCanvasOverlayRoot();
        panel = FindExistingPanel(canvasOverlayRoot);
        if (panel == null)
            panel = CreatePanel(canvasOverlayRoot);

        panel.transform.SetAsLastSibling();
        canvasGroup = panel.GetComponent<CanvasGroup>();
        titleText = panel.transform.Find("CenterBlock/TitleText")?.GetComponent<TMP_Text>();
        sceneText = panel.transform.Find("CenterBlock/SceneText")?.GetComponent<TMP_Text>();
        progressText = panel.transform.Find("BottomDock/ProgressHeader/ProgressText")?.GetComponent<TMP_Text>();
        progressFill = panel.transform.Find("BottomDock/ProgressTrack/Fill")?.GetComponent<Image>();
    }

    private void SetVisible(bool visible, bool immediate)
    {
        if (panel == null || canvasGroup == null)
            return;

        panel.SetActive(true);
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        if (immediate)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            panel.SetActive(visible);
            return;
        }

        fadeRoutine = StartCoroutine(FadeTo(visible));
    }

    private IEnumerator FadeTo(bool visible)
    {
        float start = canvasGroup.alpha;
        float end = visible ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < FadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, end, Mathf.Clamp01(elapsed / FadeDuration));
            yield return null;
        }

        canvasGroup.alpha = end;
        panel.SetActive(visible);
        fadeRoutine = null;
    }

    private void SetProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        if (progressFill != null)
            progressFill.fillAmount = progress;
        SetPlainText(progressText, $"{Mathf.RoundToInt(progress * 100f)}%");
    }

    private Transform GetOrCreateCanvasOverlayRoot()
    {
        var rootObject = GameObject.Find("UIRoot");
        if (rootObject == null)
        {
            rootObject = new GameObject("UIRoot");
            uiRoot = rootObject.transform;
        }
        else
        {
            uiRoot = rootObject.transform;
        }

        var overlay = uiRoot.Find("CanvasOverlay");
        if (overlay == null)
        {
            var overlayObject = new GameObject("CanvasOverlay", typeof(RectTransform));
            overlayObject.transform.SetParent(uiRoot, false);
            overlay = overlayObject.transform;
        }

        var overlayRect = overlay as RectTransform;
        if (overlayRect != null)
            Stretch(overlayRect);

        var canvas = overlay.GetComponent<Canvas>();
        if (canvas == null)
            canvas = overlay.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = OverlaySortingOrder - 10;

        var scaler = overlay.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = overlay.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (overlay.GetComponent<GraphicRaycaster>() == null)
            overlay.gameObject.AddComponent<GraphicRaycaster>();

        return overlay;
    }

    private static GameObject FindExistingPanel(Transform parent)
    {
        if (parent == null)
            return null;

        var existing = parent.Find("LoadingScreenUIRoot");
        return existing != null ? existing.gameObject : null;
    }

    private GameObject CreatePanel(Transform parent)
    {
        var root = new GameObject("LoadingScreenUIRoot", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        root.transform.SetAsLastSibling();

        var rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect);

        var rootCanvas = root.AddComponent<Canvas>();
        rootCanvas.overrideSorting = true;
        rootCanvas.sortingOrder = OverlaySortingOrder;
        root.AddComponent<GraphicRaycaster>();

        var rootCanvasGroup = root.AddComponent<CanvasGroup>();
        rootCanvasGroup.blocksRaycasts = true;
        rootCanvasGroup.interactable = true;

        var backgroundObject = new GameObject("Background", typeof(RectTransform));
        backgroundObject.transform.SetParent(root.transform, false);
        Stretch(backgroundObject.GetComponent<RectTransform>());
        var backgroundImage = backgroundObject.AddComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 1f);
        backgroundImage.raycastTarget = true;

        var centerBlock = new GameObject("CenterBlock", typeof(RectTransform));
        centerBlock.transform.SetParent(root.transform, false);
        var centerRect = centerBlock.GetComponent<RectTransform>();
        centerRect.anchorMin = new Vector2(0.5f, 0.5f);
        centerRect.anchorMax = new Vector2(0.5f, 0.5f);
        centerRect.pivot = new Vector2(0.5f, 0.5f);
        centerRect.anchoredPosition = new Vector2(0f, -10f);
        centerRect.sizeDelta = new Vector2(760f, 160f);

        titleText = CreateText("TitleText", centerBlock.transform, "LOADING", 28f, FontStyles.Bold, TextAlignmentOptions.Center);
        var titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 22f);
        titleRect.sizeDelta = new Vector2(500f, 44f);
        titleText.color = new Color(0.92f, 0.92f, 0.92f, 1f);
        titleText.characterSpacing = 14f;

        sceneText = CreateText("SceneText", centerBlock.transform, "Scene", 40f, FontStyles.Normal, TextAlignmentOptions.Center);
        var sceneRect = sceneText.rectTransform;
        sceneRect.anchorMin = new Vector2(0.5f, 0.5f);
        sceneRect.anchorMax = new Vector2(0.5f, 0.5f);
        sceneRect.pivot = new Vector2(0.5f, 0.5f);
        sceneRect.anchoredPosition = new Vector2(0f, -26f);
        sceneRect.sizeDelta = new Vector2(760f, 60f);
        sceneText.color = new Color(1f, 1f, 1f, 1f);

        var bottomDock = new GameObject("BottomDock", typeof(RectTransform));
        bottomDock.transform.SetParent(root.transform, false);
        var dockRect = bottomDock.GetComponent<RectTransform>();
        dockRect.anchorMin = new Vector2(0f, 0f);
        dockRect.anchorMax = new Vector2(1f, 0f);
        dockRect.pivot = new Vector2(0.5f, 0f);
        dockRect.anchoredPosition = new Vector2(0f, 0f);
        dockRect.sizeDelta = new Vector2(0f, 170f);

        var header = new GameObject("ProgressHeader", typeof(RectTransform));
        header.transform.SetParent(bottomDock.transform, false);
        var headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0.08f, 0f);
        headerRect.anchorMax = new Vector2(0.92f, 0f);
        headerRect.pivot = new Vector2(0.5f, 0f);
        headerRect.anchoredPosition = new Vector2(0f, 104f);
        headerRect.sizeDelta = new Vector2(0f, 28f);

        var statusText = CreateText("StatusText", header.transform, "Transitioning world state", 20f, FontStyles.Normal, TextAlignmentOptions.Left);
        var statusRect = statusText.rectTransform;
        statusRect.anchorMin = new Vector2(0f, 0.5f);
        statusRect.anchorMax = new Vector2(0.75f, 0.5f);
        statusRect.pivot = new Vector2(0f, 0.5f);
        statusRect.anchoredPosition = Vector2.zero;
        statusRect.sizeDelta = new Vector2(0f, 28f);
        statusText.color = new Color(0.70f, 0.70f, 0.70f, 1f);

        progressText = CreateText("ProgressText", header.transform, "0%", 20f, FontStyles.Bold, TextAlignmentOptions.Right);
        var progressTextRect = progressText.rectTransform;
        progressTextRect.anchorMin = new Vector2(0.75f, 0.5f);
        progressTextRect.anchorMax = new Vector2(1f, 0.5f);
        progressTextRect.pivot = new Vector2(1f, 0.5f);
        progressTextRect.anchoredPosition = Vector2.zero;
        progressTextRect.sizeDelta = new Vector2(0f, 28f);
        progressText.color = new Color(0.92f, 0.92f, 0.92f, 1f);

        var track = new GameObject("ProgressTrack", typeof(RectTransform));
        track.transform.SetParent(bottomDock.transform, false);
        var trackRect = track.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0.08f, 0f);
        trackRect.anchorMax = new Vector2(0.92f, 0f);
        trackRect.pivot = new Vector2(0.5f, 0f);
        trackRect.anchoredPosition = new Vector2(0f, 62f);
        trackRect.sizeDelta = new Vector2(0f, 8f);
        var trackImage = track.AddComponent<Image>();
        trackImage.color = new Color(1f, 1f, 1f, 0.16f);

        var fillObject = new GameObject("Fill", typeof(RectTransform));
        fillObject.transform.SetParent(track.transform, false);
        Stretch(fillObject.GetComponent<RectTransform>());
        progressFill = fillObject.AddComponent<Image>();
        progressFill.color = new Color(1f, 1f, 1f, 0.96f);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;
        progressFill.fillAmount = 0f;

        return root;
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, float size, FontStyles style, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.color = new Color(0.96f, 0.86f, 0.68f, 1f);
        tmp.enableWordWrapping = false;
        return tmp;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetPlainText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value ?? string.Empty;
    }
}
