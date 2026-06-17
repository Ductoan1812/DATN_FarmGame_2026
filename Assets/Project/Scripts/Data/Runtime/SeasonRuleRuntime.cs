public class SeasonRuleRuntime : IModuleRuntime
{
    private readonly SeasonRuleModule _data;

    public SeasonRuleRuntime(SeasonRuleModule data)
    {
        _data = data;
    }

    public bool AllowsSeason(Season season) => _data == null || _data.AllowsSeason(season);
    public bool BlocksPlacementOutOfSeason => _data != null && _data.blockPlacementOutOfSeason;
    public OutOfSeasonBehavior OutOfSeasonBehavior => _data != null ? _data.outOfSeasonBehavior : OutOfSeasonBehavior.None;
    public int DormantStageIndex => _data != null ? _data.dormantStageIndex : -1;

    public ModuleSaveData ToSaveData() => null;
    public void ApplySaveData(ModuleSaveData save) { }
    public bool Equals(IModuleRuntime other) => other is SeasonRuleRuntime;
}
