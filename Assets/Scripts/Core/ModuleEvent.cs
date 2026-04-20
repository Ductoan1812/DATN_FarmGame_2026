// ══════════════════════════════════════════════════════════
//  Module events (IGameEvent) — phát qua EntityRuntime.TriggerEvent.
//  CHỈ các module trong cùng entity nhận được.
//  KHÔNG dùng EventBus.Publish cho loại này.
// ══════════════════════════════════════════════════════════

public interface IHandleEvent<T> where T : IGameEvent
{
    void Handle(T e);
}

public class SpawnedEvent : IGameEvent
{
    public readonly EntityRuntime entity;
    public SpawnedEvent(EntityRuntime entity) { this.entity = entity; }
}

/// <summary>
/// Sự kiện được phát khi sang ngày mới (cho module nội bộ entity).
/// EventBus tương ứng: NextDayEventPublish.
/// </summary>
public class NextDayEvent : IGameEvent
{
}

public class OnEquipEvent : IGameEvent
{
}

/// <summary>Sự kiện khi Player/Enemy bắt đầu tấn công.</summary>
public class AttackEvent : IGameEvent
{
    public readonly EntityRuntime attacker;
    public AttackEvent(EntityRuntime attacker) { this.attacker = attacker; }
}

/// <summary>Sự kiện khi một entity nhận sát thương.</summary>
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

/// <summary>Sự kiện khi người chơi sử dụng một item.</summary>
public class UseEvent : IGameEvent
{
    public EntityRuntime entity;
    public UseEvent(EntityRuntime entity) { this.entity = entity; }
}

/// <summary>
/// Sự kiện khi entity chết (HP = 0 hoặc bị harvest).
/// Các module nội bộ subscribe để phản ứng:
///   - DropRuntime: spawn drops.
///   - MortalRuntime: publish DestroyEntityRequestPublish (hủy vĩnh viễn).
///   - RespawnRuntime: publish DespawnRequestPublish + schedule respawn.
/// HealthRuntime CHỈ phát event, không tự xử lý Despawn/Destroy.
/// </summary>
public class DieEvent : IGameEvent
{
    public readonly EntityRuntime entity;
    public DieEvent(EntityRuntime entity) { this.entity = entity; }
}
