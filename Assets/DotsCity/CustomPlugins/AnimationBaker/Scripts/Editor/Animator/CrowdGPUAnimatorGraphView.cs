#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Label = UnityEngine.UIElements.Label;

namespace Spirit604.AnimationBaker.EditorInternal
{
    public class CrowdGPUAnimatorGraphView : GraphView
    {
        #region Constans

        private const float TextFieldLabelSize = 50f;
        private readonly Rect entryPosition = new Rect(300, 200, 100, 150);
        private readonly Vector2 defaultNodeSize = new Vector2(200, 200f);
        private readonly Vector2 defaultTransitionNodeSize = new Vector2(200, 80f);

        #endregion

        #region Variables

        private NodeView entryPoint;
        private EditorWindow editorWindow;
        private NodeSearchWindow searchWindow;

        private AnimatorDataContainer animatorDataContainer;
        private AnimationCollectionContainer animationCollection;
        private Dictionary<string, NodeView> createdNodes = new Dictionary<string, NodeView>();
        private List<string> animationNames;

        #endregion

        #region Constructor & init

        public CrowdGPUAnimatorGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            graphViewChanged = OnGraphChange;
        }

        public void Initialize(EditorWindow editorWindow, AnimatorDataContainer animatorDataContainer, AnimationCollectionContainer animationCollection)
        {
            this.editorWindow = editorWindow;
            this.animatorDataContainer = animatorDataContainer;
            this.animationCollection = animationCollection;
            animationNames = animationCollection.GetAnimationNames().ToList();

            AddSearchWindow();
        }

        #endregion

        #region Overriden methods

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            ports.ForEach((port) =>
            {
                if (startPort != port && startPort.node != port.node && (!(port.node as NodeView).transitionNode | !(startPort.node as NodeView).transitionNode))
                {
                    compatiblePorts.Add(port);
                }
            });

