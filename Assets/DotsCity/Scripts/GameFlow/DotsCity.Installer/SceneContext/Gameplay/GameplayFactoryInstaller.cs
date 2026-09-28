using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Factory;
using Spirit604.DotsCity.Gameplay.Npc.Authoring;
using Spirit604.Gameplay.Factory;
using UnityEngine;

#if ZENJECT
using Zenject;
#else
using Spirit604.DotsCity.Gameplay.Npc.Factory;
using Spirit604.Gameplay.Factory.Npc;
using Spirit604.Extensions;
using System.Collections.Generic;
using System.Linq;
#endif

namespace Spirit604.DotsCity.Installer
{
    public class GameplayFactoryInstaller :
#if ZENJECT
        MonoInstaller
#else
        ManualReferenceInstaller
#endif
    {
        [SerializeField] private CitySettingsInitializerBase citySettingsInitializer;

        [SerializeField] private NpcEntityFactory npcEntityFactory;

        [SerializeField] private BulletEntityFactory bulletEntityFactory;

        [SerializeField] private BulletFactory bulletFactory;

        [SerializeField] private WeaponFactory weaponFactory;

        private IBulletFactory BulletFactory => citySettingsInitializer.GetSettings<GeneralSettingDataCore>().DOTSSimulation ? bulletEntityFactory : bulletFactory;

#if !ZENJECT
        [Header("Resolve")]
        [EditorResolve][SerializeField] private List<NpcHybridEntityFactoryBase> npcFactories = new List<NpcHybridEntityFactoryBase>();
        [EditorResolve][SerializeField] private List<NpcInCarFactoryBase> npcInCarFactories = new List<NpcInCarFactoryBase>();
        [EditorResolve][SerializeField] private List<NpcHybridMonoFactoryBase> npcHybridFactories = new List<NpcHybridMonoFactoryBase>();
        [EditorResolve][SerializeField] private List<NpcMonoFactoryBase> npcMonoFactories = new List<NpcMonoFactoryBase>();
        [EditorResolve][SerializeField] private PlayerMonoNpcFactory playerMonoNpcFactory;

        [Header("Additional Refs")]
        [EditorResolve][SerializeField] private CityCoreInstaller coreInstaller;
        [EditorResolve][SerializeField] private GameplayUIInstaller gameplayUIInstaller;
        [EditorResolve][SerializeField] private SoundInstaller soundInstaller;
#endif

#if ZENJECT
        public override void InstallBindings()
        {
            BindNpc();
            BindWeapon();
        }

        private void BindNpc()
        {
            Container.Bind<NpcEntityFactory>().FromInstance(npcEntityFactory).AsSingle();
        }

        private void BindWeapon()
        {
            Container.Bind<IBulletFactory>().FromInstance(BulletFactory).AsSingle();
            Container.Bind<WeaponFactory>().FromInstance(weaponFactory).AsSingle();
        }

#else
        public override void Resolve()
        {
            for (int i = 0; i < npcFactories.Count; i++)
            {
                npcFactories[i].Construct(coreInstaller.EntityWorldService, npcEntityFactory, weaponFactory, BulletFactory, soundInstaller.SoundPlayer);
            }

            for (int i = 0; i < npcInCarFactories.Count; i++)
            {
                npcInCarFactories[i].Construct(weaponFactory, BulletFactory, soundInstaller.SoundPlayer);
            }

            for (int i = 0; i < npcHybridFactories.Count; i++)
            {
                npcHybridFactories[i].Construct(coreInstaller.EntityWorldService);
            }

            for (int i = 0; i < npcMonoFactories.Count; i++)
            {
                npcMonoFactories[i].Construct(weaponFactory, BulletFactory, soundInstaller.SoundPlayer);
            }

            playerMonoNpcFactory.Construct(gameplayUIInstaller.Input, gameplayUIInstaller.PlayerShootTargetProvider, weaponFactory, BulletFactory, soundInstaller.SoundPlayer);
        }

#if UNITY_EDITOR
        protected override void CustomInspectorRebind(GameObject root)
        {
            npcFactories = ResolveRefsInternal<NpcHybridEntityFactoryBase>(root).ToList();
            npcInCarFactories = ResolveRefsInternal<NpcInCarFactoryBase>(root).ToList();
            npcHybridFactories = ResolveRefsInternal<NpcHybridMonoFactoryBase>(root).ToList();
            npcMonoFactories = ResolveRefsInternal<NpcMonoFactoryBase>(root).ToList();
            EditorSaver.SetObjectDirty(this);
        }
#endif
#endif
    }
}

