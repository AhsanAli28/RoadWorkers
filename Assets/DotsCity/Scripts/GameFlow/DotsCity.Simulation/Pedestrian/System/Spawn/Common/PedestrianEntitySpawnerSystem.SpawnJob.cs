using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public partial class PedestrianEntitySpawnerSystem
    {
        private const int MIN_TALK_GROUP_COUNT = 2;
        private const int MAX_TALK_GROUP_COUNT = 4;

        [BurstCompile]
        private struct SpawnJob : IJob
        {
            [WriteOnly] public EntityCommandBuffer CommandBuffer;
            [ReadOnly] public int SpawnCount;
            [ReadOnly] public Entity SpawnNodeEntity;
            [ReadOnly] public DynamicBuffer<PedestrianEntityPrefabContainer> PrefabContainer;
            [ReadOnly] public BlobAssetReference<TalkSpawnSettings> TalkSpawnSettings;
            [ReadOnly] public BlobAssetReference<PedestrianSettings> PedestrianSettings;

            [DeallocateOnJobCompletion]
            [ReadOnly] public NativeArray<Entity> AvailableSpawnPointEntities;
            [ReadOnly] public ComponentLookup<LocalToWorld> WorldTransformLookup;
            [ReadOnly] public ComponentLookup<NodeSettingsComponent> NodeSettingsLookup;
            [ReadOnly] public ComponentLookup<NodeCapacityComponent> NodeCapacityLookup;
            [ReadOnly] public ComponentLookup<NodeLightSettingsComponent> NodeLightSettingsLookup;
            [ReadOnly] public BufferLookup<NodeConnectionDataElement> NodeConnectionBufferLookup;

            public NativeList<Entity> PedestrianTalkGroupEntities;
            public NativeList<float3> SpawnPositions;
            public NativeList<int> GroupIndexes;
            public NativeHashSet<Entity> TempSpawnPointEntities;

            [ReadOnly] public NativeArray<uint> Seeds;

            public NativeParallelHashMap<Entity, int> CapacityNodeHashMap;

            private int groupIndex;
            private int spawnedCount;

            public void Execute()
            {
                groupIndex = -1;
                spawnedCount = 0;

                while (spawnedCount < SpawnCount)
                {
                    int index = spawnedCount;

                    uint baseSeed = Seeds[index];
                    var randomGen = new Random(baseSeed);

                    bool groupSpawn = UnityMathematicsExtension.ChanceDropped(TalkSpawnSettings.Value.TalkingPedestrianSpawnChance, randomGen);
                    int localSpawnCount = 1;

                    if (groupSpawn)
                    {
                        groupSpawn = GetSpawnCount(index, baseSeed, ref localSpawnCount);

                        if (groupSpawn)
                        {
                            groupIndex++;
                        }
                    }

                    bool allowCrosswalk = !groupSpawn;

                    Entity sourceNodeEntity, dstNodeEntity;
                    GetNodeIndexes(baseSeed, allowCrosswalk, out sourceNodeEntity, out dstNodeEntity);

                    if (sourceNodeEntity == Entity.Null || dstNodeEntity == Entity.Null)
                    {
                        spawnedCount++;
                        continue;
                    }

                    SpawnParams spawnParams = default;

                    var srcNodeLight = NodeLightSettingsLookup[sourceNodeEntity];
                    var dstNodeLight = NodeLightSettingsLookup[dstNodeEntity];

                    var isCrosswalk = srcNodeLight.IsCrosswalk(dstNodeLight);
                    int skinIndex = -1;

                    for (int i = 0; i < localSpawnCount; i++)
                    {
                        var prefabEntity = Entity.Null;

                        if (PrefabContainer.HasPrefabVariants())
                        {
                            var rndGen = new Random(MathUtilMethods.ModifySeed(baseSeed, spawnedCount * (i + 1)));
                            prefabEntity = PrefabContainer.GetRandomPrefab(rndGen, out skinIndex);
                        }
                        else
                        {
                            prefabEntity = PrefabContainer.GetFirstPrefab();
                        }

                        var pedestrianEntity = CommandBuffer.Instantiate(prefabEntity);

                        if (i == 0)
                        {
                            bool hasTarget = !groupSpawn;

                            var spawnRequestParams = new PedestrianSpawnUtils.SpawnRequestParams()
                            {
                                PedestrianEntity = pedestrianEntity,
                                SourceEntity = sourceNodeEntity,
                                DstEntity = dstNodeEntity,
                                BaseSeed = baseSeed,
                                GroupSpawn = groupSpawn,
                                HasTarget = hasTarget,
                                WorldTransformLookup = WorldTransformLookup,
                                NodeSettingsLookup = NodeSettingsLookup,
                                NodeLightSettingsLookup = NodeLightSettingsLookup,
                                CapacityNodeHashMap = CapacityNodeHashMap,
                            };

                            spawnParams = PedestrianSpawnUtils.GetSpawnParams(ref CommandBuffer, ref spawnRequestParams);

#if RUNTIME_ROAD
                            // Temporary solution
                            if (spawnParams.RigidTransform.pos.Equals(float3.zero))
                                continue;
#endif

                            var capturedNodeInfo = spawnParams.CapturedNodeInfo;

                            if (capturedNodeInfo.CapturedNodeEntity != Entity.Null)
                            {
                                CapacityNodeHashMap[capturedNodeInfo.CapturedNodeEntity] = --CapacityNodeHashMap[capturedNodeInfo.CapturedNodeEntity];
                            }
                        }

                        if (groupSpawn)
                        {
                            PedestrianTalkGroupEntities.Add(pedestrianEntity);
                            SpawnPositions.Add(spawnParams.RigidTransform.pos);
                            GroupIndexes.Add(groupIndex);
                        }

                        spawnParams.SkinIndex = skinIndex;

                        PedestrianInitUtils.Initialize(ref CommandBuffer, pedestrianEntity, in spawnParams, in PedestrianSettings, isCrosswalk, i);
                    }

                    spawnedCount += localSpawnCount;
                }
            }

            private bool GetSpawnCount(int spawnedCount, uint baseSeed, ref int localSpawnCount)
            {
                int remainCount = SpawnCount - spawnedCount;

                if (remainCount >= MIN_TALK_GROUP_COUNT)
                {
                    var seed = MathUtilMethods.ModifySeed(baseSeed, remainCount);
                    var randomGen = new Random(seed);
                    int groupCapacity = randomGen.NextInt(MIN_TALK_GROUP_COUNT, MAX_TALK_GROUP_COUNT + 1);
                    groupCapacity = math.clamp(groupCapacity, MIN_TALK_GROUP_COUNT, remainCount);
                    localSpawnCount = groupCapacity;
                    return true;
                }

                return false;
            }

            private void GetNodeIndexes(uint baseSeed, bool allowCrosswalk, out Entity sourceNode, out Entity dstNode)
            {
                if (SpawnNodeEntity == Entity.Null)
                {
                    sourceNode = PedestrianSpawnSelectIndexHelper.GetRandomAvailableSpawnpointIndex(baseSeed, in AvailableSpawnPointEntities, in NodeSettingsLookup, in NodeCapacityLookup, in CapacityNodeHashMap);
                }
                else
                {
                    sourceNode = SpawnNodeEntity;
                }

                dstNode = Entity.Null;

                if (sourceNode != Entity.Null)
                {
                    var nodeSettingsComponents = NodeConnectionBufferLookup[sourceNode];
                    var newSeed = MathUtilMethods.ModifySeed(baseSeed, sourceNode.Index);

                    dstNode = PedestrianSpawnSelectIndexHelper.GetRandomAvailablePedestrianNodeTargetEntity(
                        sourceNode,
                        newSeed,
                        allowCrosswalk,
                        AvailableSpawnPointEntities,
                        TempSpawnPointEntities,
                        in nodeSettingsComponents,
                        in NodeSettingsLookup,
                        in NodeCapacityLookup,
                        in NodeLightSettingsLookup,
                        in CapacityNodeHashMap);
                }
            }
        }
    }
}
