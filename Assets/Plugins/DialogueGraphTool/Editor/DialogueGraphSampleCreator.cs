using System.Collections.Generic;
using DialogueGraphTool;
using UnityEditor;
using UnityEngine;

namespace DialogueGraphTool.Editor
{
    public static class DialogueGraphSampleCreator
    {
        [MenuItem("Tools/Dialogue Graph Tool/Create Sample/Farmer Dialogue Graph")]
        public static void CreateFarmerDialogueGraph()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Farmer Dialogue Graph Sample",
                "SampleFarmerDialogueGraph",
                "asset",
                "Choose where to save the sample DialogueGraphData asset.");

            if (string.IsNullOrWhiteSpace(path))
                return;

            var graph = CreateFarmerSample();
            AssetDatabase.CreateAsset(graph, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = graph;
            DialogueGraphEditorWindow.Open(graph);
        }

        public static DialogueGraphData CreateFarmerSample()
        {
            var graph = ScriptableObject.CreateInstance<DialogueGraphData>();
            graph.id = "sample_farmer_dialogue";
            graph.startNodeId = "start";
            graph.nodes = new List<DialogueNodeData>
            {
                new()
                {
                    nodeId = "start",
                    nodeType = DialogueNodeType.Dialogue,
                    editorPosition = new Vector2(120f, 220f),
                    speakerNameKey = "npc.farmer.name",
                    lineKey = "dialogue.sample.farmer.hello",
                    portraitKey = "farmer_default",
                    portraitSlot = DialoguePortraitSlot.Left,
                    emotionKey = "happy",
                    choices = new List<DialogueChoiceData>
                    {
                        new()
                        {
                            id = "ask_work",
                            textKey = "dialogue.sample.choice.ask_work",
                            nextNodeId = "check_has_met"
                        },
                        new()
                        {
                            id = "goodbye",
                            textKey = "ui.dialogue.goodbye",
                            nextNodeId = "end"
                        }
                    }
                },
                new()
                {
                    nodeId = "check_has_met",
                    nodeType = DialogueNodeType.Condition,
                    editorPosition = new Vector2(570f, 220f),
                    conditionKey = "sample.has_met_farmer",
                    compareMode = DialogueCompareMode.Exists,
                    trueNodeId = "old_friend",
                    falseNodeId = "first_meeting"
                },
                new()
                {
                    nodeId = "first_meeting",
                    nodeType = DialogueNodeType.Event,
                    editorPosition = new Vector2(1010f, 380f),
                    eventKey = "sample.set_flag",
                    eventPayload = "sample.has_met_farmer",
                    nextNodeId = "play_greeting_audio"
                },
                new()
                {
                    nodeId = "play_greeting_audio",
                    nodeType = DialogueNodeType.Audio,
                    editorPosition = new Vector2(1430f, 380f),
                    audioKey = "farmer_greeting",
                    audioMode = DialogueAudioMode.PlayOneShot,
                    nextNodeId = "show_portrait"
                },
                new()
                {
                    nodeId = "show_portrait",
                    nodeType = DialogueNodeType.Portrait,
                    editorPosition = new Vector2(1850f, 380f),
                    portraitKey = "farmer_default",
                    portraitSlot = DialoguePortraitSlot.Left,
                    emotionKey = "happy",
                    nextNodeId = "offer_work"
                },
                new()
                {
                    nodeId = "offer_work",
                    nodeType = DialogueNodeType.Dialogue,
                    editorPosition = new Vector2(2280f, 380f),
                    speakerNameKey = "npc.farmer.name",
                    lineKey = "dialogue.sample.farmer.offer_work",
                    portraitKey = "farmer_default",
                    portraitSlot = DialoguePortraitSlot.Left,
                    emotionKey = "happy",
                    choices = new List<DialogueChoiceData>
                    {
                        new()
                        {
                            id = "accept",
                            textKey = "dialogue.sample.choice.accept_work",
                            nextNodeId = "accept_event"
                        },
                        new()
                        {
                            id = "later",
                            textKey = "dialogue.sample.choice.later",
                            nextNodeId = "end"
                        }
                    }
                },
                new()
                {
                    nodeId = "old_friend",
                    nodeType = DialogueNodeType.Dialogue,
                    editorPosition = new Vector2(1010f, 80f),
                    speakerNameKey = "npc.farmer.name",
                    lineKey = "dialogue.sample.farmer.old_friend",
                    portraitKey = "farmer_default",
                    portraitSlot = DialoguePortraitSlot.Left,
                    emotionKey = "neutral",
                    choices = new List<DialogueChoiceData>
                    {
                        new()
                        {
                            id = "work",
                            textKey = "dialogue.sample.choice.ask_work",
                            nextNodeId = "offer_work"
                        },
                        new()
                        {
                            id = "goodbye",
                            textKey = "ui.dialogue.goodbye",
                            nextNodeId = "end"
                        }
                    }
                },
                new()
                {
                    nodeId = "accept_event",
                    nodeType = DialogueNodeType.Event,
                    editorPosition = new Vector2(2720f, 380f),
                    eventKey = "sample.accept_farmer_work",
                    eventPayload = "quest.sample_farmer_work",
                    nextNodeId = "accepted"
                },
                new()
                {
                    nodeId = "accepted",
                    nodeType = DialogueNodeType.Dialogue,
                    editorPosition = new Vector2(3160f, 380f),
                    speakerNameKey = "npc.farmer.name",
                    lineKey = "dialogue.sample.farmer.accepted",
                    portraitKey = "farmer_default",
                    portraitSlot = DialoguePortraitSlot.Left,
                    emotionKey = "happy",
                    choices = new List<DialogueChoiceData>
                    {
                        new()
                        {
                            id = "goodbye",
                            textKey = "ui.dialogue.goodbye",
                            nextNodeId = "end"
                        }
                    }
                },
                new()
                {
                    nodeId = "end",
                    nodeType = DialogueNodeType.End,
                    editorPosition = new Vector2(3620f, 220f)
                }
            };

            return graph;
        }
    }
}
