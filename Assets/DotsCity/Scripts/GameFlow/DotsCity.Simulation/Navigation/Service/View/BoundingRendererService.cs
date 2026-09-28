using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Navigation
{
    public class BoundingRendererService : BoundingRendererServiceBase
    {
        [SerializeField] private BoundingRendererBase prefab;

        private Dictionary<Entity, BoundingRendererBase> binding = new Dictionary<Entity, BoundingRendererBase>();

        private void LateUpdate()
        {
            Tick();
        }

        public void Tick()
        {
            foreach (var item in binding)
            {
                var entity = item.Key;

                if (EntityManager.HasComponent<LocalTransform>(entity))
                {
                    var localtransform = EntityManager.GetComponentData<LocalTransform>(entity);

                    item.Value.Tick(localtransform.Position, localtransform.Rotation);
                }
            }
        }

        public override void Select(Entity entity)
        {
            if (!IsCreated(entity))
            {
                var obj = CreatePrefab();
                obj.SelectEntity(entity);
                binding.Add(entity, obj);
            }
            else
            {
                ResetColor(entity);
            }
        }

        public override void Remove(Entity entity)
        {
            if (IsCreated(entity))
            {
                var data = binding[entity];
                binding.Remove(entity);
                DestroyObj(data);
            }
        }

        public override bool IsCreated(Entity entity) => binding.ContainsKey(entity);

        public override void SetColor(Entity entity, Color color)
        {
            binding[entity].SetColor(color);
        }

        public override void ResetColor(Entity entity)
        {
            binding[entity].ResetColor();
        }

        protected virtual BoundingRendererBase CreatePrefab()
        {
            return Instantiate(prefab, transform);
        }

        protected virtual void DestroyObj(BoundingRendererBase obj)
        {
            Destroy(obj.gameObject);
        }
    }
}
