using Unity.Entities;

namespace Spirit604.DotsCity
{
    public abstract partial class EndSimulationSystemBase : SimpleSystemBase
    {
        protected EndSimulationEntityCommandBufferSystem EntityCommandBufferSystem { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();

            EntityCommandBufferSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected virtual void AddCommandBufferForProducer()
        {
            EntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }

        protected EntityCommandBuffer.ParallelWriter GetParallelCommandBuffer()
        {
            var commandBuffer = EntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

            return commandBuffer;
        }

        protected EntityCommandBuffer GetCommandBuffer()
        {
            var commandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();

            return commandBuffer;
        }
    }
}