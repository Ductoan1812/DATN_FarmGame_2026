using TMPro;
using UnityEngine;

/// <summary>
/// Hiển thị tên EntityData phía trên world entity như NPC/enemy.
/// Component này chỉ là visual bridge: lấy keyName từ EntityRuntime và render qua localization.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EntityRoot))]
public class WorldEntityNameplate : MonoBehaviour
{
    [SerializeField] private EntityRoot entityRoot;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Vector3 localOffset = new(0f, 2.05f, 0f);
    [SerializeField] private float localScale = 0.16f;
    [SerializeField] private float fontSize = 32f;
    [SerializeField] private Color textColor = new(1f, 0.90f, 0.45f, 1f);
    [SerializeField] private Color highlightedTextColor = new(1f, 1f, 0.85f, 1f);
    [SerializeField] private string sortingLayerName = "Effect";
    [SerializeField] private int sortingOrder = 120;

    private string currentNameKey;
    private string fallbackName;
    private bool isHighlighted;

    private void Reset()
    {
        entityRoot = GetComponent<EntityRoot>();
    }

    private void Awake()
    {
        if (entityRoot == null)
            entityRoot = GetComponent<EntityRoot>();

        EnsureText();
    }

    private void OnEnable()
    {
        if (entityRoot != null)
            entityRoot.OnEntityReady += OnEntityReady;

        LocalizationManager.LocalizationReady += RefreshText;
        LocalizationManager.LanguageChanged += RefreshText;

        if (entityRoot != null && entityRoot.IsReady)
            Bind(entityRoot.GetEntity());
        else
            RefreshText();
    }

    private void OnDisable()
    {
        if (entityRoot != null)
            entityRoot.OnEntityReady -= OnEntityReady;

        LocalizationManager.LocalizationReady -= RefreshText;
        LocalizationManager.LanguageChanged -= RefreshText;
    }

    private void LateUpdate()
    {
        if (nameText == null) return;

        if (string.IsNullOrWhiteSpace(currentNameKey) && entityRoot != null && entityRoot.IsReady)
            Bind(entityRoot.GetEntity());

        nameText.transform.localPosition = localOffset;
        nameText.transform.localRotation = Quaternion.identity;
    }

    private void OnEntityReady(EntityRuntime entity)
    {
        Bind(entity);
    }

    private void Bind(EntityRuntime entity)
    {
        currentNameKey = entity?.entityData?.keyName;
        fallbackName = entity?.entityData?.id;
        RefreshText();
    }

    private void RefreshText()
    {
        EnsureText();
        if (nameText == null) return;

        if (string.IsNullOrWhiteSpace(currentNameKey) && string.IsNullOrWhiteSpace(fallbackName))
        {
            nameText.text = string.Empty;
            nameText.gameObject.SetActive(false);
            return;
        }

        nameText.gameObject.SetActive(true);
        nameText.color = isHighlighted ? highlightedTextColor : textColor;
        nameText.text = !string.IsNullOrWhiteSpace(currentNameKey) && LocalizationManager.Instance != null
            ? LocalizationManager.Instance.GetText(currentNameKey)
            : (!string.IsNullOrWhiteSpace(currentNameKey) ? currentNameKey : fallbackName);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (isHighlighted == highlighted) return;
        isHighlighted = highlighted;
        RefreshText();
    }

    private void EnsureText()
    {
        if (nameText == null)
        {
            var existing = transform.Find("Nameplate");
            if (existing != null)
                nameText = existing.GetComponent<TMP_Text>();
        }

        if (nameText == null)
        {
            var textObject = new GameObject("Nameplate");
            textObject.transform.SetParent(transform, false);
            nameText = textObject.AddComponent<TextMeshPro>();
        }

        var textTransform = nameText.transform;
        textTransform.localPosition = localOffset;
        textTransform.localRotation = Quaternion.identity;
        textTransform.localScale = Vector3.one * Mathf.Max(0.01f, localScale);

        nameText.alignment = TextAlignmentOptions.Center;
        nameText.enableWordWrapping = false;
        nameText.fontSize = fontSize;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = textColor;
        nameText.raycastTarget = false;
        nameText.outlineColor = Color.black;
        nameText.outlineWidth = 0.18f;

        var rectTransform = nameText.rectTransform;
        if (rectTransform != null)
            rectTransform.sizeDelta = new Vector2(10f, 1.2f);

        var renderer = nameText.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder;
        }
    }
}
