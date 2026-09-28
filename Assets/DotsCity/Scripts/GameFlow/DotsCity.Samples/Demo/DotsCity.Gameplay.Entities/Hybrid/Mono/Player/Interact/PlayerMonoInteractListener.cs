using Spirit604.DotsCity.Gameplay.Player;
using Spirit604.DotsCity.Gameplay.UI;
using Spirit604.DotsCity.Simulation.Car;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay
{
    public class PlayerMonoInteractListener : IPlayerMonoInteractListener
    {
        private GameObject lastCar;
        private GameObject actor;

        private IPlayerInteractCaster playerInteractor;

        private PlayerEnterCarStatePresenter playerEnterCarStatePresenter;
        private PlayerActorTracker playerActorTracker;
        private IPlayerInteractCarService playerInteractCarService;
        private IMonoCarConverter carConverter;

        public PlayerMonoInteractListener(
            PlayerEnterCarStatePresenter playerEnterCarStatePresenter,
            PlayerActorTracker playerActorTracker,
            IPlayerInteractCarService playerInteractCarService,
            IMonoCarConverter carConverter)
        {
            this.playerEnterCarStatePresenter = playerEnterCarStatePresenter;
            this.playerActorTracker = playerActorTracker;
            this.playerInteractCarService = playerInteractCarService;
            this.carConverter = carConverter;

            playerActorTracker.OnSwitchActor += PlayerActorTracker_OnSwitchActor;
            playerEnterCarStatePresenter.OnEnter += PlayerEnterCarStatePresenter_OnEnter;
            playerEnterCarStatePresenter.OnExit += PlayerEnterCarStatePresenter_OnExit;
        }

        private void EnterCar()
        {
            carConverter.EnterCar(lastCar, CarType.Player);

            playerInteractor.SwitchEnabledState(false);

            playerEnterCarStatePresenter.SwitchPlayerInteractState(PlayerInteractCarState.Default);


            playerInteractCarService.EnterCar(lastCar, actor, (npc) =>
            {
                playerEnterCarStatePresenter.SwitchPlayerInteractState(PlayerInteractCarState.InCar);
            });
        }

        private void ExitCar()
        {
            playerEnterCarStatePresenter.SwitchPlayerInteractState(PlayerInteractCarState.Default);

            playerInteractCarService.ExitCar(lastCar, (npc) =>
            {
                npc.GetComponent<IPlayerInteractCaster>().SwitchEnabledState(true);
            });

            carConverter.ExitCar(lastCar, CarType.Player);
        }

        private void PlayerActorTracker_OnSwitchActor(GameObject actor)
        {
            if (playerInteractor != null)
            {
                playerInteractor.OnCastStateChanged -= PlayerInteractCastExample_OnCastStateChanged;
            }

            this.actor = actor;

            if (actor == null)
                return;

            if (actor.TryGetComponent<IPlayerInteractCaster>(out playerInteractor))
            {
                playerInteractor.OnCastStateChanged += PlayerInteractCastExample_OnCastStateChanged;
            }
#if UNITY_EDITOR
            else
            {
                if (actor.TryGetComponent<Animator>(out var animator))
                {
                    UnityEngine.Debug.LogError($"Player character {actor.name} IPlayerInteractCaster is null, use built-in <b>PlayerInteractCaster</b> or implement <b>IPlayerInteractCaster</b> & add component to your player character");
                }
            }
#endif
        }

        private void PlayerInteractCastExample_OnCastStateChanged(GameObject car)
        {
            if (car == null)
            {
                lastCar = null;
                playerEnterCarStatePresenter.SwitchPlayerInteractState(PlayerInteractCarState.Default);
                return;
            }

            if (car.TryGetComponent<IVehicleInput>(out var input))
            {
                lastCar = car;
                playerEnterCarStatePresenter.SwitchPlayerInteractState(PlayerInteractCarState.CloseToCar);
            }
            else
            {
                lastCar = null;
                playerEnterCarStatePresenter.SwitchPlayerInteractState(PlayerInteractCarState.Default);
            }
        }

        private void PlayerEnterCarStatePresenter_OnExit()
        {
            ExitCar();
        }

        private void PlayerEnterCarStatePresenter_OnEnter()
        {
            EnterCar();
        }
    }
}