#if RUKHANKA
using Spirit604.DotsCity.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(SimulationGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct RukhankaRagdollPoolSystem : ISystem
    {
        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            new RagdollActivateJob()
            {
                PooledEventTagLookup = SystemAPI.GetComponentLookup<PooledEventTag>(false),
                Time = (float)SystemAPI.Time.ElapsedTime,

            }.Schedule();
        }

        [BurstCompile]
        [WithNone(typeof(Simulate), typeof(RukhankaRagdollActivate))]
        public partial struct RagdollActivateJob : IJobEntity
        {
            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<PooledEventTag> PooledEventTagLookup;

            [ReadOnly]
            public float Time;

            void Execute(
                Entity entity,
                in RukhankaRagdollData rukhankaRagdollData)
            {
                if (Time >= rukhankaRagdollData.DeactivateTime)
                {
                    PooledEventTagLookup.SetComponentEnabled(entity, true);
                }
            }
        }
    }
}
#endif