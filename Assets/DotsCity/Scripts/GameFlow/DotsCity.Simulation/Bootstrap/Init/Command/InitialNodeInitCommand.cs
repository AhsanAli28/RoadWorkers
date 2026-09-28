using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Core.Bootstrap;
using Spirit604.DotsCity.Core.Initialization;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Bootstrap
{
    public class InitialNodeInitCommand : BootstrapCoroutineCommandBase
    {
        private const int AwaitCullCount = 5;
        private const float CullPointAwaitTime = 1f;
        private const float MaxAwaitTime = 2f;

        private PedestrianNodeInitializerSystem pedestrianNodeInitializerSystem;
        private TrafficNodeInitializer trafficNodeInitializer;

        private EntityQuery cullPointQuery;
        private EntityQuery cullEntitiesQuery;
        private EntityQuery trafficSettingsQuery;
        private EntityQuery trafficSpawnerConfigBlobReference;
        private EntityQuery pedestrianSpawnSettingsReference;
        private EntityQuery pedestrianSettingsQuery;
        private EntityQuery roadStatConfigQuery;

        private readonly World world;
        private readonly EntityManager entityManager;

        public InitialNodeInitCommand(World world, EntityManager entityManager, MonoBehaviour source) : base(source)
        {
            this.world = world;
            this.entityManager = entityManager;

            InitQuery();
        }

        protected override IEnumerator InternalRoutine()
        {
            world.GetOrCreateSystemManaged<CullInitializerSystem>().Launch();

            var skipTime = Time.time + CullPointAwaitTime;

            yield return new WaitWhile(() => cullPointQuery.CalculateEntityCount() == 0 && Time.time < skipTime);

            if (cullPointQuery.CalculateEntityCount() == 0)
            {
                Debug.Log($"Cull point entity not found. Make sure that you have created cull point entity by adding a new gameobject to the scene with the CullPointRuntimeAuthoring component.");
            }

            skipTime = Time.time + MaxAwaitTime;

            yield return new WaitWhile(() => cullEntitiesQuery.CalculateEntityCount() < AwaitCullCount && Time.time < skipTime);

            var pedestrianSettings = pedestrianSettingsQuery.GetSingleton<PedestrianGeneralSettingsReference>();
            var pedestrianSpawnSettings = pedestrianSpawnSettingsReference.GetSingleton<PedestrianSpawnSettingsReference>();

            if (pedestrianSettings.Config.Value.HasPedestrian)
                pedestrianNodeInitializerSystem.Launch();

            var roadStatConfig = roadStatConfigQuery.GetSingleton<RoadStatConfig>();

            if (pedestrianSettings.Config.Value.HasPedestrian &&
                pedestrianSpawnSettings.Config.Value.MinPedestrianCount > 0 &&
                roadStatConfig.PedestrianNodeTotal >= PedestrianNodeInitializerSystem.MIN_INIT_COUNT)
            {
                skipTime = Time.time + MaxAwaitTime;
                yield return new WaitWhile(() => !pedestrianNodeInitializerSystem.IsInitialized && Time.time < skipTime);

                if (!pedestrianNodeInitializerSystem.IsInitialized)
                {
                    Debug.Log($"PedestrianNode Initializer skipped. Make sure that you have added pedestrian nodes to the subscene, adjusted the culling distance & placed the spawn point not too far from the roads. Or disable pedestrians in the general settings.");
                }
            }

            var trafficSettings = trafficSettingsQuery.GetSingleton<TrafficGeneralSettingsReference>();
            var trafficSpawnerConfig = trafficSpawnerConfigBlobReference.GetSingleton<TrafficSpawnerConfigBlobReference>();

            if (trafficSettings.Config.Value.HasTraffic)
                trafficNodeInitializer.Launch();

            if (trafficSettings.Config.Value.HasTraffic &&
                trafficSpawnerConfig.Reference.Value.PreferableCount > 0 &&
                roadStatConfig.TrafficNodeTotal >= TrafficNodeInitializer.MIN_INIT_COUNT)
            {
                skipTime = Time.time + MaxAwaitTime;

                yield return new WaitWhile(() => !trafficNodeInitializer.IsInitialized && Time.time < skipTime);

                if (!trafficNodeInitializer.IsInitialized)
                {
                    Debug.Log($"TrafficNode Initializer skipped. Make sure that you have added traffic nodes to the subscene, adjusted the culling distance & placed the spawn point not too far from the roads. Or disable traffic in the general settings.");
                }
            }
        }

        private void InitQuery()
        {
            cullPointQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CullPointTag>()
                .Build(entityManager);

            cullEntitiesQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithNone<CulledEventTag>()
                .WithAny<InViewOfCameraTag, InPermittedRangeTag>()
                .WithAll<CullStateComponent>()
                .Build(entityManager);

            pedestrianNodeInitializerSystem = world.GetOrCreateSystemManaged<PedestrianNodeInitializerSystem>();
            trafficNodeInitializer = world.GetOrCreateSystemManaged<TrafficNodeInitializer>();

            trafficSettingsQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficGeneralSettingsReference>());
            trafficSpawnerConfigBlobReference = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficSpawnerConfigBlobReference>());

            pedestrianSpawnSettingsReference = entityManager.CreateEntityQuery(ComponentType.ReadOnly<PedestrianSpawnSettingsReference>());
            pedestrianSettingsQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<PedestrianGeneralSettingsReference>());
            roadStatConfigQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RoadStatConfig>());
        }
    }
}