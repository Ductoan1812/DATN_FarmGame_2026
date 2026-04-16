using UnityEngine;
using System;

/// <summary>
/// Xử lý nhận sát thương cho entity (Player, Enemy...).
/// - Đọc canTakeDamage từ HealthModule config.
/// - Tính giảm dame theo Defense.
/// - Trừ Hp trong StatsRuntime.
/// - Khi Hp <= 0 → publish EntityDiedEvent.
/// </summary>
public class HealthRuntime : IModuleRuntime, IHandleEvent<SpawnedEvent>, IHandleEvent<TakeDamageEvent>
{
    private readonly HealthModule _data;
    private EntityRuntime _entity;

    /// <summary>Có thể tắt tạm thời từ bên ngoài (invincibility frame, cutscene...).</summary>
    public bool CanTakeDamage
    {
        get => _data.canTakeDamage && _canTakeDamageOverride;
        set => _canTakeDamageOverride = value;
    }
    private bool _canTakeDamageOverride = true;

    /// <summary>Fired khi entity chết (Hp <= 0).</summary>
    public event Action<EntityRuntime> OnDied;

    public HealthRuntime(HealthModule data)
    {
        _data = data;
    }

    // ── Khởi tạo Hp khi entity được spawn ────────────────────────────────────

    public void Handle(SpawnedEvent e)
    {
        _entity = e.entity;
        float maxHp = _entity.stats.Get(StatType.MaxHp);
        if (maxHp > 0)
            _entity.stats.Set(StatType.Hp, maxHp);
    }

    // ── Nhận sát thương ───────────────────────────────────────────────────────

    public void Handle(TakeDamageEvent e)
    {
        if (_entity == null) return;
        if (!CanTakeDamage) return;

        // Tính giảm dame theo Defense: finalDamage = damage - Defense (tối thiểu 1)
        float defense    = _entity.stats.Get(StatType.Defense);
        float finalDamage = Mathf.Max(1f, e.damage - defense);

        float currentHp = _entity.stats.Get(StatType.Hp);
        float newHp     = Mathf.Max(0f, currentHp - finalDamage);
        _entity.stats.Set(StatType.Hp, newHp);

        var attackerName = e.attacker?.entityData?.keyName ?? "Unknown";
        Debug.Log($"[HealthRuntime] {_entity.entityData?.keyName} nhận {finalDamage:F1} dame " +
                  $"(raw={e.damage:F1}, def={defense:F1}) từ {attackerName}. " +
                  $"HP: {newHp:F1}/{_entity.stats.Get(StatType.MaxHp):F1}");

        if (newHp <= 0f)
            Die();
    }

    // ── Chết ─────────────────────────────────────────────────────────────────

    private void Die()
    {
        Debug.Log($"[HealthRuntime] {_entity.entityData?.keyName} đã chết.");
        OnDied?.Invoke(_entity);
        _entity.TriggerEvent(new DoDropEvent(_entity));

        // Despawn GameObject khỏi world
        var ownerGO = _entity.Owner?.GameObject;
        if (ownerGO != null)
        {
            var req = new DespawnRequest(_entity.Id);
            GameManager.Instance?.EventBus?.Publish(req);
        }
    }

    // ── Save / Load ───────────────────────────────────────────────────────────

    public ModuleSaveData ToSaveData() =>
        new ModuleSaveData { moduleType = "Health", dataJson = string.Empty };

    public void ApplySaveData(ModuleSaveData save) { }

    public bool Equals(IModuleRuntime other) => other is HealthRuntime;
}
