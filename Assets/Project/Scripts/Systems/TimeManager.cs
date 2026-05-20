using UnityEngine;

/// <summary>
/// Quản lý toàn bộ thời gian game: giờ, ngày, mùa, năm.
/// 
/// Publish events qua EventBus:
///   - GameTimeChangedPublish  — mỗi phút game thay đổi
///   - GameHourChangedPublish  — mỗi giờ game thay đổi
///   - DayChangedPublish       — sang ngày mới (thay thế NextDayEventPublish cũ)
///   - SeasonChangedPublish    — sang mùa mới
///   - YearChangedPublish      — sang năm mới
///
/// Tích hợp:
///   - DayNightLightController đọc NormalizedTime
///   - StageObject lắng nghe DayChangedPublish
///   - SaveLoadManager lưu/khôi phục TimeState
///   - DebugConsole điều khiển qua SetTime/SetDay/SkipDay
/// </summary>
[DisallowMultipleComponent]
public class TimeManager : MonoBehaviour
{
    private const int MinutesPerDay = 24 * 60;

    [SerializeField] private TimeConfig config;
    [SerializeField] private bool runOnStart = true;
    [SerializeField] private bool useUnscaledTime;

    // ── State ─────────────────────────────────────────────
    private int _year;
    private Season _season;
    private int _day;          // 1-based
    private float _timeOfDaySeconds; // 0 .. config.dayDurationRealSeconds
    private bool _isRunning;

    // ── Cache để tránh publish trùng ──────────────────────
    private int _lastPublishedMinute = -1;
    private int _lastPublishedHour = -1;

    private EventBus _eventBus;

    // ══════════════════════════════════════════════════════
    //  PUBLIC PROPERTIES (read-only)
    // ══════════════════════════════════════════════════════

    public TimeConfig Config => config;
    public int Year => _year;
    public Season Season => _season;
    public int Day => _day;
    public int Hour => TotalMinutes / 60;
    public int Minute => TotalMinutes % 60;
    public bool IsRunning => _isRunning;
    public int CurrentTotalMinutes
    {
        get
        {
            int seasonsPerYear = (int)Season.Winter + 1;
            int completedYears = Mathf.Max(0, _year - 1);
            int completedSeasons = Mathf.Clamp((int)_season, 0, seasonsPerYear - 1);
            int completedDays = completedYears * seasonsPerYear * DaysPerSeason
                              + completedSeasons * DaysPerSeason
                              + Mathf.Max(0, _day - 1);
            return completedDays * MinutesPerDay + TotalMinutes;
        }
    }

    /// <summary>0..1 trong ngày (0 = 00:00, 0.5 = 12:00, 1 = 24:00).</summary>
    public float NormalizedTime => config == null || config.dayDurationRealSeconds <= 0f
        ? 0f
        : _timeOfDaySeconds / config.dayDurationRealSeconds;

    public float DayDurationRealSeconds => config != null ? config.dayDurationRealSeconds : 840f;
    public float SecondsPerGameHour => config != null ? config.SecondsPerGameHour : 35f;
    public int DaysPerSeason => config != null ? config.daysPerSeason : 28;

    /// <summary>Snapshot hiện tại — dùng cho save/load và UI.</summary>
    public TimeState CurrentState => new TimeState
    {
        year = _year,
        season = _season,
        day = _day,
        hour = Hour,
        minute = Minute
    };

    private int TotalMinutes
    {
        get
        {
            int minutes = Mathf.FloorToInt(Mathf.Clamp01(NormalizedTime) * MinutesPerDay);
            return Mathf.Clamp(minutes, 0, MinutesPerDay - 1);
        }
    }

