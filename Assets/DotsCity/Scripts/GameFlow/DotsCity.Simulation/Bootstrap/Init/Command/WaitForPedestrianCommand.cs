using Spirit604.DotsCity.Core.Bootstrap;
using Spirit604.DotsCity.Simulation.Pedestrian;
using System.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Bootstrap
{
    public class WaitForPedestrianCommand : BootstrapCoroutineCommandBase
    {
        private EntityQuery pedestrianSpawnSettingsReference;
        private EntityQuery pedestrianSettingsQuery;
        private PedestrianEntitySpawnerSystem pedestrianEntitySpawnerSystem;

        private readonly World world;
        private readonly EntityManager entityManager;

        public WaitForPedestrianCommand(World world, EntityManager entityManager, MonoBehaviour source) : base(source)
        {
            this.world = world;
            this.entityManager = entityManager;

            InitQuery();
        }

        protected override IEnumerator InternalRoutine()
        {
            yield return new WaitWhile(() => pedestrianSettingsQuery.CalculateEntityCount() == 0);

            var pedestrianSettings = pedestrianSettingsQuery.GetSingleton<PedestrianGeneralSettingsReference>();
            var pedestrianSpawnSettings = pedestrianSpawnSettingsReference.GetSingleton<PedestrianSpawnSettingsReference>();

            if (pedestrianSettings.Config.Value.HasPedestrian && pedestrianSpawnSettings.Config.Value.MinPedestrianCount > 0)
            {
                yield return new WaitWhile(() => !pedestrianEntitySpawnerSystem.InitialSpawned);
            }
        }

        private void InitQuery()
        {
            pedestrianSpawnSettingsReference = entityManager.CreateEntityQuery(ComponentType.ReadOnly<PedestrianSpawnSettingsReference>());
            pedestrianSettingsQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<PedestrianGeneralSettingsReference>());
            pedestrianEntitySpawnerSystem = world.GetOrCreateSystemManaged<PedestrianEntitySpawnerSystem>();
        }
    }
}