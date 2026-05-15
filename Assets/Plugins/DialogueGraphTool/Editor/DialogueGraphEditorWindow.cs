using System.Collections.Generic;
using DialogueGraphTool;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueGraphTool.Editor
{
    public sealed class DialogueGraphEditorWindow : EditorWindow
    {
        private DialogueGraphData graph;
        private DialogueGraphView graphView;
        private ObjectField graphField;
        private Label statusLabel;

        [MenuItem("Tools/Dialogue Graph Tool/Open Graph Editor")]
        public static void OpenWindow()
        {
            GetWindow<DialogueGraphEditorWindow>("Dialogue Graph");
        }

        public static void Open(DialogueGraphData targetGraph)
        {
            var window = GetWindow<DialogueGraphEditorWindow>("Dialogue Graph");
            window.SetGraph(targetGraph);
            window.Focus();
        }

        private void OnEnable()
        {
            BuildLayout();
        }

        private void BuildLayout()
        {
            rootVisualElement.Clear();

            var toolbar = new Toolbar();
            graphField = new ObjectField("Graph")
            {
                objectType = typeof(DialogueGraphData),
                allowSceneObjects = false,
                value = graph
            };
            graphField.style.minWidth = 330f;
            graphField.RegisterValueChangedCallback(evt => SetGraph(evt.newValue as DialogueGraphData));
            toolbar.Add(graphField);

            toolbar.Add(new Button(() => graphView?.CreateNodeAt(DialogueNodeType.Dialogue, graphView.GetViewCenter(), true)) { text = "+ Dialogue" });
            toolbar.Add(new Button(() => graphView?.CreateNodeAt(DialogueNodeType.Condition, graphView.GetViewCenter(), true)) { text = "+ Condition" });
            toolbar.Add(new Button(() => graphView?.CreateNodeAt(DialogueNodeType.Event, graphView.GetViewCenter(), true)) { text = "+ Event" });
            toolbar.Add(new Button(() => graphView?.CreateNodeAt(DialogueNodeType.Audio, graphView.GetViewCenter(), true)) { text = "+ Audio" });
            toolbar.Add(new Button(() => graphView?.CreateNodeAt(DialogueNodeType.Portrait, graphView.GetViewCenter(), true)) { text = "+ Portrait" });
            toolbar.Add(new Button(() => graphView?.FrameAll()) { text = "Frame All" });
            toolbar.Add(new Button(Save) { text = "Save Data" });

            statusLabel = new Label();
            statusLabel.style.marginLeft = 10f;
            statusLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            toolbar.Add(statusLabel);

            rootVisualElement.Add(toolbar);

            graphView = new DialogueGraphView(this);
            graphView.StretchToParentSize();
            rootVisualElement.Add(graphView);

            UpdateStatus();
            graphView.Load(graph);
        }

        private void SetGraph(DialogueGraphData nextGraph)
        {
            graph = nextGraph;

            if (graphField != null && graphField.value != graph)
                graphField.SetValueWithoutNotify(graph);

            UpdateStatus();
            graphView?.Load(graph);
        }

        private void Save()
        {
            if (graph == null) return;

            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            UpdateStatus("Saved");
        }

        internal void MarkDirty(string action)
        {
            if (graph == null) return;

            Undo.RecordObject(graph, action);
            EditorUtility.SetDirty(graph);
            UpdateStatus("Dirty");
        }

        private void UpdateStatus(string suffix = null)
        {
            if (statusLabel == null) return;

            string graphName = graph != null ? graph.name : "No graph selected";
            statusLabel.text = string.IsNullOrWhiteSpace(suffix) ? graphName : $"{graphName} - {suffix}";
        }
    }

    public sealed class DialogueGraphView : GraphView
    {
        private readonly DialogueGraphEditorWindow window;
        private readonly Dictionary<DialogueNodeData, DialogueNodeView> nodeViews = new();
        private DialogueGraphData graph;
        private bool suppressGraphChanged;

        public DialogueGraphView(DialogueGraphEditorWindow window)
        {
            this.window = window;

            style.flexGrow = 1f;
            Insert(0, new GridBackground());

            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            graphViewChanged = OnGraphViewChanged;

            var miniMap = new MiniMap { anchored = true };
            miniMap.SetPosition(new Rect(12f, 42f, 240f, 160f));
            Add(miniMap);
        }

        public Vector2 GetViewCenter()
        {
            return contentViewContainer.WorldToLocal(worldBound.center);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var result = new List<Port>();
            ports.ForEach(port =>
            {
                if (port == startPort) return;
                if (port.node == startPort.node) return;
                if (port.direction == startPort.direction) return;
                result.Add(port);
            });
            return result;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            if (graph == null) return;

            Vector2 localPosition = contentViewContainer.WorldToLocal(evt.mousePosition);
            evt.menu.AppendAction("Create/Dialogue Node", _ => CreateNodeAt(DialogueNodeType.Dialogue, localPosition, true));
            evt.menu.AppendAction("Create/Condition Node", _ => CreateNodeAt(DialogueNodeType.Condition, localPosition, true));
            evt.menu.AppendAction("Create/Event Node", _ => CreateNodeAt(DialogueNodeType.Event, localPosition, true));
            evt.menu.AppendAction("Create/Audio Node", _ => CreateNodeAt(DialogueNodeType.Audio, localPosition, true));
            evt.menu.AppendAction("Create/Portrait Node", _ => CreateNodeAt(DialogueNodeType.Portrait, localPosition, true));
            evt.menu.AppendAction("Create/End Node", _ => CreateNodeAt(DialogueNodeType.End, localPosition, true));
        }

        public void Load(DialogueGraphData targetGraph)
        {
            graph = targetGraph;
            suppressGraphChanged = true;

            DeleteElements(graphElements.ToList());
            nodeViews.Clear();

            if (graph != null)
            {
                EnsureValidGraph();
                foreach (var node in graph.nodes)
                    CreateNodeView(node);

                RebuildEdges();
                FrameAll();
            }

            suppressGraphChanged = false;
        }

        public void CreateNodeAt(DialogueNodeType type, Vector2 position, bool recordUndo)
        {
            if (graph == null) return;
            if (recordUndo) window.MarkDirty("Create Dialogue Graph Node");

            var node = new DialogueNodeData
            {
                nodeId = MakeUniqueNodeId(type.ToString().ToLowerInvariant()),
                nodeType = type,
                editorPosition = position,
                lineKey = type == DialogueNodeType.Dialogue ? "dialogue.line.key" : string.Empty,
                eventKey = type == DialogueNodeType.Event ? "dialogue.event.key" : string.Empty,
                audioKey = type == DialogueNodeType.Audio ? "dialogue.audio.key" : string.Empty,
                portraitKey = type == DialogueNodeType.Portrait ? "dialogue.portrait.key" : string.Empty,
                conditionKey = type == DialogueNodeType.Condition ? "dialogue.condition.key" : string.Empty,
                choices = new List<DialogueChoiceData>()
            };

            graph.nodes.Add(node);
            if (string.IsNullOrWhiteSpace(graph.startNodeId))
                graph.startNodeId = node.nodeId;

            CreateNodeView(node);
        }

        internal void SetStartNode(DialogueNodeData node)
        {
            if (graph == null || node == null) return;

            window.MarkDirty("Set Dialogue Start Node");
            graph.startNodeId = node.nodeId;
            RefreshNodeTitles();
        }

        internal void RenameNode(DialogueNodeData node, string nextNodeId)
        {
            if (graph == null || node == null) return;

            nextNodeId = string.IsNullOrWhiteSpace(nextNodeId) ? string.Empty : nextNodeId.Trim();
            if (string.IsNullOrWhiteSpace(nextNodeId)) return;
            if (nextNodeId == node.nodeId) return;
            if (graph.GetNode(nextNodeId) != null) return;

            window.MarkDirty("Rename Dialogue Node");

            string oldNodeId = node.nodeId;
            node.nodeId = nextNodeId;

            if (graph.startNodeId == oldNodeId)
                graph.startNodeId = nextNodeId;

            foreach (var other in graph.nodes)
                ReplaceNodeReference(other, oldNodeId, nextNodeId);

            RefreshNodeTitles();
            RebuildEdges();
        }

        internal void ChangeNodeType(DialogueNodeData node, DialogueNodeType nextType)
        {
            if (node == null || node.nodeType == nextType) return;

            window.MarkDirty("Change Dialogue Node Type");
            node.nodeType = nextType;
            RebuildNode(node);
        }

        internal void DeleteNode(DialogueNodeData node)
        {
            DeleteNode(node, removeView: true);
        }

        internal void AddChoice(DialogueNodeData node)
        {
            if (node == null) return;

            window.MarkDirty("Add Dialogue Choice");
            node.choices ??= new List<DialogueChoiceData>();
            node.choices.Add(new DialogueChoiceData
            {
                id = MakeUniqueChoiceId(node),
                textKey = "ui.dialogue.choice",
                nextNodeId = string.Empty
            });

            RebuildNode(node);
        }

        internal void RemoveChoice(DialogueNodeData node, DialogueChoiceData choice)
        {
            if (node?.choices == null || choice == null) return;

            window.MarkDirty("Remove Dialogue Choice");
            node.choices.Remove(choice);
            RebuildNode(node);
        }

        internal void ChangeField(System.Action change, string action)
        {
            window.MarkDirty(action);
            change?.Invoke();
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (graph == null || suppressGraphChanged) return change;

            if (change.movedElements != null)
            {
                foreach (var element in change.movedElements)
                {
                    if (element is not DialogueNodeView nodeView) continue;

                    window.MarkDirty("Move Dialogue Node");
                    nodeView.Data.editorPosition = nodeView.GetPosition().position;
                }
            }

            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                    ApplyEdge(edge);
            }

            if (change.elementsToRemove != null)
            {
                var filtered = new List<GraphElement>();
                foreach (var element in change.elementsToRemove)
                {
                    if (element is Edge edge)
                    {
                        ClearEdge(edge);
                        filtered.Add(edge);
                    }
                    else if (element is DialogueNodeView nodeView)
                    {
                        if (graph.nodes.Count <= 1) continue;

                        DeleteNode(nodeView.Data, removeView: false);
                        filtered.Add(nodeView);
                    }
                    else
                    {
                        filtered.Add(element);
                    }
                }
                change.elementsToRemove = filtered;
            }

            return change;
        }

        private void ApplyEdge(Edge edge)
        {
            if (edge?.output?.userData is not DialoguePortData output) return;
            if (edge.input?.node is not DialogueNodeView targetView) return;

            window.MarkDirty("Connect Dialogue Graph Port");
            RemoveExistingEdgeForOutput(edge.output);
            SetPortTarget(output, targetView.Data.nodeId);
        }

        private void ClearEdge(Edge edge)
        {
            if (edge?.output?.userData is not DialoguePortData output) return;

            window.MarkDirty("Disconnect Dialogue Graph Port");
            SetPortTarget(output, string.Empty);
        }

        private void SetPortTarget(DialoguePortData output, string targetNodeId)
        {
            switch (output.kind)
            {
                case DialoguePortKind.Choice:
                    if (output.choice != null) output.choice.nextNodeId = targetNodeId;
                    break;
                case DialoguePortKind.Next:
                    output.node.nextNodeId = targetNodeId;
                    break;
                case DialoguePortKind.True:
                    output.node.trueNodeId = targetNodeId;
                    break;
                case DialoguePortKind.False:
                    output.node.falseNodeId = targetNodeId;
                    break;
            }
        }

        private void RemoveExistingEdgeForOutput(Port output)
        {
            Edge oldEdge = null;
            edges.ForEach(edge =>
            {
                if (edge.output == output)
                    oldEdge = edge;
            });

            if (oldEdge != null)
                RemoveElement(oldEdge);
        }

        private void DeleteNode(DialogueNodeData node, bool removeView)
        {
            if (graph == null || node == null || graph.nodes.Count <= 1) return;

            var view = GetNodeView(node);
            if (view == null) return;

            window.MarkDirty("Delete Dialogue Node");
            graph.nodes.Remove(node);

            foreach (var other in graph.nodes)
                ReplaceNodeReference(other, node.nodeId, string.Empty);

            if (graph.startNodeId == node.nodeId)
                graph.startNodeId = graph.nodes.Count > 0 ? graph.nodes[0].nodeId : string.Empty;

            if (removeView)
                RemoveElement(view);

            nodeViews.Remove(node);
            RefreshNodeTitles();
            RebuildEdges();
        }

        private void ReplaceNodeReference(DialogueNodeData node, string oldId, string nextId)
        {
            if (node == null) return;

            if (node.nextNodeId == oldId) node.nextNodeId = nextId;
            if (node.trueNodeId == oldId) node.trueNodeId = nextId;
            if (node.falseNodeId == oldId) node.falseNodeId = nextId;

            if (node.choices == null) return;
            foreach (var choice in node.choices)
            {
                if (choice != null && choice.nextNodeId == oldId)
                    choice.nextNodeId = nextId;
            }
        }

        private void CreateNodeView(DialogueNodeData node)
        {
            if (node == null) return;

            node.choices ??= new List<DialogueChoiceData>();
            var view = new DialogueNodeView(this, node, IsStartNode(node));
            view.SetPosition(new Rect(node.editorPosition, DialogueNodeView.DefaultSize));
            nodeViews[node] = view;
            AddElement(view);
        }

        private void RebuildNode(DialogueNodeData node)
        {
            var oldView = GetNodeView(node);
            Vector2 position = oldView != null ? oldView.GetPosition().position : node.editorPosition;

            suppressGraphChanged = true;
            if (oldView != null)
            {
                RemoveElement(oldView);
                nodeViews.Remove(node);
            }

            node.editorPosition = position;
            CreateNodeView(node);
            suppressGraphChanged = false;
            RebuildEdges();
        }

        private void RebuildEdges()
        {
            suppressGraphChanged = true;

            var edgeList = edges.ToList();
            foreach (var edge in edgeList)
                RemoveElement(edge);

            CreateEdgesFromData();

            suppressGraphChanged = false;
        }

        private void CreateEdgesFromData()
        {
            if (graph?.nodes == null) return;

            foreach (var node in graph.nodes)
            {
                var sourceView = GetNodeView(node);
                if (sourceView == null) continue;

                if (node.nodeType == DialogueNodeType.Dialogue && node.choices != null)
                {
                    foreach (var choice in node.choices)
                        TryCreateEdge(sourceView.GetChoicePort(choice), choice?.nextNodeId);
                }
                else if (node.nodeType == DialogueNodeType.Condition)
                {
                    TryCreateEdge(sourceView.GetSpecialPort(DialoguePortKind.True), node.trueNodeId);
                    TryCreateEdge(sourceView.GetSpecialPort(DialoguePortKind.False), node.falseNodeId);
                }
                else if (node.nodeType != DialogueNodeType.End)
                {
                    TryCreateEdge(sourceView.GetSpecialPort(DialoguePortKind.Next), node.nextNodeId);
                }
            }
        }

        private void TryCreateEdge(Port output, string targetNodeId)
        {
            if (output == null || string.IsNullOrWhiteSpace(targetNodeId)) return;

            var targetNode = graph.GetNode(targetNodeId);
            var targetView = GetNodeView(targetNode);
            if (targetView == null) return;

            AddElement(output.ConnectTo(targetView.InputPort));
        }

        private DialogueNodeView GetNodeView(DialogueNodeData node)
        {
            if (node == null) return null;
            return nodeViews.TryGetValue(node, out var view) ? view : null;
        }

        private bool IsStartNode(DialogueNodeData node)
        {
            return graph != null && node != null && graph.startNodeId == node.nodeId;
        }

        private void RefreshNodeTitles()
        {
            foreach (var pair in nodeViews)
                pair.Value.RefreshTitle(IsStartNode(pair.Key));
        }

        private void EnsureValidGraph()
        {
            graph.nodes ??= new List<DialogueNodeData>();

            if (graph.nodes.Count == 0)
            {
                graph.nodes.Add(new DialogueNodeData
                {
                    nodeId = "start",
                    nodeType = DialogueNodeType.Dialogue,
                    lineKey = "dialogue.start",
                    editorPosition = new Vector2(260f, 180f),
                    choices = new List<DialogueChoiceData>()
                });
            }

            foreach (var node in graph.nodes)
            {
                if (node == null) continue;
                if (string.IsNullOrWhiteSpace(node.nodeId))
                    node.nodeId = MakeUniqueNodeId("node");

                node.choices ??= new List<DialogueChoiceData>();
            }

            if (string.IsNullOrWhiteSpace(graph.startNodeId) || graph.GetNode(graph.startNodeId) == null)
                graph.startNodeId = graph.nodes[0].nodeId;
        }

        private string MakeUniqueNodeId(string baseName)
        {
            int index = 1;
            string candidate = baseName;
            while (graph.GetNode(candidate) != null)
            {
                candidate = $"{baseName}_{index}";
                index++;
            }
            return candidate;
        }

        private string MakeUniqueChoiceId(DialogueNodeData node)
        {
            int index = 1;
            string candidate = "choice";
            while (node.choices.Exists(choice => choice != null && choice.id == candidate))
            {
                candidate = $"choice_{index}";
                index++;
            }
            return candidate;
        }
    }

    public sealed class DialogueNodeView : Node
    {
        public static readonly Vector2 DefaultSize = new(380f, 260f);

        private readonly DialogueGraphView graphView;
        private readonly Dictionary<DialogueChoiceData, Port> choicePorts = new();
        private readonly Dictionary<DialoguePortKind, Port> specialPorts = new();

        public DialogueNodeData Data { get; }
        public Port InputPort { get; private set; }

        public DialogueNodeView(DialogueGraphView graphView, DialogueNodeData data, bool isStart)
        {
            this.graphView = graphView;
            Data = data;

            viewDataKey = data.nodeId;
            RefreshTitle(isStart);
            AddToClassList($"dialogue-node-{data.nodeType.ToString().ToLowerInvariant()}");

            BuildInputPort();
            BuildHeaderFields();
            BuildNodeTypeFields();
            BuildFooter();

            RefreshExpandedState();
            RefreshPorts();
        }

        public Port GetChoicePort(DialogueChoiceData choice)
        {
            if (choice == null) return null;
            return choicePorts.TryGetValue(choice, out var port) ? port : null;
        }

        public Port GetSpecialPort(DialoguePortKind kind)
        {
            return specialPorts.TryGetValue(kind, out var port) ? port : null;
        }

        public void RefreshTitle(bool isStart)
        {
            title = isStart ? $"[Start] {Data.nodeId}" : $"{Data.nodeType}: {Data.nodeId}";
        }

        private void BuildInputPort()
        {
            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "Input";
            inputContainer.Add(InputPort);
        }

        private void BuildHeaderFields()
        {
            mainContainer.Add(CreateTextField("Node Id", Data.nodeId, value => graphView.RenameNode(Data, value)));

            var typeField = new EnumField("Type", Data.nodeType);
            typeField.RegisterValueChangedCallback(evt => graphView.ChangeNodeType(Data, (DialogueNodeType)evt.newValue));
            mainContainer.Add(typeField);
        }

        private void BuildNodeTypeFields()
        {
            switch (Data.nodeType)
            {
                case DialogueNodeType.Dialogue:
                    BuildDialogueFields();
                    break;
                case DialogueNodeType.Condition:
                    BuildConditionFields();
                    break;
                case DialogueNodeType.Event:
                    BuildEventFields();
                    BuildNextPort("Next");
                    break;
                case DialogueNodeType.Audio:
                    BuildAudioFields();
                    BuildNextPort("Next");
                    break;
                case DialogueNodeType.Portrait:
                    BuildPortraitFields();
                    BuildNextPort("Next");
                    break;
                case DialogueNodeType.End:
                    mainContainer.Add(new HelpBox("End node closes the current dialogue path.", HelpBoxMessageType.Info));
                    break;
            }
        }

        private void BuildDialogueFields()
        {
            mainContainer.Add(CreateTextField("Speaker Key", Data.speakerNameKey, value =>
                graphView.ChangeField(() => Data.speakerNameKey = value, "Change Dialogue Speaker Key")));

            mainContainer.Add(CreateTextField("Line Key", Data.lineKey, value =>
                graphView.ChangeField(() => Data.lineKey = value, "Change Dialogue Line Key")));

            mainContainer.Add(CreateTextField("Portrait Key", Data.portraitKey, value =>
                graphView.ChangeField(() => Data.portraitKey = value, "Change Dialogue Portrait Key")));

            mainContainer.Add(CreateEnumField("Portrait Slot", Data.portraitSlot, value =>
                graphView.ChangeField(() => Data.portraitSlot = (DialoguePortraitSlot)value, "Change Dialogue Portrait Slot")));

            mainContainer.Add(CreateTextField("Emotion Key", Data.emotionKey, value =>
                graphView.ChangeField(() => Data.emotionKey = value, "Change Dialogue Emotion Key")));

            if (Data.choices != null)
            {
                foreach (var choice in Data.choices)
                    AddChoiceRow(choice);
            }
        }

        private void BuildConditionFields()
        {
            mainContainer.Add(CreateTextField("Condition Key", Data.conditionKey, value =>
                graphView.ChangeField(() => Data.conditionKey = value, "Change Dialogue Condition Key")));

            mainContainer.Add(CreateEnumField("Compare", Data.compareMode, value =>
                graphView.ChangeField(() => Data.compareMode = (DialogueCompareMode)value, "Change Dialogue Compare Mode")));

            mainContainer.Add(CreateTextField("Compare Value", Data.compareValue, value =>
                graphView.ChangeField(() => Data.compareValue = value, "Change Dialogue Compare Value")));

            AddSpecialPort(DialoguePortKind.True, "True");
            AddSpecialPort(DialoguePortKind.False, "False");
        }

        private void BuildEventFields()
        {
            mainContainer.Add(CreateTextField("Event Key", Data.eventKey, value =>
                graphView.ChangeField(() => Data.eventKey = value, "Change Dialogue Event Key")));

            mainContainer.Add(CreateTextField("Payload", Data.eventPayload, value =>
                graphView.ChangeField(() => Data.eventPayload = value, "Change Dialogue Event Payload")));
        }

        private void BuildAudioFields()
        {
            mainContainer.Add(CreateTextField("Audio Key", Data.audioKey, value =>
                graphView.ChangeField(() => Data.audioKey = value, "Change Dialogue Audio Key")));

            mainContainer.Add(CreateEnumField("Audio Mode", Data.audioMode, value =>
                graphView.ChangeField(() => Data.audioMode = (DialogueAudioMode)value, "Change Dialogue Audio Mode")));
        }

        private void BuildPortraitFields()
        {
            mainContainer.Add(CreateTextField("Portrait Key", Data.portraitKey, value =>
                graphView.ChangeField(() => Data.portraitKey = value, "Change Dialogue Portrait Key")));

            mainContainer.Add(CreateEnumField("Portrait Slot", Data.portraitSlot, value =>
                graphView.ChangeField(() => Data.portraitSlot = (DialoguePortraitSlot)value, "Change Dialogue Portrait Slot")));

            mainContainer.Add(CreateTextField("Emotion Key", Data.emotionKey, value =>
                graphView.ChangeField(() => Data.emotionKey = value, "Change Dialogue Emotion Key")));
        }

        private void BuildNextPort(string label)
        {
            AddSpecialPort(DialoguePortKind.Next, label);
        }

        private void BuildFooter()
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginTop = 6f
                }
            };

            if (Data.nodeType == DialogueNodeType.Dialogue)
                row.Add(new Button(() => graphView.AddChoice(Data)) { text = "+ Choice" });

            row.Add(new Button(() => graphView.SetStartNode(Data)) { text = "Set Start" });
            row.Add(new Button(() => graphView.DeleteNode(Data)) { text = "Delete" });
            mainContainer.Add(row);
        }

        private void AddChoiceRow(DialogueChoiceData choice)
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginTop = 4f
                }
            };

            var textKey = new TextField("Choice")
            {
                value = choice.textKey
            };
            textKey.style.flexGrow = 1f;
            textKey.RegisterValueChangedCallback(evt =>
            {
                graphView.ChangeField(() =>
                {
                    choice.textKey = evt.newValue;
                    if (choicePorts.TryGetValue(choice, out var port))
                        port.portName = string.IsNullOrWhiteSpace(choice.textKey) ? "Choice" : choice.textKey;
                }, "Change Dialogue Choice Text Key");
            });
            row.Add(textKey);

            row.Add(new Button(() => graphView.RemoveChoice(Data, choice)) { text = "X" });

            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            port.portName = string.IsNullOrWhiteSpace(choice.textKey) ? "Choice" : choice.textKey;
            port.userData = new DialoguePortData(Data, DialoguePortKind.Choice, choice);
            choicePorts[choice] = port;

            var wrap = new VisualElement
            {
                style =
                {
                    minWidth = 110f
                }
            };
            wrap.Add(port);
            row.Add(wrap);
            outputContainer.Add(row);
        }

        private void AddSpecialPort(DialoguePortKind kind, string label)
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            port.portName = label;
            port.userData = new DialoguePortData(Data, kind, null);
            specialPorts[kind] = port;
            outputContainer.Add(port);
        }

        private TextField CreateTextField(string label, string value, System.Action<string> onChanged)
        {
            var field = new TextField(label)
            {
                value = value
            };
            field.RegisterValueChangedCallback(evt => onChanged?.Invoke(evt.newValue));
            return field;
        }

        private EnumField CreateEnumField(string label, System.Enum value, System.Action<System.Enum> onChanged)
        {
            var field = new EnumField(label, value);
            field.RegisterValueChangedCallback(evt => onChanged?.Invoke(evt.newValue));
            return field;
        }
    }

    internal sealed class DialoguePortData
    {
        public readonly DialogueNodeData node;
        public readonly DialoguePortKind kind;
        public readonly DialogueChoiceData choice;

        public DialoguePortData(DialogueNodeData node, DialoguePortKind kind, DialogueChoiceData choice)
        {
            this.node = node;
            this.kind = kind;
            this.choice = choice;
        }
    }

    public enum DialoguePortKind
    {
        Choice,
        Next,
        True,
        False
    }

    [CustomEditor(typeof(DialogueGraphData))]
    public sealed class DialogueGraphDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Open Graph Editor"))
                DialogueGraphEditorWindow.Open((DialogueGraphData)target);
        }
    }
}
