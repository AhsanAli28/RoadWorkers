using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.UI;
using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Player
{
    [UpdateInGroup(typeof(MainThreadInitGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class PlayerInteractCarStateListenerSystem : SystemBase
    {
        private EntityQuery playerGroup;
        private Entity playerInteractCarStateEntity;
        private PlayerInteractCarState previousState = PlayerInteractCarState.Default;
        private PlayerEnterCarStatePresenter playerEnterCarStatePresenter;

        protected override void OnCreate()
        {
            base.OnCreate();

            playerGroup = GetEntityQuery(ComponentType.ReadOnly<PlayerTag>());

            RequireForUpdate(playerGroup);
            RequireForUpdate<PlayerInteractCarStateComponent>();
            Enabled = false;
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            if (playerInteractCarStateEntity == Entity.Null)
            {
                playerInteractCarStateEntity = SystemAPI.GetSingletonEntity<PlayerInteractCarStateComponent>();
            }
        }

        protected override void OnUpdate()
        {
            if (SystemAPI.HasSingleton<PlayerNpcComponent>())
            {
                var playerNpcComponent = SystemAPI.GetSingleton<PlayerNpcComponent>();
                int carIndex = playerNpcComponent.AvailableCarEntityIndex;

                var currentState = carIndex != -1 ? PlayerInteractCarState.CloseToCar : PlayerInteractCarState.OutOfCar;

                if (currentState != previousState)
                {
                    previousState = currentState;
                    playerEnterCarStatePresenter.SwitchPlayerInteractState(currentState);
                }
            }

            var playerInteractCarStateComponent = SystemAPI.GetSingleton<PlayerInteractCarStateComponent>();

            var playerInteractCarState = playerInteractCarStateComponent.PlayerInteractCarState;

            if (playerInteractCarState != PlayerInteractCarState.Default)
            {
                playerEnterCarStatePresenter.SwitchPlayerInteractState(playerInteractCarState);
                EntityManager.SetComponentData(playerInteractCarStateEntity, new PlayerInteractCarStateComponent());
            }
        }

        public void Initialize(PlayerEnterCarStatePresenter playerEnterCarStatePresenter)
        {
            this.playerEnterCarStatePresenter = playerEnterCarStatePresenter;
            Enabled = true;
        }
    }
}