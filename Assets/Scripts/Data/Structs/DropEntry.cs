using UnityEngine;

[System.Serializable]
public class DropEntry
{
    public EntityData item;
    public int minAmount = 1;
    public int maxAmount = 1;
    [Range(0f, 1f)]
    public float dropChance = 1f;
}
