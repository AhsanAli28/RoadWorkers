using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Spirit604.DotsCity.Hybrid.Core
{
    public struct InterpolateTransformData : IComponentData
    {
        public RigidTransform PreviousTransform;
        public PhysicsVelocity PreviousVelocity;
    }
}