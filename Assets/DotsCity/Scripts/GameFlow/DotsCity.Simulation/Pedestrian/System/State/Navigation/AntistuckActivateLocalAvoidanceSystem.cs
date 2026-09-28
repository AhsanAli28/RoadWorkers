using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct AntistuckActivateLocalAvoidanceSystem : ISystem
    {
        private EntityQuery pedestrianGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            pedestrianGroup = SystemAPI.QueryBuilder()
                .WithNone<AntistuckDestinationComponent>()
                .WithAll<HasCollisionTag, EnabledNavigationTag, LocalAvoidanceAgentTag>()
                .Build();

            state.RequireForUpdate(pedestrianGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var antistuckConfig = SystemAPI.GetSingleton<AntistuckConfigReference>().Config;
            var updateNavTargetLookup = SystemAPI.GetComponentLookup<UpdateNavTargetTag>(true);

            foreach (var (destinationComponent, entity) in
                SystemAPI.Query<RefRW<DestinationComponent>>()
                .WithNone<AntistuckDestinationComponent>()
                .WithAll<HasCollisionTag, EnabledNavigationTag, LocalAvoidanceAgentTag>()
                .WithEntityAccess())
            {
                ref var destinationComponentRef = ref destinationComponent.ValueRW;

                AntistuckUtils.ActivateAntistuck(
                    ref commandBuffer,
                    entity,
                    in antistuckConfig,
                    ref destinationComponentRef);

                if (updateNavTargetLookup.IsComponentEnabled(entity))
                {
                    updateNavTargetLookup.SetComponentEnabled(entity, false);
                }
            }
        }
    }
}
