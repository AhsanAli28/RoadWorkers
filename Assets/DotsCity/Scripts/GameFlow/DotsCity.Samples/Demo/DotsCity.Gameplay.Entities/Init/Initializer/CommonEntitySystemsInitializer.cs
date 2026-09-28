using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.CameraService;
using Spirit604.DotsCity.Gameplay.Config.Common;
using Spirit604.DotsCity.Gameplay.Events;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Pedestrian.Authoring;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.DotsCity.Simulation.VFX;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Initialization
{
    public class CommonEntitySystemsInitializer : InitializerBase
    {
        private VFXFactory vfxFactory;
        private GeneralSettingData generalSettingData;
        private PedestrianSpawnerConfigHolder pedestrianSpawnerConfigHolder;
        private CameraController cameraController;

        [InjectWrapper]
        public void Construct(
            VFXFactory vfxFactory,
            GeneralSettingData generalSettingData,
            PedestrianSpawnerConfigHolder pedestrianSpawnerConfigHolder,
            CameraController cameraController = null)
        {
            this.vfxFactory = vfxFactory;
            this.generalSettingData = generalSettingData;
            this.pedestrianSpawnerConfigHolder = pedestrianSpawnerConfigHolder;
            this.cameraController = cameraController;
        }

        public override void Initialize()
        {
            var world = World.DefaultGameObjectInjectionWorld;


            // If the user wants to replace the camera with his own solution.
            if (cameraController && generalSettingData.HealthSystemSupport)
            {
                var cameraShakeEventSystem = DefaultWorldUtils.CreateAndAddSystemManaged<CameraShakeEventSystem, StructuralSystemGroup>();
                cameraShakeEventSystem.Initialize(cameraController);
            }

            if (generalSettingData.DOTSSimulation)
            {
                if (generalSettingData.HealthSystemSupport)
                {
                    var carVfxExplodeSystem = DefaultWorldUtils.CreateAndAddSystemManaged<CarVfxExplodeSystem, StructuralSystemGroup>();
                    carVfxExplodeSystem.Initialize(vfxFactory);
                }

                if (generalSettingData.BulletSupport)
                {
                    if (generalSettingData.CarVisualDamageSystemSupport)
                    {
                        var bulletReaction = DefaultWorldUtils.CreateAndAddSystemManaged<BulletHitReactionSystem, StructuralSystemGroup>();
                        bulletReaction.Initialize(vfxFactory);
                    }
                }

                var pedestrianSettings = pedestrianSpawnerConfigHolder.PedestrianSettingsConfig;

                var hasLegacyPhysics = false;

                if (generalSettingData.SimulationType != Unity.Physics.SimulationType.NoPhysics || generalSettingData.ForceLegacyPhysics)
                {
                    hasLegacyPhysics = generalSettingData.ForceLegacyPhysics || pedestrianSettings.HasRagdoll;
                }

                Physics.simulationMode = hasLegacyPhysics ? SimulationMode.FixedUpdate : SimulationMode.Script;
            }
            else
            {
                Physics.simulationMode = SimulationMode.FixedUpdate;
            }
        }
    }
}