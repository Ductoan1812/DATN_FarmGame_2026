using UnityEngine;

public class StatListUI : MonoBehaviour
{
    [SerializeField] private StatDefinitionDatabase statDatabase;
    [SerializeField] private Transform statsContent;
    [SerializeField] private StatRowUI statRowPrefab;

    public void Show(StatDisplay[] stats)
    {
        Clear();

        if (stats == null || statDatabase == null || statsContent == null || statRowPrefab == null)
            return;

        for (int i = 0; i < stats.Length; i++)
        {
            if (!statDatabase.TryGet(stats[i].statType, out var definition))
                continue;

            var row = Instantiate(statRowPrefab, statsContent);
            row.Setup(definition, stats[i].value);
        }
    }

    public void Clear()
    {
        if (statsContent == null)
            return;

        for (int i = statsContent.childCount - 1; i >= 0; i--)
            Destroy(statsContent.GetChild(i).gameObject);
    }
}
