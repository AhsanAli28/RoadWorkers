using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public partial class RoadSegmentCreator : MonoBehaviour
    {
#if UNITY_EDITOR

        public void OnCreateSegment()
        {
            initialInit = true;
            InitialLoadSettings();
        }

        public void OnInspectorEnabled()
        {
            CheckFirstInitForExistSegment();
            SwitchTempEnabledState(true);

            if (tempParkingLineSettings == null)
            {
                tempParkingLineSettings = ScriptableObject.CreateInstance<ParkingLineSettingsContainer>();
            }

            CheckLightData();

            OnConfigChanged();

            InitOuter(true);

            MergedCrossings.Clear();

            IterateAllNodes(sourceNode =>
            {
                if (sourceNode.MergeNode != null)
                {
                    var creator = sourceNode.MergeNode.TrafficLightCrossroad.GetComponent<RoadSegmentCreator>();

                    if (creator)
                    {
                        creator.InitOuter(true);
                        MergedCrossings.Add(sourceNode, creator);
                    }
                }
            });

            foreach (var createdLight in createdLights)
            {
                if (!createdLight)
                {
                    continue;
                }

                if (createdLight.transform.parent != trafficLights && createdLight.transform.parent != pedestrianLights)
                {
                    currentLightParent = createdLight.transform.parent;
                    break;
                }
            }
        }

        public void OnInspectorDisabled()
        {
            SwitchTempEnabledState(false);

            CurrentExtrudeState = ExtrudeState.Default;
            Dispose();
            SaveSettings();
        }

        public void InitOuter(bool force = false)
        {
            if (allConnectedOuterPaths != null && !force)
                return;

            allConnectedOuterPaths = new Dictionary<TrafficNode, List<Path>>();

            foreach (var trafficNode in trafficLightCrossroad.TrafficNodes)
            {
                if (!trafficNode)
                {
                    continue;
                }

                trafficNode.IterateAllPaths((path) =>
                {
                    path.ShowInfoWaypoints = false;
                }, true);

                var paths = trafficNode.GetAllConnectedOuterPaths();

                allConnectedOuterPaths.Add(trafficNode, paths);
            }
        }

        public void Dispose()
        {
            allConnectedOuterPaths = null;
        }

        public void OnEscapeClicked()
        {
            ResetLaneExtrude();
        }

        public void OnConfigChanged()
        {
            roadSegmentCreatorConfig?.CheckForNullConfigs();
            roadSegmentCreatorConfig?.CheckForSavePath();

            SyncParkingConfigHeaders();
        }

        public void OnSegmentTypeChanged(RoadSegmentType previousType, RoadSegmentType newType)
        {
            if (newType == previousType) return;

            UninitializeType(previousType);

            ICustomCreator customCreator = null;

            switch (newType)
            {
                case RoadSegmentType.Roundabout:

                    customCreator = gameObject.GetOrCreateComponent<Roundabout>();
                    break;
            }

            if (customCreator == null)
            {
                Create();
            }
            else
            {
                customCreator.Create();
            }
        }

        public void OnCrosswalkSettingsChanged()
        {
            UpdateCrosswalk();
        }

        public void OnPathSelectionChanged()
        {
            this.selectedPath = GetPathByIndex();
            OnPathSelectionChangedEvent.Invoke(this.selectedPath);
        }

        public void OnWayPointStraightCountChanged()
        {
            if (ObjectIsPrefab())
            {
                return;
            }

            Action<Path> callback = (Path path) =>
            {
                if (path.PathCurveType == PathCurveType.StraightLine)
                {
                    path.WayPointsCountPerCurve = wayPointStraightRoadCount;
                    path.CreatePath(true);
                }
            };

            IterateAllTrafficNodesPath(callback);
        }

        public void OnWayPointTurnCountChanged()
        {
            if (ObjectIsPrefab())
            {
                return;
            }

            Action<Path> callback = (Path path) =>
            {
                if (path.PathCurveType != PathCurveType.StraightLine)
                {
                    path.WayPointsCountPerCurve = wayPointTurnCurveCount;
                    path.CreatePath(true);
                }
            };

            IterateAllTrafficNodesPath(callback);
        }

        public void OnStraightSpeedLimitChanged()
        {
            Action<Path> callback = (Path path) =>
            {
                if (path.PathCurveType == PathCurveType.StraightLine)
                {
                    path.PathSpeedLimit = straightRoadPathSpeedLimit;
                    path.Priority = straightRoadPriority;
                    path.ResetSpeedLimit();
                }
            };

            IterateAllTrafficNodesPath(callback);
        }

        public void OnTurnSpeedLimitChanged()
        {
            Action<Path> callback = (Path path) =>
            {
                if (path.PathCurveType != PathCurveType.StraightLine)
                {
                    path.PathSpeedLimit = turnRoadPathSpeedLimit;
                    path.Priority = turnRoadPriority;
                    path.ResetSpeedLimit();
                }
            };

            IterateAllTrafficNodesPath(callback);
        }

        public void OnLightSettingsChanged()
        {
            ClearLights();
            TryToAddLights();
        }

        public void UndoClicked()
        {
            if (IsCustomStraight())
            {
                RecalculateCustomPath(false);
            }

            foreach (var item in MergedCrossings)
            {
                var merge = item.Value;

                if (!merge) continue;

                if (merge.IsCustomStraight())
                {
                    merge.RecalculateCustomPath(false);
                }

                if (merge.IsCustom(false))
                {
                    merge.UpdatePaths();
                }
            }

            if (ParkingBuilderMode)
            {
                RecalcuteParkingPaths();
            }
        }

        public void RecalculateAllOuterConnectedPaths(bool recordUndo = true)
        {
            if (!roadSegmentCreatorConfig.AutoRecalculateExternalPaths)
                return;

            for (int i = 0; i < createdTrafficNodes.Count; i++)
            {
                RecalculateNodeOuterConnections(createdTrafficNodes[i], recordUndo);
            }
        }

        public void RecalculateNodeOuterConnections(TrafficNode trafficNode, bool recordUndo = false)
        {
            if (!roadSegmentCreatorConfig.AutoRecalculateExternalPaths)
                return;

            if (allConnectedOuterPaths == null || !allConnectedOuterPaths.ContainsKey(trafficNode))
                return;

            IterateOuterPaths(trafficNode, path =>
            {
                path.AttachConnectedNode(true, true, recordUndo);
            });
        }

        public void IterateAllOuterPaths(Action<Path> callback)
        {
            for (int i = 0; i < createdTrafficNodes.Count; i++)
            {
                IterateOuterPaths(createdTrafficNodes[i], callback);
            }
        }

        public void IterateOuterPaths(TrafficNode trafficNode, Action<Path> callback)
        {
            if (allConnectedOuterPaths == null)
                InitOuter();

            if (!allConnectedOuterPaths.ContainsKey(trafficNode))
                return;

            var paths = allConnectedOuterPaths[trafficNode];

            foreach (var path in paths)
            {
                if (!path) continue;

                callback(path);
            }
        }

        public void ProcessDuplicate()
        {
            var trafficNodes = GetComponentsInChildren<TrafficNode>();
            var pedestrianNodes = GetComponentsInChildren<PedestrianNode>();

            var disconnectNodes = new List<PedestrianNode>();

            for (int i = 0; i < pedestrianNodes.Length; i++)
            {
                var node = pedestrianNodes[i];

                node.IterateConnectedNodes(connectedNode =>
                {
                    if (Array.IndexOf(pedestrianNodes, connectedNode) == -1)
                    {
                        disconnectNodes.Add(connectedNode);
                    }
                });

                foreach (var disconnectNode in disconnectNodes)
                {
                    node.RemoveConnection(disconnectNode);
                }

                disconnectNodes.Clear();
            }

            var paths = new List<Path>();

            foreach (var trafficNode in trafficNodes)
            {
                trafficNode.IterateExternalPaths(path =>
                {
                    if (path.ConnectedTrafficNode == null || Array.IndexOf(trafficNodes, path.ConnectedTrafficNode) == -1)
                        paths.Add(path);
                });

                foreach (var path in paths)
                {
                    trafficNode.TryToRemovePath(path);
                    path.DestroyPath(false);
                }

                trafficNode.ExternalLanes.Clear();
                EditorSaver.SetObjectDirty(trafficNode);

                paths.Clear();
            }

            RegenerateAllIds(trafficNodes, pedestrianNodes);
        }

        public void RegenerateAllIds()
        {
            var trafficNodes = GetComponentsInChildren<TrafficNode>();
            var pedestrianNodes = GetComponentsInChildren<PedestrianNode>();

            RegenerateAllIds(trafficNodes, pedestrianNodes);
        }

        public void RegenerateAllIds(TrafficNode[] trafficNodes, PedestrianNode[] pedestrianNodes)
        {
            for (int i = 0; i < pedestrianNodes.Length; i++)
            {
                var node = pedestrianNodes[i];

                node.GenerateId(true);
                node.ClearCustomConnectionData();
            }

            foreach (var trafficNode in trafficNodes)
            {
                trafficNode.GenerateIds(true);
            }

            trafficLightCrossroad.TryToGenerateID(true);
        }

        private void UninitializeType(RoadSegmentType previousType, bool recordUndo = false)
        {
            switch (previousType)
            {
                case RoadSegmentType.Roundabout:

                    var comp = GetComponent<Roundabout>();

                    if (comp)
                    {
                        if (!recordUndo)
                        {
                            DestroyImmediate(comp);
                        }
                        else
                        {
#if UNITY_EDITOR
                            UnityEditor.Undo.DestroyObjectImmediate(comp);
#endif
                        }
                    }

                    break;
            }
        }

        private void CheckFirstInitForExistSegment()
        {
            if (initialInit)
                return;

            initialInit = true;

#pragma warning disable CS0618

            if (!customLaneWidthEnabled)
            {
                if (createdTrafficNodes.Count > 0 && createdTrafficNodes[0] != null)
                {
                    customLaneWidth = createdTrafficNodes[0].LaneWidth;
                }
                else
                {
                    customLaneWidth = roadSegmentCreatorConfig.LaneWidth;
                }
            }

            if (!uniqueCrossWalkOffset)
            {
                if (createdTrafficNodes.Count > 0 && createdTrafficNodes[0] != null)
                {
                    customCrossWalkOffset = createdTrafficNodes[0].TrafficNodeCrosswalk.CrossWalkOffset;
                }
                else
                {
                    customCrossWalkOffset = roadSegmentCreatorConfig.CrossWalkOffset;
                }
            }

#pragma warning restore CS0618

            if (SubLaneSupport())
            {
                if (createdTrafficNodes.Count > 1 && createdTrafficNodes[1] != null)
                {
                    subLaneWidth = createdTrafficNodes[1].LaneWidth;

                    if (customLaneWidth != subLaneWidth)
                    {
                        customSubLaneWidth = true;
                    }
                }
            }

            EditorSaver.SetObjectDirty(this);
        }

        private void InitialLoadSettings()
        {
            if (!roadSegmentCreatorConfig) return;

            crossroadWidth = roadSegmentCreatorConfig.CrossroadWidth;
            subTrafficNodeDistanceFromCenter = roadSegmentCreatorConfig.SubTrafficNodeDistanceFromCenter;
            customLaneWidth = roadSegmentCreatorConfig.LaneWidth;
            subLaneWidth = roadSegmentCreatorConfig.SubLaneWidth;

            customCrossWalkOffset = roadSegmentCreatorConfig.CrossWalkOffset;
            pedestrianRouteWidth = roadSegmentCreatorConfig.PedestrianRouteWidth;
            crossWalkNodeShape = roadSegmentCreatorConfig.CrossWalkNodeShape;
            crosswalkNodeWidth = roadSegmentCreatorConfig.CrosswalkNodeWidth;
            crosswalkNodeHeight = roadSegmentCreatorConfig.CrosswalkNodeHeight;
            cornerOffset = roadSegmentCreatorConfig.CornerOffset;
            pedestrianCornerConnectionType = roadSegmentCreatorConfig.PedestrianCornerConnectionType;

            pathCorner1Offset = roadSegmentCreatorConfig.PathCorner1Offset;
            pathCorner2Offset = roadSegmentCreatorConfig.PathCorner2Offset;

            subLaneWidth = roadSegmentCreatorConfig.SubLaneWidth;
            customSubLaneWidth = roadSegmentCreatorConfig.CustomSubLaneWidth;

            straightRoadPathSpeedLimit = roadSegmentCreatorConfig.StraightRoadPathSpeedLimit;
            wayPointStraightRoadCount = roadSegmentCreatorConfig.WayPointStraightRoadCount;

            turnCurveType = roadSegmentCreatorConfig.TurnCurveType;
            turnRoadPathSpeedLimit = roadSegmentCreatorConfig.TurnRoadPathSpeedLimit;
            wayPointTurnCurveCount = roadSegmentCreatorConfig.WayPointTurnCurveCount;

            addTrafficLights = roadSegmentCreatorConfig.AddTrafficLights;
            addPedestrianLights = roadSegmentCreatorConfig.AddPedestrianLights;

            EditorSaver.SetObjectDirty(this);
        }

        private void SaveSettings()
        {
            if (!roadSegmentCreatorConfig) return;
            if (!this) return;

            if (!IsCustom(false))
            {
                roadSegmentCreatorConfig.LaneWidth = customLaneWidth;
            }
            else
            {
                float maxLaneWidth = 0;

                foreach (var node in this.GetComponentsInChildren<TrafficNode>())
                {
                    if (node.LaneWidth > maxLaneWidth)
                    {
                        maxLaneWidth = node.LaneWidth;
                    }
                }

                if (maxLaneWidth > 0)
                    roadSegmentCreatorConfig.LaneWidth = maxLaneWidth;
            }

            if (addPedestrianNodes)
            {
                roadSegmentCreatorConfig.CrossWalkOffset = customCrossWalkOffset;
                roadSegmentCreatorConfig.PedestrianRouteWidth = pedestrianRouteWidth;
                roadSegmentCreatorConfig.CrossWalkNodeShape = crossWalkNodeShape;
                roadSegmentCreatorConfig.CrosswalkNodeWidth = crosswalkNodeWidth;
                roadSegmentCreatorConfig.CrosswalkNodeHeight = crosswalkNodeHeight;
            }

            roadSegmentCreatorConfig.StraightRoadPathSpeedLimit = straightRoadPathSpeedLimit;
            roadSegmentCreatorConfig.WayPointStraightRoadCount = wayPointStraightRoadCount;

            if (!IsCustom(true))
            {
                roadSegmentCreatorConfig.CrossroadWidth = crossroadWidth;
                roadSegmentCreatorConfig.PedestrianCornerConnectionType = pedestrianCornerConnectionType;
                roadSegmentCreatorConfig.CornerOffset = cornerOffset;
            }

            if (!IsCustomStraight())
            {
                roadSegmentCreatorConfig.PathCorner1Offset = pathCorner1Offset;
                roadSegmentCreatorConfig.PathCorner2Offset = pathCorner2Offset;
                roadSegmentCreatorConfig.TurnCurveType = turnCurveType;
                roadSegmentCreatorConfig.TurnRoadPathSpeedLimit = turnRoadPathSpeedLimit;
                roadSegmentCreatorConfig.WayPointTurnCurveCount = wayPointTurnCurveCount;
            }

            if (SubLaneSupport())
            {
                roadSegmentCreatorConfig.SubTrafficNodeDistanceFromCenter = subTrafficNodeDistanceFromCenter;
                roadSegmentCreatorConfig.SubLaneWidth = subLaneWidth;
                roadSegmentCreatorConfig.CustomSubLaneWidth = customSubLaneWidth;
            }

            if (TrafficLightPlacingSupported)
            {
                roadSegmentCreatorConfig.AddTrafficLights = addTrafficLights;
            }

            roadSegmentCreatorConfig.AddPedestrianLights = addPedestrianLights;

            EditorSaver.SetObjectDirty(roadSegmentCreatorConfig);
        }
#endif
    }
}