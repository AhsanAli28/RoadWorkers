using Spirit604.AnimationBaker.Entities;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(EarlyEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct LoadGPUSkinSystem : ISystem
    {
        private EntityQuery npcQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            npcQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithNone<DisableUnloadSkinTag>()
                .WithDisabled<InPermittedRangeTag>()
                .WithDisabledRW<HasSkinTag, MaterialMeshInfo>()
                .WithPresentRW<MovementStateChangedEventTag>()
                .WithAllRW<SkinAnimatorData, RenderBounds>()
                .WithAllRW<GPUSkinTag>()
                .WithAll<InViewOfCameraTag>()
                .Build(ref state);

            state.RequireForUpdate(npcQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var loadSkinBoundsJob = new LoadSkinBoundsJob()
            {
                CrowdSkinProvider = SystemAPI.GetSingleton<CrowdSkinProviderSystem.Singleton>(),
                Timestamp = (float)SystemAPI.Time.ElapsedTime,
            };

            loadSkinBoundsJob.Schedule(npcQuery);
        }
    }
}