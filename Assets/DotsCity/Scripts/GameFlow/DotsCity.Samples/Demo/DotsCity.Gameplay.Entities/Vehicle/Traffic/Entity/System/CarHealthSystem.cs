using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Events;
using Spirit604.DotsCity.Simulation.Car;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Gameplay.Car
{
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct CarHealthSystem : ISystem
    {
        private EntityArchetype eventEntityArchetype;
        private EntityQuery updateQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            eventEntityArchetype = state.EntityManager.CreateArchetype(typeof(CameraShakeEventData));

            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<AliveTag, CarExplodeRequestedTag, PlayerTag>()
                .WithAll<CarTag, HealthComponent, LocalTransform>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var requestExplodeJob = new RequestExplodeJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                InViewOfCameraLookup = SystemAPI.GetComponentLookup<InViewOfCameraTag>(true),
                CarStartExplodeComponentLookup = SystemAPI.GetComponentLookup<CarStartExplodeComponent>(true),
                EventEntityArchetype = eventEntityArchetype
            };

            requestExplodeJob.Run();
        }

        [WithNone(typeof(AliveTag), typeof(CarExplodeRequestedTag), typeof(PlayerTag))]
        [WithDisabled(typeof(PooledEventTag))]
        [WithAll(typeof(CarTag), typeof(HealthComponent))]
        [BurstCompile]
        public partial struct RequestExplodeJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public ComponentLookup<InViewOfCameraTag> InViewOfCameraLookup;

            [ReadOnly]
            public ComponentLookup<CarStartExplodeComponent> CarStartExplodeComponentLookup;

            [ReadOnly]
            public EntityArchetype EventEntityArchetype;

            void Execute(
                Entity entity,
                EnabledRefRW<PooledEventTag> pooledEventTagRW,
                in LocalTransform transform)
            {
                if (InViewOfCameraLookup.IsComponentEnabled(entity))
                {
                    var eventEntity = CommandBuffer.CreateEntity(EventEntityArchetype);

                    CommandBuffer.SetComponent(eventEntity, new CameraShakeEventData()
                    {
                        Position = transform.Position
                    });
                }

                if (CarStartExplodeComponentLookup.HasComponent(entity))
                {
                    CommandBuffer.AddComponent<CarExplodeRequestedTag>(entity);
                }
                else
                {
                    PoolEntityUtils.DestroyEntity(ref pooledEventTagRW);
                }
            }
        }
    }
}