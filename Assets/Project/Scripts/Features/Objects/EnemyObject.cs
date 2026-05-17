using UnityEngine;

/// <summary>
/// Enemy bridge:
/// - Runtime mode (khuyến nghị): dùng EntityRuntime stats/health để combat.
/// - Legacy mode: fallback HP local để tương thích prefab cũ.
/// </summary>
[DisallowMultipleComponent]
public class EnemyObject : MonoBehaviour, IDamageable
{
    [Header("Mode")]
    [SerializeField] private bool useEntityRuntime = true;

    [Header("AI Combat")]
    [SerializeField] private float detectRange = 5f;
    [SerializeField] private float attackRange = 1.1f;
    [SerializeField] private float stopDistance = 0.65f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private int fallbackAttackDamage = 2;

    [Header("Legacy Fallback HP")]
    [SerializeField] private int maxHp = 10;
    [SerializeField] private int currentHp;

    private EntityRoot selfRoot;
    private EntityRuntime selfEntity;
    private PlayerControler player;
    private EntityRoot playerRoot;
    private float lastAttackTime = -999f;

    public int CurrentHp
    {
        get
        {
            if (useEntityRuntime && selfEntity?.stats != null)
                return Mathf.RoundToInt(selfEntity.stats.Get(StatType.Hp));

            return currentHp;
        }
    }

    public int MaxHp
    {
        get
        {
            if (useEntityRuntime && selfEntity?.stats != null)
            {
                float max = selfEntity.stats.Get(StatType.MaxHp);
                return max > 0f ? Mathf.RoundToInt(max) : maxHp;
            }

            return maxHp;
        }
    }

    public bool IsAlive => CurrentHp > 0;

    private void Awake()
    {
        currentHp = maxHp;
        selfRoot = GetComponent<EntityRoot>();
    }

    private void Update()
    {
        if (!useEntityRuntime)
            return;

        RefreshRuntimeRefs();
        if (selfEntity == null || !IsAlive) return;
        if (player == null) return;

        float runtimeSpeed = selfEntity.stats.Get(StatType.Speed);
        float speed = runtimeSpeed > 0f ? runtimeSpeed : moveSpeed;

        Vector3 enemyPos = transform.position;
        Vector3 playerPos = player.transform.position;
        float distance = Vector2.Distance(enemyPos, playerPos);
        if (distance > detectRange) return;

        if (distance > stopDistance && distance > attackRange)
        {
            Vector3 dir = (playerPos - enemyPos).normalized;
            transform.position += dir * speed * Time.deltaTime;
        }

        if (distance <= attackRange && Time.time - lastAttackTime >= attackCooldown)
        {
            TryAttackPlayer();
            lastAttackTime = Time.time;
        }
    }

    public bool TakeDamage(int damage, ToolType toolType = ToolType.None)
    {
        if (!IsAlive) return false;

        if (useEntityRuntime && selfEntity != null)
        {
            int hpBefore = CurrentHp;
            selfEntity.TriggerEvent(new TakeDamageEvent(selfEntity, damage, toolType));
            return hpBefore > 0 && !IsAlive;
        }

        currentHp = Mathf.Max(0, currentHp - damage);
        Debug.Log($"[Enemy] {name} nhận {damage} damage. HP: {currentHp}/{maxHp}");

        if (currentHp <= 0)
        {
            OnLegacyDie();
            return true;
        }

        return false;
    }

    private void TryAttackPlayer()
    {
        if (playerRoot == null)
            playerRoot = player != null ? player.GetComponent<EntityRoot>() : null;

        var playerEntity = playerRoot?.GetEntity();
        if (playerEntity == null) return;

        float attack = selfEntity.stats.Get(StatType.Attack);
        if (attack <= 0f) attack = fallbackAttackDamage;

        playerEntity.TriggerEvent(new TakeDamageEvent(selfEntity, attack));
    }

    private void RefreshRuntimeRefs()
    {
        if (selfEntity == null)
            selfEntity = selfRoot?.GetEntity();

        if (player == null)
            player = FindAnyObjectByType<PlayerControler>();

        if (playerRoot == null && player != null)
            playerRoot = player.GetComponent<EntityRoot>();
    }

    private void OnLegacyDie()
    {
        Debug.Log($"[Enemy] {name} đã chết (legacy mode).");
        Destroy(gameObject);
    }
}
