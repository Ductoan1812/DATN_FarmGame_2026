using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndOfDaySummaryUI : MonoBehaviour
{
    private const float FadeDuration = 0.24f;
    private const float MinimumVisibleSeconds = 1.2f;
    private const float AutoCloseSeconds = 5.5f;
    private const int OverlaySortingOrder = 4600;

    private GameObject panel;
    private CanvasGroup canvasGroup;
    private TMP_Text seasonLineText;
    private TMP_Text dayNumberText;
    private TMP_Text statsText;
    private TMP_Text weatherText;
    private TMP_Text continueText;
    private EventBus subscribedBus;
    private Coroutine fadeRoutine;
    private bool visible;
    private float visibleTime;

    private void Awake()
    {
        BuildIfNeeded();
        SetVisible(false, true);
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Update()
    {
        if (subscribedBus == null)
            TrySubscribe();

        if (!visible)
            return;

        visibleTime += Time.unscaledDeltaTime;
        bool canDismiss = visibleTime >= MinimumVisibleSeconds;
        bool shouldDismiss = visibleTime >= AutoCloseSeconds
                             || (canDismiss && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)));

        if (shouldDismiss)
            SetVisible(false, false);
    }

    private void OnDisable()
    {
        if (subscribedBus == null)
            return;

        subscribedBus.Unsubscribe<DayChangedPublish>(OnDayChanged);
        subscribedBus = null;
    }

    private void TrySubscribe()
    {
        var bus = GameManager.Instance?.EventBus;
        if (bus == null || bus == subscribedBus)
            return;

        subscribedBus?.Unsubscribe<DayChangedPublish>(OnDayChanged);
        subscribedBus = bus;
        subscribedBus.Subscribe<DayChangedPublish>(OnDayChanged);
    }

    private void OnDayChanged(DayChangedPublish e)
    {
        BuildIfNeeded();

        var gm = GameManager.Instance;
        if (gm?.DailyTracker == null)
            return;

        var summary = gm.DailyTracker.GetLastSummary();
        string seasonLabel = summary.season.ToString().ToUpperInvariant();

        SetPlainText(seasonLineText, seasonLabel);
        SetPlainText(dayNumberText, $"DAY {summary.day}");
        SetPlainText(weatherText, $"Tomorrow Forecast: {FormatWeather(gm.WeatherSystem?.CurrentWeather)}");
        SetPlainText(statsText,
            $"Income Today    {summary.income}g\n" +
            $"Exp Gained      {summary.expGained}\n" +
            $"Level Ups       {summary.levelUps}\n" +
            $"Year            {summary.year}");
        SetPlainText(continueText, "Press Space to continue");

        visibleTime = 0f;
        panel.transform.SetAsLastSibling();
        SetVisible(true, false);
    }

    private void BuildIfNeeded()
    {
        if (panel != null)
            return;

        var overlayRoot = GetOrCreateCanvasOverlayRoot();
        panel = overlayRoot.Find("EndOfDaySummaryUIRoot")?.gameObject;
        if (panel == null)
            panel = CreatePanel(overlayRoot);

        canvasGroup = panel.GetComponent<CanvasGroup>();
        seasonLineText = panel.transform.Find("CenterBlock/SeasonLineText")?.GetComponent<TMP_Text>();
        dayNumberText = panel.transform.Find("CenterBlock/DayNumberText")?.GetComponent<TMP_Text>();
        statsText = panel.transform.Find("BottomBlock/StatsText")?.GetComponent<TMP_Text>();
        weatherText = panel.transform.Find("BottomBlock/WeatherText")?.GetComponent<TMP_Text>();
        continueText = panel.transform.Find("BottomBlock/ContinueText")?.GetComponent<TMP_Text>();
    }

    private Transform GetOrCreateCanvasOverlayRoot()
    {
        var rootObject = GameObject.Find("UIRoot");
        if (rootObject == null)
            rootObject = new GameObject("UIRoot");

        var overlay = rootObject.transform.Find("CanvasOverlay");
        if (overlay == null)
        {
            var overlayObject = new GameObject("CanvasOverlay", typeof(RectTransform));
            overlayObject.transform.SetParent(rootObject.transform, false);
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

    private GameObject CreatePanel(Transform parent)
    {
        var root = new GameObject("EndOfDaySummaryUIRoot", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        root.transform.SetAsLastSibling();

        var rect = root.GetComponent<RectTransform>();
        Stretch(rect);

        var canvas = root.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = OverlaySortingOrder;
        root.AddComponent<GraphicRaycaster>();

        var group = root.AddComponent<CanvasGroup>();
        group.blocksRaycasts = true;
        group.interactable = true;

        var background = new GameObject("Background", typeof(RectTransform));
        background.transform.SetParent(root.transform, false);
        Stretch(background.GetComponent<RectTransform>());
        var backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.02f, 0.02f, 0.03f, 0.98f);

        var vignette = new GameObject("Vignette", typeof(RectTransform));
        vignette.transform.SetParent(root.transform, false);
        Stretch(vignette.GetComponent<RectTransform>());
        var vignetteImage = vignette.AddComponent<Image>();
        vignetteImage.color = new Color(0.18f, 0.12f, 0.05f, 0.12f);

        var centerBlock = new GameObject("CenterBlock", typeof(RectTransform));
        centerBlock.transform.SetParent(root.transform, false);
        var centerRect = centerBlock.GetComponent<RectTransform>();
        centerRect.anchorMin = new Vector2(0.5f, 0.58f);
        centerRect.anchorMax = new Vector2(0.5f, 0.58f);
        centerRect.pivot = new Vector2(0.5f, 0.5f);
        centerRect.sizeDelta = new Vector2(1100f, 300f);

        seasonLineText = CreateText("SeasonLineText", centerBlock.transform, "SPRING", 30f, FontStyles.Bold, TextAlignmentOptions.Center);
        var seasonRect = seasonLineText.rectTransform;
        seasonRect.anchorMin = new Vector2(0.5f, 0.5f);
        seasonRect.anchorMax = new Vector2(0.5f, 0.5f);
        seasonRect.pivot = new Vector2(0.5f, 0.5f);
        seasonRect.anchoredPosition = new Vector2(0f, 80f);
        seasonRect.sizeDelta = new Vector2(700f, 42f);
        seasonLineText.characterSpacing = 18f;
        seasonLineText.color = new Color(0.90f, 0.86f, 0.76f, 1f);

        dayNumberText = CreateText("DayNumberText", centerBlock.transform, "DAY 1", 86f, FontStyles.Bold, TextAlignmentOptions.Center);
        var dayRect = dayNumberText.rectTransform;
        dayRect.anchorMin = new Vector2(0.5f, 0.5f);
        dayRect.anchorMax = new Vector2(0.5f, 0.5f);
        dayRect.pivot = new Vector2(0.5f, 0.5f);
        dayRect.anchoredPosition = new Vector2(0f, -6f);
        dayRect.sizeDelta = new Vector2(1000f, 110f);
        dayNumberText.color = Color.white;

        var divider = new GameObject("Divider", typeof(RectTransform));
        divider.transform.SetParent(centerBlock.transform, false);
        var dividerRect = divider.GetComponent<RectTransform>();
        dividerRect.anchorMin = new Vector2(0.5f, 0.5f);
        dividerRect.anchorMax = new Vector2(0.5f, 0.5f);
        dividerRect.pivot = new Vector2(0.5f, 0.5f);
        dividerRect.anchoredPosition = new Vector2(0f, -88f);
        dividerRect.sizeDelta = new Vector2(340f, 2f);
        var dividerImage = divider.AddComponent<Image>();
        dividerImage.color = new Color(1f, 1f, 1f, 0.16f);

        var bottomBlock = new GameObject("BottomBlock", typeof(RectTransform));
        bottomBlock.transform.SetParent(root.transform, false);
        var bottomRect = bottomBlock.GetComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0.5f, 0f);
        bottomRect.anchorMax = new Vector2(0.5f, 0f);
        bottomRect.pivot = new Vector2(0.5f, 0f);
        bottomRect.anchoredPosition = new Vector2(0f, 110f);
        bottomRect.sizeDelta = new Vector2(760f, 220f);

        weatherText = CreateText("WeatherText", bottomBlock.transform, "Tomorrow Forecast: Sunny", 24f, FontStyles.Normal, TextAlignmentOptions.Center);
        var weatherRect = weatherText.rectTransform;
        weatherRect.anchorMin = new Vector2(0.5f, 1f);
        weatherRect.anchorMax = new Vector2(0.5f, 1f);
        weatherRect.pivot = new Vector2(0.5f, 1f);
        weatherRect.anchoredPosition = new Vector2(0f, 0f);
        weatherRect.sizeDelta = new Vector2(760f, 34f);
        weatherText.color = new Color(0.82f, 0.82f, 0.82f, 1f);

        statsText = CreateText("StatsText", bottomBlock.transform, "Income Today 0g", 30f, FontStyles.Normal, TextAlignmentOptions.Center);
        var statsRect = statsText.rectTransform;
        statsRect.anchorMin = new Vector2(0.5f, 1f);
        statsRect.anchorMax = new Vector2(0.5f, 1f);
        statsRect.pivot = new Vector2(0.5f, 1f);
        statsRect.anchoredPosition = new Vector2(0f, -56f);
        statsRect.sizeDelta = new Vector2(760f, 118f);
        statsText.color = Color.white;
        statsText.lineSpacing = 8f;

        continueText = CreateText("ContinueText", bottomBlock.transform, "Press Space to continue", 22f, FontStyles.Italic, TextAlignmentOptions.Center);
        var continueRect = continueText.rectTransform;
        continueRect.anchorMin = new Vector2(0.5f, 0f);
        continueRect.anchorMax = new Vector2(0.5f, 0f);
        continueRect.pivot = new Vector2(0.5f, 0f);
        continueRect.anchoredPosition = new Vector2(0f, 0f);
        continueRect.sizeDelta = new Vector2(760f, 30f);
        continueText.color = new Color(0.72f, 0.72f, 0.72f, 1f);

        return root;
    }

    private void SetVisible(bool shouldShow, bool immediate)
    {
        if (panel == null || canvasGroup == null)
            return;

        panel.SetActive(true);
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        if (immediate)
        {
            canvasGroup.alpha = shouldShow ? 1f : 0f;
            panel.SetActive(shouldShow);
            visible = shouldShow;
            return;
        }

        fadeRoutine = StartCoroutine(FadeRoutine(shouldShow));
    }

    private IEnumerator FadeRoutine(bool shouldShow)
    {
        float start = canvasGroup.alpha;
        float end = shouldShow ? 1f : 0f;
        float elapsed = 0f;

        visible = shouldShow;

        while (elapsed < FadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, end, Mathf.Clamp01(elapsed / FadeDuration));
            yield return null;
        }

        canvasGroup.alpha = end;
        panel.SetActive(shouldShow);
        if (!shouldShow)
            visible = false;
        fadeRoutine = null;
    }

    private static string FormatWeather(WeatherType? weather)
    {
        if (!weather.HasValue)
            return "Unknown";

        return weather.Value.ToString();
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, float size, FontStyles style, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var textComponent = go.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = size;
        textComponent.fontStyle = style;
        textComponent.alignment = alignment;
        textComponent.color = Color.white;
        textComponent.enableWordWrapping = false;
        return textComponent;
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
