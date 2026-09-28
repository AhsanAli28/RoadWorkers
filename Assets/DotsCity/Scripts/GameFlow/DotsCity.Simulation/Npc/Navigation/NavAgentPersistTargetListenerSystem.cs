using Unity.Burst;
using Unity.Entities;

#if REESE_PATH
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
#endif

namespace Spirit604.DotsCity.Simulation.Npc.Navigation
{
    [UpdateInGroup(typeof(NavSimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct NavAgentPersistTargetListenerSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
#if REESE_PATH
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<CustomMovementTag, IdleTag, UpdateNavTargetTag>()
                .WithAll<PersistNavigationTag>()
                .Build();

            state.RequireForUpdate(updateQuery);

#else
            state.Enabled = false;
#endif
        }

#if REESE_PATH

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var navPersistListenJob = new NavPersistListenJob()
            {
            };

            navPersistListenJob.Schedule();
        }

        [WithNone(typeof(CustomMovementTag), typeof(IdleTag))]
        [WithDisabled(typeof(UpdateNavTargetTag))]
        [WithAll(typeof(PersistNavigationTag), typeof(NavAgentTag))]
        [BurstCompile]
        public partial struct NavPersistListenJob : IJobEntity
        {
            void Execute(
                ref PersistNavigationComponent persistNavigationComponent,
                ref NavAgentComponent navAgentComponent,
                EnabledRefRW<UpdateNavTargetTag> updateNavTargetTagRW,
                in DestinationComponent destinationComponent)
            {
                if (persistNavigationComponent.CurrentEntity != destinationComponent.DestinationNode)
                {
                    persistNavigationComponent.CurrentEntity = destinationComponent.DestinationNode;
                    navAgentComponent.PathEndPosition = destinationComponent.Value;

                    updateNavTargetTagRW.ValueRW = true;
                }
            }
        }
#endif
    }
}