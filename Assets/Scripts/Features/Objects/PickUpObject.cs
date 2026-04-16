using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class PickUpObject : MonoBehaviour
{
    [SerializeField] public string targetTag = "Player";
    private EntityRuntime entity;

    public void Init()
    {
        entity = GetComponent<EntityRoot>()?.GetEntity();
        if (entity == null)
            Debug.LogWarning($"[PickUpObject] No EntityRuntime found on {gameObject.name}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(targetTag)) return;
        var root = other.GetComponent<EntityRoot>();
        if (root == null || entity == null) return;
        var ownerEntity = root.GetEntity();
        if (ownerEntity == null) return;
        int received = GameManager.Instance.InventoryService.Pickup(entity, ownerEntity);
        if (entity.Amount <= 0)
        {
            gameObject.SetActive(false);
        }
    }
}
