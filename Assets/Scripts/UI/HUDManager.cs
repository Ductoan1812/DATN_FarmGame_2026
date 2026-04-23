using UnityEngine;

/// <summary>
/// Quản lý toggle các panel UI.
/// </summary>
public class HUDManager : MonoBehaviour
{
    [SerializeField] private GameObject menuWindow;
    [SerializeField] private KeyCode menuToggleKey = KeyCode.I;

    private bool _isOpen;

    private void Update()
    {
        if (Input.GetKeyDown(menuToggleKey) && menuWindow != null)
        {
            _isOpen = !_isOpen;
            menuWindow.SetActive(_isOpen);
        }
    }
}
