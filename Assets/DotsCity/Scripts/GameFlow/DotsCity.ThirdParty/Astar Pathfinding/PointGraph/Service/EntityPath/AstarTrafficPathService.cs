using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation;
using Spirit604.DotsCity.Simulation.Navigation;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.DotsCity.Simulation.TrafficPublic;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.Astar
{
    public class AstarTrafficPathService : AstarPathServiceBase
    {
        [Tooltip("Suitable for infrequent navigation for certain entities, if you plan to use a large amount of entities, disable this option & add <b>TrafficPathfindingAuthoring</b> & <b>AstarAgentAuthoring</b> components to entity prefab")]
        [SerializeField] private bool allowStructuralChanges = true;

        private EntityQuery trafficQuery;
        private AstarTrafficStructuralSystem.Singleton structuralSystem;
        private AstarTrafficPathEventSystem astarPathEventSystem;

        protected override AstarPathEventSystemBase AstarPathEventSystem => astarPathEventSystem;

        protected override void Awake()
        {
            base.Awake();
            trafficQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficFixedRouteTag>());
        }

        public override void GetPathPoints(Entity entity, ref List<Vector3> points)
        {
            points.Clear();

            var buffer = GetPathBuffer(entity);

            for (int i = 0; i < buffer.Length; i++)
            {
                points.Add(buffer[i].Position);
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

        /// <summary>
        /// Activate the car again to drive through the random paths.
        /// </summary>
        public override bool RemoveFollowPath(Entity trafficEntity)
        {
            RemoveEntity(trafficEntity);

            if (EntityHasRouteComponent(trafficEntity))
            {
                if (allowStructuralChanges)
                {
                    structuralSystem.RemoveComponents(trafficEntity);
                }
                else
                {
                    structuralSystem.RemoveComponentsNonStructural(trafficEntity);
                }

                return true;
            }

            return false;
        }

        public DynamicBuffer<FixedRouteNodeElement> GetPathBuffer(Entity entity)
        {
            return EntityManager.GetBuffer<FixedRouteNodeElement>(entity, true);
        }

        protected override bool AddFollowPathComponents(Entity trafficEntity)
        {
            if (!EntityHasRouteComponent(trafficEntity))
            {
                structuralSystem.AddComponents(trafficEntity);

                if (!allowStructuralChanges)
                {
                    Debug.LogError("AstarTrafficPathService. Entity found without pathfinding component make sure you have added <b>TrafficPathfindingAuthoring</b> & <b>AstarAgentAuthoring</b> to entity traffic prefabs otherwise disable <b>allowStructralChanges</b>");
                }

                return true;
            }
            else
            {
                if (!allowStructuralChanges)
                {
                    structuralSystem.AddComponentsNonStructural(trafficEntity);
                    return true;
                }
            }

            return false;
        }

        public override bool EntityHasRouteComponent(Entity entity) => EntityManager.HasComponent<TrafficFixedRouteTag>(entity);

        public override void StartPath(Entity entity)
        {
            UpdateStatus(entity, PathStatus.OnTheWay);
        }

        public override NativeArray<Entity> GetAgentEntities(Allocator allocator) => trafficQuery.ToEntityArray(allocator);

        protected override void CityBootstrapBase_OnEntityLoaded()
        {
            Initialize();
        }

        private void Initialize()
        {
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficPathfindingAchieveSystem, TrafficProcessNodeGroup>();
            var invokerSystem = DefaultWorldUtils.CreateAndAddSystemManaged<TrafficCustomPathEventInvokerSystem, BeforeDestroyGroup>();
            invokerSystem.OnStateUpdated += RaiseStatusUpdate;

            DefaultWorldUtils.CreateAndAddSystemUnmanaged<AstarTrafficStructuralSystem, MainThreadEventPlaybackGroup>();

            var handle = DefaultWorldUtils.WorldUnmanaged.GetExistingUnmanagedSystem<AstarTrafficStructuralSystem>();
            structuralSystem = EntityManager.GetComponentData<AstarTrafficStructuralSystem.Singleton>(handle);

            if (!allowStructuralChanges)
            {
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<AstarManagedStateInitSystem, StructuralSystemGroup>(true);
            }

            astarPathEventSystem = DefaultWorldUtils.CreateAndAddSystemManaged<AstarTrafficPathEventSystem, MainThreadEventPlaybackGroup>();
            astarPathEventSystem.Initialize(astarGraph);
        }
    }
}
