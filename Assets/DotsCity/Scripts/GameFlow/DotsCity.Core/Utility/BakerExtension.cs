using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public static class BakerExtension
    {
        public static Entity CreateAdditionalEntityWithBakerRef(this IBaker baker, GameObject sourceObject, TransformUsageFlags transformUsageFlags = TransformUsageFlags.ManualOverride, bool addTransform = true)
        {
            var entity = baker.CreateAdditionalEntity(transformUsageFlags);

            var sourceEntity = baker.GetEntity(sourceObject, transformUsageFlags);

            baker.AddComponent(sourceEntity, new BakerEntityRef()
            {
                LinkedEntity = entity
            });

            if (addTransform)
            {
                baker.AddComponent(entity, typeof(LocalToWorld));
                baker.AddComponent(entity, LocalTransform.FromPositionRotation(sourceObject.transform.position, sourceObject.transform.rotation));
            }

            return entity;
        }

        public static Entity GetEntity(EntityManager entityManager, Entity entity)
        {
            if (entityManager.HasComponent<BakerEntityRef>(entity))
            {
                return entityManager.GetComponentData<BakerEntityRef>(entity).LinkedEntity;
            }

            return entity;
        }
    }
}