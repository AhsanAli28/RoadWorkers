using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Binding;
using Spirit604.DotsCity.Simulation.Config;
using UnityEngine;

#if ZENJECT
using Zenject;
#else
using Spirit604.DotsCity.Simulation.Initialization;
#endif

namespace Spirit604.DotsCity.Installer
{
    public class CityCoreInstaller :
#if ZENJECT
        MonoInstaller
#else
        ManualReferenceInstaller
#endif
    {
        [SerializeField] private EntityWorldService entityWorldService;

        [SerializeField] private RuntimeConfigManager runtimeConfigManager;

        [SerializeField] private CitySettingsInitializerBase citySettingsInitializer;

        [SerializeField] private EntityBindingService entityBindingService;

#if !ZENJECT
        [EditorResolve][SerializeField] private CoreSimulationSystemsInitializer coreSimulationInitializer;
#endif

#if ZENJECT

        public override void InstallBindings()
        {
            Container.Bind<IEntityWorldService>().FromInstance(entityWorldService).AsSingle();
            Container.Bind<IRuntimeConfigManager>().FromInstance(runtimeConfigManager).AsSingle();

            Container.Bind<GeneralSettingDataSimulation>().FromInstance(citySettingsInitializer.GetSettings<GeneralSettingDataSimulation>()).AsSingle();
            Container.Bind<EntityBindingService>().FromInstance(entityBindingService).AsSingle();
        }
#else

        public IEntityWorldService EntityWorldService => entityWorldService;
        public GeneralSettingDataSimulation Settings => citySettingsInitializer.GetSettings<GeneralSettingDataSimulation>();

        public override void Resolve()
        {
            coreSimulationInitializer.Construct(entityBindingService);
        }

#endif
    }
}
