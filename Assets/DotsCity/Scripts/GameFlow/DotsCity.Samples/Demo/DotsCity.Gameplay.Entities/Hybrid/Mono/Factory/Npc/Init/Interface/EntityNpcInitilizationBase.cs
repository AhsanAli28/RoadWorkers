using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Npc.Authoring
{
    public abstract class EntityNpcInitilizationBase : ICustomEntityNpcInitilization
    {
        public EntityNpcInitilizationBase(EntityManager entityManager)
        {
            EntityManager = entityManager;
        }

        protected EntityManager EntityManager { get; private set; }

        public virtual Entity Spawn(Transform npc)
        {
            return Entity.Null;
        }

        public virtual void BindTransformToEntity(Entity entity, Transform npc)
        {
            EntityManager.AddComponentObject(entity, npc);
            EntityManager.AddComponentObject(entity, npc.GetComponent<Animator>());
        }
    }
}