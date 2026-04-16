using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Entity", fileName = "NewEntity")]
public class EntityData : ScriptableObject
{
    [Header("══════ THÔNG TIN CƠ BẢN ══════")]
    public string id;
    public string keyName;
    public string descKey;
    public Sprite icon;
    public ItemCategory category;
    public int maxStack = 1;
    public int buyPrice;
    public int sellPrice;

    [Header("══════ BaseStats ══════")]
    public StatsData baseStats = new();

    [Header("══════ Placement ══════")]
    public PlacementRule placementRule;

    [Header("══════ Module ══════")]
    [SerializeReference]
    public List<IModuleData> modules = new();
}
