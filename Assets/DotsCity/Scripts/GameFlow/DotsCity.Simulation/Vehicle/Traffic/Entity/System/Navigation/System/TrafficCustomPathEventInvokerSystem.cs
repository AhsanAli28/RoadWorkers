using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Navigation;
using Spirit604.DotsCity.Simulation.TrafficPublic;
using System;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(BeforeDestroyGroup))]
    [DisableAutoCreation]
    [DisableAutoTypeRegistration]
    public partial class TrafficCustomPathEventInvokerSystem : SystemBase
    {
        private EntityQuery updateQuery;

        public event Action<Entity, PathStatus> OnStateUpdated = delegate { };

        protected override void OnCreate()
        {
            base.OnCreate();

            updateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAny<PooledEventTag, PathAgentUpdateState>()
                .WithAll<PathAgentComponent, TrafficFixedRouteTag>()
                .Build(EntityManager);

            RequireForUpdate(updateQuery);
        }

        protected override void OnUpdate()
        {
            var pathAgentUpdateStateLookup = SystemAPI.GetComponentLookup<PathAgentUpdateState>(false);

            Entities
            .WithoutBurst()
            .WithAny<PooledEventTag, PathAgentUpdateState>()
            .WithAll<TrafficFixedRouteTag>()
            .ForEach((
                Entity entity,
                in PathAgentComponent pathAgentComponent) =>
            {
                if (!pathAgentUpdateStateLookup.HasComponent(entity))
                    return;

                if (pathAgentUpdateStateLookup.IsComponentEnabled(entity))
                {
                    RaiseEvent(entity, pathAgentComponent.Status);
                    pathAgentUpdateStateLookup.SetComponentEnabled(entity, false);
                }
                else
                {
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
