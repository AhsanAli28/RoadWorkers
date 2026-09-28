using Spirit604.Gameplay.Services;
using UnityEngine;

#if ZENJECT
using Zenject;
#endif

namespace Spirit604.Gameplay.Common
{
    public class ProjectCommonInstaller :
#if ZENJECT
        MonoInstaller
#else
        MonoBehaviour
#endif
    {
        [SerializeField] private SceneService sceneService;

        [SerializeField] private DataSaver dataSaver;

        [SerializeField] private PersistDataHolder persistDataHolder;

#if ZENJECT
        public override void InstallBindings()
        {
            base.InstallBindings();

            Container.Bind<SceneService>().FromInstance(sceneService).AsSingle();
            Container.Bind<DataSaver>().FromInstance(dataSaver).AsSingle();
            Container.Bind<PersistDataHolder>().FromInstance(persistDataHolder).AsSingle();
        }
#endif
    }
}