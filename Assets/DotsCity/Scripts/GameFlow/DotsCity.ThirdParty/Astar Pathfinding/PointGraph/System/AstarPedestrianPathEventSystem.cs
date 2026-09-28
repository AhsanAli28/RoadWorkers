using Pathfinding;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.ThirdParty.Astar
{
    [UpdateInGroup(typeof(MainThreadEventPlaybackGroup))]
    [DisableAutoCreation]
    [DisableAutoTypeRegistration]
    public partial class AstarPedestrianPathEventSystem : AstarPathEventSystemBase
    {
        protected override void PathOnCompleteInternal(Path path, Entity entity, in ComponentLookup<LocalTransform> localTransformLookup)
        {
            var nodeSequenceElementLookup = SystemAPI.GetBufferLookup<NodeSequenceElement>();
            var destinationComponentLookup = SystemAPI.GetComponentLookup<DestinationComponent>();

            var buffer = nodeSequenceElementLookup[entity];

            var destinationComponent = destinationComponentLookup[entity];

            buffer.Clear();

            for (int i = 0; i < path.path.Count; i++)
            {
                var nodeEntity = astarGraph.GetEntity(path.path[i]);

                buffer.Add(new NodeSequenceElement()
                {
                    Entity = nodeEntity,
                });
            }

            if (buffer[0].Entity != destinationComponent.DestinationNode)
            {
                destinationComponent.PreviousDestination = destinationComponent.Value;
                destinationComponent.PreviuosDestinationNode = destinationComponent.DestinationNode;
                destinationComponent.PreviousLightEntity = destinationComponent.DestinationLightEntity;

                destinationComponent.DestinationNode = buffer[0].Entity;
                destinationComponent.Value = localTransformLookup[buffer[0].Entity].Position;

                destinationComponentLookup[entity] = destinationComponent;
            }
        }
    }
}
