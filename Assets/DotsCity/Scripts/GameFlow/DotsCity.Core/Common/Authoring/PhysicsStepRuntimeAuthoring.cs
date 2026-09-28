using Spirit604.Attributes;
using Spirit604.DotsCity.Core.Initialization;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public class PhysicsStepRuntimeAuthoring : RuntimeConfigUpdater<PhysicsStep>
    {
        [ShowIfNull]
        [SerializeField] private CitySettingsInitializerBase settingsInitializer;

        private PhysicsStep defaultPhysicsStep;

        protected override bool IgnoreExist => true;
        protected override bool AutoSync => false;

        public override void Initialize(bool recreateOnStart)
        {
            base.Initialize(recreateOnStart);

            if (ConfigEntity != Entity.Null)
            {
                defaultPhysicsStep = EntityManager.GetComponentData<PhysicsStep>(ConfigEntity);
            }
            else
            {
                defaultPhysicsStep = PhysicsStep.Default;
            }
        }

        public override PhysicsStep CreateConfig()
        {
            if (settingsInitializer)
                defaultPhysicsStep.SimulationType = settingsInitializer.GetSettings<GeneralSettingDataCore>().SimulationType;

            return defaultPhysicsStep;
        }

        protected override void OnConfigUpdatedInternal()
        {
            base.OnConfigUpdatedInternal();

            if (settingsInitializer)
                CitySettingsCoreInitializer.SwitchPhysics(defaultPhysicsStep.SimulationType, settingsInitializer.GetSettings<GeneralSettingDataCore>().DOTSSimulation);
        }
    }
}
