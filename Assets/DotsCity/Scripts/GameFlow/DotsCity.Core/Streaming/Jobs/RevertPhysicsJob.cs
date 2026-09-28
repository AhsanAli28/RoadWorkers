using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.GraphicsIntegration;
using Unity.Transforms;

namespace Spirit604.DotsCity.Core
{
    [WithNone(typeof(InPermittedRangeTag), typeof(CulledEventTag))]
    [WithAny(typeof(InViewOfCameraTag), typeof(PreInitInCameraTag))]
    [WithAll(typeof(PhysicsWorldIndex), typeof(CullPhysicsTag))]
    [BurstCompile]
    public partial struct RevertPhysicsJob : IJobEntity
    {
        public EntityCommandBuffer CommandBuffer;

        [ReadOnly]
        public ComponentLookup<VelocityComponent> VelocityLookup;

        [ReadOnly]
        public ComponentLookup<PhysicsVelocity> PhysicsVelocityLookup;

        [ReadOnly]
        public ComponentLookup<Static> StaticLookup;

        [ReadOnly]
        public ComponentLookup<PhysicsGraphicalInterpolationBuffer> PhysicsGraphicalInterpolationLookup;

        void Execute(Entity entity)
        {
            float3 velocity = default;

            if (!StaticLookup.HasComponent(entity))
            {
                if (PhysicsVelocityLookup.HasComponent(entity))
                {
                    var physicsVelocity = new PhysicsVelocity();

                    if (VelocityLookup.HasComponent(entity))
                    {
                        velocity = VelocityLookup[entity].Value;
                        physicsVelocity.Linear = velocity;
                    }

                    CommandBuffer.SetComponent(entity, physicsVelocity);
                }
            }

            CommandBuffer.SetSharedComponent(entity, new PhysicsWorldIndex()
            {
                Value = 0
            });

            if (PhysicsGraphicalInterpolationLookup.HasComponent(entity))
            {
                CommandBuffer.AddComponent(entity, new PhysicsGraphicalSmoothing()
                {
                    CurrentVelocity = new PhysicsVelocity()
                    {
                        Linear = velocity
                    }
                });
            }
        }
    }
}