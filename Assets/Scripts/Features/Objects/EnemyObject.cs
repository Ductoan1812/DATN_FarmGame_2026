using UnityEngine;

/// <summary>
/// Enemy MonoBehaviour. Xử lý HP trực tiếp (không qua ItemRuntime).
/// Implement IDamageable để Tool/Player không cần biết đây là Enemy hay Plant.
/// </summary>
[DisallowMultipleComponent]
public class EnemyObject : MonoBehaviour, IDamageable
{
    [Header("Config")]
    [SerializeField] private int maxHp = 10;

    [Header("Runtime (readonly)")]
    [SerializeField] private int currentHp;

    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public bool IsAlive => currentHp > 0;

    private void Awake()
    {
        currentHp = maxHp;
    }

    public bool TakeDamage(int damage, ToolType toolType = ToolType.None)
    {
        if (!IsAlive) return false;

        currentHp = Mathf.Max(0, currentHp - damage);
        Debug.Log($"[Enemy] {name} nhận {damage} damage. HP: {currentHp}/{maxHp}");

        if (currentHp <= 0)
        {
            OnDie();
            return true;
        }
        return false;
    }

    private void OnDie()
    {
        Debug.Log($"[Enemy] {name} đã chết.");
        // TODO: drop item, play animation, publish event...
        Destroy(gameObject);
    }
}
