using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Core.Initialization;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Config;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.DotsCity.Simulation.Npc;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.DotsCity.Simulation.TrafficPublic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Initialization
{
    public class CitySettingsSimulationInitializer : CitySettingsInitializerBase<GeneralSettingDataSimulation>
    {
        public override void Initialize()
        {
            base.Initialize();

            CitySettingsCoreInitializer.InitializeStatic(Settings);
            InitializeStatic(this, Settings);
        }

        public static void InitializeStatic(MonoBehaviour sender, GeneralSettingDataSimulation settings)
        {
            var world = World.DefaultGameObjectInjectionWorld;

            if (settings.HasTraffic)
            {
                if (settings.TrafficPublicSupport)
                {
                    var spawner = DefaultWorldUtils.CreateAndAddSystemManaged<TrafficPublicSpawnerSystem, SpawnerGroup>();
                    spawner.Enabled = false;
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficPublicNodeAvailableSystem, TrafficLateSimulationGroup>();

                    var systemAwaiter = new SystemInitAwaiter(sender, () => !TrafficNodeResolverSystem.PathDataHashMapStaticRef.IsCreated, () =>
                    {
                        world.GetOrCreateSystemManaged<TrafficPublicSpawnerSystem>().Enabled = true;
                        DefaultWorldUtils.SwitchActiveUnmanagedSystem<TrafficPublicNodeAvailableSystem>(true);
                    });

                    systemAwaiter.StartInit();
                }

                if (settings.AntiStuckSupport)
                {
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficCullStuckedSystem, LateEventGroup>();
                }

                if (settings.DOTSSimulation)
                {
                    if (settings.WheelSystemSupport)
                    {
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<CarSimpleWheelSystem, TrafficFixedUpdateGroup>();
                    }

                    if (settings.CarVisualDamageSystemSupport)
                    {
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<CarAnimateHitReactionSystem, StructuralSystemGroup>();
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<CarHitReactionIndexCleanerSystem, CleanupGroup>();
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<CarHitInitSystem, StructuralInitGroup>();
                    }
                }

                if (settings.VehicleLightSupport)
                {
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficLightIndicatorStateSystem, PreEarlyJobGroup>();
                }
            }
            else
            {
                world.GetOrCreateSystemManaged<TrafficSpawnerSystem>().Enabled = false;
                world.GetOrCreateSystemManaged<TrafficSpawnerSystem>().ForceDisable = true;
            }

            if (!settings.HasPedestrian)
            {
                world.GetOrCreateSystemManaged<PedestrianEntitySpawnerSystem>().Enabled = false;
                world.GetOrCreateSystemManaged<PedestrianEntitySpawnerSystem>().ForceDisable = true;
            }
            else
            {
                if (settings.HealthSystemSupport)
                {
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<HealthWithRagdollSystem, MainThreadEventGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<HealthNoRagdollSystem, LateEventGroup>();
                    DefaultWorldUtils.CreateAndAddSystemManaged<NpcRagdollSystem, MainThreadInitGroup>();
                    DefaultWorldUtils.CreateAndAddSystemManaged<NpcProcessHitReactionSystem, StructuralSystemGroup>();
                }
            }
        }

        public class GeneralSettingsSimulationBaker : Baker<CitySettingsSimulationInitializer>
        {
            public override void Bake(CitySettingsSimulationInitializer authoring)
            {
                DependsOn(authoring.Settings);
                Bake(this, authoring.Settings);
            }

            public static void Bake(IBaker baker, GeneralSettingDataSimulation settings)
            {
                var coreSettingsEntity = baker.CreateAdditionalEntity(TransformUsageFlags.None);

                baker.AddComponent(coreSettingsEntity, GeneralCoreSettingsRuntimeAuthoring.CreateConfigStatic(baker, settings));

                var commonGeneralSettingsDataEntity = baker.CreateAdditionalEntity(TransformUsageFlags.None);

                baker.AddComponent(commonGeneralSettingsDataEntity, GeneralSettingsCommonRuntimeAuthoring.CreateConfigStatic(baker, settings));

                var pedestrianGeneralSettingsDataEntity = baker.CreateAdditionalEntity(TransformUsageFlags.None);

                baker.AddComponent(pedestrianGeneralSettingsDataEntity, PedestrianGeneralSettingsRuntimeAuthoring.CreateConfigStatic(baker, settings));

                var trafficGeneralSettingsDataEntity = baker.CreateAdditionalEntity(TransformUsageFlags.None);

                baker.AddComponent(trafficGeneralSettingsDataEntity, TrafficGeneralSettingsRuntimeAuthoring.CreateConfigStatic(baker, settings));
            }
        }
    }
}