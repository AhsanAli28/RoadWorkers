using Spirit604.AnimationBaker.Entities;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Pedestrian.Authoring;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Extensions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public partial class PedestrianEntitySpawnerSystem : SystemBase
    {
        public void Launch()
        {
            if (!ForceDisable)
            {
                InitializeInternal();
                InitialSpawn();
                Enabled = true;
            }

            IsInitialized = true;
            OnInitialized();
        }

        public void UpdateSpawnConfig()
        {
            pedestrianSpawnSettings = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PedestrianSpawnSettingsReference>()).GetSingleton<PedestrianSpawnSettingsReference>().Config;
        }

        public void UpdateSettingsConfig(bool includeWorld = false)
        {
            pedestrianSettings = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PedestrianSettingsReference>()).GetSingleton<PedestrianSettingsReference>().Config;

            if (includeWorld)
            {
                UpdateCreatedPedestrianSettings();
            }
        }

        public void UpdateTalkSpawnConfig()
        {
            pedestrianTalkSpawnSettings = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<TalkSpawnSettingsReference>()).GetSingleton<TalkSpawnSettingsReference>().Config;
        }

        private void InitializeInternal()
        {
            if (!IsInitialized)
            {
                InitCache();
            }

            UpdateSpawnConfig();
            UpdateTalkSpawnConfig();
            UpdateSettingsConfig();
        }

        private void UpdateCreatedPedestrianSettings()
        {
            var pedestrianQuery = EntityManager.CreateEntityQuery(typeof(PedestrianMovementSettings));

            var entities = pedestrianQuery.ToEntityArray(Allocator.TempJob);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];

                var stateComponent = EntityManager.GetComponentData<StateComponent>(entity);
                stateComponent.PreviousMovementState = MovementState.Default;

                EntityManager.SetComponentData(entity, stateComponent);

                var time = i * 16546;

                var rnd = UnityMathematicsExtension.GetRandomGen(time, i, i);

                var walkingSpeed = pedestrianSettings.Value.GetRandomWalkingSpeed(rnd);
                var runningSpeed = pedestrianSettings.Value.GetRandomRunningSpeed(rnd);

                EntityManager.SetComponentData(entity, new PedestrianMovementSettings()
                {
                    WalkingValue = walkingSpeed,
                    RunningValue = runningSpeed,
                    RotationSpeed = pedestrianSettings.Value.RotationSpeed,
                });

                EntityManager.SetComponentData(entity, new HealthComponent(pedestrianSettings.Value.Health));
                EntityManager.SetComponentData(entity, new CircleColliderComponent()
                {
                    Radius = pedestrianSettings.Value.ColliderRadius
                });
            }

            entities.Dispose();
        }

        private void InitCache()
        {
            var pedestrianSettingsBakingDataQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<MiscConversionSettingsReference>());
            var pedestrianSettingsBakingData = pedestrianSettingsBakingDataQuery.GetSingleton<MiscConversionSettingsReference>().Config.Value;

            if (pedestrianSettingsBakingData.HasRig)
            {
                var npcRigType = pedestrianSettingsBakingData.PedestrianRigType;

                switch (npcRigType)
                {
                    case NpcRigType.HybridLegacy:
                        {
                            break;
                        }
                    case NpcRigType.PureGPU:
                        {
                            var crowdTransitionProviderSystem = World.GetExistingSystemManaged<CrowdTransitionProviderSystem>();
                            crowdTransitionProviderSystem.Initialize();
                            break;
                        }
                    case NpcRigType.HybridAndGPU:
                        {
                            var crowdTransitionProviderSystem = World.GetExistingSystemManaged<CrowdTransitionProviderSystem>();
                            crowdTransitionProviderSystem.Initialize();
                            break;
                        }
                    case NpcRigType.HybridOnRequestAndGPU:
                        {
                            var crowdTransitionProviderSystem = World.GetExistingSystemManaged<CrowdTransitionProviderSystem>();
                            crowdTransitionProviderSystem.Initialize();
                            break;
                        }
                }
            }

            var citySpawnConfigQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CitySpawnConfigReference>()
                .Build(this);

            var citySpawnConfig = citySpawnConfigQuery.GetSingleton<CitySpawnConfigReference>();

            var inViewOfCameraSpawnPointGroupBuilder = new EntityQueryBuilder(Allocator.Temp)
                .WithNone<CustomSpawnerTag>()
                .WithAll<
                    NodeSettingsComponent,
                    NodeConnectionDataElement,
                    NodeCapacityComponent,
                    LocalToWorld>();

            CullComponentsExtension.InitStateQuery(ref inViewOfCameraSpawnPointGroupBuilder, CullState.InViewOfCamera);

            inViewOfCameraSpawnPointGroup = inViewOfCameraSpawnPointGroupBuilder.Build(this);

            var permittedSpawnPointGroupBuilder = new EntityQueryBuilder(Allocator.Temp)
                .WithNone<CustomSpawnerTag>()
                .WithAll<
                    NodeSettingsComponent,
                    NodeConnectionDataElement,
                    NodeCapacityComponent,
                    LocalToWorld>();

            if (citySpawnConfig.Config.Value.PedestrianSpawnStateNode != CullState.InViewOfCamera)
            {
                permittedSpawnPointGroupBuilder = permittedSpawnPointGroupBuilder.WithAny<NodeCanSpawnInVisionTag>();
            }

            CullComponentsExtension.InitStateQuery(ref permittedSpawnPointGroupBuilder, citySpawnConfig.Config.Value.PedestrianSpawnStateNode, false);

            permittedSpawnPointGroup = permittedSpawnPointGroupBuilder.Build(this);

            containerCreated = true;

            var roadStatConfig = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<RoadStatConfig>()).GetSingleton<RoadStatConfig>();

            CapacityNodeHashMap = new NativeParallelHashMap<Entity, int>(roadStatConfig.PedestrianNodeTotal + 10, Allocator.Persistent);
            talkGroupEntities = new NativeList<Entity>(Allocator.Persistent);
            spawnPositions = new NativeList<float3>(Allocator.Persistent);
            talkGroupIndexes = new NativeList<int>(Allocator.Persistent);
            tempSpawnPointEntities = new NativeHashSet<Entity>(10, Allocator.Persistent);
        }
    }
}
