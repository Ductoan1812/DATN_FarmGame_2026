using UnityEngine;

/// <summary>
/// Module cho Sprinkler entity — tự động tưới các ô xung quanh mỗi ngày.
/// Gắn vào EntityData của sprinkler.
/// </summary>
[System.Serializable]
public class SprinklerModule : IModuleData
{
    [Tooltip("Bán kính tưới (Manhattan distance). VD: radius=1 → 5 ô (center + 4 cạnh), radius=2 → 13 ô.")]
    public int waterRadius = 1;

    public override IModuleRuntime CreateRuntime()
    {
        return new SprinklerRuntime(this);
    }
}
