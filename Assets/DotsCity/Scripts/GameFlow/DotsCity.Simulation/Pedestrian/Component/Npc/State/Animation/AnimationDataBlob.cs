using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct AnimationDataBlob
    {
        public BlobArray<AnimationState> LegacyKeys;
        public BlobArray<LegacyAnimationDataComponent> LegacyData;

        public BlobArray<AnimationState> GPUKeys;
        public BlobArray<GPUAnimationDataComponent> GPUData;

        public BlobArray<MovementState> MovementKeys;
        public BlobArray<AnimationState> MovementValues;
    }

    public struct AnimationDataBlobReference : IComponentData
    {
        public BlobAssetReference<AnimationDataBlob> Config;
    }
}