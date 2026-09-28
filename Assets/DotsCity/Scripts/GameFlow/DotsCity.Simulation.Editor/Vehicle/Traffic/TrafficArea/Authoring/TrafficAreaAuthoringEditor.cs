#if UNITY_EDITOR
using Spirit604.CityEditor.Road;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.TrafficArea.Authoring
{
    [CustomEditor(typeof(TrafficAreaAuthoring))]
    public class TrafficAreaAuthoringEditor : SharedSettingsEditorBase<TrafficAreaAuthoringEditor.EditorSettings>
    {
        [Serializable]
        public class EditorSettings
        {
            public bool SettingsFlag = true;
            public bool NodeSettingsFlag = false;
            public bool NodeVisualSettingsFlag = true;
            public bool NodeFlag = true;
        }

        private const float AddButtonScreenSize = 35f;

        private TrafficAreaAuthoring trafficAreaAuthoring;
        private TrafficNode[] nodes;
        private bool colorsFoldout;
        private string[] buttonSelectHeaders;
        private string[] trafficAreaNodeTypeHeaders;

        protected override string SaveKey => "TrafficAreaAuthoringEditorKey";

        protected override void OnEnable()
        {
            base.OnEnable();

            nodes = ObjectUtils.FindObjectsOfType<TrafficNode>();
            trafficAreaAuthoring = target as TrafficAreaAuthoring;

            if (trafficAreaNodeTypeHeaders == null || trafficAreaNodeTypeHeaders.Length == 0)
            {
                var buttonSelectHeadersList = Enum.GetValues(typeof(TrafficAreaAuthoring.ButtonSelectType)).Cast<TrafficAreaAuthoring.ButtonSelectType>().Select(a => a.ToString()).ToList();

                for (int i = 0; i < buttonSelectHeadersList.Count; i++)
                {
                    buttonSelectHeadersList[i] = StringExtension.CamelToLabel(buttonSelectHeadersList[i]);
                }

                buttonSelectHeaders = buttonSelectHeadersList.ToArray();
                trafficAreaNodeTypeHeaders = Enum.GetValues(typeof(TrafficAreaNodeType)).Cast<TrafficAreaNodeType>().Select(a => a.ToString()).ToArray();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            System.Action settingsCallback = () =>
             {
                 EditorGUILayout.PropertyField(serializedObject.FindProperty("maxQueueCount"));
                 EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSkipEnterOrderCount"));
                 EditorGUILayout.PropertyField(serializedObject.FindProperty("hasExitOrder"));
             };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Settings", settingsCallback, ref SharedSettings.SettingsFlag);

            InspectorExtension.DrawDefaultInspectorGroupBlock("Node Settings", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("nodeSettingsDictionary"));

            }, ref SharedSettings.NodeSettingsFlag);

            System.Action sceneVisualCallback = () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("drawConnection"));

                if (trafficAreaAuthoring.DrawConnection)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("drawConnectionLines"));

                    EditorGUILayout.LabelField("Button Select Type");
                    trafficAreaAuthoring.ButtonSelectIndex = GUILayout.Toolbar(trafficAreaAuthoring.ButtonSelectIndex, buttonSelectHeaders);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("showTrafficAreaNodeType"));
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("showSceneNodes"));

                if (trafficAreaAuthoring.ShowSceneNodes)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("allowDuplicateNodes"));

                    EditorGUILayout.LabelField("New Node Type");
                    trafficAreaAuthoring.NewNodeTypeIndex = GUILayout.Toolbar(trafficAreaAuthoring.NewNodeTypeIndex, trafficAreaNodeTypeHeaders);
                }

                if (trafficAreaAuthoring.DrawConnection)
                {
                    colorsFoldout = EditorGUILayout.Foldout(colorsFoldout, "Colors");

                    if (colorsFoldout)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("nodeColorDictionary"));
                    }
                }
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Scene Visual", sceneVisualCallback, ref SharedSettings.NodeVisualSettingsFlag);

            switch (trafficAreaAuthoring.SelectType)
            {
                case TrafficAreaAuthoring.ButtonSelectType.SelectNode:
                    {
                        System.Action selectedNodeCallback = () =>
                        {
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("selectedNode"));

                            if (trafficAreaAuthoring.SelectedNode != null)
                            {
                                TrafficNodeInspectorExtension.DrawInspectorSettings(trafficAreaAuthoring.SelectedNode);
                            }
                        };

                        InspectorExtension.DrawDefaultInspectorGroupBlock("Selected Node Data", selectedNodeCallback);

                        break;
                    }
            }

            System.Action dataCallback = () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("nodeData"));
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Nodes", dataCallback, ref SharedSettings.NodeFlag);

            if (GUILayout.Button("Clear Null Nodes"))
            {
                trafficAreaAuthoring.ClearNullNodes();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            DrawConnections();

            ShowSceneNodes();
        }

        private void DrawConnections()
        {
            if (trafficAreaAuthoring.DrawConnection)
            {
                TryToDrawNodes(TrafficAreaNodeType.Enter);
                TryToDrawNodes(TrafficAreaNodeType.Queue);
                TryToDrawNodes(TrafficAreaNodeType.Default);
                TryToDrawNodes(TrafficAreaNodeType.Exit);
            }
        }

        private void TryToDrawNodes(TrafficAreaNodeType trafficAreaNodeType)
        {
            var flags = trafficAreaAuthoring.ShowTrafficAreaNodeType;

            if (flags.HasFlag(trafficAreaNodeType))
            {
                var nodes = trafficAreaAuthoring.TryToGetNodes(trafficAreaNodeType);
                var color = trafficAreaAuthoring.GetNodeColor(trafficAreaNodeType);

                DrawNodes(nodes, trafficAreaNodeType, color);
            }
        }

        private void ShowSceneNodes()
        {
            if (trafficAreaAuthoring.ShowSceneNodes)
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    TrafficNode node = nodes[i];

                    if (!trafficAreaAuthoring.HasNode(node) || trafficAreaAuthoring.AllowDuplicateNodes)
                    {
                        DrawAddButton(node, trafficAreaAuthoring.NewNodeType);
                    }
                }
            }
        }

        private void DrawNodes(List<TrafficNode> nodes, TrafficAreaNodeType trafficAreaNodeType, Color color)
        {
            for (int i = 0; i < nodes?.Count; i++)
            {
                TrafficNode node = nodes[i];

                if (node == null)
                {
                    continue;
                }

                var nodePosition = node.transform.position;
                var size = Vector3.one;

                EditorExtension.DrawSimpleHandlesCube(nodePosition, size, color);

                var direction = trafficAreaAuthoring.GetNodePlace(trafficAreaNodeType);

                Handles.color = color;

                bool rightLanes = true;

                if (!node.HasRightLanes || direction == TrafficAreaAuthoring.NodePlaceType.ForceLeftLane)
                {
                    rightLanes = false;
                }

                var laneCount = node.GetLaneCount(!rightLanes);

                for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
                {
                    Vector3 lanePos = node.GetLanePosition(laneIndex, !rightLanes);

                    if (lanePos != Vector3.zero)
                    {
                        Handles.DrawWireDisc(lanePos, Vector3.up, 1f);
                    }
                }

                if (trafficAreaAuthoring.DrawConnectionLines)
                {
                    Handles.DrawLine(trafficAreaAuthoring.transform.position, nodePosition);
                }

                switch (trafficAreaAuthoring.SelectType)
                {
                    case TrafficAreaAuthoring.ButtonSelectType.Disabled:
                        break;
                    case TrafficAreaAuthoring.ButtonSelectType.RemoveNode:
                        {
                            var contains = trafficAreaAuthoring.HasNode(node);

                            if (contains)
                            {
                                System.Action removeCallback = () =>
                                {
                                    trafficAreaAuthoring.RemoveNode(node);
                                };

                                EditorExtension.DrawButton("-", nodePosition, AddButtonScreenSize, removeCallback, centralizeGuiAlign: true);
                            }
                            break;
                        }
                    case TrafficAreaAuthoring.ButtonSelectType.SelectNode:
                        {
                            var contains = trafficAreaAuthoring.HasNode(node);

                            if (contains)
                            {
                                System.Action selectCallback = () =>
                                {
                                    trafficAreaAuthoring.SelectedNode = node;
                                };

                                string text = trafficAreaAuthoring.SelectedNode != node ? "N" : "N-";

                                EditorExtension.DrawButton(text, nodePosition, AddButtonScreenSize, selectCallback, centralizeGuiAlign: true);
                            }
                            break;
                        }
                }
            }
        }

        private void DrawAddButton(TrafficNode node, TrafficAreaNodeType trafficAreaNodeType)
        {
            var position = node.transform.position;

            System.Action addCallback = () =>
            {
                trafficAreaAuthoring.AddNode(node, trafficAreaNodeType);
            };

            EditorExtension.DrawButton("+", position, AddButtonScreenSize, addCallback, centralizeGuiAlign: true);
        }

        protected override EditorSettings GetDefaultSettings()
        {
            return new EditorSettings();
        }
    }
}
#endif