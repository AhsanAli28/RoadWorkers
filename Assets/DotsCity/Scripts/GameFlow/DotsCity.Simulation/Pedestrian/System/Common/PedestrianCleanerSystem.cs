using Spirit604.DotsCity.Core;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    public partial class PedestrianCleanerSystem : BeginSimulationSystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();

            Enabled = false;
        }

        protected override void OnUpdate()
        {
            var commandBuffer = GetParallelCommandBuffer();

            var job = Entities
                .WithBurst()
                .WithNone<PooledEventTag>()
                .WithAll<PedestrianMovementSettings, PoolableTag>()
                .ForEach((
                    Entity entity,
                    int entityInQueryIndex) =>
                {
                    PoolEntityUtils.DestroyEntity(ref commandBuffer, entityInQueryIndex, entity);
                }).ScheduleParallel(this.Dependency);

            AddCommandBufferForProducer();

            job.Complete();

            Enabled = false;
        }

        public void Clear()
        {
            Enabled = true;
        }
    }
}
