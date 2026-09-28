#if UNITY_EDITOR
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public class TrafficNodesGUIHelper
    {
        private const string SourceNodeLabel = "1";
        private const string TargetNodeLabel = "2";

        private static Color selectedPointDefaultColor = Color.green;
        private static Vector3 labelOffset = new Vector3(-3, 0, 0);
        private static Vector3 rectSize = new Vector2(100, 100);
        private static float sceneButtonWidth = 50f;
        private static float selectedRadius = 2f;

        public static void DrawNodeButtons(IEnumerable<TrafficNode> allNodes, TrafficNode sourceTrafficNode, TrafficNode targetTrafficNode, Action<TrafficNode> onAddCallback, Action<TrafficNode> onRemoveCallback)
        {
            DrawNodeButtons(allNodes, sourceTrafficNode, targetTrafficNode, onAddCallback, onRemoveCallback, Color.white);
        }

        public static void DrawNodeButtons(IEnumerable<TrafficNode> allNodes, TrafficNode sourceTrafficNode, TrafficNode targetTrafficNode, Action<TrafficNode> onAddCallback, Action<TrafficNode> onRemoveCallback, Color fontColor)
        {
            var indexGUIStyle = new GUIStyle();
            indexGUIStyle.normal.textColor = fontColor;
            indexGUIStyle.fontSize = 36;
            indexGUIStyle.fontStyle = FontStyle.Bold;

            var buttonGuiStyle = new GUIStyle("button");
            buttonGuiStyle.fontSize = 24;
            buttonGuiStyle.normal.textColor = Color.black;

            DrawNodeCube(sourceTrafficNode, SourceNodeLabel, indexGUIStyle);

            if (targetTrafficNode != sourceTrafficNode)
                DrawNodeCube(targetTrafficNode, TargetNodeLabel, indexGUIStyle);

            foreach (var node in allNodes)
            {
                if (node == null)
                    continue;

                if (node.MergeNode)
                {
                    if (node != sourceTrafficNode && node != targetTrafficNode)
                    {
                        if (node.MergeNode != sourceTrafficNode && node.MergeNode != targetTrafficNode)
                        {
                            if (node.MergeNode.GetInstanceID() < node.GetInstanceID())
                                continue;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                var guiPosition = HandleUtility.WorldToGUIPoint(node.transform.position);
                Rect rect = new Rect(guiPosition, rectSize);

                Handles.BeginGUI();

                try
                {
                    GUILayout.BeginArea(rect);
                }
                catch
                {
                    Handles.EndGUI();
                    break;
                }

                GUILayout.BeginHorizontal();

                bool contains = false;

                if (sourceTrafficNode != null)
                {
                    contains = node == sourceTrafficNode;
                }

                if (!contains && sourceTrafficNode != targetTrafficNode && targetTrafficNode != null)
                {
                    contains = node == targetTrafficNode;
                }

                if (!contains)
                {
                    if (GUILayout.Button("+", buttonGuiStyle, GUILayout.Width(sceneButtonWidth)))
                    {
                        onAddCallback(node);
                    }
                }
                else
                {
                    if (GUILayout.Button("-", buttonGuiStyle, GUILayout.Width(sceneButtonWidth)))
                    {
                        onRemoveCallback(node);
                    }
                }

                GUILayout.EndHorizontal();
                GUILayout.EndArea();
                Handles.EndGUI();
            }
        }

        private static void DrawNodeCube(TrafficNode trafficNode, string label, GUIStyle indexGUIStyle)
        {
            if (trafficNode == null)
                return;

            var boxCollider = trafficNode.GetComponent<BoxCollider>();

            Vector3 nodeSize = default;

            if (boxCollider)
            {
                nodeSize = boxCollider.size;
            }
            else
            {
                nodeSize = new Vector3(trafficNode.CalculatedRouteWidth * 2, 2f, 1f);
            }

            var matrix = Handles.matrix;

            Handles.matrix = trafficNode.transform.localToWorldMatrix;
            Handles.DrawWireCube(new Vector3(0, nodeSize.y / 2), nodeSize);

            Handles.matrix = matrix;

            Handles.Label(trafficNode.transform.position + labelOffset, label, indexGUIStyle);
        }

        public static Vector3 DrawSelectedLanePoint(TrafficNode sourceNode, int laneIndex, bool isRightSide, bool isSource)
        {
            return DrawSelectedLanePoint(sourceNode, laneIndex, isRightSide, isSource, selectedPointDefaultColor);
        }

        public static Vector3 DrawSelectedLanePoint(TrafficNode sourceNode, int laneIndex, bool isRightSide, bool isSource, Color pointColor)
        {
            if (sourceNode != null)
            {
                var selectedPoint = GetSelectedLanePoint(sourceNode, laneIndex, isRightSide, isSource);

                if (selectedPoint != Vector3.zero)
                {
                    var oldColor = Handles.color;
                    Handles.color = pointColor;
                    Handles.DrawWireDisc(selectedPoint, Vector3.up, selectedRadius);
                    Handles.color = oldColor;

                    return selectedPoint;
                }
            }

            return Vector3.zero;
        }

        public static Vector3 GetSelectedLanePoint(TrafficNode trafficNode, int laneIndex, bool isRightSide, bool isSource)
        {
            if (!isSource)
            {
                isRightSide = !isRightSide;
            }

            if (trafficNode)
            {
                return trafficNode.GetLanePosition(laneIndex, !isRightSide);
            }

            return Vector3.zero;
        }
    }
}
#endif