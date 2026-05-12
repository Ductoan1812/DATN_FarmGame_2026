public interface IModuleRuntime
{
    ModuleSaveData ToSaveData();
    void ApplySaveData(ModuleSaveData save);
    bool Equals(IModuleRuntime other);

    /// <summary>
    /// Module tự kiểm tra save data có thuộc về mình không.
    /// Default false → fallback về match bằng class name.
    /// Override khi có nhiều instance cùng type (vd: Hotbar vs Backpack).
    /// </summary>
    bool MatchesSave(ModuleSaveData save) => false;
}