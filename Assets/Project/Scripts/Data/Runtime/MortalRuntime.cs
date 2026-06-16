using System.Collections;
using UnityEngine;

/// <summary>
/// Lắng nghe DieEvent → publish DestroyEntityRequestPublish.
/// SpawnSystem sẽ remove GameObject + EntityService.Destroy (unregister khỏi registry).
/// </summary>
public class MortalRuntime : IModuleRuntime, IHandleEvent<DieEvent>
{
    private readonly MortalModule _data;

    public MortalRuntime(MortalModule data)
    {
        _data = data;
    }

    public void Handle(DieEvent e)
    {
        if (e?.entity == null) return;

        // Guard cấu hình sai: Mortal + Respawn trên cùng entity sẽ conflict.
        // Ưu tiên RespawnRuntime để tránh destroy vĩnh viễn ngoài ý muốn.
        if (e.entity.GetModule<RespawnRuntime>() != null)
        {
            Debug.LogWarning(
                $"[MortalRuntime] '{e.entity.entityData?.keyName}' có cả MortalModule và RespawnModule. " +
                "Bỏ qua DestroyEntityRequest để RespawnRuntime xử lý.");
            return;
        }

        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;

        TryScheduleSceneMarkerRespawn(e.entity);
        float destroyDelay = ResolveDestroyDelay(e.entity);
        if (destroyDelay > 0f)
        {
            bus.StartCoroutine(DestroyAfterDelay(bus, e.entity.id, destroyDelay));
            return;
        }

        bus.Publish(new DestroyEntityRequestPublish(e.entity.id));
    }

    private float ResolveDestroyDelay(EntityRuntime entity)
    {
        float delay = Mathf.Max(0f, _data != null ? _data.destroyDelay : 0f);
        var go = entity?.Owner?.GameObject;
        var deathEffect = go != null ? go.GetComponentInChildren<EnemyDeathEffect>(true) : null;
        if (deathEffect != null)
            delay = Mathf.Max(delay, deathEffect.RequiredLifetime + 0.05f);
        return delay;
    }

    private static IEnumerator DestroyAfterDelay(EventBus bus, string entityId, float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delay));
        if (bus != null && !string.IsNullOrWhiteSpace(entityId))
            bus.Publish(new DestroyEntityRequestPublish(entityId));
    }

    private static void TryScheduleSceneMarkerRespawn(EntityRuntime entity)
    {
        var gm = GameManager.Instance;
        var worldService = gm?.WorldService;
        var timeManager = gm?.TimeManager;
        if (entity == null || worldService == null || timeManager == null)
            return;

        var ep = worldService.GetEntityPosition(entity.id);
        if (ep == null || ep.respawnMinutes <= 0)
            return;

        int availableAt = timeManager.CurrentTotalMinutes + ep.respawnMinutes;
        if (worldService.ScheduleInactiveRespawn(entity, availableAt))
            Debug.Log($"[MortalRuntime] Scheduled marker respawn for {entity.entityData?.keyName} at game minute {availableAt}.");
    }

    public ModuleSaveData ToSaveData() =>
        new ModuleSaveData { moduleType = "Mortal", dataJson = string.Empty };

    public void ApplySaveData(ModuleSaveData save) { }

    public bool Equals(IModuleRuntime other) => other is MortalRuntime;
}
