using UnityEngine;

[System.Serializable]
public class ResourceHitReactionModule : IModuleData
{
    public bool reactOnlyToHarvestTool = true;
    public bool useProceduralMotion = true;
    public Color flashColor = new Color(1f, 0.9f, 0.65f, 1f);
    [Range(0.01f, 0.5f)] public float flashDuration = 0.12f;
    [Range(0f, 0.5f)] public float scalePunch = 0.12f;
    [Range(0f, 20f)] public float rotationPunch = 4f;
    public string animatorHitTrigger = "Hit";

    public override IModuleRuntime CreateRuntime()
    {
        return new ResourceHitReactionRuntime(this);
    }
}
