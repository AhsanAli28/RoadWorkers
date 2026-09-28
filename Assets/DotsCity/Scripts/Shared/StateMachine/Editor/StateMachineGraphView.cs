#if UNITY_EDITOR
using Spirit604.StateMachine.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spirit604.StateMachine.InternalEditor
{
    public class StateMachineGraphView : GraphView
    {
        public class EdgeDropListener : IEdgeConnectorListener
        {
            public void OnDrop(GraphView graphView, Edge edge) { }
            public void OnDropOutsidePort(Edge edge, Vector2 position) { }
        }

        readonly Rect entryPosition = new Rect(300, 200, 100, 150);
        private readonly Vector2 defaultNodeSize = new Vector2(150f, 200f);
        private readonly Vector2 defaultTransitionNodeSize = new Vector2(150f, 80f);
        private readonly Vector2 startNodePosition = new Vector2(450f, 200f);
        private readonly Vector2 startNodePosition2 = new Vector2(100f, 100f);
        private const float xLevelOffset = 500f;
        private const float yLevelOffset = 150f;
        private const float unlinkedYOffset = 100f;

        private Vector3 transitionOffset = new Vector3(200f, 100f);

        private StateNode entryPoint;
        private StateNode lastAddedNode;
        private List<StateBase> addedStates = new List<StateBase>();
        private List<List<StateBase>> treeList = new List<List<StateBase>>();

        public static StateMachine TargetStateMachine;

        public StateMachineGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            entryPoint = GenerateEntryPointNode();
            AddElement(entryPoint);
            LoadNodes();
        }

        private void InitTree(StateBase state)
        {
            if (state == null || state.addedToTree)
            {
                return;
            }

            state.addedToTree = true;

            int currentLevel = state.graphViewTreeLevel + 1;

            state.graphNodeIsCreated = false;

            if (treeList.Count <= currentLevel)
            {
                treeList.Add(new List<StateBase>());
            }

            int index = 0;

            while (state.Transitions.Count > index)
            {
                if (state.Transitions[index] == null)
                {
                    state.Transitions.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }

            for (int i = 0; i < state.Transitions?.Count; i++)
            {
                if (state.Transitions[i].OnSuccessState != null)
                {
                    state.Transitions[i].OnSuccessState.graphViewTreeLevel = currentLevel;

                    treeList[currentLevel].TryToAdd(state.Transitions[i].OnSuccessState);
                }

                if (state.Transitions[i].OnFailState != null)
                {
                    state.Transitions[i].OnFailState.graphViewTreeLevel = currentLevel;

                    treeList[currentLevel].TryToAdd(state.Transitions[i].OnFailState);
                }

                InitTree(state.Transitions[i].OnSuccessState);
                InitTree(state.Transitions[i].OnFailState);
            }
        }

        private void LoadNodes()
        {
            addedStates.Clear();

            if (TargetStateMachine == null)
            {
                return;
            }

            if (TargetStateMachine.InitialState)
            {
                var entryNode = CreateNode(TargetStateMachine.InitialState, startNodePosition);

                ConnectNodes(0, entryPoint, entryNode);

                addedStates.Add(TargetStateMachine.InitialState);
                CreateIndexTree(entryNode);

                entryNode.LoadPosition(startNodePosition);
                CreateNextConnectedNodes(entryNode);
            }

            var allStates = TargetStateMachine.GetComponentsInChildren<StateBase>().ToArray();
            var notAddedStates = allStates.Where(item => !addedStates.Contains(item)).ToList();

            for (int i = 0; i < notAddedStates.Count; i++)
            {
                Vector3 spawnPosition = startNodePosition2 + new Vector2(0, unlinkedYOffset * i);

                var node = CreateNode(notAddedStates[i], spawnPosition);

                node.LoadPosition(spawnPosition);
            }
        }

        private void CreateIndexTree(StateNode entryNode)
        {
            entryNode.state.graphViewTreeLevel = 0;
            treeList.Clear();
            treeList.Add(new List<StateBase>());
            treeList[0].Add(TargetStateMachine.InitialState);

            var states = TargetStateMachine.GetComponentsInChildren<StateBase>();

            states.Select(c => { c.addedToTree = false; return c; }).ToList();
            states.Select(c => { c.graphNodeIsCreated = false; return c; }).ToList();

            InitTree(TargetStateMachine.InitialState);
        }

        private void CreateNextConnectedNodes(StateNode entryNode)
        {
            if (entryNode == null)
            {
                return;
            }

            List<StateNode> createdNodes = new List<StateNode>();

            int index = 0;

            for (int i = 0; i < entryNode.state.Transitions?.Count; i++)
            {
                var nextSuccessState = entryNode.state.Transitions[i].OnSuccessState;
                var nextFailState = entryNode.state.Transitions[i].OnFailState;

                var node1 = CreateNextNode(i, entryNode, nextSuccessState, 0);

                if (node1 != null)
                {
                    index++;
                }

                var node2 = CreateNextNode(i, entryNode, nextFailState, 1);

                if (node2 != null)
                {
                    index++;
                }

                createdNodes.TryToAdd(node1);
                createdNodes.TryToAdd(node2);
            }

            for (int i = 0; i < createdNodes?.Count; i++)
            {
                CreateNextConnectedNodes(createdNodes[i]);
            }
        }

        private StateNode CreateNextNode(int transitionIndex, StateNode entryNode, StateBase connectedState, int conditionIndex)
        {
            if (connectedState != null)
            {
                addedStates.TryToAdd(connectedState);
            }
            else
            {
                return null;
            }

            int lvlIndex = connectedState.graphViewTreeLevel;
            float nodeIndex = treeList[lvlIndex].IndexOf(connectedState);
            float lvlCount = treeList[lvlIndex].Count;

            float addOffset = lvlCount % 2 == 0 ? yLevelOffset / 2 : 0;
            var newPosition = new Vector2(entryNode.position.x, startNodePosition.y) + new Vector2(xLevelOffset, yLevelOffset * (nodeIndex - Mathf.FloorToInt(lvlCount / 2)) + addOffset);

            StateNode connectedNode = null;

            float transitionCount = entryNode.state.Transitions.Count;
            float addOffset2 = transitionCount % 2 == 0 ? transitionOffset.y / 2 : 0;

            var transitionPosition = new Vector2(entryNode.position.x, entryNode.position.y) + new Vector2(transitionOffset.x, transitionOffset.y * (transitionIndex - Mathf.FloorToInt(transitionCount / 2)) + addOffset2);

            StateNode transitionNode = GetTransitionNode(transitionIndex, entryNode, transitionPosition);

            if (!connectedState.graphNodeIsCreated)
            {
                connectedState.graphNodeIsCreated = true;
                connectedNode = CreateNode(connectedState, newPosition);

                connectedNode.LoadPosition(newPosition);
                ConnectTransition(conditionIndex, transitionNode, connectedNode);
            }
            else
            {
                connectedNode = FindNode(connectedState);
                ConnectTransition(conditionIndex, transitionNode, connectedNode);
                connectedNode = null;
            }

            return connectedNode;
        }

        private StateNode GetTransitionNode(int index, StateNode entryNode, Vector2 position)
        {
            StateNode transition = null;

            if (entryNode.transitions.Count > index)
            {
                transition = entryNode.transitions.ElementAt(index);
            }

            if (transition == null)
            {
                AddTransitionPort(entryNode);
                transition = CreateTransitionNode(entryNode.state.Transitions[index].TransitionCondition.name, position);
                var edge = entryNode.ports[index].ConnectTo(transition.entryPort);

                AddElement(edge);
                entryNode.transitions.Add(transition);
            }

            return transition;
        }

        private StateNode FindNode(StateBase state)
        {
            var StateNodes = nodes.ToList().Select(item => item as StateNode).ToList();

            var node = StateNodes.Where(item => item.state == state).FirstOrDefault();

            return node;
        }

        private void ConnectTransition(int conditionIndex, StateNode transition, StateNode targetNode)
        {
            Port port = null;

            if (transition.ports.Count < conditionIndex)
            {
                port = AddConditionPort(transition, conditionIndex);
            }
            else
            {
                port = transition.ports[conditionIndex];
            }

            var edge = port.ConnectTo(targetNode.entryPort);

            AddElement(edge);
        }

        private void ConnectNodes(int connectionIndex, StateNode sourceNode, StateNode targetNode)
        {
            Edge edge = null;

            if (connectionIndex != -1)
            {
                edge = sourceNode.ports[connectionIndex].ConnectTo(targetNode.entryPort);
            }
            else
            {
                var port = sourceNode.ports.Where(item => item.connected == false).FirstOrDefault();

                if (port == null)
                {
                    port = sourceNode.ports[sourceNode.ports.Count - 1];
                }

                if (port == null)
                {
                    UnityEngine.Debug.Log("port is null");
                }

                edge = port.ConnectTo(targetNode.entryPort);
            }

            AddElement(edge);
        }

        private Port GeneratePort(StateNode node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single, Orientation orientation = Orientation.Horizontal)
        {
            return node.InstantiatePort(orientation, portDirection, capacity, typeof(float));
        }

        private StateNode GenerateEntryPointNode()
        {
            var node = new StateNode()
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

            node.SetPosition(entryPosition);

            return node;
        }

        private StateNode CreateTransitionNode(string name, Vector2 position)
        {
            var node = new StateNode()
            {
                title = name,
                GUID = Guid.NewGuid().ToString(),
            };

            var generatedPort = GeneratePort(node, Direction.Input);
            generatedPort.portName = "Input";
            node.inputContainer.Add(generatedPort);
            node.entryPort = generatedPort;

            AddConditionPort(node, 0);
            AddConditionPort(node, 1);

            node.RefreshExpandedState();
            node.RefreshPorts();

            Rect rect = new Rect(position, defaultTransitionNodeSize);
            node.position = position;
            node.SetPosition(rect);

            AddElement(node);

            return node;
        }

        public StateNode CreateNode(StateBase stateBase, Vector2 position)
        {
            var node = CreateStateNode(stateBase, position);

            AddElement(node);

            return node;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            ports.ForEach((port)
               =>
           {
               if (startPort != port && startPort.node != port.node)
               {
                   compatiblePorts.Add(port);
               }
           });

            return compatiblePorts;
        }

        public StateNode CreateStateNode(StateBase stateBase, Vector2 position)
        {
            var StateNode = new StateNode()
            {
                title = stateBase.name,
                GUID = Guid.NewGuid().ToString()
            };

            StateNode.SetPosition(new Rect(position, defaultNodeSize));
            lastAddedNode = StateNode;

            var inputPort = GeneratePort(StateNode, Direction.Input, Port.Capacity.Multi);
            inputPort.portName = "Input";

            StateNode.inputContainer.Add(inputPort);

            StateNode.entryPort = inputPort;
            StateNode.position = position;
            StateNode.state = stateBase;

            StateNode.RefreshExpandedState();
            StateNode.RefreshPorts();

            return StateNode;
        }

        private void RemoveNode(StateNode StateNode)
        {
            var entryPort = StateNode.entryPort;

            var connectionEdges = entryPort.connections.ToList();

            for (int i = 0; i < connectionEdges.Count; i++)
            {
                var node = connectionEdges[i].output.node as StateNode;
                connectionEdges[i].output.Disconnect(connectionEdges[i]);

                RemoveElement(connectionEdges[i]);

                node.RefreshPorts();
            }

            RemoveElement(StateNode);
            MarkDirtyRepaint();
        }

        private Port AddTransitionPort(StateNode StateNode, string portName = "Transition")
        {
            var generatedPort = GeneratePort(StateNode, Direction.Output);

            var outputPortCount = StateNode.outputContainer.Query("connector").ToList().Count;

            generatedPort.portName = portName;
            StateNode.outputContainer.Add(generatedPort);
            StateNode.ports.Add(generatedPort);
            StateNode.RefreshPorts();
            StateNode.RefreshExpandedState();

            generatedPort.AddManipulator(new EdgeConnector<Edge>(new EdgeDropListener()));

            return generatedPort;
        }

        private Port AddConditionPort(StateNode StateNode, int conditionIndex)
        {
            var generatedPort = GeneratePort(StateNode, Direction.Output);

            string portName = GetTransitionPortName(conditionIndex);

            generatedPort.portName = portName;
            StateNode.outputContainer.Add(generatedPort);
            StateNode.ports.Add(generatedPort);
            StateNode.RefreshPorts();
            StateNode.RefreshExpandedState();

            return generatedPort;
        }

        private string GetTransitionPortName(int conditionIndex)
        {
            switch (conditionIndex)
            {
                case 0:
                    {
                        return "OnSuccess";
                    }
                case 1:
                    {
                        return "OnFail";
                    }
                default:
                    {
                        break;
                    }
            }

            return "";
        }

        private void RemovePort(Node node, Port socket)
        {
            var targetEdge = edges.ToList().Where(x => x.output.portName == socket.portName && x.output.node == socket.node);

            if (targetEdge.Any())
            {
                var edge = targetEdge.First();
                edge.input.Disconnect(edge);
                RemoveElement(targetEdge.First());

                RemoveConnection(edge);
            }

            (node as StateNode).TryToRemovePort(socket);
            node.outputContainer.Remove(socket);
            node.RefreshPorts();
            node.RefreshExpandedState();
        }

        private void RemoveConnection(Edge edge)
        {
            var sourceNode = edge.output.node as StateNode;
            var targetNode = edge.input.node as StateNode;
        }
    }
}
#endif
