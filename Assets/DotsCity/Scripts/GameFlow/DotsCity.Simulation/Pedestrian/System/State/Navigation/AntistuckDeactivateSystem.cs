using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct AntistuckDeactivateSystem : ISystem
    {
        private EntityQuery pedestrianGroup;
        private ComponentTypeSet componentSet;

        void ISystem.OnCreate(ref SystemState state)
        {
            pedestrianGroup = SystemAPI.QueryBuilder()
                .WithAll<AntistuckDeactivateTag, AntistuckDestinationComponent>()
                .Build();

            componentSet = AntistuckUtils.GetAntistuckSet();

            state.RequireForUpdate(pedestrianGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            state.EntityManager.RemoveComponent(pedestrianGroup.ToEntityArray(state.WorldUpdateAllocator), componentSet);
        }
    }
}
