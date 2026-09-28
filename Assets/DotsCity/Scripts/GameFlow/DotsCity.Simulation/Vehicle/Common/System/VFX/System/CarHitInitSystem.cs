using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car
{
    [UpdateInGroup(typeof(StructuralInitGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct CarHitInitSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<HitReactionInitComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var hitInitJob = new HitInitJob()
            {
                CarHitReactionLookup = SystemAPI.GetComponentLookup<CarHitReactionData>(false),
                HitReactionInitComponentLookup = SystemAPI.GetComponentLookup<HitReactionInitComponent>(false),
            };

            hitInitJob.Run();
        }

        [WithAll(typeof(HitReactionInitComponent))]
        [BurstCompile]
        public partial struct HitInitJob : IJobEntity
        {
            public ComponentLookup<CarHitReactionData> CarHitReactionLookup;
            public ComponentLookup<HitReactionInitComponent> HitReactionInitComponentLookup;

            void Execute(
                Entity entity)
            {
                HitReactionInitComponentLookup.SetComponentEnabled(entity, false);

                var hitReactionInitComponent = HitReactionInitComponentLookup[entity];
                var vehicleEntity = hitReactionInitComponent.VehicleEntity;

                var carHitReaction = CarHitReactionLookup[vehicleEntity];

                carHitReaction.HitMeshEntity = entity;

                CarHitReactionLookup[vehicleEntity] = carHitReaction;
            }
        }
    }
}