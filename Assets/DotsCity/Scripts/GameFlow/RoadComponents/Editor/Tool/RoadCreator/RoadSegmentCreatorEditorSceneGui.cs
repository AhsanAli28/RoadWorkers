#if UNITY_EDITOR
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Spirit604.CityEditor.Road.RoadSegmentCreator;

namespace Spirit604.CityEditor.Road
{
    public partial class RoadSegmentCreatorEditor : Editor
    {
        private const float ParkingCircleSize = 4f;
        private const float ExtrudeSphereSize = 1f;

        private void OnSceneGUI()
        {
            if (Tools.current == Tool.Move)
            {
                Tools.current = Tool.None;
            }

            if (creator.GetRoadSegmentType == RoadSegmentCreator.RoadSegmentType.CustomSegment)
            {
                if (creator.ShowSegmentPositionHandle)
                {
                    Vector3 offset = new Vector3(10f, 0, 10f);
                    DrawSegmentHandle(offset);
                }

                if (creator.additionalSettings == AdditionalPathSettingsType.IntersectionCreation)
                {
                    for (int i = 0; i < sceneTrafficNodes?.Length; i++)
                    {
                        var sceneNode = sceneTrafficNodes[i];
                        if (!sceneNode) continue;

                        var contains = selectedTrafficNodes.Contains(sceneNode);
                        var pos = sceneNode.transform.position;

                        if (!contains)
                        {
                            EditorExtension.DrawButton("+", pos, 35f, () =>
                            {
                                selectedTrafficNodes.Add(sceneNode);
                                Repaint();
                            });
                        }
                        else
                        {
                            EditorExtension.DrawButton("-", pos, 35f, () =>
                            {
                                selectedTrafficNodes.Remove(sceneNode);
                                Repaint();
                            });
                        }
                    }
                }

                if (creator.parkingBuilderMode && creator.showSelectPathButtons)
                {
                    switch (creator.parkingConnectionSourceType)
                    {
                        case RoadSegmentCreator.ParkingConnectionSourceType.Path:
                            {
                                Action<Path> selectPath = (path) =>
                                {
                                    var selected = creator.parkingSourcePath == path;
                                    const float width = 35f;

                                    var pos = path.GetMiddlePosition();

                                    if (!selected)
                                    {
                                        Action selectPathCallback = () =>
                                        {
                                            creator.parkingSourcePath = path;
                                        };

                                        EditorExtension.DrawButton("+", pos, width, selectPathCallback);
                                    }
                                    else
                                    {
                                        Action unselectPathCallback = () =>
                                        {
                                            creator.parkingSourcePath = null;
                                        };

                                        EditorExtension.DrawButton("-", pos, width, unselectPathCallback);
                                    }
                                };

                                creator.IterateAllTrafficNodesPath(selectPath);

                                break;
                            }
                        case RoadSegmentCreator.ParkingConnectionSourceType.Node:
                            break;
                    }
                }
            }
            else
            {
                if (creator.ShowSegmentPositionHandle)
                {
                    DrawSegmentHandle();
                }
            }

            if (creator.CreatedTrafficNodes.Count > 0 && creator.CreatedTrafficNodes[0] == null)
            {
                creator.Create();
            }

            if (!creator.showLightIndexes)
            {
                for (int i = 0; i < creator.TrafficNodeCount; i++)
                {
                    var node = creator.TryToGetNode(i);

                    if (!node)
                    {
                        creator.ClearNullNodes();
                        return;
                    }

                    bool canShow = true;

                    if (creator.SelectedTab == TabType.LightSettings)
                    {
                        canShow = creator.selectedLightNodeIndex == -1 || creator.selectedLightNodeIndex == i;
                    }

                    if (canShow)
                    {
                        Handles.Label(node.transform.position, (i + 1).ToString(), trafficNodeGuiStyle);
                    }
                }
            }
            else
            {
                for (int i = 0; i < creator.TrafficNodeCount; i++)
                {
                    var node = creator.TryToGetNode(i);

                    if (node.TrafficLightHandler == null)
                    {
                        continue;
                    }

                    int index = node.TrafficLightHandler.RelatedLightIndex;
                    Handles.Label(node.transform.position, (index).ToString(), trafficNodeGuiStyle);
                }
            }

            if (creator.GetRoadSegmentType != RoadSegmentCreator.RoadSegmentType.CustomStraightRoad)
            {
                if (creator.ShowPathHandles)
                {
                    if (creator.selectedTrafficNodeIndex == -1)
                    {
                        for (int i = 0; i < creator.CreatedTrafficNodes?.Count; i++)
                        {
                            TrafficNodeEditorExtension.ShowPathHandles(creator.CreatedTrafficNodes[i], gUIStyle, creator.PathDirection, creator.showEditButtonsPathNodes, null, creator.lockYAxisMove, creator.showYPosition, creator.roundYPosition, creator.roundYValue);
                        }
                    }
                    else
                    {
                        if (creator.CreatedTrafficNodes?.Count > creator.selectedTrafficNodeIndex)
                        {
                            TrafficNodeEditorExtension.ShowPathHandles(creator.CreatedTrafficNodes[creator.selectedTrafficNodeIndex], gUIStyle, creator.PathDirection, creator.showEditButtonsPathNodes, creator.selectedPath, creator.lockYAxisMove, creator.showYPosition, creator.roundYPosition, creator.roundYValue);
                        }
                    }
                }

                for (int i = 0; i < creator.CreatedTrafficNodes?.Count; i++)
                {
                    var node = creator.CreatedTrafficNodes[i];
                    var selectedPath = creator.selectedPath;
                    bool state = i == creator.selectedTrafficNodeIndex;

                    TrafficNodeEditorExtension.SwitchSelectionState(node, selectedPath, state, creator.PathDirection);
                }

                if (creator.showWaypoints)
                {
                    GUIStyle gUIStyle = new GUIStyle();
                    gUIStyle.normal.textColor = Color.white;

                    if (creator.selectedTrafficNodeIndex == -1)
                    {
                        for (int i = 0; i < creator.CreatedTrafficNodes?.Count; i++)
                        {
                            TrafficNodeEditorExtension.ShowWaypointInfo(creator.CreatedTrafficNodes[i], gUIStyle, creator.showWaypointsInfo);
                        }
                    }
                    else
                    {
                        if (creator.CreatedTrafficNodes?.Count > creator.selectedTrafficNodeIndex)
                        {
                            TrafficNodeEditorExtension.ShowWaypointInfo(creator.CreatedTrafficNodes[creator.selectedTrafficNodeIndex], gUIStyle, creator.showWaypointsInfo, creator.selectedPathIndex);
                        }
                    }
                }
            }

            ProcessCustomRoad();

            if (creator.showLightIndexes)
            {
                var lights = creator.createdLights;

                for (int i = 0; i < lights.Count; i++)
                {
                    TrafficLightObjectEditor.DrawIndexes(lights[i], trafficNodeGuiStyle);
                }
            }

            DrawParkingMode();

            DrawLightHandles();

            if (creator.Extrude)
            {
                HandleExtrude();
            }

            HandleHotkeys();
        }

