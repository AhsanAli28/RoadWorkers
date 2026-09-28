#if RUKHANKA
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [InternalBufferCapacity(0)]
    public struct RukhankaRagdollBuffer : IBufferElementData
    {
        public Entity Entity;
        public bool Joint;
        public RigidTransform RelativeParentTransform;
    }

    public struct RukhankaRagdollPrefabContainer : IBufferElementData
    {
        public Entity PrefabEntity;
    }

    public struct RukhankaRagdollActivate : IComponentData, IEnableableComponent
    {
    }

    public struct RukhankaRagdollData : IComponentData
    {
        public float DeactivateTime;
    }

}
#endif