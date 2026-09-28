using Spirit604.Attributes;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.CityEditor.Utils
{
    /// <summary>
    /// Class to automatically create an intersection when the intersection was created by connecting a set of external paths before instead of a separate segment.
    /// </summary>
    public class ExternalCrossingCreator : MonoBehaviour
    {
        [Serializable]
        public class ExternalCrossing
        {
            public Vector3 Position;
            public List<TrafficNode> Nodes = new List<TrafficNode>();
        }

        public List<ExternalCrossing> crossings = new List<ExternalCrossing>();
        public List<Path> newPaths = new List<Path>();

        private HashSet<TrafficNode> ignoreNodes = new HashSet<TrafficNode>();

        [Button]
        public void FindCrossings()
        {
            crossings.Clear();
            ignoreNodes.Clear();

            var allNodes = ObjectUtils.FindObjectsOfType<TrafficNode>();

            foreach (var node in allNodes)
            {
                if (ignoreNodes.Contains(node)) continue;

                bool added = false;

                node.IterateExternalLanes((lane, external) =>
                {
                    if (added) return;

                    var laneData = node.TryToGetLaneData(lane, external);

                    if (laneData.paths.Count > 1)
                    {
                        var externalCrossing = new ExternalCrossing();

                        var sourceNode = laneData.paths[0].SourceTrafficNode;

                        IteratePaths(externalCrossing, sourceNode);

                        Vector3 pos = default;

                        for (var i = 0; i < externalCrossing.Nodes.Count; i++)
                        {
                            pos += externalCrossing.Nodes[i].transform.position;
                        }

                        pos /= externalCrossing.Nodes.Count;

                        externalCrossing.Position = pos;

                        if (externalCrossing.Nodes.Count > 2)
                            crossings.Add(externalCrossing);

                        added = true;
                    }
                });
            }
        }

        [Button]
        public void Create()
        {
            if (crossings.Count == 0)
                FindCrossings();

            var roadParent = ObjectUtils.FindObjectOfType<RoadParent>();

            if (roadParent == null)
            {
                Debug.LogError("RoadParent not found");
                return;
            }

            newPaths.Clear();

            var oldPaths = new List<Path>();

            for (int i = 0; i < crossings.Count; i++)
            {
                var nodes = crossings[i].Nodes;
                var intersection = RoadSceneUtils.CreateIntersection(crossings[i].Nodes, segmentName: $"GeneratedCrossing{i + 1}");
                var newNodes = intersection.CreatedTrafficNodes;

                intersection.ClearAutoPaths();
                intersection.transform.parent = roadParent.transform;

                for (var j = 0; j < nodes.Count; j++)
                {
                    var node = nodes[j];

                    oldPaths.Clear();

                    node.IterateExternalPaths(path =>
                    {
                        var copyPath = Instantiate(path);

                        copyPath.name = path.name;
                        copyPath.SourceTrafficNode = copyPath.SourceTrafficNode.MergeNode;
                        copyPath.ConnectedTrafficNode = copyPath.ConnectedTrafficNode.MergeNode;

                        newNodes[j].AddPath(copyPath, path.SourceLaneIndex, true, false);

                        copyPath.transform.position = path.transform.position;
                        copyPath.transform.rotation = path.transform.rotation;

                        newPaths.Add(copyPath);

                        oldPaths.Add(path);
                    });

                    oldPaths.DestroyGameObjects();
                }
            }

            EditorSaver.SetObjectDirty(this);
        }

        private void IteratePaths(ExternalCrossing externalCrossing, TrafficNode sourceNode)
        {
            if (ignoreNodes.Contains(sourceNode))
                return;

            ignoreNodes.Add(sourceNode);

            externalCrossing.Nodes.Add(sourceNode);

            sourceNode.IterateExternalPaths(path =>
            {
                IteratePaths(externalCrossing, path.ConnectedTrafficNode);
            });
        }

        private void OnDrawGizmos()
        {
            foreach (var item in crossings)
            {
                Gizmos.DrawWireCube(item.Position, Vector3.one * 20);
            }
        }
    }
}
