using UnityEngine;

/// <summary>
/// Quản lý toggle các panel UI.
/// </summary>
public class HUDManager : MonoBehaviour
{
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private KeyCode inventoryToggleKey = KeyCode.I;

    private void Update()
    {
        if (Input.GetKeyDown(inventoryToggleKey) && inventoryUI != null)
            inventoryUI.Toggle();
    }
}
