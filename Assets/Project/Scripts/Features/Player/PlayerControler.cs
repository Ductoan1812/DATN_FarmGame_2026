using UnityEngine;
using System.Collections;
using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Enums;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerControler : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private EventBus eventBus;
    [SerializeField] private Character4D character4D;
    [SerializeField] private AnimationManager _anim ;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool allowRightMouseInteract;
    [Header("Dodge")]
    [SerializeField] private KeyCode dodgeKey = KeyCode.LeftShift;
    [SerializeField] private float dodgeDistance = 1.25f;
    [SerializeField] private float dodgeDuration = 0.16f;
    [SerializeField] private float dodgeStaminaCost = 12f;
    [SerializeField] private float collisionSkin = 0.02f;

    private Vector3 lastMoveDirection = Vector3.up;
    public Vector3 LastMoveDirection => lastMoveDirection;

    public bool InputEnabled { get; set; } = true;

    private PlayerInventory _inventory;
    private EntityRoot _entityRoot;
    private ToolActionBridge _toolBridge;
    private Rigidbody2D _rigidbody2D;
    private Collider2D _movementCollider;
    private StatsRuntime _boundStats;
    private Vector2 _moveInput;
    private float _runtimeMoveSpeed;
    private bool isDodging;
    public bool IsDodging => isDodging;
    private readonly RaycastHit2D[] _movementHits = new RaycastHit2D[8];
    private readonly WaitForFixedUpdate _waitForFixedUpdate = new();
    private ContactFilter2D _movementFilter;

    private void Awake()
    {
        if (eventBus == null)
            eventBus = FindAnyObjectByType<EventBus>();

        _rigidbody2D = GetComponent<Rigidbody2D>();
        _movementCollider = GetComponent<Collider2D>();
        _entityRoot = GetComponent<EntityRoot>();
        _inventory = GetComponent<PlayerInventory>();
        _toolBridge = GetComponent<ToolActionBridge>();
        _runtimeMoveSpeed = moveSpeed;
        ConfigureMovementFilter();
    }

    private void Start()
    {
        if (character4D == null)
            character4D = GetComponentInChildren<Character4D>();

        if (character4D != null)
        {
            _anim = character4D.AnimationManager;
            character4D.SetDirection(Vector2.down);
            character4D.AnimationManager.SetState(CharacterState.Idle);
        }

        BindEntityStats(_entityRoot != null ? _entityRoot.GetEntity() : null);
    }

    private void OnEnable()
    {
        if (_entityRoot == null)
            _entityRoot = GetComponent<EntityRoot>();

        if (_entityRoot != null)
            _entityRoot.OnEntityReady += OnEntityReady;
    }

    private void OnDisable()
    {
        if (_entityRoot != null)
            _entityRoot.OnEntityReady -= OnEntityReady;

        UnbindEntityStats();
        _moveInput = Vector2.zero;
    }

    private bool IsActionBusy => _toolBridge != null && _toolBridge.IsBusy;

    private void Update()
    {
        if (!InputEnabled)
        {
            StopMovement();
            return;
        }

        // Animation đang chạy → block movement + action
        if (IsActionBusy || isDodging)
        {
            StopMovement(playIdle: !isDodging);
            return;
        }

        if (TryStartDodge())
            return;

        ReadMovementInput();
        UpdateMovementVisuals();
        HandleActions();
    }

    private void FixedUpdate()
    {
        if (!InputEnabled || IsActionBusy || isDodging)
            return;

        MoveWithCollision(_moveInput, ResolveMoveSpeed() * Time.fixedDeltaTime);
    }

    private void HandleActions()
    {
        var playerEntity = GetPlayerEntity();
        if (playerEntity == null) return;

        // ── Chuột trái: PrimaryAction ─────────────────────────────────────────
        if (Input.GetMouseButtonDown(0))
        {
            playerEntity.TriggerEvent(new PrimaryActionEvent(playerEntity));
        }

        // ── E: SecondaryAction / Interact ─────────────────────────────────────
        if (Input.GetKeyDown(GameplayInputSettings.GetInteractKey(interactKey)) || (allowRightMouseInteract && Input.GetMouseButtonDown(1)))
        {
            playerEntity.TriggerEvent(new SecondaryActionEvent(playerEntity));
        }

        // ── 1-8: Chọn hotbar slot ────────────────────────────────────────────
        if (_inventory != null)
        {
            for (int i = 0; i < 8; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    _inventory.SelectSlot(i);
                    break;
                }
            }

            // Scroll chuột: cycle hotbar
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f) _inventory.CycleHotbar(-1);
            else if (scroll < 0f) _inventory.CycleHotbar(1);
        }

        // ── F5: Save ─────────────────────────────────────────────────────────
        if (Input.GetKeyDown(KeyCode.F5))
        {
            eventBus?.Publish(new SaveGameRequestPublish());
            Debug.Log("[Player] Save requested.");
        }
    }

    private bool TryStartDodge()
    {
        if (!Input.GetKeyDown(dodgeKey)) return false;

        var playerEntity = GetPlayerEntity();
        if (playerEntity == null) return false;

        if (!StaminaHelper.TrySpend(playerEntity, dodgeStaminaCost))
        {
            Debug.Log("[Player] Không đủ thể lực để né.");
            return false;
        }

        Vector2 direction = ReadInputDirection();
        if (direction.sqrMagnitude <= 0.001f)
            direction = lastMoveDirection.sqrMagnitude > 0.001f ? (Vector2)lastMoveDirection : Vector2.down;

        StartCoroutine(DodgeRoutine(direction.normalized));
        return true;
    }

    private IEnumerator DodgeRoutine(Vector2 direction)
    {
        isDodging = true;
        _moveInput = Vector2.zero;

        float elapsed = 0f;
        float dodgeSpeed = dodgeDuration <= 0f ? dodgeDistance : dodgeDistance / dodgeDuration;

        while (elapsed < dodgeDuration)
        {
            MoveWithCollision(direction, dodgeSpeed * Time.fixedDeltaTime);
            elapsed += Time.fixedDeltaTime;
            yield return _waitForFixedUpdate;
        }

        isDodging = false;
    }

    private Vector2 ReadInputDirection()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        return new Vector2(horizontal, vertical).normalized;
    }

    private EntityRuntime GetPlayerEntity()
    {
        if (_entityRoot == null)
            _entityRoot = GetComponent<EntityRoot>();

        if (_entityRoot == null)
            return null;

        return _entityRoot.GetEntity();
    }

    private void OnEntityReady(EntityRuntime entity)
    {
        BindEntityStats(entity);
    }

    private void BindEntityStats(EntityRuntime entity)
    {
        var stats = entity?.stats;
        if (ReferenceEquals(_boundStats, stats))
        {
            RefreshRuntimeMoveSpeed();
            return;
        }

        UnbindEntityStats();

        _boundStats = stats;
        if (_boundStats != null)
            _boundStats.OnChanged += OnStatsChanged;

        RefreshRuntimeMoveSpeed();
    }

    private void UnbindEntityStats()
    {
        if (_boundStats != null)
            _boundStats.OnChanged -= OnStatsChanged;

        _boundStats = null;
        _runtimeMoveSpeed = moveSpeed;
    }

    private void OnStatsChanged(StatType statType, float newValue)
    {
        if (statType != StatType.Speed)
            return;

        _runtimeMoveSpeed = newValue > 0f ? newValue : moveSpeed;
    }

    private void RefreshRuntimeMoveSpeed()
    {
        _runtimeMoveSpeed = moveSpeed;

        if (_boundStats != null && _boundStats.Has(StatType.Speed))
        {
            float runtimeSpeed = _boundStats.Get(StatType.Speed);
            if (runtimeSpeed > 0f)
                _runtimeMoveSpeed = runtimeSpeed;
        }
    }

    private float ResolveMoveSpeed()
    {
        return _runtimeMoveSpeed > 0f ? _runtimeMoveSpeed : moveSpeed;
    }

    private void ReadMovementInput()
    {
        _moveInput = ReadInputDirection();
        if (_moveInput.sqrMagnitude > 0.0001f)
            lastMoveDirection = new Vector3(_moveInput.x, _moveInput.y, 0f);
    }

    private void UpdateMovementVisuals()
    {
        if (_moveInput.sqrMagnitude > 0.0001f)
        {
            float horizontal = _moveInput.x;
            float vertical = _moveInput.y;

            Vector2 dir;
            if (Mathf.Abs(horizontal) >= Mathf.Abs(vertical))
                dir = horizontal > 0 ? Vector2.right : Vector2.left;
            else
                dir = vertical > 0 ? Vector2.up : Vector2.down;

            if (character4D != null)
            {
                character4D.SetDirection(dir);
                character4D.AnimationManager.SetState(CharacterState.Run);
            }
        }
        else if (character4D != null)
        {
            character4D.AnimationManager.SetState(CharacterState.Idle);
        }
    }

    private void StopMovement(bool playIdle = true)
    {
        _moveInput = Vector2.zero;

        if (playIdle && character4D != null)
            character4D.AnimationManager.SetState(CharacterState.Idle);
    }

    private void ConfigureMovementFilter()
    {
        _movementFilter.useTriggers = false;
        _movementFilter.useLayerMask = true;
        _movementFilter.useNormalAngle = false;
        _movementFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
    }

    private void MoveWithCollision(Vector2 direction, float distance)
    {
        if (_rigidbody2D == null || direction.sqrMagnitude <= 0.0001f || distance <= 0f)
            return;

        Vector2 normalized = direction.normalized;
        float allowedDistance = distance;

        if (_movementCollider != null)
        {
            int hitCount = _movementCollider.Cast(normalized, _movementFilter, _movementHits, distance + collisionSkin);
            for (int i = 0; i < hitCount; i++)
            {
                var hit = _movementHits[i];
                if (hit.collider == null)
                    continue;

                if (hit.collider.attachedRigidbody == _rigidbody2D)
                    continue;

                allowedDistance = Mathf.Min(allowedDistance, Mathf.Max(0f, hit.distance - collisionSkin));
            }
        }

        if (allowedDistance <= 0f)
            return;

        _rigidbody2D.MovePosition(_rigidbody2D.position + normalized * allowedDistance);
    }
}
