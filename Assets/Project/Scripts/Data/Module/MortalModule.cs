/// <summary>
/// Marker module: entity có module này sẽ bị HỦY VĨNH VIỄN khi chết.
/// Dùng cho Enemy, Tree, EntityDrop, và bất kỳ entity nào không cần respawn.
///
/// KHÔNG được dùng chung với RespawnModule trên cùng 1 entity.
/// </summary>
[System.Serializable]
public class MortalModule : IModuleData
{
    [UnityEngine.Min(0f)]
    public float destroyDelay;

    public override IModuleRuntime CreateRuntime()
    {
        return new MortalRuntime(this);
    }
}
