using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


#if REESE_PATH
using Reese.Path;
#endif

namespace Spirit604.DotsCity.Simulation.Npc.Navigation
{
    [UpdateInGroup(typeof(NavSimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct NpcRecalculateNavTargetSystem : ISystem
    {
        #region Variables

        private EntityQuery updateQuery;

        #endregion

        #region Unity lifecycle           

        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithDisabled<UpdateNavTargetTag>()
                .WithAll<NavAgentComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
            state.RequireForUpdate<CarHashMapSystem.Singleton>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var recalcuteTargetJob = new RecalcuteTargetJob()
            {
                CarHashMapSingleton = SystemAPI.GetSingleton<CarHashMapSystem.Singleton>(),
                Timestamp = (float)SystemAPI.Time.ElapsedTime,
                DestinationLookup = SystemAPI.GetComponentLookup<DestinationComponent>(true),
                LocalAvoidanceAgentLookup = SystemAPI.GetComponentLookup<LocalAvoidanceAgentTag>(true),
                EnabledNavigationLookup = SystemAPI.GetComponentLookup<EnabledNavigationTag>(true),
                NavAgentConfigReference = SystemAPI.GetSingleton<NavAgentConfigReference>(),
            };

            recalcuteTargetJob.ScheduleParallel();
        }

        [WithDisabled(typeof(UpdateNavTargetTag))]
#if !REESE_PATH
        [WithNone(typeof(IdleTag), typeof(AchievedNavTargetTag), typeof(CustomMovementTag))]
#else
        [WithNone(typeof(IdleTag), typeof(AchievedNavTargetTag), typeof(CustomMovementTag), typeof(PathPlanning))]
#endif
        [BurstCompile]
        public partial struct RecalcuteTargetJob : IJobEntity
        {
            [ReadOnly]
            public CarHashMapSystem.Singleton CarHashMapSingleton;

            [ReadOnly]
            public ComponentLookup<DestinationComponent> DestinationLookup;

            [ReadOnly]
            public ComponentLookup<LocalAvoidanceAgentTag> LocalAvoidanceAgentLookup;

            [ReadOnly]
            public ComponentLookup<EnabledNavigationTag> EnabledNavigationLookup;

            [ReadOnly]
            public NavAgentConfigReference NavAgentConfigReference;

            [ReadOnly]
            public float Timestamp;

            void Execute(
                Entity entity,
                ref NavAgentComponent navAgentComponent,
                EnabledRefRW<UpdateNavTargetTag> updateNavTargetTagRW,
                in LocalTransform transform,
                in NavAgentSteeringComponent navAgentSteeringComponent,
                in CircleColliderComponent collider)
            {
                var navigationIsEnabled = EnabledNavigationLookup.IsComponentEnabled(entity);

                if (navigationIsEnabled && !navAgentSteeringComponent.HasSteeringTarget)
                {
                    return;
                }

                var npcPosition = transform.Position;
                var npcPositionFlat = npcPosition.Flat();

                float3 destination = default;

                var npcIsPedestrian = DestinationLookup.HasComponent(entity);

                if (navigationIsEnabled && navAgentSteeringComponent.HasSteeringTarget)
                {
                    destination = navAgentSteeringComponent.SteeringTargetValue.Flat();
                }
                else
                {
                    if (npcIsPedestrian)
                    {
                        destination = DestinationLookup[entity].Value;
                    }
                    else
                    {
                        destination = navAgentComponent.PathEndPosition.Flat();
                    }
                }

                VectorExtensions.Line routeLine = new VectorExtensions.Line(npcPositionFlat, destination);
                var dirToTarget = math.normalize(destination - npcPositionFlat);

                var keys = HashMapHelper.GetHashMapPosition9Cells(npcPositionFlat);
                var obstacleEntity = Entity.Null;
                var currentDistanceToTarget = float.MaxValue;

                for (int i = 0; i < keys.Length; i++)
                {
                    if (CarHashMapSingleton.CarHashMap.TryGetFirstValue(keys[i], out var obstacleHash, out var nativeMultiHashMapIterator))
                    {
                        do
                        {
                            var obstaclePosition = obstacleHash.Position;
                            var obstaclePositionFlat = obstaclePosition.Flat();

                            var dirToObstacle = math.normalize(obstaclePositionFlat - npcPositionFlat);

                            var inFrontOfNpcDir = math.dot(dirToTarget, dirToObstacle);

                            if (inFrontOfNpcDir < 0)
                                continue;

                            float distance = math.distancesq(npcPositionFlat, obstaclePositionFlat);

                            if (distance >= NavAgentConfigReference.Config.Value.MaxDistanceToObstacleSQ)
                                continue;

                            if (math.abs(npcPosition.y - obstaclePosition.y) > NavAgentConfigReference.Config.Value.MaxHeightDiff)
                                continue;

                            Vector3 size = obstacleHash.BoundsExtents;

                            size.x += collider.Radius + 0.01f;
                            size.z += collider.Radius + 0.01f;

                            Quaternion obstacleRotation = obstacleHash.Rotation;

                            ObstacleSquare obstacleSquare = new ObstacleSquare(obstaclePositionFlat, obstacleRotation, size);

                            Vector3 intersectPoint = VectorExtensions.LineWithSquareIntersect(routeLine, obstacleSquare.Square, true);

                            var isIntersect = intersectPoint != Vector3.zero;

                            if (isIntersect)
                            {
                                if (currentDistanceToTarget > distance)
                                {
                                    currentDistanceToTarget = distance;
                                    obstacleEntity = obstacleHash.Entity;
                                }
                            }

                        } while (CarHashMapSingleton.CarHashMap.TryGetNextValue(out obstacleHash, ref nativeMultiHashMapIterator));
                    }
                }

                keys.Dispose();

                if (obstacleEntity != Entity.Null)
                {
                    bool updateTimePassed = Timestamp - navAgentComponent.LastUpdateTimestamp > NavAgentConfigReference.Config.Value.RecalcFrequency;
                    bool localAvoidanceAgent = LocalAvoidanceAgentLookup.HasComponent(entity);

                    var targetChanged = navAgentComponent.ObstacleEntity != obstacleEntity;

                    bool updateTarget = false;

                    if (!localAvoidanceAgent)
                    {
                        updateTarget = (navAgentSteeringComponent.HasSteeringTarget && updateTimePassed) || !navigationIsEnabled || targetChanged;
                    }
                    else
                    {
                        updateTarget = updateTimePassed && !navigationIsEnabled;
                    }

                    if (updateTarget)
                    {
                        navAgentComponent.ObstacleEntity = obstacleEntity;

                        if (npcIsPedestrian)
                        {
                            navAgentComponent.PathEndPosition = DestinationLookup[entity].Value;
                        }

                        updateNavTargetTagRW.ValueRW = true;
                    }
                }
            }
        }

        #endregion
    }
}