        private void DrawSegmentHandle()
        {
            DrawSegmentHandle(Vector3.zero);
        }

        private void DrawSegmentHandle(Vector3 offset)
        {
            EditorGUI.BeginChangeCheck();

            Vector3 handleOffset = default;

            if (Selection.gameObjects.Length == 1)
            {
                var newPosition = Handles.PositionHandle(creator.transform.position + offset, creator.transform.rotation) - offset;
                handleOffset = newPosition - creator.transform.position;
            }
            else
            {
                Vector3 sourcePos = default;

                for (int i = 0; i < Selection.gameObjects.Length; i++)
                {
                    sourcePos += Selection.gameObjects[i].transform.position;
                }

                sourcePos /= Selection.gameObjects.Length;

                var newPosition = Handles.PositionHandle(sourcePos, creator.transform.rotation);
                handleOffset = newPosition - sourcePos;
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (Selection.gameObjects.Length == 1)
                {
                    creator.ChangeCreatorPosition(handleOffset, allowMerge: true);
                }
                else
                {
                    IterateAllSelected(creator =>
                    {
                        creator.ChangeCreatorPosition(handleOffset, false, allowMerge: true);
                    });

                    IterateAllSelected(creator =>
                    {
                        creator.RecalculateConnection();
                    });
                }

                EditorExtension.CollapseUndoCurrentOperations();
            }
        }

