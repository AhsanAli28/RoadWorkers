using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.NavMesh;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Config;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic.Authoring;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.GraphicsIntegration;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public partial class TrafficSpawnerSystem : EndInitSystemBase
    {
        private GeneralSettingDataSimulation generalSettings;
        private TrafficSettings trafficSettings;

        public void Initialize(
            GeneralSettingDataSimulation generalSettings,
            TrafficSettings trafficSettings)
        {
            this.generalSettings = generalSettings;
            this.trafficSettings = trafficSettings;
        }

        public void Initialize()
        {
            if (ForceDisable)
                return;

            SetTrafficSettings();

            if (!IsInitialized)
            {
                FirstInit();
            }

            InitPrefabs();

            OnConfigInitialized();
        }

        private void FirstInit()
        {
            var citySpawnConfigQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CitySpawnConfigReference>()
                .Build(this);

            var citySpawnConfig = citySpawnConfigQuery.GetSingleton<CitySpawnConfigReference>();

            var permittedTrafficNodeGroupBuilder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<
                    TrafficNodeComponent,
                    TrafficNodeSettingsComponent,
                    TrafficNodeCapacityComponent,
                    TrafficNodeAvailableTag,
                    LocalToWorld>();

            CullComponentsExtension.InitStateQuery(ref permittedTrafficNodeGroupBuilder, citySpawnConfig.Config.Value.TrafficSpawnStateNode);

            permittedTrafficNodeGroup = permittedTrafficNodeGroupBuilder.Build(this);

            var inViewOfCameraTrafficNodeGroupBuilder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<
                    TrafficNodeComponent,
                    TrafficNodeSettingsComponent,
                    TrafficNodeCapacityComponent,
                    TrafficNodeAvailableTag,
                    LocalToWorld>();

            CullComponentsExtension.InitStateQuery(ref inViewOfCameraTrafficNodeGroupBuilder, CullState.InViewOfCamera);

            inViewOfCameraTrafficNodeGroup = inViewOfCameraTrafficNodeGroupBuilder.Build(this);

            UpdateSpawnConfig();
        }

        private void InitPrefabs()
        {
            trafficPrefabGroup.SetSharedComponentFilter(new TrafficPrefabSort()
            {
                TrafficEntityType = entityType
            });

            var entities = trafficPrefabGroup.ToComponentDataArray<TrafficPrefabData>(Allocator.TempJob);

            if (entities.Length == 0)
            {
                UnityEngine.Debug.LogError($"TrafficSpawner. Preset with entity type {entityType} not found. Make sure you have assigned a preset to the TrafficCarEntityPoolBakerRef on the main scene & subscene.");
            }

            var trafficSettings = EntityManager.CreateEntityQuery(typeof(TrafficSettingsConfigBlobReference)).GetSingleton<TrafficSettingsConfigBlobReference>();
            prefabEntities = new NativeParallelHashMap<int, PrefabEntityData>(entities.Length, Allocator.Persistent);

            for (int i = 0; i < entities.Length; i++)
            {
                var prefabEntity = entities[i].PrefabEntity;

                var carModelComponent = EntityManager.GetComponentData<CarModelComponent>(prefabEntity);
                var carModel = carModelComponent.Value;
                var weight = entities[i].Weight;

                if (trafficSettings.Reference.Value.HasNavMeshObstacle && !EntityManager.HasComponent<NavMeshObstacleData>(prefabEntity))
                {
                    UnityEngine.Debug.Log($"TrafficSpawnerSystem.Initialize. HasNavMeshObstacle is enabled, but CarModel {carModelComponent.Value} doesn't have NavMeshObstacleAuthoring component.");
                }

                spawnSettings.SumSpawnWeight += weight;

                if (!prefabEntities.ContainsKey(carModel))
                {
                    var boundsComponent = EntityManager.GetComponentData<BoundsComponent>(prefabEntity);
                    var trafficTypeComponent = EntityManager.GetComponentData<TrafficTypeComponent>(prefabEntity);
                    var trafficSettingsComponent = EntityManager.GetComponentData<TrafficSettingsComponent>(prefabEntity);

                    bool availableForSpawnByDefault = EntityManager.HasComponent<TrafficDefaultTag>(prefabEntity);
                    bool interpolation = EntityManager.HasComponent<PhysicsGraphicalInterpolationBuffer>(prefabEntity);

                    var prefabData = new PrefabEntityData(
                        prefabEntity,
                        boundsComponent,
                        trafficTypeComponent.TrafficGroup,
                        weight,
                        availableForSpawnByDefault,
                        interpolation,
                        trafficSettingsComponent.OffsetY);

                    prefabEntities.Add(carModel, prefabData);
                }
                else
                {
                    UnityEngine.Debug.LogError($"TrafficSpawner. Car '{carModel}' already added to spawner dictionary! Make sure that the traffic pool doesn't have any duplicate ID vehicle entries.");
                }
            }

            entities.Dispose();
        }

        private void SetTrafficSettings()
        {
            entityType = trafficSettings.EntityType;

            if (generalSettings.SimulationType == SimulationType.NoPhysics && entityType != EntityType.HybridEntityMonoPhysics)
            {
                if (entityType != EntityType.PureEntityNoPhysics)
                {
                    UnityEngine.Debug.Log($"TrafficSpawnerSystem.Initialize. Traffic entity type '{entityType}' is reset to '{EntityType.PureEntityNoPhysics}', make sure have you physics enabled in the General Settings or set 'PureEntityNoPhysics' in the Traffic settings to hide this message.");
                    entityType = EntityType.PureEntityNoPhysics;
                }
            }
        }

        public void UpdateSpawnConfig()
        {
            spawnerConfig = SystemAPI.GetSingleton<TrafficSpawnerConfigBlobReference>();

            if (!TempSpawnCarHashMap.IsCreated)
            {
                TempSpawnCarHashMap = new NativeParallelMultiHashMap<int, SpawnJob.CarSpawnInfo>(spawnerConfig.Reference.Value.PreferableCount, Allocator.Persistent);
            }
            else
            {
                if (TempSpawnCarHashMap.Capacity < spawnerConfig.Reference.Value.PreferableCount)
                    TempSpawnCarHashMap.Capacity = spawnerConfig.Reference.Value.PreferableCount;
            }
        }
    }
}
