using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Lắng nghe DieEvent → despawn GameObject, sau delay → respawn tại CurrentRespawnPosition.
/// Entity KHÔNG bị Unregister — giữ nguyên inventory, stats, modules.
///
/// CurrentRespawnPosition có thể thay đổi runtime (setter public) và được lưu save/load.
/// Giá trị khởi tạo lấy từ RespawnModule.defaultRespawnPosition.
/// </summary>
public class RespawnRuntime : IModuleRuntime, IHandleEvent<DieEvent>
{
    private readonly RespawnModule _data;
    private EntityRuntime _entity;

    /// <summary>Vị trí respawn hiện tại. Có thể set runtime (checkpoint…). Được save/load.</summary>
    public Vector2 CurrentRespawnPosition { get; set; }

    public RespawnRuntime(RespawnModule data)
    {
        _data = data;
        CurrentRespawnPosition = data.defaultRespawnPosition;
    }

    public void Handle(DieEvent e)
    {
        if (e?.entity == null) return;
        _entity = e.entity;

        var gm = GameManager.Instance;
        if (gm == null || gm.EventBus == null)
        {
            Debug.LogWarning("[RespawnRuntime] GameManager/EventBus chưa sẵn sàng.");
            return;
        }

        // 1. Despawn GameObject (ẩn khỏi world) — entity vẫn còn trong registry.
        gm.EventBus.Publish(new DespawnRequestPublish(_entity.id));

        // 2. Schedule respawn sau delay.
        gm.EventBus.StartCoroutine(RespawnAfterDelay(gm));
    }

    private IEnumerator RespawnAfterDelay(GameManager gm)
    {
        yield return new WaitForSeconds(_data.respawnDelay);

        if (_entity == null) yield break;

        // Restore HP nếu cấu hình
        if (_data.restoreFullHp)
        {
            float maxHp = _entity.stats.Get(StatType.MaxHp);
            if (maxHp > 0) _entity.stats.Set(StatType.Hp, maxHp);
        }

        // Publish SpawnRequestPublish với runtime = entity hiện tại (không tạo mới)
        gm.EventBus.Publish(new SpawnRequestPublish(
            worldPos:         CurrentRespawnPosition,
            idPrefab:         _data.respawnPrefabId,
            runtime:          _entity,
            bypassValidation: true
        ));

        Debug.Log($"[RespawnRuntime] Respawned {_entity.entityData?.keyName} tại {CurrentRespawnPosition}.");
    }

    // ── Save / Load ───────────────────────────────────────────────────────────

    [Serializable]
    private class RespawnSaveData
    {
        public float x;
        public float y;
    }

    public ModuleSaveData ToSaveData()
    {
        var data = new RespawnSaveData
        {
            x = CurrentRespawnPosition.x,
            y = CurrentRespawnPosition.y
        };
        return new ModuleSaveData
        {
            moduleType = "Respawn",
            dataJson   = JsonUtility.ToJson(data)
        };
    }

    public void ApplySaveData(ModuleSaveData save)
    {
        if (save == null || string.IsNullOrEmpty(save.dataJson)) return;
        var data = JsonUtility.FromJson<RespawnSaveData>(save.dataJson);
        if (data == null) return;
        CurrentRespawnPosition = new Vector2(data.x, data.y);
    }

    public bool Equals(IModuleRuntime other) => other is RespawnRuntime;
}
