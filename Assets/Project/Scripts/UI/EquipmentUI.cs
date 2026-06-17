using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentUI : MonoBehaviour
{
    private static readonly Color ReadableStatTextColor = new(0.08f, 0.05f, 0.02f, 1f);

    [Header("Player Info")]
    [SerializeField] private GameObject playerNameInfo;
    [SerializeField] private TMP_Text playerNameText;

    [Header("Player Stats")]
    [SerializeField] private Transform statsContent;
    [SerializeField] private StatDefinitionDatabase statDatabase;
    [SerializeField] private StatRowUI statRowPrefab;
    [SerializeField] private List<EquipmentStatDisplayRule> statDisplayRules = new();

    private EquipmentSlotUI[] slots;
    private EventBus eventBus;
    private bool subscribedToBus;
    private bool clearedInitialStatsContent;
    private bool receivedPlayerInfo;
    private readonly List<GameObject> spawnedStatRows = new();

    private void Reset()
    {
        EnsureDefaultStatDisplayRules();
    }

    private void OnValidate()
    {
        EnsureDefaultStatDisplayRules();
    }

    private void Awake()
    {
        CacheSlots();
        AutoFindRefs();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnEnable()
    {
        TrySubscribe();
        ShowWaitingPlayerInfoIfNeeded();
    }

    private void Update()
    {
        if (!subscribedToBus)
            TrySubscribe();
    }

    private void OnDisable()
    {
        if (subscribedToBus && eventBus != null)
        {
            eventBus.Unsubscribe<EquipmentSlotChangedPublish>(OnEquipmentSlotChanged);
            eventBus.Unsubscribe<PlayerInfoChangedPublish>(OnPlayerInfoChanged);
            subscribedToBus = false;
        }
    }

    private void TrySubscribe()
    {
        if (subscribedToBus)
        {
            return;
        }

        eventBus = GameManager.Instance?.EventBus;
        if (eventBus == null)
        {
            return;
        }

        eventBus.Subscribe<EquipmentSlotChangedPublish>(OnEquipmentSlotChanged);
        eventBus.Subscribe<PlayerInfoChangedPublish>(OnPlayerInfoChanged);
        subscribedToBus = true;

        eventBus.Publish(new InventoryVisualRefreshRequestPublish());
    }

    private void OnEquipmentSlotChanged(EquipmentSlotChangedPublish e)
    {
        if (slots == null)
        {
            return;
        }

        foreach (var slot in slots)
        {
            if (slot == null)
            {
                continue;
            }

            if (slot.Part == e.part)
            {
                slot.SetItem(e.item);
            }
        }
    }

    private void OnPlayerInfoChanged(PlayerInfoChangedPublish e)
    {
        receivedPlayerInfo = true;
        SetPlayerName(e.keyName);
        ApplyPlayerStats(e.stats);
    }

    private void CacheSlots()
    {
        slots = GetComponentsInChildren<EquipmentSlotUI>(true);
    }

    private void AutoFindRefs()
    {
        playerNameText ??= FindText("PlayerNameText")
                        ?? FindText("NameText")
                        ?? FindText("Name_Tmp")
                        ?? FindText("PlayerName");

        playerNameInfo ??= playerNameText != null ? playerNameText.gameObject : null;

        statsContent ??= FindDeepChild(transform, "StatsContent")
                      ?? FindDeepChild(transform, "Content");

        statDatabase ??= Resources.Load<StatDefinitionDatabase>("StatDefinitionDatabase");

        if (statRowPrefab == null && statsContent != null)
        {
            statRowPrefab = statsContent.GetComponentInChildren<StatRowUI>(true);
            if (statRowPrefab != null && statRowPrefab.transform.IsChildOf(statsContent))
                statRowPrefab.gameObject.SetActive(false);
        }

        EnsureDefaultStatDisplayRules();
        ApplyReadableTextStyle();

#if UNITY_EDITOR
        if (statRowPrefab == null)
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Prefabs/UI/attributeInfo.prefab");
            if (prefab != null)
                statRowPrefab = prefab.GetComponent<StatRowUI>();
        }
#endif
    }

    private void SetPlayerName(string keyName)
    {
        if (playerNameInfo == null && playerNameText == null)
        {
            return;
        }

        bool hasName = !string.IsNullOrWhiteSpace(keyName);
        if (playerNameInfo != null)
        {
            playerNameInfo.SetActive(hasName);
        }

        if (!hasName || playerNameText == null)
        {
            return;
        }

        UiTextStyleUtility.ApplyRobotoAndColor(playerNameText, ReadableStatTextColor);

        var localized = playerNameText.GetComponent<LocalizedText>();
        if (localized != null)
        {
            localized.SetKey(keyName);
            return;
        }

        playerNameText.text = keyName;
    }

    private void ShowWaitingPlayerInfoIfNeeded()
    {
        if (receivedPlayerInfo)
            return;

        AutoFindRefs();

        if (playerNameText != null)
        {
            UiTextStyleUtility.ApplyRobotoAndColor(playerNameText, ReadableStatTextColor);
            playerNameText.text = "Người chơi";
        }

        if (playerNameInfo != null)
            playerNameInfo.SetActive(true);

        if (statsContent == null || spawnedStatRows.Count > 0)
            return;

        ClearPlayerStats();
        AddPlaceholderStatRow("Cấp độ", "-");
        AddPlaceholderStatRow("HP", "-");
        AddPlaceholderStatRow("Thể lực", "-");
        AddPlaceholderStatRow("Tiền", "-");
    }

    private void AddPlaceholderStatRow(string label, string value)
    {
        var row = CreateStatRow();
        if (row == null)
            return;

        row.SetupRawText(label, value);
        spawnedStatRows.Add(row.gameObject);
    }

    private void ApplyPlayerStats(StatDisplay[] stats)
    {
        AutoFindRefs();
        ClearPlayerStats();

        if (statsContent == null || stats == null || stats.Length == 0)
            return;

        var statMap = BuildStatMap(stats);
        EnsureDefaultStatDisplayRules();

        for (int i = 0; i < statDisplayRules.Count; i++)
        {
            var rule = statDisplayRules[i];
            if (rule == null)
                continue;

            if (!statMap.TryGetValue(rule.StatType, out var value))
            {
                if (rule.HideWhenMissing)
                    continue;

                value = 0f;
            }

            string displayValue;
            if (rule.UseMaxStat)
            {
                if (!statMap.TryGetValue(rule.MaxStatType, out var maxValue))
                {
                    if (rule.HideWhenMissing)
                        continue;

                    maxValue = 0f;
                }

                displayValue = $"{FormatNumber(value)}/{FormatNumber(maxValue)}";
            }
            else
            {
                var number = FormatNumber(value);
                displayValue = string.IsNullOrWhiteSpace(rule.ValueFormat)
                    ? number
                    : string.Format(rule.ValueFormat, number);
            }

            var row = CreateStatRow();
            if (row == null)
                continue;

            if (string.IsNullOrWhiteSpace(rule.LabelOverride) &&
                statDatabase != null &&
                statDatabase.TryGet(rule.StatType, out var definition))
            {
                row.SetupText(definition, displayValue);
            }
            else
            {
                var label = !string.IsNullOrWhiteSpace(rule.LabelOverride)
                    ? rule.LabelOverride
                    : rule.StatType.ToString();
                row.SetupRawText(label, displayValue);
            }

            spawnedStatRows.Add(row.gameObject);
        }
    }

    private static Dictionary<StatType, float> BuildStatMap(StatDisplay[] stats)
    {
        var result = new Dictionary<StatType, float>();
        for (int i = 0; i < stats.Length; i++)
            result[stats[i].statType] = stats[i].value;

        return result;
    }

    private void EnsureDefaultStatDisplayRules()
    {
        if (statDisplayRules != null && statDisplayRules.Count > 0)
            return;

        statDisplayRules = new List<EquipmentStatDisplayRule>
        {
            EquipmentStatDisplayRule.Single(StatType.Level, "Cấp độ"),
            EquipmentStatDisplayRule.Pair(StatType.Hp, StatType.MaxHp, "HP"),
            EquipmentStatDisplayRule.Pair(StatType.Mp, StatType.MaxMp, "MP"),
            EquipmentStatDisplayRule.Pair(StatType.Stamina, StatType.MaxStamina, "Thể lực"),
            EquipmentStatDisplayRule.Pair(StatType.Exp, StatType.MaxExp, "EXP"),
            EquipmentStatDisplayRule.Single(StatType.Attack),
            EquipmentStatDisplayRule.Single(StatType.Defense),
            EquipmentStatDisplayRule.Single(StatType.Speed),
            EquipmentStatDisplayRule.Single(StatType.CritChance, valueFormat: "{0}%"),
            EquipmentStatDisplayRule.Single(StatType.CritDamage, valueFormat: "{0}%"),
            EquipmentStatDisplayRule.Single(StatType.Money, "Tiền")
        };
    }

    private void ClearPlayerStats()
    {
        for (int i = spawnedStatRows.Count - 1; i >= 0; i--)
        {
            if (spawnedStatRows[i] != null)
                Destroy(spawnedStatRows[i]);
        }

        spawnedStatRows.Clear();

        if (statsContent == null || clearedInitialStatsContent)
            return;

        clearedInitialStatsContent = true;

        for (int i = statsContent.childCount - 1; i >= 0; i--)
        {
            var child = statsContent.GetChild(i);
            if (statRowPrefab != null && child == statRowPrefab.transform)
            {
                child.gameObject.SetActive(false);
                continue;
            }

            if (child.GetComponent<StatRowUI>() != null || child.name.StartsWith("StatRow_"))
                Destroy(child.gameObject);
        }
    }

    private StatRowUI CreateStatRow()
    {
        if (statsContent == null)
            return null;

        if (statRowPrefab != null)
        {
            var row = Instantiate(statRowPrefab, statsContent);
            row.gameObject.SetActive(true);
            return row;
        }

        var rowObject = new GameObject("StatRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(StatRowUI));
        rowObject.transform.SetParent(statsContent, false);

        var layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var nameObject = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObject.transform.SetParent(rowObject.transform, false);
        var nameText = nameObject.GetComponent<TextMeshProUGUI>();
        nameText.fontSize = 18f;
        UiTextStyleUtility.ApplyRobotoAndColor(nameText, ReadableStatTextColor);

        var valueObject = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
        valueObject.transform.SetParent(rowObject.transform, false);
        var valueText = valueObject.GetComponent<TextMeshProUGUI>();
        valueText.fontSize = 18f;
        UiTextStyleUtility.ApplyRobotoAndColor(valueText, ReadableStatTextColor);

        return rowObject.GetComponent<StatRowUI>();
    }

    private void ApplyReadableTextStyle()
    {
        UiTextStyleUtility.ApplyRobotoAndColor(playerNameText, ReadableStatTextColor);
        UiTextStyleUtility.ApplyRobotoAndColorToChildren(statsContent, ReadableStatTextColor);
    }

    private TMP_Text FindText(string objectName)
    {
        var child = FindDeepChild(transform, objectName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static string FormatNumber(float value)
    {
        if (Mathf.Approximately(value, Mathf.Round(value)))
            return Mathf.RoundToInt(value).ToString();

        return value.ToString("0.##");
    }

    private static Transform FindDeepChild(Transform root, string objectName)
    {
        if (root == null || string.IsNullOrEmpty(objectName))
        {
            return null;
        }

        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeepChild(root.GetChild(i), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}

[System.Serializable]
public class EquipmentStatDisplayRule
{
    [SerializeField] private StatType statType;
    [SerializeField] private bool useMaxStat;
    [SerializeField] private StatType maxStatType;
    [SerializeField] private bool hideWhenMissing = true;
    [SerializeField] private string labelOverride;
    [SerializeField] private string valueFormat;

    public StatType StatType => statType;
    public bool UseMaxStat => useMaxStat;
    public StatType MaxStatType => maxStatType;
    public bool HideWhenMissing => hideWhenMissing;
    public string LabelOverride => labelOverride;
    public string ValueFormat => valueFormat;

    public static EquipmentStatDisplayRule Single(StatType statType, string labelOverride = "", string valueFormat = "")
    {
        return new EquipmentStatDisplayRule
        {
            statType = statType,
            useMaxStat = false,
            hideWhenMissing = true,
            labelOverride = labelOverride,
            valueFormat = valueFormat
        };
    }

    public static EquipmentStatDisplayRule Pair(StatType currentStatType, StatType maxStatType, string labelOverride = "")
    {
        return new EquipmentStatDisplayRule
        {
            statType = currentStatType,
            useMaxStat = true,
            maxStatType = maxStatType,
            hideWhenMissing = true,
            labelOverride = labelOverride
        };
    }
}
