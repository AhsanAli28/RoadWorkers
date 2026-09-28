using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Core
{
    [UpdateInGroup(typeof(CullSimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct CalcCustomCullingSystem : ISystem
    {
        private EntityQuery cullPointGroup;
        private EntityQuery cullGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            cullPointGroup = SystemAPI.QueryBuilder()
                .WithAll<CullPointTag, LocalTransform>()
                .Build();

            cullGroup = SystemAPI.QueryBuilder()
                .WithNone<PooledEventTag>()
                .WithAbsent<PreInitInCameraTag>()
                .WithAllRW<CullStateComponent>()
                .WithPresentRW<CulledEventTag, InPermittedRangeTag>()
                .WithPresentRW<InViewOfCameraTag>()
                .WithAll<LocalTransform, CullSharedConfig>()
                .Build();

            state.RequireForUpdate(cullPointGroup);
            state.RequireForUpdate<CullSystemConfigReference>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();

            var calcCullJob = new CalcCustomCullJob()
            {
                CullPointPosition = cullPointGroup.GetSingleton<LocalTransform>().Position,
            };

            calcCullJob.ScheduleParallel(cullGroup);
        }

        [WithNone(typeof(PooledEventTag))]
        [WithAbsent(typeof(PreInitInCameraTag))]
        [BurstCompile(DisableSafetyChecks = true)]
        public partial struct CalcCustomCullJob : IJobEntity
        {
            [ReadOnly]
            public float3 CullPointPosition;

            void Execute(
                ref CullStateComponent cullComponent,
                EnabledRefRW<CulledEventTag> culledTagRW,
                EnabledRefRW<InPermittedRangeTag> inPermittedRangeTagRW,
                EnabledRefRW<InViewOfCameraTag> inViewOfCameraTagRW,
                in CullSharedConfig cullSharedConfig,
                in LocalTransform transform)
            {
                float distance = 0;

                if (!cullSharedConfig.IgnoreY)
                {
                    distance = math.distancesq(transform.Position, CullPointPosition);
                }
                else
                {
                    distance = math.distancesq(transform.Position.Flat(), CullPointPosition.Flat());
                }

                CullState cullState = CullState.Culled;

                if (distance < cullSharedConfig.VisibleDistanceSQ)
                {
                    cullState = CullState.InViewOfCamera;
                }
                else if (distance < cullSharedConfig.MaxDistanceSQ)
                {
                    cullState = CullState.CloseToCamera;
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