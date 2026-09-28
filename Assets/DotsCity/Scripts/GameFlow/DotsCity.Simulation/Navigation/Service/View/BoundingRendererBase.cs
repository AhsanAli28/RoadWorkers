using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Navigation
{
    public abstract class BoundingRendererBase : MonoBehaviour
    {
        protected Entity Entity { get; private set; }

        protected EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        public virtual void Tick(Vector3 pos, Quaternion rot)
        {
            UpdatePosInternal(pos, rot);
        }

        public abstract void SwitchActiveState(bool isActive);

        public virtual void SelectEntity(Entity entity)
        {
            Entity = entity;

            InitInternal(entity);

            SwitchActiveState(true);
        }

        public void UnselectEntity(Entity selectedEntity)
        {
            SwitchActiveState(false);
        }

        public abstract void SetColor(Color color);

        public abstract void ResetColor();

        protected abstract void UpdatePosInternal(Vector3 pos, Quaternion rot);

        protected virtual void InitInternal(Entity entity) { }

        protected virtual void DestroyBounds()
        {
            SwitchActiveState(false);
        }
    }
}
