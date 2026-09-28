using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

namespace Spirit604.DotsCity.Core.Initialization
{
    public class CitySettingsCoreInitializer : CitySettingsInitializerBase<GeneralSettingDataCore>
    {
        public override void Initialize()
        {
            base.Initialize();
            InitializeStatic(Settings);
        }

        public static void InitializeStatic(GeneralSettingDataCore settings)
        {
            Screen.sleepTimeout = 120;
            SwitchPhysics(settings.SimulationType, settings.DOTSSimulation);
        }

        public static void SwitchPhysics(SimulationType simulationType, bool dotsSimulation)
        {
            var isActive = simulationType != SimulationType.NoPhysics && dotsSimulation;
            var world = World.DefaultGameObjectInjectionWorld;

            world.GetOrCreateSystemManaged<PhysicsSystemGroup>().Enabled = isActive;
        }

        public class GeneralSettingDataCoreBaker : Baker<CitySettingsCoreInitializer>
        {
            public override void Bake(CitySettingsCoreInitializer authoring)
            {
                DependsOn(authoring.Settings);

                var commonGeneralSettingsDataEntity = CreateAdditionalEntity(TransformUsageFlags.None);

                AddComponent(commonGeneralSettingsDataEntity, GeneralCoreSettingsRuntimeAuthoring.CreateConfigStatic(this, authoring.Settings));
            }
        }
    }
}