/// <summary>
/// Snapshot toàn bộ trạng thái thời gian — dùng cho save/load và truy vấn.
/// Serializable bằng JsonUtility.
/// </summary>
[System.Serializable]
public struct TimeState
{
    public int year;
    public Season season;
    public int day;       // 1-based (1..daysPerSeason)
    public int hour;      // 0-23
    public int minute;    // 0-59 (bước theo minutesPerTick)
    public bool hasPreciseTimeOfDay;
    public float timeOfDaySeconds;

    /// <summary>Tổng số ngày tuyệt đối kể từ đầu game (Year1 Spring Day1 = 1).</summary>
    public int TotalDays(int daysPerSeason)
    {
        return (year - 1) * 4 * daysPerSeason
             + (int)season * daysPerSeason
             + day;
    }

    public override string ToString()
        => $"Y{year} {season} Day{day} {hour:D2}:{minute:D2}";
}
