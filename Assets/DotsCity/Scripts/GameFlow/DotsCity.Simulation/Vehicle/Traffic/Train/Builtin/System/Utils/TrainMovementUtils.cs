using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Train
{
    public static class TrainMovementUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Calculate(
            in PathGraphSystem.Singleton graph,
            in TrafficPathComponent trafficPathComponent,
            in TrafficDestinationComponent trafficDestinationComponent,
            in TrafficSettingsComponent trafficSettingsComponent,
            in SpeedComponent speedComponent,
            in LocalTransform transform,
            in TrafficRailConfigReference trafficRailConfig,
            float deltaTime,
            out float3 nextPos,
            out quaternion newRotation)
        {
            newRotation = transform.Rotation;

            var deltaDist = deltaTime * speedComponent.Value;

            ref readonly var pathData = ref graph.GetPathData(trafficPathComponent.CurrentGlobalPathIndex);

            nextPos = default;
            bool hasPos = true;
            float3 movingForwardDirection = default;

            ref readonly var node1 = ref graph.GetPathNodeData(trafficPathComponent.CurrentGlobalPathIndex, trafficPathComponent.SourceLocalNodeIndex);
            ref readonly var node2 = ref graph.GetPathNodeData(trafficPathComponent.CurrentGlobalPathIndex, trafficPathComponent.SourceLocalNodeIndex + 1);

            var dot = math.dot(math.normalizesafe(node2.Position - transform.Position), transform.Forward());
            var distanceToWaypoint = math.distance(transform.Position.Flat(), node2.Position.Flat());

            if (distanceToWaypoint >= deltaDist && dot > 0)
            {
                var remainDistance = distanceToWaypoint - deltaDist;

                movingForwardDirection = math.normalize(node2.Position - node1.Position);
                nextPos = node2.Position - movingForwardDirection * remainDistance;
            }
            else
            {
                var initialExceedDistance = deltaDist - distanceToWaypoint;
                var exceedDistance = initialExceedDistance;

                int nodeIndex = trafficPathComponent.LocalPathNodeIndex;

                bool found = CalcInternal(trafficPathComponent.CurrentGlobalPathIndex, nodeIndex, in graph, ref exceedDistance, ref nextPos, ref movingForwardDirection);

                if (!found)
                {
                    if (dot < 0)
                    {
                        exceedDistance = deltaDist + distanceToWaypoint;
                    }

                    if (graph.Exist(trafficDestinationComponent.NextGlobalPathIndex))
                    {
                        found = CalcInternal(trafficDestinationComponent.NextGlobalPathIndex, 0, in graph, ref exceedDistance, ref nextPos, ref movingForwardDirection);
                    }
                }

                if (!found)
                {
                    nextPos = transform.Position;
                    hasPos = false;
                }
            }

            if (hasPos)
            {
                var currentSpeed = speedComponent.ValueAbs;

                if (currentSpeed > 0.01f)
                {
                    var targetRot = quaternion.LookRotation(movingForwardDirection, new float3(0, 1, 0));

                    bool lerp = DotsEnumExtension.HasFlagUnsafe<TrafficAdditionalSettings>(trafficSettingsComponent.AdditionalSettings, TrafficAdditionalSettings.HasRailRotationLerp);

                    if (!lerp)
                    {
                        newRotation = targetRot;
                    }
                    else
                    {
                        newRotation = math.slerp(transform.Rotation, targetRot, trafficRailConfig.Config.Value.TrainRotationLerpSpeed * deltaTime);
                    }
                }
            }

            return hasPos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CalcInternal(
            int pathIndex,
            int nodeIndex,
            in PathGraphSystem.Singleton graph,
            ref float exceedDistance,
            ref float3 nextPos,
            ref float3 movingForwardDirection)
        {
            bool found = false;

            ref readonly var pathData = ref graph.GetPathData(pathIndex);

            while (true)
            {
                if (nodeIndex + 1 < pathData.NodeCount)
                {
                    ref readonly var node11 = ref graph.GetPathNodeData(pathIndex, nodeIndex);
                    ref readonly var node22 = ref graph.GetPathNodeData(pathIndex, nodeIndex + 1);

                    float distance = math.distance(node11.Position, node22.Position);

                    if (exceedDistance - distance < 0)
                    {
                        movingForwardDirection = math.normalize(node22.Position - node11.Position);
                        nextPos = node11.Position + movingForwardDirection * exceedDistance;
                        found = true;
                        break;
                    }
                    else
                    {
                        exceedDistance -= distance;
                        nodeIndex++;
                    }
                }
                else
                {
                    break;
                }
            }

            return found;
        }
    }
}