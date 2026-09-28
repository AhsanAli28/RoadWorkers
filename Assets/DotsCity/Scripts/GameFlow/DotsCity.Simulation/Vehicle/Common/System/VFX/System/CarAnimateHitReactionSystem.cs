using Spirit604.DotsCity.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace Spirit604.DotsCity.Simulation.Car
{
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    partial struct CarAnimateHitReactionSystem : ISystem, ISystemStartStop
    {
        private EntityQuery updateQuery;
        private NativeHashSet<int> takenIndexesLocalRef;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<AnimateHitReactionTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
            state.RequireForUpdate<CarHitReactProviderSystem.FactoryCreatedEventTag>();
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            takenIndexesLocalRef = default;
        }

        void ISystemStartStop.OnStartRunning(ref SystemState state)
        {
            takenIndexesLocalRef = CarHitReactProviderSystem.TakenIndexes;
        }

        void ISystemStartStop.OnStopRunning(ref SystemState state) { }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var animateHitJob = new AnimateHitJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                TakenIndexes = takenIndexesLocalRef,
                CarHitReactionDataLookup = SystemAPI.GetComponentLookup<CarHitReactionData>(false),
                HitReactionInitLookup = SystemAPI.GetComponentLookup<HitReactionInitComponent>(true),
                CarHitReactionConfigReference = SystemAPI.GetSingleton<CarHitReactionConfigReference>(),
                CurrentTime = (float)SystemAPI.Time.ElapsedTime,
                DeltaTime = SystemAPI.Time.DeltaTime,
            };

            animateHitJob.Schedule();
        }

        [WithAll(typeof(AnimateHitReactionTag))]
        [BurstCompile]
        public partial struct AnimateHitJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [NativeDisableContainerSafetyRestriction]
            public NativeHashSet<int> TakenIndexes;

            public ComponentLookup<CarHitReactionData> CarHitReactionDataLookup;

            [ReadOnly]
            public ComponentLookup<HitReactionInitComponent> HitReactionInitLookup;

            [ReadOnly]
            public CarHitReactionConfigReference CarHitReactionConfigReference;

            [ReadOnly]
            public float CurrentTime;

            [ReadOnly]
            public float DeltaTime;

            void Execute(
                Entity entity,
                ref HitReactionMaterialDataComponent hitReactionComponent,
                ref HitReactionStateComponent hitReactionStateComponent,
                ref CarShaderDeviationData shaderDeviationData,
                ref CarShaderLerpData shaderLerpData,
                in CarRelatedHullComponent carRelatedHullComponent,
                in CarHitReactionTakenIndex carHitReactionTakenIndex)
            {
                shaderDeviationData.Value = new float3(CarHitReactionConfigReference.Config.Value.DivHorizontalRate, CarHitReactionConfigReference.Config.Value.DivVerticalRate, CarHitReactionConfigReference.Config.Value.DivHorizontalRate);

                int side = hitReactionComponent.IsForth == 1 ? 1 : -1;

                hitReactionComponent.TValue += DeltaTime * CarHitReactionConfigReference.Config.Value.LerpSpeed * side;
                float shaderLerpValue = 0;

                if (hitReactionComponent.IsForth == 1)
                {
                    shaderLerpValue = math.lerp(0f, CarHitReactionConfigReference.Config.Value.MaxLerp, hitReactionComponent.TValue);

                    if (hitReactionComponent.TValue >= 1f)
                    {
                        int forth = hitReactionComponent.IsForth == 1 ? 0 : 1;
                        hitReactionComponent.IsForth = forth;
                    }
                }
                else
                {
                    shaderLerpValue = math.lerp(CarHitReactionConfigReference.Config.Value.MaxLerp, 0, hitReactionComponent.TValue);

                    if (hitReactionComponent.TValue <= 0f)
                    {
                        int forth = hitReactionComponent.IsForth == 1 ? 0 : 1;
                        hitReactionComponent.IsForth = forth;
                    }
                }

                shaderLerpData.Value = shaderLerpValue;

                if (hitReactionStateComponent.ActivateTime < CurrentTime)
                {
                    hitReactionComponent = hitReactionComponent.GetDefault();

                    if (TakenIndexes.Contains(carHitReactionTakenIndex.TakenIndex))
                    {
                        TakenIndexes.Remove(carHitReactionTakenIndex.TakenIndex);
                    }

                    CommandBuffer.RemoveComponent<CarHitReactionTakenIndex>(entity);

                    PoolEntityUtils.DestroyEntity(ref CommandBuffer, entity);

                    var vehicleEntity = HitReactionInitLookup[entity].VehicleEntity;

                    if (CarHitReactionDataLookup.HasComponent(vehicleEntity))
                    {
                        var carHitReactionData = CarHitReactionDataLookup[vehicleEntity];
                        carHitReactionData.HitMeshEntity = Entity.Null;
                        CarHitReactionDataLookup[vehicleEntity] = carHitReactionData;
                    }

                    CommandBuffer.SetComponentEnabled<MaterialMeshInfo>(carRelatedHullComponent.HullEntity, true);
                }
            }
        }
    }
}