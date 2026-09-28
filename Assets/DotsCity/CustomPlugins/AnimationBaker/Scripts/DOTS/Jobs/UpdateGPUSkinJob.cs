using Spirit604.AnimationBaker.Entities.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine.Rendering;

namespace Spirit604.AnimationBaker.Entities.Jobs
{
    [WithAll(typeof(GPUSkinTag), typeof(UpdateSkinTag))]
    [BurstCompile]
    public partial struct UpdateGPUSkinJob : IJobEntity
    {
        public EntityCommandBuffer CommandBuffer;

        [ReadOnly]
        public AnimationBlobReference AnimationBlobReference;

        public ComponentLookup<TakenAnimationDataComponent> TakenAnimationDataLookup;

        [NativeDisableContainerSafetyRestriction]
        public NativeHashSet<int> TakenIndexes;

        [ReadOnly]
        public NativeHashMap<SkinAnimationHash, HashToIndexData> HashToLocalData;

        [ReadOnly]
        public NativeHashSet<int> AllowDuplicateHashes;

        [ReadOnly]
        public NativeHashMap<int, BatchMaterialID> MaterialMapping;

        [ReadOnly]
        public NativeHashMap<int, BatchMeshID> MeshMapping;

        [ReadOnly]
        public float CurrentTime;

        void Execute(
            Entity entity,
            ref SkinUpdateComponent skinUpdateComponent,
            ref SkinAnimatorData skinAnimatorData,
            ref ShaderPlaybackTime shaderPlaybackTime,
            ref ShaderTargetFrameOffsetData shaderTargetFrameOffsetData,
            ref ShaderTargetFrameStepInvData shaderTargetFrameStepInvData,
            ref MaterialMeshInfo materialMeshInfo,
            EnabledRefRW<UpdateSkinTag> updateSkinTagRW)
        {
            var rnd = UnityMathematicsExtension.GetRandomGen(CurrentTime, entity.Index);

            LoadGPUSkinUtils.UpdateSkin(
                rnd,
                CurrentTime,
                ref CommandBuffer,
                entity,
                ref skinUpdateComponent,
                ref skinAnimatorData,
                ref shaderPlaybackTime,
                ref shaderTargetFrameOffsetData,
                ref shaderTargetFrameStepInvData,
                ref materialMeshInfo,
                ref updateSkinTagRW,
                in AnimationBlobReference,
                ref TakenAnimationDataLookup,
                ref TakenIndexes,
                in HashToLocalData,
                in AllowDuplicateHashes,
                in MaterialMapping,
                in MeshMapping);
        }
    }
}
