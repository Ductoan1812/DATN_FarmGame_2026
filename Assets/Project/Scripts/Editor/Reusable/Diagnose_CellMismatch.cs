using UnityEngine;

/// <summary>
/// Debug: So sánh cell của cây (từ transform) vs cell đã tưới
/// </summary>
public class Diagnose_CellMismatch
{
    public static void Execute()
    {
        var gm = GameManager.Instance;
        if (gm == null) { Debug.LogError("[DIAG] GameManager null"); return; }

        var tracker = gm.WateredTileTracker;
        var roots = Object.FindObjectsOfType<EntityRoot>();

        Debug.Log("[DIAG] === CELL MISMATCH DIAGNOSIS ===");

        foreach (var root in roots)
        {
            var entity = root.GetEntity();
            if (entity == null) continue;
            var stage = entity.GetModule<StageRuntime>();
            if (stage == null) continue;

            var worldPos = root.gameObject.transform.position;

            // Method 1: FloorToInt (old way)
            var cellFloor = new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));

            // Method 2: GridSystem.WorldToCell (new way)
            var cell3 = GridSystem.WorldToCell(worldPos);
            var cellGrid = new Vector2Int(cell3.x, cell3.y);

            // Method 3: RoundToInt
            var cellRound = new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));

            Debug.Log($"[DIAG] Plant '{root.gameObject.name}': worldPos={worldPos}");
            Debug.Log($"[DIAG]   FloorToInt cell: {cellFloor} | watered={tracker?.IsWatered(cellFloor)}");
            Debug.Log($"[DIAG]   GridSystem cell: {cellGrid} | watered={tracker?.IsWatered(cellGrid)}");
            Debug.Log($"[DIAG]   RoundToInt cell: {cellRound} | watered={tracker?.IsWatered(cellRound)}");

            // Water all 3 cells and check
            tracker?.SetWatered(cellFloor);
            tracker?.SetWatered(cellGrid);
            tracker?.SetWatered(cellRound);

            Debug.Log($"[DIAG]   After watering all: floor={tracker?.IsWatered(cellFloor)}, grid={tracker?.IsWatered(cellGrid)}, round={tracker?.IsWatered(cellRound)}");
        }

        Debug.Log("[DIAG] === DONE ===");
    }
}
