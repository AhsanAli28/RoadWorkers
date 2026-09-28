using Spirit604.DotsCity.Gameplay.Player;
using Spirit604.Gameplay.UI;
using System;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.UI
{
    public class PlayerEnterCarStatePresenter : MonoBehaviour
    {
        [SerializeField] private InteractVehicleButtonsView carInteractButtonsView;

        private PlayerInteractCarStateModel playerStateModel;

        private PlayerInteractCarStateModel PlayerStateModel
        {
            get
            {
                if (playerStateModel == null)
                {
                    var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                    playerStateModel = new PlayerInteractCarStateModel(entityManager);
                }

                return playerStateModel;
            }
        }


        public event Action OnEnter = delegate { };
        public event Action OnExit = delegate { };

        private void Awake()
        {
            carInteractButtonsView.OnEnterClicked += CarInteractButtonsView_OnEnterClicked;
            carInteractButtonsView.OnExitClicked += CarInteractButtonsView_OnExitClicked;
        }

        public void SwitchExitCarButton(bool isActive)
        {
            carInteractButtonsView.SwitchExitCarButton(isActive);
        }

        public void SwitchEnterCarButton(bool isActive)
        {
            carInteractButtonsView.SwitchEnterCarButton(isActive);
        }

        public void SwitchPlayerInteractState(PlayerInteractCarState playerInteractCarState)
        {
            PlayerStateModel.SetInteractState(playerInteractCarState);

            SwitchExitCarButton(false);
            SwitchEnterCarButton(false);

            switch (playerInteractCarState)
            {
                case PlayerInteractCarState.InCar:
                    SwitchExitCarButton(true);
                    break;
                case PlayerInteractCarState.CloseToCar:
                    SwitchEnterCarButton(true);
                    break;
                case PlayerInteractCarState.OutOfCar:
                    break;
            }
        }

        public void InteractCar()
        {
            switch (PlayerStateModel.PlayerInteractCarState)
            {
                case PlayerInteractCarState.InCar:
                    RaiseExitCar();
                    break;
                case PlayerInteractCarState.CloseToCar:
                    RaiseEnterCar();
                    break;
            }
        }

        private void RaiseEnterCar()
        {
            PlayerStateModel.RaiseEnterCar();
            OnEnter();
        }

        private void RaiseExitCar()
        {
            PlayerStateModel.RaiseExitCar();
            OnExit();
        }

        private void CarInteractButtonsView_OnEnterClicked()
        {
            RaiseEnterCar();
        }

        private void CarInteractButtonsView_OnExitClicked()
        {
            RaiseExitCar();
        }
    }
}