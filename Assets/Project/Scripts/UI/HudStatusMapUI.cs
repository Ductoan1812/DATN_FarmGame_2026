using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HudStatusMapUI : MonoBehaviour
{
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text moneyText;

    private EventBus subscribedBus;
    private TimeManager timeManager;
    private EntityRuntime playerEntity;
    private float nextResolveTime;

    private void OnEnable()
    {
        LocalizationManager.LocalizationReady += OnLocalizationChanged;
        LocalizationManager.LanguageChanged += OnLocalizationChanged;
        EnsureBasicLayout();
        TrySubscribe();
        ResolveRefs();
        RefreshAll();
    }

    private void Start()
    {
        EnsureBasicLayout();
        TrySubscribe();
        ResolveRefs();
        RefreshAll();
    }

    private void Update()
    {
        if (subscribedBus == null)
            TrySubscribe();

        if (Time.unscaledTime >= nextResolveTime && (timeManager == null || playerEntity == null))
        {
            nextResolveTime = Time.unscaledTime + 0.5f;
            ResolveRefs();
            RefreshAll();
        }
    }

    private void OnDisable()
    {
        LocalizationManager.LocalizationReady -= OnLocalizationChanged;
        LocalizationManager.LanguageChanged -= OnLocalizationChanged;

        if (subscribedBus == null) return;

        subscribedBus.Unsubscribe<GameTimeChangedPublish>(OnGameTimeChanged);
        subscribedBus.Unsubscribe<DayChangedPublish>(OnDayChanged);
        subscribedBus.Unsubscribe<SeasonChangedPublish>(OnSeasonChanged);
        subscribedBus.Unsubscribe<YearChangedPublish>(OnYearChanged);
        subscribedBus.Unsubscribe<PlayerReadyPublish>(OnPlayerReady);
        subscribedBus.Unsubscribe<GameReadyPublish>(OnGameReady);
        subscribedBus.Unsubscribe<StatsChangedPublish>(OnStatsChanged);
        subscribedBus = null;
    }

    private void OnLocalizationChanged()
    {
        RefreshDate();
    }

    private void OnGameTimeChanged(GameTimeChangedPublish _)
    {
        RefreshTime();
        RefreshDate();
    }

    private void OnDayChanged(DayChangedPublish _)
    {
        RefreshDate();
    }

    private void OnSeasonChanged(SeasonChangedPublish _)
    {
        RefreshDate();
    }

    private void OnYearChanged(YearChangedPublish _)
    {
        RefreshDate();
    }

    private void OnPlayerReady(PlayerReadyPublish _)
    {
        playerEntity = null;
        ResolveRefs();
        RefreshMoney();
    }

    private void OnGameReady(GameReadyPublish _)
    {
        ResolveRefs();
        RefreshAll();
    }

    private void OnStatsChanged(StatsChangedPublish e)
    {
        if (playerEntity == null || e.entityId != playerEntity.id) return;
        if (e.statType == StatType.Money)
            RefreshMoney();
    }

    private void TrySubscribe()
    {
        if (subscribedBus != null) return;

        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;

        bus.Subscribe<GameTimeChangedPublish>(OnGameTimeChanged);
        bus.Subscribe<DayChangedPublish>(OnDayChanged);
        bus.Subscribe<SeasonChangedPublish>(OnSeasonChanged);
        bus.Subscribe<YearChangedPublish>(OnYearChanged);
        bus.Subscribe<PlayerReadyPublish>(OnPlayerReady);
        bus.Subscribe<GameReadyPublish>(OnGameReady);
        bus.Subscribe<StatsChangedPublish>(OnStatsChanged);
        subscribedBus = bus;
    }

    private void ResolveRefs()
    {
        AutoFindRefs();

        if (timeManager == null)
            timeManager = GameManager.Instance != null
                ? GameManager.Instance.TimeManager
                : FindAnyObjectByType<TimeManager>();

        if (playerEntity != null)
            return;

        var bridge = FindAnyObjectByType<PlayerBridge>();
        var root = bridge != null
            ? bridge.GetComponent<EntityRoot>()
            : FindAnyObjectByType<PlayerInventory>()?.GetComponent<EntityRoot>();

        playerEntity = root != null ? root.GetEntity() : null;
    }

    private void AutoFindRefs()
    {
        dateText ??= FindText(transform, "DateText")
                  ?? FindText(transform, "DayText");
        timeText ??= FindText(transform, "TimeText");
        moneyText ??= FindText(transform, "MoneyText")
                   ?? FindText(transform, "CoinText");
    }

    private void EnsureBasicLayout()
    {
        AutoFindRefs();
        if (dateText != null && timeText != null && moneyText != null)
            return;

        var existing = FindDeepChild(transform, "HudStatusFallback");
        if (existing != null)
        {
            AutoFindRefs();
            return;
        }

        var root = CreateUiObject("HudStatusFallback", transform);
        Stretch(root, Vector2.zero, Vector2.zero);

        var statusPanel = CreatePanel("StatusPanel", root, new Vector2(0f, 0f), new Vector2(0.62f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(-8f, 0f), new Color(0.86f, 0.56f, 0.22f, 0.90f));
        ConfigureVertical(statusPanel, 3f, new RectOffset(12, 12, 6, 6));
        dateText = CreateStatusRow(statusPanel, "DateText", "D", FormatDate(1, Season.Spring));
        timeText = CreateStatusRow(statusPanel, "TimeText", "T", "06:00 AM");
        moneyText = CreateStatusRow(statusPanel, "MoneyText", "$", "0");

        var mapPanel = CreatePanel("MiniMapPanel", root, new Vector2(0.64f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.18f, 0.38f, 0.20f, 0.92f));
        var mapTitle = CreateText("MapTitle", mapPanel, "Bản đồ", 14f, TextAlignmentOptions.Center, new Color(0.95f, 0.90f, 0.58f));
        SetRect(mapTitle.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-12f, 22f));

        CreateMapBlock("Water", mapPanel, new Color(0.12f, 0.45f, 0.62f, 1f), new Vector2(0.08f, 0.12f), new Vector2(0.34f, 0.66f));
        CreateMapBlock("Farm", mapPanel, new Color(0.26f, 0.55f, 0.20f, 1f), new Vector2(0.34f, 0.12f), new Vector2(0.92f, 0.88f));
        CreateMapBlock("Path", mapPanel, new Color(0.72f, 0.55f, 0.26f, 1f), new Vector2(0.35f, 0.46f), new Vector2(0.92f, 0.66f));
        CreateMapBlock("House", mapPanel, new Color(0.38f, 0.18f, 0.08f, 1f), new Vector2(0.58f, 0.20f), new Vector2(0.84f, 0.42f));
        CreateMapBlock("PlayerMarker", mapPanel, new Color(0.78f, 0.92f, 1f, 1f), new Vector2(0.42f, 0.54f), new Vector2(0.50f, 0.64f));
    }

    private void RefreshAll()
    {
        RefreshDate();
        RefreshTime();
        RefreshMoney();
    }

    private void RefreshDate()
    {
        if (dateText == null) return;

        int day = timeManager != null ? timeManager.Day : 1;
        Season season = timeManager != null ? timeManager.Season : Season.Spring;
        dateText.text = FormatDate(day, season);
    }

    private void RefreshTime()
    {
        if (timeText == null) return;

        if (timeManager == null)
        {
            timeText.text = "06:00 AM";
            return;
        }

        int hour24 = Mathf.Clamp(timeManager.Hour, 0, 23);
        int minute = Mathf.Clamp(timeManager.Minute, 0, 59);
        string suffix = hour24 >= 12 ? "PM" : "AM";
        int hour12 = hour24 % 12;
        if (hour12 == 0) hour12 = 12;
        timeText.text = $"{hour12:00}:{minute:00} {suffix}";
    }

    private void RefreshMoney()
    {
        if (moneyText == null) return;

        int money = 0;
        if (playerEntity?.stats != null && playerEntity.stats.Has(StatType.Money))
            money = Mathf.FloorToInt(playerEntity.stats.Get(StatType.Money));

        moneyText.text = money.ToString("N0");
    }

    private static string FormatDate(int day, Season season)
    {
        string dayText = LocalizeFormat(LocalizationKeys.UiTimeDay, $"Ngày {day}", day);
        string seasonText = Localize(SeasonKey(season), SeasonNameFallback(season));
        return $"{dayText} - {seasonText}";
    }

    private static string SeasonKey(Season season)
    {
        return season switch
        {
            Season.Spring => LocalizationKeys.UiTimeSpring,
            Season.Summer => LocalizationKeys.UiTimeSummer,
            Season.Fall => LocalizationKeys.UiTimeFall,
            Season.Winter => LocalizationKeys.UiTimeWinter,
            _ => string.Empty
        };
    }

    private static string SeasonNameFallback(Season season)
    {
        return season switch
        {
            Season.Spring => "Xuân",
            Season.Summer => "Hạ",
            Season.Fall => "Thu",
            Season.Winter => "Đông",
            _ => season.ToString()
        };
    }

    private static string Localize(string key, string fallback)
    {
        var localization = LocalizationManager.Instance;
        if (localization == null || !localization.IsReady || string.IsNullOrWhiteSpace(key))
            return fallback;

        var value = localization.GetText(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static string LocalizeFormat(string key, string fallback, params object[] args)
    {
        var localization = LocalizationManager.Instance;
        if (localization == null || !localization.IsReady || string.IsNullOrWhiteSpace(key))
            return fallback;

        var value = localization.GetText(key, args);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static TMP_Text CreateStatusRow(Transform parent, string valueName, string prefix, string value)
    {
        var row = CreateUiObject(valueName + "Row", parent);
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        SetLayoutSize(row.gameObject, 0f, 30f);

        var label = CreateText("Label", row, prefix, 18f, TextAlignmentOptions.Center, new Color(0.18f, 0.11f, 0.06f));
        SetLayoutSize(label.gameObject, 30f, 28f);

        var text = CreateText(valueName, row, value, 18f, TextAlignmentOptions.MidlineLeft, new Color(0.18f, 0.11f, 0.06f));
        text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        return text;
    }

    private static RectTransform CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        var rect = CreateUiObject(name, parent);
        SetRect(rect, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.30f, 0.17f, 0.06f, 0.95f);
        outline.effectDistance = new Vector2(2f, -2f);
        return rect;
    }

    private static void CreateMapBlock(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        var rect = CreateUiObject(name, parent);
        SetRect(rect, anchorMin, anchorMax, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
    }

    private static void ConfigureVertical(Transform target, float spacing, RectOffset padding)
    {
        var layout = target.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = padding;
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static RectTransform CreateUiObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
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
        text.enableWordWrapping = false;
        return text;
    }

    private static TMP_Text FindText(Transform root, string name)
    {
        var child = FindDeepChild(root, name);
        return child != null ? child.GetComponent<TMP_Text>() : null;
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

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void SetLayoutSize(GameObject go, float width, float height)
    {
        var layout = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.minHeight = height;
        layout.preferredHeight = height;
    }
}
