using Spirit604.DotsCity.Simulation.Road;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public static class TrafficSpawnUtils
    {
        public static TrafficSpawnParams GetSpawnParams(in PathGraphSystem.Singleton graph, EntityManager entityManager, Entity sourceNodeEntity, int carModel, TrafficCustomInitType initType, int localPathIndex = -1, float normalizedPathPosition = 0)
        {
            int targetWaypointIndex = -1;
            var pathConnections = entityManager.GetBuffer<PathConnectionElement>(sourceNodeEntity);

            if (localPathIndex == -1)
            {
                localPathIndex = UnityEngine.Random.Range(0, pathConnections.Length);
            }

            var currentGlobalPathIndex = pathConnections[localPathIndex].GlobalPathIndex;

            ref readonly var pathData = ref graph.GetPathData(currentGlobalPathIndex);

            var spawnRotation = Quaternion.identity;
            var spawnPosition = GetSpawnData(in graph, in pathData, normalizedPathPosition, ref targetWaypointIndex, ref spawnRotation);

            int selectedPathIndex = -1;
            Entity targetNodeEntity = Entity.Null;

            PathConnectionExtension.GetIndexes(ref pathConnections, currentGlobalPathIndex, ref selectedPathIndex, ref targetNodeEntity);

            var targetPosition = graph.GetEndPosition(in pathData);
            ref readonly var previousNode = ref graph.GetPathNodeData(in pathData, targetWaypointIndex - 1);
            ref readonly var targetNode = ref graph.GetPathNodeData(in pathData, targetWaypointIndex);

            var priority = pathData.Priority;

            var trafficPathComponent = new TrafficPathComponent()
            {
                DestinationWayPoint = targetNode.Position,
                LocalPathNodeIndex = targetWaypointIndex,
                CurrentGlobalPathIndex = currentGlobalPathIndex,
                PathDirection = previousNode.ForwardNodeDirectionType,
                Priority = priority,
            };

            float speedLimit = previousNode.SpeedLimit;

            var destinationComponent = new TrafficDestinationComponent
            {
                Destination = targetPosition,
                DestinationNode = targetNodeEntity,
                NextDestinationNode = Entity.Null,
                PreviousNode = sourceNodeEntity,
                CurrentNode = sourceNodeEntity,
                NextGlobalPathIndex = -1,
                PathConnectionType = pathData.PathConnectionType,
            };

            var trafficSpawnParams = new TrafficSpawnParams(spawnPosition, spawnRotation, destinationComponent)
            {
                globalPathIndex = currentGlobalPathIndex,
                targetNodeEntity = targetNodeEntity,
                previousNodeEntity = sourceNodeEntity,
                spawnNodeEntity = sourceNodeEntity,
                hasDriver = true,
                carModelIndex = carModel,
                trafficPathComponent = trafficPathComponent,
                pathConnectionType = pathData.PathConnectionType,
                customSpawnData = true,
                trafficCustomInit = initType,
                speedLimit = speedLimit,
            };

            return trafficSpawnParams;
        }

        private static Vector3 GetSpawnData(in PathGraphSystem.Singleton graph, in PathGraphSystem.PathData path, float normalizedLength, ref int targetWaypointIndex, ref Quaternion spawnRotation)
        {
            float pathLength = path.PathLength;
            float currentDistance = 0;
            float prevCurrentDistance = 0;

            var routeNodes = graph.GetRouteNodes(in path);

            for (int i = 0; i < routeNodes.Length - 1; i++)
            {
                Vector3 A1point = routeNodes[i].Position;
                Vector3 A2point = routeNodes[i + 1].Position;

                currentDistance += Vector3.Distance(A1point, A2point);

                if (currentDistance >= normalizedLength * pathLength)
                {
                    var offsetDistance = normalizedLength * pathLength - prevCurrentDistance;

                    var offsetPoint = A1point + (A2point - A1point).normalized * offsetDistance;

                    var dir = routeNodes[i].ForwardNodeDirection ? A2point - A1point : A1point - A2point;

                    spawnRotation = quaternion.LookRotationSafe((dir.normalized), math.up());

                    targetWaypointIndex = i + 1;

                    return offsetPoint;
                }

                prevCurrentDistance = currentDistance;
            }

            return default;
        }
    }
}
