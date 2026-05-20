using UnityEngine;
using System.Collections;
using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Enums;

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

    private Vector3 lastMoveDirection = Vector3.up;
    public Vector3 LastMoveDirection => lastMoveDirection;

    public bool InputEnabled { get; set; } = true;

    private PlayerInventory _inventory;
    private EntityRoot _entityRoot;
    private ToolActionBridge _toolBridge;
    private bool isDodging;

    private void Awake()
    {
        if (eventBus == null)
            eventBus = FindAnyObjectByType<EventBus>();
    }

    private void Start()
    {
        _inventory   = GetComponent<PlayerInventory>();
        _entityRoot  = GetComponent<EntityRoot>();
        _toolBridge  = GetComponent<ToolActionBridge>();
        _anim = character4D.AnimationManager;
        character4D.SetDirection(Vector2.down);
        character4D.AnimationManager.SetState(CharacterState.Idle);
    }

    private bool IsActionBusy => _toolBridge != null && _toolBridge.IsBusy;

    private void Update()
    {
        if (!InputEnabled) return;

        // Animation đang chạy → block movement + action
        if (IsActionBusy || isDodging) return;

        if (TryStartDodge())
            return;

        HandleMovement();
        HandleActions();
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 moveDirection = new Vector3(horizontal, vertical, 0f).normalized;
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            lastMoveDirection = moveDirection;

            // Cập nhật hướng nhân vật
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
        else
        {
            if (character4D != null)
            {
                character4D.AnimationManager.SetState(CharacterState.Idle);
            }
        }
    }

    private void HandleActions()
    {
        var playerEntity = _entityRoot?.GetEntity();
        if (playerEntity == null) return;

        // ── Chuột trái: PrimaryAction ─────────────────────────────────────────
        if (Input.GetMouseButtonDown(0))
        {
            playerEntity.TriggerEvent(new PrimaryActionEvent(playerEntity));
        }

        // ── E: SecondaryAction / Interact ─────────────────────────────────────
        if (Input.GetKeyDown(interactKey) || (allowRightMouseInteract && Input.GetMouseButtonDown(1)))
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

        var playerEntity = _entityRoot?.GetEntity();
        if (playerEntity == null) return false;

        if (!TrySpendStamina(playerEntity, dodgeStaminaCost))
        {
            Debug.Log("[Player] Không đủ thể lực để né.");
            return false;
        }

        Vector3 direction = ReadInputDirection();
        if (direction.sqrMagnitude <= 0.001f)
            direction = lastMoveDirection.sqrMagnitude > 0.001f ? lastMoveDirection : Vector3.down;

        StartCoroutine(DodgeRoutine(direction.normalized));
        return true;
    }

    private IEnumerator DodgeRoutine(Vector3 direction)
    {
        isDodging = true;

        float elapsed = 0f;
        Vector3 start = transform.position;
        Vector3 end = start + direction * dodgeDistance;

        while (elapsed < dodgeDuration)
        {
            float t = dodgeDuration <= 0f ? 1f : elapsed / dodgeDuration;
            transform.position = Vector3.Lerp(start, end, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = end;
        isDodging = false;
    }

    private Vector3 ReadInputDirection()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        return new Vector3(horizontal, vertical, 0f).normalized;
    }

    private static bool TrySpendStamina(EntityRuntime entity, float cost)
    {
        if (entity?.stats == null || cost <= 0f) return true;

        float maxStamina = entity.stats.Get(StatType.MaxStamina);
        if (maxStamina <= 0f) return true;

        float stamina = entity.stats.Get(StatType.Stamina);
        if (stamina < cost) return false;

        entity.stats.Set(StatType.Stamina, Mathf.Max(0f, stamina - cost));
        return true;
    }
}
