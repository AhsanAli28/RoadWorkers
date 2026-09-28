using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public partial class RoadSegmentCreator : MonoBehaviour
    {
        public TrafficNode AddTrafficNode()
        {
            TempNodeSettings newNodeSettings = null;

            switch (newNodeSettingsType)
            {
                case NewNodeSettingsType.Prefab:
                    break;
                case NewNodeSettingsType.Unique:
                    {
                        newNodeSettings = roadSegmentCreatorConfig.NewNodeSettings;

                        break;
                    }
                case NewNodeSettingsType.CopyLast:
                    {
                        if (createdTrafficNodes.Count > 1)
                        {
                            int lastIndex = createdTrafficNodes.Count - 1;
                            var selectedNode = createdTrafficNodes[lastIndex];

                            if (selectedNode != null)
                            {
                                var hasPedestrianNodes = GetPedestrianNodeEnabledState(lastIndex);
                                newNodeSettings = new TempNodeSettings(selectedNode, hasPedestrianNodes);
                            }
                        }

                        break;
                    }
                case NewNodeSettingsType.CopySelected:
                    {
                        if (copySelectedIndex < createdTrafficNodes.Count)
                        {
                            int selectedIndex = copySelectedIndex;

                            var selectedNode = createdTrafficNodes[selectedIndex];

                            if (selectedNode != null)
                            {
                                var hasPedestrianNodes = GetPedestrianNodeEnabledState(selectedIndex);
                                newNodeSettings = new TempNodeSettings(selectedNode, hasPedestrianNodes);
                            }
                        }

                        break;
                    }
            }

            int index = GetEmptyIndex();

            var node = CreateTrafficNode(index);

            OnTrafficNodeAdd(node);

            InitializeTrafficNodeHeaders();

            trafficLightCrossroad?.AddNode(node);

            if (newNodeSettings != null)
            {
                newNodeSettings.InstallSettings(node);
                SetEnterFlagOfOneWay(index, newNodeSettings.IsEndOfOneWay);
                SetPedestrianEnabledState(index, newNodeSettings.HasPedestrianNodes);
                SetCrosswalkEnabledState(index, newNodeSettings.HasCrosswalk);
                node.Resize();

                node.TrafficNodeCrosswalk.SwitchConnectionState(customCrossWalksData[index]);
                node.TrafficNodeCrosswalk.SwitchEnabledState(customPedestrianNodesData[index]);
            }

            return node;
        }

        public void DestroyNode(TrafficNode trafficNode, bool recordUndo = true, bool recordUndoSegment = true)
        {
            if (trafficNode == null)
            {
                return;
            }

#if UNITY_EDITOR

            if (recordUndo)
            {
                if (recordUndoSegment)
                {
                    RecordUndoSegment();

                    trafficLightCrossroad?.RemoveNode(trafficNode, true, true);

                    RemoveTrafficNode(trafficNode, true, true);
                }

                trafficNode.DestroyNode();

                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            }
            else
            {
                trafficLightCrossroad?.RemoveNode(trafficNode, true, false);

                RemoveTrafficNode(trafficNode, true, false);

                GameObject.DestroyImmediate(trafficNode.gameObject);
            }
#endif
        }

        public void RemoveTrafficNode(TrafficNode trafficNode, bool destroyRelated = true, bool recordUndoPaths = false)
        {
#if UNITY_EDITOR

            if (recordUndoPaths)
            {
                if (this)
                    Undo.RegisterCompleteObjectUndo(this, "Node removed");
            }

            if (destroyRelated)
            {
                TryToGetRelatedNodes(trafficNode, out var relatedNodes, out var relatedPaths);

                if (recordUndoPaths)
                {
                    for (int i = 0; i < relatedNodes?.Count; i++)
                    {
                        TrafficNode relatedNode = relatedNodes[i];

                        if (relatedNode)
                        {
                            Undo.RecordObject(relatedNode, "Node changed");
                        }
                    }
                }

                relatedPaths?.DestroyGameObjects(recordUndoPaths);

                if (recordUndoPaths)
                {
                    EditorExtension.CollapseUndoCurrentOperations();
                }

                relatedNodes = null;
                relatedPaths = null;
            }

            createdTrafficNodes.TryToRemove(trafficNode);
            TryToRemoveNodeFromParkingLine(trafficNode);

            UpdateNames();

            InitializeTrafficNodeHeaders();
            OnTrafficNodeRemove(trafficNode);
            RoadEditorEvents.RemoveNode(trafficNode);

            EditorSaver.SetObjectDirty(this);
#endif
        }

        public void TryToGetRelatedNodes(TrafficNode trafficNode, out List<TrafficNode> relatedTrafficNode, out List<Path> relatedPaths)
        {
            relatedTrafficNode = null;
            relatedPaths = null;

            for (int i = 0; i < createdTrafficNodes.Count; i++)
            {
                if (createdTrafficNodes[i] == null)
                {
                    continue;
                }

                var lanes = createdTrafficNodes[i].Lanes;

                for (int laneIndex = 0; laneIndex < lanes?.Count; laneIndex++)
                {
                    var laneData = lanes[laneIndex];

                    for (int j = 0; j < laneData.paths?.Count; j++)
                    {
                        var path = laneData.paths[j];

                        if (path && path.ConnectedTrafficNode == trafficNode)
                        {
                            if (relatedPaths == null)
                            {
                                relatedPaths = new List<Path>();
                            }
                            if (relatedTrafficNode == null)
                            {
                                relatedTrafficNode = new List<TrafficNode>();
                            }

                            relatedTrafficNode.TryToAdd(createdTrafficNodes[i]);
                            relatedPaths.TryToAdd(path);
                        }
                    }
                }
            }
        }

        public TrafficNode TryToGetNode(int index)
        {
            if (index >= 0 && createdTrafficNodes.Count > index)
            {
                return createdTrafficNodes[index];
            }

            return null;
        }

        public TrafficNode TryToGetClosestNode(Vector3 point, bool checkClosestLane = false)
        {
            if (point == default)
                throw new ArgumentException("TryToGetClosestNode point is Vector.zero");

            var maxDistance = float.MaxValue;

            TrafficNode closest = null;

            IterateAllNodes(node =>
            {
                var distance = 0f;

                if (checkClosestLane)
                {
                    node.GetClosestLane(point, out var pos);
                    distance = Vector3.SqrMagnitude(point - pos);
                }
                else
                {
                    distance = Vector3.SqrMagnitude(point - node.transform.position);
                }

                if (distance < maxDistance)
                {
                    maxDistance = distance;
                    closest = node;
                }
            });

            return closest;
        }

        public void IterateAllNodes(Action<TrafficNode> action)
        {
            for (int i = 0; i < createdTrafficNodes.Count; i++)
            {
                TrafficNode createdTrafficNode = createdTrafficNodes[i];

                if (createdTrafficNode)
                {
                    action?.Invoke(createdTrafficNode);
                }
            }
        }

        public void EnsureTrafficNodes(int count)
        {
            var nodeToCreate = count - TrafficNodeCount;

            if (nodeToCreate > 0)
            {
                for (int i = 0; i < nodeToCreate; i++)
                {
                    AddTrafficNode();
                }
            }
            else if (nodeToCreate < 0)
            {
                var node = TryToGetNode(1);

                if (node)
                {
                    RemoveTrafficNode(node);
                    GameObject.DestroyImmediate(node.gameObject);
                }
            }
        }

        public void IterateAllTrafficNodesPath(Action<Path> callback, bool includeExternal = false)
        {
            for (int i = 0; i < createdTrafficNodes?.Count; i++)
            {
                createdTrafficNodes[i].IterateAllPaths(callback, includeExternal);
            }
        }

        public bool GetEnterFlagOfOneWay(int index)
        {
            CheckForSizeCollection(isEnterOfOneWay, index);
            return isEnterOfOneWay[index];
        }

        public void SetEnterFlagOfOneWay(int index, bool value)
        {
            CheckForSizeCollection(isEnterOfOneWay, index);
            isEnterOfOneWay[index] = value;
        }

        public int GetNodeIndex(TrafficNode trafficNode)
        {
            return createdTrafficNodes.IndexOf(trafficNode);
        }

        public void InitializeTrafficNodeHeaders()
        {
            trafficNodeHeaders = new string[createdTrafficNodes.Count + 1];
            trafficNodeHeaders[0] = "All";

            for (int i = 1; i < trafficNodeHeaders.Length; i++)
            {
                trafficNodeHeaders[i] = $"Node{i}";
            }
        }

        public int GetSubwayCount()
        {
            if (GetRoadSegmentType == RoadSegmentCreator.RoadSegmentType.MergeCrossRoadToOneWayRoad)
            {
                int roadSubwayCount = 1;

                if (directionCount == 4)
                {
                    roadSubwayCount = 2;
                }
                else if (flipSubLaneNode)
                {
                    roadSubwayCount = 2;
                }

                return roadSubwayCount;
            }

            return 0;
        }

        private TrafficNode CreateTrafficNode()
        {
            GameObject traffinNodeGO = null;
            var prefab = roadSegmentCreatorConfig.TrafficNodePrefab;

#if UNITY_EDITOR
            traffinNodeGO = PrefabUtility.InstantiatePrefab(prefab.gameObject, trafficNodeParent) as GameObject;
#endif
            var trafficNode = traffinNodeGO.GetComponent<TrafficNode>();

#if UNITY_EDITOR
            RoadEditorEvents.AddNode(trafficNode);
#endif

            return trafficNode;
        }

        private TrafficNode CreateTrafficNode(int index, int side = -1)
        {
            var trafficNode = CreateTrafficNode();

            trafficNode.name = "TrafficNode" + (createdTrafficNodes.Count + 1).ToString();

            trafficNode.TempCreatorLocalIndex = index;
            trafficNode.TrafficLightCrossroad = trafficLightCrossroad;
            trafficNode.Initialize(this);

            createdTrafficNodes.Add(trafficNode);

            if (IsSubLane(index))
            {
                createdSubLaneTrafficNodes.Add(trafficNode);
            }

            SetLightSettings(index, side, trafficNode);
            SetTrafficNodeSettings(index, trafficNode);

            return trafficNode;
        }

        private TrafficNode CreateSimpleNode(bool autoAdd = true)
        {
            var trafficNode = CreateTrafficNode();

            trafficNode.name = "TrafficNode" + (createdTrafficNodes.Count + 1).ToString();

            trafficNode.TempCreatorLocalIndex = createdTrafficNodes.Count;
            trafficNode.TrafficLightCrossroad = trafficLightCrossroad;
            trafficNode.Initialize(this);

            if (autoAdd)
            {
                createdTrafficNodes.Add(trafficNode);
                OnTrafficNodeAdd(trafficNode);

                InitializeTrafficNodeHeaders();

                trafficLightCrossroad?.AddNode(trafficNode);
            }

            return trafficNode;
        }

        private void SetLightSettings(int index, int side, TrafficNode trafficNode)
        {
            if (trafficLightCrossroad)
            {
                TrafficLightHandler trafficLightHandler = null;

                if (side == -1)
                {
                    trafficLightHandler = index % 2 == 0 ? trafficLightCrossroad.GetTrafficLightHandler(0) : trafficLightCrossroad.GetTrafficLightHandler(1);
                }
                else
                {
                    trafficLightHandler = side == 0 ? trafficLightCrossroad.GetTrafficLightHandler(0) : trafficLightCrossroad.GetTrafficLightHandler(1);
                }

                trafficLightHandler.AddNode(trafficNode);
            }
        }

        private GameObject SetTrafficNodeSettings(int index, TrafficNode trafficNode, bool changePositionAndRotation = true, bool recordUndo = false)
        {
            var currentLaneCount = GetLaneCount(index);
            var currentLaneWidth = !IsSubLaneTrafficNode(trafficNode) ? LaneWidth : SubLaneWidth;

            var trafficNodeGo = trafficNode.gameObject;

            if (changePositionAndRotation)
            {
#if UNITY_EDITOR
                if (recordUndo)
                {
                    Undo.RecordObject(trafficNodeGo.transform, "Undo position");
                }
#endif

                SetNodePositionAndRotation(ref trafficNodeGo, index);
            }

#if UNITY_EDITOR

            if (recordUndo)
            {
                Undo.RegisterCompleteObjectUndo(trafficNode, "Undo settings");
            }

#endif
            trafficNode.LaneCount = currentLaneCount;
            trafficNode.LaneWidth = currentLaneWidth;
            trafficNode.DividerWidth = !trafficNode.IsOneWay ? DividerWidth : 0;
            trafficNode.TrafficNodeCrosswalk.CrossWalkOffset = CrossWalkOffset;
            trafficNode.ClearEmptyLanes();

            InitializeTrafficNodeCrosswalk(trafficNode);

            TryToInitializeOneWaySettings(index, trafficNode);

            trafficNode.Resize(recordUndo);

            var hasCrossWalkValue = HasCrossWalkConnection(index);
            var crossWalkNodesIsEnabled = GetPedestrianNodeEnabledState(index);

            trafficNode.TrafficNodeCrosswalk.SwitchConnectionState(hasCrossWalkValue, recordUndo);
            trafficNode.TrafficNodeCrosswalk.SwitchEnabledState(crossWalkNodesIsEnabled);

            return trafficNodeGo;
        }

        private int GetLaneCount(int index)
        {
            int currentLaneCount = laneCount;

            if (IsSubLane(index))
            {
                currentLaneCount = subLaneCount;
            }

            return currentLaneCount;
        }

        private void TryToInitializeOneWaySettings(int index, TrafficNode trafficNode)
        {
            if (!IsOneWayRoad(index))
            {
                trafficNode.IsOneWay = false;
                return;
            }

            trafficNode.IsOneWay = true;

            if (CreateTrafficNodeCount == 2)
            {
                if (index == 0)
                {
                    trafficNode.IsEndOfOneWay = shouldRevertDirection;
                }
                if (index == 2 || index == 1)
                {
                    trafficNode.IsEndOfOneWay = !shouldRevertDirection;
                }
            }
            else
            {
                if (!FlipSubLaneNode)
                {
                    if (index == 1)
                    {
                        trafficNode.IsEndOfOneWay = GetEnterFlagOfOneWay(0);
                    }
                    if (index == 3)
                    {
                        trafficNode.IsEndOfOneWay = GetEnterFlagOfOneWay(1);
                    }
                }
                else
                {
                    if (index == 0)
                    {
                        trafficNode.IsEndOfOneWay = GetEnterFlagOfOneWay(0);
                    }
                    if (index == 2)
                    {
                        trafficNode.IsEndOfOneWay = GetEnterFlagOfOneWay(1);
                    }
                }
            }
        }

        private bool HasCrossWalkConnection(int index)
        {
            CheckForSizeCollection(customCrossWalksData, index);
            return customCrossWalk ? customCrossWalksData[index] : customCrossWalksData[0];
        }

        private void SetNodePositionAndRotation(ref GameObject trafficNode, int index)
        {
            float distance = !IsSubLane(index) ? crossroadWidth : subTrafficNodeDistanceFromCenter;

            switch (index)
            {
                case 0:
                    {
                        Vector3 offset = Vector3.zero;

                        if (IsStraightRoad())
                        {
                            offset = new Vector3(0, trafficNodeHeight1);
                        }

                        trafficNode.transform.position = transform.TransformPoint(new Vector3(trafficNodeOffset1, 0, -distance)) + transform.rotation * offset;
                        trafficNode.transform.localRotation = Quaternion.Euler(0, 180, 0);
                        trafficNode.transform.localRotation = Quaternion.Euler(0, 180 + AdditionalLocalAngle1, 0);
                        break;
                    }
                case 1:
                    {
                        trafficNode.transform.position = transform.TransformPoint(new Vector3(-distance, 0, 0)) + new Vector3(trafficNodeOffset2, 0);

                        Vector3 direction = (trafficNode.transform.position - transform.position).normalized;
                        trafficNode.transform.rotation = MathUtilMethods.LookRotationSafe(direction);
                        trafficNode.transform.localRotation = Quaternion.Euler(0, trafficNode.transform.localRotation.eulerAngles.y + AdditionalLocalAngle2, 0);
                        break;
                    }
                case 2:
                    {
                        Vector3 offset = Vector3.zero;

                        if (IsStraightRoad())
                        {
                            offset = new Vector3(0, trafficNodeHeight2);
                        }

                        trafficNode.transform.position = transform.TransformPoint(new Vector3(trafficNodeOffset2, 0, distance)) + transform.rotation * offset;
                        trafficNode.transform.localRotation = Quaternion.Euler(0, 0, 0);
                        break;
                    }
                case 3:
                    {
                        trafficNode.transform.position = transform.TransformPoint(new Vector3(distance, 0, 0));
                        trafficNode.transform.localRotation = Quaternion.Euler(0, 90, 0);
                        break;
                    }
                default:
                    {
                        trafficNode.transform.localPosition = Vector3.zero;
                        break;
                    }
            }
        }

        private int GetEmptyIndex()
        {
            int index = 0;

            while (true)
            {
                var node = createdTrafficNodes.Where(item => item.TempCreatorLocalIndex == index).FirstOrDefault();

                if (node == null)
                {
                    return index;
                }
                else
                {
                    index++;
                }
            }
        }

        private void UpdateNames()
        {
            for (int i = 0; i < createdTrafficNodes?.Count; i++)
            {
                if (createdTrafficNodes[i] == null)
                {
                    continue;
                }

                createdTrafficNodes[i].gameObject.name = "TrafficNode" + (i + 1).ToString();
            }
        }
    }
}
