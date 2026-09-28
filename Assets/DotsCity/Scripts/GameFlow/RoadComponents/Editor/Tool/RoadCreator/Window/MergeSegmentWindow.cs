#if UNITY_EDITOR
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public class MergeSegmentWindow : EditorWindowBase
    {
        #region Consts

        private float SameDirectionDot = 0.9f;

        #endregion

        #region Helper types

        private enum MergeType { ByTrafficNode, BySegment }

        private enum SearchStep { SourceSegment, SourceNode, MergeSegment, MergeNode, Default }

        #endregion

        #region Serialized variables

        [SerializeField] private MergeType mergeType;

        [SerializeField] private RoadSegmentCreator sourceSegment;

        [SerializeField] private RoadSegmentCreator mergeSegment;

        [SerializeField] private TrafficNode sourceTrafficNode;

        [SerializeField] private TrafficNode mergeTrafficNode;

        [SerializeField] private bool autoRenameNodes = true;

        [SerializeField] private bool convertToStraightLines;

        [SerializeField] private bool destroyMergeSegment = true;

        #endregion

        #region Variables

        private SerializedObject so;
        private List<PedestrianNode> ignoreNodes = new List<PedestrianNode>();
        private RoadSegmentCreator[] roadSegmentCreators;
        private TrafficNode[] sourceNodes;
        private TrafficNode[] mergeNodes;

        #endregion

        #region Properties

        private bool MergeAvaiable
        {
            get
            {
                switch (mergeType)
                {
                    case MergeType.ByTrafficNode:
                        return sourceSegment && mergeSegment && sourceTrafficNode && mergeTrafficNode;
                    case MergeType.BySegment:
                        return sourceSegment && mergeSegment;
                }

                return false;
            }
        }

        #endregion

        #region Unity methods

        public static MergeSegmentWindow ShowWindow()
        {
            var window = (MergeSegmentWindow)GetWindow(typeof(MergeSegmentWindow));
            window.titleContent = new GUIContent("Merge Segment");

            return window;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            so = new SerializedObject(this);
            roadSegmentCreators = ObjectUtils.FindObjectsOfType<RoadSegmentCreator>();
            SceneView.duringSceneGui += SceneView_duringSceneGui;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SceneView.duringSceneGui -= SceneView_duringSceneGui;
        }

        private void OnGUI()
        {
            so.Update();

            var step = (int)GetCurrentStep();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(so.FindProperty(nameof(mergeType)));

            if (EditorGUI.EndChangeCheck())
            {
                Clear();
            }

            EditorGUILayout.PropertyField(so.FindProperty(nameof(sourceSegment)));

            if (mergeType == MergeType.ByTrafficNode)
            {
                GUI.enabled = step >= 1;
                EditorGUILayout.PropertyField(so.FindProperty(nameof(sourceTrafficNode)));
            }

            GUI.enabled = step >= 2;

            EditorGUILayout.PropertyField(so.FindProperty(nameof(mergeSegment)));

            if (mergeType == MergeType.ByTrafficNode)
            {
                GUI.enabled = step >= 3;
                EditorGUILayout.PropertyField(so.FindProperty(nameof(mergeTrafficNode)));
            }

            GUI.enabled = true;

            if (mergeType == MergeType.ByTrafficNode)
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(autoRenameNodes)));
                EditorGUILayout.PropertyField(so.FindProperty(nameof(convertToStraightLines)));
            }

            EditorGUILayout.PropertyField(so.FindProperty(nameof(destroyMergeSegment)));

            so.ApplyModifiedProperties();

            if (GUILayout.Button("Clear"))
            {
                Clear();

            }

            GUI.enabled = MergeAvaiable;

            if (GUILayout.Button("Merge"))
            {
                Merge();
            }

            GUI.enabled = true;
        }

        #endregion

        #region Private methods

        private void Clear()
        {
            sourceSegment = null;
            sourceTrafficNode = null;
            mergeSegment = null;
            mergeTrafficNode = null;
        }

        private void Merge()
        {
            float dot = 0;

            if (mergeType == MergeType.ByTrafficNode)
            {
                dot = Vector3.Dot(sourceTrafficNode.transform.forward, mergeTrafficNode.transform.forward);

                var absDot = Mathf.Abs(dot);

                if (absDot < SameDirectionDot)
                {
                    UnityEngine.Debug.Log($"Only nodes in the same direction can be merged.");
                    return;
                }
            }

            if (PrefabUtility.GetPrefabInstanceHandle(sourceSegment) != null)
            {
                UnityEngine.Debug.Log($"Source segment {sourceSegment.name} is prefab. Unpack prefab and try again.");
                return;
            }

            if (PrefabUtility.GetPrefabInstanceHandle(mergeSegment) != null)
            {
                UnityEngine.Debug.Log($"Merge segment {mergeSegment.name} is prefab. Unpack prefab and try again.");
                return;
            }

            sourceSegment.ConvertToCustom();

            if (mergeType == MergeType.ByTrafficNode)
            {
                var connectedPaths = mergeSegment.GetComponentsInChildren<Path>().Where(a => a.ConnectedTrafficNode == mergeTrafficNode).ToList();

                foreach (var connectedPath in connectedPaths)
                {
                    connectedPath.ConnectedTrafficNode = sourceTrafficNode;

                    if (convertToStraightLines)
                    {
                        connectedPath.ConvertToStraightLine();
                    }

                    if (dot < 0)
                    {
                        connectedPath.ReversedConnectionSide = true;
                    }

                    connectedPath.AttachToNodes();

                    EditorSaver.SetObjectDirty(connectedPath);
                }

                var sourcePaths = mergeSegment.GetComponentsInChildren<Path>().Where(a => a.SourceTrafficNode == mergeTrafficNode).ToList();

                foreach (var sourcePath in sourcePaths)
                {
                    if (convertToStraightLines)
                    {
                        sourcePath.ConvertToStraightLine();
                    }

                    var sourceLaneIndex = sourcePath.SourceLaneIndex;
                    var externalLane = dot < 0;
                    sourceTrafficNode.AddPath(sourcePath, sourceLaneIndex, true, externalLane);

                    if (externalLane)
                    {
                        sourceTrafficNode.LockPathAutoCreation = true;
                    }

                    sourcePath.SourceTrafficNode = sourceTrafficNode;
                    sourcePath.AttachToTrafficNodes(sourceLaneIndex, externalLane);

                    EditorSaver.SetObjectDirty(sourcePath);
                    EditorSaver.SetObjectDirty(sourceTrafficNode);
                }
            }

            var sourceNodes = sourceSegment.trafficLightCrossroad.TrafficNodes;
            var mergeNodes = mergeSegment.trafficLightCrossroad.TrafficNodes;

            int startIndex = sourceNodes.Count + 1;

            var sourceCrossroad = sourceSegment.trafficLightCrossroad;

            foreach (var otherMergeNode in mergeNodes)
            {
                if (otherMergeNode == mergeTrafficNode || otherMergeNode == null)
                    continue;

                MergeTrafficLight(sourceCrossroad, otherMergeNode);

                if (mergeType == MergeType.ByTrafficNode)
                {
                    otherMergeNode.transform.parent = sourceTrafficNode.transform.parent;
                }
                else
                {
                    otherMergeNode.transform.parent = sourceSegment.trafficLightCrossroad.TrafficNodes[0].transform.parent;
                }

                otherMergeNode.transform.SetAsLastSibling();

                sourceSegment.CreatedTrafficNodes.TryToAdd(otherMergeNode);

                MergeCrosswalk(otherMergeNode, dot);

                if (autoRenameNodes)
                {
                    otherMergeNode.name = $"TrafficNode{startIndex}";
                }

                startIndex++;
            }

            MergeParkingLines();

            EditorSaver.SetObjectDirty(sourceSegment);
            EditorSaver.SetObjectDirty(sourceCrossroad);

            if (destroyMergeSegment)
            {
                DestroyImmediate(mergeSegment.gameObject);
                mergeSegment = null;
                mergeTrafficNode = null;
            }

            EditorGUIUtility.PingObject(sourceSegment);

            UnityEngine.Debug.Log($"Merge complete.");
        }

        private void MergeTrafficLight(TrafficLightCrossroad sourceCrossroad, TrafficNode otherMergeNode)
        {
            sourceCrossroad.TrafficNodes.TryToAdd(otherMergeNode);

            if (!otherMergeNode.TrafficLightCrossroad)
                return;

            var handler = otherMergeNode.TrafficLightCrossroad.GetTrafficLightHandlerContainsNode(otherMergeNode);

            if (handler != null)
            {
                var index = handler.RelatedLightIndex;

                var newHandler = sourceCrossroad.GetTrafficLightHandler(index);

                otherMergeNode.TrafficLightHandler = newHandler;

                EditorSaver.SetObjectDirty(otherMergeNode);
            }
        }

        private void MergeCrosswalk(TrafficNode otherMergeNode, float dot)
        {
            if (mergeType != MergeType.ByTrafficNode)
                return;

            if (otherMergeNode.TrafficNodeCrosswalk && sourceTrafficNode.TrafficNodeCrosswalk)
            {
                var node1 = otherMergeNode.GetCrosswalkNode(0);
                var node2 = otherMergeNode.GetCrosswalkNode(1);

                var otherNode1 = sourceTrafficNode.GetCrosswalkNode(0);
                var otherNode2 = sourceTrafficNode.GetCrosswalkNode(1);

                ignoreNodes.Clear();
                ignoreNodes.Add(node1);
                ignoreNodes.Add(node2);
                ignoreNodes.Add(otherNode1);
                ignoreNodes.Add(otherNode2);

                MergePedestrianNode(node1, dot);
                MergePedestrianNode(node2, -dot);
            }
        }

        private void MergePedestrianNode(PedestrianNode node, float dot)
        {
            if (!node || !node.gameObject.activeSelf)
                return;

            var connectedNodes = node.DefaultConnectedPedestrianNodes.ToList();
            connectedNodes.AddRange(node.AutoConnectedPedestrianNodes);
            connectedNodes = connectedNodes.Except(ignoreNodes).ToList();

            if (connectedNodes?.Count > 0)
            {
                var newConnectedNode = dot < 0 ? sourceTrafficNode.GetCrosswalkNode(1) : sourceTrafficNode.GetCrosswalkNode(0);

                foreach (var item in connectedNodes)
                {
                    newConnectedNode.AddConnection(item);
                }
            }
        }

        private void MergeParkingLines()
        {
            if (mergeSegment.GetRoadSegmentType == RoadSegmentCreator.RoadSegmentType.CustomSegment && mergeSegment.lineDatas?.Count > 0)
            {
                sourceSegment.lineDatas.AddRange(mergeSegment.lineDatas);
            }
        }

        private SearchStep GetCurrentStep()
        {
            if (sourceSegment == null)
                return SearchStep.SourceSegment;

            if (mergeType == MergeType.ByTrafficNode && sourceTrafficNode == null)
                return SearchStep.SourceNode;

            if (mergeSegment == null)
                return SearchStep.MergeSegment;

            if (mergeType == MergeType.ByTrafficNode && mergeTrafficNode == null)
                return SearchStep.MergeNode;

            return SearchStep.Default;
        }

        private void ShowSegment(Action<RoadSegmentCreator> callback, RoadSegmentCreator ignoreSegment = null)
        {
            for (int i = 0; i < roadSegmentCreators.Length; i++)
            {
                if (!roadSegmentCreators[i] || roadSegmentCreators[i] == ignoreSegment)
                    continue;

                var position = roadSegmentCreators[i].transform.position;

                EditorExtension.DrawButton("+", position, 35f, () => callback(roadSegmentCreators[i]), fontSize: 16);
            }
        }

        private void ShowNode(TrafficNode[] nodes, Action<TrafficNode> callback)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                var position = nodes[i].transform.position;

                EditorExtension.DrawButton("+", position, 35f, () => callback(nodes[i]), fontSize: 16);
            }
        }

        private void SelectSourceSegment(RoadSegmentCreator roadSegmentCreator)
        {
            sourceSegment = roadSegmentCreator;
            sourceNodes = roadSegmentCreator.GetComponentsInChildren<TrafficNode>();
            Repaint();
        }

        private void SelectMergeSegment(RoadSegmentCreator roadSegmentCreator)
        {
            mergeSegment = roadSegmentCreator;
            mergeNodes = roadSegmentCreator.GetComponentsInChildren<TrafficNode>();
            Repaint();
        }

        #endregion

        #region Event handlers

        private void SceneView_duringSceneGui(SceneView obj)
        {
            var step = GetCurrentStep();

            switch (step)
            {
                case SearchStep.Default:
                    break;
                case SearchStep.SourceSegment:
                    {
                        ShowSegment(SelectSourceSegment);
                        break;
                    }
                case SearchStep.SourceNode:

                    ShowNode(sourceNodes, (node) =>
                    {
                        sourceTrafficNode = node;
                        Repaint();
                    });

                    break;
                case SearchStep.MergeSegment:
                    ShowSegment(SelectMergeSegment, sourceSegment);
                    break;
                case SearchStep.MergeNode:
                    ShowNode(mergeNodes, (node) =>
                    {
                        mergeTrafficNode = node;
                        Repaint();
                    });
                    break;
            }
        }

        #endregion
    }
}
#endif