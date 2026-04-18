using UnityEngine;

/// <summary>
/// Gate + modify damage cho cây trồng.
/// Trách nhiệm DUY NHẤT:
///   - Kiểm tra StageRuntime.CanHarvest → cho phép/chặn nhận dame
///   - Kiểm tra ToolType → modify e.damage nếu sai tool
/// KHÔNG trừ Hp, KHÔNG xử lý chết — đó là việc của HealthRuntime / DropRuntime.
/// </summary>
public class HarvestRuntime : IModuleRuntime, IHandleEvent<SpawnedEvent>, IHandleEvent<TakeDamageEvent>
{
    private readonly HarvestModule _data;
    private EntityRuntime _entity;

    public HarvestRuntime(HarvestModule data)
    {
        _data = data;
    }

    // ── Handle TakeDamageEvent — chạy TRƯỚC HealthRuntime (do thứ tự Inspector) ──

    public void Handle(TakeDamageEvent e)
    {
        // Lần đầu nhận event → cache entity ref
        if (_entity == null)
        {
            // Không có entity → không thể kiểm tra gì
            Debug.LogWarning("[HarvestRuntime] _entity null — chưa nhận SpawnedEvent?");
            return;
        }

        // ── Lấy HealthRuntime để set gate ──
        var health = _entity.GetModule<HealthRuntime>();

        // ── Lấy StageRuntime → kiểm tra CanHarvest ──
        var stage = _entity.GetModule<StageRuntime>();
        if (stage != null && !stage.CanHarvest)
        {
            // Chưa đến mùa thu hoạch → chặn dame
            if (health != null) health.CanTakeDamage = false;
            Debug.Log("[HarvestRuntime] Cây chưa đến mùa thu hoạch → bất tử.");
            return;
        }

        // ── Đến mùa → cho phép nhận dame ──
        if (health != null) health.CanTakeDamage = true;

        // ── Kiểm tra ToolType của attacker ──
        if (_data.harvestTool != ToolType.None && e.attacker != null)
        {
            var attackerToolType = GetAttackerToolType(e.attacker);

            if (attackerToolType != _data.harvestTool)
            {
                // Sai tool → giảm dame theo penalty
                float penalized = e.damage * _data.wrongToolPenalty;
                e.damage = Mathf.Max(1f, penalized);
                Debug.Log($"[HarvestRuntime] Sai tool (cần {_data.harvestTool}, dùng {attackerToolType}). " +
                          $"Dame giảm → {e.damage:F1}");
            }
            else
            {
                Debug.Log($"[HarvestRuntime] Đúng tool ({attackerToolType}). Dame giữ nguyên: {e.damage:F1}");
            }
        }
    }

    // ── Lấy ToolType từ weapon attacker đang cầm ──

    private ToolType GetAttackerToolType(EntityRuntime attacker)
    {
        var hotbar = attacker.GetModules<InventoryRuntime>()
                             .Find(i => i.Type == InventoryType.Hotbar);
        var weapon = hotbar?.SelectedEntity;
        if (weapon == null) return ToolType.None;

        var toolModule = weapon.GetModule<ToolRuntime>();
        if (toolModule == null) return ToolType.None;

        return toolModule.ToolType;
    }

    // ── Nhận entity ref khi spawn ──

    public void Handle(SpawnedEvent e)
    {
        _entity = e.entity;
    }

    // ── Save / Load ──

    public ModuleSaveData ToSaveData() =>
        new ModuleSaveData { moduleType = "Harvest", dataJson = string.Empty };

    public void ApplySaveData(ModuleSaveData save) { }

    public bool Equals(IModuleRuntime other)
    {
        if (other is not HarvestRuntime o) return false;
        return _data.harvestTool == o._data.harvestTool;
    }
}
