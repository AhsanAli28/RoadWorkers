using Spirit604.AnimationBaker.Entities;
using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Config;
using Spirit604.DotsCity.Simulation.Factory.Pedestrian;
using Spirit604.DotsCity.Simulation.Npc;
using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Pedestrian.Authoring;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.DotsCity.Simulation.Traffic.Authoring;
using Spirit604.DotsCity.Simulation.TrafficPublic;
using Unity.Entities;
using UnityEngine;
using EntityType = Spirit604.DotsCity.Simulation.Pedestrian.EntityType;

namespace Spirit604.DotsCity.Simulation.Initialization
{
    public class PedestrianGlobalInitializer : InitializerBase
    {
        [SerializeField] private bool forceCleanSimulation;

        #region Variables

        private World entityWorld;

        #endregion

        #region Constructor

        private GeneralSettingDataSimulation generalSettingData;
        private PedestrianSpawnerConfigHolder pedestrianSpawnerConfigHolder;
        private PedestrianSkinFactory pedestrianSkinFactory;
        private PedestrianCrowdSkinFactory pedestrianBakedSkinFactory;
        private PedestrianRagdollSpawner pedestrianRagdollSpawner;
        private TrafficSettings trafficSettings;
        private HybridShapeFactory hybridShapeFactory;

        [InjectWrapper]
        public void Construct(
            GeneralSettingDataSimulation generalSettingData,
            PedestrianSpawnerConfigHolder pedestrianSpawnerConfigHolder,
            PedestrianSkinFactory pedestrianSkinFactory,
            PedestrianCrowdSkinFactory pedestrianBakedSkinFactory,
            PedestrianRagdollSpawner pedestrianRagdollSpawner,
            TrafficSettings trafficSettings,
            HybridShapeFactory hybridShapeFactory = null)
        {
            this.generalSettingData = generalSettingData;
            this.pedestrianSpawnerConfigHolder = pedestrianSpawnerConfigHolder;
            this.pedestrianSkinFactory = pedestrianSkinFactory;
            this.pedestrianBakedSkinFactory = pedestrianBakedSkinFactory;
            this.pedestrianRagdollSpawner = pedestrianRagdollSpawner;
            this.hybridShapeFactory = hybridShapeFactory;
            this.trafficSettings = trafficSettings;
        }

        #endregion

        #region InitializerBase methods

        public override void Initialize()
        {
            base.Initialize();
            InitializeInternal();
        }

        #endregion

        #region Methods

        private void InitializeInternal()
        {
            entityWorld = World.DefaultGameObjectInjectionWorld;

            InitSystems();
        }

