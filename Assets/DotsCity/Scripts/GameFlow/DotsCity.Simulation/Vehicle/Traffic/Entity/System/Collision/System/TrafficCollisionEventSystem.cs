using Spirit604.DotsCity.Simulation.Car.Sound;
using Spirit604.DotsCity.Simulation.Sound;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    // This system applies an impulse to any dynamic that collides with a Repulsor.
    // A Repulsor is defined by a PhysicsShape with the `Raise Collision Events` flag ticked and a
    // CollisionEventImpulse behaviour added.
    [UpdateInGroup(typeof(PhysicsTriggerGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    unsafe public partial struct TrafficCollisionEventSystem : ISystem, ISystemStartStop
    {
        private const float MinCollisionForce = 200f;
        private const float MaxCollisionForce = 1000f;
        private const float MinVolume = 0.1f;

        private SystemHandle carContactCollectorSystem;
        private EntityQuery updateQuery;
        private NativeParallelHashMap<EntityPair, CollisionData> contactHashMapLocalRef;

        void ISystem.OnCreate(ref SystemState state)
        {
            carContactCollectorSystem = state.WorldUnmanaged.GetExistingUnmanagedSystem<CarContactCollectorSystem>();

            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<CarCollisionComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);

            state.RequireForUpdate<CarSoundCommonConfigReference>();
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            contactHashMapLocalRef = default;
        }

        void ISystemStartStop.OnStartRunning(ref SystemState state)
        {
            contactHashMapLocalRef = CarContactCollectorSystem.ContactHashMapStaticRef;
        }

        void ISystemStartStop.OnStopRunning(ref SystemState state) { }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            ref var carContactCollectorSystemRef = ref state.WorldUnmanaged.ResolveSystemStateRef(carContactCollectorSystem);
            var depJob = JobHandle.CombineDependencies(carContactCollectorSystemRef.Dependency, state.Dependency);

            state.Dependency = new CollisionEventImpulseJob
            {
                CommandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                SoundEventQueue = SystemAPI.GetSingleton<SoundEventPlaybackSystem.Singleton>(),
                ContactHashMap = contactHashMapLocalRef,
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                TrafficStateLookup = SystemAPI.GetComponentLookup<TrafficStateComponent>(false),
                CollisionEventGroup = SystemAPI.GetComponentLookup<CarCollisionComponent>(false),
                TrafficCollisionConfigReference = SystemAPI.GetSingleton<TrafficCollisionConfigReference>(),
                PhysicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld,
                carSoundConfigReference = SystemAPI.GetSingleton<CarSoundCommonConfigReference>(),
                Timestamp = (float)SystemAPI.Time.ElapsedTime,

            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), depJob);
        }

        [BurstCompile]
        struct CollisionEventImpulseJob : ICollisionEventsJob
        {
            public EntityCommandBuffer CommandBuffer;
            public SoundEventPlaybackSystem.Singleton SoundEventQueue;

            public NativeParallelHashMap<EntityPair, CollisionData> ContactHashMap;

            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

            public ComponentLookup<CarCollisionComponent> CollisionEventGroup;
            public ComponentLookup<TrafficStateComponent> TrafficStateLookup;

            [ReadOnly] public TrafficCollisionConfigReference TrafficCollisionConfigReference;
            [ReadOnly] public PhysicsWorld PhysicsWorld;
            [ReadOnly] public CarSoundCommonConfigReference carSoundConfigReference;
            [ReadOnly] public float Timestamp;

            public void Execute(CollisionEvent collisionEvent)
            {
                Entity entityA = collisionEvent.EntityA;
                Entity entityB = collisionEvent.EntityB;

                bool isBodyARepulser = CollisionEventGroup.HasComponent(entityA);
                bool isBodyBRepulser = CollisionEventGroup.HasComponent(entityB);

                if (isBodyARepulser || isBodyBRepulser)
                {
                    var collisionDefails = collisionEvent.CalculateDetails(ref PhysicsWorld);

                    if (collisionDefails.EstimatedImpulse >= MinCollisionForce)
                    {
                        bool enoughTimePassed = false;
                        var pair = new EntityPair(entityA, entityB);

                        enoughTimePassed = !ContactHashMap.ContainsKey(pair);

                        if (enoughTimePassed)
                        {
                            var position = collisionDefails.AverageContactPointPosition;

                            ContactHashMap.Add(pair, new CollisionData()
                            {
                                Position = position,
                                Impulse = collisionDefails.EstimatedImpulse,
                                ActivateTime = Timestamp
                            });

                            var volume = collisionDefails.EstimatedImpulse / MaxCollisionForce;
                            volume = math.clamp(volume, MinVolume, 1f);

                            var soundId = carSoundConfigReference.Config.Value.CollisionSoundId;
                            SoundEventQueue.PlayOneShot(soundId, position, volume: volume);
                        }
                        else
                        {
                            var data = ContactHashMap[pair];
                            data.ActivateTime = Timestamp;
                            ContactHashMap[pair] = data;
                        }
                    }
                }

                if (isBodyARepulser && isBodyBRepulser)
                {
                    CalculateDirection(entityA, entityB, out var source, out var target);

                    AddCollision(entityA, entityB, source, target);
                    AddCollision(entityB, entityA, target, source);
                }
            }

            private void CalculateDirection(Entity entityA, Entity entityB, out TrafficCollisionDirectionType source, out TrafficCollisionDirectionType target)
            {
                var sourceCollisionComponent = CollisionEventGroup[entityA];
                var targetCollisionComponent = CollisionEventGroup[entityB];

                var sourceTransform = TransformLookup[entityA];
                var targetransform = TransformLookup[entityB];

                var sourceForward = sourceTransform.Forward().Flat();
                var targetForward = targetransform.Forward().Flat();

                var sourceToTarget = math.normalize(targetransform.Position.Flat() - sourceTransform.Position.Flat());

                var sameDirectionValue = math.dot(sourceForward, targetForward);

                var directionToTarget = math.dot(sourceForward, sourceToTarget);

                source = TrafficCollisionDirectionType.None;
                target = TrafficCollisionDirectionType.None;

                if (sameDirectionValue > TrafficCollisionConfigReference.Config.Value.ForwardDirectionValue)
                {
                    if (directionToTarget > 0)
                    {
                        source = TrafficCollisionDirectionType.Front;
                        target = TrafficCollisionDirectionType.Back;
                    }
                    else
                    {
                        source = TrafficCollisionDirectionType.Back;
                        target = TrafficCollisionDirectionType.Front;
                    }
                }
                else if (sameDirectionValue < -TrafficCollisionConfigReference.Config.Value.ForwardDirectionValue)
                {
                    source = TrafficCollisionDirectionType.Front;
                    target = TrafficCollisionDirectionType.Front;
                }
                else
                {
                    var sourceRight = sourceTransform.Right().Flat();
                    var targetRight = targetransform.Right().Flat();

                    var sourceRightDirection = math.dot(sourceRight, sourceToTarget);
                    var targetDirectionDirection = math.dot(targetRight, -sourceToTarget);

                    if (sourceRightDirection > TrafficCollisionConfigReference.Config.Value.SideDirectionValue)
                    {
                        source = TrafficCollisionDirectionType.Right;
                    }
                    else if (sourceRightDirection < -TrafficCollisionConfigReference.Config.Value.SideDirectionValue)
                    {
                        source = TrafficCollisionDirectionType.Left;
                    }
                    else if (directionToTarget > 0)
                    {
                        source = TrafficCollisionDirectionType.Front;
                    }
                    else
                    {
                        source = TrafficCollisionDirectionType.Back;
                    }

                    if (targetDirectionDirection > TrafficCollisionConfigReference.Config.Value.SideDirectionValue)
                    {
                        target = TrafficCollisionDirectionType.Right;
                    }
                    else if (targetDirectionDirection < -TrafficCollisionConfigReference.Config.Value.SideDirectionValue)
                    {
                        target = TrafficCollisionDirectionType.Left;
                    }
                    else if (directionToTarget > 0)
                    {
                        target = TrafficCollisionDirectionType.Front;
                    }
                    else
                    {
                        target = TrafficCollisionDirectionType.Back;
                    }
                }
            }

            private void AddCollision(Entity sourceEntity, Entity collidedEntity, TrafficCollisionDirectionType source, TrafficCollisionDirectionType target)
            {
                var collisionComponent = CollisionEventGroup[sourceEntity];

                bool addState = false;

                if (collisionComponent.LastCollisionEntity != collidedEntity)
                {
                    collisionComponent.LastCollisionEntity = collidedEntity;
                    collisionComponent.CollisionTime = Timestamp;
                    collisionComponent.LastCollisionEventTime = Timestamp;
                    collisionComponent.SourceCollisionDirectionType = source;
                    collisionComponent.TargetCollisionDirectionType = target;
                }
                else
                {
                    collisionComponent.LastCollisionEventTime = Timestamp;
                }

                CollisionEventGroup[sourceEntity] = collisionComponent;

                if (!TrafficStateLookup.HasComponent(sourceEntity))
                    return;

                addState = Timestamp - collisionComponent.LastIdleTime >= TrafficCollisionConfigReference.Config.Value.IgnoreCollisionDuration;

                var trafficStateComponent = TrafficStateLookup[sourceEntity];

                if (addState && !trafficStateComponent.HasIdleState(TrafficIdleState.Collided))
                {
                    TrafficCollisionUtils.AddCollisionState(
                        ref CommandBuffer,
                        sourceEntity,
                        ref collisionComponent,
                        ref trafficStateComponent,
                        Timestamp);

                    TrafficStateLookup[sourceEntity] = trafficStateComponent;
                }
            }
        }
    }
}