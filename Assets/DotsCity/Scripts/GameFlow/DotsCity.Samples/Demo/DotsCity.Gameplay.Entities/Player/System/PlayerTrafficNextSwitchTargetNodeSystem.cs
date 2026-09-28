using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Player
{
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class PlayerTrafficNextSwitchTargetNodeSystem : SystemBase
    {
        private PlayerSpawnTrafficControlService playerTrafficControlService;

        protected override void OnUpdate()
        {
            Entities
            .WithoutBurst()
            .WithAll<TrafficNextTrafficNodeRequestTag, TrafficPlayerControlTag>()
            .ForEach((
                Entity entity) =>
            {
                EntityManager.SetComponentEnabled<TrafficNextTrafficNodeRequestTag>(entity, false);
                playerTrafficControlService.UpdateNext();
            }).Run();
        }

        public void Initialize(PlayerSpawnTrafficControlService playerTrafficControlService)
        {
            this.playerTrafficControlService = playerTrafficControlService;
        }
    }
}