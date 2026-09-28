using Pathfinding;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Core.Bootstrap;
using Spirit604.DotsCity.Simulation.Navigation;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.Astar
{
    public abstract class AstarPathServiceBase : EntityCustomPathServiceBase
    {
        [SerializeField] protected AstarEntityGraphBase astarGraph;

        private AstarUpdateStateEventSystem.Singleton stateEventSystem;

        protected abstract AstarPathEventSystemBase AstarPathEventSystem { get; }

        protected override void Awake()
        {
            base.Awake();

            DefaultWorldUtils.CreateAndAddSystemUnmanaged<AstarUpdateStateEventSystem, MainThreadEventPlaybackGroup>(true);

            var handle = DefaultWorldUtils.WorldUnmanaged.GetExistingUnmanagedSystem<AstarUpdateStateEventSystem>();
            stateEventSystem = EntityManager.GetComponentData<AstarUpdateStateEventSystem.Singleton>(handle);

            CityBootstrapBase.OnEntityLoaded += CityBootstrapBase_OnEntityLoaded;

            if (astarGraph && astarGraph is IRuntimeGraph)
            {
                (astarGraph as IRuntimeGraph).OnGraphChanged += RuntimeGraph_OnGraphChanged;
            }
        }

        protected abstract void CityBootstrapBase_OnEntityLoaded();

        public override void RecalculateTarget(Entity entity)
        {
            var targetNode = AstarPathEventSystem.Targets[entity];

            if (NodeExist(targetNode))
            {
                SetFollowPath(entity, targetNode);
            }
            else
            {
                UpdateStatus(entity, PathStatus.TargetNotExist);
            }
        }

        private bool NodeExist(Entity targetNode)
        {
            return EntityManager.HasComponent<LocalTransform>(targetNode);
        }

        protected override void SetTargetInternal(Entity entity, Entity endNode)
        {
            AstarPathEventSystem.SetTarget(entity, endNode, GraphMask.FromGraph(astarGraph.PointGraph));
        }

        protected override void RaiseStatusUpdate(Entity entity, PathStatus status)
        {
            base.RaiseStatusUpdate(entity, status);

            if (status == PathStatus.AgentNotExist)
            {
                RemoveEntity(entity);
            }
        }

        protected void UpdateStatus(Entity entity, PathStatus status)
        {
            stateEventSystem.UpdateState(entity, status);
        }

        protected void RemoveEntity(Entity entity)
        {
            AstarPathEventSystem.RemoveEntity(entity);
        }

        private void RuntimeGraph_OnGraphChanged()
        {
            RecalculateAllTargets();
        }
    }
}
