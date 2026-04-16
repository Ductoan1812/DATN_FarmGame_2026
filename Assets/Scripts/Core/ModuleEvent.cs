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
    public int damage;
    public ToolType toolType;

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
public class DoDropEvent : IGameEvent
{
    public EntityRuntime entity;
    public DoDropEvent(EntityRuntime entity)
    {
        this.entity = entity;
    }
}