using UnityEngine;

/// <summary>
/// Định nghĩa unlock khi đạt mastery level.
/// Dùng để config unlock table cho progression system.
/// </summary>
[CreateAssetMenu(menuName = "FarmGame/Config/MasteryUnlockData", fileName = "MasteryUnlockData")]
public class MasteryUnlockData : ScriptableObject
{
    [System.Serializable]
    public class UnlockEntry
    {
        public int masteryLevel;
        public string unlockId;
        public string description;
    }

    public UnlockEntry[] unlocks = System.Array.Empty<UnlockEntry>();
}
