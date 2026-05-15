using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hệ thống quét entity phía trước actor — dịch vụ dùng chung.
///
///   - GetAll():     danh sách entity trong vùng (Attack / PrimaryAction).
///   - GetClosest(): 1 entity gần nhất CÓ IInteractable (SecondaryAction / Interact).
///
/// Vùng quét: OverlapCircle tại (actor + facing * range/2), bán kính = range.
/// </summary>
public static class EntityScanSystem
{
    /// <summary>
    /// Quét TẤT CẢ entity trong vùng phía trước actor.
    /// </summary>
    public static List<EntityRuntime> GetAll(GameObject actorGO, float range = 1.5f)
    {
        var result = new List<EntityRuntime>();
        if (actorGO == null) return result;

        Vector2 facing = GetFacing(actorGO);
        Vector2 origin = (Vector2)actorGO.transform.position + facing * (range * 0.5f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range);

        foreach (var col in hits)
        {
            if (col == null) continue;
            if (col.gameObject == actorGO) continue;

            var entityRoot = col.GetComponentInParent<EntityRoot>();
            if (entityRoot == null) continue;

            var entity = entityRoot.GetEntity();
            if (entity == null) continue;

            if (!result.Contains(entity))
                result.Add(entity);
        }

        return result;
    }

    /// <summary>
    /// Quét 1 entity GẦN NHẤT phía trước actor, chỉ lấy GameObject có IInteractable.
    /// </summary>
    public static EntityRuntime GetClosest(GameObject actorGO, float range = 1f)
    {
        if (actorGO == null) return null;

        Vector2 facing = GetFacing(actorGO);
        Vector2 origin = (Vector2)actorGO.transform.position + facing * (range * 0.5f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range);

        EntityRuntime closest = null;
        float closestDist = float.MaxValue;

        foreach (var col in hits)
        {
            if (col == null) continue;
            if (col.gameObject == actorGO) continue;

            // Chỉ lấy entity có IInteractable trên GameObject.
            // Nếu dùng InteractablePrompt thì chỉ collider được chọn làm interactionCollider mới hợp lệ.
            var interactable = col.GetComponentInParent<IInteractable>();
            if (interactable == null) continue;

            var prompt = interactable as InteractablePrompt;
            if (prompt != null && !prompt.AcceptsScanCollider(col)) continue;

            var entityRoot = col.GetComponentInParent<EntityRoot>();
            if (entityRoot == null) continue;

            var entity = entityRoot.GetEntity();
            if (entity == null) continue;

            Vector2 distancePoint = prompt?.InteractionCollider != null
                ? prompt.InteractionCollider.ClosestPoint(actorGO.transform.position)
                : (Vector2)col.transform.position;

            float dist = Vector2.Distance(actorGO.transform.position, distancePoint);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = entity;
            }
        }

        return closest;
    }

    /// <summary>
    /// Lấy entity tại đúng 1 ô (cell). Dùng cho tool tác động 1 tile (Hoe, Axe, Pickaxe).
    /// </summary>
    public static EntityRuntime GetAtCell(Vector2Int cell2d, float radius = 0.4f)
    {
        Vector2 center = new Vector2(cell2d.x + 0.5f, cell2d.y + 0.5f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);

        EntityRuntime closest = null;
        float closestDist = float.MaxValue;

        foreach (var col in hits)
        {
            if (col == null) continue;

            var entityRoot = col.GetComponentInParent<EntityRoot>();
            if (entityRoot == null) continue;

            var entity = entityRoot.GetEntity();
            if (entity == null) continue;

            float dist = Vector2.Distance(center, (Vector2)col.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = entity;
            }
        }

        return closest;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Vector2 GetFacing(GameObject actorGO)
    {
        var ctrl = actorGO.GetComponent<PlayerControler>();
        if (ctrl != null)
        {
            var dir = ctrl.LastMoveDirection;
            if (dir.sqrMagnitude > 0.001f)
                return new Vector2(dir.x, dir.y).normalized;
        }
        return Vector2.up;
    }

    /// <summary>Vẽ gizmo debug vùng quét.</summary>
    public static void DrawGizmo(GameObject actorGO, float range = 1.5f)
    {
        if (actorGO == null) return;
        Vector2 facing = GetFacing(actorGO);
        Vector2 origin = (Vector2)actorGO.transform.position + facing * (range * 0.5f);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(origin, range);
    }
}
