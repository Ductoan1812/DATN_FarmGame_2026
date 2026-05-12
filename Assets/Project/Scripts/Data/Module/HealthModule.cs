using UnityEngine;

/// <summary>
/// Module cấu hình khả năng nhận sát thương của entity.
/// HP thực tế nằm trong StatsRuntime.
/// </summary>
[System.Serializable]
public class HealthModule : IModuleData
{
    [Tooltip("Cho phép entity nhận sát thương.")]
    public bool canTakeDamage = true;

    public override IModuleRuntime CreateRuntime()
    {
        return new HealthRuntime(this);
    }
}
