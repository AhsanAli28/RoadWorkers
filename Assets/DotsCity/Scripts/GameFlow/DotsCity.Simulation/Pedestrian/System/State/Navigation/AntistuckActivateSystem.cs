using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct AntistuckActivateSystem : ISystem
    {
        private EntityQuery pedestrianGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            pedestrianGroup = SystemAPI.QueryBuilder()
                .WithNone<AntistuckActivateTag, EnabledNavigationTag, CustomLocomotionTag>()
                .WithAll<HasCollisionTag, DestinationComponent>()
                .Build();

            state.RequireForUpdate(pedestrianGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var antistuckConfig = SystemAPI.GetSingleton<AntistuckConfigReference>().Config;

            foreach (var (destinationComponent, entity) in
                SystemAPI.Query<RefRW<DestinationComponent>>()
                .WithNone<AntistuckActivateTag, EnabledNavigationTag, CustomLocomotionTag>()
                .WithAll<HasCollisionTag>()
                .WithEntityAccess())
            {
                ref var destinationComponentRef = ref destinationComponent.ValueRW;

                AntistuckUtils.ActivateAntistuck(
                    ref commandBuffer,
                    entity,
                    in antistuckConfig,
                    ref destinationComponentRef);
            }
        }
    }
}
