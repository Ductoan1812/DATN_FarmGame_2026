using System;
using System.Collections.Generic;
using DialogueGraphTool;
using UnityEngine;

public static class DialogueService
{
    private const int MaxNodeHops = 64;

    public static Func<DialogueConditionContext, bool> ConditionEvaluator { get; set; }

    public static void Start(EntityRuntime speaker, EntityRuntime listener, DialogueGraphData graph)
    {
        if (graph == null) return;
        RunNode(speaker, listener, graph, graph.GetStartNode(), 0);
    }

    private static void RunNode(
        EntityRuntime speaker,
        EntityRuntime listener,
        DialogueGraphData graph,
        DialogueNodeData node,
        int hopCount)
    {
        if (node == null) return;
        if (hopCount > MaxNodeHops)
        {
            Debug.LogWarning($"[DialogueService] Graph '{graph?.id}' exceeded max node hops. Check for an infinite loop.");
            return;
        }

        switch (node.nodeType)
        {
            case DialogueNodeType.Dialogue:
                ShowDialogueNode(speaker, listener, graph, node);
                break;

            case DialogueNodeType.Condition:
                RunConditionNode(speaker, listener, graph, node, hopCount + 1);
                break;

            case DialogueNodeType.Event:
                PublishEventNode(speaker, listener, graph, node);
                RunNode(speaker, listener, graph, graph.GetNode(node.nextNodeId), hopCount + 1);
                break;

            case DialogueNodeType.Audio:
                PublishAudioNode(speaker, listener, graph, node);
                RunNode(speaker, listener, graph, graph.GetNode(node.nextNodeId), hopCount + 1);
                break;

            case DialogueNodeType.Portrait:
                PublishPortraitNode(speaker, listener, graph, node);
                RunNode(speaker, listener, graph, graph.GetNode(node.nextNodeId), hopCount + 1);
                break;

            case DialogueNodeType.End:
                return;
        }
    }

    private static void ShowDialogueNode(
        EntityRuntime speaker,
        EntityRuntime listener,
        DialogueGraphData graph,
        DialogueNodeData node)
    {
        if (node == null) return;

        var choices = new List<DialogueChoiceViewData>();
        if (node.choices != null)
        {
            foreach (var choice in node.choices)
            {
                if (choice == null || string.IsNullOrWhiteSpace(choice.textKey)) continue;
                if (!IsChoiceVisible(speaker, listener, graph, choice.conditionKey)) continue;

                Action execute = string.IsNullOrWhiteSpace(choice.nextNodeId)
                    ? null
                    : () => RunNode(speaker, listener, graph, graph.GetNode(choice.nextNodeId), 0);

                choices.Add(new DialogueChoiceViewData(choice.id, choice.textKey, execute));
            }
        }

        var speakerKey = string.IsNullOrWhiteSpace(node.speakerNameKey)
            ? speaker?.entityData?.keyName
            : node.speakerNameKey;

        var viewData = new DialogueViewData(
            speaker,
            listener,
            graph.id,
            speakerKey,
            node.lineKey,
            node.portraitKey,
            node.portraitSlot,
            node.emotionKey,
            choices);

        GameManager.Instance?.EventBus?.Publish(new DialogueViewPublish(viewData));
    }

    private static void RunConditionNode(
        EntityRuntime speaker,
        EntityRuntime listener,
        DialogueGraphData graph,
        DialogueNodeData node,
        int hopCount)
    {
        bool result = false;
        if (ConditionEvaluator != null)
        {
            var context = new DialogueConditionContext(
                speaker,
                listener,
                graph,
                node.conditionKey,
                node.compareMode,
                node.compareValue);

            result = ConditionEvaluator.Invoke(context);
        }
        else
        {
            Debug.LogWarning($"[DialogueService] No ConditionEvaluator registered for condition '{node.conditionKey}'.");
        }

        string nextNodeId = result ? node.trueNodeId : node.falseNodeId;
        RunNode(speaker, listener, graph, graph.GetNode(nextNodeId), hopCount);
    }

