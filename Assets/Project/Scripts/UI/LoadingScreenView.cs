using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenView : MonoBehaviour
{
    public const string ConfigResourcePath = "UI/LoadingScreenPrefabConfig";

    [Header("Localization")]
    [SerializeField] private string titleKey = "ui.loading.title";
    [SerializeField] private string sceneFormatKey = "ui.loading.scene";
    [SerializeField] private string percentFormatKey = "ui.loading.percent";
    [SerializeField] private string[] tipKeys =
    {
        "ui.loading.tip.1",
        "ui.loading.tip.2",
        "ui.loading.tip.3",
        "ui.loading.tip.4",
        "ui.loading.tip.5"
    };

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text sceneText;
    [SerializeField] private TMP_Text percentText;
    [SerializeField] private TMP_Text tipText;

    [Header("Progress")]
    [SerializeField] private Image progressFill;
    [SerializeField] private RectTransform runnerRoot;
    [SerializeField] private Image runnerImage;
    [SerializeField] private Sprite[] runnerFrames;
    [SerializeField] private float runnerFrameRate = 12f;
    [SerializeField] private float runnerStartX = -420f;
    [SerializeField] private float runnerEndX = 420f;

    [Header("Visibility")]
    [SerializeField] private CanvasGroup canvasGroup;

    private string currentSceneName = string.Empty;
    private string currentTipKey;
    private float currentProgress;
    private float frameTimer;
    private int frameIndex;
    private bool isShowing;

    private void Awake()
    {
        AutoBind();
        if (Application.isPlaying)
            HideImmediate();
    }

    private void OnEnable()
    {
        LocalizationManager.LocalizationReady += RefreshLocalizedText;
        LocalizationManager.LanguageChanged += RefreshLocalizedText;
        RefreshLocalizedText();
    }

    private void OnDisable()
    {
        LocalizationManager.LocalizationReady -= RefreshLocalizedText;
        LocalizationManager.LanguageChanged -= RefreshLocalizedText;
    }

    private void Update()
    {
        if (!isShowing)
            return;

        AnimateRunner();
    }

    public static LoadingScreenView InstantiateForParent(Transform parent, LoadingScreenView explicitPrefab = null)
    {
        LoadingScreenView prefab = explicitPrefab != null
            ? explicitPrefab
            : LoadConfiguredPrefab();

        if (prefab == null)
        {
            Debug.LogWarning($"[LoadingScreenView] Missing prefab config at Resources/{ConfigResourcePath}.asset or missing prefab reference.");
            return null;
        }

        var instance = Instantiate(prefab, parent, false);
        instance.name = "LoadingScreenView";
        var rect = instance.transform as RectTransform;
        if (rect != null)
            Stretch(rect);

        return instance;
    }

    private static LoadingScreenView LoadConfiguredPrefab()
    {
        var config = Resources.Load<LoadingScreenPrefabConfig>(ConfigResourcePath);
        return config != null ? config.Prefab : null;
    }

    public void Show(string sceneName)
    {
        bool wasShowing = isShowing;
        isShowing = true;
        gameObject.SetActive(true);
        currentSceneName = string.IsNullOrWhiteSpace(sceneName) ? "Scene" : sceneName.Trim();

        if (!wasShowing)
        {
            PickRandomTip();
            frameTimer = 0f;
            frameIndex = 0;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        transform.SetAsLastSibling();
        RefreshLocalizedText();
        SetProgress(currentProgress);
    }

    public void Hide()
    {
        SetProgress(1f);
        HideImmediate();
    }

    public void HideImmediate()
    {
        isShowing = false;
        currentProgress = 0f;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        gameObject.SetActive(false);
    }

    public void SetProgress(float progress)
    {
        currentProgress = Mathf.Clamp01(progress);

        if (progressFill != null)
            progressFill.fillAmount = currentProgress;

        if (runnerRoot != null)
        {
            var position = runnerRoot.anchoredPosition;
            position.x = Mathf.Lerp(runnerStartX, runnerEndX, currentProgress);
            runnerRoot.anchoredPosition = position;
        }

        RefreshPercentText();
    }

    public void PickRandomTip()
    {
        if (tipKeys == null || tipKeys.Length == 0)
        {
            currentTipKey = string.Empty;
            return;
        }

        currentTipKey = tipKeys[UnityEngine.Random.Range(0, tipKeys.Length)];
    }

    private void AutoBind()
    {
        canvasGroup ??= GetComponent<CanvasGroup>();
        titleText ??= FindChildComponent<TMP_Text>("TitleText");
        sceneText ??= FindChildComponent<TMP_Text>("SceneText");
        percentText ??= FindChildComponent<TMP_Text>("PercentText");
        tipText ??= FindChildComponent<TMP_Text>("TipText");
        progressFill ??= FindChildComponent<Image>("ProgressFill");
        runnerRoot ??= FindChildComponent<RectTransform>("RunnerRoot");
        runnerImage ??= FindChildComponent<Image>("RunnerImage");
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        var child = FindDeepChild(transform, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private void AnimateRunner()
    {
        if (runnerImage == null || runnerFrames == null || runnerFrames.Length == 0)
            return;

        frameTimer += Time.unscaledDeltaTime;
        float frameDuration = 1f / Mathf.Max(1f, runnerFrameRate);
        if (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex = (frameIndex + 1) % runnerFrames.Length;
        }

        runnerImage.sprite = runnerFrames[Mathf.Clamp(frameIndex, 0, runnerFrames.Length - 1)];
        runnerImage.enabled = runnerImage.sprite != null;
    }

    private void RefreshLocalizedText()
    {
        SetText(titleText, Localize(titleKey, "LOADING..."));

        string sceneDisplayName = LocalizeSceneName(currentSceneName);
        SetText(sceneText, FormatLocalized(sceneFormatKey, "Loading scene {0}", sceneDisplayName));
        RefreshPercentText();

        string tip = string.IsNullOrWhiteSpace(currentTipKey)
            ? string.Empty
            : Localize(currentTipKey, currentTipKey);
        SetText(tipText, tip);
    }

    private void RefreshPercentText()
    {
        SetText(percentText, FormatLocalized(percentFormatKey, "{0}%", Mathf.RoundToInt(currentProgress * 100f)));
    }

    private string LocalizeSceneName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return Localize("ui.loading.scene_name.scene", "Scene");

        string sceneKey = $"ui.loading.scene_name.{sceneName}";
        string localized = Localize(sceneKey, sceneName);
        return string.Equals(localized, sceneKey, StringComparison.Ordinal) ? sceneName : localized;
    }

    private static string Localize(string key, string fallback)
    {
        if (string.IsNullOrWhiteSpace(key))
            return fallback ?? string.Empty;

        if (LocalizationManager.Instance == null)
            return fallback ?? key;

        string value = LocalizationManager.Instance.GetText(key);
        return string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.Ordinal)
            ? fallback ?? key
            : value;
    }

    private static string FormatLocalized(string key, string fallback, params object[] args)
    {
        string template = Localize(key, fallback);
        try
        {
            return args == null || args.Length == 0 ? template : string.Format(template, args);
        }
        catch (FormatException)
        {
            return fallback ?? template;
        }
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value ?? string.Empty;
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeepChild(root.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
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
