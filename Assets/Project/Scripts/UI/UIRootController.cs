using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIRootController : MonoBehaviour
{
    public static UIRootController Instance { get; private set; }

    [Header("UI Entries")]
    [SerializeField] private List<UIEntry> entries = new();

    [Header("Canvas Roots")]
    [SerializeField] private List<CanvasRef> canvasRefs = new();

    private readonly Dictionary<string, UIEntry> entriesById = new();
    private bool playerInputLockedByUi;

    public IReadOnlyList<UIEntry> Entries => entries;
    public event Action<string> EntryOpened;
    public event Action<string> EntryClosed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        AutoAssignCanvases();
        BuildEntryLookup();
        InitializeEntries();
        RefreshPlayerInputLock();
    }

    private void Update()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null || entry.ToggleKey == KeyCode.None) continue;
            if (Input.GetKeyDown(entry.ToggleKey))
                Toggle(entry.Id);
        }
    }

    private void OnDestroy()
    {
        UnbindEntryButtons();

        if (Instance == this)
            Instance = null;
    }

    public bool TryGetEntry(string id, out UIEntry entry)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            entry = null;
            return false;
        }

        return entriesById.TryGetValue(id, out entry);
    }

    public void Show(string id) => Open(id);

    public void Hide(string id) => Close(id);

    public void Open(string id)
    {
        if (!TryGetEntry(id, out var entry)) return;
        Open(entry);
    }

    public void Close(string id)
    {
        if (!TryGetEntry(id, out var entry)) return;
        Close(entry);
    }

    public void Toggle(string id)
    {
        if (!TryGetEntry(id, out var entry)) return;

        if (entry.IsOpen)
            Close(entry);
        else
            Open(entry);
    }

    public void CloseAllModals()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null || !entry.Modal || !entry.IsOpen) continue;
            Close(entry);
        }
    }

    public void NotifyWindowStateChanged()
    {
        RefreshPlayerInputLock();
    }

    public Canvas GetCanvas(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        for (int i = 0; i < canvasRefs.Count; i++)
        {
            var item = canvasRefs[i];
            if (item != null && item.Id == id)
                return item.Canvas;
        }

        return null;
    }

    private void Open(UIEntry entry)
    {
        if (entry == null || entry.Target == null) return;

        if (entry.Modal)
            CloseOtherModals(entry);

        entry.ApplyOpen();
        EntryOpened?.Invoke(entry.Id);
        RefreshPlayerInputLock();
    }

    private void Close(UIEntry entry)
    {
        if (entry == null || entry.Target == null) return;

        entry.ApplyClose();
        EntryClosed?.Invoke(entry.Id);
        RefreshPlayerInputLock();
    }

    private void CloseOtherModals(UIEntry activeEntry)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null || entry == activeEntry) continue;
            if (!entry.Modal || !entry.IsOpen) continue;
            Close(entry);
        }
    }

    private void InitializeEntries()
    {
        UnbindEntryButtons();

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null) continue;

            entry.BindButtons(this);
            if (entry.HideOnAwake)
                entry.ApplyClose();
            else
                entry.RefreshStateObjects();
        }
    }

    private void BuildEntryLookup()
    {
        entriesById.Clear();

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.Id)) continue;
            entriesById[entry.Id] = entry;
        }
    }

    private void UnbindEntryButtons()
    {
        for (int i = 0; i < entries.Count; i++)
            entries[i]?.UnbindButtons();
    }

    private void RefreshPlayerInputLock()
    {
        bool shouldLockInput = false;

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null || !entry.LockPlayerInput || !entry.IsOpen) continue;
            shouldLockInput = true;
            break;
        }

        if (playerInputLockedByUi == shouldLockInput) return;
        playerInputLockedByUi = shouldLockInput;

        var player = FindAnyObjectByType<PlayerControler>();
        if (player != null)
            player.InputEnabled = !playerInputLockedByUi;
    }

    private void AutoAssignCanvases()
    {
        AddCanvasIfMissing("hud", "Canvas_HUD");
        AddCanvasIfMissing("windows", "Canvas_Windows");
        AddCanvasIfMissing("overlay", "Canvas_Overlay");
        AddCanvasIfMissing("debug", "Canvas_Debug");
    }

    private void AddCanvasIfMissing(string id, string objectName)
    {
        for (int i = 0; i < canvasRefs.Count; i++)
        {
            if (canvasRefs[i] != null && canvasRefs[i].Id == id)
                return;
        }

        var canvas = FindCanvas(objectName);
        if (canvas != null)
            canvasRefs.Add(new CanvasRef(id, canvas));
    }

    private Canvas FindCanvas(string canvasName)
    {
        var child = transform.Find(canvasName);
        if (child != null)
            return child.GetComponent<Canvas>();

        var go = GameObject.Find(canvasName);
        return go != null ? go.GetComponent<Canvas>() : null;
    }
}

