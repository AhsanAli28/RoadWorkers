using Spirit604.CityEditor.Road;
using Spirit604.Collections.Dictionary;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.TrafficArea.Authoring
{
    [ExecuteInEditMode]
    public class TrafficAreaAuthoring : MonoBehaviour
    {
        #region Helper types

        public enum ButtonSelectType { Disabled = 0, RemoveNode = 1, SelectNode = 2 }

        public enum NodePlaceType { Default, ForceRightLane, ForceLeftLane }

        [Serializable]
        public class NodeSettingsData
        {
            public NodePlaceType PlaceType;
        }

        [Serializable]
        public class NodeData
        {
            public List<TrafficNode> Nodes = new List<TrafficNode>();
        }

        [Serializable]
        public class NodeDataDictionary : AbstractSerializableDictionary<TrafficAreaNodeType, NodeData> { }

        [Serializable]
        public class NodeSettingsDictionary : AbstractSerializableDictionary<TrafficAreaNodeType, NodeSettingsData> { }

        [Serializable]
        public class NodeColorDictionary : AbstractSerializableDictionary<TrafficAreaNodeType, Color> { }

        #endregion

        #region Serialized variables

        [Tooltip("Maximum number of cars in a queue (if the maximum number is exceeded the entrance node will be closed)")]
        [SerializeField][Range(0, 20)] private int maxQueueCount = 1;

        [Tooltip("Number of vehicles that can be let in at the entrance (1 value example: 1 enters vehicle - 1 exits - 1 enters - 1 exits)")]
        [SerializeField][Range(0, 20)] private int maxSkipEnterOrderCount = 1;

        [SerializeField]
        private NodeSettingsDictionary nodeSettingsDictionary = new NodeSettingsDictionary()
        {
            {
                TrafficAreaNodeType.Default, new NodeSettingsData()
                {
                    PlaceType = NodePlaceType.Default
                }
            },
            {
                TrafficAreaNodeType.Enter, new NodeSettingsData()
                {
                    PlaceType = NodePlaceType.Default
                }
            },
            {
                TrafficAreaNodeType.Queue, new NodeSettingsData()
                {
                    PlaceType = NodePlaceType.Default
                }
            },

            {
                TrafficAreaNodeType.Exit, new NodeSettingsData()
                {
                    PlaceType = NodePlaceType.Default
                }
            }
        };

        [Tooltip("Cars leave the TrafficArea on a queue basis")]
        [SerializeField] private bool hasExitOrder = true;

        [Tooltip("On/off visual connections")]
        [SerializeField] private bool drawConnection = true;

        [Tooltip("On/off connection lines to the traffic nodes")]
        [SerializeField] private bool drawConnectionLines;

        [Tooltip(
            "<b>Default</b> : a node which is included in the TrafficArea but does not belong to one of the types listed below\r\n\r\n" +
            "<b>Enter</b> : entrance node to the TrafficArea (if the maximum number of vehicles in the queue is exceeded, the node will be closed)\r\n\r\n" +
            "<b>Queue</b> : node in front of which a line of cars is waiting\r\n\r\n" +
            "<b>Exit</b> : when it passes this node, the car leaves the TrafficArea")]
        [SerializeField] private TrafficAreaNodeType showTrafficAreaNodeType = (TrafficAreaNodeType)(~0);

        [Tooltip("On/off display add buttons paths to TrafficArea.")]
        [SerializeField] private bool showSceneNodes;

        [Tooltip("Allow one node to have multiple types")]
        [SerializeField] private bool allowDuplicateNodes;

        [Tooltip("" +
          "<b>Remove node</b> : selected node will be removed from TrafficArea\r\n\r\n" +
          "<b>Select node</b> : selected node will be added to TrafficArea with the select New node type")]
        [SerializeField] private ButtonSelectType buttonSelectType;

        [Tooltip(
            "<b>Default</b> : a node which is included in the TrafficArea but does not belong to one of the types listed below\r\n\r\n" +
            "<b>Enter</b> : entrance node to the TrafficArea (if the maximum number of vehicles in the queue is exceeded, the node will be closed)\r\n\r\n" +
            "<b>Queue</b> : node in front of which a line of cars is waiting\r\n\r\n" +
            "<b>Exit</b> : when it passes this node, the car leaves the TrafficArea")]
        [EnumPopup][SerializeField] private TrafficAreaNodeType newNodeType = TrafficAreaNodeType.Default;

        [SerializeField] private TrafficNode selectedNode;
        [SerializeField] private int buttonSelectIndex = 0;
        [SerializeField] private int newNodeTypeIndex = 0;

        [SerializeField]
        private NodeColorDictionary nodeColorDictionary = new NodeColorDictionary()
        {
            { TrafficAreaNodeType.Default, Color.yellow },
            { TrafficAreaNodeType.Enter, Color.green },
            { TrafficAreaNodeType.Queue, Color.blue },
            { TrafficAreaNodeType.Exit, Color.cyan },
        };

        [SerializeField] private NodeDataDictionary nodeData = new NodeDataDictionary();

        [SerializeField] private RoadSegmentCreator roadSegmentCreator;

        #endregion

        #region Properties

        public int MaxQueueCount { get => maxQueueCount; set => maxQueueCount = value; }

        public bool HasExitOrder { get => hasExitOrder; set => hasExitOrder = value; }

        public int MaxSkipEnterOrderCount { get => maxSkipEnterOrderCount; set => maxSkipEnterOrderCount = value; }

        public bool DrawConnection { get => drawConnection; set => drawConnection = value; }

        public bool DrawConnectionLines { get => drawConnectionLines; set => drawConnectionLines = value; }

        public TrafficAreaNodeType ShowTrafficAreaNodeType { get => showTrafficAreaNodeType; set => showTrafficAreaNodeType = value; }

        public ButtonSelectType SelectType { get => buttonSelectType; set => buttonSelectType = value; }

        public bool ShowSceneNodes { get => showSceneNodes; set => showSceneNodes = value; }

        public TrafficAreaNodeType NewNodeType { get => newNodeType; set => newNodeType = value; }

        public TrafficNode SelectedNode { get => selectedNode; set => selectedNode = value; }

        public int ButtonSelectIndex
        {
            get
            {
                return buttonSelectIndex;
            }
            set
            {
                if (value != buttonSelectIndex)
                {
                    buttonSelectIndex = value;
                    buttonSelectType = (ButtonSelectType)(buttonSelectIndex);
                }
            }
        }

        public int NewNodeTypeIndex
        {
            get
            {
                return newNodeTypeIndex;
            }
            set
            {
                if (value != newNodeTypeIndex)
                {
                    newNodeTypeIndex = value;
                    newNodeType = (TrafficAreaNodeType)(1 << newNodeTypeIndex);
                }
            }
        }

        public bool AllowDuplicateNodes => allowDuplicateNodes;

        #endregion

        #region Unity lifecycle

#if UNITY_EDITOR

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

#endif

        #endregion

        #region Methods

        public bool HasNode(TrafficNode trafficNode)
        {
            foreach (var data in nodeData)
            {
                if (data.Value.Nodes.Contains(trafficNode))
                {
                    return true;
                }
            }

            return false;
        }

        public void AddNode(TrafficNode trafficNode, TrafficAreaNodeType trafficAreaNodeType)
        {
            if (!nodeData.ContainsKey(trafficAreaNodeType))
            {
                nodeData.Add(trafficAreaNodeType, new NodeData()
                {

                });
            }

            if (!allowDuplicateNodes)
            {
                RemoveNode(trafficNode);
            }

            nodeData[trafficAreaNodeType].Nodes.TryToAdd(trafficNode);

            AssignSegment(trafficNode);

            EditorSaver.SetObjectDirty(this);
        }

        public List<TrafficNode> TryToGetNodes(TrafficAreaNodeType trafficAreaNodeType)
        {
            if (nodeData.TryGetValue(trafficAreaNodeType, out var data))
            {
                return data.Nodes;
            }

            return null;
        }

        public void RemoveNode(TrafficNode trafficNode)
        {
            foreach (var data in nodeData)
            {
                data.Value.Nodes.TryToRemove(trafficNode);
            }

            EditorSaver.SetObjectDirty(this);
        }

        public Color GetNodeColor(TrafficAreaNodeType trafficAreaNodeType)
        {
            if (nodeColorDictionary.TryGetValue(trafficAreaNodeType, out var color))
            {
                return color;
            }

            return Color.white;
        }

        public NodePlaceType GetNodePlace(TrafficAreaNodeType trafficAreaNodeType)
        {
            if (nodeSettingsDictionary.ContainsKey(trafficAreaNodeType))
            {
                return nodeSettingsDictionary[trafficAreaNodeType].PlaceType;
            }

            return NodePlaceType.Default;
        }

        public void ClearNullNodes()
        {
            foreach (var data in nodeData)
            {
                var nodes = data.Value.Nodes.Where(a => a != null).ToList();
                data.Value.Nodes = nodes;
            }

            EditorSaver.SetObjectDirty(this);
        }

        private void AssignSegment(TrafficNode trafficNode)
        {
#if UNITY_EDITOR
            if (roadSegmentCreator)
            {
                return;
            }

            if (trafficNode.TrafficLightCrossroad != null)
            {
                roadSegmentCreator = trafficNode.TrafficLightCrossroad.GetComponent<RoadSegmentCreator>();

                if (roadSegmentCreator)
                {
                    Subscribe();
                    EditorSaver.SetObjectDirty(this);
                }
            }
#endif
        }

        #endregion

        #region Editor events

#if UNITY_EDITOR
        private void Subscribe()
        {
            if (!roadSegmentCreator)
            {
                return;
            }

            roadSegmentCreator.ParkingLineDestroyed += RoadSegmentCreator_ParkingLineDestroyed;
            roadSegmentCreator.ParkingLineCreated += RoadSegmentCreator_ParkingLineCreated;
        }

        private void Unsubscribe()
        {
            if (!roadSegmentCreator)
            {
                return;
            }

            roadSegmentCreator.ParkingLineDestroyed -= RoadSegmentCreator_ParkingLineDestroyed;
            roadSegmentCreator.ParkingLineCreated -= RoadSegmentCreator_ParkingLineCreated;
        }

        private void RoadSegmentCreator_ParkingLineCreated(List<TrafficNode> nodes)
        {
            nodeData[TrafficAreaNodeType.Default].Nodes.AddRange(nodes);
            EditorSaver.SetObjectDirty(this);
        }

        private void RoadSegmentCreator_ParkingLineDestroyed(List<TrafficNode> nodes)
        {
            bool edited = false;

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];

                if (!node)
                {
                    continue;
                }

                if (nodeData[TrafficAreaNodeType.Default].Nodes.TryToRemove(node) && !edited)
                {
                    edited = true;
                }
            }

            if (edited)
            {
                EditorSaver.SetObjectDirty(this);
            }
        }

#endif

        #endregion

    }
}
