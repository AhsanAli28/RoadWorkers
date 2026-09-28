using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.DotsCity.Simulation.Level.Streaming.Authoring;
using Spirit604.DotsCity.Simulation.Pedestrian.Authoring;
using Spirit604.Extensions;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup), OrderFirst = true)]
    public partial class TrafficSectionConversionSystem : SimpleSystemBase
    {
        private struct SectionData
        {
            public Entity Entity;
            public float3 Position;
        }

        private const int StartSectionIndex = 1;

        private int sectionIndex = StartSectionIndex;
        private NativeHashMap<int, int> sectionIndexMapping; //hash - scene section index
        private NativeParallelMultiHashMap<int, SectionData> sectionEntityMapping; //hash - local section
        private NativeHashMap<int, Entity> crossroadBinding;
        private EntityQuery sectionEntityQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<CrossroadBakingData>();
            RequireForUpdate<PedestrianNodeBakingData>();
            RequireForUpdate<RoadSceneData>();
            RequireForUpdate<RoadStreamingConfigReference>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (sectionIndexMapping.IsCreated)
            {
                sectionIndexMapping.Dispose();
            }

            if (sectionEntityMapping.IsCreated)
            {
                sectionEntityMapping.Dispose();
            }

            if (crossroadBinding.IsCreated)
            {
                crossroadBinding.Dispose();
            }

            sectionEntityQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SectionMetadataSetup>().Build(this);
        }

        protected override void OnUpdate()
        {
            sectionIndex = StartSectionIndex;

            if (!sectionIndexMapping.IsCreated)
            {
                sectionIndexMapping = new NativeHashMap<int, int>(1000, Allocator.Persistent);
            }

            if (!sectionEntityMapping.IsCreated)
            {
                sectionEntityMapping = new NativeParallelMultiHashMap<int, SectionData>(1000, Allocator.Persistent);
            }

            if (!crossroadBinding.IsCreated)
            {
                crossroadBinding = new NativeHashMap<int, Entity>(1000, Allocator.Persistent);
            }

            sectionIndexMapping.Clear();
            sectionEntityMapping.Clear();

            var roadSceneData = SystemAPI.GetSingleton<RoadSceneData>();
            var roadStreamingConfig = SystemAPI.GetSingleton<RoadStreamingConfigReference>().Config.Value;

            ConvertCrossroadSections(roadSceneData, roadStreamingConfig);

            if (roadStreamingConfig.StreamingIsEnabled)
            {
                ConvertPedestrianSections(roadSceneData, roadStreamingConfig);
                ConvertCustomSectionObject(roadSceneData, roadStreamingConfig);
            }
        }

        private void ConvertCrossroadSections(RoadSceneData roadSceneData, RoadStreamingConfig roadStreamingConfig)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
            .WithoutBurst()
            .WithNone<CrossroadSubSegmentTag>()
            .WithStructuralChanges()
            .ForEach((Entity crossRoadEntity, ref CrossroadBakingData crossroadData, ref SegmentComponent crossroadComponent) =>
            {
                var hash = HashMapHelper.GetHashMapPosition(crossroadData.Position.Flat(), roadStreamingConfig.SectionCellSize);
                crossroadData.PositionHash = hash;

                // For some reason, baking data editing doesn't apply changes made by the system, but by the EntityManager works.
                EntityManager.SetComponentData(crossRoadEntity, crossroadData);

                crossroadBinding.Clear();

                var currentSectionIndex = 0;

                bool destroyEntity = false;

                if (roadStreamingConfig.StreamingIsEnabled)
                {
                    bool added = false;

                    if (crossroadData.HasSubNodes)
                    {
                        destroyEntity = crossroadData.HasInnerSubNodes;

                        if (destroyEntity)
                        {
                            for (int i = 0; i < crossroadData.TrafficNodeScopes.Length; i++)
                            {
                                var scopeEntity = crossroadData.TrafficNodeScopes[i];
                                var scopeData = EntityManager.GetComponentData<TrafficNodeScopeBakingData>(scopeEntity);
                                var scopeCrossRef = EntityManager.GetComponentData<TrafficNodeCrossroadRef>(scopeEntity);

                                Entity newScopeCrossroadEntity = GetOrCreateCrossroad(scopeData.Position, roadSceneData, roadStreamingConfig, crossroadData);

                                var scopeCrossroadBakingData = EntityManager.GetComponentData<CrossroadBakingData>(newScopeCrossroadEntity);

                                NativeList<Entity> scopes = default;

                                if (scopeCrossroadBakingData.TrafficNodeScopes.IsCreated)
                                {
                                    scopes = new NativeList<Entity>(scopeCrossroadBakingData.TrafficNodeScopes.Length, Allocator.Temp);
                                    scopes.AddRange(scopeCrossroadBakingData.TrafficNodeScopes);

                                    scopeCrossroadBakingData.TrafficNodeScopes.Dispose();
                                }
                                else
                                {
                                    scopes = new NativeList<Entity>(Allocator.Temp);
                                }

                                scopes.Add(scopeEntity);
                                scopeCrossroadBakingData.TrafficNodeScopes = scopes.ToArray(Allocator.Temp);
                                scopeCrossroadBakingData.HasSubNodes = crossroadData.HasSubNodes;
                                scopeCrossroadBakingData.HasInnerSubNodes = crossroadData.HasInnerSubNodes;
                                scopes.Dispose();

                                EntityManager.SetComponentData(newScopeCrossroadEntity, scopeCrossroadBakingData);

                                if (scopeCrossRef.RelatedSubCrossroadEntity != newScopeCrossroadEntity)
                                {
                                    scopeCrossRef.RelatedSubCrossroadEntity = newScopeCrossroadEntity;
                                    commandBuffer.SetComponent(scopeEntity, scopeCrossRef);
                                }
                            }
                        }
                        else
                        {
                            added = true;
                            crossroadBinding.Add(hash, crossRoadEntity);
                        }

                        for (int i = 0; i < crossroadData.TrafficNodeScopes.Length; i++)
                        {
                            var scopeEntity = crossroadData.TrafficNodeScopes[i];
                            var scopeData = EntityManager.GetComponentData<TrafficNodeScopeBakingData>(scopeEntity);

                            for (int j = 0; j < scopeData.SubNodes.Length; j++)
                            {
                                var subNodeEntity = scopeData.SubNodes[j].Entity;

                                var transform = EntityManager.GetComponentData<LocalTransform>(subNodeEntity);

                                Entity newSubCrossroadEntity = GetOrCreateCrossroad(transform.Position, roadSceneData, roadStreamingConfig, crossroadData);

                                var subCrossroadBakingData = EntityManager.GetComponentData<CrossroadBakingData>(newSubCrossroadEntity);

                                if (!subCrossroadBakingData.SubNodes.IsCreated || !subCrossroadBakingData.SubNodes.Contains(subNodeEntity))
                                {
                                    NativeList<Entity> nodes = default;

                                    if (subCrossroadBakingData.SubNodes.IsCreated)
                                    {
                                        nodes = new NativeList<Entity>(subCrossroadBakingData.SubNodes.Length + 1, Allocator.Temp);
                                        nodes.AddRange(subCrossroadBakingData.SubNodes);

                                        subCrossroadBakingData.SubNodes.Dispose();
                                    }
                                    else
                                    {
                                        nodes = new NativeList<Entity>(Allocator.Temp);
                                    }

                                    nodes.Add(subNodeEntity);

                                    subCrossroadBakingData.SubNodes = nodes.ToArray(Allocator.Temp);
                                    nodes.Dispose();
                                }

                                subCrossroadBakingData.Position = (subCrossroadBakingData.Position + transform.Position) / 2;
                                EntityManager.SetComponentData(newSubCrossroadEntity, subCrossroadBakingData);

                                commandBuffer.SetComponent(subNodeEntity, new TrafficNodeCrossroadRef()
                                {
                                    RelatedCrossroadEntity = newSubCrossroadEntity,
                                    RelatedSubCrossroadEntity = newSubCrossroadEntity
                                });
                            }
                        }
                    }

                    if (!destroyEntity && !added)
                    {
                        if (!crossroadBinding.ContainsKey(hash))
                        {
                            crossroadBinding.Add(hash, crossRoadEntity);
                        }
                        else
                        {
                            UnityEngine.Debug.Log($"TrafficSectionConversionSystem. Entity collision crossRoadEntity source '{crossroadBinding[hash].Index}' new '{crossRoadEntity.Index}' Position {crossroadData.Position}");
                        }
                    }

                    foreach (var binding in crossroadBinding)
                    {
                        var localHash = binding.Key;
                        var localEntity = binding.Value;
                        var localCrossroadBakingData = EntityManager.GetComponentData<CrossroadBakingData>(localEntity);
                        var localCrossroadComponent = EntityManager.GetComponentData<SegmentComponent>(localEntity);

                        if (sectionIndexMapping.ContainsKey(localHash))
                        {
                            currentSectionIndex = sectionIndexMapping[localHash];
                        }
                        else
                        {
                            currentSectionIndex = sectionIndex;
                            sectionIndexMapping.Add(localHash, currentSectionIndex);

                            CreateSceneSectionEntity(currentSectionIndex, localCrossroadBakingData.PositionHash, localCrossroadBakingData.Position);

                            sectionIndex++;
                        }

                        sectionEntityMapping.Add(localHash, new SectionData()
                        {
                            Entity = localEntity,
                            Position = localCrossroadBakingData.Position
                        });

                        commandBuffer.AddSharedComponent(localEntity, new SceneSection()
                        {
                            SceneGUID = roadSceneData.Hash128,
                            Section = currentSectionIndex
                        });

                        commandBuffer.AddComponent<SegmentUnloadTag>(localEntity);
                        commandBuffer.SetComponentEnabled<SegmentUnloadTag>(localEntity, false);

                        localCrossroadBakingData.SceneHashCode = roadSceneData.Hash128;
                        localCrossroadBakingData.SectionIndex = currentSectionIndex;

                        localCrossroadComponent.SegmentHash = localHash;
                        localCrossroadComponent.SectionIndex = currentSectionIndex;

                        commandBuffer.SetComponent(localEntity, localCrossroadBakingData);
                        commandBuffer.SetComponent(localEntity, localCrossroadComponent);
                    }
                }

                if (destroyEntity)
                {
                    commandBuffer.RemoveComponent<SegmentInitTag>(crossRoadEntity);
                    commandBuffer.RemoveComponent<SegmentComponent>(crossRoadEntity);
                    commandBuffer.RemoveComponent<SegmentPedestrianNodeData>(crossRoadEntity);
                    commandBuffer.RemoveComponent<SegmentTrafficNodeData>(crossRoadEntity);
                }

            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }

        private Entity GetOrCreateCrossroad(float3 position, RoadSceneData roadSceneData, RoadStreamingConfig roadStreamingConfig, CrossroadBakingData crossroadData)
        {
            var newHash = HashMapHelper.GetHashMapPosition(position.Flat(), roadStreamingConfig.SectionCellSize);

            if (!crossroadBinding.ContainsKey(newHash))
            {
                var crossroad = CreateCrossroadEntity(position, roadSceneData, newHash, crossroadData.InstanceId);

                crossroadBinding.Add(newHash, crossroad);
            }

            var crossroadEntity = crossroadBinding[newHash];
            return crossroadEntity;
        }

        private Entity CreateCrossroadEntity(float3 position, RoadSceneData roadSceneData, int hash, int instanceId)
        {
            var entity = EntityManager.CreateEntity(
                typeof(SegmentInitTag),
                typeof(SegmentComponent),
                typeof(CrossroadBakingData),
                typeof(CrossroadSubSegmentTag),
                typeof(SegmentPedestrianNodeData),
                typeof(SegmentTrafficNodeData));

            EntityManager.SetComponentData(entity, new CrossroadBakingData()
            {
                Position = position,
                PositionHash = hash,
                InstanceId = instanceId,
                SceneHashCode = roadSceneData.Hash128,
            });

            return entity;
        }

        private void ConvertPedestrianSections(RoadSceneData roadSceneData, RoadStreamingConfig roadStreamingConfig)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((Entity nodeEntity, ref PedestrianNodeBakingData pedestrianNodeBakingData) =>
            {
                var segmentSectionEntity = Entity.Null;

                int hash = GetNodeHash(pedestrianNodeBakingData, roadStreamingConfig);

                var currentSectionIndex = 0;

                if (sectionIndexMapping.ContainsKey(hash))
                {
                    currentSectionIndex = sectionIndexMapping[hash];

                    if (sectionEntityMapping.TryGetFirstValue(hash, out var crossroadData, out var iterator))
                    {
                        do
                        {
                            segmentSectionEntity = crossroadData.Entity;

                        } while (sectionEntityMapping.TryGetNextValue(out crossroadData, ref iterator));
                    }
                }
                else
                {
                    segmentSectionEntity = CreateRoadSection(commandBuffer, hash, pedestrianNodeBakingData.Position, roadSceneData, roadStreamingConfig, out currentSectionIndex);
                }

                pedestrianNodeBakingData.CrossroadEntity = segmentSectionEntity;

                commandBuffer.AddSharedComponent(nodeEntity, new SceneSection()
                {
                    SceneGUID = roadSceneData.Hash128,
                    Section = currentSectionIndex
                });

            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }

        private void ConvertCustomSectionObject(RoadSceneData roadSceneData, RoadStreamingConfig roadStreamingConfig)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((ref SectionObjectBakingData sectionObjectBakingData, in SceneSection sceneSection) =>
            {
                var currentSectionIndex = 0;
                Hash128 roadSectionHash = roadSceneData.Hash128;

                if (sceneSection.SceneGUID != roadSectionHash)
                    return;

                switch (sectionObjectBakingData.SectionObjectType)
                {
                    case SectionObjectType.AttachToClosest:
                        {
                            AttachOrCreateSection(ref commandBuffer, roadSceneData, roadStreamingConfig, sectionObjectBakingData, ref currentSectionIndex);
                            break;
                        }
                    case SectionObjectType.CreateNewIfNessesary:
                        {
                            AttachOrCreateSection(ref commandBuffer, roadSceneData, roadStreamingConfig, sectionObjectBakingData, ref currentSectionIndex);
                            break;
                        }
                    case SectionObjectType.ProviderObject:
                        {
                            if (sectionObjectBakingData.RelatedObject != Entity.Null)
                            {
                                GetRelatedSection(sectionObjectBakingData, ref currentSectionIndex, ref roadSectionHash);
                            }

                            break;
                        }
                    case SectionObjectType.CustomObject:
                        {
                            GetRelatedSection(sectionObjectBakingData, ref currentSectionIndex, ref roadSectionHash);
                            break;
                        }
                }

                var entities = sectionObjectBakingData.ChildEntities;

                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];

                    commandBuffer.AddSharedComponent(entity, new SceneSection()
                    {
                        SceneGUID = roadSectionHash,
                        Section = currentSectionIndex
                    });
                }

            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }

        private void AttachOrCreateSection(
            ref EntityCommandBuffer commandBuffer,
            RoadSceneData roadSceneData,
            RoadStreamingConfig roadStreamingConfig,
            SectionObjectBakingData sectionObjectBakingData,
            ref int currentSectionIndex)
        {
            var segmentSectionEntity = Entity.Null;
            var includeClosest = sectionObjectBakingData.SectionObjectType == SectionObjectType.AttachToClosest;
            int hash = GetSectionHash(sectionObjectBakingData.Position, roadStreamingConfig, includeClosest, 1f);

            if (sectionIndexMapping.ContainsKey(hash))
            {
                currentSectionIndex = sectionIndexMapping[hash];

                if (sectionEntityMapping.TryGetFirstValue(hash, out var crossroadData, out var iterator))
                {
                    do
                    {
                        segmentSectionEntity = crossroadData.Entity;

                    } while (sectionEntityMapping.TryGetNextValue(out crossroadData, ref iterator));
                }
            }
            else
            {
                segmentSectionEntity = CreateRoadSection(commandBuffer, hash, sectionObjectBakingData.Position, roadSceneData, roadStreamingConfig, out currentSectionIndex);
            }
        }

        private void GetRelatedSection(SectionObjectBakingData sectionObjectBakingData, ref int currentSectionIndex, ref Hash128 roadSectionHash)
        {
            if (sectionObjectBakingData.RelatedObject == Entity.Null)
                return;

            var relatedEntity = BakerExtension.GetEntity(EntityManager, sectionObjectBakingData.RelatedObject);

            if (EntityManager.HasComponent<SceneSection>(relatedEntity))
            {
                var sceneSection = EntityManager.GetSharedComponent<SceneSection>(relatedEntity);

                roadSectionHash = sceneSection.SceneGUID;
                currentSectionIndex = sceneSection.Section;
            }
        }

        private Entity CreateRoadSection(
            EntityCommandBuffer commandBuffer,
            int hash,
            float3 position,
            RoadSceneData roadSceneData,
            RoadStreamingConfig roadStreamingConfig,
            out int currentSectionIndex)
        {
            currentSectionIndex = sectionIndex;

            Entity segmentSectionEntity;
            var sectionPosition = HashMapHelper.GetCellPosition(position, roadStreamingConfig.SectionCellSize);

            var newRoadSectionEntity = EntityManager.CreateEntity(
                typeof(SegmentComponent),
                typeof(SegmentInitTag),
                typeof(SegmentUnloadTag),
                typeof(SceneSection));

            segmentSectionEntity = newRoadSectionEntity;

            commandBuffer.AddBuffer<SegmentPedestrianNodeData>(newRoadSectionEntity);
            commandBuffer.AddBuffer<SegmentTrafficNodeData>(newRoadSectionEntity);

            commandBuffer.AddComponent(newRoadSectionEntity, new SegmentComponent()
            {
                SectionIndex = currentSectionIndex,
                SegmentHash = hash
            });

            commandBuffer.SetComponentEnabled<SegmentUnloadTag>(newRoadSectionEntity, false);
            commandBuffer.AddSharedComponent(newRoadSectionEntity, new SceneSection()
            {
                SceneGUID = roadSceneData.Hash128,
                Section = currentSectionIndex
            });

            CreateSceneSectionEntity(currentSectionIndex, hash, sectionPosition);

            sectionIndexMapping.Add(hash, currentSectionIndex);

            sectionEntityMapping.Add(hash, new SectionData()
            {
                Entity = newRoadSectionEntity,
                Position = sectionPosition
            });

            sectionIndex++;

            return segmentSectionEntity;
        }

        private void CreateSceneSectionEntity(int currentSectionIndex, int hash, float3 position)
        {
            var sectionEntity = SerializeUtility.GetSceneSectionEntity(currentSectionIndex,
            EntityManager, ref sectionEntityQuery, true);

            EntityManager.AddComponentData(sectionEntity, new RoadSectionData
            {
                SectionIndex = currentSectionIndex,
                SegmentHash = hash,
                Position = position
            });
        }

        private int GetNodeHash(PedestrianNodeBakingData pedestrianNodeBakingData, RoadStreamingConfig roadStreamingConfig)
        {
            var hash = -1;

            if (pedestrianNodeBakingData.TrafficNodeScopeEntity != Entity.Null)
            {
                if (EntityManager.HasComponent<TrafficNodeCrossroadRef>(pedestrianNodeBakingData.TrafficNodeScopeEntity))
                {
                    var scope = EntityManager.GetComponentData<TrafficNodeCrossroadRef>(pedestrianNodeBakingData.TrafficNodeScopeEntity);

                    hash = GetCrossroadHash(scope.RelatedCrossroadEntity);
                }
            }

            if (hash == -1 && pedestrianNodeBakingData.LightEntity != Entity.Null)
            {
                var lightEntity = BakerExtension.GetEntity(EntityManager, pedestrianNodeBakingData.LightEntity);
                var crossroadEntity = EntityManager.GetComponentData<TrafficLightHandlerBakingData>(lightEntity).CrossroadEntity;
                hash = GetCrossroadHash(crossroadEntity);
            }

            if (hash == -1 && pedestrianNodeBakingData.CrossroadEntity != Entity.Null)
            {
                hash = GetCrossroadHash(pedestrianNodeBakingData.CrossroadEntity);
            }

            if (hash == -1)
            {
                hash = GetSectionHash(pedestrianNodeBakingData.Position, roadStreamingConfig, true);
            }

            return hash;
        }

        private int GetSectionHash(float3 sourcePosition, RoadStreamingConfig roadStreamingConfig, bool includeClosest, float sectionOffset = 0.5f)
        {
            var position = sourcePosition.Flat();

            var hash = HashMapHelper.GetHashMapPosition(position, roadStreamingConfig.SectionCellSize);

            if (!sectionIndexMapping.ContainsKey(hash) && includeClosest)
            {
                float minDistance = float.MaxValue;

                var keys = HashMapHelper.GetHashMapPosition9Cells(position, roadStreamingConfig.SectionCellSize);

                int closestKey = -1;

                foreach (var key in keys)
                {
                    if (sectionEntityMapping.TryGetFirstValue(key, out var crossroadData, out var iterator))
                    {
                        do
                        {
                            var distance = math.distance(crossroadData.Position.Flat(), position);

                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                closestKey = key;
                            }

                        } while (sectionEntityMapping.TryGetNextValue(out crossroadData, ref iterator));
                    }
                }

                keys.Dispose();

                if (closestKey != -1)
                {
                    if (minDistance < roadStreamingConfig.SectionCellSize * sectionOffset)
                    {
                        hash = closestKey;
                    }
                }
            }

            return hash;
        }

        private int GetCrossroadHash(Entity crossroadBakerEntity)
        {
            if (crossroadBakerEntity != Entity.Null)
            {
                var crossroadEntity = BakerExtension.GetEntity(EntityManager, crossroadBakerEntity);

                if (EntityManager.HasComponent<SegmentComponent>(crossroadEntity))
                {
                    var crossroadComponent = EntityManager.GetComponentData<SegmentComponent>(crossroadEntity);

                    return crossroadComponent.SegmentHash;
                }
            }

            return -1;
        }
    }
}