        private void DrawParkingMode()
        {
            if (!creator.ParkingBuilderMode)
            {
                return;
            }

            var config = creator.CurrentParkingLineSettings;

            if (config == null)
            {
                return;
            }

            const float cursorRadius = 1f;
            const float yBoxSize = 2f;

            if (!creator.tempStartParkingPoint)
            {
                creator.InitializeTempStartParkingPoint();
            }

            var parkingLineHandleType = config.ParkingLineSettings.ParkingLineHandleType;
            var lineHandleObjectType = config.ParkingLineSettings.LineHandleObjectType;
            var rotationSnapType = config.ParkingLineSettings.RotationSnapType;

            switch (parkingLineHandleType)
            {
                case HandleType.None:
                    break;
                case HandleType.Position:
                    {
                        EditorGUI.BeginChangeCheck();

                        var newPosition = Handles.PositionHandle(creator.tempStartParkingPoint.position, creator.transform.rotation);

                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RegisterCompleteObjectUndo(creator.tempStartParkingPoint, "Undo Point");

                            var positionSnapType = creator.CurrentParkingLineSettings.ParkingLineSettings.PositionSnapType;

                            switch (positionSnapType)
                            {
                                case ParkingPositionSnapType.Custom:
                                    {
                                        newPosition -= creator.CurrentParkingLineSettings.ParkingLineSettings.SnapOffset;
                                        MathUtilMethods.CustomRoundVectorValue(ref newPosition, creator.CurrentParkingLineSettings.ParkingLineSettings.PositionSnap);
                                        newPosition += creator.CurrentParkingLineSettings.ParkingLineSettings.SnapOffset;
                                        break;
                                    }
                            }

                            var offset = newPosition - creator.tempStartParkingPoint.position;
                            creator.tempStartParkingPoint.position += offset;
                            creator.LineStartPointWorld = creator.tempStartParkingPoint.position;
                            config.LineStartPointLocal = creator.tempStartParkingPoint.localPosition;

                            if (creator.autoRecalculateParkingPaths)
                            {
                                creator.RecalcuteParkingPaths(offset);
                                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                            }

                            creator.RepaintInspector();
                        }
                        else
                        {
                            config.LineStartPointLocal = creator.tempStartParkingPoint.localPosition;
                            creator.LineStartPointWorld = creator.tempStartParkingPoint.position;
                        }

                        break;
                    }
                case HandleType.Rotation:
                    {
                        Quaternion sourceRotation = Quaternion.identity;

                        switch (lineHandleObjectType)
                        {
                            case LineHandleObjectType.ParkingLine:
                                {
                                    sourceRotation = Quaternion.LookRotation(creator.GetLineDirection(), Vector3.up);
                                    break;
                                }
                            case LineHandleObjectType.ParkingPlace:
                                {
                                    sourceRotation = creator.GetNodeRotation();
                                    break;
                                }
                        }

                        EditorGUI.BeginChangeCheck();

                        var newRotation = Handles.Disc(sourceRotation, creator.tempStartParkingPoint.transform.position, Vector3.up, ParkingCircleSize, false, 0);

                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RegisterCompleteObjectUndo(config, "Undo rotation");

                            newRotation = newRotation * Quaternion.Inverse(creator.transform.rotation);

                            float angle = newRotation.eulerAngles.y;
                            float roundAngle = 0;

                            switch (rotationSnapType)
                            {
                                case ParkingRotationSnapType.RightCorner:
                                    {
                                        roundAngle = 90f;
                                        break;
                                    }
                                case ParkingRotationSnapType.Custom:
                                    {
                                        roundAngle = config.ParkingLineSettings.RotationSnapAngle;
                                        break;
                                    }
                            }

                            MathUtilMethods.CustomRoundValue(ref angle, roundAngle);

                            var newDirection = Vector3.Normalize(Quaternion.Euler(0, angle, 0) * Vector3.forward);

                            if (newDirection.x.IsEqual(0, 0.0001f))
                            {
                                newDirection.x = 0;
                            }
                            if (newDirection.z.IsEqual(0, 0.0001f))
                            {
                                newDirection.z = 0;
                            }

                            switch (lineHandleObjectType)
                            {
                                case LineHandleObjectType.ParkingLine:
                                    {
                                        config.ParkingLineSettings.LineDirection = newDirection;
                                        break;
                                    }
                                case LineHandleObjectType.ParkingPlace:
                                    {
                                        config.ParkingLineSettings.NodeDirection = -newDirection;
                                        break;
                                    }
                            }

                            if (creator.autoRecalculateParkingPaths)
                            {
                                creator.RecalcuteParkingPaths();
                                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                            }

                            creator.RepaintInspector();
                        }

                        break;
                    }
            }

            DrawDisk(creator.LineStartPointWorld, cursorRadius, Color.magenta);

            var nodeCenter = creator.LineStartPointWorld;

            var enterPath = creator.GetTempPath(0);

            bool drawEnterPath = enterPath != null && creator.selectedPathToolbarOption == 0 || creator.selectedPathToolbarOption == 1;

            if (drawEnterPath && creator.ShowPathParkingHandles)
            {
                PathEditorExtension.DrawPathHandles(enterPath, recordWaypoints: true, showEditButtons: creator.showEditPathParkingButtons, customAddNodeCallback: ParkingAddPathNode, customRemoveNodeCallback: ParkingRemovePathNode);
            }

            var exitPath = creator.GetTempPath(1);

            bool drawExitPath = exitPath != null && creator.selectedPathToolbarOption == 0 || creator.selectedPathToolbarOption == 2;

            if (drawExitPath && creator.ShowPathParkingHandles)
            {
                PathEditorExtension.DrawPathHandles(exitPath, recordWaypoints: true, showEditButtons: creator.showEditPathParkingButtons, customAddNodeCallback: ParkingAddPathNode, customRemoveNodeCallback: ParkingRemovePathNode);
            }

            var size = creator.GetLinePlaceSize();

