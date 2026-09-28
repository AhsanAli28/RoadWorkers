using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Player
{
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class PlayerTrafficControlInitSystem : BeginSimulationSystemBase
    {
        private PlayerSpawnTrafficControlService playerTrafficControlService;
        private Entity previousEntity;

        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
        }

        protected override void OnUpdate()
        {
            var commandBuffer = GetCommandBuffer();

            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .WithNone<TrafficInitTag>()
            .WithAll<TrafficPlayerSelected>()
            .ForEach((
                Entity entity) =>
            {
                if (EntityManager.HasComponent<TrafficTag>(previousEntity))
                {
                    commandBuffer.AddComponent<TrafficDefaultTag>(previousEntity);
                    commandBuffer.RemoveComponent<TrafficPlayerControlTag>(previousEntity);
                    commandBuffer.RemoveComponent<TrafficCustomTargetControlTag>(previousEntity);
                }

                previousEntity = entity;
                commandBuffer.AddComponent<TrafficPlayerControlTag>(entity);
                commandBuffer.AddComponent<TrafficCustomTargetControlTag>(entity);
                commandBuffer.RemoveComponent<TrafficPlayerSelected>(entity);

                if (EntityManager.HasComponent<TrafficDefaultTag>(entity))
                {
                    commandBuffer.RemoveComponent<TrafficDefaultTag>(entity);
                }

                playerTrafficControlService.BindCar(entity);

            }).Run();

            AddCommandBufferForProducer();
        }

        public void Initialize(PlayerSpawnTrafficControlService playerTrafficControlService)
        {
            this.playerTrafficControlService = playerTrafficControlService;
            Enabled = true;
        }
    }
}