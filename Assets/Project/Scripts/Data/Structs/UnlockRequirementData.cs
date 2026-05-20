using System;

[Serializable]
public class UnlockRequirementData
{
    public int requiredLevel = 1;
    public string[] requiredQuestIds = Array.Empty<string>();
}
