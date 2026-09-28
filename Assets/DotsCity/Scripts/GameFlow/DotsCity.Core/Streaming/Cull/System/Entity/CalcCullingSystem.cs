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
    public partial struct CalcCullingSystem : ISystem
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
                .WithNone<PooledEventTag, CullSharedConfig, CullCameraSharedConfig>()
                .WithAbsent<PreInitInCameraTag>()
                .WithAllRW<CullStateComponent>()
                .WithPresentRW<CulledEventTag, InPermittedRangeTag>()
                .WithPresentRW<InViewOfCameraTag>()
                .WithAll<LocalTransform>()
                .Build();

            state.RequireForUpdate(cullPointGroup);
            state.RequireForUpdate<CullSystemConfigReference>();
            state.Enabled = false;
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();

            var calcCullJob = new CalcCullJob()
            {
                CullPointPosition = cullPointGroup.GetSingleton<LocalTransform>().Position,
                Config = SystemAPI.GetSingleton<CullSystemConfigReference>()
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
            public CullSystemConfigReference Config;

            void Execute(
                ref CullStateComponent cullComponent,
                EnabledRefRW<CulledEventTag> culledTagRW,
                EnabledRefRW<InPermittedRangeTag> inPermittedRangeTagRW,
                EnabledRefRW<InViewOfCameraTag> inViewOfCameraTagRW,
                in LocalTransform transform)
            {
                float distance = 0;

                if (!Config.Config.Value.IgnoreY)
                {
                    distance = math.distancesq(transform.Position, CullPointPosition);
                }
                else
                {
                    distance = math.distancesq(transform.Position.Flat(), CullPointPosition.Flat());
                }

                CullState cullState = CullState.Culled;

                if (distance < Config.Config.Value.VisibleDistanceSQ)
                {
                    cullState = CullState.InViewOfCamera;
                }
                else if (distance < Config.Config.Value.MaxDistanceSQ)
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