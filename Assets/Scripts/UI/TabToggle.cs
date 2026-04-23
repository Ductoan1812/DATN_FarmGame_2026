using UnityEngine;
using UnityEngine.UI;
using System;

public class TabToggle : MonoBehaviour
{
    [Header("Tab Settings")]
    [SerializeField] private TabType tabType;
    [SerializeField] private Toggle toggle;
    [SerializeField] private RectTransform toggleTransform;
    [SerializeField] private float activeOffsetY = -10f;

    [Header("Visual Settings")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private bool enableVisualEffects = true;

    private Vector3 originalPosition;
    private bool isInitialized;
    private Image[] cachedImages;
    private bool currentState;
    private Color lastAppliedColor;
    private Vector3 lastAppliedPosition;

    public TabType TabType => tabType;
    public bool IsOn => toggle != null && toggle.isOn;

    public event Action<TabType> OnTabSelected;
    public event Action<TabType> OnTabDeselected;

    private void Awake() => Initialize();

    private void Start()
    {
        if (!isInitialized) Initialize();
    }

    private void Initialize()
    {
        toggle ??= GetComponent<Toggle>();
        toggleTransform ??= GetComponent<RectTransform>();
        if (toggle == null || toggleTransform == null) return;

        originalPosition = toggleTransform.anchoredPosition;
        cachedImages = GetComponentsInChildren<Image>(true);
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
        currentState = toggle.isOn;
        UpdateVisualState(toggle.isOn);
        isInitialized = true;
    }

    private void OnDestroy()
    {
        if (toggle != null) toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
    }

    private void OnToggleValueChanged(bool isOn)
    {
        if (isOn && currentState) return;
        if (!isOn && currentState && !HasAnyOtherTabActive())
        {
            toggle.isOn = true;
            return;
        }
        if (currentState == isOn) return;
        currentState = isOn;
        if (enableVisualEffects) UpdateVisualState(isOn);
        if (isOn) OnTabSelected?.Invoke(tabType);
        else OnTabDeselected?.Invoke(tabType);
    }

    private void UpdateVisualState(bool isOn)
    {
        if (!enableVisualEffects || toggleTransform == null) return;

        Vector3 targetPos = originalPosition;
        if (isOn) targetPos.y += activeOffsetY;
        if (lastAppliedPosition != targetPos)
        {
            toggleTransform.anchoredPosition = targetPos;
            lastAppliedPosition = targetPos;
        }

        Color targetColor = isOn ? activeColor : inactiveColor;
        if (cachedImages != null && lastAppliedColor != targetColor)
        {
            foreach (var img in cachedImages)
                if (img != null) img.color = targetColor;
            lastAppliedColor = targetColor;
        }
    }

    public void SetTabState(bool isOn, bool triggerEvent = false)
    {
        if (toggle == null || currentState == isOn) return;
        if (triggerEvent)
        {
            toggle.isOn = isOn;
        }
        else
        {
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            toggle.isOn = isOn;
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
            currentState = isOn;
            UpdateVisualState(isOn);
        }
    }

    private bool HasAnyOtherTabActive()
    {
        var all = FindObjectsOfType<TabToggle>();
        foreach (var t in all)
            if (t != this && t.IsOn) return true;
        return false;
    }
}

public enum TabType
{
    Inventory,
    InfoPlayer,
    Skills,
    Quest,
    Map,
    Crafting,
    Achievements,
    Social,
    Pet,
    Settings
}
