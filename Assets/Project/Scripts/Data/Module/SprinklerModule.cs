using UnityEngine;

/// <summary>
/// Module cho Sprinkler entity — tự động tưới các ô xung quanh mỗi ngày.
/// Gắn vào EntityData của sprinkler.
/// </summary>
[System.Serializable]
public class SprinklerModule : IModuleData
{
    [Tooltip("Bán kính tưới theo Manhattan distance, bỏ ô trung tâm. VD: radius=1 → 4 ô cạnh theo dấu cộng.")]
    public int waterRadius = 1;

    public override IModuleRuntime CreateRuntime()
    {
        return new SprinklerRuntime(this);
    }
}
