using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [RequireMatchingQueriesForUpdate]
    [UpdateAfter(typeof(StopTalkStateSystem))]
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class DisableTalkStateSystem : BeginSimulationSystemBase
    {
        protected override void OnUpdate()
        {
            var commandBuffer = GetCommandBuffer();

            Entities
            .WithoutBurst()
            .WithNone<TalkComponent>()
            .WithAll<TalkAreaComponent>()
            .ForEach((
                Entity entity) =>
            {
                commandBuffer.RemoveComponent<TalkAreaComponent>(entity);
            }).Run();

            AddCommandBufferForProducer();
        }
    }
}