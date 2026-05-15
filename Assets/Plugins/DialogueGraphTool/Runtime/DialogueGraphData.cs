using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueGraphTool
{
    [CreateAssetMenu(menuName = "Dialogue Graph Tool/Dialogue Graph", fileName = "NewDialogueGraph")]
    public class DialogueGraphData : ScriptableObject
    {
        public string id;
        public string startNodeId = "start";
        public List<DialogueNodeData> nodes = new();

        public DialogueNodeData GetNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId)) return null;
            return nodes.Find(n => n != null && n.nodeId == nodeId);
        }

        public DialogueNodeData GetStartNode()
        {
            return GetNode(startNodeId) ?? (nodes.Count > 0 ? nodes[0] : null);
        }
    }

    [Serializable]
    public class DialogueNodeData
    {
        public string nodeId = "start";
        public DialogueNodeType nodeType = DialogueNodeType.Dialogue;
        public Vector2 editorPosition = new(260f, 160f);

        [Header("Dialogue")]
        public string speakerNameKey;
        public string lineKey;
        public string portraitKey;
        public DialoguePortraitSlot portraitSlot = DialoguePortraitSlot.Left;
        public string emotionKey;
        public List<DialogueChoiceData> choices = new();

        [Header("Condition")]
        public string conditionKey;
        public DialogueCompareMode compareMode = DialogueCompareMode.Exists;
        public string compareValue;
        public string trueNodeId;
        public string falseNodeId;

        [Header("Event")]
        public string eventKey;
        public string eventPayload;

        [Header("Audio")]
        public string audioKey;
        public DialogueAudioMode audioMode = DialogueAudioMode.PlayOneShot;

        [Header("Pass Through")]
        public string nextNodeId;
    }

    [Serializable]
    public class DialogueChoiceData
    {
        public string id;
        public string textKey;
        public string nextNodeId;
        public string conditionKey;
    }

    public enum DialogueNodeType
    {
        Dialogue,
        Condition,
        Event,
        Audio,
        Portrait,
        End
    }

    public enum DialoguePortraitSlot
    {
        Left,
        Right,
        Center
    }

    public enum DialogueAudioMode
    {
        PlayOneShot,
        StartMusic,
        StopMusic
    }

    public enum DialogueCompareMode
    {
        Exists,
        Equals,
        NotEquals,
        GreaterOrEqual,
        LessOrEqual
    }
}
