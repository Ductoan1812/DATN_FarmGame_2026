using UnityEngine;
using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Enums;

public class PlayerControler : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private EventBus eventBus;
    [SerializeField] private Character4D character4D;
    [SerializeField] private AnimationManager _anim ;

    private Vector3 lastMoveDirection = Vector3.up;
    public Vector3 LastMoveDirection => lastMoveDirection;

    /// <summary>Bật/tắt nhận input. False = bấm gì cũng không nhận (DebugConsole, cutscene...).</summary>
    public bool InputEnabled { get; set; } = true;

    private PlayerInventory _inventory;
    private EntityRoot _entityRoot;

    private void Awake()
    {
        if (eventBus == null)
            eventBus = FindAnyObjectByType<EventBus>();
    }

    private void Start()
    {
        _inventory  = GetComponent<PlayerInventory>();
        _entityRoot = GetComponent<EntityRoot>();
        _anim = character4D.AnimationManager;
    }

    private void Update()
    {
        if (!InputEnabled) return;

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

        // ── Chuột phải: SecondaryAction ───────────────────────────────────────
        if (Input.GetMouseButtonDown(1))
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
        if (Input.GetKeyDown(KeyCode.F9))
        {
            _anim.Animator.SetTrigger("Hoe");
            _anim.IsAction = true;
        }
    }
}
