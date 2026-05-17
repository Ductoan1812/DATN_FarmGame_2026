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

        bus.Publish(new DestroyEntityRequestPublish(e.entity.id));
    }

    public ModuleSaveData ToSaveData() =>
        new ModuleSaveData { moduleType = "Mortal", dataJson = string.Empty };

    public void ApplySaveData(ModuleSaveData save) { }

    public bool Equals(IModuleRuntime other) => other is MortalRuntime;
}
