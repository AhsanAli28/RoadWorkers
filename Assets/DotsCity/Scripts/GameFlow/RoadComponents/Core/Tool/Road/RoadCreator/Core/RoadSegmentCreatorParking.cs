using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public partial class RoadSegmentCreator : MonoBehaviour
    {
        public bool HasParkingConnectionData
        {
            get
            {
                switch (parkingConnectionSourceType)
                {
                    case ParkingConnectionSourceType.Path:
                        return parkingSourcePath;
                    case ParkingConnectionSourceType.Node:
                        return sourceTrafficNode && targetTrafficNode;
                    case ParkingConnectionSourceType.SingleNode:
                        return sourceTrafficNode;
                }

                return false;
            }
        }

        public void CreateParkingLine()
        {
            if (CurrentParkingLineSettings == null)
            {
                return;
            }

            if (CurrentParkingLineSettings.PlaceCount <= 0)
            {
                return;
            }

            List<TrafficNode> newNodeLine = new List<TrafficNode>();
            PedestrianNode previousNode = null;

            for (int i = 0; i < CurrentParkingLineSettings.PlaceCount; i++)
            {
                var parkingNode = CreateSimpleNode();

                parkingNode.TrafficNodeType = CurrentParkingLineSettings.PlaceTrafficNodeType;
                parkingNode.LaneCount = 1;
                parkingNode.Weight = CurrentParkingLineSettings.ParkingTrafficNodeWeight;
                parkingNode.CustomAchieveDistance = CurrentParkingLineSettings.ParkingTrafficNodeCustomAchieveDistance;

                var lineOffset = GetLineOffset(i);

                var currentCenter = LineStartPointWorld + lineOffset;
                parkingNode.transform.position = currentCenter;

                parkingNode.transform.rotation = GetNodeRotation();
                parkingNode.IsOneWay = true;
                parkingNode.LockPathAutoCreation = true;
                parkingNode.TrafficNodeCrosswalk.SwitchEnabledState(false);

                var size = GetLinePlaceSize();
                parkingNode.SetColliderSize(size);

                if (HasParkingConnectionData)
                {
                    CreateParkingPaths(i, parkingNode, lineOffset);
                }

                if (CurrentParkingLineSettings.AddParkingPedestrianNodes)
                {
                    var parkingPedestrianNode = CreatePedestrianNode();

                    parkingPedestrianNode.ConnectedTrafficNode = parkingNode;

                    var enterParkingPedestrianNode = CreatePedestrianNode();

                    parkingPedestrianNode.transform.position = GetParkingPedestrianNodePosition(currentCenter);
                    enterParkingPedestrianNode.transform.position = GetEnterParkingPedestrianNodePosition(currentCenter);

                    parkingPedestrianNode.AddNode(enterParkingPedestrianNode);
                    enterParkingPedestrianNode.AddNode(parkingPedestrianNode);

                    parkingPedestrianNode.Capacity = 0;
                    parkingPedestrianNode.PriorityWeight = CurrentParkingLineSettings.ParkingPedestrianNodeWeight;
                    parkingPedestrianNode.PedestrianNodeType = CurrentParkingLineSettings.ParkingPedestrianNodeType;

                    parkingPedestrianNode.transform.parent = parkingNode.transform;
                    enterParkingPedestrianNode.transform.parent = parkingNode.transform;

                    if (CurrentParkingLineSettings.AutoConnectNodes && previousNode)
                    {
                        enterParkingPedestrianNode.AddNode(previousNode);
                        previousNode.AddNode(enterParkingPedestrianNode);
                        EditorSaver.SetObjectDirty(previousNode);
                    }

                    previousNode = enterParkingPedestrianNode;

                    parkingPedestrianNode.name = "ParkingPedestrianNode";
                    enterParkingPedestrianNode.name = "EnterParkingPedestrianNode";

                    EditorSaver.SetObjectDirty(parkingPedestrianNode);
                    EditorSaver.SetObjectDirty(enterParkingPedestrianNode);
                }

                EditorSaver.SetObjectDirty(parkingNode);

                newNodeLine.Add(parkingNode);
            }

            var savedEnterPath = GetParkingPathData(GetTempPath(0));
            var savedExitPath = GetParkingPathData(GetTempPath(1));

            lineDatas.Add(new ParkingLineData()
            {
                ParkingLineSettings = CurrentParkingLineSettings.GetSettingsClone(),
                SourceTrafficNode = sourceTrafficNode,
                TargetTrafficNode = targetTrafficNode,
                SourcePath = parkingSourcePath,
                SavedEnterPath = savedEnterPath,
                SavedExitPath = savedExitPath,
                EnterPathOffsets = CloneOffsets(enterPathOffsets),
                ExitPathOffsets = CloneOffsets(exitPathOffsets),
                LineData = newNodeLine
            });

            DestroyTempPath();

            ResetOffsets(0);
            ResetOffsets(1);

            RoadSegment?.BakeData();

#if UNITY_EDITOR

            ParkingLineCreated(newNodeLine);

#endif

            EditorSaver.SetObjectDirty(this);
        }

        public void ReattachExitParkingPaths()
        {
            for (int i = 0; i < lineDatas.Count; i++)
            {
                var trafficNodes = lineDatas[i].LineData;

                for (int j = 0; j < trafficNodes?.Count; j++)
                {
                    var trafficNode = trafficNodes[j];

                    if (!trafficNode) continue;

                    var path = trafficNode.GetAnyPath();

                    if (path == null || path.PathConnectionType != PathConnectionType.PathPoint || !path.HasConnection)
                        continue;

                    var lastNode = path.Nodes[path.Nodes.Count - 1];

                    lastNode.position = PathHelper.GetAttachPoint(path.ConnectedPath, lastNode.position);
                    path.CreatePath(false);
                }
            }
        }

        private TempParkingPathData GetParkingPathData(Path path)
        {
            if (!path)
            {
                return null;
            }

            TempParkingPathData parkingPathData = new TempParkingPathData();
            parkingPathData.Nodes = new List<TempParkingPathNodeData>(path.WayPoints.Count);
            parkingPathData.trafficGroupMask = path.TrafficGroupMask.GetClone();

            for (int i = 0; i < path.WayPoints.Count; i++)
            {
                parkingPathData.Nodes.Add(new TempParkingPathNodeData()
                {
                    LocalPosition = transform.InverseTransformPoint(path.WayPoints[i].transform.position),
                    SpeedLimit = path.WayPoints[i].SpeedLimit,
                    BackwardDirection = path.WayPoints[i].BackwardDirection,
                    TrafficGroupMask = path.WayPoints[i].TrafficGroupMask.GetClone()
                });
            }

            return parkingPathData;
        }

        private void CreateParkingPaths(int index, TrafficNode parkingNode, Vector3 lineOffset)
        {
            switch (parkingConnectionSourceType)
            {
                case ParkingConnectionSourceType.Path:
                    {
                        TrafficNode sourceTrafficNode = parkingSourcePath.SourceTrafficNode;

                        var enterPath = GetTempPath(0);

                        if (enterPath != null)
                        {
                            var sourceLaneIndex = sourceTrafficNode.GetLaneIndexOfPath(parkingSourcePath);

                            var newEnterPath = CreatePath(PathDirectionType.Forward, parkingSourcePath.SourceTrafficNode, parkingNode);
                            newEnterPath.ConnectedLaneIndex = 0;

                            newEnterPath.name = GetPathName("EnterPath", sourceTrafficNode, parkingNode);
                            parkingSourcePath.SourceTrafficNode.AddPath(newEnterPath, sourceLaneIndex);

                            CloneTempPath(index, enterPath, newEnterPath, lineOffset, CurrentParkingLineSettings.NodeCloneCount);
                        }

                        var exitPath = GetTempPath(1);

                        if (exitPath != null)
                        {
                            var newExitPath = CreatePath(PathDirectionType.Forward, parkingNode, null);

                            newExitPath.PathConnectionType = PathConnectionType.PathPoint;
                            newExitPath.ConnectedPath = parkingSourcePath;
                            newExitPath.ConnectedLaneIndex = exitPath.ConnectedLaneIndex;
                            newExitPath.name = "ExitPath";

                            parkingNode.AddPath(newExitPath, 0);

                            CloneTempPath(index, exitPath, newExitPath, lineOffset, 0, CurrentParkingLineSettings.NodeSkipLastCount, exitPath: true);
                        }

                        break;
                    }
                case ParkingConnectionSourceType.Node:
                    {
                        CreateParkingNodeConnectedLine(index, parkingNode, sourceTrafficNode, targetTrafficNode, lineOffset);
                        break;
                    }
                case ParkingConnectionSourceType.SingleNode:
                    {
                        CreateParkingNodeConnectedLine(index, parkingNode, sourceTrafficNode, sourceTrafficNode, lineOffset, true);
                        break;
                    }
            }
        }

        private void CreateParkingNodeConnectedLine(int index, TrafficNode parkingNode, TrafficNode sourceNode, TrafficNode targetNode, Vector3 lineOffset, bool singleNode = false)
        {
            var enterPath = GetTempPath(0);

            if (enterPath != null)
            {
                var sourceLaneIndex = connectionLaneIndex;

                var newEnterPath = CreatePath(PathDirectionType.Forward, sourceNode, parkingNode);
                newEnterPath.ConnectedLaneIndex = 0;

                newEnterPath.name = GetPathName("EnterPath", sourceNode, parkingNode);
                sourceTrafficNode.AddPath(newEnterPath, sourceLaneIndex);

                CloneTempPath(index, enterPath, newEnterPath, lineOffset, CurrentParkingLineSettings.NodeCloneCount, singleNode: singleNode);
            }

            var exitPath = GetTempPath(1);

            if (exitPath != null)
            {
                var newExitPath = CreatePath(PathDirectionType.Forward, parkingNode, targetNode);

                newExitPath.PathConnectionType = PathConnectionType.TrafficNode;
                newExitPath.ConnectedLaneIndex = exitPath.ConnectedLaneIndex;
                newExitPath.name = "ExitPath";

                parkingNode.AddPath(newExitPath, 0);

                CloneTempPath(index, exitPath, newExitPath, lineOffset, 0, CurrentParkingLineSettings.NodeSkipLastCount, singleNode: singleNode, exitPath: true);
            }
        }

        private void CloneTempPath(int index, Path sourcePath, Path targetPath, Vector3 offset, int skipCount = 0, int skipLastCount = 0, bool singleNode = false, bool exitPath = false)
        {
            targetPath.WayPointsCountPerCurve = sourcePath.WayPointsCountPerCurve;
            targetPath.PathRoadType = sourcePath.PathRoadType;
            targetPath.PathCurveType = sourcePath.PathCurveType;
            targetPath.PathSpeedLimit = sourcePath.PathSpeedLimit;
            targetPath.Priority = sourcePath.Priority;
            targetPath.Rail = sourcePath.Rail;
            targetPath.TrafficGroupMask = sourcePath.TrafficGroupMask.GetClone();

            var nodesToAdd = sourcePath.Nodes.Count - targetPath.Nodes.Count;

            int indexOffset = 0;

            for (int i = 0; i < nodesToAdd; i++)
            {
                targetPath.AddNode(false);
            }

            if (singleNode)
            {
                if (!exitPath)
                {
                    if (skipCount == 0)
                    {
                        targetPath.AddNode(false);
                        targetPath.Nodes[0].position = sourcePath.StartPosition;
                        indexOffset++;
                    }
                }
                else
                {
                    if (skipLastCount == 0)
                    {
                        targetPath.AddNode(false);
                        targetPath.Nodes[targetPath.Nodes.Count - 1].position = sourcePath.EndPosition;
                    }
                }
            }

            for (int i = 0; i < sourcePath.Nodes.Count; i++)
            {
                var sourceNode = sourcePath.Nodes[i];

                var targetIndex = i + indexOffset;
                var targetNode = targetPath.Nodes[targetIndex];

                var currentOffset = GetPointOffset(i, offset, sourcePath.Nodes.Count, skipCount, skipLastCount);
                currentOffset += GetHandleOffset(index, i, exitPath);

                targetNode.transform.position = sourceNode.transform.position + currentOffset;
                targetNode.transform.rotation = sourceNode.transform.rotation;

                EditorSaver.SetObjectDirty(targetNode);
            }

            targetPath.CreatePath(true);

            for (int i = 0; i < sourcePath.WayPoints.Count; i++)
            {
                var sourceNode = sourcePath.WayPoints[i];
                var targetNode = targetPath.WayPoints[i];

                targetNode.Copy(sourceNode);

                EditorSaver.SetObjectDirty(targetNode);
            }

            EditorSaver.SetObjectDirty(targetPath);
        }

        public Vector3 GetPointOffset(int nodeIndex, Vector3 lineOffset, int wayPointsCount, int skipCount = 0, int skipLastCount = 0, int indexOffset = 0)
        {
            var offset = lineOffset;

            if (nodeIndex + indexOffset < skipCount)
            {
                offset = Vector3.zero;
            }

            if (wayPointsCount - nodeIndex - 1 - indexOffset < skipLastCount)
            {
                offset = Vector3.zero;
            }

            return offset;
        }

        public Vector3 GetHandleOffset(int placeIndex, int nodeIndex, bool exitPath)
        {
            Vector3 offset = default;

            if (!exitPath)
            {
                if (enterPathOffsets.Count > placeIndex && enterPathOffsets[placeIndex].Offsets.Count > nodeIndex)
                {
                    offset = enterPathOffsets[placeIndex].Offsets[nodeIndex];
                }
            }
            else
            {
                if (exitPathOffsets.Count > placeIndex && exitPathOffsets[placeIndex].Offsets.Count > nodeIndex)
                {
                    offset = exitPathOffsets[placeIndex].Offsets[nodeIndex];
                }
            }

            return offset;
        }

        public void EditParkingLine(int index = -1, bool recordUndo = false)
        {
            if (ObjectIsPrefab())
            {
                return;
            }

            InitializeTempStartParkingPoint();
            parkingBuilderMode = true;
            var lineData = lineDatas[index];

            if (lineData != null)
            {
#if UNITY_EDITOR

                ParkingLineDestroyed(lineData.LineData);

#endif

                parkingConfigType = ParkingConfigType.Temp;

                CurrentParkingLineSettings.InstallSettings(lineData.ParkingLineSettings);

                sourceTrafficNode = lineData.SourceTrafficNode;
                targetTrafficNode = lineData.TargetTrafficNode;
                parkingSourcePath = lineData.SourcePath;

                var savedEnterPath = lineData.SavedEnterPath;
                var savedExitPath = lineData.SavedExitPath;

                tempStartParkingPoint.localPosition = lineData.ParkingLineSettings.LineStartPointLocal;
                LineStartPointWorld = tempStartParkingPoint.transform.position;

                ClearParkingLine(index, recordUndo);

                CreateTempParkingPaths(CurrentParkingLineSettings, 1);
                CreateTempParkingPaths(CurrentParkingLineSettings, 2);

                var enterPath = GetTempPath(0);
                var exitPath = GetTempPath(1);

                RestoreSavedPath(savedEnterPath, enterPath);
                RestoreSavedPath(savedExitPath, exitPath);

                TrafficMaskChanged();

                if (lineData.EnterPathOffsets != null)
                {
                    enterPathOffsets = CloneOffsets(lineData.EnterPathOffsets);
                }
                else
                {
                    CreateOffsets(enterPath, 0);
                }

                if (lineData.ExitPathOffsets != null)
                {
                    exitPathOffsets = CloneOffsets(lineData.ExitPathOffsets);
                }
                else
                {
                    CreateOffsets(exitPath, 1);
                }
            }
        }

        public void ClearParkingLine(int index = -1, bool recordUndo = false)
        {
            if (ObjectIsPrefab())
            {
                return;
            }

            if (index >= 0)
            {
                if (lineDatas.Count > index)
                {
                    if (recordUndo)
                    {
                        RecordUndoSegment();

                        List<TrafficNode> relatedNodes = null;
                        List<Path> relatedPaths = null;

                        var lineData = lineDatas[index];

                        if (lineData != null)
                        {
                            for (int i = 0; i < lineData.LineData.Count; i++)
                            {
                                var node = lineData.LineData[i];

                                if (node != null)
                                {
                                    TryToGetRelatedNodes(node, out var tempNodes, out var tempPaths);

                                    if (tempNodes != null)
                                    {
                                        if (relatedNodes == null)
                                        {
                                            relatedNodes = new List<TrafficNode>();
                                        }

                                        relatedNodes.TryToAdd(tempNodes);
                                    }

                                    if (tempPaths != null)
                                    {
                                        if (relatedPaths == null)
                                        {
                                            relatedPaths = new List<Path>();
                                        }

                                        relatedPaths.TryToAdd(tempPaths);
                                    }
                                }
                            }
                        }

#if UNITY_EDITOR

                        for (int i = 0; i < relatedNodes?.Count; i++)
                        {
                            var relatedNode = relatedNodes[i];
                            Undo.RegisterCompleteObjectUndo(relatedNode, "Undo node");
                        }

                        relatedPaths?.DestroyGameObjects(true);
#endif
                    }

                    DestroyParkingLineElement(index, recordUndo);
                }
            }
            else
            {
                var count = lineDatas.Count;

                if (count > 0)
                {
                    if (recordUndo)
                    {
                        RecordUndoSegment();
                    }

                    for (int i = 0; i < count; i++)
                    {
                        DestroyParkingLineElement(0, recordUndo);
                    }
                }

                lineDatas.Clear();
            }

            if (recordUndo)
            {
#if UNITY_EDITOR
                EditorExtension.CollapseUndoCurrentOperations();
#endif
            }
        }

        private void DestroyParkingLineElement(int index, bool recordUndo)
        {
            var lineData = lineDatas[index];
            lineDatas.RemoveAt(index);

            if (lineData?.LineData.Count > 0)
            {
                for (int i = 0; i < lineData.LineData.Count; i++)
                {
                    TrafficNode node = lineData.LineData[i];
                    DestroyNode(node, recordUndo, false);
                }
            }
        }

        public void TryToRemoveNodeFromParkingLine(TrafficNode trafficNode)
        {
            if (lineDatas.Count > 0)
            {
                foreach (var lineData in lineDatas)
                {
                    lineData.LineData.TryToRemove(trafficNode);
                }
            }
        }

        public Vector3 GetLinePlaceSize()
        {
            return CurrentParkingLineSettings.PlaceSize;
        }

        public Vector3 GetLineDirection()
        {
            return transform.rotation * CurrentParkingLineSettings.LineDirection;
        }

        public Vector3 GetNodeDirection()
        {
            return transform.rotation * CurrentParkingLineSettings.NodeDirection;
        }

        public Quaternion GetNodeRotation()
        {
            var nodeDirection = GetNodeDirection();
            return Quaternion.LookRotation(-nodeDirection);
        }

        public Vector3 GetLineOffset(int i)
        {
            var size = GetLinePlaceSize();

            var lineDirection = GetLineDirection();
            var nodeDirection = GetNodeDirection();

            var dot = Mathf.Abs(Vector3.Dot(lineDirection, nodeDirection));

            var offset = Mathf.Lerp(size.x, size.z, dot);
            offset += CurrentParkingLineSettings.ParkingPlaceSpacingOffset;

            return lineDirection * i * offset;
        }

        public Vector3 GetEnterParkingPedestrianNodePosition(Vector3 nodePosition)
        {
            var nodeRotation = GetNodeRotation();
            return nodePosition - nodeRotation * CurrentParkingLineSettings.ParkingEnterNodeOffset;
        }

        public Vector3 GetParkingPedestrianNodePosition(Vector3 nodePosition)
        {
            var nodeRotation = GetNodeRotation();
            return nodePosition - nodeRotation * CurrentParkingLineSettings.ParkingNodeOffset;
        }

        public void InitializeTempStartParkingPoint()
        {
            if (roadSegmentType != RoadSegmentType.CustomSegment)
            {
                return;
            }

            if (tempStartParkingPoint == null)
            {
                tempStartParkingPoint = new GameObject("tempStartParkingPoint").transform;
                tempStartParkingPoint.gameObject.hideFlags = HideFlags.HideAndDontSave;
                tempStartParkingPoint.parent = transform;

                UpdateTempParkingPointPosition();

                if (tempStartParkingPoint.position == Vector3.zero)
                {
                    tempStartParkingPoint.position = VectorExtensions.GetCenterOfSceneView();
                }
            }
        }

        public void UpdateTempParkingPointPosition(bool recordUndo = false)
        {
            if (CurrentParkingLineSettings != null)
            {
                if (recordUndo)
                {
#if UNITY_EDITOR
                    Undo.RegisterCompleteObjectUndo(tempStartParkingPoint, "Undo point");
#endif
                }

                tempStartParkingPoint.localPosition = CurrentParkingLineSettings.LineStartPointLocal;
            }
        }

        public void SaveParkingConfig()
        {
            if (!string.IsNullOrEmpty(configName) && tempParkingLineSettings != null)
            {
                var asset = Instantiate(tempParkingLineSettings);

#if UNITY_EDITOR
                AssetDatabaseExtension.SavePersistScriptableObject(asset, roadSegmentCreatorConfig.RoadParkingConfigSavePath, configName);
#endif

                roadSegmentCreatorConfig.CheckForNullConfigs();
                roadSegmentCreatorConfig.AddParkingConfig(asset);
                SyncParkingConfigHeaders();

                EditorSaver.SetObjectDirty(roadSegmentCreatorConfig);
            }
        }

        public void RecalcuteParkingPaths()
        {
            var enterTempPath = GetTempPath(0);

            if (enterTempPath)
            {
                enterTempPath.Nodes[enterTempPath.Nodes.Count - 1].transform.position = tempStartParkingPoint.position;
                enterTempPath.CreatePath(false, false);
            }

            var exitTempPath = GetTempPath(1);

            if (exitTempPath)
            {
                exitTempPath.Nodes[0].transform.position = tempStartParkingPoint.position;
                exitTempPath.CreatePath(false, false);
            }
        }

        public void RecalcuteParkingPaths(Vector3 offset)
        {
            var enterTempPath = GetTempPath(0);

            if (enterTempPath)
            {
                var nodes = enterTempPath.Nodes;

                for (int i = 1; i < nodes.Count; i++)
                {
                    Transform node = nodes[i];

#if UNITY_EDITOR
                    Undo.RecordObject(node, "Undo node position");
#endif

                    node.transform.position += offset;
                }

                enterTempPath.CreatePath(false, true);
            }

            var exitTempPath = GetTempPath(1);

            if (exitTempPath)
            {
                var nodes = exitTempPath.Nodes;

                for (int i = 0; i < nodes.Count - 1; i++)
                {
                    Transform node = nodes[i];

#if UNITY_EDITOR
                    Undo.RecordObject(node, "Undo node position");
#endif

                    node.transform.position += offset;
                }

                exitTempPath.CreatePath(false, true);
            }
        }

        public void CreateTempParkingPaths(ParkingLineSettingsContainer parkingConfig, int selectedPathToolbarOption, bool userCreated = false)
        {
            if (userCreated)
            {
                handleTypeTabIndex = 1;
            }

            switch (parkingConnectionSourceType)
            {
                case RoadSegmentCreator.ParkingConnectionSourceType.Path:
                    {
                        Path selectedPath = parkingSourcePath;
                        TrafficNode trafficNode = parkingSourcePath.SourceTrafficNode;

                        switch (selectedPathToolbarOption)
                        {
                            case 1:
                                {
                                    var path = AddTempPath(RoadSegmentCreator.PathDirectionType.Forward, trafficNode, null, 0);

                                    path.Nodes[0].transform.position = selectedPath.StartPosition;
                                    path.Nodes[1].transform.position = LineStartPointWorld;

                                    path.CreatePath(true);
                                    path.name = "TempEnterPath";
                                    path.ConnectedLaneIndex = parkingSourcePath.ConnectedLaneIndex;
                                    InitConfig(parkingConfig, path, false);
                                    break;
                                }
                            case 2:
                                {
                                    var path = AddTempPath(RoadSegmentCreator.PathDirectionType.Forward, trafficNode, null, 1, true);

                                    path.Nodes[0].transform.position = LineStartPointWorld;

                                    var intersectPoint = PathHelper.GetAttachPoint(selectedPath, LineStartPointWorld);
                                    path.Nodes[1].transform.position = intersectPoint;

                                    path.PathConnectionType = PathConnectionType.PathPoint;
                                    path.ConnectedPath = selectedPath;
                                    path.CreatePath(true);
                                    path.ConnectedLaneIndex = parkingSourcePath.SourceLaneIndex;
                                    path.name = "TempExitPath";
                                    InitConfig(parkingConfig, path, true);
                                    break;
                                }
                        }

                        break;
                    }
                case RoadSegmentCreator.ParkingConnectionSourceType.Node:
                    {
                        CreateTempParkingPathNodeConnection(parkingConfig, sourceTrafficNode, targetTrafficNode, selectedPathToolbarOption);
                        break;
                    }
                case RoadSegmentCreator.ParkingConnectionSourceType.SingleNode:
                    {
                        CreateTempParkingPathNodeConnection(parkingConfig, sourceTrafficNode, sourceTrafficNode, selectedPathToolbarOption);
                        break;
                    }
            }

            switch (selectedPathToolbarOption)
            {
                case 1:
                    {
                        var path = GetTempPath(0);
                        CreateOffsets(path, 0);
                        break;
                    }
                case 2:
                    {
                        var path = GetTempPath(1);
                        CreateOffsets(path, 1);
                        break;
                    }
            }
        }

        public void CreateTempParkingPathNodeConnection(ParkingLineSettingsContainer parkingConfig, TrafficNode sourceNode, TrafficNode targetNode, int selectedPathToolbarOption)
        {
            switch (selectedPathToolbarOption)
            {
                case 1:
                    {
                        var path = AddTempPath(RoadSegmentCreator.PathDirectionType.Forward, sourceNode, null, 0);

                        var sourcePoint = sourceNode.GetLanePosition(connectionLaneIndex);
                        path.Nodes[0].transform.position = sourcePoint;
                        path.Nodes[1].transform.position = LineStartPointWorld;

                        path.CreatePath(true);
                        path.name = "TempEnterPath";
                        path.ConnectedLaneIndex = connectionLaneIndex;
                        path.TrafficGroupMask = parkingConfig.ParkingLineSettings.TrafficGroupMask.GetClone();
                        InitConfig(parkingConfig, path, false);
                        break;
                    }
                case 2:
                    {
                        var path = AddTempPath(RoadSegmentCreator.PathDirectionType.Forward, sourceNode, targetNode, 1, true);

                        path.Nodes[0].transform.position = LineStartPointWorld;

                        var connectPoint = targetNode.GetLanePosition(connectionLaneIndex, true);

                        path.Nodes[1].transform.position = connectPoint;

                        path.PathConnectionType = PathConnectionType.TrafficNode;
                        path.CreatePath(true);
                        path.ConnectedLaneIndex = 0;
                        path.name = "TempExitPath";
                        path.TrafficGroupMask = parkingConfig.ParkingLineSettings.TrafficGroupMask.GetClone();
                        InitConfig(parkingConfig, path, true);
                        break;
                    }
            }
        }

        public void InitConfig(ParkingLineSettingsContainer parkingConfig, Path path, bool exitPath)
        {
            path.PathSpeedLimit = parkingConfig.InitialPathSpeedLimit;
            path.ResetSpeedLimit(false);
            path.Rail = parkingConfig.ParkingLineSettings.IsRail(exitPath);
        }

        public void PlaceCountChanged()
        {
            var placeCount = CurrentParkingLineSettings.PlaceCount;
            var expand = placeCount - enterPathOffsets.Count;

            if (expand != 0)
            {
                if (expand > 0)
                {
                    AddOffsets(expand);
                }
                else
                {
                    RemoveOffsets(expand);
                }
            }
        }

        public void AddOffsets(int placeCount)
        {
            for (int i = 0; i < placeCount; i++)
            {
                enterPathOffsets.Add(new TempParkingPathOffsetData());
            }

            for (int i = 0; i < placeCount; i++)
            {
                exitPathOffsets.Add(new TempParkingPathOffsetData());
            }
        }

        public void CreateOffsets(Path path, int pathDirection)
        {
            switch (pathDirection)
            {
                case 0:
                    {
                        CreateOffsets(path, enterPathOffsets);
                        break;
                    }
                case 1:
                    {
                        CreateOffsets(path, exitPathOffsets);
                        break;
                    }
            }
        }

        public void CreateOffsets(Path path, List<TempParkingPathOffsetData> list)
        {
            var placeCount = CurrentParkingLineSettings.PlaceCount;
            list.Clear();

            for (int i = 0; i < placeCount; i++)
            {
                var offsets = new List<Vector3>();

                if (path)
                {
                    for (int j = 0; j < path.WayPoints.Count; j++)
                    {
                        offsets.Add(new Vector3());
                    }
                }

                list.Add(new TempParkingPathOffsetData()
                {
                    Offsets = offsets
                });
            }
        }

        public void InitOffsets(Path path, List<TempParkingPathOffsetData> list, int index)
        {
            var offsets = new List<Vector3>();

            for (int j = 0; j < path.WayPoints.Count; j++)
            {
                offsets.Add(new Vector3());
            }

            list[index].Offsets = offsets;
        }

        public void RemoveOffsets(int placeCount)
        {
            RemoveOffsets(enterPathOffsets, placeCount);
            RemoveOffsets(exitPathOffsets, placeCount);
        }

        public void ResetOffsets(int index, bool recordUndo = false)
        {
            if (recordUndo)
            {
#if UNITY_EDITOR
                Undo.RegisterCompleteObjectUndo(this, "Undo reset");
#endif
            }

            switch (index)
            {
                case 0:
                    {
                        for (int i = 0; i < enterPathOffsets.Count; i++)
                        {
                            var enterPath = enterPathOffsets[i];

                            if (enterPath != null)
                            {
                                for (int j = 0; j < enterPath.Offsets?.Count; j++)
                                {
                                    enterPath.Offsets[j] = Vector3.zero;
                                }
                            }
                        }

                        break;
                    }
                case 1:
                    {
                        for (int i = 0; i < exitPathOffsets.Count; i++)
                        {
                            var exitPath = exitPathOffsets[i];

                            if (exitPath != null)
                            {
                                for (int j = 0; j < exitPath.Offsets.Count; j++)
                                {
                                    exitPath.Offsets[j] = Vector3.zero;
                                }
                            }
                        }

                        break;
                    }
            }
        }

        public void ParkingRemovePathNode(Path path, int index)
        {
            var tempEnterPath = GetTempPath(0);
            var tempExitPath = GetTempPath(1);

            if (tempEnterPath == path)
            {
#if UNITY_EDITOR
                Undo.RegisterCompleteObjectUndo(this, "Undo remove");
#endif
                for (int i = 0; i < enterPathOffsets.Count; i++)
                {
                    var enterPath = enterPathOffsets[i];
                    enterPath.Offsets.RemoveAt(index);
                }
            }

            if (tempExitPath == path)
            {
#if UNITY_EDITOR
                Undo.RegisterCompleteObjectUndo(this, "Undo remove");
#endif

                for (int i = 0; i < exitPathOffsets.Count; i++)
                {
                    var exitPath = exitPathOffsets[i];
                    exitPath.Offsets.RemoveAt(index);
                }
            }

#if UNITY_EDITOR
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#endif
        }

        public void ParkingAddPathNode(Path path, int index)
        {
            var tempEnterPath = GetTempPath(0);
            var tempExitPath = GetTempPath(1);

            if (tempEnterPath == path)
            {
#if UNITY_EDITOR
                Undo.RegisterCompleteObjectUndo(this, "Undo insert");
#endif

                for (int i = 0; i < enterPathOffsets.Count; i++)
                {
                    var enterPath = enterPathOffsets[i];
                    enterPath.Offsets.Insert(index, new Vector3());
                }
            }

            if (tempExitPath == path)
            {
#if UNITY_EDITOR
                Undo.RegisterCompleteObjectUndo(this, "Undo insert");
#endif

                for (int i = 0; i < exitPathOffsets.Count; i++)
                {
                    var exitPath = exitPathOffsets[i];
                    exitPath.Offsets.Insert(index, new Vector3());
                }
            }

#if UNITY_EDITOR
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#endif
        }

        public void ValidateOffsetPath(Path path, int parkingPlaceIndex, int directionIndex)
        {
            if (!path)
            {
                return;
            }

            if (parkingPlaceIndex >= enterPathOffsets.Count)
            {
                PlaceCountChanged();
            }

            switch (directionIndex)
            {
                case 0:
                    {
                        if (enterPathOffsets[parkingPlaceIndex].Offsets == null || path.WayPoints.Count > enterPathOffsets[parkingPlaceIndex].Offsets.Count)
                        {
                            InitOffsets(path, enterPathOffsets, parkingPlaceIndex);
                        }

                        break;
                    }
                case 1:
                    {
                        if (exitPathOffsets[parkingPlaceIndex].Offsets == null || path.WayPoints.Count > exitPathOffsets[parkingPlaceIndex].Offsets.Count)
                        {
                            InitOffsets(path, exitPathOffsets, parkingPlaceIndex);
                        }

                        break;
                    }
            }
        }

        public void ParkingConfigChanged()
        {
            if (!CurrentParkingLineSettings)
            {
                return;
            }

            RailChanged();
            TrafficMaskChanged();
            ParkingSpeedLimitChanged(0);
            ParkingSpeedLimitChanged(1);
        }

        public void ParkingSpeedLimitChanged(int index, bool saveUndo = false)
        {
            var path = GetTempPath(index);

            if (path)
            {
                path.PathSpeedLimit = CurrentParkingLineSettings.InitialPathSpeedLimit;
                path.ResetSpeedLimit(saveUndo);
            }
        }

        public void RailChanged()
        {
            RailChanged(0);
            RailChanged(1);
        }

        public void RailChanged(int index)
        {
            var path = GetTempPath(index);

            if (path)
            {
                path.Rail = CurrentParkingLineSettings.ParkingLineSettings.IsRail(index);
            }
        }

        public void TrafficMaskChanged()
        {
            var path1 = GetTempPath(0);

            if (path1)
            {
                path1.TrafficGroupMask = CurrentParkingLineSettings.ParkingLineSettings.TrafficGroupMask.GetClone();
            }

            var path2 = GetTempPath(1);

            if (path2)
            {
                path2.TrafficGroupMask = CurrentParkingLineSettings.ParkingLineSettings.TrafficGroupMask.GetClone();
            }
        }

        private void RemoveOffsets(List<TempParkingPathOffsetData> list, int placeCount)
        {
            while (placeCount > 0 || list.Count > 0)
            {
                list.RemoveAt(list.Count - 1);
                placeCount--;
            }
        }

        private void RestoreSavedPath(TempParkingPathData savedPath, Path targetPath)
        {
            if (savedPath == null || targetPath == null)
            {
                return;
            }

            int expand = savedPath.Nodes.Count - targetPath.Nodes.Count;

            for (int i = 0; i < expand; i++)
            {
                targetPath.AddNode(false);
            }

            if (expand > 0)
            {
                targetPath.CreatePath(true, false);
            }

            for (int i = 0; i < savedPath.Nodes.Count; i++)
            {
                var worldPosition = transform.TransformPoint(savedPath.Nodes[i].LocalPosition);

                targetPath.Nodes[i].transform.position = worldPosition;
                targetPath.WayPoints[i].transform.position = worldPosition;
                targetPath.WayPoints[i].BackwardDirection = savedPath.Nodes[i].BackwardDirection;

                if (savedPath.Nodes[i].TrafficGroupMask != null)
                {
                    targetPath.WayPoints[i].TrafficGroupMask = savedPath.Nodes[i].TrafficGroupMask.GetClone();
                }

                targetPath.WayPoints[i].SpeedLimit = savedPath.Nodes[i].SpeedLimit;
            }

            targetPath.TrafficGroupMask = savedPath.trafficGroupMask.GetClone();
        }

        private List<TempParkingPathOffsetData> CloneOffsets(List<TempParkingPathOffsetData> sourceOffsets)
        {
            var clonedList = new List<TempParkingPathOffsetData>(sourceOffsets.Count);

            for (int i = 0; i < sourceOffsets?.Count; i++)
            {
                if (sourceOffsets[i].Offsets != null)
                {
                    var tempParkingPathOffsetData = new TempParkingPathOffsetData()
                    {
                        Offsets = new List<Vector3>(sourceOffsets[i].Offsets)
                    };

                    clonedList.Add(tempParkingPathOffsetData);
                }
            }

            return clonedList;
        }

        private void SyncParkingConfigHeaders()
        {
            if (roadSegmentCreatorConfig != null)
            {
                parkingConfigNames = roadSegmentCreatorConfig.GetParkingConfigNames();

                if (parkingConfigNames != null)
                {
                    if (roadSegmentCreatorConfig.SelectedParkingPresetIndex != 0 && roadSegmentCreatorConfig.SelectedParkingPresetIndex >= parkingConfigNames.Length)
                    {
                        roadSegmentCreatorConfig.SelectedParkingPresetIndex = 0;
                        EditorSaver.SetObjectDirty(roadSegmentCreatorConfig);
                    }
                }
                else
                {
                    roadSegmentCreatorConfig.SelectedParkingPresetIndex = 0;
                }
            }
        }
    }
}
