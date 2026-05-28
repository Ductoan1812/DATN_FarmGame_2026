using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class WorldInteractionHintUI : MonoBehaviour
{
    private const string DefaultFontResourcePath = "Fonts & Materials/Roboto-Bold SDF";

    [SerializeField] private Vector3 worldOffset = new(0f, 1.9f, 0f);
    [SerializeField] private float textScale = 0.13f;
    [SerializeField] private float fontSize = 36f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color blockedColor = new(1f, 0.72f, 0.72f, 1f);
    [SerializeField] private string sortingLayerName = "UI";
    [SerializeField] private int sortingOrder = 200;
    [SerializeField] private TMP_FontAsset fontAsset;

    private EventBus eventBus;
    private TextMeshPro hintText;
    private InteractionPreviewData currentPreview;
    private bool subscribed;

    private void Awake()
    {
        EnsureText();
        Hide();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Update()
    {
        if (!subscribed) TrySubscribe();
        UpdateFollow();
    }

    private void OnDisable()
    {
        if (!subscribed || eventBus == null) return;
        eventBus.Unsubscribe<InteractionPreviewChangedPublish>(OnPreviewChanged);
        subscribed = false;
    }

    private void TrySubscribe()
    {
        if (subscribed) return;
        eventBus = GameManager.Instance?.EventBus;
        if (eventBus == null) return;

        eventBus.Subscribe<InteractionPreviewChangedPublish>(OnPreviewChanged);
        subscribed = true;
    }

    private void OnPreviewChanged(InteractionPreviewChangedPublish e)
    {
        currentPreview = e.preview;
        RenderPreview();
    }

    private void UpdateFollow()
    {
        if (hintText == null) return;
        if (!TryGetTargetTransform(out var targetTransform))
        {
            Hide();
            currentPreview = default;
            return;
        }

        hintText.transform.position = targetTransform.position + worldOffset;
    }

    private void RenderPreview()
    {
        EnsureText();
        if (hintText == null) return;

        if (!TryGetTargetTransform(out _))
        {
            Hide();
            return;
        }

        string targetName = ResolveText(currentPreview.targetNameKey, currentPreview.targetNameFallback);
        string actionText = ResolveText(currentPreview.actionTextKey, "Tương tác");

        string line = ResolveText("ui.interaction.prompt_format", "E - {0}: {1}");
        line = string.Format(line, actionText, targetName);

        if (currentPreview.isBlocked && !string.IsNullOrWhiteSpace(currentPreview.blockedReasonKey))
            line = ResolveBlockedLine(currentPreview.blockedReasonKey);
        else if (!string.IsNullOrWhiteSpace(currentPreview.statusTextKey))
            line = ResolveText(currentPreview.statusTextKey, currentPreview.statusTextKey);

        hintText.color = currentPreview.isBlocked ? blockedColor : normalColor;
        hintText.text = line;
        hintText.gameObject.SetActive(true);
    }

    private bool TryGetTargetTransform(out Transform targetTransform)
    {
        targetTransform = null;
        if (!currentPreview.HasTarget || currentPreview.target == null)
            return false;

        var owner = currentPreview.target.Owner;
        if (owner is UnityEngine.Object ownerObject && ownerObject == null)
            return false;

        var go = owner?.GameObject;
        if (go == null)
            return false;

        targetTransform = go.transform;
        return targetTransform != null;
    }

    private string ResolveBlockedLine(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;

        int separator = key.IndexOf('|');
        if (separator < 0)
            return ResolveText(key, key);

        string formatKey = key.Substring(0, separator);
        string argKey = key.Substring(separator + 1);
        string toolName = ResolveText(argKey, argKey);
        return ResolveText(formatKey, "Cần {0}", toolName);
    }

    private string ResolveText(string key, string fallback, params object[] args)
    {
        if (string.IsNullOrWhiteSpace(key))
            return fallback ?? string.Empty;

        var lm = LocalizationManager.Instance;
        if (lm == null)
            return args == null || args.Length == 0 ? key : string.Format(key, args);

        string value = args == null || args.Length == 0
            ? lm.GetText(key)
            : lm.GetText(key, args);

        if (string.IsNullOrWhiteSpace(value) || value == key)
            return args == null || args.Length == 0 ? fallback : string.Format(fallback, args);

        return value;
    }

    private void Hide()
    {
        if (hintText == null) return;
        hintText.gameObject.SetActive(false);
    }

    private void EnsureText()
    {
        if (hintText != null) return;

        var child = transform.Find("WorldHintText");
        if (child != null)
            hintText = child.GetComponent<TextMeshPro>();

        if (hintText == null)
        {
            var go = new GameObject("WorldHintText");
            go.transform.SetParent(transform, false);
            hintText = go.AddComponent<TextMeshPro>();
        }

        hintText.alignment = TextAlignmentOptions.Center;
        if (fontAsset == null)
            fontAsset = Resources.Load<TMP_FontAsset>(DefaultFontResourcePath);
        if (fontAsset != null)
            hintText.font = fontAsset;
        hintText.enableWordWrapping = false;
        hintText.fontSize = fontSize;
        hintText.color = normalColor;
        hintText.outlineWidth = 0.2f;
        hintText.outlineColor = new Color(0f, 0f, 0f, 0.95f);
        hintText.raycastTarget = false;

        hintText.transform.localScale = Vector3.one * Mathf.Max(0.01f, textScale);

        var renderer = hintText.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder;
        }
    }
}
