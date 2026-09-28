using Spirit604.AnimationBaker.Entities;
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
    public partial struct LoadInitialHybridGPUSkinSystem : ISystem
    {
        private EntityQuery npcQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            npcQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithDisabledRW<HasSkinTag>()
                .WithPresentRW<MovementStateChangedEventTag, MaterialMeshInfo>()
                .WithAllRW<SkinAnimatorData, RenderBounds>()
                .WithAll<HybridLegacySkinTag>()
                .WithPresentRW<GPUSkinTag>()
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
                ForceDisableUpdate = true
            };

            loadSkinBoundsJob.Schedule(npcQuery);
        }
    }
}