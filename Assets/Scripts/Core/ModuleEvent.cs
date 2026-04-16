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
///     Sự kiện khi Player/Enemy bắt đầu tấn công.
/// </summary>
public class AttackEvent : IGameEvent
{
    public readonly EntityRuntime attacker;

    public AttackEvent(EntityRuntime attacker)
    {
        this.attacker = attacker;
    }
}

/// <summary>
///     Sự kiện khi một entity nhận sát thương.
/// </summary>
public class TakeDamageEvent : IGameEvent
{
    public readonly EntityRuntime attacker;
    public readonly float damage;
    public readonly ToolType toolType;

    /// <summary>Tấn công từ entity (combat system).</summary>
    public TakeDamageEvent(EntityRuntime attacker, float damage)
    {
        this.attacker = attacker;
        this.damage   = damage;
        this.toolType = ToolType.None;
    }

    /// <summary>Tấn công từ tool (harvest system — backward compat).</summary>
    public TakeDamageEvent(float damage, ToolType toolType = ToolType.None)
    {
        this.attacker = null;
        this.damage   = damage;
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