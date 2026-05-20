using TMPro;
using UnityEngine;

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
        TrySubscribe();
        ResolveRefs();
        RefreshAll();
    }

    private void Start()
    {
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

    private void RefreshAll()
    {
        RefreshDate();
        RefreshTime();
        RefreshMoney();
    }

    private void RefreshDate()
    {
        if (dateText == null) return;

        if (timeManager == null)
        {
            dateText.text = "Ngày 1 - Xuân";
            return;
        }

        dateText.text = $"Ngày {timeManager.Day} - {SeasonName(timeManager.Season)}";
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

    private static string SeasonName(Season season)
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
}
