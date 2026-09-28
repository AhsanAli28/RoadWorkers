using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Navigation;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public class PedestrianCustomPathService : EntityCustomPathServiceBase
    {
        [Tooltip("Limit agent count calculation per frame")]
        [SerializeField]
        private int limitAgentCount = 50;

        [Tooltip("Max number of pedestrian nodes")]
        [SerializeField]
        private int bufferSize = 2000;

        [Tooltip("Suitable for infrequent navigation for certain entities, if you plan to use a large amount of entities, disable this option & add <b>PedestrianPathfindingAuthoring</b> component to entity prefab")]
        [SerializeField] private bool allowStructuralChanges = true;

        [SerializeField] private bool registerStatusEvents = true;

        private PedestrianCustomPathEventSystem.Singleton eventSystem;
        private PedestrianCustomPathStructuralSystem.Singleton structuralEventSystem;
        private BlobAssetReference<PathAgentConfig> blobRef;
        private EntityQuery pedestrianQuery;

        public static PedestrianCustomPathService Instance { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
            Initialize();
        }

        private void OnDestroy()
        {
            if (blobRef.IsCreated)
            {
                blobRef.Dispose();
            }
        }

        protected override void SetTargetInternal(Entity entity, Entity endNode)
        {
            eventSystem.FindPath(entity, endNode);
        }

        public override bool EntityHasRouteComponent(Entity entity)
            => EntityManager.HasComponent<NodeSequenceElement>(entity);

        public override bool IsFollowingPath(Entity entity)
        {
            if (EntityHasRouteComponent(entity))
            {
                return !EntityManager.IsComponentEnabled<IdleTag>(entity);
            }

            return false;
        }

        /// <summary>
        /// Activate the car again to drive through the random paths.
        /// </summary>
        public override bool RemoveFollowPath(Entity entity)
        {
            if (EntityHasRouteComponent(entity))
            {
                if (allowStructuralChanges)
                {
                    structuralEventSystem.RemoveComponents(entity);
                }
                else
                {
                    structuralEventSystem.RemoveComponentsNonStructural(entity);
                }

                return true;
            }

            return false;
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

        public override void RecalculateTarget(Entity entity)
        {
            eventSystem.RecalculatePath(entity);
        }

        public DynamicBuffer<NodeSequenceElement> GetPathBuffer(Entity entity)
        {
            return EntityManager.GetBuffer<NodeSequenceElement>(entity, true);
        }

        public override void StartPath(Entity entity)
        {
            eventSystem.StartPath(entity);
        }

        public override NativeArray<Entity> GetAgentEntities(Allocator allocator) => pedestrianQuery.ToEntityArray(allocator);

        protected override bool AddFollowPathComponents(Entity entity)
        {
            if (!EntityHasRouteComponent(entity))
            {
                if (allowStructuralChanges)
                {
                    structuralEventSystem.AddComponents(entity);
                }
                else
                {
                    Debug.LogError("PedestrianCustomPathService. Entity found without pathfinding component make sure you have added <b>PedestrianPathfindingAuthoring</b> to entity pedestrian prefabs otherwise disable <b>allowStructralChanges</b>");
                }

                return true;
            }

            return false;
        }

        private void Initialize()
        {
            pedestrianQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NodeSequenceElement>(), ComponentType.ReadOnly<CustomReachTargetTag>());

            DefaultWorldUtils.CreateAndAddSystemUnmanaged<SequenceNodeReachProccesingSystem, MainThreadEventGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<SequenceReachDestinationSystem, SimulationGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<PedestrianPathfindingSystem, PreEarlyJobGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<PedestrianPathfindingAchieveSystem, MainThreadEventGroup>();

            DefaultWorldUtils.CreateAndAddSystemUnmanaged<PedestrianCustomPathEventSystem, MainThreadEventPlaybackGroup>();

            var handle = DefaultWorldUtils.WorldUnmanaged.GetExistingUnmanagedSystem<PedestrianCustomPathEventSystem>();
            eventSystem = EntityManager.GetComponentData<PedestrianCustomPathEventSystem.Singleton>(handle);

            if (registerStatusEvents)
            {
                var eventInvokerSystem = DefaultWorldUtils.CreateAndAddSystemManaged<PedestrianCustomPathEventInvokerSystem, BeforeDestroyGroup>(true);
                eventInvokerSystem.OnStateUpdated += RaiseStatusUpdate;
            }

            DefaultWorldUtils.CreateAndAddSystemUnmanaged<PedestrianCustomPathStructuralSystem, MainThreadEventPlaybackGroup>();
            var handle2 = DefaultWorldUtils.WorldUnmanaged.GetExistingUnmanagedSystem<PedestrianCustomPathStructuralSystem>();
            structuralEventSystem = EntityManager.GetComponentData<PedestrianCustomPathStructuralSystem.Singleton>(handle2);

            CreateConfig();
        }

        private void CreateConfig()
        {
            var entity = EntityManager.CreateEntity(typeof(PedestrianPathAgentConfigReference));

            blobRef = CreateBlobConfig();

            EntityManager.AddComponentData(entity, new PedestrianPathAgentConfigReference()
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
