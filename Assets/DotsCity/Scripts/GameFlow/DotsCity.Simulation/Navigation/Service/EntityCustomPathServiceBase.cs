using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Navigation
{
    public abstract class EntityCustomPathServiceBase : MonoBehaviour
    {
        protected EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        public event Action<Entity, PathStatus> OnStatusUpdated = delegate { };

        protected virtual void Awake()
        {
        }

        public void SetFollowPath(Entity entity, Entity endNode)
        {
            AddFollowPathComponents(entity);
            SetTargetInternal(entity, endNode);
        }

        public abstract bool RemoveFollowPath(Entity entity);

        public abstract bool EntityHasRouteComponent(Entity entity);

        public abstract bool IsFollowingPath(Entity entity);

        public abstract void GetPathPoints(Entity entity, ref List<Vector3> points);

        public abstract void RecalculateTarget(Entity entity);

        public abstract void StartPath(Entity entity);

        public abstract NativeArray<Entity> GetAgentEntities(Allocator allocator);

        protected abstract void SetTargetInternal(Entity entity, Entity endNode);

        protected abstract bool AddFollowPathComponents(Entity entity);

        public PathStatus GetStatus(Entity entity)
        {
            if (EntityManager.HasComponent<PathAgentComponent>(entity))
                return EntityManager.GetComponentData<PathAgentComponent>(entity).Status;

            return PathStatus.AgentNotExist;
        }

        public void RecalculateAllTargets()
        {
            NativeArray<Entity> entities = GetAgentEntities(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                RecalculateTarget(entities[i]);
            }

            entities.Dispose();
        }

        protected virtual void RaiseStatusUpdate(Entity entity, PathStatus status)
        {
            OnStatusUpdated(entity, status);
        }
    }
}
