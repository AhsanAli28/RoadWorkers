using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    [UpdateInGroup(typeof(CullSimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct CalcCameraCustomCullingSystem : ISystem
    {
        private EntityQuery cullPointGroup;
        private EntityQuery cameraDataGroup;
        private EntityQuery cullGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            cullPointGroup = SystemAPI.QueryBuilder()
                .WithAll<CullPointTag, LocalTransform>()
                .Build();

            cameraDataGroup = SystemAPI.QueryBuilder()
                .WithAll<CameraData>()
                .Build();

            cullGroup = SystemAPI.QueryBuilder()
                .WithNone<PooledEventTag>()
                .WithAbsent<PreInitInCameraTag>()
                .WithAllRW<CullStateComponent>()
                .WithPresentRW<CulledEventTag, InPermittedRangeTag>()
                .WithPresentRW<InViewOfCameraTag>()
                .WithAll<LocalTransform, CullCameraSharedConfig>()
                .Build();

            state.RequireForUpdate(cullPointGroup);
            state.RequireForUpdate(cameraDataGroup);
            state.RequireForUpdate<CullSystemConfigReference>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();

            var calcCullJob = new CalcCullJob()
            {
                CullPointPosition = cullPointGroup.GetSingleton<LocalTransform>().Position,
                ViewProjectionMatrix = cameraDataGroup.GetSingleton<CameraData>().ViewProjectionMatrix,
            };

            calcCullJob.ScheduleParallel(cullGroup);
        }

        [WithNone(typeof(PooledEventTag))]
        [WithAbsent(typeof(PreInitInCameraTag))]
        [BurstCompile(DisableSafetyChecks = true)]
        public partial struct CalcCullJob : IJobEntity
        {
            [ReadOnly]
            public float3 CullPointPosition;

            [ReadOnly]
            public Matrix4x4 ViewProjectionMatrix;

            void Execute(
                ref CullStateComponent cullComponent,
                EnabledRefRW<CulledEventTag> culledTagRW,
                EnabledRefRW<InPermittedRangeTag> inPermittedRangeTagRW,
                EnabledRefRW<InViewOfCameraTag> inViewOfCameraTagRW,
                in CullCameraSharedConfig config,
                in LocalTransform transform)
            {
                float distance = 0;

                if (!config.IgnoreY)
                {
                    distance = math.distancesq(transform.Position, CullPointPosition);
                }
                else
                {
                    distance = math.distancesq(transform.Position.Flat(), CullPointPosition.Flat());
                }

                bool inViewOfCamera = CullUtils.InViewOfCamera(ViewProjectionMatrix, transform.Position, config.ViewPortOffset);

                CullState cullState = CullState.Culled;

                if (inViewOfCamera)
                {
                    if (distance < config.VisibleDistanceSQ)
                    {
                        cullState = CullState.InViewOfCamera;
                    }
                    else if (distance < config.MaxDistanceSQ)
                    {
                        cullState = CullState.CloseToCamera;
                    }
                }
                else
                {
                    if (distance < config.BehindVisibleDistanceSQ)
                    {
                        cullState = CullState.InViewOfCamera;
                    }
                    else if (distance < config.BehindMaxDistanceSQ)
                    {
                        cullState = CullState.CloseToCamera;
                    }
                }

                if (cullComponent.State != cullState)
                {
                    CullStateUtils.ChangeState(
                        in cullState,
                        ref cullComponent,
                        ref culledTagRW,
                        ref inPermittedRangeTagRW,
                        ref inViewOfCameraTagRW);
                }
            }
        }
    }
}