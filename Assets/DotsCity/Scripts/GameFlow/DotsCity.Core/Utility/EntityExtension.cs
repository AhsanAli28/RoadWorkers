using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Core
{
    public static class EntityExtension
    {
        public static void TryToAddComponent<T>(this EntityManager entityManager, Entity entity) where T : IComponentData
        {
            if (!entityManager.HasComponent<T>(entity))
            {
                entityManager.AddComponent(entity, typeof(T));
            }
        }

        public static void TryToAddComponent(this EntityManager entityManager, Entity entity, ComponentType type)
        {
            if (!entityManager.HasComponent(entity, type))
            {
                entityManager.AddComponent(entity, type);
            }
        }

        public static void TryToRemoveComponent<T>(this EntityManager entityManager, Entity entity) where T : IComponentData
        {
            if (entityManager.HasComponent<T>(entity))
            {
                entityManager.RemoveComponent(entity, typeof(T));
            }
        }

        public static void TryToRemoveComponent(this EntityManager entityManager, Entity entity, ComponentType type)
        {
            if (entityManager.HasComponent(entity, type))
            {
                entityManager.RemoveComponent(entity, type);
            }
        }

        public static void TryToRemoveComponent(this EntityManager entityManager, ref EntityCommandBuffer commandBuffer, Entity entity, ComponentType type)
        {
            if (entityManager.HasComponent(entity, type))
            {
                commandBuffer.RemoveComponent(entity, type);
            }
        }

        public static void AssignChild(
            ref this EntityCommandBuffer commandBuffer,
            ref DynamicBuffer<LinkedEntityGroup> parentLinkedEntityGroup,
            Entity parentEntity,
            Entity childEntity)
        {
            parentLinkedEntityGroup.Add(new LinkedEntityGroup() { Value = childEntity });

            var parent = new Parent()
            {
                Value = parentEntity
            };

            commandBuffer.AddComponent(childEntity, parent);
            commandBuffer.SetComponent(childEntity, LocalTransform.Identity);
        }

        public static void AssignChild(
            ref this EntityManager entityManager,
            Entity parentEntity,
            Entity childEntity)
        {
            var parentLinkedEntityGroup = entityManager.GetBuffer<LinkedEntityGroup>(parentEntity);
            parentLinkedEntityGroup.Add(new LinkedEntityGroup() { Value = childEntity });

            var parent = new Parent()
            {
                Value = parentEntity
            };

            entityManager.AddComponentData(childEntity, parent);
            entityManager.SetComponentData(childEntity, LocalTransform.Identity);
        }
    }
}
