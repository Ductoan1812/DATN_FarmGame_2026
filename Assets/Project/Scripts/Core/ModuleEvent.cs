public interface IHandleEvent<T> where T : IGameEvent
{
    void Handle(T e);
}

public class SpawnedEvent : IGameEvent
{
    public readonly EntityRuntime entity;
    public SpawnedEvent(EntityRuntime entity) { this.entity = entity; }
}

public class NextDayEvent : IGameEvent
{
}

public class TakeDamageEvent : IGameEvent
{
    public readonly EntityRuntime attacker;
    public readonly float damage;
    public readonly ToolType toolType;

    public TakeDamageEvent(EntityRuntime attacker, float damage, ToolType toolType = ToolType.None)
    {
        this.attacker = attacker;
        this.damage   = damage;
        this.toolType = toolType;
    }
}


public class PrimaryActionEvent : IGameEvent
{
    public readonly EntityRuntime actor;
    public readonly EntityRuntime item;

    public PrimaryActionEvent(EntityRuntime actor)
    {
        this.actor = actor;
        this.item = null;
    }

    public PrimaryActionEvent(EntityRuntime actor, EntityRuntime item)
    {
        this.actor = actor;
        this.item = item;
    }
}

public class SecondaryActionEvent : IGameEvent
{
    public readonly EntityRuntime initiator;
    public readonly EntityRuntime target;
    public readonly InteractionContext context;

    public SecondaryActionEvent(EntityRuntime initiator)
    {
        this.initiator = initiator;
    }

    public SecondaryActionEvent(EntityRuntime initiator, EntityRuntime target, InteractionContext context)
    {
        this.initiator = initiator;
        this.target = target;
        this.context = context;
    }
}


public class DieEvent : IGameEvent
{
    public readonly EntityRuntime entity;
    public readonly EntityRuntime killer;
    public readonly bool suppressWorldDrops;

    public DieEvent(EntityRuntime entity, EntityRuntime killer = null, bool suppressWorldDrops = false)
    {
        this.entity = entity;
        this.killer = killer;
        this.suppressWorldDrops = suppressWorldDrops;
    }
}

/// <summary>
/// Animation đã đến frame "Strike" → thực thi logic chính của entity.
/// ToolActionBridge fire event này lên item entity khi animation chạy tới frame thực thi.
/// </summary>
public class AnimStrikeEvent : IGameEvent
{
    public readonly EntityRuntime actor;
    public readonly EntityRuntime item;

    public AnimStrikeEvent(EntityRuntime actor, EntityRuntime item)
    {
        this.actor = actor;
        this.item = item;
    }
}
