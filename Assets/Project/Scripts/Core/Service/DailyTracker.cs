using UnityEngine;

public struct DailySummaryData
{
    public int day, year;
    public Season season;
    public int income, expGained, levelUps;
}

public class DailyTracker
{
    private readonly EventBus _bus;
    private readonly TimeManager _time;
    private int _income, _exp, _levelUps;
    public DailySummaryData LastSummary { get; private set; }

    public DailyTracker(EventBus bus, TimeManager time)
    {
        _bus = bus;
        _time = time;
        _bus?.Subscribe<ProgressionChangedPublish>(OnProgression);
        _bus?.Subscribe<LevelUpPublish>(OnLevelUp);
        _bus?.Subscribe<DayChangedPublish>(OnDayChanged);
    }

    public DailySummaryData GetLastSummary() => LastSummary;

    public void RecordIncome(int amount) => _income += Mathf.Max(0, amount);

    private void OnProgression(ProgressionChangedPublish e) => _exp += e.amount;
    private void OnLevelUp(LevelUpPublish e) => _levelUps++;

    private void OnDayChanged(DayChangedPublish e)
    {
        ResolveEndedDay(e, out int year, out Season season, out int day);
        LastSummary = new DailySummaryData
        {
            day = day, season = season, year = year,
            income = _income, expGained = _exp, levelUps = _levelUps
        };
        _income = _exp = _levelUps = 0;
    }

    private void ResolveEndedDay(DayChangedPublish e, out int year, out Season season, out int day)
    {
        year = e.year;
        season = e.season;

        if (e.day > 1)
        {
            day = e.day - 1;
            return;
        }

        day = _time != null ? _time.DaysPerSeason : 28;
        int seasonIndex = (int)e.season - 1;
        if (seasonIndex >= 0)
        {
            season = (Season)seasonIndex;
            return;
        }

        season = Season.Winter;
        year = Mathf.Max(1, e.year - 1);
    }

    public void Shutdown()
    {
        _bus?.Unsubscribe<ProgressionChangedPublish>(OnProgression);
        _bus?.Unsubscribe<LevelUpPublish>(OnLevelUp);
        _bus?.Unsubscribe<DayChangedPublish>(OnDayChanged);
    }
}
