// Một dòng chỉ số: "Attack +5", "Defense +3"...
// Dùng ở: ItemData.baseStats, ConsumableInfo.buffEffects, ItemRuntime.bonusEffects
[System.Serializable]
public class StatEntry
{
    public StatType statType;
    public float value;

}
