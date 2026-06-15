using UnityEngine;

/// <summary>
/// Module khai báo chi phí stamina (thể lực) cho một hành động.
/// Attach vào EntityData của tool hoặc weapon trong Inspector.
///
/// Runtime (StaminaCostRuntime) cung cấp CanAfford() và Spend()
/// để ToolRuntime base class và WeaponRuntime query tập trung.
///
/// Nếu EntityData không có module này → hành động miễn phí (không tiêu stamina).
/// </summary>
[System.Serializable]
public class StaminaCostModule : IModuleData
{
    [Tooltip("Lượng thể lực tiêu hao mỗi lần thực hiện hành động. 0 = miễn phí.")]
    [Min(0f)]
    public float cost = 0f;

    public override IModuleRuntime CreateRuntime()
        => new StaminaCostRuntime(this);
}