            return compatiblePorts;
        }

        #endregion

        #region Public methods

        public void UpdateGraph()
        {
            ClearGraph();
            LoadNodes();
        }

        public NodeView CreateAnimationNode(Vector2 position)
        {
            var animationNode = "Animation Node";

            var guid = Guid.NewGuid().ToString();
            var createdNode = animatorDataContainer.GenerateAnimationNode(animationNode, guid) as AnimationNodeData;

            return CreateAnimationNode(animationNode, guid, createdNode, position);
        }

        public NodeView CreateAnimationNode(string name, string guid, AnimationNodeData createdNode, Vector2 position)
        {
            var node = new NodeView()
            {
                title = "Animation Node",
                GUID = guid,
            };

            node.RelatedNode = createdNode;
            createdNode.AnimatorNodePosition = position;

            Rect rect = new Rect(position, defaultNodeSize);
            node.position = position;
            node.SetPosition(rect);

            var inputPort = GeneratePort(node, Direction.Input, Port.Capacity.Multi);
            inputPort.portName = "Input";

            node.inputContainer.Add(inputPort);

            var outputPort = GeneratePort(node, Direction.Output, Port.Capacity.Multi);

            node.outputContainer.Add(outputPort);
            node.ports.Add(outputPort);

            AddDeleteButton(node);
            AddAssetNameField(node);

            var animationNametextField = new TextField("Anim Name");
            var animationNametextLabel = animationNametextField.Q<Label>(className: TextField.labelUssClassName);
            animationNametextLabel.style.minWidth = TextFieldLabelSize;
            animationNametextField.value = createdNode.AnimName;

            animationNametextField.RegisterValueChangedCallback(newStr =>
            {
                var relatedNode = node.RelatedNode;

                if (relatedNode != null && relatedNode is AnimationNodeData)
                {
                    ((AnimationNodeData)relatedNode).SetAnimName(newStr.newValue);
                }
            });

            node.entryPort = inputPort;
            node.position = position;

            node.RefreshExpandedState();
            node.RefreshPorts();
            node.RelatedNode.AnimatorNodePosition = position;

            int localAnimIndex = 0;

            var animNode = (node.RelatedNode as AnimationNodeData);

            if (animationNames.Contains(animNode.AnimName))
            {
                localAnimIndex = animationNames.IndexOf(animNode.AnimName);
            }
            else
            {
                animNode.SetAnimName(animationNames[0]);
            }

            if (string.IsNullOrEmpty(animationNametextField.value))
            {
                animationNametextField.value = animationNames[localAnimIndex];
            }

            var animationPopupField = new DropdownField(animationNames, localAnimIndex);

            animationPopupField.RegisterValueChangedCallback(choise =>
            {
                if (node.RelatedNode)
                {
                    var animNode = (node.RelatedNode as AnimationNodeData);

                    animNode.SetAnimName(choise.newValue);
                    animationNametextField.value = choise.newValue;
                }
            });

            var toggle = new Toggle("Unique Animation");
            toggle.value = createdNode.UniqueAnimation;

            toggle.RegisterValueChangedCallback(toggleEvent =>
            {
                if (node.RelatedNode != null && node.RelatedNode is AnimationNodeData)
                {
                    (node.RelatedNode as AnimationNodeData).UniqueAnimation = toggleEvent.newValue;
                }
            });

            node.mainContainer.Add(animationNametextField);
            node.mainContainer.Add(animationPopupField);
            node.mainContainer.Add(toggle);

#if UNITY_EDITOR
            EditorUtility.SetDirty(createdNode);
#endif

            AddElement(node);

            return node;
        }

        public NodeView CreateTransitionNode(Vector2 position)
        {
            var transitionNode = "Transition Node";
            var guid = Guid.NewGuid().ToString();
            var createdNode = animatorDataContainer.GenerateTransitionNode(transitionNode, guid) as TransitionNodeData;

            return CreateTransitionNode(transitionNode, guid, createdNode, position);
        }

        public NodeView CreateTransitionNode(string name, string guid, TransitionNodeData createdNode, Vector2 position)
        {
            var node = new NodeView()
            {
                title = "Transition Node",
                GUID = guid,
                transitionNode = true
            };

            node.RelatedNode = createdNode;
            createdNode.AnimatorNodePosition = position;

            AddDeleteButton(node);
            AddAssetNameField(node);

            var generatedPort = GeneratePort(node, Direction.Input);
            generatedPort.portName = "Input";
            node.inputContainer.Add(generatedPort);

            node.entryPort = generatedPort;

            AddOutputPort(node);

            node.RefreshExpandedState();
            node.RefreshPorts();

            Rect rect = new Rect(position, defaultTransitionNodeSize);
            node.position = position;
            node.SetPosition(rect);

            var choises = Enum.GetNames(typeof(AnimationTransitionType)).ToList();
            var defaultValue = choises[0];

            var transitionDurationField = new FloatField("Duration");
            var transitionDurationFieldLabel = transitionDurationField.Q<Label>(className: FloatField.labelUssClassName);
            transitionDurationFieldLabel.style.minWidth = 70f;

            transitionDurationField.value = createdNode.TransitionDuration;
            transitionDurationField.RegisterValueChangedCallback(floatEvent =>
            {
                if (node.RelatedNode)
                {
                    (node.RelatedNode as TransitionNodeData).TransitionDuration = floatEvent.newValue;
                }
            });

            int selectedIndex = choises.IndexOf(createdNode.AnimationTransitionType.ToString());
            var transitionTypePopupField = new DropdownField(choises, selectedIndex);

            transitionDurationField.visible = createdNode.AnimationTransitionType != AnimationTransitionType.Default;

            transitionTypePopupField.RegisterValueChangedCallback(choise =>
            {
                if (node.RelatedNode)
                {
                    var newEnumValue = (AnimationTransitionType)Enum.Parse(typeof(AnimationTransitionType), choise.newValue);
                    (node.RelatedNode as TransitionNodeData).AnimationTransitionType = newEnumValue;
                    transitionDurationField.visible = newEnumValue != AnimationTransitionType.Default;
                }
            });

            node.mainContainer.Add(transitionTypePopupField);
            node.mainContainer.Add(transitionDurationField);

#if UNITY_EDITOR
            EditorUtility.SetDirty(createdNode);
#endif

            AddElement(node);

            return node;
        }

        public void ClearGraph()
        {
            this.graphElements.ForEach(graphElement => RemoveElement(graphElement));
            createdNodes.Clear();
        }

        #endregion

        #region Private methods

        private void LoadNodes()
        {
            if (animatorDataContainer.LayerCount == 0)
            {
                animatorDataContainer.AddLayer();
                animatorDataContainer.SelectedLayerIndex = 0;
            }

            var layerData = animatorDataContainer.GetSelectedLayerData();

            var allNodes = layerData.AllNodes;

            for (int i = 0; i < allNodes.Count; i++)
            {
                if (allNodes[i] && !createdNodes.ContainsKey(allNodes[i].Guid))
                {
                    CreateNodeView(allNodes[i]);
                }
            }

            entryPoint = GenerateEntryPointNode();
            AddElement(entryPoint);

            var entryNode = animatorDataContainer.GetEntryNode();

            if (entryNode && createdNodes.ContainsKey(entryNode.Guid))
            {
                var entryNodeView = createdNodes[entryNode.Guid];
                ConnectNodes(entryPoint, entryNodeView);
            }

            createdNodes.Clear();
        }

        private NodeView CreateNodeView(NodeData nodeData)
        {
            if (nodeData == null)
            {
                return null;
            }

            if (createdNodes.ContainsKey(nodeData.Guid))
            {
                return createdNodes[nodeData.Guid];
            }

            NodeView nodeView = null;

            if (nodeData is AnimationNodeData)
            {
                nodeView = CreateAnimationNode(nodeData.name, nodeData.Guid, nodeData as AnimationNodeData, nodeData.AnimatorNodePosition);
            }
            else if (nodeData is TransitionNodeData)
            {
                nodeView = CreateTransitionNode(nodeData.name, nodeData.Guid, nodeData as TransitionNodeData, nodeData.AnimatorNodePosition);
            }

            createdNodes.Add(nodeData.Guid, nodeView);

            for (int i = 0; i < nodeData.ConnectedNodes.Count; i++)
            {
                var connectedNode = nodeData.ConnectedNodes[i];

                if (connectedNode != null)
                {
                    var connectedNodeView = CreateNodeView(connectedNode);

                    ConnectNodes(nodeView, connectedNodeView);
                }
            }

            return nodeView;
        }

        private void AddSearchWindow()
        {
            searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
            searchWindow.Initialize(editorWindow, this);
            nodeCreationRequest = context => SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindow);
        }

        private NodeView GenerateEntryPointNode()
        {
            var node = new NodeView()
            {
                title = "START",
                GUID = Guid.NewGuid().ToString(),
                EntryPoint = true
            };

            var generatedPort = GeneratePort(node, Direction.Output);
            generatedPort.portName = "Next";
            node.outputContainer.Add(generatedPort);
            node.ports.Add(generatedPort);

            node.RefreshExpandedState();
            node.RefreshPorts();

            var triggerNameTextField = new TextField();

            var hashTextField = new TextField()
            {
                isReadOnly = true
            };

            var transitionLayerData = animatorDataContainer.GetSelectedLayerData();
            triggerNameTextField.value = transitionLayerData.ActivateTrigger;
            hashTextField.value = transitionLayerData.ActivateTriggerHash.ToString();

            triggerNameTextField.RegisterValueChangedCallback(newStr =>
            {
                var layerData = animatorDataContainer.GetSelectedLayerData();

                layerData.SetTriggerName(newStr.newValue);

                hashTextField.value = layerData.ActivateTriggerHash.ToString();

#if UNITY_EDITOR
                EditorUtility.SetDirty(animatorDataContainer);
#endif
            });

            node.mainContainer.Add(triggerNameTextField);
            node.mainContainer.Add(hashTextField);

            node.SetPosition(entryPosition);

            var position = animatorDataContainer.GetEntryPointPosition();

            if (position != Vector2.zero)
            {
                node.SetPosition(position);
            }

            return node;
        }

        private void RemoveNode(NodeView node)
        {
            var entryPort = node.entryPort;

            DisconnectPort(entryPort, true);

            var ports = node.ports;

            for (int i = 0; i < ports?.Count; i++)
            {
                Port port = ports[i];
                DisconnectPort(port, false);
            }

            animatorDataContainer.TryToRemoveNode(node.GUID);
            RemoveElement(node);
            MarkDirtyRepaint();
        }

        private void DisconnectPort(Port port, bool output)
        {
            if (port == null)
            {
                return;
            }

            var connectionEdges = port.connections.ToList();

            for (int i = 0; i < connectionEdges.Count; i++)
            {
                NodeView connectedNode = null;

                if (output)
                {
                    connectedNode = connectionEdges[i].output.node as NodeView;
                    connectionEdges[i].output.Disconnect(connectionEdges[i]);

                }
                else
                {
                    connectedNode = connectionEdges[i].input.node as NodeView;
                    connectionEdges[i].input.Disconnect(connectionEdges[i]);
                }

                RemoveElement(connectionEdges[i]);

                connectedNode.RefreshPorts();
            }
        }

        private Edge ConnectNodes(NodeView sourceNode, NodeView targetNode)
        {
            var edge = sourceNode.ports[0].ConnectTo(targetNode.entryPort);
            AddElement(edge);

            return edge;
        }

        private void AddDeleteButton(NodeView node)
        {
            var deleteButton = new Button(() => { RemoveNode(node); });
            deleteButton.text = "X";
            node.titleContainer.Add(deleteButton);
        }

        private void AddAssetNameField(NodeView node)
        {
            var nodeNameField = new TextField("Asset Name");
            var nodeNameFieldLabel = nodeNameField.Q<Label>(className: TextField.labelUssClassName);
            nodeNameFieldLabel.style.minWidth = TextFieldLabelSize;
            nodeNameField.value = node.RelatedNode.name;

            nodeNameField.RegisterValueChangedCallback(stringEvent =>
            {
                if (node.RelatedNode)
                {
                    node.RelatedNode.SetAssetName(stringEvent.newValue);
                }
            });

            node.mainContainer.Add(nodeNameField);
        }

        private Port AddOutputPort(NodeView node, string portName = "")
        {
            var generatedPort = GeneratePort(node, Direction.Output);

            generatedPort.portName = portName;
            node.outputContainer.Add(generatedPort);
            node.ports.Add(generatedPort);
            node.RefreshPorts();
            node.RefreshExpandedState();

            return generatedPort;
        }

        private Port GeneratePort(NodeView node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single, Orientation orientation = Orientation.Horizontal)
        {
            return node.InstantiatePort(orientation, portDirection, capacity, typeof(float));
        }

        #endregion

        #region Event handlers

        private GraphViewChange OnGraphChange(GraphViewChange change)
        {
            if (change.edgesToCreate != null)
            {
                foreach (Edge edge in change.edgesToCreate)
                {
                    var sourceNode = (edge.output.node as NodeView);
                    var targetNode = (edge.input.node as NodeView);

                    if (!sourceNode.EntryPoint)
                    {
                        animatorDataContainer.Connect(sourceNode.RelatedNode, targetNode.RelatedNode);
                    }
                    else
                    {
                        animatorDataContainer.SetEntryPoint(targetNode.RelatedNode);
                    }
                }
            }

            if (change.elementsToRemove != null)
            {
                foreach (GraphElement e in change.elementsToRemove)
                {
                    if (e is NodeView)
                    {
                        var nodeView = e as NodeView;

                        animatorDataContainer.TryToRemoveNode(nodeView.GUID);
                    }

                    if (e is Edge)
                    {
                        var edge = e as Edge;
                        var sourceNode = (edge.output.node as NodeView);
                        var targetNode = (edge.input.node as NodeView);

                        if (!sourceNode.EntryPoint)
                        {
                            animatorDataContainer.Disconnect(sourceNode.RelatedNode, targetNode.RelatedNode);
                        }
                        else
                        {
                            animatorDataContainer.SetEntryPoint(null);
                        }
                    }
                }
            }

            if (change.movedElements != null)
            {
                foreach (GraphElement e in change.movedElements)
                {
                    if (e is NodeView)
                    {
                        var nodeView = e as NodeView;

                        if (nodeView.RelatedNode != null)
                        {
                            nodeView.RelatedNode.AnimatorNodePosition = e.GetPosition().position;
                        }

                        if (nodeView.EntryPoint)
                        {
                            animatorDataContainer.SetEntryPointPosition(e.GetPosition().position);
                        }
                    }
                }
            }

            return change;
        }

        #endregion
    }
}
#endif