using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Core.Bootstrap;
using Spirit604.DotsCity.Simulation.Pedestrian;
using System.Threading.Tasks;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Bootstrap
{
    public class PostSpawnPedestrianCommand : IBootstrapCommand
    {
        private readonly WorldUnmanaged world;
        private readonly EntityManager entityManager;
        private EntityQuery pedestrianGeneralSettingsReference;

        public PostSpawnPedestrianCommand(WorldUnmanaged world, EntityManager entityManager)
        {
            this.world = world;
            this.entityManager = entityManager;

            InitQuery();
        }

        public Task Execute()
        {
            var pedGeneralConfig = pedestrianGeneralSettingsReference.GetSingleton<PedestrianGeneralSettingsReference>();

            if (pedGeneralConfig.Config.Value.TriggerSupport)
            {
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<AreaTriggerSystem, LateSimulationGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TriggerImpactSystem, MainThreadEventGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<AreaTriggerPlaybackSystem, MainThreadEventPlaybackGroup>();
            }

            return Task.CompletedTask;
        }

        private void InitQuery()
        {
            pedestrianGeneralSettingsReference = entityManager.CreateEntityQuery(ComponentType.ReadOnly<PedestrianGeneralSettingsReference>());
        }
    }
}