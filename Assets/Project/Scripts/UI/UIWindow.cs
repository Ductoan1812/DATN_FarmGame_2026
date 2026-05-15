using System;
using UnityEngine;
using UnityEngine.UI;

public class UIWindow : MonoBehaviour
{
    [SerializeField] private string windowId;
    [SerializeField] private GameObject panel;
    [SerializeField] private Button closeButton;
    [SerializeField] private bool hideOnAwake = true;
    [SerializeField] private bool lockPlayerInput;
    [SerializeField] private bool modal;

    private bool closeListenerRegistered;

    public string WindowId => string.IsNullOrWhiteSpace(windowId) ? gameObject.name : windowId;
    public bool IsOpen => Target.activeSelf;
    public bool LockPlayerInput => lockPlayerInput;
    public bool Modal => modal;

    public event Action<UIWindow> Shown;
    public event Action<UIWindow> Hidden;

    private GameObject Target => panel != null ? panel : gameObject;

    private void Awake()
    {
        RegisterCloseButton();
        if (hideOnAwake) Hide();
    }

    private void OnEnable()
    {
        RegisterCloseButton();
    }

    private void OnDisable()
    {
        if (closeButton != null && closeListenerRegistered)
        {
            closeButton.onClick.RemoveListener(Hide);
            closeListenerRegistered = false;
        }
    }

    public void Configure(string id, GameObject targetPanel, Button close = null, bool inputLock = false, bool isModal = false, bool hideInitially = true)
    {
        windowId = id;
        panel = targetPanel;
        closeButton = close;
        lockPlayerInput = inputLock;
        modal = isModal;
        hideOnAwake = hideInitially;
        RegisterCloseButton();
    }

    public void Show()
    {
        Target.SetActive(true);
        Shown?.Invoke(this);
        UIRootController.Instance?.NotifyWindowStateChanged();
    }

    public void Hide()
    {
        Target.SetActive(false);
        Hidden?.Invoke(this);
        UIRootController.Instance?.NotifyWindowStateChanged();
    }

    public void Toggle()
    {
        if (IsOpen) Hide();
        else Show();
    }

    private void RegisterCloseButton()
    {
        if (closeButton == null || closeListenerRegistered) return;

        closeButton.onClick.AddListener(Hide);
        closeListenerRegistered = true;
    }
}
