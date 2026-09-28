using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Events;
using Spirit604.DotsCity.Events.Authoring;
using Spirit604.DotsCity.Simulation.Binding;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    [TemporaryBakingType]
    public struct PedestrianNodeBakingData : IComponentData
    {
        public Entity LightEntity;
        public Entity TrafficNodeScopeEntity;
        public Entity CrossroadEntity;
        public int PedestrianNodeInstanceId;
        public float3 Position;
    }

    [TemporaryBakingType]
    public struct PedestrianNodeMergeBakingData : IComponentData
    {
        public Entity MergeEntity;
    }

    public class PedestrianNodeAuthoring : MonoBehaviour
    {
        class PedestrianEntityPrefabAuthoringBaker : Baker<PedestrianNodeAuthoring>
        {
            private readonly static HashSet<PedestrianNodeType> IgnoreTypes = new HashSet<PedestrianNodeType>
            {
                PedestrianNodeType.TalkArea,
                PedestrianNodeType.TrafficPublicEntry
            };

            public void CheckConnection(PedestrianNode pedestrianNode)
            {
                if (IgnoreTypes.Contains(pedestrianNode.PedestrianNodeType))
                    return;

                var defaultConnectedPedestrianNodes = pedestrianNode.DefaultConnectedPedestrianNodes;
                var autoConnectedPedestrianNodes = pedestrianNode.AutoConnectedPedestrianNodes;

                var mergeNode = pedestrianNode.TryToGetMergeNode();

                if (mergeNode == null)
                {
                    if (defaultConnectedPedestrianNodes == null || (defaultConnectedPedestrianNodes.Count == 0 && autoConnectedPedestrianNodes.Count == 0))
                    {
                        Debug.Log($"PedestrianNode {pedestrianNode.name} InstanceId {pedestrianNode.GetInstanceID()} doesn't have connections{TrafficObjectFinderMessage.GetMessage()}");
                        return;
                    }
                }
            }

            public override void Bake(PedestrianNodeAuthoring authoring)
            {
                if (!authoring.gameObject.activeInHierarchy)
                    return;

                PedestrianNode pedestrianNode = authoring.GetComponent<PedestrianNode>();

                var entity = this.CreateAdditionalEntityWithBakerRef(pedestrianNode.gameObject);

                Entity lightEntity = TryToGetLightEntity(pedestrianNode);

                var trafficNodeScopeEntity = pedestrianNode.ConnectedTrafficNode != null ? GetEntity(pedestrianNode.ConnectedTrafficNode.gameObject, TransformUsageFlags.Dynamic) : Entity.Null;
                var crossroadEntity = Entity.Null;

                if (lightEntity == Entity.Null && trafficNodeScopeEntity == Entity.Null)
                {
                    var crossroad = authoring.GetComponentInParent<TrafficLightCrossroad>();

                    if (crossroad)
                    {
                        crossroadEntity = GetEntity(crossroad.gameObject, TransformUsageFlags.None);
                    }
                }

                if (trafficNodeScopeEntity == Entity.Null)
                {
                    if (pedestrianNode.PedestrianNodeType == PedestrianNodeType.CarParking || pedestrianNode.PedestrianNodeType == PedestrianNodeType.TrafficPublicStopStation)
                    {
                        Debug.Log($"Node '{pedestrianNode.name}' InstanceId '{pedestrianNode.GetInstanceID()}' Type '{pedestrianNode.PedestrianNodeType}' doesn't have linked traffic node{TrafficObjectFinderMessage.GetMessage()}");
                    }
                }

                AddComponent(entity, new PedestrianNodeBakingData()
                {
                    LightEntity = lightEntity,
                    TrafficNodeScopeEntity = trafficNodeScopeEntity,
                    CrossroadEntity = crossroadEntity,
                    PedestrianNodeInstanceId = pedestrianNode.GetInstanceID(),
                    Position = pedestrianNode.transform.position
                });

                AddComponent(entity, new EntityIDBakingData()
                {
                    Value = pedestrianNode.UniqueID
                });

                ProcessAdditionalSettings(pedestrianNode, entity);

                if (pedestrianNode.CanSpawnInView)
                {
                    AddComponent(entity, typeof(NodeCanSpawnInVisionTag));
                }

                int capacity = pedestrianNode.Capacity;

                int maxCapacity = pedestrianNode.PedestrianNodeType == PedestrianNodeType.CarParking ? 1 : capacity;
                AddComponent(entity, new NodeCapacityComponent { MaxAvailableCount = maxCapacity, CurrentCount = capacity });

                if (capacity >= 0)
                {
                    AddComponent(entity, typeof(NodeHasCapacityOptionTag));
                }

                var connectionBuffer = AddBuffer<NodeConnectionDataElement>(entity);

                int connectionCapacity = pedestrianNode.AutoConnectedPedestrianNodes.Count + pedestrianNode.DefaultConnectedPedestrianNodes.Count;
                connectionBuffer.EnsureCapacity(connectionCapacity);

                float sumWeight = 0;

                ConvertConnectedNodes(entity, pedestrianNode, pedestrianNode.AutoConnectedPedestrianNodes, ref sumWeight);
                ConvertConnectedNodes(entity, pedestrianNode, pedestrianNode.DefaultConnectedPedestrianNodes, ref sumWeight);

                var mergeEntity = TryToGetMergeEntity(pedestrianNode);

                if (mergeEntity != Entity.Null)
                {
                    AddComponent(entity, new PedestrianNodeMergeBakingData()
                    {
                        MergeEntity = mergeEntity,
                    });
                }

                var nodeSettingsComponent = new NodeSettingsComponent()
                {
                    NodeType = pedestrianNode.PedestrianNodeType,
                    NodeShapeType = pedestrianNode.PedestrianNodeShapeType,
                    Weight = pedestrianNode.PriorityWeight,
                    CustomAchieveDistance = pedestrianNode.CustomAchieveDistance,
                    CanSpawnInVision = pedestrianNode.CanSpawnInView ? 1 : 0,
                    ChanceToSpawn = pedestrianNode.ChanceToSpawn,
                    MaxPathWidth = pedestrianNode.MaxPathWidth,
                    Height = pedestrianNode.Height,
                    HasMovementRandomOffset = pedestrianNode.HasMovementRandomOffset ? 1 : 0,
                    SumWeight = sumWeight
                };

                AddComponent(entity, nodeSettingsComponent);

                if (pedestrianNode.PedestrianNodeType == PedestrianNodeType.Trigger || pedestrianNode.PedestrianNodeType == PedestrianNodeType.TriggerAndDestroy)
                {
                    AddComponent<TriggerNodeValidationTag>(entity);

                    AddComponent(entity, new TriggerNodeComponent()
                    {
                        DestroyEntity = pedestrianNode.PedestrianNodeType == PedestrianNodeType.TriggerAndDestroy
                    });
                }

                AddComponent(entity, new NodeLightSettingsComponent()
                {
                    HasCrosswalk = pedestrianNode.ConnectedTrafficNode != null && pedestrianNode.ConnectedTrafficNode.HasCrosswalk,
                    LightEntity = Entity.Null,
                    CrosswalkIndex = -1,
                });

                CheckConnection(pedestrianNode);

                void ConvertConnectedNodes(Entity sourceEntity, PedestrianNode pedestrianNode, IList<PedestrianNode> connectedNodes, ref float sumWeight)
                {
                    for (int i = 0; i < connectedNodes?.Count; i++)
                    {
                        var connectedNode = connectedNodes[i];

                        if (connectedNode == null || !connectedNode.gameObject.activeInHierarchy)
                            continue;

                        if (!connectedNode.CheckConnection(pedestrianNode))
                            continue;

                        if (!connectedNode.HasConnection(pedestrianNode))
                            continue;

                        var connectedEntity = GetEntity(connectedNode.gameObject, TransformUsageFlags.Dynamic);

                        var customData = pedestrianNode.TryToGetConnectionData(connectedNode);

                        if (customData != null && customData.SubNodeCount > 0)
                        {
                            bool isOneway = pedestrianNode.IsOneWayConnection(connectedNode);

                            if (pedestrianNode.GetInstanceID() < connectedNode.GetInstanceID() || isOneway)
                            {
                                var subEntities = new NativeArray<Entity>(customData.SubNodeCount, Allocator.Temp);

                                int subNodeCount = customData.SubNodeCount;

                                for (int j = 0; j < subNodeCount; j++)
                                {
                                    var subNodeEntity = CreateAdditionalEntity(TransformUsageFlags.ManualOverride);

                                    subEntities[j] = subNodeEntity;

                                    var currentSumWeight = sumWeight;

                                    if (!isOneway)
                                    {
                                        currentSumWeight += pedestrianNode.PriorityWeight;
                                    }

                                    var nodeSettingsComponent = new NodeSettingsComponent()
                                    {
                                        NodeType = pedestrianNode.PedestrianNodeType,
                                        NodeShapeType = pedestrianNode.PedestrianNodeShapeType,
                                        Weight = pedestrianNode.PriorityWeight,
                                        CustomAchieveDistance = pedestrianNode.CustomAchieveDistance,
                                        CanSpawnInVision = pedestrianNode.CanSpawnInView ? 1 : 0,
                                        ChanceToSpawn = pedestrianNode.ChanceToSpawn,
                                        MaxPathWidth = pedestrianNode.MaxPathWidth,
                                        Height = pedestrianNode.Height,
                                        HasMovementRandomOffset = pedestrianNode.HasMovementRandomOffset ? 1 : 0,
                                        SumWeight = currentSumWeight
                                    };

                                    AddComponent(subNodeEntity, nodeSettingsComponent);

                                    AddComponent(subNodeEntity, new NodeLightSettingsComponent()
                                    {
                                        HasCrosswalk = true,
                                        LightEntity = Entity.Null,
                                        CrosswalkIndex = -1,
                                    });

                                    AddComponent(subNodeEntity, new NodeCapacityComponent { MaxAvailableCount = -1 });

                                    var subNodePos = pedestrianNode.GetSubNodePosition(connectedNode.transform.position, j, customData.SubNodeCount);

                                    AddComponent(subNodeEntity, LocalTransform.FromPosition(subNodePos));
                                    AddComponent(subNodeEntity, new LocalToWorld());
                                    AddComponent(subNodeEntity, new NodeLinkedTrafficNodeComponent());

                                    AddComponent(subNodeEntity, new PedestrianSubNodeBakingData()
                                    {
                                        SourceEntity = sourceEntity,
                                        TargetEntity = connectedEntity,
                                        Last = j == subNodeCount - 1,
                                        Oneway = isOneway,
                                    });

                                    AddComponent(subNodeEntity, new PedestrianNodeBakingData()
                                    {
                                        Position = subNodePos
                                    });
                                }

                                for (int j = 0; j < subNodeCount; j++)
                                {
                                    var subNodeEntity = subEntities[j];
                                    var subConnectionBuffer = AddBuffer<NodeConnectionDataElement>(subNodeEntity);
                                    subConnectionBuffer.EnsureCapacity(2);

                                    Entity previousEntity = default;
                                    Entity nextEntity = default;

                                    if (j == 0)
                                    {
                                        previousEntity = sourceEntity;
                                    }
                                    else
                                    {
                                        previousEntity = subEntities[j - 1];
                                    }

                                    if (j == subNodeCount - 1)
                                    {
                                        nextEntity = connectedEntity;
                                    }
                                    else
                                    {
                                        nextEntity = subEntities[j + 1];
                                    }

                                    subConnectionBuffer.Add(new NodeConnectionDataElement()
                                    {
                                        ConnectedEntity = nextEntity,
                                        SumWeight = connectedNode.PriorityWeight
                                    });

                                    if (!isOneway)
                                    {
                                        subConnectionBuffer.Add(new NodeConnectionDataElement()
                                        {
                                            ConnectedEntity = previousEntity,
                                            SumWeight = pedestrianNode.PriorityWeight + connectedNode.PriorityWeight
                                        });
                                    }
                                }

                                connectedEntity = subEntities[0];
                                subEntities.Dispose();
                            }
                        }

                        if (connectedEntity != Entity.Null)
                        {
                            sumWeight += connectedNode.PriorityWeight;

                            connectionBuffer.Add(new NodeConnectionDataElement()
                            {
                                ConnectedEntity = connectedEntity,
                                SumWeight = sumWeight
                            });
                        }
                    }
                }
            }

            private Entity TryToGetMergeEntity(PedestrianNode pedestrianNode)
            {
                var mergeNode = pedestrianNode.TryToGetMergeNode();

                if (mergeNode)
                {
                    return GetEntity(mergeNode.gameObject, TransformUsageFlags.Dynamic);
                }

                return Entity.Null;
            }

            private Entity TryToGetLightEntity(PedestrianNode pedestrianNode)
            {
                var lightHandler = pedestrianNode.RelatedTrafficLightHandler;

                if (lightHandler != null &&
                    lightHandler.gameObject.activeSelf &&
                    lightHandler.TrafficLightCrossroad &&
                    lightHandler.TrafficLightCrossroad.HasLights)
                {

                    return GetEntity(pedestrianNode.RelatedTrafficLightHandler.gameObject, TransformUsageFlags.Dynamic);
                }

                return Entity.Null;
            }

            private void ProcessAdditionalSettings(PedestrianNode pedestrianNode, Entity entity)
            {
                int capacity = pedestrianNode.Capacity;

                switch (pedestrianNode.PedestrianNodeType)
                {
                    case PedestrianNodeType.Sit:
                        {
                            PedestrianNodeSeatSettings pedestrianNodeSeatSettings = pedestrianNode.GetComponent<PedestrianNodeSeatSettings>();

                            if (!pedestrianNodeSeatSettings)
                            {
                                pedestrianNodeSeatSettings = pedestrianNode.gameObject.AddComponent<PedestrianNodeSeatSettings>();
                            }

                            AddComponent(entity,
                                new NodeSeatSettingsComponent
                                {
                                    InitialPosition = pedestrianNode.transform.position,
                                    InitialRotation = pedestrianNode.transform.rotation,
                                    SeatsCount = capacity,
                                    BaseOffset = pedestrianNodeSeatSettings.BaseOffset,
                                    SeatOffset = pedestrianNodeSeatSettings.SeatOffset,
                                    EnterSeatOffset = pedestrianNodeSeatSettings.EnterSeatOffset,
                                    SeatHeight = pedestrianNodeSeatSettings.SeatHeight,
                                });

                            var seats = AddBuffer<BenchSeatElement>(entity);
                            seats.EnsureCapacity(capacity);

                            for (int i = 0; i < capacity; i++)
                            {
                                seats.Add(new BenchSeatElement());
                            }

                            break;
                        }
                    case PedestrianNodeType.TalkArea:
                        {
                            AddComponent(entity, typeof(CustomSpawnerTag));

                            AddComponent(entity,
                                new SpawnAreaComponent
                                {
                                    AreaType = pedestrianNode.CurrentPedestrianNodeAreaSettings.areaShapeType,
                                    AreaSize = pedestrianNode.CurrentPedestrianNodeAreaSettings.areaSize,
                                    MinSpawnCount = pedestrianNode.CurrentPedestrianNodeAreaSettings.minSpawnCount,
                                    MaxSpawnCount = pedestrianNode.CurrentPedestrianNodeAreaSettings.maxSpawnCount,
                                });

                            AddComponent(entity,
                                new TalkAreaSettingsComponent
                                {
                                    UnlimitedTalkTime = pedestrianNode.CurrentPedestrianNodeAreaSettings.unlimitedTalkTime ? 1 : 0,
                                    MinTalkTime = pedestrianNode.CurrentPedestrianNodeAreaSettings.minTalkTime,
                                    MaxTalkTime = pedestrianNode.CurrentPedestrianNodeAreaSettings.maxTalkTime,
                                });

                            AddComponent(entity, typeof(NodeTalkAreaTag));

                            AddComponent(entity, typeof(NodeAreaSpawnedTag));
                            AddComponent(entity, typeof(NodeAreaSpawnRequestedTag));

                            this.SetComponentEnabled<NodeAreaSpawnedTag>(entity, false);
                            this.SetComponentEnabled<NodeAreaSpawnRequestedTag>(entity, false);

                            break;
                        }
                    case PedestrianNodeType.TrafficPublicStopStation:
                        {
                            AddBuffer<WaitQueueElement>(entity);
                            AddComponent(entity, typeof(WaitQueueComponent));
                            AddComponent(entity, typeof(NodeProcessWaitQueueTag));
                            this.SetComponentEnabled<NodeProcessWaitQueueTag>(entity, false);

                            float minIdleTime = PedestrianNodeStopStationSettings.MIN_IDLE_TIME;
                            float maxIdleTime = PedestrianNodeStopStationSettings.MAX_IDLE_TIME;

                            var pedestrianNodeStopStationSettings = pedestrianNode.GetComponent<PedestrianNodeStopStationSettings>();

                            if (pedestrianNodeStopStationSettings)
                            {
                                minIdleTime = pedestrianNodeStopStationSettings.IdleDuration.x;
                                maxIdleTime = pedestrianNodeStopStationSettings.IdleDuration.y;
                            }

                            AddComponent(entity, new NodeIdleComponent()
                            {
                                MinIdleTime = minIdleTime,
                                MaxIdleTime = maxIdleTime
                            });

                            break;
                        }
                    case PedestrianNodeType.Idle:
                        {
                            float minIdleTime = PedestrianNodeIdleSettings.MIN_IDLE_TIME;
                            float maxIdleTime = PedestrianNodeIdleSettings.MAX_IDLE_TIME;

                            var pedestrianNodeIdleAuthoring = pedestrianNode.GetComponent<PedestrianNodeIdleSettings>();

                            if (pedestrianNodeIdleAuthoring)
                            {
                                minIdleTime = pedestrianNodeIdleAuthoring.IdleDuration.x;
                                maxIdleTime = pedestrianNodeIdleAuthoring.IdleDuration.y;
                            }

                            AddComponent(entity, new NodeIdleComponent()
                            {
                                MinIdleTime = minIdleTime,
                                MaxIdleTime = maxIdleTime
                            });

                            break;
                        }
                }
            }
        }
    }
}
