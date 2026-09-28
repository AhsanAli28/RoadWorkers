using Spirit604.DotsCity.Core.Bootstrap;
using Spirit604.DotsCity.Simulation.Traffic;
using System.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Bootstrap
{
    public class WaitForTrafficCommand : BootstrapCoroutineCommandBase
    {
        private EntityQuery trafficSettingsQuery;
        private EntityQuery trafficSpawnerConfigBlobReference;
        private TrafficSpawnerSystem trafficSpawnerSystem;

        private readonly World world;
        private readonly EntityManager entityManager;

        public WaitForTrafficCommand(World world, EntityManager entityManager, MonoBehaviour source) : base(source)
        {
            this.world = world;
            this.entityManager = entityManager;

            InitQuery();
        }

        protected override IEnumerator InternalRoutine()
        {
            yield return new WaitWhile(() => trafficSettingsQuery.CalculateEntityCount() == 0);

            var trafficSettings = trafficSettingsQuery.GetSingleton<TrafficGeneralSettingsReference>();
            var trafficSpawnerConfig = trafficSpawnerConfigBlobReference.GetSingleton<TrafficSpawnerConfigBlobReference>();

            if (trafficSettings.Config.Value.HasTraffic && trafficSpawnerConfig.Reference.Value.PreferableCount > 0)
            {
                yield return new WaitWhile(() => !trafficSpawnerSystem.InitialSpawned);
            }
        }

        private void InitQuery()
        {
            trafficSettingsQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficGeneralSettingsReference>());
            trafficSpawnerConfigBlobReference = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficSpawnerConfigBlobReference>());
            trafficSpawnerSystem = world.GetOrCreateSystemManaged<TrafficSpawnerSystem>();
        }
    }
}