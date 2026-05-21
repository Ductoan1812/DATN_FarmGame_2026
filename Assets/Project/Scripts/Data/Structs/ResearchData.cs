using UnityEngine;

[CreateAssetMenu(fileName = "Research_", menuName = "Game/Research Data")]
public class ResearchData : ScriptableObject
{
    public string id;
    public string titleKey;
    public string descriptionKey;
    public int unlockDay;
    public string unlockedRecipeId;
    public string rewardId;
}
