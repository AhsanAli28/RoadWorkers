#if RUKHANKA
using Rukhanka;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(EarlyJobGroup), OrderFirst = true)]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct RukhankaRagdollInitialSyncAnimSystem : ISystem
    {
        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            new RagdollActivateJob()
            {
                SimulateLookup = SystemAPI.GetComponentLookup<Simulate>(false),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(false),

            }.Schedule();
        }

        [BurstCompile]
        [WithDisabled(typeof(RukhankaRagdollActivate))]
        [WithAll(typeof(Simulate))]
        public partial struct RagdollActivateJob : IJobEntity
        {
            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<Simulate> SimulateLookup;

            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<LocalTransform> LocalTransformLookup;

            void Execute(
                Entity entity,
                EnabledRefRW<RukhankaRagdollActivate> rukhankaRagdollActivate,
                in DynamicBuffer<RukhankaRagdollBuffer> ragdollBuffer)
            {
                var transform = LocalTransformLookup[entity];

                rukhankaRagdollActivate.ValueRW = true;

                SimulateLookup.SetComponentEnabled(entity, false);

                for (int i = 0; i < ragdollBuffer.Length; i++)
                {
                    var ragdollBufferData = ragdollBuffer[i];

                    var boneEntity = ragdollBufferData.Entity;
                    var relativeParentTransform = ragdollBufferData.RelativeParentTransform;

                    var newWorldPos = math.transform(new RigidTransform(transform.Rotation, transform.Position), relativeParentTransform.pos);
                    var newWorldRotation = math.mul(transform.Rotation, relativeParentTransform.rot);

                    if (ragdollBufferData.Joint) continue;

                    LocalTransformLookup[boneEntity] = LocalTransform.FromPositionRotation(newWorldPos, newWorldRotation);
                }
            }
        }
    }
}
#endif