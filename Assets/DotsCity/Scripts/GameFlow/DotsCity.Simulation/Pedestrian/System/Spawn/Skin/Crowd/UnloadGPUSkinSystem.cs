using Spirit604.AnimationBaker.Entities;
using Spirit604.DotsCity.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(LateEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct UnloadGPUSkinSystem : ISystem
    {
        private EntityQuery unloadQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            unloadQuery = SystemAPI.QueryBuilder()
                .WithNone<InViewOfCameraTag, DisableUnloadSkinTag>()
                .WithAllRW<HasSkinTag, MaterialMeshInfo>()
                .WithAll<GPUSkinTag>()
                .Build();

            state.RequireForUpdate(unloadQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var unloadGPUSkinJob = new UnloadGPUSkinJob()
            {
            };

            unloadGPUSkinJob.Schedule();
        }

        [WithNone(typeof(InViewOfCameraTag), typeof(DisableUnloadSkinTag))]
        [WithAll(typeof(GPUSkinTag))]
        [BurstCompile]
        public partial struct UnloadGPUSkinJob : IJobEntity
        {
            void Execute(
                EnabledRefRW<MaterialMeshInfo> materialMeshInfoRW,
                EnabledRefRW<HasSkinTag> hasSkinTagRW)
            {
                materialMeshInfoRW.ValueRW = false;
                hasSkinTagRW.ValueRW = false;
            }
        }
    }
}