    // ══════════════════════════════════════════════════════
    //  LIFECYCLE
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError("[TimeManager] TimeConfig chưa gán! Dùng default values.");
            // Fallback — vẫn chạy được nhưng không có config
        }

        _year = config != null ? config.startYear : 1;
        _season = config != null ? config.startSeason : Season.Spring;
        _day = config != null ? Mathf.Max(1, config.startDay) : 1;
        _timeOfDaySeconds = HourMinuteToDaySeconds(
            config != null ? config.startHour : 6,
            config != null ? config.startMinute : 0
        );
        _isRunning = runOnStart;
    }

    private void Start()
    {
        PublishCurrentTime(true);
    }

    private void Update()
    {
        if (!_isRunning) return;

        float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        AdvanceTime(delta);
    }

    // ══════════════════════════════════════════════════════
    //  PUBLIC API
    // ══════════════════════════════════════════════════════

    public void Play() => _isRunning = true;
    public void Pause() => _isRunning = false;

    /// <summary>Set giờ:phút trong ngày hiện tại.</summary>
    public void SetTime(int hour, int minute)
    {
        _timeOfDaySeconds = HourMinuteToDaySeconds(hour, minute);
        _lastPublishedMinute = -1;
        _lastPublishedHour = -1;
        PublishCurrentTime(true);
    }

    /// <summary>Set ngày trong mùa hiện tại (1-based).</summary>
    public void SetDay(int value)
    {
        _day = Mathf.Clamp(value, 1, DaysPerSeason);
        PublishCurrentTime(true);
    }

    /// <summary>Set mùa (giữ ngày/giờ hiện tại).</summary>
    public void SetSeason(Season season)
    {
        var oldSeason = _season;
        _season = season;
        if (oldSeason != _season)
            ResolveEventBus()?.Publish(new SeasonChangedPublish(_year, _season));
        PublishCurrentTime(true);
    }

    /// <summary>Nhảy sang ngày tiếp theo, reset giờ về startHour.</summary>
    public void SkipToNextDay()
    {
        AdvanceDay();
        _timeOfDaySeconds = HourMinuteToDaySeconds(
            config != null ? config.startHour : 6,
            config != null ? config.startMinute : 0
        );
        _lastPublishedMinute = -1;
        _lastPublishedHour = -1;
        PublishCurrentTime(true);
    }

    /// <summary>Tiến thời gian bằng realSeconds (dùng cho DebugConsole hoặc sleep transition).</summary>
    public void AdvanceTime(float realSeconds)
    {
        if (realSeconds <= 0f) return;
        float dayDuration = config != null ? config.dayDurationRealSeconds : 840f;
        if (dayDuration <= 0f) return;

        _timeOfDaySeconds += realSeconds;

        while (_timeOfDaySeconds >= dayDuration)
        {
            _timeOfDaySeconds -= dayDuration;
            AdvanceDay();
        }

        PublishCurrentTime(false);
    }

    // ══════════════════════════════════════════════════════
    //  SAVE / LOAD
    // ══════════════════════════════════════════════════════

    /// <summary>Lấy state để save.</summary>
    public TimeState GetSaveState() => CurrentState;

    /// <summary>Khôi phục state từ save.</summary>
    public void ApplySaveState(TimeState state)
    {
        _year = Mathf.Max(1, state.year);
        _season = state.season;
        _day = Mathf.Clamp(state.day, 1, DaysPerSeason);
        _timeOfDaySeconds = HourMinuteToDaySeconds(state.hour, state.minute);
        _lastPublishedMinute = -1;
        _lastPublishedHour = -1;
        PublishCurrentTime(true);
        Debug.Log($"[TimeManager] Restored: {CurrentState}");
    }

    // ══════════════════════════════════════════════════════
    //  PRIVATE
    // ══════════════════════════════════════════════════════

    private void AdvanceDay()
    {
        _day++;
        int maxDays = DaysPerSeason;

        if (_day > maxDays)
        {
            _day = 1;
            AdvanceSeason();
        }

        var bus = ResolveEventBus();
        // Thứ tự publish quan trọng:
        // 1. NextDayEventPublish TRƯỚC → StageObject/AnimalObject xử lý grow/produce
        // 2. DayChangedPublish SAU → reset watered tiles, UI update, etc.
        // Nếu đảo ngược: watered tiles bị reset trước khi cây check → cây không grow
        bus?.Publish(new NextDayEventPublish());
        bus?.Publish(new DayChangedPublish(_year, _season, _day));

        Debug.Log($"[TimeManager] Ngày mới: {CurrentState}");
    }

    private void AdvanceSeason()
    {
        var oldSeason = _season;
        int next = (int)_season + 1;

        if (next > (int)Season.Winter)
        {
            _season = Season.Spring;
            _year++;
            ResolveEventBus()?.Publish(new YearChangedPublish(_year));
            Debug.Log($"[TimeManager] Năm mới: {_year}");
        }
        else
        {
            _season = (Season)next;
        }

        ResolveEventBus()?.Publish(new SeasonChangedPublish(_year, _season));
        Debug.Log($"[TimeManager] Mùa mới: {_season}");
    }

    private void PublishCurrentTime(bool force)
    {
        var bus = ResolveEventBus();
        if (bus == null) return;

        int totalMinutes = TotalMinutes;
        int hour = totalMinutes / 60;
        int minute = totalMinutes % 60;

        if (force || totalMinutes != _lastPublishedMinute)
        {
            bus.Publish(new GameTimeChangedPublish(_day, hour, minute, NormalizedTime));
            _lastPublishedMinute = totalMinutes;
        }

        if (force || hour != _lastPublishedHour)
        {
            bus.Publish(new GameHourChangedPublish(_day, hour));
            _lastPublishedHour = hour;
        }
    }

    private float HourMinuteToDaySeconds(int hour, int minute)
    {
        float dayDuration = config != null ? config.dayDurationRealSeconds : 840f;
        int h = Mathf.Clamp(hour, 0, 23);
        int m = Mathf.Clamp(minute, 0, 59);
        float normalized = (h * 60f + m) / MinutesPerDay;
        return normalized * dayDuration;
    }

    private EventBus ResolveEventBus()
    {
        if (_eventBus != null) return _eventBus;
        _eventBus = GameManager.Instance != null ? GameManager.Instance.EventBus : FindAnyObjectByType<EventBus>();
        return _eventBus;
    }
}
