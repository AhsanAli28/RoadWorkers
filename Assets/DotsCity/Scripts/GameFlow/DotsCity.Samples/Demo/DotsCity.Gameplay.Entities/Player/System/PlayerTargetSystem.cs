using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Npc;
using Spirit604.DotsCity.Gameplay.Weapon;
using Spirit604.Extensions;
using Spirit604.Gameplay.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player
{
    [UpdateInGroup(typeof(LateSimulationGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct PlayerTargetSystem : ISystem
    {
        private const float DefaultTargetScale = 1f;

        private EntityQuery targets;
        private EntityQuery playerInputQuery;
        private EntityQuery crossHairQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            targets = SystemAPI.QueryBuilder()
                .WithAll<PlayerTargetComponent, InViewOfCameraTag, LocalToWorld>()
                .Build();

            playerInputQuery = SystemAPI.QueryBuilder()
                .WithAll<PlayerTag, InputComponent, LocalTransform>()
                .Build();

            crossHairQuery = SystemAPI.QueryBuilder()
                .WithAll<CrossHairComponent>()
                .Build();

            state.RequireForUpdate(playerInputQuery);
            state.RequireForUpdate(crossHairQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            if (playerInputQuery.CalculateEntityCount() != 1)
                return;

            var playerEntity = playerInputQuery.GetSingletonEntity();
            var playerPosition = state.EntityManager.GetComponentData<LocalTransform>(playerEntity);
            var inputComponent = state.EntityManager.GetComponentData<InputComponent>(playerEntity);
            float3 shootInput = inputComponent.ShootInput;
            float3 shootDirection = inputComponent.ShootDirection;

            var playerTargetSettingsReference = SystemAPI.GetSingleton<PlayerTargetSettingsReference>();

            switch (playerTargetSettingsReference.Config.Value.PlayerShootDirectionSource)
            {
                case ShootDirectionSource.Joystick:
                    {
                        var crosshairTargetDirectionJob = new CrosshairTargetDirectionJob()
                        {
                            PlayerTargetSettings = playerTargetSettingsReference,
                            PlayerPosition = playerPosition,
                            ShootDirection = shootDirection
                        };

                        state.Dependency = crosshairTargetDirectionJob.Schedule(state.Dependency);

                        break;
                    }
                case ShootDirectionSource.CrossHair:
                    {
                        var chunks = targets.ToArchetypeChunkArray(Allocator.TempJob);

                        var crosshairTargetJob = new CrosshairTargetJob()
                        {
                            TargetChunks = chunks,
                            LocalToWorldType = SystemAPI.GetComponentTypeHandle<LocalToWorld>(true),
                            PlayerTargetComponentType = SystemAPI.GetComponentTypeHandle<PlayerTargetComponent>(true),
                            CrossHairUpdateScaleTagLookup = SystemAPI.GetComponentLookup<CrossHairUpdateScaleTag>(false),
                            PlayerTargetSettings = playerTargetSettingsReference,
                            PlayerPosition = playerPosition.Position,
                            ShootDirection = shootDirection,
                        };

                        state.Dependency = crosshairTargetJob.Schedule(state.Dependency);
                        chunks.Dispose(state.Dependency);

                        break;
                    }
                case ShootDirectionSource.Mouse:
                    {
                        var targetPointJob = new CrosshairTargetPointJob()
                        {
                            PlayerTargetSettings = playerTargetSettingsReference,
                            TargetPosition = shootInput
                        };

                        state.Dependency = targetPointJob.Schedule(state.Dependency);

                        break;
                    }
            }
        }

        [BurstCompile]
        private partial struct CrosshairTargetJob : IJobEntity
        {
            [ReadOnly]
            public NativeArray<ArchetypeChunk> TargetChunks;

            [ReadOnly]
            public ComponentTypeHandle<LocalToWorld> LocalToWorldType;

            [ReadOnly]
            public ComponentTypeHandle<PlayerTargetComponent> PlayerTargetComponentType;

            public ComponentLookup<CrossHairUpdateScaleTag> CrossHairUpdateScaleTagLookup;

            [ReadOnly]
            public PlayerTargetSettingsReference PlayerTargetSettings;

            [ReadOnly]
            public float3 PlayerPosition;

            [ReadOnly]
            public float3 ShootDirection;

            void Execute(
                Entity entity,
                ref LocalTransform transform,
                ref CrossHairComponent сrossHairComponent)
            {
                if (ShootDirection.x == 0 && ShootDirection.z == 0)
                {
                    transform = LocalTransform.FromPosition(new float3(0, -100, 0));
                    return;
                }

                float maxDistance = float.MaxValue;
                bool hasTarget = false;

                float3 targetPosition = default;
                float targetScale = 1f;

                if (TargetChunks.IsCreated)
                {
                    for (int i = 0; i < TargetChunks.Length; i++)
                    {
                        var chunk = TargetChunks[i];

                        var targetPositions = chunk.GetNativeArray(ref LocalToWorldType);
                        var targetComponents = chunk.GetNativeArray(ref PlayerTargetComponentType);

                        var hasScale = chunk.HasChunkComponent(ref PlayerTargetComponentType);

                        if (targetPositions.Length > 0)
                        {
                            for (int j = 0; j < targetPositions.Length; j++)
                            {
                                float distanceSQ = math.distancesq(targetPositions[j].Position.Flat(), PlayerPosition.Flat());

                                if (distanceSQ < PlayerTargetSettings.Config.Value.MaxTargetDistanceSQ &&
                                      distanceSQ < maxDistance &&
                                      InFieldOfView(PlayerPosition, targetPositions[j].Position, ShootDirection, PlayerTargetSettings.Config.Value.MaxCaptureAngle))
                                {
                                    hasTarget = true;
                                    maxDistance = distanceSQ;
                                    targetPosition = targetPositions[j].Position;
                                    targetPosition.y = PlayerTargetSettings.Config.Value.DefaultAimPointYPosition;
                                    targetScale = hasScale ? targetComponents[j].ScaleRadius : DefaultTargetScale;
                                }
                            }
                        }
                    }

                    bool InFieldOfView(float3 _playerPosition, float3 _targetPosition, float3 _shootDirection, float maxCaptureAngle)
                    {
                        float3 directionToTarget = math.normalize(_targetPosition.Flat() - _playerPosition.Flat());
                        float signedAngle = math.abs(Vector3.SignedAngle(directionToTarget, _shootDirection, Vector3.up));

                        return signedAngle < maxCaptureAngle;
                    }
                }

                if (!hasTarget)
                {
                    targetPosition = PlayerPosition + math.normalize(ShootDirection * 10) * PlayerTargetSettings.Config.Value.DefaultAimPointDistance;
                    targetPosition.y = PlayerTargetSettings.Config.Value.DefaultAimPointYPosition;
                }

                transform.Position = targetPosition;

                if (сrossHairComponent.TargetScale != targetScale)
                {
                    сrossHairComponent.TargetScale = targetScale;
                    CrossHairUpdateScaleTagLookup.SetComponentEnabled(entity, true);
                }
            }
        }

        [BurstCompile]
        private partial struct CrosshairTargetDirectionJob : IJobEntity
        {
            [ReadOnly]
            public PlayerTargetSettingsReference PlayerTargetSettings;

            [ReadOnly]
            public LocalTransform PlayerPosition;

            [ReadOnly]
            public float3 ShootDirection;

            void Execute(
                 ref LocalTransform transform,
                 ref CrossHairComponent сrossHairComponent)
            {
                float3 targetPosition;

                if (!ShootDirection.Equals(float3.zero))
                {
                    targetPosition = PlayerPosition.Position + math.normalizesafe(ShootDirection * 10) * PlayerTargetSettings.Config.Value.DefaultAimPointDistance;
                    targetPosition.y = PlayerTargetSettings.Config.Value.DefaultAimPointYPosition;
                }
                else
                {
                    targetPosition = new float3(-1000, -100, 0);
                }

                transform.Position = targetPosition;
                сrossHairComponent.TargetScale = 1f;
            }
        }


        [BurstCompile]
        private partial struct CrosshairTargetPointJob : IJobEntity
        {
            [ReadOnly]
            public PlayerTargetSettingsReference PlayerTargetSettings;

            [ReadOnly]
            public float3 TargetPosition;

            void Execute(
                 ref LocalTransform transform,
                 ref CrossHairComponent сrossHairComponent)
            {
                float3 targetPosition;

                if (!TargetPosition.Equals(float3.zero))
                {
                    targetPosition = TargetPosition;
                    targetPosition.y = PlayerTargetSettings.Config.Value.DefaultAimPointYPosition;
                }
                else
                {
                    targetPosition = new float3(-1000, -100, 0);
                }

                transform.Position = targetPosition;
                сrossHairComponent.TargetScale = 1f;
            }
        }

    }
}