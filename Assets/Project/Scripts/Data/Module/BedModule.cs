using UnityEngine;

/// <summary>
/// Module cho Bed entity — cho phép player ngủ kết thúc ngày.
/// Gắn vào EntityData của giường.
/// </summary>
[System.Serializable]
public class BedModule : IModuleData
{
    public override IModuleRuntime CreateRuntime()
    {
        return new BedRuntime();
    }
}
