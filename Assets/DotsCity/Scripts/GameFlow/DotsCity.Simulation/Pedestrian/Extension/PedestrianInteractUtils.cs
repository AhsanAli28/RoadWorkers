using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    /// <summary>
    /// Class when the user wants to temporarily or permanently take full control of the pedestrian for customised movement, state, etc.
    /// </summary>
    public static class PedestrianInteractUtils
    {
        [Flags]
        public enum ComponentFlags
        {
            None = 0,
            DisableUnloadSkinTag = 1 << 0,
            HybridSkinTemporaryEnabled = 1 << 1,
            CopyFromGameobjectAdded = 1 << 2,
        }

        /// <summary>
        /// Contains temporary info for disabled pedestrian simulation.
        /// </summary>
        public struct PedestrianInteractTempInfo : IComponentData
        {
            public ComponentFlags ComponentFlags;
        }

        /// <summary>
        /// Remove the pedestrian entity from the DOTS simulation. All custom states, locomotion & animation should be handled by custom user code using monobehavior scripts.
        /// </summary>
        public static bool RemoveFromSimulation(Entity entity)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var activated = RemoveFromSimulation(entity, ref commandBuffer, ref entityManager);
            commandBuffer.Playback(entityManager);
            commandBuffer.Dispose();

            return activated;
        }

        /// <summary>
        /// Remove the pedestrian entities from the DOTS simulation. All custom states, locomotion & animation should be handled by custom user code using monobehavior scripts.
        /// </summary>
        public static void RemoveFromSimulation(List<Entity> entities)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            for (int i = 0; i < entities.Count; i++)
            {
                RemoveFromSimulation(entities[i], ref commandBuffer, ref entityManager);
            }

            commandBuffer.Playback(entityManager);
            commandBuffer.Dispose();
        }

        /// <summary>
        /// Remove the pedestrian entity from the DOTS simulation. All custom states, locomotion & animation should be handled by custom user code using monobehavior scripts.
        /// </summary>
        public static bool RemoveFromSimulation(Entity entity, ref EntityCommandBuffer commandBuffer, ref EntityManager entityManager)
        {
            if (entityManager.HasComponent<PedestrianInteractTempInfo>(entity))
                return false;

            if (!entityManager.HasComponent<HasTargetTag>(entity))
                return false;

            commandBuffer.AddComponent<CustomMovementTag>(entity);
            commandBuffer.AddComponent<CustomLocomotionTag>(entity);
            commandBuffer.AddComponent<CustomAnimatorStateTag>(entity);
            commandBuffer.SetComponentEnabled<HasTargetTag>(entity, false);

            var componentFlags = ComponentFlags.None;

            if (!entityManager.HasComponent<DisableUnloadSkinTag>(entity))
            {
                componentFlags |= ComponentFlags.DisableUnloadSkinTag;
                commandBuffer.AddComponent<DisableUnloadSkinTag>(entity);
            }

            if (entityManager.HasComponent<HybridGPUSkinTag>(entity) && entityManager.HasComponent<PreventHybridSkinTagTag>(entity) && entityManager.IsComponentEnabled<PreventHybridSkinTagTag>(entity))
            {
                commandBuffer.SetComponentEnabled<PreventHybridSkinTagTag>(entity, false);
                componentFlags |= ComponentFlags.HybridSkinTemporaryEnabled;
            }

            if (entityManager.HasComponent<CopyTransformToGameObject>(entity))
            {
                commandBuffer.SetComponentEnabled<CopyTransformToGameObject>(entity, false);

                if (!entityManager.HasComponent<CopyTransformFromGameObject>(entity))
                {
                    componentFlags |= ComponentFlags.CopyFromGameobjectAdded;
                    commandBuffer.AddComponent<CopyTransformFromGameObject>(entity);
                }
            }

            commandBuffer.AddComponent(entity, new PedestrianInteractTempInfo()
            {
                ComponentFlags = componentFlags,
            });

            return true;
        }

        /// <summary>
        /// Return the entity to the simulation.
        /// </summary>
        public static bool RestoreToSimulation(Entity entity)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var activated = RestoreToSimulation(entity, ref commandBuffer, ref entityManager);
            commandBuffer.Playback(entityManager);
            commandBuffer.Dispose();

            return activated;
        }

        /// <summary>
        /// Return the list of entities to the simulation.
        /// </summary>
        public static void RestoreToSimulation(List<Entity> entities)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            for (int i = 0; i < entities.Count; i++)
            {
                RestoreToSimulation(entities[i], ref commandBuffer, ref entityManager);
            }

            commandBuffer.Playback(entityManager);
            commandBuffer.Dispose();
        }

        /// <summary>
        /// Return the entity to the simulation.
        /// </summary>
        public static bool RestoreToSimulation(Entity entity, ref EntityCommandBuffer commandBuffer, ref EntityManager entityManager)
        {
            if (!entityManager.HasComponent<PedestrianInteractTempInfo>(entity))
                return false;

            commandBuffer.RemoveComponent<CustomMovementTag>(entity);
            commandBuffer.RemoveComponent<CustomLocomotionTag>(entity);
            commandBuffer.RemoveComponent<CustomAnimatorStateTag>(entity);
            commandBuffer.SetComponentEnabled<MovementStateChangedEventTag>(entity, true);
            commandBuffer.SetComponentEnabled<HasTargetTag>(entity, true);

            var interactInfo = entityManager.GetComponentData<PedestrianInteractTempInfo>(entity);

            if (interactInfo.ComponentFlags.HasFlag(ComponentFlags.DisableUnloadSkinTag))
            {
                commandBuffer.RemoveComponent<DisableUnloadSkinTag>(entity);
            }

            if (interactInfo.ComponentFlags.HasFlag(ComponentFlags.HybridSkinTemporaryEnabled))
            {
                commandBuffer.SetComponentEnabled<PreventHybridSkinTagTag>(entity, true);
            }

            if (entityManager.HasComponent<CopyTransformToGameObject>(entity))
            {
                commandBuffer.SetComponentEnabled<CopyTransformToGameObject>(entity, true);
            }

            if (interactInfo.ComponentFlags.HasFlag(ComponentFlags.CopyFromGameobjectAdded))
            {
                commandBuffer.RemoveComponent<CopyTransformFromGameObject>(entity);
            }

            commandBuffer.RemoveComponent<PedestrianInteractTempInfo>(entity);

            return true;
        }
    }
}