using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Core
{
    [BurstCompile]
    public partial struct DestroyEntityJob : IJobEntity
    {
        public EntityCommandBuffer commandBuffer;

        void Execute(Entity entity)
        {
            commandBuffer.DestroyEntity(entity);
        }
    }
}