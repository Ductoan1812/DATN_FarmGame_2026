using DialogueGraphTool;
using UnityEngine;

[System.Serializable]
public class DialogueModule : IModuleData
{
    public DialogueGraphData graph;
    public string optionTextKey = "ui.dialogue.talk";
    public int priority = 10;

    public override IModuleRuntime CreateRuntime()
    {
        return new DialogueRuntime(this);
    }
}
