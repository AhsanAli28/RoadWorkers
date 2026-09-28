using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateAfter(typeof(PedestrianEntitySpawnerSystem))]
    [UpdateInGroup(typeof(SpawnerGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class PedestrianSpawnTalkAreaSystem : EndInitSystemBase
    {
        private EntityQuery updateQuery;

        public bool ForceDisable { get; set; }

        protected override void OnCreate()
        {
            base.OnCreate();

            PedestrianEntitySpawnerSystem.OnInitialized += PedestrianEntitySpawnerSystem_OnInitialized;

            updateQuery = new EntityQueryBuilder(Allocator.Temp)
                 .WithNone<NodeAreaSpawnedTag>()
                 .WithAll<TalkAreaSettingsComponent>()
                 .Build(this);

            RequireForUpdate(updateQuery);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            PedestrianEntitySpawnerSystem.OnInitialized -= PedestrianEntitySpawnerSystem_OnInitialized;
        }

        protected override void OnUpdate()
        {
            var commandBuffer = GetCommandBuffer();
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            var prefabContainer = SystemAPI.GetSingletonBuffer<PedestrianEntityPrefabContainer>(true);
            var pedestrianSettingsRef = SystemAPI.GetSingleton<PedestrianSettingsReference>();
            var nodeSettingsLookup = SystemAPI.GetComponentLookup<NodeSettingsComponent>(true);
            var nodeCapacityLookup = SystemAPI.GetComponentLookup<NodeCapacityComponent>(true);
            var baseSeed = MathUtilMethods.GetRandomSeed();

            Entities
            .WithoutBurst()
            .WithReadOnly(nodeSettingsLookup)
            .WithReadOnly(nodeCapacityLookup)
            .WithNone<NodeAreaSpawnedTag>()
            .WithAll<NodeAreaSpawnRequestedTag, NodeTalkAreaTag>()
            .ForEach((
                Entity entity,
                ref SpawnAreaComponent pedestrianNodeTalkArea,
                in TalkAreaSettingsComponent pedestrianTalkAreaSettingsComponent,
                in LocalTransform transform) =>
            {
                var connectionBuffer = SystemAPI.GetBuffer<NodeConnectionDataElement>(entity);

                var rndGen = new Random(baseSeed);

                int spawnBunchCount = rndGen.NextInt(pedestrianNodeTalkArea.MinSpawnCount, pedestrianNodeTalkArea.MaxSpawnCount);

                for (int i = 0; i < spawnBunchCount; i++)
                {
                    var randomAngle = rndGen.NextInt(0, 360);
                    var seed = MathUtilMethods.ModifySeed(baseSeed, randomAngle * i);

                    Vector3 axisPosition = default;

                    if (pedestrianNodeTalkArea.AreaType == PedestrianAreaShapeType.Circle)
                    {
                        axisPosition = transform.Position + (rndGen.NextFloat3(float3.zero, new float3(1, 1, 1)) * pedestrianNodeTalkArea.AreaSize).Flat();
                    }
                    else
                    {
                        Vector3 size = new Vector3(pedestrianNodeTalkArea.AreaSize, 0, pedestrianNodeTalkArea.AreaSize) * 2;
                        axisPosition = UnityMathematicsExtension.RandomPointInBox(transform.Position, size, rndGen);
                    }

                    int counter = 0;
                    int spawnCount = rndGen.NextInt(2, 5);

                    float stopTalkingTime = float.MaxValue;

                    if (pedestrianTalkAreaSettingsComponent.UnlimitedTalkTime == 0)
                    {
                        stopTalkingTime = currentTime + rndGen.NextFloat(pedestrianTalkAreaSettingsComponent.MinTalkTime, pedestrianTalkAreaSettingsComponent.MaxTalkTime);
                    }

                    for (int j = 0; j < spawnCount; j++)
                    {
                        Vector3 spawnPosition = default;
                        Quaternion spawnRotation = default;
                        TalkPeopleSpawnHelper.GetSpawnPosition(j, randomAngle, axisPosition, seed, ref counter, out spawnPosition, out spawnRotation);

                        var destination = float3.zero;
                        var destinationEntity = Entity.Null;

                        if (connectionBuffer.Length > 0)
                        {
                            destinationEntity = PedestrianNodeEntityUtils.GetRandomDestinationEntity(in connectionBuffer, in nodeSettingsLookup, in nodeCapacityLookup, baseSeed);

                            if (destinationEntity != Entity.Null)
                            {
                                destination = SystemAPI.GetComponent<LocalToWorld>(destinationEntity).Position;
                            }
                        }

                        var pedestrianSettings = pedestrianSettingsRef.Config;

                        var prefabEntity = prefabContainer.GetRandomPrefab(rndGen, out var skinIndex);

                        var pedestrianEntity = Spawn(
                            ref commandBuffer,
                            in pedestrianSettings,
                            seed,
                            prefabEntity,
                            skinIndex,
                            spawnPosition,
                            spawnRotation,
                            destinationEntity,
                            destination);

                        commandBuffer.AddComponent<TalkAreaComponent>(pedestrianEntity);

                        PedestrianInitUtils.InitTalkState(ref commandBuffer, pedestrianEntity, stopTalkingTime);
                    }
                }

                commandBuffer.SetComponentEnabled<NodeAreaSpawnRequestedTag>(entity, false);
                commandBuffer.SetComponentEnabled<NodeAreaSpawnedTag>(entity, true);
            }).Schedule();

            AddCommandBufferForProducer();
        }

        private static Entity Spawn(
            ref EntityCommandBuffer commandBuffer,
            in BlobAssetReference<PedestrianSettings> pedestrianSettings,
            uint seed,
            Entity entityPrefab,
            int skinIndex,
            Vector3 spawnPosition,
            Quaternion spawnRotation,
            Entity destinationEntity,
            float3 destination)
        {
            var pedestrianEntity = commandBuffer.Instantiate(entityPrefab);

            var destinationComponent = new DestinationComponent()
            {
                Value = destination,
                PreviousDestination = destination,
                DestinationNode = destinationEntity,
                PreviuosDestinationNode = destinationEntity,
            };

            var spawnParams = new SpawnParams()
            {
                DestinationComponent = destinationComponent,
                RigidTransform = new RigidTransform(spawnRotation, spawnPosition),
                Seed = seed,
                SkinIndex = skinIndex
            };

            PedestrianInitUtils.Initialize(ref commandBuffer, pedestrianEntity, in spawnParams, in pedestrianSettings);

            return pedestrianEntity;
        }

        private void PedestrianEntitySpawnerSystem_OnInitialized()
        {
            if (!ForceDisable)
            {
                Enabled = true;
            }
        }
    }
}