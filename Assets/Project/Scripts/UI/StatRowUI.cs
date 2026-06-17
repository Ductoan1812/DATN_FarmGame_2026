using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatRowUI : MonoBehaviour
{
    private static readonly Color ReadableStatTextColor = new(0.08f, 0.05f, 0.02f, 1f);

    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text valueText;

    private string _nameKey;
    private string _rawName;

    private void Reset()
    {
        AutoBindReferences();
    }

    private void OnEnable()
    {
        LocalizationManager.LocalizationReady += RefreshName;
        LocalizationManager.LanguageChanged += RefreshName;
        RefreshName();
    }

    private void OnDisable()
    {
        LocalizationManager.LocalizationReady -= RefreshName;
        LocalizationManager.LanguageChanged -= RefreshName;
    }

    public void Setup(StatDefinition definition, float value)
    {
        AutoBindReferences();
        ApplyReadableTextStyle();

        if (definition == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        SetIcon(definition.Icon);
        SetName(definition.NameKey);
        SetValue(value, definition.ValueFormat);
    }

    public void SetupText(StatDefinition definition, string value)
    {
        AutoBindReferences();
        ApplyReadableTextStyle();

        if (definition == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        SetIcon(definition.Icon);
        SetName(definition.NameKey);
        SetValueText(value);
    }

    public void SetupRaw(string name, float value, Sprite icon = null, string valueFormat = null)
    {
        AutoBindReferences();
        ApplyReadableTextStyle();

        gameObject.SetActive(true);
        SetIcon(icon);
        _nameKey = string.Empty;
        _rawName = name;

        if (nameText != null)
            nameText.text = name;

        SetValue(value, valueFormat);
    }

    public void SetupRawText(string name, string value, Sprite icon = null)
    {
        AutoBindReferences();
        ApplyReadableTextStyle();

        gameObject.SetActive(true);
        SetIcon(icon);
        _nameKey = string.Empty;
        _rawName = name;

        if (nameText != null)
            nameText.text = name;

        SetValueText(value);
    }

    private void AutoBindReferences()
    {
        if (iconImage == null)
        {
            var icon = transform.Find("Icon") ?? transform.Find("icon");
            if (icon != null) iconImage = icon.GetComponent<Image>();
        }

        if (nameText == null)
        {
            var name = transform.Find("Name") ?? transform.Find("name") ?? transform.Find("NameText");
            if (name != null) nameText = name.GetComponent<TMP_Text>();
        }

        if (valueText == null)
        {
            var value = transform.Find("Value") ?? transform.Find("value") ?? transform.Find("ValueText");
            if (value != null) valueText = value.GetComponent<TMP_Text>();
        }

        if (nameText == null || valueText == null)
        {
            var texts = GetComponentsInChildren<TMP_Text>(true);
            if (nameText == null && texts.Length > 0) nameText = texts[0];
            if (valueText == null && texts.Length > 1) valueText = texts[1];
        }

        ApplyReadableTextStyle();
    }

    private void ApplyReadableTextStyle()
    {
        UiTextStyleUtility.ApplyRobotoAndColor(nameText, ReadableStatTextColor);
        UiTextStyleUtility.ApplyRobotoAndColor(valueText, ReadableStatTextColor);
    }

    private void SetIcon(Sprite icon)
    {
        if (iconImage == null) return;

        iconImage.sprite = icon;
        iconImage.color = icon != null ? Color.white : new Color(1f, 1f, 1f, 0f);
    }

    private void SetName(string key)
    {
        _nameKey = key;
        _rawName = string.Empty;
        RefreshName();
    }

    private void RefreshName()
    {
        if (nameText == null)
            return;

        if (string.IsNullOrWhiteSpace(_nameKey))
        {
            nameText.text = _rawName ?? string.Empty;
            return;
        }

        nameText.text = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.GetText(_nameKey)
            : _nameKey;
    }

    private void SetValue(float value, string format)
    {
        if (valueText == null) return;

        var number = FormatNumber(value);
        valueText.text = string.IsNullOrEmpty(format) ? number : string.Format(format, number);
    }

    private void SetValueText(string value)
    {
        if (valueText == null) return;
        valueText.text = value ?? string.Empty;
    }

    private static string FormatNumber(float value)
    {
        if (Mathf.Approximately(value, Mathf.Round(value)))
            return Mathf.RoundToInt(value).ToString();

        return value.ToString("0.##");
    }
}
