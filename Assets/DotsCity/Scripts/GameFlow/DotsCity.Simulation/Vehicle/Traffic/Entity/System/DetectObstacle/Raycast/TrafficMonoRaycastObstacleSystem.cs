using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(PreEarlyJobGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficMonoRaycastObstacleSystem : ISystem, ISystemStartStop
    {
        private const int InitialRaycastCapacity = 1000;
        private const int MinCommandsPerJob = 12;

        public struct CustomTargetHash
        {
            public Entity Entity;
            public float3 Position;
        }

        public struct EntityRaycastData
        {
            public Entity Entity;
        }

        private EntityQuery raycastGroupQuery;
        private NativeList<BoxcastCommand> commands;
        private NativeList<EntityRaycastData> entities;

        void ISystem.OnCreate(ref SystemState state)
        {
            raycastGroupQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<HasDriverTag, TrafficTag, TrafficRaycastTag, TrafficRaycastObstacleComponent>()
                .Build(state.EntityManager);

            state.RequireForUpdate(raycastGroupQuery);

#if UNITY_EDITOR
            state.RequireForUpdate<TrafficRaycastGizmosSystem.Singleton>();
#endif
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            if (commands.IsCreated)
            {
                commands.Dispose();
            }

            if (entities.IsCreated)
            {
                entities.Dispose();
            }
        }

        void ISystemStartStop.OnStartRunning(ref SystemState state)
        {
            if (!commands.IsCreated)
            {
                commands = new NativeList<BoxcastCommand>(InitialRaycastCapacity, Allocator.Persistent);
            }

            if (!entities.IsCreated)
            {
                entities = new NativeList<EntityRaycastData>(InitialRaycastCapacity, Allocator.Persistent);
            }
        }

        void ISystemStartStop.OnStopRunning(ref SystemState state) { }

        void ISystem.OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();

#if UNITY_EDITOR
            SystemAPI.GetSingleton<TrafficRaycastGizmosSystem.Singleton>().Clear();
#endif

            var entityCount = raycastGroupQuery.CalculateEntityCount();
            var commandArray = new NativeArray<BoxcastCommand>(entityCount, Allocator.TempJob);
            var results = new NativeArray<UnityEngine.RaycastHit>(entityCount, Allocator.TempJob);

            var depJob = state.Dependency;

            var trafficCommonSettingsConfigBlobReference = SystemAPI.GetSingleton<TrafficCommonSettingsConfigBlobReference>();

            commands.Clear();
            entities.Clear();

            var raycastObstacleJob = new RaycastObstacleJob()
            {
                Commands = commands.AsParallelWriter(),
                Entities = entities.AsParallelWriter(),
                TrafficChangingLaneEventLookup = SystemAPI.GetComponentLookup<TrafficChangingLaneEventTag>(true),
                TrafficRaycastConfigReference = SystemAPI.GetSingleton<RaycastConfigReference>(),
                Timestamp = (float)SystemAPI.Time.ElapsedTime,

#if UNITY_EDITOR
                TrafficRaycastDebugInfo = SystemAPI.GetSingleton<TrafficRaycastGizmosSystem.Singleton>().TrafficRaycastDebugInfo.AsParallelWriter()
#endif
            };

            var allocateCommandJob = new AllocateCommandJob()
            {
                Commands = commands,
                CommandArray = commandArray,
            };

            var raycastObstacleHandle = raycastObstacleJob.ScheduleParallel(depJob);

            var allocateCommandHandle = allocateCommandJob.Schedule(raycastObstacleHandle);

            var castCommandHandle = BoxcastCommand.ScheduleBatch(commandArray, results, MinCommandsPerJob, 1, allocateCommandHandle);

            var resultJob = new ResultJob()
            {
                Results = results,
                Entities = entities,
                TrafficRaycastObstacleComponentLookup = SystemAPI.GetComponentLookup<TrafficRaycastObstacleComponent>(false)
            }.Schedule(castCommandHandle);

            commandArray.Dispose(resultJob);
            results.Dispose(resultJob);

            state.Dependency = resultJob;
        }

        [BurstCompile]
        private struct AllocateCommandJob : IJob
        {
            [ReadOnly]
            public NativeList<BoxcastCommand> Commands;

            [WriteOnly]
            public NativeArray<BoxcastCommand> CommandArray;

            public void Execute()
            {
                for (int i = 0; i < Commands.Length; i++)
                {
                    CommandArray[i] = Commands[i];
                }
            }
        }

        [BurstCompile]
        private struct ResultJob : IJob
        {
            [ReadOnly]
            public NativeArray<UnityEngine.RaycastHit> Results;

            [ReadOnly]
            public NativeList<EntityRaycastData> Entities;

            public ComponentLookup<TrafficRaycastObstacleComponent> TrafficRaycastObstacleComponentLookup;

            public void Execute()
            {
                for (int i = 0; i < Results.Length; i++)
                {
                    if (Results[i].colliderInstanceID != 0)
                    {
                        var trafficRaycastObstacle = TrafficRaycastObstacleComponentLookup[Entities[i].Entity];
                        trafficRaycastObstacle.HasObstacle = true;
                        TrafficRaycastObstacleComponentLookup[Entities[i].Entity] = trafficRaycastObstacle;
                    }
                }
            }
        }

        [WithAll(typeof(TrafficTag), typeof(HasDriverTag))]
        [BurstCompile]
        public partial struct RaycastObstacleJob : IJobEntity
        {
            [WriteOnly]
            public NativeList<BoxcastCommand>.ParallelWriter Commands;

            [WriteOnly]
            public NativeList<EntityRaycastData>.ParallelWriter Entities;

            [ReadOnly]
            public ComponentLookup<TrafficChangingLaneEventTag> TrafficChangingLaneEventLookup;

            [ReadOnly]
            public RaycastConfigReference TrafficRaycastConfigReference;

            [ReadOnly]
            public float Timestamp;

#if UNITY_EDITOR
            [Unity.Collections.LowLevel.Unsafe.NativeDisableContainerSafetyRestriction]
            public NativeParallelMultiHashMap<Entity, TrafficRaycastGizmosSystem.BoxCastInfo>.ParallelWriter TrafficRaycastDebugInfo;
#endif

            void Execute(
                Entity entity,
                ref TrafficRaycastObstacleComponent trafficRaycastObstacleComponent,
                EnabledRefRW<TrafficRaycastTag> trafficRaycastTagRW,
                in TrafficPathComponent pathComponent,
                in TrafficSettingsComponent trafficSettingsComponent,
                in SpeedComponent speedComponent,
                in BoundsComponent boundsComponent,
                in LocalTransform transform)
            {
                trafficRaycastTagRW.ValueRW = false;

                var previousState = trafficRaycastObstacleComponent.HasObstacle;
                trafficRaycastObstacleComponent.HasObstacle = false;
                trafficRaycastObstacleComponent.ObstacleEntity = Entity.Null;

                var startOffset = boundsComponent.Size.z * TrafficRaycastConfigReference.Config.Value.BoundsMultiplier;

                float currentSpeed = speedComponent.Value;
                float maxSpeed = trafficSettingsComponent.MaxSpeed;
                float t = 1 - (maxSpeed - currentSpeed) / maxSpeed;
                var rayLength = math.lerp(TrafficRaycastConfigReference.Config.Value.MinRayLength, TrafficRaycastConfigReference.Config.Value.MaxRayLength, t);

                var sourcePoint = transform.Position;
                var targetPoint = pathComponent.DestinationWayPoint;
                float3 castDirection = math.normalizesafe(targetPoint - sourcePoint);

                float3 origin = transform.Position + new float3(0, TrafficRaycastConfigReference.Config.Value.RayYaxisOffset, 0) + castDirection * startOffset;
                quaternion orientation = transform.Rotation;
                float3 halfExtents = new float3(TrafficRaycastConfigReference.Config.Value.SideOffset, TrafficRaycastConfigReference.Config.Value.BoxCastHeight, 0.01f);
                float maxDistance = rayLength;

                int layerMask = (int)TrafficRaycastConfigReference.Config.Value.RaycastFilter;

                var queryParams = new QueryParameters(layerMask, false, QueryTriggerInteraction.Ignore, false);

                var command = new BoxcastCommand(origin, halfExtents, orientation, castDirection, queryParams, maxDistance);

                Entities.AddNoResize(new EntityRaycastData()
                {
                    Entity = entity
                });

                Commands.AddNoResize(command);
                //if (CollisionWorld.BoxCast(center, orientation, halfExtents, castDirection, maxDistance, out var hitInfo, currentCollisionFilter, QueryInteraction.IgnoreTriggers))
                //{
                //    var targetVehicleEntity = hitInfo.Entity;
                //    var hasTarget = RaycastAlways && targetVehicleEntity != Entity.Null || CustomEntityTargets.Contains(targetVehicleEntity);

                //    if (hasTarget && targetVehicleEntity != entity)
                //    {
                //        var myVehicleChangingLane = TrafficChangingLaneEventLookup.HasComponent(entity) && TrafficChangingLaneEventLookup.IsComponentEnabled(entity);
                //        var targetVehicleChangingLane = TrafficChangingLaneEventLookup.HasComponent(targetVehicleEntity) && TrafficChangingLaneEventLookup.IsComponentEnabled(targetVehicleEntity);

                //        if (!myVehicleChangingLane || !targetVehicleChangingLane)
                //        {
                //            trafficRaycastObstacleComponent.HasObstacle = true;
                //            trafficRaycastObstacleComponent.ObstacleEntity = targetVehicleEntity;
                //        }
                //    }
                //}

#if UNITY_EDITOR
                var boxCastInfo = new TrafficRaycastGizmosSystem.BoxCastInfo()
                {
                    origin = origin,
                    halfExtents = halfExtents,
                    orientation = orientation,
                    direction = castDirection,
                    MaxDistance = maxDistance,
                    HasHit = previousState
                };

                TrafficRaycastDebugInfo.Add(entity, boxCastInfo);
#endif
            }

        }
    }
}