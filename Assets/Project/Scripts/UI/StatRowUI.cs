using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatRowUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text valueText;

    private string _nameKey;

    private void Reset()
    {
        if (nameText == null)
            nameText = GetComponentInChildren<TMP_Text>();
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

    private void SetIcon(Sprite icon)
    {
        if (iconImage == null) return;

        iconImage.sprite = icon;
        iconImage.color = icon != null ? Color.white : new Color(1f, 1f, 1f, 0f);
    }

    private void SetName(string key)
    {
        _nameKey = key;
        RefreshName();
    }

    private void RefreshName()
    {
        if (nameText == null)
            return;

        if (string.IsNullOrWhiteSpace(_nameKey))
        {
            nameText.text = string.Empty;
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

    private static string FormatNumber(float value)
    {
        if (Mathf.Approximately(value, Mathf.Round(value)))
            return Mathf.RoundToInt(value).ToString();

        return value.ToString("0.##");
    }
}
