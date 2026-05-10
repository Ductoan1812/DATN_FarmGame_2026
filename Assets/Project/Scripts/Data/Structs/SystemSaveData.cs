/// <summary>
/// Save data cho các system-level state (không phải entity).
/// Mở rộng thêm field khi cần (weather, quest progress, NPC friendship...).
/// </summary>
[System.Serializable]
public class SystemSaveData
{
    public TimeState time;
}
