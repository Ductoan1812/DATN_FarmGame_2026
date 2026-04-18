using UnityEngine;

/// <summary>
/// Base class chung cho tất cả tool runtime.
/// Xử lý: cooldown, owner → targetCell.
/// Subclass chỉ cần override Execute() với logic riêng.
/// </summary>
public abstract class ToolRuntime : IModuleRuntime, IHandleEvent<UseEvent>
{
    protected readonly ToolModule _data;
    private float _lastUseTime = -999f;

    /// <summary>Loại tool (Hoe, Axe...) — expose cho các system bên ngoài đọc.</summary>
    public ToolType ToolType => _data?.toolType ?? ToolType.None;

    protected ToolRuntime(ToolModule data)
    {
        _data = data;
    }

    public void Handle(UseEvent e)
    {
        // ── Cooldown ──────────────────────────────────────────────────────────
        float cd = _data?.cooldown ?? 0.3f;
        if (cd <= 0f)
            Debug.LogWarning($"[{GetType().Name}] cooldown = 0, kiểm tra Inspector!");

        if (Time.time - _lastUseTime < cd) return;

        // ── Owner → GameObject ────────────────────────────────────────────────
        var ownerGO = e.entity?.Owner?.GameObject;
        if (ownerGO == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Owner.GameObject null — entity chưa vào inventory?");
            return;
        }

        // ── Target cell ───────────────────────────────────────────────────────
        var targetCell = GridSystem.GetCellInFrontOf(ownerGO);
        var cell2d = new Vector2Int(targetCell.x, targetCell.y);

        // ── Delegate logic riêng cho subclass ─────────────────────────────────
        if (!Execute(ownerGO, targetCell, cell2d)) return;

        _lastUseTime = Time.time;
    }

    /// <summary>
    /// Thực thi logic riêng của từng tool.
    /// Trả về true nếu thành công (cập nhật cooldown), false nếu bị block.
    /// </summary>
    protected abstract bool Execute(GameObject ownerGO, Vector3Int targetCell, Vector2Int cell2d);

    // ── Save / Load ───────────────────────────────────────────────────────────

    public virtual ModuleSaveData ToSaveData() =>
        new ModuleSaveData { moduleType = _data?.toolType.ToString() ?? "Tool", dataJson = "" };

    public virtual void ApplySaveData(ModuleSaveData save) { }

    public virtual bool Equals(IModuleRuntime other) => other?.GetType() == GetType();
}
