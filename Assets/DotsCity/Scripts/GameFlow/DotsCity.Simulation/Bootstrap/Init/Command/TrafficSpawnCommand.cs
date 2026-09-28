using Spirit604.DotsCity.Core.Bootstrap;
using Spirit604.DotsCity.Simulation.Traffic;
using System.Threading.Tasks;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Bootstrap
{
    public class TrafficSpawnCommand : IBootstrapCommand
    {
        private EntityQuery trafficSettingsQuery;
        private EntityQuery trafficSpawnerConfigBlobReference;
        private TrafficSpawnerSystem trafficSpawnerSystem;

        private readonly World world;
        private readonly EntityManager entityManager;

        public TrafficSpawnCommand(World world, EntityManager entityManager)
        {
            this.world = world;
            this.entityManager = entityManager;

            InitQuery();
        }

        public Task Execute()
        {
            var trafficSettings = trafficSettingsQuery.GetSingleton<TrafficGeneralSettingsReference>();
            var trafficSpawnerConfig = trafficSpawnerConfigBlobReference.GetSingleton<TrafficSpawnerConfigBlobReference>();

            if (trafficSettings.Config.Value.HasTraffic)
            {
                trafficSpawnerSystem.Initialize();
                trafficSpawnerSystem.Launch();
            }

            return Task.CompletedTask;
        }

        private void InitQuery()
        {
            trafficSettingsQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficGeneralSettingsReference>());
            trafficSpawnerConfigBlobReference = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficSpawnerConfigBlobReference>());
            trafficSpawnerSystem = world.GetExistingSystemManaged<TrafficSpawnerSystem>();
        }
    }
}