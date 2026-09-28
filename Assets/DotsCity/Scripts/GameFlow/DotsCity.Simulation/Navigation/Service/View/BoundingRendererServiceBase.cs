using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Navigation
{
    public abstract class BoundingRendererServiceBase : MonoBehaviour
    {
        protected EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        public abstract void Select(Entity entity);
        public abstract void Remove(Entity entity);
        public abstract bool IsCreated(Entity entity);
        public abstract void SetColor(Entity entity, Color color);
        public abstract void ResetColor(Entity entity);
    }
}
