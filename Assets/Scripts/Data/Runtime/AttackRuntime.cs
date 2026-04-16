using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Xử lý tấn công cho entity (Player, Enemy...).
/// Flow:
///   1. Nhận AttackEvent
///   2. Lấy CoolDown / Range từ entity đang cầm (Hotbar) → fallback AttackModule
///   3. Kiểm tra cooldown
///   4. Tính finalDamage = Attack × (1 + CritDamage nếu trúng CritChance)
///   5. Gọi CombatScanSystem.GetTargets() → danh sách target
///   6. Gửi TakeDamageEvent tới từng target
/// </summary>
public class AttackRuntime : IModuleRuntime, IHandleEvent<AttackEvent>
{
    private readonly AttackModule _data;
    private float _lastAttackTime = -999f;

    public AttackRuntime(AttackModule data)
    {
        _data = data;
    }

    // ── Xử lý tấn công ───────────────────────────────────────────────────────

    public void Handle(AttackEvent e)
    {
        var attacker = e.attacker;
        if (attacker == null) return;

        // ── Lấy CoolDown / Range từ weapon đang cầm ──────────────────────────
        float cooldown;
        float range;
        ResolveWeaponStats(attacker, out cooldown, out range);

        // ── Cooldown ──────────────────────────────────────────────────────────
        if (Time.time - _lastAttackTime < cooldown) return;

        // ── Owner GameObject ──────────────────────────────────────────────────
        var ownerGO = attacker.Owner?.GameObject;
        if (ownerGO == null)
        {
            Debug.LogWarning("[AttackRuntime] Owner.GameObject null.");
            return;
        }

        // ── Tính finalDamage ──────────────────────────────────────────────────
        float attack     = attacker.stats.Get(StatType.Attack);
        float critChance = attacker.stats.Get(StatType.CritChance);   // 0..1
        float critDamage = attacker.stats.Get(StatType.CritDamage);   // 0..1 (0.5 = +50%)

        bool isCrit      = Random.value < critChance;
        float finalDamage = isCrit ? attack * (1f + critDamage) : attack;

        if (isCrit)
            Debug.Log($"[AttackRuntime] CHÍ MẠNG! Dame: {finalDamage:F1} (base={attack:F1}, crit×{1f + critDamage:F2})");
        else
            Debug.Log($"[AttackRuntime] Tấn công. Dame: {finalDamage:F1}");

        // ── Quét target ───────────────────────────────────────────────────────
        List<EntityRuntime> targets = CombatScanSystem.GetTargets(ownerGO, range);

        if (targets == null || targets.Count == 0)
        {
            Debug.Log("[AttackRuntime] Không có target trong tầm.");
            _lastAttackTime = Time.time;
            return;
        }

        // ── Gửi TakeDamageEvent tới từng target ───────────────────────────────
        foreach (var target in targets)
        {
            target.TriggerEvent(new TakeDamageEvent(attacker, finalDamage));
        }

        _lastAttackTime = Time.time;
    }

    // ── Lấy CoolDown / Range từ weapon, fallback AttackModule ─────────────────

    private void ResolveWeaponStats(EntityRuntime attacker, out float cooldown, out float range)
    {
        // Thử lấy entity đang cầm từ Hotbar
        var hotbar = attacker.GetModules<InventoryRuntime>()
                             .Find(i => i.Type == InventoryType.Hotbar);
        var weapon = hotbar?.SelectedEntity;

        if (weapon != null)
        {
            // Có weapon → lấy CoolDown / Range từ stats của weapon
            cooldown = weapon.stats.Get(StatType.CoolDown);
            range    = weapon.stats.Get(StatType.Range);

            if (cooldown <= 0f)
                Debug.LogWarning($"[AttackRuntime] Weapon '{weapon.entityData?.keyName}' không có stat CoolDown. Kiểm tra EntityData.");
            if (range <= 0f)
                Debug.LogWarning($"[AttackRuntime] Weapon '{weapon.entityData?.keyName}' không có stat Range. Kiểm tra EntityData.");

            return;
        }

        // Không có weapon → fallback AttackModule
        Debug.Log("[AttackRuntime] Không có weapon đang cầm → dùng giá trị mặc định từ AttackModule.");
        cooldown = _data.attackCooldown;
        range    = _data.attackRange;
    }

    // ── Save / Load ───────────────────────────────────────────────────────────

    public ModuleSaveData ToSaveData() =>
        new ModuleSaveData { moduleType = "Attack", dataJson = string.Empty };

    public void ApplySaveData(ModuleSaveData save) { }

    public bool Equals(IModuleRuntime other) => other is AttackRuntime;
}
