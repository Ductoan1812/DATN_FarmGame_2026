using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenUI : MonoBehaviour
{
    private const float FadeDuration = 0.18f;

    private CanvasGroup canvasGroup;
    private GameObject panel;
    private TMP_Text titleText;
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
        string sceneName = string.IsNullOrWhiteSpace(e.targetSceneName) ? "Scene" : e.targetSceneName;
        SetPlainText(titleText, $"Dang toi {sceneName}...");
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

        var parent = OverlayUIHelper.GetOrCreateOverlayRoot(gameObject, 2000);
        var canvas = parent.GetComponent<Canvas>();
        if (canvas != null && canvas.sortingOrder < 2000)
            canvas.sortingOrder = 2000;

        panel = new GameObject("LoadingScreenOverlay", typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        panel.transform.SetAsLastSibling();

        var rect = panel.GetComponent<RectTransform>();
        Stretch(rect);

        var panelCanvas = panel.AddComponent<Canvas>();
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = 5000;
        panel.AddComponent<GraphicRaycaster>();

        canvasGroup = panel.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        var backgroundObject = new GameObject("Background", typeof(RectTransform));
        backgroundObject.transform.SetParent(panel.transform, false);
        Stretch(backgroundObject.GetComponent<RectTransform>());
        var image = backgroundObject.AddComponent<Image>();
        image.color = new Color(0.03f, 0.025f, 0.02f, 0.96f);
        image.raycastTarget = true;

        titleText = CreateText("TitleText", panel.transform, "Dang tai...", 36, FontStyles.Bold, TextAlignmentOptions.Center);
        var titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.25f, 0.50f);
        titleRect.anchorMax = new Vector2(0.75f, 0.50f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 44f);
        titleRect.sizeDelta = new Vector2(0f, 58f);

        var barBack = new GameObject("ProgressBar", typeof(RectTransform));
        barBack.transform.SetParent(panel.transform, false);
        var barRect = barBack.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.35f, 0.50f);
        barRect.anchorMax = new Vector2(0.65f, 0.50f);
        barRect.pivot = new Vector2(0.5f, 0.5f);
        barRect.anchoredPosition = new Vector2(0f, -20f);
        barRect.sizeDelta = new Vector2(0f, 18f);
        var barImage = barBack.AddComponent<Image>();
        barImage.color = new Color(0.18f, 0.10f, 0.04f, 0.95f);

        var fillObject = new GameObject("Fill", typeof(RectTransform));
        fillObject.transform.SetParent(barBack.transform, false);
        var fillRect = fillObject.GetComponent<RectTransform>();
        Stretch(fillRect);
        progressFill = fillObject.AddComponent<Image>();
        progressFill.color = new Color(0.95f, 0.64f, 0.22f, 1f);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;

        progressText = CreateText("ProgressText", panel.transform, "0%", 22, FontStyles.Normal, TextAlignmentOptions.Center);
        var progressRect = progressText.rectTransform;
        progressRect.anchorMin = new Vector2(0.35f, 0.50f);
        progressRect.anchorMax = new Vector2(0.65f, 0.50f);
        progressRect.pivot = new Vector2(0.5f, 0.5f);
        progressRect.anchoredPosition = new Vector2(0f, -58f);
        progressRect.sizeDelta = new Vector2(0f, 36f);
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