        private void InitSystems()
        {
            if (!generalSettingData.HasPedestrian) return;

            var pedestrianSettingsConfig = pedestrianSpawnerConfigHolder.PedestrianSettingsConfig;
            var antistuck = false;

            if (DefaultWorldUtils.TryToGetConfig<AntistuckConfigReference>(out var antiStuckConfig) && antiStuckConfig.Config.Value.AntistuckEnabled)
            {
                antistuck = true;
            }

            InitCommonSystems();

            var obstacleAvoidanceType = pedestrianSettingsConfig.ObstacleAvoidanceType;

            if (!generalSettingData.NavigationSupport)
            {
                obstacleAvoidanceType = ObstacleAvoidanceType.Disabled;
            }

            switch (obstacleAvoidanceType)
            {
                case ObstacleAvoidanceType.Disabled:
                    {
                        break;
                    }
                case ObstacleAvoidanceType.CalcNavPath:
                    {
#if REESE_PATH
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<NpcRecalculateNavTargetSystem, NavSimulationGroup>();
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<UpdateNavAgentTargetSystem, NavSimulationGroup>();

                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<NavAgentCleanStateSystem1, MainThreadEventGroup>();
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<NavAgentCleanStateSystem2, MainThreadEventGroup>();
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<NavAgentNavigationSystem, NavSimulationGroup>();

                        if (pedestrianSettingsConfig.PedestrianNavigationType != NpcNavigationType.Persist)
                        {
                            DefaultWorldUtils.CreateAndAddSystemUnmanaged<RevertNavAgentTargetSystem, MainThreadEventGroup>();
                        }
                        else
                        {
                            DefaultWorldUtils.CreateAndAddSystemUnmanaged<NavAgentPersistTargetListenerSystem, NavSimulationGroup>();
                        }

                        var hasNavObstacle = trafficSettings.TrafficSettingsConfig?.HasNavObstacle ?? false;

                        if (!hasNavObstacle)
                        {
                            UnityEngine.Debug.Log("PedestrianGlobalInitializer. ObstacleAvoidanceType.CalcNavPath enabled for pedestrians. Make sure, that NavMesh obstacle for traffic is enabled in the traffic settings");
                        }
#endif
                        break;
                    }
                case ObstacleAvoidanceType.LocalAvoidance:
                    {
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<NpcRecalculateNavTargetSystem, NavSimulationGroup>();

                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<LocalAvoidanceObstacleSystem, NavSimulationGroup>();

                        if (antistuck)
                        {
                            DefaultWorldUtils.CreateAndAddSystemUnmanaged<AntistuckActivateLocalAvoidanceSystem, StructuralSystemGroup>();
                        }

                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<FollowAvoidanceSystem, NavSimulationGroup>();
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<DisableAvoidanceSystem, LateEventGroup>();
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<ForceDisableAvoidanceSystem, LateEventGroup>();
                        break;
                    }
            }

            if (antistuck)
            {
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<AntistuckActivateSystem, MainThreadEventGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<AntistuckTargetSystem, LateEventGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<AntistuckMovementSystem, PedestrianFixedSimulationGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<AntistuckDeactivateSystem, MainThreadEventGroup>();
            }

            var pedestrianCollisionType = pedestrianSettingsConfig.PedestrianCollisionType;
            var pedestrianEntityType = pedestrianSettingsConfig.PedestrianEntityType;

            bool hasPhysics = generalSettingData.SimulationType != Unity.Physics.SimulationType.NoPhysics &&
                pedestrianSettingsConfig.PedestrianEntityType == EntityType.Physics;

            if (!hasPhysics)
            {
                if (pedestrianEntityType == EntityType.Physics)
                {
                    pedestrianEntityType = EntityType.NoPhysics;
                }

                if (pedestrianCollisionType == CollisionType.Physics)
                {
                    pedestrianCollisionType = CollisionType.Calculate;
                }
            }

            switch (pedestrianEntityType)
            {
                case EntityType.NoPhysics:
                    break;
                case EntityType.Physics:
                    {
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<PhysicsCollisionStateSystem, SimulationGroup>();
                        break;
                    }
            };

            switch (pedestrianCollisionType)
            {
                case CollisionType.Calculate:
                    {
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<CalculateCollisionSystem, SimulationGroup>();
                        DefaultWorldUtils.CreateAndAddSystemManaged<ReactionCollisionSystem, SimulationGroup>();
                        break;
                    }
                case CollisionType.Physics:
                    {
                        if (generalSettingData.DOTSPhysics)
                        {
                            DefaultWorldUtils.CreateAndAddSystemManaged<ReactionCollisionSystem, SimulationGroup>();
                            DefaultWorldUtils.CreateAndAddSystemUnmanaged<PhysicsCollisionEventSystem, PhysicsTriggerGroup>();
                        }
                        break;
                    }
            }

            var npcSkinType = pedestrianSettingsConfig.PedestrianSkinType;
            var npcRigType = pedestrianSettingsConfig.PedestrianRigType;

            var hasSkin = npcSkinType ==
                NpcSkinType.RigShowOnlyInView ||
                npcSkinType == NpcSkinType.RigShowAlways;

            bool hasBench = generalSettingData.BenchSystemSupport;

            bool hasHybridSkin = false;
            bool hasGPUSkin = false;
            bool hashybridGPUSkin = false;
            bool hashybridShape = false;
            var anyBuiltInSkin = (int)npcRigType <= 3 && hasSkin;

            if (hasSkin)
            {
                switch (npcRigType)
                {
                    case NpcRigType.HybridLegacy:
                        {
                            hasHybridSkin = true;

                            if (pedestrianSettingsConfig.PedestrianSkinType != NpcSkinType.RigShowAlways)
                            {
                                DefaultWorldUtils.CreateAndAddSystemUnmanaged<UnloadHybridSkinSystem, MainThreadEventGroup>();
                            }

                            break;
                        }
                    case NpcRigType.PureGPU:
                        {
                            hasGPUSkin = true;

                            if (pedestrianSettingsConfig.PedestrianSkinType == NpcSkinType.RigShowAlways)
                            {
                                DefaultWorldUtils.CreateAndAddSystemUnmanaged<LoadPermamentGPUSkinSystem, EarlyEventGroup>();
                                DefaultWorldUtils.CreateAndAddSystemUnmanaged<UpdatePermamentGPUSkinSystem, LateEventGroup>();
                            }
                            else
                            {
                                DefaultWorldUtils.CreateAndAddSystemUnmanaged<UnloadGPUSkinSystem, LateEventGroup>();
                                DefaultWorldUtils.CreateAndAddSystemUnmanaged<LoadGPUSkinSystem, EarlyEventGroup>();
                                DefaultWorldUtils.CreateAndAddSystemUnmanaged<UpdateGPUSkinSystem, PreEarlyJobGroup>();
                            }

                            break;
                        }
                    case NpcRigType.HybridAndGPU:
                        {
                            hasHybridSkin = true;
                            hasGPUSkin = true;
                            hashybridGPUSkin = true;

                            DefaultWorldUtils.CreateAndAddSystemUnmanaged<UpdatePermamentGPUSkinSystem, LateEventGroup>();
                            DefaultWorldUtils.CreateAndAddSystemUnmanaged<LoadInitialHybridGPUSkinSystem, EarlyEventGroup>();
                            DefaultWorldUtils.CreateAndAddSystemUnmanaged<LoadInitialHybridGPUSkinSystem2, EarlyEventGroup>();
                            break;
                        }
                    case NpcRigType.HybridOnRequestAndGPU:
                        {
                            hasHybridSkin = true;
                            hasGPUSkin = true;
                            hashybridGPUSkin = true;

                            DefaultWorldUtils.CreateAndAddSystemUnmanaged<UpdatePermamentGPUSkinSystem, LateEventGroup>();
                            DefaultWorldUtils.CreateAndAddSystemUnmanaged<LoadInitialHybridGPUSkinSystem3, EarlyEventGroup>();
                            DefaultWorldUtils.CreateAndAddSystemManaged<UnloadOnRequestHybridGPUSkinSystem, MainThreadEventGroup>();
                            break;
                        }
                    case NpcRigType.HybridShapeGPU:
                        {
                            hashybridShape = true;
                            hasGPUSkin = true;

                            if (pedestrianSettingsConfig.PedestrianSkinType == NpcSkinType.RigShowAlways)
                            {
                                DefaultWorldUtils.CreateAndAddSystemUnmanaged<LoadPermamentGPUSkinSystem, EarlyEventGroup>();
                                DefaultWorldUtils.CreateAndAddSystemUnmanaged<UpdatePermamentGPUSkinSystem, LateEventGroup>();
                            }
                            else
                            {
                                DefaultWorldUtils.CreateAndAddSystemUnmanaged<UnloadGPUSkinSystem, LateEventGroup>();
                                DefaultWorldUtils.CreateAndAddSystemUnmanaged<LoadGPUSkinSystem, EarlyEventGroup>();
                                DefaultWorldUtils.CreateAndAddSystemUnmanaged<UpdateGPUSkinSystem, PreEarlyJobGroup>();
                            }

                            break;
                        }
                    case NpcRigType.RukhankaHybrid:
                        {
                            hashybridShape = true;
                            break;
                        }
                }

                if (hasHybridSkin)
                {
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<LegacyAnimatorSystem, MainThreadInitGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<LegacyAnimatorCustomStateSystem, MainThreadInitGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<LegacyAnimatorCustomExitStateSystem, MainThreadInitGroup>();

                    if (hasBench)
                    {
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<BenchWaitForExitLegacySkinSystem, MainThreadInitGroup>();
                    }

                    if (pedestrianSettingsConfig.PedestrianSkinType != NpcSkinType.RigShowAlways && !hashybridGPUSkin || hashybridGPUSkin)
                    {
                        var pedestrianLoadSkinSystem = DefaultWorldUtils.CreateAndAddSystemManaged<LoadHybridSkinSystem, MainThreadEventGroup>();
                        pedestrianLoadSkinSystem.Initialize(pedestrianSkinFactory);
                    }
                    else
                    {
                        // System duplicate due to Entities.WithAny bug in Run() mode
                        var pedestrianLoadSkinSystem = DefaultWorldUtils.CreateAndAddSystemManaged<LoadAlwaysHybridSkinSystem, MainThreadEventGroup>();
                        pedestrianLoadSkinSystem.Initialize(pedestrianSkinFactory);
                    }
                }

                if (hasGPUSkin)
                {
                    if (!forceCleanSimulation)
                    {
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<GPUAnimatorCustomStateSystem, LateEventGroup>();
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<CrowdAnimatorTransitionSystem, InitializationSystemGroup>();
                    }

                    pedestrianBakedSkinFactory.CreateFactory();

                    if (hasBench)
                    {
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<BenchWaitForExitGPUSkinSystem, LateEventGroup>();
                    }

                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<GPUAnimatorSystem, LateEventGroup>();

                    var crowdSkinProviderSystem = DefaultWorldUtils.CreateAndAddSystemManaged<CrowdSkinProviderSystem, InitializationSystemGroup>();
                    DefaultWorldUtils.CreateAndAddSystemManaged<InitGPUSkinSystem, MainThreadEventGroup>();

                    crowdSkinProviderSystem.Initialize(pedestrianBakedSkinFactory);
                    crowdSkinProviderSystem.CreateBlobEntity();
                }

                if (hashybridGPUSkin)
                {
                    var pedestrianLoadSkinSystem = DefaultWorldUtils.CreateAndAddSystemManaged<LoadHybridGPUSkinSystem, MainThreadEventGroup>();
                    pedestrianLoadSkinSystem.Initialize(pedestrianSkinFactory);

                    DefaultWorldUtils.CreateAndAddSystemManaged<UnloadHybridGPUSkinSystem, MainThreadEventGroup>();
                }

                if (hashybridShape)
                {
                    if (hybridShapeFactory)
                    {
                        hybridShapeFactory.Populate();

                        if (pedestrianSettingsConfig.PedestrianSkinType != NpcSkinType.RigShowAlways)
                        {
                            var loadHybridShapeSystem = DefaultWorldUtils.CreateAndAddSystemManaged<LoadHybridShapeSystem, MainThreadEventGroup>();
                            loadHybridShapeSystem.Initialize(hybridShapeFactory);

                            DefaultWorldUtils.CreateAndAddSystemUnmanaged<UnloadHybridShapeSystem, MainThreadEventGroup>();
                        }
                        else
                        {
                            // System duplicate due to Entities.WithAny bug in Run() mode
                            var loadHybridShapeSystem = DefaultWorldUtils.CreateAndAddSystemManaged<LoadAlwaysHybridShapeSystem, MainThreadEventGroup>();
                            loadHybridShapeSystem.Initialize(hybridShapeFactory);
                        }
                    }
                    else
                    {
                        Debug.LogError("PedestrianGlobalInitializer. <b>HybridShapeFactory</b> not assigned in <b>TrafficSimulationInstaller</b>");
                    }
                }

                if (!forceCleanSimulation)
                {
                    if (anyBuiltInSkin)
                    {
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<AnimatorCustomWaitForSkinSystem, MainThreadEventGroup>();
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<AnimatorCustomUnloadSkinListenerSystem, MainThreadEventGroup>();
                    }

                    DefaultWorldUtils.SwitchActiveManagedSystem<CrowdTransitionProviderSystem>(false);
                }

                if (hasBench)
                {
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<BenchWaitForExitNoSkinSystem, MainThreadEventGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<PedestrianMovementSittingSystem, PedestrianFixedSimulationGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<BenchCleanSystem, CleanupGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<BenchStateSystem, StructuralInitGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<ProcessEnteredEntrySeatPositionSystem, StructuralSystemGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<ProcessEnteredSeatNodeSystem, StructuralSystemGroup>();
                }

                if (generalSettingData.TrafficPublicSupport)
                {
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<ProcessTrafficStationNodeSystem, MainThreadEventGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<QueueWaitSystem, MainThreadEventGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<CleanQueueWaitSystem, CleanupGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficPublicExitPedestrianSystem, MainThreadEventGroup>();
                }

                if (generalSettingData.TalkingSupport)
                {
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<EnableSpawnAreaSystem, EarlyEventGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<DisableSpawnAreaSystem, LateEventGroup>();
                    DefaultWorldUtils.CreateAndAddSystemManaged<PedestrianSpawnTalkAreaSystem, SpawnerGroup>();

                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<StopTalkStateSystem, MainThreadEventGroup>();
                    DefaultWorldUtils.CreateAndAddSystemManaged<DisableTalkStateSystem, MainThreadEventGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<SwitchTalkingKeySystem, MainThreadEventGroup>();
                }

                if (generalSettingData.HealthSystemSupport && pedestrianSettingsConfig.HasRagdoll)
                {
                    NpcDeathEventConsumerSystem npcDeathEventConsumerSystem = default;

                    if (npcRigType != NpcRigType.Rukhanka)
                    {
                        var ragdollSystem = DefaultWorldUtils.CreateAndAddSystemManaged<RagdollSystem, MainThreadEventGroup>();

                        if (generalSettingData.PedestrianTriggerSystemSupport)
                        {
                            npcDeathEventConsumerSystem = DefaultWorldUtils.CreateAndAddSystemManaged<NpcDeathEventConsumerSystem, MainThreadEventGroup>();
                            ragdollSystem.TriggerSupported = true;
                        }

                        ragdollSystem.Initialize(pedestrianRagdollSpawner, npcDeathEventConsumerSystem);
                    }
                    else
                    {
#if RUKHANKA
                        var ragdollSystem = DefaultWorldUtils.CreateAndAddSystemManaged<RukhankaRagdollSpawnSystem, BeginSimulationGroup>();

                        if (generalSettingData.PedestrianTriggerSystemSupport)
                        {
                            npcDeathEventConsumerSystem = DefaultWorldUtils.CreateAndAddSystemManaged<NpcDeathEventConsumerSystem, MainThreadEventGroup>();
                            ragdollSystem.TriggerSupported = true;
                        }

                        ragdollSystem.Initialize(npcDeathEventConsumerSystem);
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<RukhankaRagdollInitialSyncAnimSystem, EarlyJobGroup>();
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<RukhankaRagdollActivateSystem, LateSimulationGroup>();
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<RukhankaRagdollPoolSystem, SimulationGroup>();
#else
                        Debug.LogError("Rukhanka not installed");
#endif
                    }
                }

                if (generalSettingData.PedestrianTriggerSystemSupport)
                {
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<LifeTimeEntitySystem, DestroyGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<ActivateRunningScaryStateSystem, MainThreadEventGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<DisableScaryStateSystem, MainThreadEventGroup>();
                }

                if (generalSettingData.TrafficParkingSupport)
                {
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<EnterParkingCarSystem, MainThreadEventGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<ProcessEnterParkingNodeSystem, TrafficProcessNodeGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<ProcessTrafficEntryNodeSystem, MainThreadEventGroup>();
                }
            }
        }

        private void InitCommonSystems()
        {
            if (DefaultWorldUtils.TryToGetConfig<NpcCommonConfigReference>(out var hashMapConfig) && hashMapConfig.Config.Value.HashEnabled)
            {
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<NpcHashMapSystem, HashMapGroup>(true);
            }

            DefaultWorldUtils.CreateAndAddSystemUnmanaged<PedestrianStateSystem, PreEarlyJobGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<PoolNoRagdollSystem, LateEventGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<IdleTimeSystem, MainThreadEventGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<DefaultReachProccesingSystem, MainThreadEventGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<PedestrianMovementSystem, PreEarlyJobGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<ProcessDefaultNodeSystem, PreEarlyJobGroup>();
        }

        #endregion
    }
}
