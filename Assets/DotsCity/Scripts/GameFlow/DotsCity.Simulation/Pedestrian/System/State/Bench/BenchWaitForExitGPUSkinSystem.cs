using Spirit604.AnimationBaker.Entities;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(LateEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct BenchWaitForExitGPUSkinSystem : ISystem
    {
        private NativeArray<int> movementsHashes;
        private EntityQuery updateGroup;

        void ISystem.OnCreate(ref SystemState state)
        {
            movementsHashes = PedestrianGPUAnimationsConstans.GetMovementHashes(Allocator.Persistent);

            updateGroup = SystemAPI.QueryBuilder()
                .WithDisabled<HasAnimTransitionTag>()
                .WithAll<BenchWaitForExitTag, GPUSkinTag, HasSkinTag>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            movementsHashes.Dispose();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var gpuWaitForExitJob = new GPUWaitForExitJob()
            {
                MovementsHashes = movementsHashes,
            };

            gpuWaitForExitJob.Schedule();
        }

        [WithDisabled(typeof(HasAnimTransitionTag))]
        [WithAll(typeof(BenchWaitForExitTag), typeof(GPUSkinTag), typeof(HasSkinTag))]
        [BurstCompile]
        public partial struct GPUWaitForExitJob : IJobEntity
        {
            [ReadOnly]
            public NativeArray<int> MovementsHashes;

            void Execute(
                ref SeatSlotLinkedComponent seatSlotLinkedComponent,
                in SkinAnimatorData skinAnimatorData,
                EnabledRefRW<BenchWaitForExitTag> benchWaitForExitTagRW)
            {
                for (int i = 0; i < MovementsHashes.Length; i++)
                {
                    if (skinAnimatorData.CurrentAnimationHash == MovementsHashes[i])
                    {
                        seatSlotLinkedComponent.Exited = true;
                        benchWaitForExitTagRW.ValueRW = false;
                        break;
                    }
                }
            }
        }
    }
}
