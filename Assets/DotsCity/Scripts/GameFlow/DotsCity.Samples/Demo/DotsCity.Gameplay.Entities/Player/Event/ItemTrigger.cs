using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Level
{
    public class ItemTrigger : IEntityTrigger
    {
        private readonly EntityManager entityManager;

        public ItemTrigger(EntityManager entityManager)
        {
            this.entityManager = entityManager;
        }

        public void Process(Entity entity)
        {
            TakeItem(entity);
        }

        private void TakeItem(Entity entity)
        {
            entityManager.SetComponentEnabled<ItemTakenTag>(entity, true);
        }
    }
}