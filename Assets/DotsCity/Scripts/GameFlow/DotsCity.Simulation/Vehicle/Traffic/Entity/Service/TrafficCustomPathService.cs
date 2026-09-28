using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Initialization;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.DotsCity.Simulation.Navigation;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.TrafficPublic;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public class TrafficCustomPathService : EntityCustomPathServiceBase
    {
        [Tooltip("Limit agent count calculation per frame")]
        [SerializeField]
        private int limitAgentCount = 50;

        [Tooltip("Max number of paths")]
        [SerializeField]
        private int bufferSize = 2000;

        [Tooltip("Suitable for infrequent navigation for certain entities, if you plan to use a large amount of entities, disable this option & add <b>TrafficPathfindingAuthoring</b> component to entity prefab")]
        [SerializeField] private bool allowStructuralChanges = true;

        [SerializeField]
        private bool registerStatusEvents = true;

        public static TrafficCustomPathService Instance { get; private set; }

        private EntityQuery graphQuery;
        private EntityQuery entityGraphQuery;
        private EntityQuery trafficQuery;
        private TrafficCustomPathEventSystem.Singleton eventSystem;
        private TrafficCustomPathStructuralSystem.Singleton structuralEventSystem;
        private BlobAssetReference<PathAgentConfig> blobRef;

        protected override void Awake()
        {
            base.Awake();
            graphQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PathGraphSystem.Singleton>());
            entityGraphQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficNodeResolverSystem.RuntimePathDataRef>());
            trafficQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficFixedRouteTag>());
            Initialize();
            Instance = this;
        }

        private void OnDestroy()
        {
            if (blobRef.IsCreated)
                blobRef.Dispose();
        }

        public void SetFollowPath(Entity trafficEntity, int endPath)
        {
            var graph = GetEntityGraph();
            var endNode = graph.TryToGetConnectedNode(endPath);

            SetFollowPath(trafficEntity, endNode);
        }

        public override bool EntityHasRouteComponent(Entity entity)
            => EntityManager.HasComponent<TrafficFixedRouteTag>(entity);

        public override bool IsFollowingPath(Entity entity)
        {
            if (EntityHasRouteComponent(entity))
            {
                return !EntityManager.HasComponent<TrafficNoTargetTag>(entity);
            }

            return false;
        }

        public DynamicBuffer<FixedRouteNodeElement> GetPathBuffer(Entity entity)
        {
            return EntityManager.GetBuffer<FixedRouteNodeElement>(entity, true);
        }

        /// <summary>
        /// Activate the car again to drive through the random paths.
        /// </summary>
        public override bool RemoveFollowPath(Entity trafficEntity)
        {
            if (EntityHasRouteComponent(trafficEntity))
            {
                if (allowStructuralChanges)
                {
                    structuralEventSystem.RemoveComponents(trafficEntity);
                }
                else
                {
                    structuralEventSystem.RemoveComponentsNonStructural(trafficEntity);
                }

                return true;
            }

            return false;
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

        public override void RecalculateTarget(Entity entity)
        {
            eventSystem.RecalculatePath(entity);
        }

        public override void StartPath(Entity entity)
        {
            eventSystem.StartPath(entity);
        }

        public override NativeArray<Entity> GetAgentEntities(Allocator allocator) => trafficQuery.ToEntityArray(allocator);

        public PathGraphSystem.Singleton GetRoadGraph() => graphQuery.GetSingleton<PathGraphSystem.Singleton>();

        public TrafficNodeResolverSystem.RuntimePathDataRef GetEntityGraph() => entityGraphQuery.GetSingleton<TrafficNodeResolverSystem.RuntimePathDataRef>();

        protected override void SetTargetInternal(Entity entity, Entity endNode)
        {
            eventSystem.FindPath(entity, endNode);
        }

        protected override bool AddFollowPathComponents(Entity trafficEntity)
        {
            if (!EntityHasRouteComponent(trafficEntity))
            {
                structuralEventSystem.AddComponents(trafficEntity);

                if (!allowStructuralChanges)
                {
                    Debug.LogError("TrafficCustomPathService. Entity found without pathfinding component make sure you have added <b>TrafficPathfindingAuthoring</b> to entity traffic prefabs otherwise disable <b>allowStructralChanges</b>");
                }

                return true;
            }
            else
            {
                if (!allowStructuralChanges)
                {
                    structuralEventSystem.AddComponentsNonStructural(trafficEntity);
                    return true;
                }
            }

            return false;
        }

        private void Initialize()
        {
            TrafficInitializer.RegisterFixedRouteSystems();

            DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficPathfindingSystem, TrafficSimulationGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficPathfindingAchieveSystem, TrafficProcessNodeGroup>();

            DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficCustomPathEventSystem, MainThreadEventPlaybackGroup>();

            var handle = DefaultWorldUtils.WorldUnmanaged.GetExistingUnmanagedSystem<TrafficCustomPathEventSystem>();
            eventSystem = EntityManager.GetComponentData<TrafficCustomPathEventSystem.Singleton>(handle);

            if (registerStatusEvents)
            {
                var eventInvokerSystem = DefaultWorldUtils.CreateAndAddSystemManaged<TrafficCustomPathEventInvokerSystem, BeforeDestroyGroup>(true);
                eventInvokerSystem.OnStateUpdated += RaiseStatusUpdate;
            }

            DefaultWorldUtils.CreateAndAddSystemUnmanaged<TrafficCustomPathStructuralSystem, MainThreadEventPlaybackGroup>();
            var handle2 = DefaultWorldUtils.WorldUnmanaged.GetExistingUnmanagedSystem<TrafficCustomPathStructuralSystem>();
            structuralEventSystem = EntityManager.GetComponentData<TrafficCustomPathStructuralSystem.Singleton>(handle2);

            CreateConfig();
        }

        private void CreateConfig()
        {
            var entity = EntityManager.CreateEntity(typeof(TrafficPathAgentConfigReference));

            blobRef = CreateBlobConfig();

            EntityManager.AddComponentData(entity, new TrafficPathAgentConfigReference()
            {
                Config = blobRef
            });
        }

        private BlobAssetReference<PathAgentConfig> CreateBlobConfig()
        {
            using (var builder = new BlobBuilder(Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<PathAgentConfig>();

                root.AgentCount = limitAgentCount;
                root.BufferSize = bufferSize;

                var blobRef = builder.CreateBlobAssetReference<PathAgentConfig>(Allocator.Persistent);

                return blobRef;
            }
        }
    }
}
