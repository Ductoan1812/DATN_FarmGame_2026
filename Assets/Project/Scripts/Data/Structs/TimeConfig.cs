using UnityEngine;

/// <summary>
/// Config thời gian game — tạo qua Create menu, gán vào TimeManager.
/// Tách riêng để dễ tune balance mà không sửa code.
/// </summary>
[CreateAssetMenu(menuName = "Data/TimeConfig", fileName = "TimeConfig")]
public class TimeConfig : ScriptableObject
{
    [Header("Tốc độ thời gian")]
    [Tooltip("Bao nhiêu giây thực = toàn bộ 1 ngày game (24h). VD: 840 = 14 phút thực/ngày.")]
    [Min(1f)] public float dayDurationRealSeconds = 840f;

    [Header("Lịch")]
    [Tooltip("Số ngày mỗi mùa.")]
    [Min(1)] public int daysPerSeason = 28;

    [Header("Bắt đầu")]
    [Range(0, 23)] public int startHour = 6;
    [Range(0, 59)] public int startMinute = 0;
    public Season startSeason = Season.Spring;
    [Min(1)] public int startYear = 1;
    [Min(1)] public int startDay = 1;

    [Header("Giới hạn")]
    [Tooltip("Giờ bắt buộc ngủ (kiệt sức). 0 = không giới hạn.")]
    [Range(0, 23)] public int exhaustionHour = 2;

    /// <summary>Bao nhiêu giây thực = 1 giờ game.</summary>
    public float SecondsPerGameHour => dayDurationRealSeconds / 24f;

    /// <summary>Bao nhiêu giây thực = 1 phút game.</summary>
    public float SecondsPerGameMinute => dayDurationRealSeconds / (24f * 60f);
}
