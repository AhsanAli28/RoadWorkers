using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Entities;
using Unity.Jobs;

namespace Spirit604.DotsCity.Events
{
    [UpdateInGroup(typeof(MainThreadEventPlaybackGroup))]
    [RequireMatchingQueriesForUpdate]
    [DisableAutoCreation]
    [DisableAutoTypeRegistration]
    public partial class TriggerNodeSystem : SimpleSystemBase
    {
        private TriggerNodeService triggerNodeService;

        protected override void OnUpdate()
        {
            var destinationComponentLookup = SystemAPI.GetComponentLookup<DestinationComponent>(true);
            var trafficDestinationComponentLookup = SystemAPI.GetComponentLookup<TrafficDestinationComponent>(true);
            var triggerNodeComponentLookup = SystemAPI.GetComponentLookup<TriggerNodeComponent>(true);

            Entities
            .WithoutBurst()
            .WithReadOnly(destinationComponentLookup)
            .WithReadOnly(trafficDestinationComponentLookup)
            .WithReadOnly(triggerNodeComponentLookup)
            .WithAll<TriggerNodeTag>()
            .ForEach((
                Entity entity) =>
            {
                var nodeEntity = Entity.Null;
                var triggerNodeType = TriggerNodeType.Default;

                if (destinationComponentLookup.HasComponent(entity))
                {
                    var destinationComponent = destinationComponentLookup[entity];

                    // Node has been switched
                    nodeEntity = destinationComponent.PreviuosDestinationNode;
                    triggerNodeType = TriggerNodeType.Pedestrian;
                }
                else if (trafficDestinationComponentLookup.HasComponent(entity))
                {
                    var destinationComponent = trafficDestinationComponentLookup[entity];
                    nodeEntity = destinationComponent.CurrentNode;
                    triggerNodeType = TriggerNodeType.Traffic;
                }

                if (triggerNodeType != TriggerNodeType.Default)
                {
                    triggerNodeService.RaiseEvent(entity, nodeEntity, triggerNodeType);
                }

                EntityManager.SetComponentEnabled<TriggerNodeTag>(entity, false);

                if (triggerNodeComponentLookup.HasComponent(nodeEntity))
                {
                    var triggerNodeComponent = triggerNodeComponentLookup[nodeEntity];

                    if (triggerNodeComponent.DestroyEntity)
                        EntityManager.SetComponentEnabled<PooledEventTag>(entity, true);
                }

            }).Run();
        }

        public void Initialize(TriggerNodeService triggerNodeService)
        {
            this.triggerNodeService = triggerNodeService;
        }
    }
}
