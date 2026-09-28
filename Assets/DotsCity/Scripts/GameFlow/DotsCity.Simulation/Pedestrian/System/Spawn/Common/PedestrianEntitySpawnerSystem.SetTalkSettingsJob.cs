using Spirit604.Extensions;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public partial class PedestrianEntitySpawnerSystem
    {
        private const float DISTANCE_FROM_SPAWN_POINT = 0.6f;
        private const float DEVIATION_FROM_SPAWN_POINT = 0.2f;
        private const float MIN_DISTANCE_BETWEEN_NODES = 1f;
        private const float MAX_RANDOM_DEVIATION_ANGLE = 20F;
        private const float MAX_SHIFT_FROM_SPAWN_AXIS = 0.2f;

        private struct SetTalkSettingsJob : IJob
        {
            [WriteOnly] public EntityCommandBuffer commandBuffer;

            [ReadOnly] public BlobAssetReference<TalkSpawnSettings> pedestrianTalkSpawnSettings;
            [ReadOnly] public double currentTime;

            [ReadOnly] public NativeList<Entity> talkGroupEntities;
            [ReadOnly] public NativeList<float3> spawnPositions;
            [ReadOnly] public NativeList<int> talkGroupIndexes;
            [ReadOnly] public NativeArray<uint> seeds;

            private int counter;
            private float talkTime;
            private int randomAngle;
            private Vector3 spawnAxisWithDeviation;

            private int currentGroupIndex;
            private int localPedestrianGroupIndex;

            public void Execute()
            {
                if (talkGroupIndexes.Length == 0)
                {
                    return;
                }

                currentGroupIndex = -1;

                for (int index = 0; index < talkGroupEntities.Length; index++)
                {
                    var pedestrianEntity = talkGroupEntities[index];
                    var baseSeed = seeds[index];

                    if (currentGroupIndex != talkGroupIndexes[index])
                    {
                        currentGroupIndex = talkGroupIndexes[index];
                        localPedestrianGroupIndex = 0;
                        counter = 0;

                        InitPedestrianTalkGroup(index, baseSeed, localPedestrianGroupIndex);
                    }

                    Vector3 spawnPosition; Quaternion spawnRotation;

                    TalkPeopleSpawnHelper.GetSpawnPosition(localPedestrianGroupIndex, randomAngle, spawnAxisWithDeviation, baseSeed, ref counter, out spawnPosition, out spawnRotation);

                    var stopTalkingTime = currentTime + talkTime;
                    PedestrianInitUtils.InitTalkState(ref commandBuffer, pedestrianEntity, stopTalkingTime, spawnPosition, spawnRotation);

                    localPedestrianGroupIndex++;
                }
            }

            private void InitPedestrianTalkGroup(int i, uint baseSeed, int localPedestrianGroupIndex)
            {
                Vector3 initialSpawnPosition = spawnPositions[i];

                counter = 0;

                var rndGen = new Random(baseSeed);

                randomAngle = rndGen.NextInt(0, 360);

                var newSeed = MathUtilMethods.ModifySeed(baseSeed, randomAngle);
                rndGen.InitState(newSeed);

                int randomAngle2 = rndGen.NextInt(0, 360);

                newSeed = MathUtilMethods.ModifySeed(baseSeed, randomAngle2);
                rndGen.InitState(newSeed);

                talkTime = rndGen.NextFloat(pedestrianTalkSpawnSettings.Value.MinTalkTime, pedestrianTalkSpawnSettings.Value.MaxTalkTime);
                spawnAxisWithDeviation = initialSpawnPosition + Quaternion.Euler(0, randomAngle2, 0) * Vector3.forward * DEVIATION_FROM_SPAWN_POINT;
            }
        }
    }
}
