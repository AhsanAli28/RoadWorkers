using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Navigation;
using System;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(BeforeDestroyGroup))]
    [DisableAutoCreation]
    [DisableAutoTypeRegistration]
    public partial class PedestrianCustomPathEventInvokerSystem : SystemBase
    {
        private EntityQuery updateQuery;

        public event Action<Entity, PathStatus> OnStateUpdated = delegate { };

        protected override void OnCreate()
        {
            base.OnCreate();

            updateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAny<PooledEventTag, PathAgentUpdateState>()
                .WithAll<PathAgentComponent, CustomReachTargetTag>()
                .Build(EntityManager);

            RequireForUpdate(updateQuery);
        }

        protected override void OnUpdate()
        {
            var pathAgentUpdateStateLookup = SystemAPI.GetComponentLookup<PathAgentUpdateState>(false);
            var customReachTargetTagLookup = SystemAPI.GetComponentLookup<CustomReachTargetTag>(true);

            Entities
            .WithoutBurst()
            .WithReadOnly(customReachTargetTagLookup)
            .WithAny<PooledEventTag, PathAgentUpdateState>()
            .ForEach((
                Entity entity,
                in PathAgentComponent pathAgentComponent) =>
            {
                if (pathAgentUpdateStateLookup.IsComponentEnabled(entity))
                {
                    RaiseEvent(entity, pathAgentComponent.Status);
                    pathAgentUpdateStateLookup.SetComponentEnabled(entity, false);
                }
                else
                {
                    if (customReachTargetTagLookup.HasComponent(entity) && customReachTargetTagLookup.IsComponentEnabled(entity))
                        RaiseEvent(entity, PathStatus.AgentNotExist);
                }

            }).Run();
        }

        internal void RaiseEvent(Entity entity, PathStatus status)
        {
            OnStateUpdated(entity, status);
        }
    }
}
