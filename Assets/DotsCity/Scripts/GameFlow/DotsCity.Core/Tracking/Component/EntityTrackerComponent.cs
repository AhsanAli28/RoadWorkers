using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Core
{
    public struct EntityTrackerComponent : IComponentData
    {
        public Entity LinkedEntity;
        public bool TrackOnlyInView;
        public bool HasOffset;
        public float3 Offset;
    }
}