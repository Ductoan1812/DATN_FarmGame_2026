using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quét các GameObject trong tầm tấn công phía trước owner.
/// Trả về danh sách EntityRuntime của các target hợp lệ.
///
/// Cách hoạt động:
///   1. Lấy hướng facing của owner (từ PlayerControler hoặc mặc định Vector2.up).
///   2. Tính điểm origin = vị trí owner + offset nhỏ theo hướng facing.
///   3. OverlapCircle bán kính attackRange tại origin.
///   4. Lọc: bỏ chính owner, lấy EntityRoot → EntityRuntime.
/// </summary>
public static class CombatScanSystem
{
    /// <summary>
    /// Quét target trong tầm tấn công của ownerGO.
    /// </summary>
    /// <param name="ownerGO">GameObject của attacker.</param>
    /// <param name="attackRange">Bán kính quét (đơn vị Unity).</param>
    /// <returns>Danh sách EntityRuntime bị tác động (không bao gồm chính owner).</returns>
    public static List<EntityRuntime> GetTargets(GameObject ownerGO, float attackRange = 1.5f)
    {
        var result = new List<EntityRuntime>();
        if (ownerGO == null) return result;

        // ── Lấy hướng facing ─────────────────────────────────────────────────
        Vector2 facing = GetFacing(ownerGO);

        // ── Origin = vị trí owner + offset nhỏ theo hướng facing ─────────────
        Vector2 origin = (Vector2)ownerGO.transform.position + facing * (attackRange * 0.5f);

        // ── OverlapCircle ─────────────────────────────────────────────────────
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, attackRange);

        foreach (var col in hits)
        {
            if (col == null) continue;
            if (col.gameObject == ownerGO) continue;

            // Lấy EntityRoot trên chính GO hoặc parent gần nhất
            var entityRoot = col.GetComponentInParent<EntityRoot>();
            if (entityRoot == null) continue;

            var entity = entityRoot.GetEntity();
            if (entity == null) continue;

            // Tránh thêm trùng (nhiều collider trên cùng 1 entity)
            if (!result.Contains(entity))
                result.Add(entity);
        }

        return result;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Lấy hướng facing của ownerGO.
    /// Ưu tiên PlayerControler.LastMoveDirection, fallback về Vector2.up.
    /// </summary>
    private static Vector2 GetFacing(GameObject ownerGO)
    {
        var ctrl = ownerGO.GetComponent<PlayerControler>();
        if (ctrl != null)
        {
            var dir = ctrl.LastMoveDirection;
            if (dir.sqrMagnitude > 0.001f)
                return new Vector2(dir.x, dir.y).normalized;
        }

        // Fallback: hướng lên (mặc định khi đứng yên)
        return Vector2.up;
    }

    /// <summary>
    /// Vẽ gizmo debug vùng quét (gọi từ OnDrawGizmosSelected nếu cần).
    /// </summary>
    public static void DrawGizmo(GameObject ownerGO, float attackRange = 1.5f)
    {
        if (ownerGO == null) return;
        Vector2 facing = GetFacing(ownerGO);
        Vector2 origin = (Vector2)ownerGO.transform.position + facing * (attackRange * 0.5f);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(origin, attackRange);
    }
}