[Serializable]
public class UIEntry
{
    [SerializeField] private string id;
    [SerializeField] private GameObject target;
    [SerializeField] private bool hideOnAwake = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.None;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private bool modal = true;
    [SerializeField] private bool lockPlayerInput = true;
    [SerializeField] private List<GameObject> showWhenOpen = new();
    [SerializeField] private List<GameObject> hideWhenOpen = new();
    [SerializeField] private List<GameObject> showWhenClosed = new();
    [SerializeField] private List<GameObject> hideWhenClosed = new();

    private UIRootController owner;

    public string Id => id;
    public GameObject Target => target;
    public bool HideOnAwake => hideOnAwake;
    public KeyCode ToggleKey => toggleKey;
    public bool Modal => modal;
    public bool LockPlayerInput => lockPlayerInput;
    public bool IsOpen => target != null && target.activeSelf;

    public void Configure(
        string entryId,
        GameObject targetObject,
        bool hideInitially,
        KeyCode key,
        Button open,
        Button close,
        bool isModal,
        bool locksInput,
        IReadOnlyList<GameObject> showOpen = null,
        IReadOnlyList<GameObject> hideOpen = null,
        IReadOnlyList<GameObject> showClosed = null,
        IReadOnlyList<GameObject> hideClosed = null)
    {
        id = entryId;
        target = targetObject;
        hideOnAwake = hideInitially;
        toggleKey = key;
        openButton = open;
        closeButton = close;
        modal = isModal;
        lockPlayerInput = locksInput;
        CopyList(showWhenOpen, showOpen);
        CopyList(hideWhenOpen, hideOpen);
        CopyList(showWhenClosed, showClosed);
        CopyList(hideWhenClosed, hideClosed);
    }

    public void BindButtons(UIRootController controller)
    {
        owner = controller;

        if (openButton != null)
            openButton.onClick.AddListener(OpenFromButton);
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseFromButton);
    }

    public void UnbindButtons()
    {
        if (openButton != null)
            openButton.onClick.RemoveListener(OpenFromButton);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseFromButton);
    }

    public void ApplyOpen()
    {
        if (target != null) target.SetActive(true);
        SetActive(showWhenOpen, true);
        SetActive(hideWhenOpen, false);
    }

    public void ApplyClose()
    {
        if (target != null) target.SetActive(false);
        SetActive(showWhenClosed, true);
        SetActive(hideWhenClosed, false);
    }

    public void RefreshStateObjects()
    {
        if (IsOpen)
        {
            SetActive(showWhenOpen, true);
            SetActive(hideWhenOpen, false);
        }
        else
        {
            SetActive(showWhenClosed, true);
            SetActive(hideWhenClosed, false);
        }
    }

    private void OpenFromButton()
    {
        owner?.Open(id);
    }

    private void CloseFromButton()
    {
        owner?.Close(id);
    }

    private static void SetActive(List<GameObject> objects, bool active)
    {
        if (objects == null) return;

        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i] != null)
                objects[i].SetActive(active);
        }
    }

    private static void CopyList(List<GameObject> targetList, IReadOnlyList<GameObject> source)
    {
        targetList.Clear();
        if (source == null) return;

        for (int i = 0; i < source.Count; i++)
        {
            if (source[i] != null)
                targetList.Add(source[i]);
        }
    }
}

[Serializable]
public class CanvasRef
{
    [SerializeField] private string id;
    [SerializeField] private Canvas canvas;

    public string Id => id;
    public Canvas Canvas => canvas;

    public CanvasRef(string canvasId, Canvas targetCanvas)
    {
        id = canvasId;
        canvas = targetCanvas;
    }
}
