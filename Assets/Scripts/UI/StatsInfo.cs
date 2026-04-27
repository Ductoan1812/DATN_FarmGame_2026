using TMPro;
using UnityEngine;

public class StatsInfo : MonoBehaviour
{
    [SerializeField] private StatType stattype;
    [SerializeField] private TMP_Text valueText;

    public StatType StatType => stattype;

    public void Show(float value)
    {
        gameObject.SetActive(true);

        if (valueText != null)
            valueText.text = Format(value);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private static string Format(float value)
    {
        if (Mathf.Approximately(value, Mathf.Round(value)))
            return Mathf.RoundToInt(value).ToString();

        return value.ToString("0.##");
    }
}
