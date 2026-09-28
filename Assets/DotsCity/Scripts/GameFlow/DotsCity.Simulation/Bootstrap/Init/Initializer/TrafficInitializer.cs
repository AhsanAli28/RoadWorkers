using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Core.Initialization;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.NavMesh;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Car.Custom;
using Spirit604.DotsCity.Simulation.Car.Sound;
using Spirit604.DotsCity.Simulation.Config;
using Spirit604.DotsCity.Simulation.Factory.Traffic;
using Spirit604.DotsCity.Simulation.Npc;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Sound;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.DotsCity.Simulation.Traffic.Authoring;
using Spirit604.DotsCity.Simulation.Traffic.Sound;
using Spirit604.DotsCity.Simulation.TrafficArea;
using Spirit604.DotsCity.Simulation.TrafficPublic;
using Spirit604.DotsCity.Simulation.Train;
using Spirit604.Gameplay.Road;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Initialization
{
    public class TrafficInitializer : InitializerBase
    {
        private World world;

        private GeneralSettingDataSimulation generalSettings;
        private TrafficCarPoolGlobal trafficPoolGlobal;
        private TrafficSettings trafficSettings;
        private NavMeshObstacleFactory navMeshObstacleFactory;

        [InjectWrapper]
        public void Construct(
            GeneralSettingDataSimulation generalSettings,
            TrafficCarPoolGlobal trafficPoolGlobal,
            TrafficSettings trafficSettings,
            NavMeshObstacleFactory navMeshObstacleFactory)
        {
            this.generalSettings = generalSettings;
            this.trafficPoolGlobal = trafficPoolGlobal;
            this.trafficSettings = trafficSettings;
            this.navMeshObstacleFactory = navMeshObstacleFactory;
        }

        public override void Initialize()
        {
            if (!generalSettings.HasTraffic)
            {
                DisableSystems();
                return;
            }

            InitializeInternal();
            InitSystems();
        }

        private void InitializeInternal()
        {
            world = World.DefaultGameObjectInjectionWorld;

            var nasNavObstacle = generalSettings.NavigationSupport && trafficSettings.TrafficSettingsConfig.HasNavObstacle;

            if (nasNavObstacle)
            {
                DefaultWorldUtils.CreateAndAddSystemManaged<NavMeshObstacleLoader, MainThreadInitGroup>().Initialize(navMeshObstacleFactory);
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<NavMeshObstacleResetLoadSystem, PreEarlyJobGroup>();
            }

            InitHitReaction();

            var trafficSpawnerSystem = world.GetExistingSystemManaged<TrafficSpawnerSystem>();

            if (trafficSpawnerSystem != null)
                trafficSpawnerSystem.Initialize(generalSettings, trafficSettings);
        }

        private void InitHitReaction()
        {
            var entityType = trafficSettings.EntityType;
            var poolData = trafficPoolGlobal.GetPoolData(entityType);

            if (poolData == null)
            {
                UnityEngine.Debug.Log($"TrafficInitializer. PoolData {entityType} is null");
                return;
            }

            if (generalSettings.CarVisualDamageSystemSupport && generalSettings.DOTSSimulation)
            {
                var carHitReactProviderSystem = world.GetOrCreateSystemManaged<CarHitReactProviderSystem>();

                var query = carHitReactProviderSystem.GetPrefabQuery();

                var initAwaiter = new SystemInitAwaiter(this, () => query.CalculateEntityCount() == 0,
                    () =>
                    {
                        carHitReactProviderSystem.Initialize();
                    });

                initAwaiter.StartInit();
            }
        }

        private void DisableSystems()
        {
            DefaultWorldUtils.SwitchActiveManagedSystem<TrafficProcessNodeGroup>(false);
            DefaultWorldUtils.SwitchActiveManagedSystem<TrafficFixedUpdateGroup>(false);
            DefaultWorldUtils.SwitchActiveManagedSystem<TrafficSimulationGroup>(false);
            DefaultWorldUtils.SwitchActiveManagedSystem<TrafficInputGroup>(false);
            DefaultWorldUtils.SwitchActiveManagedSystem<TrafficAreaSimulationGroup>(false);
            DefaultWorldUtils.SwitchActiveManagedSystem<TrafficLateSimulationGroup>(false);
            DefaultWorldUtils.SwitchActiveManagedSystem<BeforePhysXFixedStepGroup>(false);
            DefaultWorldUtils.SwitchActiveManagedSystem<MonoSyncGroup>(false);
            DefaultWorldUtils.SwitchActiveManagedSystem<HybridEntityAdapterSyncSystem>(false);
            DefaultWorldUtils.SwitchActiveUnmanagedSystem<TrafficNodeAvailableSystem>(false);
            DefaultWorldUtils.SwitchActiveUnmanagedSystem<TrafficIdleAtNodeSystem>(false);
            DefaultWorldUtils.SwitchActiveUnmanagedSystem<TrafficNodeCalculateOverlapSystem>(false);
            DefaultWorldUtils.SwitchActiveUnmanagedSystem<CarHashMapSystem>(false);
        }

        private void InitSystems()
        {
            bool hasLinkedNodes = false;

            if (DefaultWorldUtils.TryToGetConfig<TrafficRoadConfigReference>(out var roadConfig))
            {
                var defaultMask = 1 << (int)TrafficNodeType.Parking | 1 << (int)TrafficNodeType.TrafficPublicStop;
                hasLinkedNodes = roadConfig.Config.Value.LinkedNodeFlags != defaultMask;
            }

            InitCommon();

            var trafficSettingsConfig = trafficSettings.TrafficSettingsConfig;
            var hasParkingConfig = DefaultWorldUtils.TryToGetConfig<TrafficParkingConfigReference>(out var parkingConfig);

            if (generalSettings.TrafficPublicSupport)
            {
                RegisterFixedRouteSystems();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficPublicIdleSystem, MainThreadEventGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficPublicInitSystem, StructuralInitGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficPublicRouteCleanerSystem, CleanupGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficPublicStationCleanupSystem, CleanupGroup>();
                hasLinkedNodes = true;
            }

            if (generalSettings.ChangeLaneSupport)
            {
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficChangeLaneReachSystem, LateEventGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficWaitForChangeLaneEventSystem, MainThreadEventGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficChangeLaneSystem, TrafficSimulationGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficChangeLaneTargetSystem, TrafficLateSimulationGroup>();
            }

            if (generalSettings.DOTSSimulation)
            {
                DefaultWorldUtils.SwitchActiveManagedSystem<BeforePhysXFixedStepGroup>(false);
                DefaultWorldUtils.SwitchActiveManagedSystem<HybridEntityAdapterSyncSystem>(false);

                if (trafficSettingsConfig.CullWheelSupported && trafficSettingsConfig.CullWheels || trafficSettingsConfig.CullPhysics)
                {
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficEnableWheelSystem, LateSimulationGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficDisableWheelSystem, SimulationGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficCullWheelSystem, LateEventGroup>();
                }

                if (trafficSettingsConfig.SimplePhysicsEntity)
                {
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficPhysicsMovementSystem, TrafficFixedUpdateGroup>();
                }

                if (generalSettings.DOTSPhysics)
                {
                    if (trafficSettingsConfig.CustomPhysicsEntity)
                    {
                        InitCustomVehicleSystems();
                    }

                    if (generalSettings.CullPhysics && trafficSettingsConfig.CullPhysics && !trafficSettingsConfig.NoPhysicsEntity)
                    {
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<RevertTrafficCulledPhysicsSystem, MainThreadEventGroup>();
                    }
                }
            }
            else
            {
                DefaultWorldUtils.CreateAndAddSystemManaged<TrafficHybridMonoSyncSystem, MonoSyncGroup>();

                if (generalSettings.AvoidanceSupport)
                {
                    if (DefaultWorldUtils.TryToGetConfig<TrafficCollisionConfigReference>(out var collisionConfig) && collisionConfig.Config.Value.AvoidStuckedCollision)
                    {
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficMonoCollisionSystem, PreEarlyJobGroup>();
                    }
                }
            }

            if (trafficSettingsConfig.NoPhysicsEntity || trafficSettingsConfig.CullPhysics)
            {
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficSimpleMovementSystem, TrafficFixedUpdateGroup>();
            }

            if (generalSettings.HealthSystemSupport)
            {
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficExplodedInputSystem, TrafficInputGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficPoolSystem, MainThreadInitGroup>();
            }

            if (!generalSettings.TrafficParkingSupport)
            {
                DefaultWorldUtils.SwitchActiveManagedSystem<TrafficAreaSimulationGroup>(false);
            }
            else
            {
                hasLinkedNodes = true;
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<CarStoppingEngineSystem, MainThreadEventGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<CarIgnitionStateSystem, MainThreadEventGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficAreaCleanSystem, CleanupGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficIdleAtParkingNodeSystem, MainThreadEventGroup>();

                if (hasParkingConfig && parkingConfig.Config.Value.AligmentAtNode)
                {
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficAlignmentAtNodeSystem, TrafficFixedUpdateGroup>();
                }
            }

            if (hasLinkedNodes)
            {
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficCleanLinkedNodeSystem, CleanupGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficProcessEntereredTriggerTrafficNodeSystem, TrafficProcessNodeGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficEnteringLinkedNodeEventSystem, TrafficProcessNodeGroup>();
            }

            if (trafficSettingsConfig.HybridEntity)
            {
                var trafficInitializeHybridHullSystem = DefaultWorldUtils.CreateAndAddSystemManaged<TrafficInitializeHybridHullSystem, StructuralSystemGroup>();
                trafficInitializeHybridHullSystem.Initialize(trafficPoolGlobal);
            }

            if (trafficSettingsConfig.CustomPhysicsEntity)
            {
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficVehicleCustomInputSystem, TrafficInputGroup>();
            }

            if (generalSettings.AvoidanceSupport)
            {
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficCustomDestinationSystem, SimulationGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficAvoidanceEventPlaybackSystem, MainThreadEventPlaybackGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficAvoidanceSystem, SimulationGroup>();
            }

            switch (trafficSettingsConfig.DetectObstacleMode)
            {
                case DetectObstacleMode.Hybrid:
                    break;
                case DetectObstacleMode.CalculateOnly:
                    break;
                case DetectObstacleMode.RaycastOnly:
                    break;
            }

            var hasLerp = trafficSettingsConfig.HasRotationLerp;

            if (hasLerp)
            {
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficSpeedRotationSystem, PreEarlyJobGroup>();
            }

            var currentTrafficSettings = trafficSettingsConfig;

            var trafficDetectObstacleMode = currentTrafficSettings.DetectObstacleMode;
            var trafficDetectNpcMode = currentTrafficSettings.DetectNpcMode;

            if (!generalSettings.HasPedestrian)
            {
                trafficDetectNpcMode = DetectNpcMode.Disabled;
            }

            switch (trafficDetectNpcMode)
            {
                case DetectNpcMode.Disabled:
                    {
                        break;
                    }
                case DetectNpcMode.Calculate:
                    {
                        if (DefaultWorldUtils.TryToGetConfig<NpcCommonConfigReference>(out var hashMapConfig) && hashMapConfig.Config.Value.HashEnabled)
                        {
                            DefaultWorldUtils.CreateAndAddSystemUnmanaged<NpcHashMapSystem, HashMapGroup>(true);
                        }

                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficNpcCalculateObstacleSystem, HashMapGroup>();
                        break;
                    }
                case DetectNpcMode.Raycast:
                    {
                        break;
                    }
            }

            var customTargetQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TrafficDestinationSharedConfig, Prefab>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(world.EntityManager);

            if (customTargetQuery.CalculateEntityCount() > 0)
            {
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficTargetCustomSystem, TrafficLateSimulationGroup>();
            }

            bool hasRaycast = HasRaycast(currentTrafficSettings);

            if (hasRaycast)
            {
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficEnableRaycastSystem, EarlyEventGroup>();

                if (generalSettings.DOTSSimulation)
                {
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficRaycastObstacleSystem, RaycastGroup>();
                }
                else
                {
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficMonoRaycastObstacleSystem, PreEarlyJobGroup>();
                }

                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficRaycastEnableCustomTargetSelectorSystem, MainThreadEventGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficRaycastDisableCustomTargetSelectorSystem1, MainThreadEventGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficRaycastDisableCustomTargetSelectorSystem2, MainThreadEventGroup>();
            }

            if (generalSettings.CarHitCollisionReaction && generalSettings.DOTSPhysics && !currentTrafficSettings.NoPhysicsEntity)
            {
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<CarContactCollectorSystem, EarlyJobGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficCollisionEventSystem, PhysicsTriggerGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficCollisionStateSystem, MainThreadEventGroup>();
            }

            if (generalSettings.RailMovementSupport)
            {
                if (generalSettings.DOTSSimulation)
                {
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<RailSimpleMovementSystem, TrafficFixedUpdateGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<RailSimplePhysicsMovementSystem, TrafficFixedUpdateGroup>();
                }
            }

            if (generalSettings.TrainSupport)
            {
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficTrainTargetSystem, LateInitGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrainSpeedSystem, TrafficFixedUpdateGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficWagonSyncSystem, TrafficSimulationGroup>();

                if (!generalSettings.DOTSSimulation)
                {
                    DefaultWorldUtils.CreateAndAddSystemManaged<TrafficMonoWagonInitSystem, StructuralInitGroup>();
                    DefaultWorldUtils.CreateAndAddSystemManaged<TrainWagonHybridMonoSyncSystem, MonoSyncGroup>();
                    DefaultWorldUtils.CreateAndAddSystemManaged<TrainHybridMonoSyncSystem, MonoSyncGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrainMonoMovementSystem, BeforePhysXFixedStepGroup>();
                }
                else
                {
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficWagonInitSystem, StructuralInitGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrainPhysicsMovementSystem, TrafficFixedUpdateGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrainMovementSystem, TrafficFixedUpdateGroup>();
                }
            }

            bool customMovement = generalSettings.RailMovementSupport || hasParkingConfig && parkingConfig.Config.Value.AligmentAtNode;

            if (customMovement)
            {
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficEnableCustomMovementStateSystem, MainThreadEventGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficDisableCustomMovementStateSystem, MainThreadEventGroup>();
            }

            InitSound();
        }

        public static void RegisterFixedRouteSystems()
        {
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficFixedRouteChangeLaneReachedSystem, EarlyJobGroup>(true);
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficFixedRouteSwitchTargetNodeSystem, TrafficProcessNodeGroup>(true);
        }

        public static void InitCustomVehicleSystems()
        {
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<VehicleToWheelsSystem, PhysicsSimGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<WheelTransformSystem, BeforeTransformGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<WheelSimulationSystem, PhysicsSimGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<VehicleInputSystem, PreEarlyJobGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<WheelContactSystem, RaycastGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<VehicleApplyImpulseSystem, PhysicsSimGroup>();
        }

        private void InitSound()
        {
            if (!DefaultWorldUtils.TryToGetConfig<SoundLevelConfigReference>(out var soundConfig) || !soundConfig.Config.Value.TrafficHasSounds)
                return;

            DefaultWorldUtils.CreateAndAddSystemUnmanaged<CarInitSoundSystem, EarlyEventGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<CarEnableSoundSystem, PreEarlyJobGroup>();

            DefaultWorldUtils.CreateAndAddSystemUnmanaged<CarSoundSystem, HashMapGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<CarUpdateSoundSystem, MainThreadEventGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<CarCullSoundSystem1, LateEventGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<CarCullSoundSystem2, LateEventGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<CarCullSoundSystem3, LateEventGroup>();

#if FMOD
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<CarUpdateParamSoundSystem, MainThreadEventGroup>();
#endif

            if (soundConfig.Config.Value.RandomHornsSound)
            {
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficHornSoundSystem, MainThreadEventGroup>();
            }
        }

        private void InitCommon()
        {
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficLightStateSystem, PreEarlyJobGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficStartInitSystem, PreEarlyJobGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficSpeedLimitSystem, EarlyJobGroup>();
        }

        public static bool HasRaycast(TrafficCarSettingsConfig currentTrafficSettings)
        {
            return currentTrafficSettings.DetectObstacleMode == DetectObstacleMode.Hybrid || currentTrafficSettings.DetectObstacleMode == DetectObstacleMode.RaycastOnly || currentTrafficSettings.DetectNpcMode == DetectNpcMode.Raycast;
        }
    }
}
