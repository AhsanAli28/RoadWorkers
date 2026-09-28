using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Common
{
    [UpdateInGroup(typeof(FixedStepGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial class SimpleRouteFollowerSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float dt = SystemAPI.Time.DeltaTime;

            Entities
            .WithBurst()
            .ForEach((
                Entity entity,
                DynamicBuffer<SimpleRouteElement> buffer,
                ref LocalTransform transform,
                ref SimpleRouteFollowerComponent routeFollower,
                in SimpleRouteFollowerSettingsComponent routeFollowerSettings) =>
            {
                int nodeIndex = routeFollower.NodeIndex;

                float distance = math.distance(transform.Position, buffer[nodeIndex].Position);

                var movementDirection = math.normalize(buffer[nodeIndex].Position - transform.Position);

                transform.Position += movementDirection * routeFollowerSettings.MovementSpeed * dt;

                if (distance < routeFollowerSettings.AchieveDistance)
                {
                    routeFollower.NodeIndex = ++nodeIndex % buffer.Length;
                }

            }).ScheduleParallel();
        }
    }
}