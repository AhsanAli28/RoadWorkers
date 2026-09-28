using Spirit604.DotsCity.Simulation.Navigation;
using Spirit604.DotsCity.Simulation.TrafficPublic;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(TrafficProcessNodeGroup), OrderFirst = true)]
    [BurstCompile]
    [DisableAutoCreation]
    [DisableAutoTypeRegistration]
    [RequireMatchingQueriesForUpdate]
    public partial struct TrafficPathfindingAchieveSystem : ISystem
    {
        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            foreach (var (pathAgentComponent, trafficFixedRouteComponent, pathAgentUpdateState, entity) in
                SystemAPI.Query<RefRW<PathAgentComponent>, RefRO<TrafficFixedRouteComponent>, EnabledRefRW<PathAgentUpdateState>>()
                .WithPresent<PathAgentUpdateState>()
                .WithAll<TrafficSwitchTargetNodeRequestTag, TrafficFixedRouteTag>()
                .WithEntityAccess())
            {
                if (trafficFixedRouteComponent.ValueRO.FinishedPath)
                {
                    pathAgentComponent.ValueRW.Status = PathStatus.Complete;
                    pathAgentUpdateState.ValueRW = true;
                }
            }
        }
    }
}