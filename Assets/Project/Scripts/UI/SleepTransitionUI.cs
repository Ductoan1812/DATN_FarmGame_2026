using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fade đen ngắn khi player đi ngủ — không có progress bar/tip text như loading scene thật,
/// chỉ che màn hình trong lúc TimeManager advance qua ngày mới.
/// </summary>
public class SleepTransitionUI : MonoBehaviour
{
    private const int OverlaySortingOrder = 4800;
    private const string IconResourcePath = "UI/EndOfDay/Icon_SleepMoon";

    private const float FadeInSeconds = 0.35f;
    private const float HoldSeconds = 0.9f;
    private const float FadeOutSeconds = 0.45f;
    private const float IconSpinDegreesPerSecond = 40f;

    private CanvasGroup canvasGroup;
    private RectTransform iconRect;
    private EventBus subscribedBus;
    private Coroutine routine;

    private void Awake()
    {
        BuildIfNeeded();
        SetAlphaImmediate(0f);
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Update()
    {
        if (subscribedBus == null)
            TrySubscribe();

        if (iconRect != null && canvasGroup != null && canvasGroup.alpha > 0f)
            iconRect.Rotate(0f, 0f, -IconSpinDegreesPerSecond * Time.unscaledDeltaTime);
    }

    private void OnDisable()
    {
        if (subscribedBus == null)
            return;

        subscribedBus.Unsubscribe<SleepTransitionPublish>(OnSleepTransition);
        subscribedBus = null;
    }

    private void TrySubscribe()
    {
        var bus = GameManager.Instance?.EventBus;
        if (bus == null || bus == subscribedBus)
            return;

        subscribedBus?.Unsubscribe<SleepTransitionPublish>(OnSleepTransition);
        subscribedBus = bus;
        subscribedBus.Subscribe<SleepTransitionPublish>(OnSleepTransition);
    }

    private void OnSleepTransition(SleepTransitionPublish e)
    {
        BuildIfNeeded();

        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        gameObject.SetActive(true);

        yield return Fade(0f, 1f, FadeInSeconds);
        yield return new WaitForSecondsRealtime(HoldSeconds);
        yield return Fade(1f, 0f, FadeOutSeconds);

        routine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlphaImmediate(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetAlphaImmediate(to);
    }

    private void SetAlphaImmediate(float alpha)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = alpha;
        canvasGroup.blocksRaycasts = alpha > 0f;
        canvasGroup.interactable = alpha > 0f;
    }

    private void BuildIfNeeded()
    {
        if (canvasGroup != null)
            return;

        var overlayRoot = GetOrCreateCanvasOverlayRoot();
        var existing = overlayRoot.Find("SleepTransitionUIRoot")?.gameObject;
        var root = existing != null ? existing : CreatePanel(overlayRoot);

        canvasGroup = root.GetComponent<CanvasGroup>();
        iconRect = root.transform.Find("Icon")?.GetComponent<RectTransform>();
    }

    private GameObject CreatePanel(Transform parent)
    {
        var root = new GameObject("SleepTransitionUIRoot", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        root.transform.SetAsLastSibling();

        var rect = root.GetComponent<RectTransform>();
        Stretch(rect);

        var canvas = root.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = OverlaySortingOrder;
        root.AddComponent<GraphicRaycaster>();

        var group = root.AddComponent<CanvasGroup>();

        var background = new GameObject("Background", typeof(RectTransform));
        background.transform.SetParent(root.transform, false);
        Stretch(background.GetComponent<RectTransform>());
        var backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = Color.black;

        var iconGo = new GameObject("Icon", typeof(RectTransform));
        iconGo.transform.SetParent(root.transform, false);
        var iconRectTransform = iconGo.GetComponent<RectTransform>();
        iconRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        iconRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        iconRectTransform.pivot = new Vector2(0.5f, 0.5f);
        iconRectTransform.sizeDelta = new Vector2(96f, 96f);

        var iconImage = iconGo.AddComponent<Image>();
        var sprite = LoadIconSprite();
        if (sprite != null)
        {
            iconImage.sprite = sprite;
            iconImage.color = Color.white;
        }
        else
        {
            iconImage.color = new Color(0.95f, 0.85f, 0.55f, 0.4f);
        }
        iconImage.preserveAspect = true;

        var textGo = new GameObject("LoadingText", typeof(RectTransform));
        textGo.transform.SetParent(root.transform, false);
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = new Vector2(0f, -58f);
        textRect.sizeDelta = new Vector2(300f, 40f);

        var label = textGo.AddComponent<TextMeshProUGUI>();
        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Roboto-Bold SDF");
        if (font != null) label.font = font;
        label.text = "Đang tải...";
        label.fontSize = 22f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.85f, 0.80f, 0.65f, 0.8f);

        return root;
    }

    private static Sprite LoadIconSprite()
    {
        return Resources.Load<Sprite>(IconResourcePath);
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

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
