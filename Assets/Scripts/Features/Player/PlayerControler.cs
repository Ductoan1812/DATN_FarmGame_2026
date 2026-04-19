using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControler : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private EventBus eventBus;

    private Vector3 lastMoveDirection = Vector3.up;
    public Vector3 LastMoveDirection => lastMoveDirection;

    private PlayerInventory _inventory;
    private EntityRoot _entityRoot;

    private static readonly Key[] HotbarKeys = new Key[]
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
        Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8
    };

    private void Awake()
    {
        if (eventBus == null)
            eventBus = FindAnyObjectByType<EventBus>();
    }

    private void Start()
    {
        _inventory  = GetComponent<PlayerInventory>();
        _entityRoot = GetComponent<EntityRoot>();
    }

    private void Update()
    {
        HandleMovement();
        HandleActions();
    }

    private void HandleMovement()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)  horizontal -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)  vertical   -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)    vertical   += 1f;

        Vector3 moveDirection = new Vector3(horizontal, vertical, 0f).normalized;
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);

        if (moveDirection.sqrMagnitude > 0.0001f)
            lastMoveDirection = moveDirection;
    }

    private void HandleActions()
    {
        var mouse    = Mouse.current;
        var keyboard = Keyboard.current;

        // Chuột trái: dùng item đang cầm
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && _inventory != null)
        {
            var selected = _inventory.SelectedItem;
            if (selected != null)
                selected.TriggerEvent(new UseEvent(selected));
        }

        // Chuột phải: tấn công
        if (mouse != null && mouse.rightButton.wasPressedThisFrame)
        {
            var playerEntity = _entityRoot?.GetEntity();
            if (playerEntity != null)
                playerEntity.TriggerEvent(new AttackEvent(playerEntity));
        }

        // 1-8: Chọn hotbar slot
        if (keyboard != null && _inventory != null)
        {
            for (int i = 0; i < HotbarKeys.Length; i++)
            {
                if (keyboard[HotbarKeys[i]].wasPressedThisFrame)
                {
                    _inventory.SelectSlot(i);
                    break;
                }
            }

            // Scroll chuột: cycle hotbar
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll > 0f) _inventory.CycleHotbar(-1);
                else if (scroll < 0f) _inventory.CycleHotbar(1);
            }
        }

        // F5: Save
        if (keyboard != null && keyboard.f5Key.wasPressedThisFrame)
        {
            eventBus?.Publish(new SaveGameRequest());
            Debug.Log("[Player] Save requested.");
        }
    }
}