    private static DialogueViewData CreateContext(EntityRuntime speaker, EntityRuntime listener, DialogueGraphData graph)
    {
        return new DialogueViewData(
            speaker,
            listener,
            graph?.id,
            speaker?.entityData?.keyName,
            string.Empty,
            string.Empty,
            DialoguePortraitSlot.Left,
            string.Empty,
            Array.Empty<DialogueChoiceViewData>());
    }

    private static void PublishEventNode(EntityRuntime speaker, EntityRuntime listener, DialogueGraphData graph, DialogueNodeData node)
    {
        GameManager.Instance?.EventBus?.Publish(
            new DialogueEventNodePublish(CreateContext(speaker, listener, graph), node.eventKey, node.eventPayload));
    }

    private static void PublishAudioNode(EntityRuntime speaker, EntityRuntime listener, DialogueGraphData graph, DialogueNodeData node)
    {
        GameManager.Instance?.EventBus?.Publish(
            new DialogueAudioNodePublish(CreateContext(speaker, listener, graph), node.audioKey, node.audioMode));
    }

    private static void PublishPortraitNode(EntityRuntime speaker, EntityRuntime listener, DialogueGraphData graph, DialogueNodeData node)
    {
        GameManager.Instance?.EventBus?.Publish(
            new DialoguePortraitNodePublish(CreateContext(speaker, listener, graph), node.portraitKey, node.portraitSlot, node.emotionKey));
    }

    private static bool IsChoiceVisible(
        EntityRuntime speaker,
        EntityRuntime listener,
        DialogueGraphData graph,
        string conditionKey)
    {
        if (string.IsNullOrWhiteSpace(conditionKey))
            return true;

        if (ConditionEvaluator == null)
        {
            Debug.LogWarning($"[DialogueService] Choice condition '{conditionKey}' skipped because ConditionEvaluator is null.");
            return false;
        }

        var context = new DialogueConditionContext(
            speaker,
            listener,
            graph,
            conditionKey,
            DialogueCompareMode.Exists,
            string.Empty);

        return ConditionEvaluator.Invoke(context);
    }
}

public sealed class DialogueViewData
{
    public EntityRuntime Speaker { get; }
    public EntityRuntime Listener { get; }
    public string GraphId { get; }
    public string SpeakerNameKey { get; }
    public string LineKey { get; }
    public string PortraitKey { get; }
    public DialoguePortraitSlot PortraitSlot { get; }
    public string EmotionKey { get; }
    public IReadOnlyList<DialogueChoiceViewData> Choices { get; }

    public DialogueViewData(
        EntityRuntime speaker,
        EntityRuntime listener,
        string graphId,
        string speakerNameKey,
        string lineKey,
        string portraitKey,
        DialoguePortraitSlot portraitSlot,
        string emotionKey,
        IReadOnlyList<DialogueChoiceViewData> choices)
    {
        Speaker = speaker;
        Listener = listener;
        GraphId = graphId;
        SpeakerNameKey = speakerNameKey;
        LineKey = lineKey;
        PortraitKey = portraitKey;
        PortraitSlot = portraitSlot;
        EmotionKey = emotionKey;
        Choices = choices;
    }
}

public sealed class DialogueConditionContext
{
    public EntityRuntime Speaker { get; }
    public EntityRuntime Listener { get; }
    public DialogueGraphData Graph { get; }
    public string ConditionKey { get; }
    public DialogueCompareMode CompareMode { get; }
    public string CompareValue { get; }

    public DialogueConditionContext(
        EntityRuntime speaker,
        EntityRuntime listener,
        DialogueGraphData graph,
        string conditionKey,
        DialogueCompareMode compareMode,
        string compareValue)
    {
        Speaker = speaker;
        Listener = listener;
        Graph = graph;
        ConditionKey = conditionKey;
        CompareMode = compareMode;
        CompareValue = compareValue;
    }
}

public sealed class DialogueChoiceViewData
{
    public string Id { get; }
    public string TextKey { get; }
    public Action Execute { get; }

    public DialogueChoiceViewData(string id, string textKey, Action execute)
    {
        Id = id;
        TextKey = textKey;
        Execute = execute;
    }
}
