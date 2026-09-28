using Pathfinding;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.DotsCity.Simulation.TrafficPublic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.ThirdParty.Astar
{
    [UpdateInGroup(typeof(MainThreadEventPlaybackGroup))]
    [DisableAutoCreation]
    [DisableAutoTypeRegistration]
    public partial class AstarTrafficPathEventSystem : AstarPathEventSystemBase
    {
        private ComponentLookup<TrafficDestinationComponent> trafficDestinationLookup;

        protected override void OnStartUpdate()
        {
            base.OnStartUpdate();
            trafficDestinationLookup = SystemAPI.GetComponentLookup<TrafficDestinationComponent>(false);
        }

        protected override float3 GetStartPosition(Entity entity, in ComponentLookup<LocalTransform> componentLookup)
        {
            return trafficDestinationLookup[entity].Destination;
        }

        protected override void PathOnCompleteInternal(Path path, Entity entity, in ComponentLookup<LocalTransform> localTransformLookup)
        {
            var pathConnectionLookup = SystemAPI.GetBufferLookup<PathConnectionElement>(true);
            var fixedRouteNodeElementLookup = SystemAPI.GetBufferLookup<FixedRouteNodeElement>(false);

            var trafficFixedRouteLookup = SystemAPI.GetComponentLookup<TrafficFixedRouteComponent>(false);

            var pathBuffer = fixedRouteNodeElementLookup[entity];

            var destinationComponent = trafficDestinationLookup[entity];
            var trafficFixedRouteComponent = trafficFixedRouteLookup[entity];

            pathBuffer.Clear();

            for (int i = 0; i < path.path.Count; i++)
            {
                var nodeEntity = astarGraph.GetEntity(path.path[i]);
                var nextNodeEntity = Entity.Null;

                if (path.path.Count > i + 1)
                    nextNodeEntity = astarGraph.GetEntity(path.path[i + 1]);

                var localTransform = localTransformLookup[nodeEntity];

                int pathIndex = -1;

                if (nextNodeEntity != Entity.Null)
                {
                    var buffer = pathConnectionLookup[nodeEntity];

                    for (int j = 0; j < buffer.Length; j++)
                    {
                        if (buffer[j].ConnectedNodeEntity == nextNodeEntity)
                        {
                            pathIndex = buffer[j].GlobalPathIndex;
                            break;
                        }
                    }
                }

                pathBuffer.Add(new FixedRouteNodeElement()
                {
                    TrafficNodeEntity = nodeEntity,
                    Position = localTransform.Position,
                    Rotation = localTransform.Rotation,
                    PathKey = pathIndex
                });
            }

            trafficFixedRouteComponent.RouteNodeIndex = 0;
            trafficFixedRouteComponent.RouteEntity = entity;
            trafficFixedRouteComponent.RouteLength = pathBuffer.Length;

            if (pathBuffer.Length > 1)
            {
                destinationComponent.NextDestinationNode = pathBuffer[1].TrafficNodeEntity;
                destinationComponent.NextGlobalPathIndex = pathBuffer[1].PathKey;
            }

            trafficDestinationLookup[entity] = destinationComponent;
            trafficFixedRouteLookup[entity] = trafficFixedRouteComponent;
        }
    }
}