            for (int i = 0; i < config.PlaceCount; i++)
            {
                creator.ValidateOffsetPath(enterPath, i, 0);
                creator.ValidateOffsetPath(exitPath, i, 1);

                Vector3 lineOffset = creator.GetLineOffset(i);
                var currentCenter = nodeCenter + lineOffset;
                var boxCenter = currentCenter + new Vector3(0, yBoxSize / 2);

                var rot = creator.GetNodeRotation();

                var sourceMatrix = Handles.matrix;
                Handles.matrix = Matrix4x4.TRS(boxCenter, rot, Vector3.one);
                Handles.DrawWireCube(Vector3.zero, size);
                Handles.matrix = sourceMatrix;

                var nodeDirection = creator.GetNodeDirection();

                DebugLine.DrawArrow(currentCenter, nodeDirection, Color.green);

                if (config.AddParkingPedestrianNodes)
                {
                    Vector3 pedestrianNodePosition = creator.GetParkingPedestrianNodePosition(currentCenter);

                    const float nodeRadius = 0.5f;
                    DrawDisk(pedestrianNodePosition, nodeRadius, Color.blue);
                    Vector3 pedestrianEnterNodePosition = creator.GetEnterParkingPedestrianNodePosition(currentCenter);

                    DrawDisk(pedestrianEnterNodePosition, nodeRadius, Color.green);
                }

                if (i > 0 || creator.ShowPathParkingOffsetHandles)
                {
                    if (drawEnterPath)
                    {
                        DrawPath(i, enterPath, lineOffset, skipCount: config.NodeCloneCount);
                    }

                    if (drawExitPath)
                    {
                        DrawPath(i, exitPath, lineOffset, skipCount: 0, skipLastCount: config.NodeSkipLastCount, exitPath: true);
                    }
                }

                if (creator.ShowPathParkingOffsetHandles && creator.selectedPathToolbarOption > 0 && (creator.SelectedParkingOffsetPathIndex == i || creator.SelectedParkingOffsetPathIndex == -1))
                {
                    if (drawEnterPath)
                    {
                        var enterPathOffsets = creator.enterPathOffsets;
                        DrawOffsetHandle(enterPathOffsets, enterPath, lineOffset, i, true, skipCount: config.NodeCloneCount);
                    }

                    if (drawExitPath)
                    {
                        var exitPathOffsets = creator.exitPathOffsets;
                        DrawOffsetHandle(exitPathOffsets, exitPath, lineOffset, i, false, skipLastCount: config.NodeSkipLastCount);
                    }
                }
            }

