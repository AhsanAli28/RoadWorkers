using Spirit604.DotsCity.Core.Bootstrap;
using Spirit604.DotsCity.Simulation.Pedestrian;
using System.Threading.Tasks;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Bootstrap
{
    public class PedestrianSpawnCommand : IBootstrapCommand
    {
        private PedestrianEntitySpawnerSystem pedestrianEntitySpawnerSystem;
        private EntityQuery pedestrianSpawnSettingsReference;
        private EntityQuery pedestrianSettingsQuery;

        private readonly World world;
        private readonly EntityManager entityManager;

        public PedestrianSpawnCommand(World world, EntityManager entityManager)
        {
            this.world = world;
            this.entityManager = entityManager;

            InitQuery();
        }

        public Task Execute()
        {
            var pedestrianSettings = pedestrianSettingsQuery.GetSingleton<PedestrianGeneralSettingsReference>();
            var pedestrianSpawnSettings = pedestrianSpawnSettingsReference.GetSingleton<PedestrianSpawnSettingsReference>();

            if (pedestrianSettings.Config.Value.HasPedestrian)
            {
                pedestrianEntitySpawnerSystem.Launch();
            }

            return Task.CompletedTask;
        }

        private void InitQuery()
        {
            pedestrianSpawnSettingsReference = entityManager.CreateEntityQuery(ComponentType.ReadOnly<PedestrianSpawnSettingsReference>());
            pedestrianSettingsQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<PedestrianGeneralSettingsReference>());
            pedestrianEntitySpawnerSystem = world.GetOrCreateSystemManaged<PedestrianEntitySpawnerSystem>();
        }
    }
}