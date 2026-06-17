using UnityEngine;

[System.Serializable]
public class ConsumableModule : IModuleData
{
    [Tooltip("Hồi HP khi dùng.")]
    public float restoreHp;

    [Tooltip("Hồi Stamina khi dùng.")]
    public float restoreStamina;

    [Tooltip("Hồi MP khi dùng.")]
    public float restoreMp;

    [Tooltip("Số lượng item bị tiêu hao mỗi lần dùng.")]
    public int consumeAmount = 1;

    [Tooltip("Dùng xong có bị tiêu hao item không.")]
    public bool destroyOnUse = true;

    public override IModuleRuntime CreateRuntime()
    {
        return new ConsumableRuntime(this);
    }
}
