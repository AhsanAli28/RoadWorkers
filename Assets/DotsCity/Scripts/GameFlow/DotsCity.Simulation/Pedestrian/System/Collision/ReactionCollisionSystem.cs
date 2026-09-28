using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Events;
using Spirit604.DotsCity.Simulation.Car.Sound;
using Spirit604.DotsCity.Simulation.Sound;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateAfter(typeof(CalculateCollisionSystem))]
    [UpdateInGroup(typeof(SimulationGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class ReactionCollisionSystem : BeginSimulationSystemBase
    {
        private const float MinForceToDeath = 1.5f;
        private const float MinForceToDeathPhysics = 30f;
        private const float MinForceMultiplier = 0.7f;
        private const float MaxForceMultiplier = 2f;
        private const float ImpulseRelativeRate = 20f;
        private const float MaxVolumeImpulse = 10f;
        private const float MinCollisionVolume = 0.3f;

        protected EntityDamageEventConsumerSystem EntityDamageEventConsumerSystem { get; private set; }
        private EntityQuery npcQuery;
        private EntityQuery npcChunkQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            EntityDamageEventConsumerSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<EntityDamageEventConsumerSystem>();

            npcQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<HasCollisionTag, AliveTag, HealthComponent, CollisionComponent>()
                .Build(this);

            npcChunkQuery = new EntityQueryBuilder(Allocator.Temp)
               .WithAll<HasCollisionTag, HealthComponent, CollisionComponent>()
               .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
               .Build(this);

            RequireForUpdate(npcQuery);
        }

        protected override void OnUpdate()
        {
            Dependency = new ReactCollisionJob()
            {
                SoundEventQueue = SystemAPI.GetSingleton<SoundEventPlaybackSystem.Singleton>(),
                Writer = EntityDamageEventConsumerSystem.CreateConsumerWriter(npcChunkQuery.CalculateChunkCount()),
                EntityHandle = SystemAPI.GetEntityTypeHandle(),
                LocalToWorldHandle = SystemAPI.GetComponentTypeHandle<LocalToWorld>(true),
                CollisionComponentHandle = SystemAPI.GetComponentTypeHandle<CollisionComponent>(true),
                HasCollisionComponentHandle = SystemAPI.GetComponentTypeHandle<HasCollisionTag>(true),
                PhysicsColliderHandle = SystemAPI.GetComponentTypeHandle<PhysicsCollider>(true),
                InViewOfCameraLookup = SystemAPI.GetComponentLookup<InViewOfCameraTag>(true),
                CarSoundCommonConfigReference = SystemAPI.GetSingleton<CarSoundCommonConfigReference>(),

            }.Schedule(npcQuery, Dependency);

            EntityDamageEventConsumerSystem.RegisterTriggerDependency(Dependency);

            AddCommandBufferForProducer();
        }

        [BurstCompile]
        public struct ReactCollisionJob : IJobChunk
        {
            [NativeDisableContainerSafetyRestriction]
            public SoundEventPlaybackSystem.Singleton SoundEventQueue;
            public NativeStream.Writer Writer;

            [ReadOnly] public EntityTypeHandle EntityHandle;
            [ReadOnly] public ComponentTypeHandle<LocalToWorld> LocalToWorldHandle;
            [ReadOnly] public ComponentTypeHandle<CollisionComponent> CollisionComponentHandle;
            [ReadOnly] public ComponentTypeHandle<HasCollisionTag> HasCollisionComponentHandle;
            [ReadOnly] public ComponentTypeHandle<PhysicsCollider> PhysicsColliderHandle;
            [ReadOnly] public ComponentLookup<InViewOfCameraTag> InViewOfCameraLookup;

            [ReadOnly] public CarSoundCommonConfigReference CarSoundCommonConfigReference;

            public void Execute(in ArchetypeChunk chunk, int chunkIndex, bool useEnabledMask, in Unity.Burst.Intrinsics.v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(EntityHandle);
                var localToWorlds = chunk.GetNativeArray(ref LocalToWorldHandle);
                var collisionComponents = chunk.GetNativeArray(ref CollisionComponentHandle);

                Writer.BeginForEachIndex(chunkIndex);

                for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    if (useEnabledMask && !chunk.IsComponentEnabled(ref HasCollisionComponentHandle, entityIndex))
                        continue;

                    var entity = entities[entityIndex];
                    var position = localToWorlds[entityIndex].Position;
                    var rotation = localToWorlds[entityIndex].Rotation;

                    var collisionComponent = collisionComponents[entityIndex];

                    if (!collisionComponent.HasCollision())
                        continue;

                    float3 force = collisionComponent.Force;

                    float forceImpulse = math.length(force);

                    float forceValue = !chunk.Has(ref PhysicsColliderHandle) ? MinForceToDeath : MinForceToDeathPhysics;
                    bool forceIsEnough = forceImpulse > forceValue;

                    if (forceIsEnough)
                    {
                        if (InViewOfCameraLookup.HasComponent(entity) && InViewOfCameraLookup.IsComponentEnabled(entity))
                        {
                            var volume = math.clamp(forceImpulse / MaxVolumeImpulse, MinCollisionVolume, 1f);
                            SoundEventQueue.PlayOneShot(CarSoundCommonConfigReference.Config.Value.NpcHitSoundId, position, volume);
                        }

                        float forceRate = forceImpulse / ImpulseRelativeRate;

                        float forceMultiplier = math.lerp(MinForceMultiplier, MaxForceMultiplier, forceRate);
                        forceMultiplier = math.clamp(forceMultiplier, MinForceMultiplier, MaxForceMultiplier);

                        float3 forceDirection = math.normalizesafe(collisionComponent.Force);

                        var damageHitDataComponent = new DamageHitData()
                        {
                            DamagedEntity = entity,
                            HitPosition = position,
                            HitDirection = forceDirection,
                            Damage = 9999,
                            ForceMultiplier = forceMultiplier,
                            DamageDealerID = collisionComponent.LastDamageDealerID
                        };

                        Writer.Write(damageHitDataComponent);
                    }
                }

                Writer.EndForEachIndex();
            }
        }
    }
}