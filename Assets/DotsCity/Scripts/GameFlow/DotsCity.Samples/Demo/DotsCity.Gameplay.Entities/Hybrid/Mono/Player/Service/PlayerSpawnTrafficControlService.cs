using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Player.Spawn;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Binding;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player
{
    public class PlayerSpawnTrafficControlService : MonoBehaviour, IPlayerSpawnerService
    {
        private const float ButtonOffsetDistance = 10f;
        private const string LaneButtonKey = "LaneButtons";

        [SerializeField] private VehicleDataHolder vehicleDataHolder;
        [SerializeField] private TrackingCameraService trackingCameraService;
        [SerializeField] private EntityBindingService entityBindingService;
        [SerializeField] private PlayerActorTracker playerActorTracker;
        [SerializeField] private TrafficNodeEntitySelectorService trafficNodeEntitySelectorService;
        [SerializeField] private int carModel;
        [SerializeField] private EntityWeakRef spawnPoint;

        private Entity currentCarEntity;
        private EntityQuery graphQuery;
        private TrafficChangeLaneConfigReference laneConfig;
        private TrafficCommonSettingsConfigBlobReference commonConfig;
        private bool changingLane;
        private int changingLaneIndex = -1;
        private bool initLaneButtons;
        private List<TrafficNodeEntitySelectorService.SceneEntityData> laneButtons = new List<Core.EntitySceneSelectorServiceBase<Hybrid.Core.WorldInteractView>.SceneEntityData>();
        private Dictionary<Entity, int> nextNodeBinding = new Dictionary<Entity, int>();
        private List<EntitySceneSelectorServiceBase<WorldInteractView>.SceneEntityData> nextNodeButtons = new List<Core.EntitySceneSelectorServiceBase<Hybrid.Core.WorldInteractView>.SceneEntityData>();

        public VehicleDataCollection VehicleDataCollection => vehicleDataHolder.VehicleDataCollection;

        private World DefaultWorld => World.DefaultGameObjectInjectionWorld;
        private EntityManager EntityManager => DefaultWorld.EntityManager;

        private void Awake()
        {
            graphQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PathGraphSystem.Singleton>());
            enabled = false;
        }

        private void Update()
        {
            HandleCar();
        }

        private void OnDestroy()
        {
            entityBindingService.Unsubscribe(spawnPoint);
        }

        public void Initialize()
        {
            entityBindingService.Subscribe(spawnPoint);
            trafficNodeEntitySelectorService.Initialize();
            DefaultWorldUtils.CreateAndAddSystemManaged<PlayerTrafficControlInitSystem, StructuralSystemGroup>().Initialize(this);
            DefaultWorldUtils.CreateAndAddSystemManaged<PlayerTrafficNextSwitchTargetNodeSystem, StructuralSystemGroup>().Initialize(this);
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<PlayerTrafficSwitchTargetNodeSystem, StructuralSystemGroup>();

            laneConfig = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficChangeLaneConfigReference>()).GetSingleton<TrafficChangeLaneConfigReference>();
            commonConfig = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficCommonSettingsConfigBlobReference>()).GetSingleton<TrafficCommonSettingsConfigBlobReference>();
            enabled = true;
        }

        public IEnumerator Spawn()
        {
            if (spawnPoint.Id == 0)
            {
                UnityEngine.Debug.Log("PlayerSpawnTrafficControlService. SpawnPoint not assigned.");
            }

            yield return new WaitWhile(() => !spawnPoint.IsInitialized);

            trackingCameraService.SetPoint(spawnPoint.GetPosition());
            playerActorTracker.Actor = trackingCameraService.TrackingPoint.gameObject;

            StartCoroutine(PlayerTrafficSpawn());
        }

        public IEnumerator PlayerTrafficSpawn()
        {
            var trafficSpawnerSystem = DefaultWorld.GetOrCreateSystemManaged<TrafficSpawnerSystem>();

            yield return new WaitWhile(() => !TrafficSpawnerSystem.IsInitialized);

            var graph = graphQuery.GetSingleton<PathGraphSystem.Singleton>();
            var spawnParams = TrafficSpawnUtils.GetSpawnParams(in graph, EntityManager, spawnPoint.Entity, carModel, TrafficCustomInitType.PlayerControlled);
            trafficSpawnerSystem.Spawn(spawnParams, true);
        }

        public void BindCar(Entity entity)
        {
            currentCarEntity = entity;
            trackingCameraService.SetEntity(entity);

            trafficNodeEntitySelectorService.ClearAll();
            trafficNodeEntitySelectorService.Draw = true;
            UpdateNext();
        }

        public void UpdateNext()
        {
            SetNextEntities();
        }

        private void SetNextEntities()
        {
            var destinationComponent = EntityManager.GetComponentData<TrafficDestinationComponent>(currentCarEntity);
            var graph = graphQuery.GetSingleton<PathGraphSystem.Singleton>();

            var destinationNode = destinationComponent.DestinationNode;

            var nextEntities = trafficNodeEntitySelectorService.GetNextEntities(destinationNode, Allocator.TempJob);

            ReleaseLane();

            nextNodeButtons.Clear();
            nextNodeBinding.Clear();

            for (int i = 0; i < nextEntities.Length; i++)
            {
                var nextNode = nextEntities[i].NextNode;
                nextNodeBinding.Add(nextNode, nextEntities[i].NextPath);
                nextNodeButtons.Add(new Core.EntitySceneSelectorServiceBase<Hybrid.Core.WorldInteractView>.SceneEntityData()
                {
                    BindingID = nextEntities[i].NextNode.Index,
                    Position = graph.GetPositionOnRoad(nextEntities[i].PreviousPath, ButtonOffsetDistance),
                    Rotation = EntityManager.GetComponentData<LocalTransform>(nextNode).Rotation,
                    OnClick = () =>
                    {
                        SetNextDestination(nextNode);
                    }
                });
            }

            trafficNodeEntitySelectorService.Draw = true;
            trafficNodeEntitySelectorService.SetSelectedEntities(nextNodeButtons);

            nextEntities.Dispose();
        }

        private void SetNextDestination(Entity nodeEntity)
        {
            var dest = EntityManager.GetComponentData<TrafficDestinationComponent>(currentCarEntity);

            if (dest.NextDestinationNode != Entity.Null)
                return;

            dest.NextDestinationNode = nodeEntity;
            dest.NextGlobalPathIndex = nextNodeBinding[nodeEntity];

            trafficNodeEntitySelectorService.ClearSelectedEntities();
            trafficNodeEntitySelectorService.Draw = false;

            EntityManager.SetComponentData(currentCarEntity, dest);
        }

        private void HandleCar()
        {
            if (!EntityManager.HasComponent<TrafficPathComponent>(currentCarEntity))
                return;

            var trafficPathComponent = EntityManager.GetComponentData<TrafficPathComponent>(currentCarEntity);

            var graph = graphQuery.GetSingleton<PathGraphSystem.Singleton>();
            var sourcePathIndex = trafficPathComponent.CurrentGlobalPathIndex;

            var paths = graph.GetParallelPaths(sourcePathIndex);

            if (paths.Length <= 0)
                return;

            if (changingLane)
            {
                if (sourcePathIndex == changingLaneIndex)
                {
                    changingLane = false;
                    changingLaneIndex = -1;
                    UpdateNext();
                }
                else
                {
                    return;
                }
            }

            var trafficDestinationComponent = EntityManager.GetComponentData<TrafficDestinationComponent>(currentCarEntity);

            if (trafficDestinationComponent.DistanceToEndOfPath > laneConfig.Config.Value.MaxDistanceToEndOfPath)
            {
                var speedComponent = EntityManager.GetComponentData<SpeedComponent>(currentCarEntity);
                var vehicleTransform = EntityManager.GetComponentData<LocalTransform>(currentCarEntity);

                CheckLaneInit(sourcePathIndex);

                for (int i = 0; i < paths.Length; i++)
                {
                    var targetPathIndex = paths[i];

                    if (TrafficChangeLaneUtils.GetTargetLanePositionAndIndex(
                        speedComponent.Value,
                        vehicleTransform.Position,
                        ref graph,
                        ref laneConfig,
                        ref commonConfig,
                        sourcePathIndex,
                        targetPathIndex,
                        trafficPathComponent.SourceLocalNodeIndex,
                        out var targetPathNodeIndex,
                        out var targetDst))
                    {
                        laneButtons[i].Position = targetDst;
                        laneButtons[i].Rotation = Quaternion.LookRotation(Vector3.Normalize(targetDst - vehicleTransform.Position));
                    }
                    else
                    {
                        laneButtons[i].Position = new Vector3(-1000, 0, -1000);
                    }
                }
            }
            else
            {
                ReleaseLane();
            }
        }

        private void CheckLaneInit(int sourcePathIndex)
        {
            if (initLaneButtons)
                return;

            initLaneButtons = true;
            laneButtons.Clear();

            var trafficPathComponent = EntityManager.GetComponentData<TrafficPathComponent>(currentCarEntity);

            var graph = graphQuery.GetSingleton<PathGraphSystem.Singleton>();

            var paths = graph.GetParallelPaths(sourcePathIndex);

            for (int i = 0; i < paths.Length; i++)
            {
                var targetPathIndex = paths[i];

                var data = new TrafficNodeEntitySelectorService.SceneEntityData()
                {
                    BindingID = sourcePathIndex + paths[i],
                    Rotation = Quaternion.identity,
                    OnClick = () =>
                    {
                        var speedComponent = EntityManager.GetComponentData<SpeedComponent>(currentCarEntity);
                        var vehicleTransform = EntityManager.GetComponentData<LocalTransform>(currentCarEntity);

                        if (TrafficChangeLaneUtils.GetTargetLanePositionAndIndex(
                            speedComponent.Value,
                            vehicleTransform.Position,
                            ref graph,
                            ref laneConfig,
                            ref commonConfig,
                            sourcePathIndex,
                            targetPathIndex,
                            trafficPathComponent.SourceLocalNodeIndex,
                            out var targetPathNodeIndex,
                            out var targetDst))
                        {
                            ReleaseLane();
                            changingLane = true;
                            trafficNodeEntitySelectorService.Draw = false;
                            trafficNodeEntitySelectorService.ClearAll();
                            changingLaneIndex = targetPathIndex;

                            var pathDataHashMap = TrafficNodeResolverSystem.PathDataHashMapStaticRef;

                            pathDataHashMap.TryGetValue(targetPathIndex, out var pathData);

                            var request = new TrafficChangeLaneRequestedPositionComponent()
                            {
                                Destination = targetDst,
                                TargetPathKey = targetPathIndex,
                                TargetPathNodeIndex = targetPathNodeIndex,
                                TargetSourceLaneEntity = pathData.SourceNode
                            };

                            EntityManager.AddComponentData(currentCarEntity, request);
                            EntityManager.AddComponent<TrafficWaitForChangeLaneTag>(currentCarEntity);
                            EntityManager.SetComponentEnabled<TrafficIdleTag>(currentCarEntity, true);

                            var trafficStateComponent = EntityManager.GetComponentData<TrafficStateComponent>(currentCarEntity);

                            trafficStateComponent.TrafficIdleState = DotsEnumExtension.AddFlag(trafficStateComponent.TrafficIdleState, TrafficIdleState.WaitForChangeLane);

                            EntityManager.SetComponentData(currentCarEntity, trafficStateComponent);
                        }
                    }
                };

                laneButtons.Add(data);
            }

            trafficNodeEntitySelectorService.Draw = true;
            trafficNodeEntitySelectorService.SetSelectedEntities(laneButtons, LaneButtonKey);
        }

        private void ReleaseLane()
        {
            if (initLaneButtons)
            {
                initLaneButtons = false;
                laneButtons.Clear();
                trafficNodeEntitySelectorService.ClearSelectedEntities(LaneButtonKey);
            }
        }
    }
}
