using UnityEngine;

/// <summary>
/// Sprinkler runtime — tự động tưới các ô xung quanh mỗi ngày.
/// Đăng ký vào SprinklerRegistry, GameManager tick TRƯỚC crop growth.
/// </summary>
public class SprinklerRuntime : IModuleRuntime, IHandleEvent<SpawnedEvent>
{
    private readonly SprinklerModule _data;
    private IEntityContainer _owner;

    public bool IsAlive => _owner?.GameObject != null;

    public SprinklerRuntime(SprinklerModule data)
    {
        _data = data;
    }

    public void Handle(SpawnedEvent e)
    {
        _owner = e.entity.Owner;
        GameManager.Instance?.SprinklerRegistry?.Register(this);
    }

    public void Tick()
    {
        if (_owner?.GameObject == null) return;

        var tracker = GameManager.Instance?.WateredTileTracker;
        var worldService = GameManager.Instance?.WorldService;
        if (tracker == null || worldService == null) return;

        var worldPos = _owner.GameObject.transform.position;
        Vector3Int centerCell = GridSystem.WorldToCell(worldPos);

        int count = 0;
        for (int dx = -_data.waterRadius; dx <= _data.waterRadius; dx++)
        {
            for (int dy = -_data.waterRadius; dy <= _data.waterRadius; dy++)
            {
                if (Mathf.Abs(dx) + Mathf.Abs(dy) <= _data.waterRadius)
                {
                    var cell = new Vector2Int(centerCell.x + dx, centerCell.y + dy);
                    if (!worldService.IsPlowed(cell))
                        continue;

                    tracker.SetWatered(cell);
                    count++;
                }
            }
        }

        Debug.Log($"[SprinklerRuntime] '{_owner.GameObject.name}' đã tưới {count} ô (radius={_data.waterRadius}).");
    }

    // ── Save / Load ───────────────────────────────────────────────────────────
    public ModuleSaveData ToSaveData() => null;
    public void ApplySaveData(ModuleSaveData save) { }
    public bool Equals(IModuleRuntime other) => other is SprinklerRuntime o && _data.waterRadius == o._data.waterRadius;
}
