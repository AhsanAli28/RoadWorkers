using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.GraphicsIntegration;
using Collider = Unity.Physics.Collider;

namespace Spirit604.DotsCity.Simulation.Car.Custom
{
    [UpdateInGroup(typeof(RaycastGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct WheelContactSystem : ISystem, ISystemStartStop
    {
#if UNITY_EDITOR
        private const int MaxRecordCapacity = 10000;
#endif

        private EntityQuery updateQuery;

#if UNITY_EDITOR
        private NativeParallelMultiHashMap<Entity, WheelDebug> wheelDebugInfo;

        public static NativeParallelMultiHashMap<Entity, WheelDebug> WheelDebugInfoStaticRef { get; private set; }
#endif

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<WheelContact>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
#if UNITY_EDITOR
            if (wheelDebugInfo.IsCreated)
            {
                wheelDebugInfo.Dispose();
            }
#endif
        }

        public void OnStartRunning(ref SystemState state)
        {
#if UNITY_EDITOR
            if (!wheelDebugInfo.IsCreated)
            {
                wheelDebugInfo = new NativeParallelMultiHashMap<Entity, WheelDebug>(1000, Allocator.Persistent);
                WheelDebugInfoStaticRef = wheelDebugInfo;
            }
#endif
        }

        public void OnStopRunning(ref SystemState state) { }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
#if UNITY_EDITOR
            wheelDebugInfo.Clear();

            var entityCount = updateQuery.CalculateEntityCount();

            if (wheelDebugInfo.Capacity < entityCount)
            {
                wheelDebugInfo.Capacity = entityCount * 2;
            }
#endif

            var contactJob = new ContactJob()
            {
                PhysicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld,
                PhysicsGraphicalSmoothingLookup = SystemAPI.GetComponentLookup<PhysicsGraphicalSmoothing>(),

#if UNITY_EDITOR
                WheelDebugInfo = wheelDebugInfo.AsParallelWriter(),
#endif
            };

            contactJob.ScheduleParallel();
        }

        [BurstCompile]
        public unsafe partial struct ContactJob : IJobEntity
        {
            [ReadOnly]
            public PhysicsWorld PhysicsWorld;

            [ReadOnly]
            public ComponentLookup<PhysicsGraphicalSmoothing> PhysicsGraphicalSmoothingLookup;

#if UNITY_EDITOR
            [NativeDisableContainerSafetyRestriction]
            public NativeParallelMultiHashMap<Entity, WheelDebug>.ParallelWriter WheelDebugInfo;
#endif

            void Execute(
                Entity entity,
                ref WheelContact contact,
                in Wheel wheel,
                in WheelInput input)
            {
                // Should be replaced with PhysicsWorldIndex check
                if (!PhysicsGraphicalSmoothingLookup.HasComponent(wheel.VehicleEntity))
                {
                    contact.IsInContact = true;
                    contact.CurrentSuspensionLength = wheel.SuspensionLength;
                    return;
                }

                switch (wheel.CastType)
                {
                    case CastType.Ray:
                        {
                            var colliderCastInput = new RaycastInput
                            {
                                Start = input.LocalToWorld.pos,
                                End = input.LocalToWorld.pos - input.Up * (wheel.SuspensionLength + wheel.Radius),
                                Filter = wheel.CastFilter
                            };

#if UNITY_EDITOR
                            var wheelDebug = new WheelDebug();

                            wheelDebug.Start = colliderCastInput.Start;
                            wheelDebug.End = colliderCastInput.End;
                            wheelDebug.IsInContact = true;
#endif

                            if (!PhysicsWorld.CastRay(colliderCastInput, out var hit) || math.isnan(hit.SurfaceNormal).x)
                            {
                                contact.IsInContact = false;
                                contact.CurrentSuspensionLength = wheel.SuspensionLength;

#if UNITY_EDITOR
                                if (WheelDebugInfo.Capacity < MaxRecordCapacity)
                                {
                                    wheelDebug.IsInContact = false;
                                    WheelDebugInfo.Add(entity, wheelDebug);
                                }
#endif
                                return;
                            }

#if UNITY_EDITOR
                            if (WheelDebugInfo.Capacity < MaxRecordCapacity)
                            {
                                wheelDebug.End = hit.Position;
                                WheelDebugInfo.Add(entity, wheelDebug);
                            }
#endif

                            contact.IsInContact = true;
                            contact.Point = hit.Position;
                            contact.Normal = hit.SurfaceNormal;

                            contact.CurrentSuspensionLength = hit.Fraction * (wheel.SuspensionLength + wheel.Radius) - wheel.Radius;

                            break;
                        }
                    case CastType.Collider:
                        {
                            var colliderCastInput = new ColliderCastInput
                            {
                                Collider = (Collider*)wheel.Collider.GetUnsafePtr(),
                                Start = input.LocalToWorld.pos,
                                End = input.LocalToWorld.pos - input.Up * (wheel.SuspensionLength),
                                Orientation = input.LocalToWorld.rot,
                            };

#if UNITY_EDITOR
                            var wheelDebug = new WheelDebug();
                            wheelDebug.Start = colliderCastInput.Start;
                            wheelDebug.End = colliderCastInput.End;
                            wheelDebug.IsInContact = true;
#endif

                            if (!PhysicsWorld.CastCollider(colliderCastInput, out var hit))
                            {
                                contact.IsInContact = false;
                                contact.CurrentSuspensionLength = wheel.SuspensionLength;

#if UNITY_EDITOR
                                wheelDebug.IsInContact = false;
                                WheelDebugInfo.Add(entity, wheelDebug);
#endif

                                return;
                            }

#if UNITY_EDITOR
                            wheelDebug.End = hit.Position;
                            WheelDebugInfo.Add(entity, wheelDebug);
#endif

                            contact.IsInContact = true;
                            contact.Point = hit.Position;
                            contact.Normal = hit.SurfaceNormal;
                            contact.CurrentSuspensionLength = hit.Fraction * (wheel.SuspensionLength);

                            break;
                        }
                }
            }
        }
    }
}