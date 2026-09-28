using System.Runtime.CompilerServices;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public static class TrafficCollisionUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddCollisionState(
            ref EntityCommandBuffer commandBuffer,
            Entity entity,
            ref CarCollisionComponent carCollisionComponent,
            ref TrafficStateComponent trafficStateComponent,
            float elapsedTime)
        {
            var added = TrafficStateExtension.AddIdleState<TrafficCollidedTag>(ref commandBuffer, entity, ref trafficStateComponent, TrafficIdleState.Collided);

            if (added)
            {
                carCollisionComponent.CollisionTime = elapsedTime;
            }
        }
    }
}