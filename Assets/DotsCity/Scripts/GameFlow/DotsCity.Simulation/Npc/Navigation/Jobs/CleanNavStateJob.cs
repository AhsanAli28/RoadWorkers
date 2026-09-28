#if REESE_PATH
using Reese.Path;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Npc.Navigation
{
    [WithAll(typeof(NavAgentTag), typeof(PathBufferElement))]
    [BurstCompile]
    public partial struct CleanNavStateJob : IJobEntity
    {
        public EntityCommandBuffer CommandBuffer;

        [ReadOnly]
        public bool DisableNavigation;

        void Execute(
            Entity entity,
            ref NavAgentComponent navAgentComponent,
            ref NavAgentSteeringComponent navAgentSteeringComponent)
        {
            if (DisableNavigation)
            {
                CommandBuffer.SetComponentEnabled<AchievedNavTargetTag>(entity, false);
                CommandBuffer.SetComponentEnabled<EnabledNavigationTag>(entity, false);
            }

            navAgentComponent.HasPath = 0;
            navAgentSteeringComponent.SteeringTarget = 0;

            CommandBuffer.RemoveComponent<PathBufferElement>(entity);
        }
    }
}
#endif