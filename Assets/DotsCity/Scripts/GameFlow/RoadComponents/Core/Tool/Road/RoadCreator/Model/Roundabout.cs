using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public class Roundabout : MonoBehaviour, ICustomCreator
    {
        public enum RoundaboutType { SingleInnerNode, DoubleInnerNodes }

        public enum RoadType { Default, OnewayIn, OnewayOut }

        public RoundaboutType Type = RoundaboutType.SingleInnerNode;

        [Range(3, 10)] public int EntranceNodeCount = 4;
        [Range(0, 1f)] public float StepOffset = 0.25f;

        [Range(5, 100)] public float OuterRadius = 20f;

        [Range(0, 90f)] public float EntranceSpeedLimit = 30;

        [Range(-2, 2)] public int EntrancePathPriority = -1;

        [Range(1, 100)] public float InnerRadius = 5f;

        [Range(0, 1f)] public float InnerBaseOffset = 0.05f;

        [Range(1, 4)] public int InnerLaneCount = 1;

        [Range(1, 5f)] public float InnerLaneWidth = 2;

        [Range(0, 90f)] public float InnerSpeedLimit = 30;

        [Range(-2, 2)] public int InnerPathPriority = 1;

        public RoadSegmentCreator.TurnCurveType TurnCurveType = RoadSegmentCreator.TurnCurveType.BezierQuad;

        [Range(0f, 1f)] public float BaseCircularOffset;

        [Serializable]
        public class EntranceNodeData : NodeData
        {
            public RoadType RoadType;
            public int CustomLaneCount;
            public float DividerWidth;
            public float AdditiveOffset;
            public float AdditiveRotation;
        }

        [Serializable]
        public class NodeData
        {
            [Range(0f, 1f)]
            public float InitialCircuralPosition;

            [Range(-1f, 1f)]
            public float AdditiveCircuralPosition;

            public float CircularPosition => Clamp(InitialCircuralPosition + AdditiveCircuralPosition);
        }

        public List<NodeData> InnerNodes = new List<NodeData>();

        public List<EntranceNodeData> EntranceNodes = new List<EntranceNodeData>();

        private RoadSegmentCreator creator;

        private RoadSegmentCreator Creator
        {
            get
            {
                if (creator == null)
                {
                    creator = GetComponent<RoadSegmentCreator>();
                }

                return creator;
            }
        }

        private TrafficLightCrossroad TrafficLightCrossroad => Creator.trafficLightCrossroad;

        private int TotalNodeCount => EntranceNodeCount + InnerNodeCount;

        private int InnerNodeCount
        {
            get
            {
                if (Type == RoundaboutType.DoubleInnerNodes)
                {
                    return EntranceNodeCount * 2;
                }

                return EntranceNodeCount;
            }
        }

        private int CircleInnerNodeCount
        {
            get
            {
                if (Type == RoundaboutType.DoubleInnerNodes)
                {
                    return 2;
                }

                return 1;
            }
        }

        public void Create()
        {
            Creator.Clear();

            Creator.EnsureTrafficNodes(TotalNodeCount);

            EnsureCapacity();

            var baseCircularOffset = BaseCircularOffset / 2;

            for (int i = 0; i < EntranceNodeCount; i++)
            {
                float t = i * StepOffset + baseCircularOffset;

                t = Clamp(t);

                var sourceT = t + baseCircularOffset;

                if (sourceT >= 1)
                {
                    sourceT -= 1;
                }

                EntranceNodes[i].InitialCircuralPosition = sourceT;

                sourceT = EntranceNodes[i].InitialCircuralPosition + EntranceNodes[i].AdditiveCircuralPosition;

                switch (Type)
                {
                    case RoundaboutType.SingleInnerNode:
                        {
                            var innerT = sourceT + (float)StepOffset / 2;
                            innerT = Clamp(innerT);

                            InnerNodes[i].InitialCircuralPosition = innerT;

                            break;
                        }
                    case RoundaboutType.DoubleInnerNodes:
                        {
                            var innerT1 = sourceT + InnerBaseOffset;
                            innerT1 = Clamp(innerT1);

                            var index = 2 * i;
                            InnerNodes[index].InitialCircuralPosition = innerT1;

                            var innerT2 = sourceT - InnerBaseOffset;
                            innerT2 = Clamp(innerT2);

                            var prev = index - 1;

                            if (prev < 0)
                            {
                                prev = InnerNodes.Count - 1;
                            }

                            InnerNodes[prev].InitialCircuralPosition = innerT2;

                            break;
                        }
                }
            }

            UpdateNodes();
        }

        public void UpdateStepOffset()
        {
            StepOffset = GetStepOffset();
        }

        private float GetStepOffset()
        {
            return (float)1 / EntranceNodeCount;
        }

        private static float Clamp(float t)
        {
            if (t >= 1)
            {
                t -= 1;
            }

            if (t < 0)
            {
                t += 1;
            }

            return t;
        }

        private void UpdateNodes()
        {
            Creator.customCrossWalk = true;

            var step = CircleInnerNodeCount + 1;

            for (int i = 0; i < EntranceNodes.Count; i++)
            {
                var index = i * step;
                var node = Creator.TryToGetNode(index);
                var nodeData = EntranceNodes[i];

                node.transform.position = GetWorldCircularPosition(OuterRadius + nodeData.AdditiveOffset, nodeData.CircularPosition);
                node.transform.rotation = Quaternion.LookRotation((node.transform.position - transform.position).normalized) * Quaternion.Euler(0, nodeData.AdditiveRotation, 0);

                if (nodeData.RoadType != RoadType.Default)
                {
                    node.IsOneWay = true;
                    node.IsEndOfOneWay = nodeData.RoadType == RoadType.OnewayOut;
                }
                else
                {
                    node.IsOneWay = false;
                    node.IsEndOfOneWay = false;
                }

                if (nodeData.CustomLaneCount > 0)
                {
                    node.LaneCount = nodeData.CustomLaneCount;
                }
                else
                {
                    node.LaneCount = Creator.laneCount;
                }

                node.DividerWidth = Creator.dividerWidth;

                var lightIndex = i % 2 == 0 ? 0 : 1;
                var pedestrianLight = (lightIndex + 1) % 2;

                var trafficHandler = TrafficLightCrossroad.GetTrafficLightHandler(lightIndex);
                var pedestrianHandler = TrafficLightCrossroad.GetTrafficLightHandler(pedestrianLight);

                node.TrafficLightHandler = trafficHandler;
                node.TrafficNodeCrosswalk.SetLight(pedestrianHandler);
                node.DividerWidth = nodeData.DividerWidth;

                node.Resize();
                EditorSaver.SetObjectDirty(node);
            }

            int prevIndex = 1;
            int counter = 0;

            for (int i = 0; i < InnerNodes.Count; i++)
            {
                var index = 0;

                if (Type == RoundaboutType.SingleInnerNode)
                {
                    index = i * step + 1;
                }
                else
                {
                    if (i % 2 == 0)
                    {
                        index = counter * step + 1;
                        prevIndex = index;
                        counter++;
                    }
                    else
                    {
                        index = prevIndex + 1;
                    }
                }

                var node = Creator.TryToGetNode(index);
                var nodeData = InnerNodes[i];

                node.LaneCount = InnerLaneCount;
                node.IsOneWay = true;
                node.LaneWidth = InnerLaneWidth;
                node.transform.position = GetWorldCircularPosition(InnerRadius, nodeData.CircularPosition);
                node.transform.rotation = Quaternion.LookRotation(-GetTangent(nodeData.CircularPosition));

                node.TrafficLightHandler = null;

                Creator.SetPedestrianEnabledState(index, false);
                node.Resize();
                EditorSaver.SetObjectDirty(node);
            }

            for (int i = 0; i < TotalNodeCount; i += step)
            {
                var enterNode = Creator.TryToGetNode(i);

                var previousNodeIndex = i - 1;

                if (previousNodeIndex < 0)
                {
                    previousNodeIndex = TotalNodeCount - 1;
                }

                var previousNextNode = Creator.TryToGetNode(previousNodeIndex);
                var nextNode = Creator.TryToGetNode(i + 1);

                int laneCount1 = Mathf.Max(enterNode.LaneCount, nextNode.LaneCount);

                for (var laneIndex = 0; laneIndex < laneCount1; laneIndex++)
                {
                    if (enterNode.IsOneWay && enterNode.IsEndOfOneWay)
                        continue;

                    var sourceLaneIndex = Mathf.Clamp(laneIndex, 0, enterNode.LaneCount - 1);
                    var targetLaneIndex = Mathf.Clamp(laneIndex, 0, nextNode.LaneCount - 1);
                    var path = Creator.CreatePath(RoadSegmentCreator.PathDirectionType.Right, enterNode, nextNode, TurnCurveType, sourceLaneIndex, targetLaneIndex, side: 1);
                    path.Priority = EntrancePathPriority;
                    path.PathSpeedLimit = EntranceSpeedLimit;
                    path.ResetSpeedLimit();

                    EditorSaver.SetObjectDirty(path);

                    enterNode.AddPath(path, laneIndex);
                }

                int laneCount2 = Mathf.Max(previousNextNode.LaneCount, enterNode.LaneCount);

                for (var laneIndex = 0; laneIndex < laneCount2; laneIndex++)
                {
                    if (enterNode.IsOneWay && !enterNode.IsEndOfOneWay)
                        continue;

                    var sourceLaneIndex = Mathf.Clamp(laneIndex, 0, previousNextNode.LaneCount - 1);
                    var targetLaneIndex = Mathf.Clamp(laneIndex, 0, enterNode.LaneCount - 1);

                    var path = Creator.CreatePath(RoadSegmentCreator.PathDirectionType.Right, previousNextNode, enterNode, TurnCurveType, sourceLaneIndex, targetLaneIndex);

                    path.PathSpeedLimit = InnerSpeedLimit;
                    path.ResetSpeedLimit();

                    EditorSaver.SetObjectDirty(path);

                    previousNextNode.AddPath(path, laneIndex);
                }

                int laneCount3 = Mathf.Max(previousNextNode.LaneCount, nextNode.LaneCount);

                for (var laneIndex = 0; laneIndex < laneCount3; laneIndex++)
                {
                    var sourceLaneIndex = Mathf.Clamp(laneIndex, 0, previousNextNode.LaneCount - 1);
                    var targetLaneIndex = Mathf.Clamp(laneIndex, 0, nextNode.LaneCount - 1);

                    var path = Creator.CreatePath(RoadSegmentCreator.PathDirectionType.Right, previousNextNode, nextNode, TurnCurveType, laneIndex, laneIndex, side: 1);

                    path.PathSpeedLimit = InnerSpeedLimit;
                    path.ResetSpeedLimit();
                    path.Priority = InnerPathPriority;
                    path.PathRoadType = PathRoadType.StraightRoad;

                    EditorSaver.SetObjectDirty(path);

                    previousNextNode.AddPath(path, laneIndex);
                }

                if (Type == RoundaboutType.DoubleInnerNodes)
                {
                    var nextNextIndex = i + 2;

                    if (nextNextIndex >= TotalNodeCount)
                    {
                        nextNextIndex -= TotalNodeCount;
                    }

                    var nextNextNode = Creator.TryToGetNode(nextNextIndex);

                    for (var laneIndex = 0; laneIndex < InnerLaneCount; laneIndex++)
                    {
                        var path = Creator.CreatePath(RoadSegmentCreator.PathDirectionType.Right, nextNode, nextNextNode, TurnCurveType, laneIndex, laneIndex, side: 1);

                        path.PathSpeedLimit = InnerSpeedLimit;
                        path.ResetSpeedLimit();
                        path.Priority = InnerPathPriority;

                        EditorSaver.SetObjectDirty(path);

                        nextNode.AddPath(path, laneIndex);
                    }
                }
            }
        }

        private bool IsInnerNode(int index) => index % 2 != 0;

        private Vector3 GetWorldCircularPosition(float radius, float normalizedCircularPos)
        {
            float angle = GetAngle(normalizedCircularPos);

            float angleInRadians = angle * Mathf.Deg2Rad;

            // Calculate the position on the circle in the XZ plane
            return transform.position + transform.rotation * new Vector3(Mathf.Cos(angleInRadians) * radius, 0f, Mathf.Sin(angleInRadians) * radius);
        }

        private Vector3 GetTangent(float normalizedCircularPos)
        {
            float angle = GetAngle(normalizedCircularPos);

            float angleInRadians = angle * Mathf.Deg2Rad;

            Vector3 tangent = new Vector3(-Mathf.Sin(angleInRadians), 0f, Mathf.Cos(angleInRadians));

            return transform.rotation * tangent.normalized;
        }

        private float GetAngle(float normalizedCircularPos)
        {
            float angle = 0;

            if (normalizedCircularPos > 0)
            {
                angle = 360 * normalizedCircularPos;
            }

            return angle;
        }

        public void EnsureCapacity()
        {
            if (InnerNodes.Count > InnerNodeCount)
            {
                var deleteCount = InnerNodes.Count - InnerNodeCount;

                while (deleteCount > 0)
                {
                    InnerNodes.RemoveAt(InnerNodes.Count - 1);
                    deleteCount--;
                }
            }

            if (InnerNodes.Count < InnerNodeCount)
            {
                var addCount = InnerNodeCount - InnerNodes.Count;

                for (var i = 0; i < addCount; i++)
                {
                    InnerNodes.Add(new NodeData());
                }
            }

            if (EntranceNodes.Count > EntranceNodeCount)
            {
                var deleteCount = EntranceNodes.Count - EntranceNodeCount;

                while (deleteCount > 0)
                {
                    EntranceNodes.RemoveAt(EntranceNodes.Count - 1);
                    deleteCount--;
                }
            }

            if (EntranceNodes.Count < EntranceNodeCount)
            {
                var addCount = EntranceNodeCount - EntranceNodes.Count;

                for (var i = 0; i < addCount; i++)
                {
                    EntranceNodes.Add(new EntranceNodeData());
                }
            }
        }
    }
}