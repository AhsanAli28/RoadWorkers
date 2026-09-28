using Spirit604.Collections.Dictionary;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.TrafficPublic.Authoring
{
    public abstract class TrafficRoute : MonoBehaviour
    {
        #region Helper Types

        [Serializable]
        public class RouteChangeLaneTransition
        {
            public Path SourcePath;
            public Path TargetPath;
            [Range(-50, 50)] public float SourceOffset;
            [Range(-50, 50)] public float TargetOffset;
            [Range(0, 90)] public float SpeedLimit;
        }

        [Serializable]
        public class TrafficNodeRouteDictionary : AbstractSerializableDictionary<TrafficNode, TrafficNodeData>
        {
        }

        [Serializable]
        public class TrafficNodeData
        {
            public List<Path> RelatedPaths = new List<Path>();
        }

        public enum NodeType { Default, StartTransition, EndTransition }

        #endregion

        #region Inspector variables

        [Tooltip("Offset start point of transition in source path")]
        [SerializeField][Range(0, 50)] private float sourceOffset = 4f;

        [Tooltip("Offset end point of transition in target path")]
        [SerializeField][Range(0, 50)] private float targetOffset = 8f;

        [Tooltip("Max distance between traffic nodes to find a transition path")]
        [SerializeField][Range(0, 10)] private float distanceBeetweenParallelNodes = 4f;

#if UNITY_EDITOR
        public bool hightlightRoute = true;
        public bool showPathSelectionButtons = true;
        public bool showSwapButtons = true;
        public bool routesFoldout = true;
        public bool transitionSettingsFoldout = true;
        public bool sceneSettingsFoldout = true;
        public bool routeDataFoldout = true;
#endif

        [SerializeField] private bool showOnlyRelatedNodes = true;
        [SerializeField] private List<Path> routes = new List<Path>();
        [SerializeField] private TrafficNodeRouteDictionary trafficNodeRouteData = new TrafficNodeRouteDictionary();

        public List<RouteChangeLaneTransition> routeChangeLaneTransitions = new List<RouteChangeLaneTransition>();

        #endregion

        #region Properties

        public List<Path> Routes { get => routes; }

        public List<RouteChangeLaneTransition> RouteChangeLaneTransitions { get => routeChangeLaneTransitions; }

        public float TransitionDistance { get => sourceOffset; set => sourceOffset = value; }

        #endregion

        #region Methods

        public void AddPath(Path path)
        {
            bool added = routes.TryToAdd(path);

            if (!added)
            {
                return;
            }

            if (routes.Count > 1)
            {
                bool shouldAddTransition = false;
                Path parallelPath = null;

                for (int i = 0; i < routes.Count; i++)
                {
                    if (routes[i] == path)
                    {
                        continue;
                    }

                    if (routes[i].SourceTrafficNode == path.SourceTrafficNode)
                    {
                        float distance = Vector3.Distance(routes[i].StartPosition, path.StartPosition);

                        shouldAddTransition = distance < distanceBeetweenParallelNodes;
                    }

                    if (shouldAddTransition)
                    {
                        parallelPath = routes[i];
                        break;
                    }
                }

                if (shouldAddTransition)
                {
                    bool forward = true;
                    var sourcePath = forward ? parallelPath : path;
                    var targetPath = forward ? path : parallelPath;

                    AddRouteTransition(sourcePath, targetPath);
                }
            }

            AddNodeDataFromPath(path);


            EditorSaver.SetObjectDirty(this);
        }

        public void RemovePath(Path path)
        {
            if (path != null)
            {
#if UNITY_EDITOR
                path.Highlighted = false;
                path.HightlightNormalizedLength = 1f;
#endif
            }

            routes.TryToRemove(path);

            RemoveTransition(path);

            EditorSaver.SetObjectDirty(this);
        }

        public RouteChangeLaneTransition GetTransition(Path path, bool isSourcePath)
        {
            for (int i = 0; i < routeChangeLaneTransitions.Count; i++)
            {
                if (isSourcePath)
                {
                    if (routeChangeLaneTransitions[i].SourcePath == path)
                    {
                        return routeChangeLaneTransitions[i];
                    }
                }
                else
                {
                    if (routeChangeLaneTransitions[i].TargetPath == path)
                    {
                        return routeChangeLaneTransitions[i];
                    }
                }
            }

            return default;
        }

        public RouteChangeLaneTransition GetTransition(Path path)
        {
            for (int i = 0; i < routeChangeLaneTransitions.Count; i++)
            {
                if (routeChangeLaneTransitions[i].SourcePath == path)
                {
                    return routeChangeLaneTransitions[i];
                }
                if (routeChangeLaneTransitions[i].TargetPath == path)
                {
                    return routeChangeLaneTransitions[i];
                }
            }

            return null;
        }

        public void AddRouteTransition(Path sourcePath, Path targetPath)
        {
            bool shouldAdd = true;

            for (int i = 0; i < routeChangeLaneTransitions.Count; i++)
            {
                if (routeChangeLaneTransitions[i].SourcePath == sourcePath && routeChangeLaneTransitions[i].TargetPath == targetPath)
                {
                    shouldAdd = false;
                    break;
                }
            }

            float sourcePathLength = sourcePath.GetPathLength();
            float targetPathLength = sourcePath.GetPathLength();

            float currentSourceOffset = Mathf.Min(sourceOffset, sourcePathLength);
            float targetSourceOffset = Mathf.Min(targetOffset, targetPathLength);

            if (shouldAdd)
            {
                routeChangeLaneTransitions.Add(new RouteChangeLaneTransition()
                {
                    SourcePath = sourcePath,
                    TargetPath = targetPath,
                    SourceOffset = currentSourceOffset,
                    TargetOffset = targetSourceOffset
                });
            }
        }

        public void UpdateTransitions()
        {
            for (int i = 0; i < routeChangeLaneTransitions.Count; i++)
            {
                var sourcePath = routeChangeLaneTransitions[i].SourcePath;
                var pathLength = sourcePath.GetPathLength();


                //var normLength = routeTransitions[i].ChangePathRemainDistance / pathLength;
                //sourcePath.HightlightNormalizedLength = 1 - normLength;
            }
        }

        public void SwapTransition(RouteChangeLaneTransition routeChangeLaneTransition)
        {
            var sourcePath = routeChangeLaneTransition.SourcePath;
            var targetPath = routeChangeLaneTransition.TargetPath;

            routeChangeLaneTransition.TargetPath = sourcePath;
            routeChangeLaneTransition.SourcePath = targetPath;

            EditorSaver.SetObjectDirty(this);
        }

        public void ClearRoute()
        {
            while (routes.Count > 0)
            {
                if (routes[0] == null)
                {
                    routes.RemoveAt(0);
                }
                else
                {
                    RemovePath(routes[0]);
                }
            }

            routeChangeLaneTransitions.Clear();
            trafficNodeRouteData.Clear();

            EditorSaver.SetObjectDirty(this);
        }

        public Vector3 GetSourceTransitionPoint(Path path, bool isSourcePath)
        {
            var pathLength = path.GetPathLength();
            var transition = GetTransition(path, isSourcePath);

            if (transition.Equals(default(RouteChangeLaneTransition)))
            {
                return Vector3.zero;
            }

            return isSourcePath ? GetOffsetPoint(path, transition.SourceOffset, pathLength) : GetOffsetPoint(path, transition.TargetOffset, pathLength);
        }

        public Vector3 GetSourceTransitionPoint(RouteChangeLaneTransition transition, Path path)
        {
            var pathLength = path.GetPathLength();

            if (transition.SourcePath == path)
            {
                return GetOffsetPoint(path, transition.SourceOffset, pathLength);
            }

            if (transition.TargetPath == path)
            {
                return GetOffsetPoint(path, transition.TargetOffset, pathLength);
            }

            return default;
        }

        public NodeType GetNodeType(Path path)
        {
            for (int i = 0; i < routeChangeLaneTransitions.Count; i++)
            {
                if (routeChangeLaneTransitions[i].SourcePath == path)
                {
                    return NodeType.StartTransition;
                }
                if (routeChangeLaneTransitions[i].TargetPath == path)
                {
                    return NodeType.EndTransition;
                }
            }

            return NodeType.Default;
        }

        public void RefreshRelatedNodes()
        {
            trafficNodeRouteData.Clear();

            for (int i = 0; i < routes?.Count; i++)
            {
                AddNodeDataFromPath(routes[i]);
            }
        }

        public bool ShouldShowPath(Path path)
        {
            if (!showOnlyRelatedNodes)
            {
                return true;
            }

            return trafficNodeRouteData.ContainsKey(path.SourceTrafficNode) || trafficNodeRouteData.ContainsKey(path.ConnectedTrafficNode) || trafficNodeRouteData.Keys.Count == 0;
        }

        private void RemoveTransition(Path path)
        {
            if (path == null)
            {
                return;
            }

            for (int i = 0; i < routeChangeLaneTransitions.Count; i++)
            {
                if (routeChangeLaneTransitions[i].SourcePath == path || routeChangeLaneTransitions[i].TargetPath == path)
                {
                    routeChangeLaneTransitions[i].SourcePath.HightlightNormalizedLength = 1f;
                    routeChangeLaneTransitions[i].TargetPath.HightlightNormalizedLength = 1f;
                    routeChangeLaneTransitions.RemoveAt(i);
                    break;
                }
            }

            RemoveNodeDataFromPath(path);
        }

        private Vector3 GetOffsetPoint(Path path, float offsetDistance, float pathLength = -1)
        {
            Vector3 point = default;
            float currentTotalDistance = 0;

            if (pathLength != -1)
            {
                offsetDistance = Mathf.Clamp(offsetDistance, 0, pathLength);
            }

            for (int i = 0; i < path.WayPoints.Count - 1; i++)
            {
                var p1 = path.WayPoints[i].transform.position;
                var p2 = path.WayPoints[i + 1].transform.position;

                float distance = Vector3.Distance(p1, p2);
                currentTotalDistance += distance;

                if (currentTotalDistance >= offsetDistance)
                {
                    float offset = Mathf.Clamp(offsetDistance - (currentTotalDistance - distance), 0, offsetDistance);

                    point = p1 + (p2 - p1).normalized * offset;

                    return point;
                }
            }

            return default;
        }

        private void AddNodeDataFromPath(Path path)
        {
            if (!trafficNodeRouteData.ContainsKey(path.SourceTrafficNode))
            {
                trafficNodeRouteData.Add(path.SourceTrafficNode, new TrafficNodeData());
            }
            if (!trafficNodeRouteData.ContainsKey(path.ConnectedTrafficNode))
            {
                trafficNodeRouteData.Add(path.ConnectedTrafficNode, new TrafficNodeData());
            }

            trafficNodeRouteData[path.SourceTrafficNode].RelatedPaths.TryToAdd(path);
            trafficNodeRouteData[path.ConnectedTrafficNode].RelatedPaths.TryToAdd(path);

            EditorSaver.SetObjectDirty(this);
        }

        private void RemoveNodeDataFromPath(Path path)
        {
            if (trafficNodeRouteData.ContainsKey(path.SourceTrafficNode))
            {
                trafficNodeRouteData[path.SourceTrafficNode].RelatedPaths.TryToRemove(path);

                if (trafficNodeRouteData[path.SourceTrafficNode].RelatedPaths.Count == 0)
                {
                    trafficNodeRouteData.Remove(path.SourceTrafficNode);
                }
            }
            if (trafficNodeRouteData.ContainsKey(path.ConnectedTrafficNode))
            {
                trafficNodeRouteData[path.ConnectedTrafficNode].RelatedPaths.TryToRemove(path);

                if (trafficNodeRouteData[path.ConnectedTrafficNode].RelatedPaths.Count == 0)
                {
                    trafficNodeRouteData.Remove(path.ConnectedTrafficNode);
                }
            }

            EditorSaver.SetObjectDirty(this);
        }

        #endregion
    }
}