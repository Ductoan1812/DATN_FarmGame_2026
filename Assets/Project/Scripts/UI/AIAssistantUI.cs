using UnityEngine;
using TMPro;

/// <summary>
/// Runtime-created UI that displays AI assistant tips.
/// Toggles with H key. Refreshes on game events and periodically.
/// </summary>
public class AIAssistantUI : MonoBehaviour
{
    private const string DefaultFontResourcePath = "Fonts & Materials/Roboto-Bold SDF";

    private Canvas _canvas;
    private TextMeshProUGUI _tipText;
    private GameObject _panel;
    private bool _visible = true;
    private float _refreshTimer;
    private const float RefreshInterval = 5f;

    private void Awake()
    {
        CreateUI();
    }

    private void OnEnable()
    {
        var eventBus = GameManager.Instance?.EventBus;
        if (eventBus != null)
        {
            eventBus.Subscribe<GameReadyPublish>(OnRefreshNeeded);
            eventBus.Subscribe<DayChangedPublish>(OnRefreshNeeded);
            eventBus.Subscribe<WeatherChangedPublish>(OnRefreshNeeded);
        }
    }

    private void OnDisable()
    {
        var eventBus = GameManager.Instance?.EventBus;
        if (eventBus != null)
        {
            eventBus.Unsubscribe<GameReadyPublish>(OnRefreshNeeded);
            eventBus.Unsubscribe<DayChangedPublish>(OnRefreshNeeded);
            eventBus.Unsubscribe<WeatherChangedPublish>(OnRefreshNeeded);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            _visible = !_visible;
            _panel.SetActive(_visible);
        }

        _refreshTimer += Time.deltaTime;
        if (_refreshTimer >= RefreshInterval)
        {
            _refreshTimer = 0f;
            RefreshTip();
        }
    }

    private void CreateUI()
    {
        var root = OverlayUIHelper.GetOrCreateOverlayRoot(gameObject, 100);

        _panel = new GameObject("TipPanel");
        _panel.transform.SetParent(root, false);
        var panelRect = _panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(0, 0);
        panelRect.pivot = new Vector2(0, 0);
        panelRect.anchoredPosition = new Vector2(10, 10);
        panelRect.sizeDelta = new Vector2(300, 60);

        var panelImage = _panel.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f);

        // Text
        var textGO = new GameObject("TipText");
        textGO.transform.SetParent(_panel.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(5, 5);
        textRect.offsetMax = new Vector2(-5, -5);

        _tipText = textGO.AddComponent<TextMeshProUGUI>();
        var fontAsset = Resources.Load<TMP_FontAsset>(DefaultFontResourcePath);
        if (fontAsset != null)
            _tipText.font = fontAsset;
        _tipText.fontSize = 14;
        _tipText.color = Color.white;
        _tipText.alignment = TextAlignmentOptions.TopLeft;
        _tipText.text = "AI Assistant";

        RefreshTip();
    }

    private void OnRefreshNeeded<T>(T _) => RefreshTip();

    private void RefreshTip()
    {
        var service = GameManager.Instance?.AIAssistantService;
        if (service == null)
        {
            _tipText.text = "AI: Waiting...";
            return;
        }

        var tipKey = service.GetPrimaryTipKey();
        var localized = LocalizationManager.Instance?.GetText(tipKey) ?? tipKey;
        _tipText.text = $"AI: {localized}";
    }
}
