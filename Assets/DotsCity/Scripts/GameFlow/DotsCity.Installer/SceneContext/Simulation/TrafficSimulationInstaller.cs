using Spirit604.DotsCity.NavMesh;
using Spirit604.DotsCity.Simulation.Factory.Pedestrian;
using Spirit604.DotsCity.Simulation.Factory.Traffic;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Pedestrian.Authoring;
using Spirit604.DotsCity.Simulation.Traffic.Authoring;
using UnityEngine;

#if ZENJECT
using Zenject;
#else
using Spirit604.DotsCity.Simulation.Initialization;
#endif

namespace Spirit604.DotsCity.Installer
{
    public class TrafficSimulationInstaller :
#if ZENJECT
        MonoInstaller
#else
        ManualReferenceInstaller
#endif
    {
        [Header("Refs")]
        [SerializeField] private TrafficCarPoolGlobal trafficCarPoolGlobal;
        [SerializeField] private TrafficSettings trafficSettings;
        [SerializeField] private PedestrianCrowdSkinFactory pedestrianCrowdSkinFactory;
        [SerializeField] private PedestrianSpawnerConfigHolder pedestrianSpawnerConfigHolder;
        [SerializeField] private NavMeshObstacleFactory navMeshObstacleFactory;
        [SerializeField] private HybridShapeFactory hybridShapeFactory;

#if ZENJECT

        [EditorResolve][SerializeField] private PedestrianSkinFactory pedestrianSkinFactory;
        [EditorResolve][SerializeField] private PedestrianRagdollFactory pedestrianRagdollFactory;
        [EditorResolve][SerializeField] private PedestrianRagdollSpawner pedestrianRagdollSpawner;

#else
        [Header("Resolve")]

        [EditorResolve][SerializeField] private TrafficInitializer trafficInitializer;
        [EditorResolve][SerializeField] private PedestrianGlobalInitializer pedestrianGlobalInitializer;
        [EditorResolve][SerializeField] private PedestrianSkinFactory pedestrianSkinFactory;
        [EditorResolve][SerializeField] private PedestrianRagdollFactory pedestrianRagdollFactory;
        [EditorResolve][SerializeField] private PedestrianRagdollSpawner pedestrianRagdollSpawner;

        [Space]

        [EditorResolve][SerializeField] private PedestrianSettingsConfigAuthoring pedestrianSettingsConfigAuthoring;
        [EditorResolve][SerializeField] private PedestrianSpawnerConfigAuthoring pedestrianSpawnerConfigAuthoring;
        [EditorResolve][SerializeField] private PedestrianTalkSpawnerConfigAuthoring pedestrianTalkSpawnerConfigAuthoring;

        [Header("Additional Refs")]

        [EditorResolve][SerializeField] private CityCoreInstaller coreInstaller;
#endif

#if ZENJECT

        public override void InstallBindings()
        {
            BindTraffic();
            BindPedestrian();
            BindCommon();
        }

        private void BindTraffic()
        {
            Container.Bind<TrafficCarPoolGlobal>().FromInstance(trafficCarPoolGlobal).AsSingle();
            Container.Bind<TrafficSettings>().FromInstance(trafficSettings).AsSingle();
        }

        private void BindPedestrian()
        {
            Container.Bind<PedestrianSkinFactory>().FromInstance(pedestrianSkinFactory).AsSingle();
            Container.Bind<PedestrianCrowdSkinFactory>().FromInstance(pedestrianCrowdSkinFactory).AsSingle();
            Container.Bind<PedestrianSpawnerConfigHolder>().FromInstance(pedestrianSpawnerConfigHolder).AsSingle();

            var pedestrianRigType = pedestrianSpawnerConfigHolder.PedestrianSettingsConfig.PedestrianRigType;

            switch (pedestrianRigType)
            {
                case NpcRigType.HybridLegacy:
                    {
                        Container.Bind<IPedestrianRagdollPrefabProvider>().FromInstance(pedestrianSkinFactory).AsSingle();
                        Container.Bind<IPedestrianSkinInfoProvider>().FromInstance(pedestrianSkinFactory).AsSingle();
                        break;
                    }
                case NpcRigType.PureGPU:
                    {
                        Container.Bind<IPedestrianRagdollPrefabProvider>().FromInstance(pedestrianCrowdSkinFactory).AsSingle();
                        Container.Bind<IPedestrianSkinInfoProvider>().FromInstance(pedestrianCrowdSkinFactory).AsSingle();
                        break;
                    }
                case NpcRigType.HybridAndGPU:
                    {
                        Container.Bind<IPedestrianRagdollPrefabProvider>().FromInstance(pedestrianCrowdSkinFactory).AsSingle();
                        Container.Bind<IPedestrianSkinInfoProvider>().FromInstance(pedestrianCrowdSkinFactory).AsSingle();
                        break;
                    }
                case NpcRigType.HybridOnRequestAndGPU:
                    {
                        Container.Bind<IPedestrianRagdollPrefabProvider>().FromInstance(pedestrianCrowdSkinFactory).AsSingle();
                        Container.Bind<IPedestrianSkinInfoProvider>().FromInstance(pedestrianCrowdSkinFactory).AsSingle();
                        break;
                    }
                case NpcRigType.HybridShapeGPU:
                    {
                        Container.Bind<IPedestrianRagdollPrefabProvider>().FromInstance(pedestrianCrowdSkinFactory).AsSingle();
                        Container.Bind<IPedestrianSkinInfoProvider>().FromInstance(pedestrianCrowdSkinFactory).AsSingle();
                        break;
                    }
                case NpcRigType.Rukhanka:
                    {
                        Container.Bind<IPedestrianRagdollPrefabProvider>().FromInstance(new DummyRagdollPrefabProvider()).AsSingle();
                        Container.Bind<IPedestrianSkinInfoProvider>().FromInstance(pedestrianSpawnerConfigHolder).AsSingle();
                        break;
                    }
                case NpcRigType.RukhankaHybrid:
                    {
                        Container.Bind<IPedestrianRagdollPrefabProvider>().FromInstance(new DummyRagdollPrefabProvider()).AsSingle();
                        Container.Bind<IPedestrianSkinInfoProvider>().FromInstance(pedestrianSpawnerConfigHolder).AsSingle();
                        break;
                    }
                default:
                    {
                        Container.Bind<IPedestrianRagdollPrefabProvider>().FromInstance(pedestrianSkinFactory).AsSingle();
                        Container.Bind<IPedestrianSkinInfoProvider>().FromInstance(pedestrianSkinFactory).AsSingle();
                        break;
                    }
            }

            Container.Bind<IPedestrianRagdollFactory>().FromInstance(pedestrianRagdollFactory).AsSingle();
            Container.Bind<PedestrianRagdollSpawner>().FromInstance(pedestrianRagdollSpawner).AsSingle();

            if (hybridShapeFactory)
                Container.Bind<HybridShapeFactory>().FromInstance(hybridShapeFactory).AsSingle();
        }

        private void BindCommon()
        {
            Container.Bind<NavMeshObstacleFactory>().FromInstance(navMeshObstacleFactory).AsSingle();
        }
#else

        public override void Resolve()
        {
            trafficInitializer.Construct(coreInstaller.Settings, trafficCarPoolGlobal, trafficSettings, navMeshObstacleFactory);
            pedestrianGlobalInitializer.Construct(coreInstaller.Settings, pedestrianSpawnerConfigHolder, pedestrianSkinFactory, pedestrianCrowdSkinFactory, pedestrianRagdollSpawner, trafficSettings, hybridShapeFactory);

            pedestrianSkinFactory.Construct(pedestrianSpawnerConfigHolder);

            var pedestrianRigType = pedestrianSpawnerConfigHolder.PedestrianSettingsConfig.PedestrianRigType;

            switch (pedestrianRigType)
            {
                case NpcRigType.HybridLegacy:
                    {
                        pedestrianRagdollFactory.Construct(pedestrianSpawnerConfigHolder, pedestrianSkinFactory);
                        pedestrianSettingsConfigAuthoring.Construct(pedestrianSkinFactory);
                        break;
                    }
                case NpcRigType.PureGPU:
                    {
                        pedestrianRagdollFactory.Construct(pedestrianSpawnerConfigHolder, pedestrianCrowdSkinFactory);
                        pedestrianSettingsConfigAuthoring.Construct(pedestrianCrowdSkinFactory);
                        break;
                    }
                case NpcRigType.HybridAndGPU:
                    {
                        pedestrianRagdollFactory.Construct(pedestrianSpawnerConfigHolder, pedestrianCrowdSkinFactory);
                        pedestrianSettingsConfigAuthoring.Construct(pedestrianCrowdSkinFactory);
                        break;
                    }
                case NpcRigType.HybridOnRequestAndGPU:
                    {
                        pedestrianRagdollFactory.Construct(pedestrianSpawnerConfigHolder, pedestrianCrowdSkinFactory);
                        pedestrianSettingsConfigAuthoring.Construct(pedestrianCrowdSkinFactory);
                        break;
                    }
                case NpcRigType.HybridShapeGPU:
                    {
                        pedestrianRagdollFactory.Construct(pedestrianSpawnerConfigHolder, pedestrianCrowdSkinFactory);
                        pedestrianSettingsConfigAuthoring.Construct(pedestrianCrowdSkinFactory);
                        break;
                    }
                case NpcRigType.Rukhanka:
                    {
                        pedestrianRagdollFactory.Construct(pedestrianSpawnerConfigHolder, new DummyRagdollPrefabProvider());
                        pedestrianSettingsConfigAuthoring.Construct(pedestrianSpawnerConfigHolder);
                        break;
                    }
                case NpcRigType.RukhankaHybrid:
                    {
                        pedestrianRagdollFactory.Construct(pedestrianSpawnerConfigHolder, new DummyRagdollPrefabProvider());
                        pedestrianSettingsConfigAuthoring.Construct(pedestrianSpawnerConfigHolder);
                        break;
                    }
                default:
                    {
                        pedestrianRagdollFactory.Construct(pedestrianSpawnerConfigHolder, pedestrianSkinFactory);
                        pedestrianSettingsConfigAuthoring.Construct(pedestrianSkinFactory);
                        break;
                    }
            }

            pedestrianRagdollSpawner.Construct(pedestrianRagdollFactory);

            pedestrianSpawnerConfigAuthoring.Construct(pedestrianSpawnerConfigHolder);
            pedestrianTalkSpawnerConfigAuthoring.Construct(pedestrianSpawnerConfigHolder);
        }
#endif

    }
}

