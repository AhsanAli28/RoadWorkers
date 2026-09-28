using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Config.Common;
using Spirit604.DotsCity.Gameplay.Factory.Player;
using Spirit604.DotsCity.Gameplay.Npc;
using Spirit604.DotsCity.Gameplay.Player;
using Spirit604.DotsCity.Gameplay.Player.Session;
using Spirit604.DotsCity.Gameplay.Sound.Player;
using Spirit604.DotsCity.Gameplay.Sound.Player.Authoring;
using Spirit604.DotsCity.Gameplay.UI;
using Spirit604.DotsCity.Gameplay.Weapon;
using Spirit604.DotsCity.Simulation;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Gameplay.InputService;
using Spirit604.Gameplay.Npc;
using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Initialization
{
    public class PlayerInitializer : InitializerBase
    {
        private GeneralSettingData generalSettingData;
        private IPlayerInteractCarService playerInteractCarService;
        private ICarConverter carConverter;
        private IPlayerEntityTriggerProccesor playerEntityTriggerProccesor;
        private PlayerActorTracker playerTargetHandler;
        private PlayerCarPool playerCarPool;
        private PlayerEnterCarStatePresenter playerEnterCarStatePresenter;
        private IMotionInput motionInput;
        private ICarMotionInput carMotionInput;
        private IShootTargetProvider targetProvider;
        private PlayerSession playerSession;
        private IPlayerMonoInteractListener playerMonoInteractListener;

        [InjectWrapper]
        public void Construct(
            GeneralSettingData generalSettingData,
            IPlayerInteractCarService playerInteractCarService,
            ICarConverter carConverter,
            IPlayerEntityTriggerProccesor playerEntityTriggerProccesor,
            PlayerActorTracker playerTargetHandler,
            PlayerCarPool playerCarPool,
            PlayerEnterCarStatePresenter playerEnterCarStatePresenter,
            IMotionInput motionInput,
            ICarMotionInput carMotionInput,
            IShootTargetProvider targetProvider,
            PlayerSession playerSession,
            IPlayerMonoInteractListener playerMonoInteractListener = null)
        {
            this.generalSettingData = generalSettingData;
            this.playerInteractCarService = playerInteractCarService;
            this.carConverter = carConverter;
            this.playerEntityTriggerProccesor = playerEntityTriggerProccesor;
            this.playerTargetHandler = playerTargetHandler;
            this.playerCarPool = playerCarPool;
            this.playerEnterCarStatePresenter = playerEnterCarStatePresenter;
            this.motionInput = motionInput;
            this.carMotionInput = carMotionInput;
            this.targetProvider = targetProvider;
            this.playerSession = playerSession;
            this.playerMonoInteractListener = playerMonoInteractListener;
        }

        public override void Initialize()
        {
            base.Initialize();

            var world = World.DefaultGameObjectInjectionWorld;

            playerEnterCarStatePresenter.SwitchPlayerInteractState(PlayerInteractCarState.OutOfCar);

            if (generalSettingData.PlayerSelected)
            {
                if (generalSettingData.BuiltInInteraction)
                {
                    if (generalSettingData.DOTSSimulation)
                    {
                        var playerInteractCarSystem = DefaultWorldUtils.CreateAndAddSystemManaged<PlayerInteractCarSystem, StructuralSystemGroup>();

                        playerInteractCarSystem.Initialize(playerInteractCarService, carConverter);
                        playerInteractCarSystem.Enabled = generalSettingData.PlayerSelected;
                        var playerCarListener = DefaultWorldUtils.CreateAndAddSystemManaged<PlayerInteractCarStateListenerSystem, MainThreadInitGroup>();
                        playerCarListener.Initialize(playerEnterCarStatePresenter);
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<PlayerGetAvailableCarForEnterSystem, HashMapGroup>();
                    }
                    else
                    {

                    }
                }

                if (generalSettingData.BuiltInSolution)
                {
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<CarStoppingEngineSystem, MainThreadEventGroup>(true);
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<CarIgnitionStateSystem, MainThreadEventGroup>(true);

                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<PlayerTargetSystem, LateSimulationGroup>();
                    DefaultWorldUtils.CreateAndAddSystemManaged<PlayerMobTargetSystem, LateSimulationGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<PlayerSoundFootstepsSystem, SimulationGroup>();

                    DefaultWorldUtils.CreateAndAddSystemManaged<CrossHairScaleSystem, StructuralSystemGroup>();

                    DefaultWorldUtils.CreateAndAddSystemManaged<PlayerInitSoundSystem, StructuralInitGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<NpcAnimatorGroundSystem, MainThreadInitGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<NpcControllerSystem, FixedStepGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<NpcGroundStateSystem, SimulationGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<NpcCustomTargetHandlerSystem, SimulationGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<NpcFreezeVerticalRotationSystem, BeginSimulationGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<NpcAnimatorMovementSystem, MainThreadInitGroup>();
                    DefaultWorldUtils.CreateAndAddSystemManaged<NpcResetCustomTargetSystem, StructuralSystemGroup>();
                    DefaultWorldUtils.CreateAndAddSystemManaged<NpcInitializeCustomTargetSystem, StructuralSystemGroup>();
                    DefaultWorldUtils.CreateAndAddSystemManaged<ItemDestroySystem, StructuralSystemGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<NpcInputSystem, SimulationGroup>();
                    DefaultWorldUtils.CreateAndAddSystemUnmanaged<EnemyNpcTargetSystem, SimulationGroup>();

                    if (generalSettingData.DOTSSimulation)
                    {
                        DefaultWorldUtils.CreateAndAddSystemUnmanaged<NpcRaycastGroundSystem, RaycastGroup>();

                        var playerEnterTriggerSystem = DefaultWorldUtils.CreateAndAddSystemManaged<PlayerEnterTriggerSystem, PhysicsTriggerGroup>();

                        playerEnterTriggerSystem.Initialize(playerEntityTriggerProccesor);
                        playerEnterTriggerSystem.Enabled = generalSettingData.PlayerSelected;

                        DefaultWorldUtils.CreateAndAddSystemManaged<MobEnterCarSystem, StructuralSystemGroup>().Initialize(playerTargetHandler);
                        DefaultWorldUtils.CreateAndAddSystemManaged<PlayerInputSystem, InitGroup>().Initialize(motionInput, targetProvider);
                    }

                    DefaultWorldUtils.CreateAndAddSystemManaged<PlayerVehicleInputSystem, MainThreadInitGroup>().Initialize(carMotionInput);
                }

                if (!generalSettingData.DOTSSimulation)
                {
                    DefaultWorldUtils.CreateAndAddSystemManaged<PlayerCarMonoSyncVelocitySystem, MonoSyncGroup>();
                }

                if (generalSettingData.PedestrianTriggerSystemSupport)
                {
                    DefaultWorldUtils.CreateAndAddSystemManaged<PlayerScaryTriggerSystem, BeginSimulationGroup>().Initialize(playerSession);
                }
            }

            playerCarPool.Initialize();
        }
    }
}
