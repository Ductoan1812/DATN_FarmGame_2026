using UnityEngine;

public class ToolRequirementRuntime : IModuleRuntime
{
    private readonly ToolRequirementModule _data;

    public ToolRequirementRuntime(ToolRequirementModule data)
    {
        _data = data;
    }

    public bool TryResolveDamage(TakeDamageEvent e, out float damageMultiplier, out string reason)
    {
        damageMultiplier = 1f;
        reason = string.Empty;

        if (_data == null)
            return true;

        if (_data.requiredToolType != ToolType.None && e.toolType != _data.requiredToolType)
        {
            reason = $"Wrong tool: requires {_data.requiredToolType}, got {e.toolType}.";
            if (_data.blockDamageIfWrongTool)
                return false;

            damageMultiplier = _data.wrongToolPenalty;
            return damageMultiplier > 0f;
        }

        if (e.toolTier < Mathf.Max(1, _data.minimumToolTier))
        {
            reason = $"Tool tier too low: requires T{_data.minimumToolTier}, got T{e.toolTier}.";
            if (_data.blockDamageIfBelowTier)
                return false;

            damageMultiplier = _data.wrongToolPenalty;
            return damageMultiplier > 0f;
        }

        return true;
    }

    public ModuleSaveData ToSaveData() => null;
    public void ApplySaveData(ModuleSaveData save) { }
    public bool Equals(IModuleRuntime other) => other is ToolRequirementRuntime;
}
