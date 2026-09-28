using Unity.Entities;

namespace Spirit604.DotsCity
{
    public abstract partial class BeginPresentationSystemBase : SimpleSystemBase
    {
        protected BeginPresentationEntityCommandBufferSystem EntityCommandBufferSystem { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();

            EntityCommandBufferSystem = World.GetOrCreateSystemManaged<BeginPresentationEntityCommandBufferSystem>();
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