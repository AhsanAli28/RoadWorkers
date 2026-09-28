using Spirit604.AnimationBaker.Entities;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [WithDisabled(typeof(HasSkinTag))]
    [BurstCompile]
    public partial struct LoadSkinBoundsJob : IJobEntity
    {
        private const float LoadSkinFrequency = 0.4f;

        [ReadOnly]
        public CrowdSkinProviderSystem.Singleton CrowdSkinProvider;

        [ReadOnly]
        public float Timestamp;

        [ReadOnly]
        public bool ForceDisableUpdate;

        void Execute(
            ref SkinAnimatorData skinAnimatorData,
            ref RenderBounds renderBounds,
            EnabledRefRW<HasSkinTag> hasSkinTagRW,
            EnabledRefRW<MaterialMeshInfo> materialMeshInfoRW,
            EnabledRefRW<GPUSkinTag> gPUSkinTagRW,
            EnabledRefRW<MovementStateChangedEventTag> movementStateChangedEventTagRW)
        {
            bool shouldLoad = (Timestamp - skinAnimatorData.LoadSkinTimestamp) >= LoadSkinFrequency;

            if (shouldLoad)
            {
                skinAnimatorData.LoadSkinTimestamp = Timestamp;

                hasSkinTagRW.ValueRW = true;

                if (!ForceDisableUpdate)
                {
                    gPUSkinTagRW.ValueRW = true;
                    materialMeshInfoRW.ValueRW = true;
                    movementStateChangedEventTagRW.ValueRW = true;
                }

                renderBounds.Value = CrowdSkinProvider.GetSkinBounds(skinAnimatorData.SkinIndex).Value;
            }
        }
    }
}