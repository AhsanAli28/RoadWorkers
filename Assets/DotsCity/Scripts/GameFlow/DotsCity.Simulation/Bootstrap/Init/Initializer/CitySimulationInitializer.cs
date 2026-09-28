using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Config;
using Spirit604.DotsCity.Simulation.Level.Props;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.VFX;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Initialization
{
    public class CitySimulationInitializer : InitializerBase
    {
        [SerializeField] private bool forceNoHybridEntities;

        private GeneralSettingDataSimulation generalSettingDataSimulation;
        private VFXFactory vfxFactory;

        [InjectWrapper]
        public void Construct(
            GeneralSettingDataSimulation generalSettingDataSimulation,
            VFXFactory vfxFactory)
        {
            this.generalSettingDataSimulation = generalSettingDataSimulation;
            this.vfxFactory = vfxFactory;
        }

        public override void Initialize()
        {
            base.Initialize();

            var world = World.DefaultGameObjectInjectionWorld;

            if (generalSettingDataSimulation.DOTSPhysics)
            {
                if (generalSettingDataSimulation.CullPhysics)
                {
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<CullPhysicsSystem, MainThreadEventGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<RevertCulledPhysicsSystem, MainThreadEventGroup>();

                    if (generalSettingDataSimulation.PropsPhysics)
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<RevertCulledPropsPhysicsSystem, MainThreadEventGroup>();

                    if (generalSettingDataSimulation.CullStaticPhysics)
                    {
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<CullStaticPhysicsSystem, CullSimulationGroup>();
                    }
                    else
                    {
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<CleanStaticPhysicsSystem, EarlyJobGroup>();
                    }
                }
            }

            if (generalSettingDataSimulation.PropsDamageSystemSupport)
            {
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<LightPropDamageSystem, LateSimulationGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<LightPropResetSystem, LateEventGroup>();

                var hydrantSystem = DefaultWorldUtils.CreateAndAddSystemManaged<HydrantPropDamageSystem, StructuralSystemGroup>();
                hydrantSystem.Initialize(vfxFactory);

                DefaultWorldUtils.CreateAndAddSystemManaged<HydrantPropResetSystem, DestroyGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<PropsResetSystem, LateEventGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<PropsDamageSystem, LateEventGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<PropsCullSystem, LateEventGroup>();
            }

            if (generalSettingDataSimulation.DOTSSimulation)
            {
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<WorldLightListenerSystem, SimulationGroup>();
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<WorldLightSwitchSystem, SimulationGroup>();
            }

            if (!forceNoHybridEntities)
            {
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<PoolHybridEntitySystem, DestroyGroup>();
            }
        }
    }
}