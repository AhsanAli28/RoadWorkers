using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Player
{
    public class PlayerInteractCarStateModel
    {
        private EntityManager entityManager;
        private Entity playerEnterCarStateEntity;
        private PlayerInteractCarState playerInteractCarState;

        public PlayerInteractCarState PlayerInteractCarState => playerInteractCarState;

        public PlayerInteractCarStateModel(EntityManager entityManager)
        {
            this.entityManager = entityManager;
            playerEnterCarStateEntity = entityManager.CreateEntity(typeof(PlayerEnterCarStateComponent));
        }

        public void SetInteractState(PlayerInteractCarState playerInteractCarState)
        {
            this.playerInteractCarState = playerInteractCarState;
        }

        public void RaiseEnterCar()
        {
            entityManager.SetComponentData(playerEnterCarStateEntity, new PlayerEnterCarStateComponent { EnterCarState = EnterCarState.Enter });
        }

        public void RaiseExitCar()
        {
            entityManager.SetComponentData(playerEnterCarStateEntity, new PlayerEnterCarStateComponent { EnterCarState = EnterCarState.Leave });
        }
    }
}