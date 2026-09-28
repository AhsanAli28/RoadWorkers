#if RUKHANKA
using Rukhanka;
using Spirit604.DotsCity.Simulation.Npc;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(LateSimulationGroup), OrderLast = true)]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct RukhankaRagdollActivateSystem : ISystem
    {
        private const float RagdollDuration = 5f;
        private const float ApplyImpulseMultiplier = 40f;

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            new RagdollActivateJob()
            {
                SimulateLookup = SystemAPI.GetComponentLookup<Simulate>(false),
                PhysicsVelocityLookup = SystemAPI.GetComponentLookup<PhysicsVelocity>(false),
                PhysicsMassLookup = SystemAPI.GetComponentLookup<PhysicsMass>(true),
                Time = (float)SystemAPI.Time.ElapsedTime,

            }.Schedule();
        }

        [BurstCompile]
        [WithNone(typeof(Simulate))]
        public partial struct RagdollActivateJob : IJobEntity
        {
            public ComponentLookup<Simulate> SimulateLookup;
            public ComponentLookup<PhysicsVelocity> PhysicsVelocityLookup;

            [ReadOnly]
            public ComponentLookup<PhysicsMass> PhysicsMassLookup;

            [ReadOnly]
            public float Time;

            void Execute(
                Entity entity,
                ref DynamicBuffer<AnimatorControllerLayerComponent> layerBuffer,
                ref DynamicBuffer<AnimationToProcessComponent> animationToProcessComponent,
                ref RukhankaRagdollData rukhankaRagdollData,
                EnabledRefRW<RukhankaRagdollActivate> rukhankaRagdollActivate,
                in RagdollComponent ragdollComponent,
                in DynamicBuffer<RukhankaRagdollBuffer> ragdollBuffer,
                in LocalTransform localTransform)
            {
                rukhankaRagdollActivate.ValueRW = false;

                for (int i = 0; i < layerBuffer.Length; i++)
                {
                    var layerData = layerBuffer[i];
                    layerData.weight = 0;
                    layerBuffer[i] = layerData;
                }

                animationToProcessComponent.Clear();

                rukhankaRagdollData.DeactivateTime = Time + RagdollDuration;

                for (int i = 0; i < ragdollBuffer.Length; i++)
                {
                    var ragdollBufferData = ragdollBuffer[i];

                    var boneEntity = ragdollBufferData.Entity;

                    SimulateLookup.SetComponentEnabled(boneEntity, true);
                }

                var middleSpineEntity = ragdollBuffer[1].Entity;

                var velocity = PhysicsVelocityLookup[middleSpineEntity];
                var mass = PhysicsMassLookup[middleSpineEntity];
                velocity.ApplyImpulse(in mass, localTransform.Position, localTransform.Rotation, ragdollComponent.ForceDirection * ApplyImpulseMultiplier, ragdollComponent.Position);
                PhysicsVelocityLookup[middleSpineEntity] = velocity;
            }
        }
    }
}
#endif