            void DrawDisk(Vector3 pos, float radius, Color color)
            {
                var oldColor = Handles.color;
                Handles.color = color;
                Handles.DrawWireDisc(pos, Vector3.up, radius);
                Handles.color = oldColor;
            }
        }

        private void ParkingRemovePathNode(Path path, int index)
        {
            creator.ParkingRemovePathNode(path, index);
        }

        private void ParkingAddPathNode(Path path, int index)
        {
            creator.ParkingAddPathNode(path, index);
        }

        private void DrawOffsetHandle(List<TempParkingPathOffsetData> pathOffsets, Path path, Vector3 lineOffset, int placeIndex, bool enterPath, int skipCount = 0, int skipLastCount = 0)
        {
            if (!path)
            {
                return;
            }

            for (int j = 1; j < pathOffsets[placeIndex].Offsets.Count - 1; j++)
            {
                if (!path.WayPoints[j])
                {
                    continue;
                }

                EditorGUI.BeginChangeCheck();

                var oldPosition = path.WayPoints[j].transform.position;
                var currentLineOffset = creator.GetPointOffset(j, lineOffset, pathOffsets[placeIndex].Offsets.Count, skipCount, skipLastCount);

                oldPosition += currentLineOffset;

                var sourcePosition = oldPosition;
                oldPosition += pathOffsets[placeIndex].Offsets[j];

                var newPosition = Handles.PositionHandle(oldPosition, Quaternion.identity);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RegisterCompleteObjectUndo(creator, "Undo offset");
                    var offset = newPosition - sourcePosition;
                    pathOffsets[placeIndex].Offsets[j] = offset;
                }
            }
        }

        private void DrawPath(int index, Path path, Vector3 lineOffset, int skipCount = 0, int skipLastCount = 0, bool exitPath = false)
        {
            if (path && path.WayPoints != null)
            {
                var wayPoints = path.WayPoints;

                for (int i = 0; i < wayPoints.Count - 1; i++)
                {
                    PathNode wayPoint = wayPoints[i];
                    PathNode nextWayPoint = wayPoints[i + 1];

                    Vector3 offset = creator.GetPointOffset(i, lineOffset, wayPoints.Count, skipCount, skipLastCount);

                    var pos1 = wayPoint.transform.position + offset;

                    pos1 += creator.GetHandleOffset(index, i, exitPath);

                    offset = creator.GetPointOffset(i, lineOffset, wayPoints.Count, skipCount, skipLastCount, indexOffset: 1);

                    var pos2 = nextWayPoint.transform.position + offset;

                    if (!exitPath)
                    {
                        pos2 += creator.enterPathOffsets[index].Offsets[i + 1];
                    }
                    else
                    {
                        pos2 += creator.exitPathOffsets[index].Offsets[i + 1];
                    }

                    Handles.DrawLine(pos1, pos2);
                }

                if (creator.parkingConnectionSourceType == ParkingConnectionSourceType.SingleNode)
                {
                    if (!exitPath)
                    {
                        if (skipCount == 0)
                        {
                            var pos1 = wayPoints[0].transform.position;
                            var pos2 = pos1 + lineOffset;
                            Handles.DrawLine(pos1, pos2);
                        }
                    }
                    else
                    {
                        if (skipLastCount == 0)
                        {
                            var pos1 = wayPoints[wayPoints.Count - 1].transform.position;
                            var pos2 = pos1 + lineOffset;
                            Handles.DrawLine(pos1, pos2);
                        }
                    }
                }
            }
        }

        private void ProcessCustomRoad()
        {
            if (!creator.IsCustom())
            {
                return;
            }

            for (int i = 0; i < creator.CreatedTrafficNodes.Count; i++)
            {
                if (creator.showTrafficNodeForward)
                {
                    var forward = creator.CreatedTrafficNodes[i].transform.forward;
                    DebugLine.DrawSlimArrow(creator.CreatedTrafficNodes[i].transform.position, forward, Color.white, 10f, 2f);
                }

                TrafficNodeEditorExtension.ShowDivider(creator.CreatedTrafficNodes[i]);
            }

            if (creator.showTrafficNodeHandles)
            {
                for (int i = 0; i < creator.CreatedTrafficNodes.Count; i++)
                {
                    var trafficNode = creator.CreatedTrafficNodes[i];

                    EditorGUI.BeginChangeCheck();

                    var oldPosition = trafficNode.transform.position;
                    var oldRotation = trafficNode.transform.rotation;

                    var newPosition = Handles.PositionHandle(trafficNode.transform.position, trafficNode.transform.rotation);
                    var newRotation = Handles.Disc(trafficNode.transform.rotation, trafficNode.transform.position, Vector3.up, trafficNode.LaneCount * trafficNode.LaneWidth, false, 5f);

                    if (EditorGUI.EndChangeCheck())
                    {
                        TrafficNode mergeNode = null;

                        if (creator.MergedCrossings.TryGetValue(trafficNode, out var mergedCrossing))
                        {
                            mergeNode = trafficNode.MergeNode;

                            if (!mergedCrossing.IsCustomStraight())
                            {
                                mergedCrossing.RecordAllPathUndo(true, true);
                            }
                            else
                            {
                                mergedCrossing.RecordAllOuterPathUndo(mergeNode, true);
                            }
                        }

                        if (!creator.IsCustomStraight())
                        {
                            creator.RecordAllPathUndo(true, true);
                        }
                        else
                        {
                            creator.RecordAllOuterPathUndo(trafficNode, true);
                        }

                        trafficNode.transform.position = newPosition;

                        if (creator.autoSnapPosition && creator.snapObjectType.HasFlag(RoadSegmentCreator.SnapObjectType.TrafficNode) && oldPosition != newPosition)
                        {
                            bool notOneWaySnapping = !trafficNode.IsOneWay || (trafficNode.IsOneWay && trafficNode.LaneCount % 2 != 0);

                            if (creator.addHalfOffset)
                            {
                                notOneWaySnapping = !notOneWaySnapping;
                            }

                            creator.SnapObject(trafficNode.transform, notOneWaySnapping, creator.autoSnapCustomSize);
                        }

                        if (!creator.lockYAxisMove)
                        {
                            trafficNode.transform.position = trafficNode.transform.position.SetY(newPosition.y);
                        }

                        trafficNode.transform.rotation = newRotation;

                        if (creator.autoRoundRotation && oldRotation != newRotation)
                        {
                            trafficNode.transform.RoundAngle(creator.roundAngle);
                        }

                        creator.RecalculateNodeOuterConnections(trafficNode, false);

                        if (mergeNode)
                        {
                            var offset = trafficNode.transform.position - oldPosition;
                            var rotationOffset = trafficNode.transform.rotation * Quaternion.Inverse(oldRotation);

                            mergeNode.transform.position += offset;
                            mergeNode.transform.rotation *= rotationOffset;

                            if (mergedCrossing.GetRoadSegmentType == RoadSegmentCreator.RoadSegmentType.CustomSegment)
                            {
                                mergedCrossing.UpdatePaths();
                            }

                            if (mergedCrossing.GetRoadSegmentType == RoadSegmentCreator.RoadSegmentType.CustomStraightRoad)
                            {
                                mergedCrossing.AttachInnerExternalPaths(mergeNode);
                                mergedCrossing.RecalculateCustomPath();
                            }
                        }

                        if (creator.GetRoadSegmentType == RoadSegmentCreator.RoadSegmentType.CustomSegment)
                        {
                            creator.UpdatePaths();
                        }

                        if (creator.GetRoadSegmentType == RoadSegmentCreator.RoadSegmentType.CustomStraightRoad)
                        {
                            creator.AttachInnerExternalPaths(trafficNode);
                            creator.RecalculateCustomPath();
                        }

                        EditorExtension.CollapseUndoCurrentOperations();
                    }
                }
            }

            DrawCustomPathHandles();
        }

        private void DrawCustomPathHandles()
        {
            if (creator.GetRoadSegmentType != RoadSegmentCreator.RoadSegmentType.CustomStraightRoad)
            {
                return;
            }

            DrawNodes();

            var tempCustomPath = creator.GetTempPath(0);

            if (tempCustomPath == null)
            {
                return;
            }

            GUIStyle gUIStyle = new GUIStyle();
            gUIStyle.normal.textColor = Color.white;

            for (int i = 0; i < tempCustomPath.WayPoints.Count - 1; i++)
            {
                Handles.DrawLine(tempCustomPath.WayPoints[i].transform.position, tempCustomPath.WayPoints[i + 1].transform.position);
            }

            Action<Path, int> changedCallback = (path, index) =>
            {
                creator.RecalculateCustomPath();
            };

            Action<Path, int, Vector3> positionCallback = (path, index, oldPosition) =>
            {
                Transform node = tempCustomPath.Nodes[index];

                if (creator.autoSnapPosition && creator.snapObjectType.HasFlag(RoadSegmentCreator.SnapObjectType.PathNode))
                {
                    creator.SnapObject(node, creator.addHalfOffset, creator.autoSnapCustomSize);
                }

                creator.SetOffsetLanes(index);
            };

            Action<Path, int, Quaternion> rotationCallback = (path, index, oldRotation) =>
            {
                Transform node = tempCustomPath.Nodes[index];

                if (creator.autoRoundRotation && creator.snapObjectType.HasFlag(RoadSegmentCreator.SnapObjectType.PathNode))
                {
                    node.transform.RoundAngle(creator.roundAngle);
                }

                creator.SetOffsetLanes(index);
            };

            PathEditorExtension.DrawPathHandles(
                tempCustomPath,
                gUIStyle,
                creator.showEditButtonsPathNodes,
                true,
                creator.lockYAxisMove,
                creator.showYPosition,
                creator.roundYPosition,
                true,
                creator.roundYValue,
                false,
                changedCallback,
                changedCallback,
                positionCallback,
                rotationCallback);
        }

        private void DrawNodes()
        {
            for (int i = 0; i < creator.CreatedTrafficNodes.Count; i++)
            {
                for (int laneIndex = 0; laneIndex < creator.CreatedTrafficNodes[i].Lanes?.Count; laneIndex++)
                {
                    var path = creator.CreatedTrafficNodes[i].Lanes[laneIndex].paths[0];

                    for (int nodeIndex = 0; nodeIndex < path.Nodes.Count; nodeIndex++)
                    {
                        Handles.DrawSolidDisc(path.Nodes[nodeIndex].transform.position, Vector3.up, 1f);
                    }
                }
            }
        }

        private void DrawLightHandles()
        {
            if (creator.SelectedTab != TabType.LightSettings)
            {
                return;
            }

            if (creator.lightHandleType == HandleType.None)
            {
                return;
            }

            var lights = creator.createdLights;
            var lightBinding = creator.lightBinding;

            for (int i = 0; i < lights.Count; i++)
            {
                var bindindData = lightBinding[i];

                if (!creator.lightType.HasFlag(bindindData.LightType))
                {
                    continue;
                }

                if (creator.selectedLightNodeIndex != -1 && creator.selectedLightNodeIndex != bindindData.Index)
                {
                    continue;
                }

                var sourceLight = lights[i].transform;
                var sourcePosition = sourceLight.position;
                var sourceRotation = lights[i].transform.localRotation;
                var node = creator.TryToGetNode(bindindData.Index);

                if (creator.lightHandleType == HandleType.Position)
                {
                    EditorGUI.BeginChangeCheck();

                    var newPosition = Handles.PositionHandle(sourcePosition, lights[i].transform.rotation);

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (creator.lightSnapPosition)
                        {
                            newPosition = creator.SnapPosition(newPosition, creator.lightAddHalfOffset, creator.lightSnapCustomSize);
                        }

                        var offset = newPosition - sourcePosition;

                        if (node != null)
                        {
                            offset = Quaternion.Inverse(node.transform.rotation) * offset;
                            offset.x *= bindindData.Side;
                            creator.AddLightOffset(offset, bindindData.LightType);
                        }
                        else
                        {
                            sourceLight.position += offset;
                        }
                    }
                }

                if (creator.lightHandleType == HandleType.Rotation)
                {
                    EditorGUI.BeginChangeCheck();

                    var newRotation = Handles.RotationHandle(sourceRotation, sourcePosition);

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (creator.lightAutoRoundRotation)
                        {
                            newRotation = VectorExtensions.RoundAngle(newRotation, creator.lightRoundAngle);
                        }

                        if (node != null)
                        {
                            var diff = Quaternion.Inverse(sourceRotation) * newRotation;
                            creator.AddLightRotation(diff, bindindData.LightType);
                        }
                        else
                        {
                            sourceLight.transform.localRotation = newRotation;
                        }
                    }
                }
            }
        }

        private void HandleExtrude()
        {
            var mouseWorldPosition = Event.current.mousePosition.GUIScreenToWorldSpace(true);

            if (creator.CurrentExtrudeState != ExtrudeState.Default && creator.CurrentExtrudeState != ExtrudeState.Creating)
            {
                creator.CurrentDragPosition = mouseWorldPosition;
            }

            switch (creator.CurrentExtrudeState)
            {
                case ExtrudeState.Default:
                    {
                        for (int i = 0; i < creator.TrafficNodeCount; i++)
                        {
                            var node = creator.TryToGetNode(i);

                            if (creator.PathDirection.HasFlag(TrafficNodeDirectionType.Right))
                            {
                                node.IterateAllLanes((laneIndex, external) =>
                                {
                                    DrawExrudeSphereHandle(node, laneIndex, false);
                                });
                            }

                            if (creator.PathDirection.HasFlag(TrafficNodeDirectionType.Left))
                            {
                                node.IterateExternalLanes((laneIndex, external) =>
                                {
                                    DrawExrudeSphereHandle(node, laneIndex, true);
                                });
                            }
                        }

                        break;
                    }
                case ExtrudeState.WaitingForDrag:
                    {
                        Handles.color = Color.blue;
                        Handles.SphereHandleCap(0, creator.StartDragPosition, Quaternion.identity, ExtrudeSphereSize, EventType.Repaint);

                        var distance = Vector3.Distance(creator.StartDragPosition, creator.CurrentDragPosition);

                        if (distance > RoadSegmentCreator.StartDragDistance)
                        {
                            creator.CurrentExtrudeState = ExtrudeState.IsDrag;
                        }

                        if (Event.current.type.Equals(EventType.MouseUp))
                        {
                            creator.ResetLaneExtrude();
                            Event.current.Use();
                        }

                        break;
                    }
                case ExtrudeState.IsDrag:
                    {
                        DrawPreviewNode();

                        if (Event.current.type.Equals(EventType.MouseUp))
                        {
                            creator.CurrentExtrudeState = ExtrudeState.Creating;
                            Event.current.Use();
                        }

                        break;
                    }
                case ExtrudeState.Creating:
                    {
                        DrawPreviewNode(true);

                        if (Event.current.keyCode == KeyCode.E)
                        {
                            creator.CreateLaneExtrude();
                        }

                        break;
                    }
            }
        }

        private void DrawExrudeSphereHandle(TrafficNode node, int laneIndex, bool external)
        {
            Handles.color = Color.green;

            var position = node.GetLanePosition(laneIndex, external);

            Handles.FreeMoveHandle(position, ExtrudeSphereSize, Vector3.zero, (controlID, position, rotation, hSize, eventType) =>
            {
                Handles.SphereHandleCap(controlID, position, rotation, hSize, eventType);

                if (controlID == GUIUtility.hotControl && GUIUtility.hotControl != 0)
                {
                    GUIUtility.hotControl = 0;
                    creator.StartLaneExtrude(node, laneIndex, external);
                    Repaint();
                }
            });
        }

        private void DrawPreviewNode(bool showHandles = false)
        {
            var sourceLaneIndex = creator.SourceExtrudeLaneIndex;

            int currentExtrudeCount = creator.CurrentExtrudeCount;
            var nodeDirection = creator.ExtrudeNodeRotation * Vector3.forward;
            var nodeRight = creator.ExtrudeNodeRotation * Vector3.right;
            var nodePos = creator.CurrentDragPosition;

            for (int i = 0; i < currentExtrudeCount; i++)
            {
                Handles.color = Color.magenta;

                var p1 = creator.SourceExtrudeNode.GetLanePosition(sourceLaneIndex - i, creator.ExtrudeExternal);

                var connectedLaneIndex = currentExtrudeCount - 1 - i;
                var p3 = TrafficNodeExtension.GetOneWaypoint(nodePos, nodeRight, currentExtrudeCount, creator.SourceExtrudeNode.LaneWidth, connectedLaneIndex, true);
                var p2 = creator.GetExtrudeSplinePoint(p1, p3);

                Handles.SphereHandleCap(0, p1, Quaternion.identity, ExtrudeSphereSize, EventType.Repaint);

                if (i == 0)
                {
                    var newP2 = Handles.PositionHandle(p2 + creator.HandleOffset, Quaternion.identity) - creator.HandleOffset;
                    var offset = newP2 - p2;
                    creator.HandleOffset += offset;
                }

                p2 += creator.HandleOffset;

                var prevPoint = p1;

                for (int j = 0; j < Bezier.SEGMENT_COUNT; j++)
                {
                    var point = Bezier.GetCurvePoint(p1, p2, p3, j, Bezier.SEGMENT_COUNT);
                    Handles.DrawLine(prevPoint, point);

                    prevPoint = point;
                }

                Handles.DrawLine(prevPoint, p3);

                Handles.DrawWireDisc(p2, Vector3.up, 0.5f);

                DebugLine.DrawArrow(p3, nodeDirection, Color.green);

                if (i == 0)
                {
                    var extrudeNode = creator.SourceExtrudeNode;
                    var size = extrudeNode.GetColliderSize();
                    size.x = extrudeNode.LaneWidth * currentExtrudeCount;

                    var matrix = Handles.matrix;

                    Handles.matrix = Matrix4x4.TRS(nodePos + new Vector3(0, size.y / 2), creator.ExtrudeNodeRotation, Vector3.one);

                    Handles.DrawWireCube(Vector3.zero, size);

                    Handles.matrix = matrix;

                    if (showHandles)
                    {
                        creator.CurrentDragPosition = Handles.PositionHandle(creator.CurrentDragPosition, creator.ExtrudeNodeRotation);
                        creator.ExtrudeNodeRotation = Handles.RotationHandle(creator.ExtrudeNodeRotation, creator.CurrentDragPosition);
                    }
                }
            }
        }

        private void HandleStraightPath()
        {
            int controlId = GUIUtility.GetControlID(FocusType.Passive);

            if (Event.current.shift && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                Event.current.Use();
                GUIUtility.hotControl = controlId;

                Vector3 mouseWorldPosition;

                var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

                if (Physics.Raycast(ray, out var hit, float.MaxValue, ~0, QueryTriggerInteraction.Collide))
                {
                    mouseWorldPosition = hit.point;
                }
                else
                {
                    mouseWorldPosition = Event.current.mousePosition.GUIScreenToWorldSpace();
                }

                creator.ContinuePath(mouseWorldPosition);
            }
        }

        private void HandleHotkeys()
        {
            if (creator.GetRoadSegmentType == RoadSegmentCreator.RoadSegmentType.CustomStraightRoad)
            {
                HandleStraightPath();
            }

            if (Event.current.type == EventType.KeyDown)
            {
                if (creator.GetRoadSegmentType == RoadSegmentCreator.RoadSegmentType.CustomSegment)
                {
                    if (Event.current.keyCode == Config.GetKey("SelectNode", KeyCode.R))
                    {
                        Event.current.Use();
                        Vector3 clickPosition = Event.current.mousePosition.GUIScreenToWorldSpace();
                        TryToAddTrafficNodeToPathCreator(clickPosition);
                        return;
                    }
                }

                if (Event.current.keyCode == Config.GetKey("RotateRoad", KeyCode.CapsLock))
                {
                    Event.current.Use();
                    creator.Rotate(90f);
                }

                if (Event.current.keyCode == KeyCode.Keypad0)
                {
                    creator.Create(RoadSegmentCreator.RoadSegmentType.DefaultCrossRoad);
                }
                if (Event.current.keyCode == KeyCode.Keypad1)
                {
                    creator.Create(RoadSegmentCreator.RoadSegmentType.TurnRoad);
                }
                if (Event.current.keyCode == KeyCode.Keypad2)
                {
                    creator.Create(RoadSegmentCreator.RoadSegmentType.StraightRoad);
                }
                if (Event.current.keyCode == KeyCode.Keypad3)
                {
                    creator.Create(RoadSegmentCreator.RoadSegmentType.MergeCrossRoad);
                }
                if (Event.current.keyCode == KeyCode.Keypad4)
                {
                    creator.Create(RoadSegmentCreator.RoadSegmentType.MergeStraightRoad);
                }
                if (Event.current.keyCode == KeyCode.Keypad5)
                {
                    creator.Create(RoadSegmentCreator.RoadSegmentType.MergeCrossRoadToOneWayRoad);
                }
                if (Event.current.keyCode == KeyCode.Keypad6)
                {
                    creator.Create(RoadSegmentCreator.RoadSegmentType.OneWayStraight);
                }
                if (Event.current.keyCode == KeyCode.Keypad7)
                {
                    creator.Create(RoadSegmentCreator.RoadSegmentType.OneWayTurn);
                }
                if (Event.current.keyCode == KeyCode.Keypad8)
                {
                    creator.Create(RoadSegmentCreator.RoadSegmentType.CustomStraightRoad);
                }
                if (Event.current.keyCode == KeyCode.Keypad9)
                {
                    creator.Create(RoadSegmentCreator.RoadSegmentType.CustomSegment);
                }
                if (Event.current.keyCode == KeyCode.Escape)
                {
                    creator.OnEscapeClicked();
                }
            }
        }
    }
}
#endif