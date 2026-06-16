using System.Collections;
using UnityEngine;

/// <summary>
/// Runtime bridge for enemy movement, combat, and animation.
/// EntityRuntime remains the source of truth for stats, damage, drops, and death.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyObject : MonoBehaviour, IDamageable
{
    public enum EnemyState
    {
        Idle,
        Wander,
        Chase,
        Attack,
        Hurt,
        ReturnHome,
        Dead
    }

    private static readonly int FacingParam = Animator.StringToHash("Facing");
    private static readonly int MoveStateParam = Animator.StringToHash("MoveState");
    private static readonly int AttackParam = Animator.StringToHash("Attack");
    private static readonly int HurtParam = Animator.StringToHash("Hurt");
    private static readonly int DeadParam = Animator.StringToHash("Dead");

    [Header("Mode")]
    [SerializeField] private bool useEntityRuntime = true;

    [Header("Detection")]
    [SerializeField, Min(0.1f)] private float detectRange = 5f;
    [SerializeField, Min(0.1f)] private float attackRange = 1.1f;
    [SerializeField, Min(0.1f)] private float leashRange = 8f;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 2f;
    [SerializeField, Min(0f)] private float wanderRadius = 3f;
    [SerializeField, Min(0.1f)] private float wanderWaitMin = 1.25f;
    [SerializeField, Min(0.1f)] private float wanderWaitMax = 3f;
    [SerializeField, Min(0.05f)] private float chaseRepathInterval = 0.25f;

    [Header("Combat")]
    [SerializeField, Min(0.05f)] private float attackCooldown = 1f;
    [SerializeField, Min(0f)] private float attackWindupFallback = 0.35f;
    [SerializeField, Min(0.1f)] private float attackAnimationTimeout = 1.5f;
    [SerializeField, Min(0.1f)] private float hurtAnimationTimeout = 0.5f;
    [SerializeField] private int fallbackAttackDamage = 2;

    [Header("Legacy Fallback HP")]
    [SerializeField] private int maxHp = 10;
    [SerializeField] private int currentHp;

    private EntityRoot selfRoot;
    private EntityRuntime selfEntity;
    private HealthRuntime healthRuntime;
    private PlayerControler player;
    private EntityRoot playerRoot;
    private NavAgent2D navAgent;
    private Animator animator;
    private Rigidbody2D body;
    private Collider2D movementCollider;
    private StatsRuntime boundStats;
    private EnemyState state;
    private Vector2 homePosition;
    private Vector2 facing = Vector2.down;
    private float previousHp;
    private float nextWanderTime;
    private float nextChaseRepathTime;
    private float lastAttackTime = -999f;
    private bool attackStrikeConsumed;
    private Coroutine actionSafetyCoroutine;

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
                float runtimeMax = selfEntity.stats.Get(StatType.MaxHp);
                return runtimeMax > 0f ? Mathf.RoundToInt(runtimeMax) : maxHp;
            }

            return maxHp;
        }
    }

    public bool IsAlive => CurrentHp > 0 && state != EnemyState.Dead;
    public EnemyState CurrentState => state;
    public event System.Action<EnemyState> StateChanged;
    public event System.Action AttackStarted;
    public event System.Action DeathStarted;
    public event System.Action DeathAnimationCompleted;

    private void Awake()
    {
        currentHp = maxHp;
        selfRoot = GetComponent<EntityRoot>();
        navAgent = GetComponent<NavAgent2D>();
        animator = GetComponentInChildren<Animator>();
        body = GetComponent<Rigidbody2D>();
        movementCollider = GetComponent<Collider2D>();
        homePosition = transform.position;
        EnsureFeedbackComponents();
        ScheduleNextWander();
    }

    private void OnEnable()
    {
        if (selfRoot != null)
            selfRoot.OnEntityReady += OnEntityReady;
    }

    private void Start()
    {
        BindEntity(selfRoot != null ? selfRoot.GetEntity() : null);
        ApplyFacing(facing);
        ApplyMoveState(0);
    }

    private void OnDisable()
    {
        if (selfRoot != null)
            selfRoot.OnEntityReady -= OnEntityReady;

        UnbindEntity();
    }

    private void Update()
    {
        RefreshRuntimeRefs();
        if (state == EnemyState.Dead || state == EnemyState.Hurt || state == EnemyState.Attack)
            return;
        if (useEntityRuntime && (selfEntity == null || !IsAlive))
            return;

        UpdateMovementAnimation();

        if (player == null || !IsPlayerAlive())
        {
            if (state == EnemyState.Chase)
            {
                BeginReturnHome();
                return;
            }

            UpdatePassiveMovement();
            return;
        }

        Vector2 enemyPosition = transform.position;
        Vector2 playerPosition = player.transform.position;
        float distanceToPlayer = Vector2.Distance(enemyPosition, playerPosition);
        float playerDistanceFromHome = Vector2.Distance(homePosition, playerPosition);

        if (state == EnemyState.ReturnHome)
        {
            UpdateReturnHome();
            return;
        }

        bool canAggro = distanceToPlayer <= detectRange && playerDistanceFromHome <= leashRange;
        if (!canAggro && state != EnemyState.Chase)
        {
            UpdatePassiveMovement();
            return;
        }

        if (playerDistanceFromHome > leashRange)
        {
            BeginReturnHome();
            return;
        }

        FaceTowards(playerPosition - enemyPosition);
        if (distanceToPlayer <= attackRange && HasAttackLine(playerPosition))
        {
            navAgent?.Stop();
            ApplyMoveState(0);
            if (Time.time - lastAttackTime >= attackCooldown)
                BeginAttack();
            return;
        }

        SetState(EnemyState.Chase);
        ApplyMoveState(2);
        if (navAgent != null && Time.time >= nextChaseRepathTime)
        {
            navAgent.SetDestination(playerPosition, Mathf.Max(0.05f, attackRange * 0.8f));
            nextChaseRepathTime = Time.time + chaseRepathInterval;
        }
    }

    public void Configure(
        float newDetectRange,
        float newAttackRange,
        float newAttackCooldown,
        float newLeashRange,
        float newWanderRadius)
    {
        detectRange = Mathf.Max(0.1f, newDetectRange);
        attackRange = Mathf.Max(0.1f, newAttackRange);
        attackCooldown = Mathf.Max(0.05f, newAttackCooldown);
        leashRange = Mathf.Max(detectRange, newLeashRange);
        wanderRadius = Mathf.Max(0f, newWanderRadius);
    }

    public bool TakeDamage(int damage, ToolType toolType = ToolType.None)
    {
        if (!IsAlive)
            return false;

        if (useEntityRuntime && selfEntity != null)
        {
            int hpBefore = CurrentHp;
            RefreshRuntimeRefs();
            var attacker = playerRoot != null ? playerRoot.GetEntity() : null;
            selfEntity.TriggerEvent(new TakeDamageEvent(attacker, damage, toolType));
            return hpBefore > 0 && CurrentHp <= 0;
        }

        currentHp = Mathf.Max(0, currentHp - damage);
        if (currentHp <= 0)
        {
            EnterDeadState();
            Destroy(gameObject, ResolveLegacyDestroyDelay());
            return true;
        }

        BeginHurt();
        return false;
    }

    public void AnimationAttackStrike()
    {
        if (state != EnemyState.Attack || attackStrikeConsumed)
            return;

        attackStrikeConsumed = true;
        TryAttackPlayer();
    }

    public void AnimationAttackComplete()
    {
        if (state != EnemyState.Attack)
            return;

        StopActionSafety();
        SetState(EnemyState.Idle);
        ApplyMoveState(0);
    }

    public void AnimationHurtComplete()
    {
        if (state != EnemyState.Hurt)
            return;

        StopActionSafety();
        SetState(EnemyState.Idle);
        ScheduleNextWander();
    }

    public void AnimationDeathComplete()
    {
        StopActionSafety();
        DeathAnimationCompleted?.Invoke();
    }

    private void OnEntityReady(EntityRuntime entity)
    {
        BindEntity(entity);
        homePosition = transform.position;
    }

    private void BindEntity(EntityRuntime entity)
    {
        if (ReferenceEquals(selfEntity, entity))
            return;

        UnbindEntity();
        selfEntity = entity;
        boundStats = entity?.stats;
        healthRuntime = entity?.GetModule<HealthRuntime>();

        if (boundStats != null)
        {
            boundStats.OnChanged += OnStatChanged;
            previousHp = boundStats.Get(StatType.Hp);
            float runtimeSpeed = boundStats.Get(StatType.Speed);
            navAgent?.SetMoveSpeed(runtimeSpeed > 0f ? runtimeSpeed : moveSpeed);
        }

        if (healthRuntime != null)
            healthRuntime.OnDied += OnRuntimeDied;
    }

    private void UnbindEntity()
    {
        if (boundStats != null)
            boundStats.OnChanged -= OnStatChanged;
        if (healthRuntime != null)
            healthRuntime.OnDied -= OnRuntimeDied;

        boundStats = null;
        healthRuntime = null;
        selfEntity = null;
    }

    private void OnStatChanged(StatType statType, float value)
    {
        if (statType == StatType.Speed)
        {
            navAgent?.SetMoveSpeed(value > 0f ? value : moveSpeed);
            return;
        }

        if (statType != StatType.Hp)
            return;

        bool tookDamage = value < previousHp;
        previousHp = value;
        if (tookDamage && value > 0f)
            BeginHurt();
    }

    private void OnRuntimeDied(EntityRuntime _)
    {
        EnterDeadState();
    }

    private void BeginAttack()
    {
        SetState(EnemyState.Attack);
        AttackStarted?.Invoke();
        GameManager.Instance?.EventBus?.Publish(new EnemyAttackStartedPublish(selfEntity, transform.position));
        lastAttackTime = Time.time;
        attackStrikeConsumed = false;
        navAgent?.Stop();
        ApplyMoveState(0);

        if (HasAnimatorParameter(AttackParam, AnimatorControllerParameterType.Trigger))
        {
            animator.ResetTrigger(HurtParam);
            animator.SetTrigger(AttackParam);
            StartActionSafety(attackAnimationTimeout, AnimationAttackComplete, true);
            return;
        }

        StartActionSafety(Mathf.Max(attackWindupFallback, 0.01f), AnimationAttackComplete, true);
    }

    private void BeginHurt()
    {
        if (state == EnemyState.Dead)
            return;

        attackStrikeConsumed = true;
        SetState(EnemyState.Hurt);
        navAgent?.Stop();
        ApplyMoveState(0);

        if (HasAnimatorParameter(HurtParam, AnimatorControllerParameterType.Trigger))
        {
            animator.ResetTrigger(AttackParam);
            animator.SetTrigger(HurtParam);
        }

        StartActionSafety(hurtAnimationTimeout, AnimationHurtComplete, false);
    }

    private void EnterDeadState()
    {
        if (state == EnemyState.Dead)
            return;

        SetState(EnemyState.Dead);
        DeathStarted?.Invoke();
        attackStrikeConsumed = true;
        navAgent?.Stop();
        ApplyMoveState(0);
        StopActionSafety();

        if (body != null)
            body.velocity = Vector2.zero;
        if (movementCollider != null)
            movementCollider.enabled = false;
        if (HasAnimatorParameter(DeadParam, AnimatorControllerParameterType.Bool))
            animator.SetBool(DeadParam, true);
    }

    private void UpdatePassiveMovement()
    {
        if (Vector2.Distance(transform.position, homePosition) > leashRange)
        {
            BeginReturnHome();
            return;
        }

        if (navAgent != null && navAgent.HasDestination && !navAgent.IsAtDestination)
        {
            SetState(EnemyState.Wander);
            ApplyMoveState(1);
            return;
        }

        SetState(EnemyState.Idle);
        ApplyMoveState(0);
        if (Time.time < nextWanderTime || wanderRadius <= 0.01f)
            return;

        Vector2 target = PickWanderTarget();
        if (Vector2.Distance(transform.position, target) <= 0.05f)
        {
            ScheduleNextWander();
            return;
        }

        navAgent?.SetDestination(target);
        SetState(EnemyState.Wander);
        ApplyMoveState(1);
        ScheduleNextWander();
    }

    private void BeginReturnHome()
    {
        SetState(EnemyState.ReturnHome);
        navAgent?.SetDestination(homePosition, 0.08f);
        ApplyMoveState(2);
    }

    private void UpdateReturnHome()
    {
        if (Vector2.Distance(transform.position, homePosition) <= 0.12f)
        {
            navAgent?.Stop();
            SetState(EnemyState.Idle);
            ApplyMoveState(0);
            ScheduleNextWander();
            return;
        }

        if (navAgent != null && (!navAgent.HasDestination || navAgent.IsAtDestination))
            navAgent.SetDestination(homePosition, 0.08f);

        ApplyMoveState(2);
    }

    private Vector2 PickWanderTarget()
    {
        if (navAgent == null)
            return homePosition;

        for (int i = 0; i < 8; i++)
        {
            Vector2 candidate = homePosition + Random.insideUnitCircle * wanderRadius;
            if (navAgent.IsWalkable(candidate))
                return candidate;
        }

        return homePosition;
    }

    private void ScheduleNextWander()
    {
        float min = Mathf.Max(0.1f, wanderWaitMin);
        float max = Mathf.Max(min, wanderWaitMax);
        nextWanderTime = Time.time + Random.Range(min, max);
    }

    private void UpdateMovementAnimation()
    {
        if (navAgent == null || !navAgent.IsMoving)
            return;

        FaceTowards(navAgent.MoveDirection);
    }

    private void FaceTowards(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return;

        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            facing = direction.x >= 0f ? Vector2.right : Vector2.left;
        else
            facing = direction.y >= 0f ? Vector2.up : Vector2.down;

        ApplyFacing(facing);
    }

    private void ApplyFacing(Vector2 direction)
    {
        if (!HasAnimatorParameter(FacingParam, AnimatorControllerParameterType.Int))
            return;

        int value;
        if (direction.y < -0.5f)
            value = 0;
        else if (direction.y > 0.5f)
            value = 1;
        else if (direction.x < 0f)
            value = 2;
        else
            value = 3;

        animator.SetInteger(FacingParam, value);
    }

    private void ApplyMoveState(int value)
    {
        if (HasAnimatorParameter(MoveStateParam, AnimatorControllerParameterType.Int))
            animator.SetInteger(MoveStateParam, value);
    }

    private bool HasAttackLine(Vector2 playerPosition)
    {
        return navAgent == null || navAgent.HasLineOfSight(playerPosition);
    }

    private bool IsPlayerAlive()
    {
        var entity = playerRoot != null ? playerRoot.GetEntity() : null;
        if (entity?.stats == null)
            return player != null && player.gameObject.activeInHierarchy;

        float hp = entity.stats.Get(StatType.Hp);
        return !entity.stats.Has(StatType.Hp) || hp > 0f;
    }

    private void TryAttackPlayer()
    {
        if (player == null || selfEntity == null || !IsPlayerAlive())
            return;

        Vector2 playerPosition = player.transform.position;
        if (Vector2.Distance(transform.position, playerPosition) > attackRange || !HasAttackLine(playerPosition))
            return;

        var target = playerRoot != null ? playerRoot.GetEntity() : null;
        if (target == null)
            return;

        float attack = selfEntity.stats.Get(StatType.Attack);
        target.TriggerEvent(new TakeDamageEvent(selfEntity, attack > 0f ? attack : fallbackAttackDamage));
    }

    private void RefreshRuntimeRefs()
    {
        if (selfEntity == null && selfRoot != null)
            BindEntity(selfRoot.GetEntity());

        if (player == null)
            player = FindAnyObjectByType<PlayerControler>();

        if (playerRoot == null && player != null)
            playerRoot = player.GetComponent<EntityRoot>();
    }

    private bool HasAnimatorParameter(int hash, AnimatorControllerParameterType type)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return false;

        foreach (var parameter in animator.parameters)
        {
            if (parameter.nameHash == hash && parameter.type == type)
                return true;
        }

        return false;
    }

    private void StartActionSafety(float delay, System.Action completion, bool strikeBeforeComplete)
    {
        StopActionSafety();
        actionSafetyCoroutine = StartCoroutine(ActionSafetyRoutine(delay, completion, strikeBeforeComplete));
    }

    private IEnumerator ActionSafetyRoutine(float delay, System.Action completion, bool strikeBeforeComplete)
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, delay));
        actionSafetyCoroutine = null;
        if (strikeBeforeComplete)
            AnimationAttackStrike();
        completion?.Invoke();
    }

    private void StopActionSafety()
    {
        if (actionSafetyCoroutine == null)
            return;

        StopCoroutine(actionSafetyCoroutine);
        actionSafetyCoroutine = null;
    }

    private void SetState(EnemyState nextState)
    {
        if (state == nextState)
            return;

        state = nextState;
        StateChanged?.Invoke(state);
    }

    private void EnsureFeedbackComponents()
    {
        EnsureFeedbackComponent<HitFlashEffect>();
        EnsureFeedbackComponent<EnemyHealthBarUI>();
        EnsureFeedbackComponent<EnemyAlertIndicator>();
        EnsureFeedbackComponent<EnemyDeathEffect>();
    }

    private void EnsureFeedbackComponent<T>() where T : Component
    {
        if (GetComponent<T>() == null)
            gameObject.AddComponent<T>();
    }

    private float ResolveLegacyDestroyDelay()
    {
        float delay = Mathf.Max(0.1f, attackAnimationTimeout);
        var deathEffect = GetComponentInChildren<EnemyDeathEffect>(true);
        if (deathEffect != null)
            delay = Mathf.Max(delay, deathEffect.RequiredLifetime + 0.05f);
        return delay;
    }
}
