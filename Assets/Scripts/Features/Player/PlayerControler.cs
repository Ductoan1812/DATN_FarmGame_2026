using UnityEngine;

public class PlayerControler : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private EventBus eventBus;

    private Vector3 lastMoveDirection = Vector3.up;
    public Vector3 LastMoveDirection => lastMoveDirection;

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
    }

    private void Update()
    {
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
            lastMoveDirection = moveDirection;
    }

    private void HandleActions()
    {
        // Chuột trái: dùng item đang cầm
        if (Input.GetMouseButtonDown(0) && _inventory != null)
        {
            var selected = _inventory.SelectedItem;
            if (selected != null)
                selected.TriggerEvent(new UseEvent(selected));
        }

        // Chuột phải: tấn công
        if (Input.GetMouseButtonDown(1))
        {
            var playerEntity = _entityRoot?.GetEntity();
            if (playerEntity != null)
                playerEntity.TriggerEvent(new AttackEvent(playerEntity));
        }

        // 1-8: Chọn hotbar slot
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

        // F5: Save
        if (Input.GetKeyDown(KeyCode.F5))
        {
            eventBus?.Publish(new SaveGameRequest());
            Debug.Log("[Player] Save requested.");
        }
    }
}
