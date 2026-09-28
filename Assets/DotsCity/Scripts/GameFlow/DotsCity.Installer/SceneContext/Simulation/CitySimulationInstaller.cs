using Spirit604.DotsCity.Simulation.VFX;
using UnityEngine;

#if ZENJECT
using Zenject;
#else
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Config;
using Spirit604.DotsCity.Simulation.Initialization;
#endif

namespace Spirit604.DotsCity.Installer
{
    public class CitySimulationInstaller :
#if ZENJECT
        MonoInstaller
#else
        ManualReferenceInstaller
#endif
    {
        [SerializeField] private VFXFactory vfxFactory;

#if !ZENJECT

        [Header("Resolve")]
        [EditorResolve][SerializeField] private CitySimulationInitializer citySimulationInitializer;

        [Header("Additional Refs")]
        [EditorResolve][SerializeField] private CitySettingsInitializerBase citySettingsSimulationInitializer;
        [EditorResolve][SerializeField] private SoundInstaller soundInstaller;
#endif

#if ZENJECT
        public override void InstallBindings()
        {
            Container.Bind<VFXFactory>().FromInstance(vfxFactory).AsSingle();
        }

#else
        public override void Resolve()
        {
            citySimulationInitializer.Construct(citySettingsSimulationInitializer.GetSettings<GeneralSettingDataSimulation>(), vfxFactory);
        }
#endif
    }
}

