using UnityEngine;
public class Stat
{
    public float baseValue;
    public float flatBonus;
    public float percentBonus;

    public float GetValue()
    {
        return (baseValue + flatBonus) * (1 + percentBonus);
    }
}