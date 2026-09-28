using Pathfinding.ECS;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Navigation;
using Spirit604.DotsCity.Simulation.Pedestrian;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.Astar
{
    public class AstarPedestrianPathService : AstarPathServiceBase
    {
        [Tooltip("Suitable for infrequent navigation for certain entities, if you plan to use a large amount of entities, disable this option & add <b>TrafficPathfindingAuthoring</b> & <b>AstarAgentAuthoring</b> components to entity prefab")]
        [SerializeField] private bool allowStructuralChanges = true;

        private EntityQuery pedestrianQuery;
        private AstarPedestrianStructuralSystem.Singleton structuralSystem;
        private AstarPedestrianPathEventSystem astarPathEventSystem;

        protected override AstarPathEventSystemBase AstarPathEventSystem => astarPathEventSystem;

        protected override void Awake()
        {
            base.Awake();
            pedestrianQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<CustomReachTargetTag>());
        }

        public override void GetPathPoints(Entity entity, ref List<Vector3> points)
        {
            points.Clear();

            points.Add(EntityManager.GetComponentData<LocalTransform>(entity).Position);
            var buffer = GetPathBuffer(entity);

            var destinationComponent = EntityManager.GetComponentData<DestinationComponent>(entity);

            if (buffer.Length > 0)
            {
                if (buffer[0].Entity != destinationComponent.DestinationNode)
                {
                    points.Add(destinationComponent.Value);
                }
            }
            else
            {
                points.Add(destinationComponent.Value);
            }

            for (int i = 0; i < buffer.Length; i++)
            {
                var pos = EntityManager.GetComponentData<LocalTransform>(buffer[i].Entity).Position;
                points.Add(pos);
            }
        }

        public override bool IsFollowingPath(Entity entity)
        {
            if (EntityHasRouteComponent(entity))
            {
                return !EntityManager.HasComponent<IdleTimeComponent>(entity);
            }

            return false;
        }

        public override bool RemoveFollowPath(Entity entity)
        {
            RemoveEntity(entity);

            if (EntityHasRouteComponent(entity))
            {
                if (allowStructuralChanges)
                {
                    structuralSystem.RemoveComponents(entity);
                }
                else
                {
                    structuralSystem.RemoveComponentsNonStructural(entity);
                }

                return true;
            }

            return true;
        }

        public override void StartPath(Entity entity)
        {
            EntityManager.SetComponentEnabled<CustomReachTargetTag>(entity, true);
            UpdateStatus(entity, PathStatus.OnTheWay);
        }

        public override bool EntityHasRouteComponent(Entity entity)
        {
            return EntityManager.HasComponent<AutoRepathPolicy>(entity);
        }

        public DynamicBuffer<NodeSequenceElement> GetPathBuffer(Entity entity)
        {
            return EntityManager.GetBuffer<NodeSequenceElement>(entity, true);
        }

        public override NativeArray<Entity> GetAgentEntities(Allocator allocator)
        {
            return pedestrianQuery.ToEntityArray(allocator);
        }

        protected override bool AddFollowPathComponents(Entity entity)
        {
            if (!EntityHasRouteComponent(entity))
            {
                structuralSystem.AddComponents(entity);

                if (!allowStructuralChanges)
                {
                    Debug.LogError("AstarPedestrianPathService. Entity found without pathfinding component make sure you have added <b>PedestrianPathfindingAuthoring</b> & <b>AstarAgentAuthoring</b> to entity traffic prefabs otherwise disable <b>allowStructralChanges</b>");
                }

                return true;
            }

            return false;
        }

        protected override void CityBootstrapBase_OnEntityLoaded()
        {
            Initialize();
        }

        private void Initialize()
        {
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<SequenceNodeReachProccesingSystem, MainThreadEventGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<SequenceReachDestinationSystem, SimulationGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<PedestrianPathfindingAchieveSystem, MainThreadEventGroup>();
            var invokerSystem = DefaultWorldUtils.CreateAndAddSystemManaged<PedestrianCustomPathEventInvokerSystem, BeforeDestroyGroup>();
            invokerSystem.OnStateUpdated += RaiseStatusUpdate;

            DefaultWorldUtils.CreateAndAddSystemUnmanaged<AstarPedestrianStructuralSystem, MainThreadEventPlaybackGroup>();

            var handle = DefaultWorldUtils.WorldUnmanaged.GetExistingUnmanagedSystem<AstarPedestrianStructuralSystem>();
            structuralSystem = EntityManager.GetComponentData<AstarPedestrianStructuralSystem.Singleton>(handle);

            if (!allowStructuralChanges)
            {
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<AstarManagedStateInitSystem, StructuralSystemGroup>(true);
            }

            astarPathEventSystem = DefaultWorldUtils.CreateAndAddSystemManaged<AstarPedestrianPathEventSystem, MainThreadEventPlaybackGroup>();
            astarPathEventSystem.Initialize(astarGraph);
        }
    }
}
