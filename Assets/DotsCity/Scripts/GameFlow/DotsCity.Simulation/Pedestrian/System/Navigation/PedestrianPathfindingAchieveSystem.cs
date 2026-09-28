using Spirit604.DotsCity.Simulation.Navigation;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(MainThreadEventGroup), OrderFirst = true)]
    [BurstCompile]
    [DisableAutoCreation]
    [DisableAutoTypeRegistration]
    [RequireMatchingQueriesForUpdate]
    public partial struct PedestrianPathfindingAchieveSystem : ISystem
    {
        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            foreach (var (pathAgentComponent, pathAgentUpdateState, nodeSequenceBuffer) in
                SystemAPI.Query<RefRW<PathAgentComponent>, EnabledRefRW<PathAgentUpdateState>, DynamicBuffer<NodeSequenceElement>>()
                .WithPresent<PathAgentUpdateState>()
                .WithAll<NodeSequenceElement, ReachTargetTag, CustomReachTargetTag>())
            {
                if (nodeSequenceBuffer.Length <= 0)
                {
                    pathAgentComponent.ValueRW.Status = PathStatus.Complete;
                    pathAgentUpdateState.ValueRW = true;
                }
            }
        }
    }
}