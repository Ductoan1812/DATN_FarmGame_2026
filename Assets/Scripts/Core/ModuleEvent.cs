public class SpawnedEvent : IGameEvent
{
    public readonly EntityRuntime entity;
    public SpawnedEvent(EntityRuntime entity) { this.entity = entity; }
}
public interface IHandleEvent<T> where T : IGameEvent
{
    void Handle(T e);
}

/// <summary>
///    Sự kiện được publish toàn cục khi sang ngày mới (được GameManager gọi).
/// </summary>
public class NextDayEvent : IGameEvent
{

}
public class OnEquipEvent : IGameEvent
{
    
}
/// <summary>
///     Sự kiện khi một entity nhận sát thương.
/// </summary>
public class TakeDamageEvent : IGameEvent
{
<<<<<<< HEAD
    public readonly EntityRuntime attacker;
    public float damage;  // mutable — module trước có thể modify trước khi module sau đọc
    public readonly ToolType toolType;
=======
    public int damage;
    public ToolType toolType;
>>>>>>> parent of 48e8ab7 (Feat: tạo module xử lý tấn công và nhận sát thương)

    public TakeDamageEvent(int damage, ToolType toolType = ToolType.None)
    {
        this.damage = damage;
        this.toolType = toolType;
    }
}
/// <summary>
/// Sự kiện khi người chơi sử dụng một item 
/// </summary>
public class UseEvent : IGameEvent
{
    public EntityRuntime entity;

    public UseEvent(EntityRuntime entity)
    {
        this.entity = entity;
    }
}
/// <summary>
/// Sự kiện khi entity chết (Hp <= 0). HealthRuntime phát hiện → TriggerEvent.
/// Các module khác (DropRuntime...) lắng nghe để xử lý hậu quả.
/// </summary>
public class DieEvent : IGameEvent
{
    public readonly EntityRuntime entity;
    public DieEvent(EntityRuntime entity)
    {
        this.entity = entity;
    }
}