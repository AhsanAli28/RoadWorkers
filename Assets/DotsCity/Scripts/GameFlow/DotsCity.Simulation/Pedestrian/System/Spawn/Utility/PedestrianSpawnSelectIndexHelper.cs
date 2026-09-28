using Spirit604.Extensions;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public static class PedestrianSpawnSelectIndexHelper
    {
        private const int ATTEMPT_COUNT = 10;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity GetRandomAvailableSpawnpointIndex(
           uint baseSeed,
           in NativeArray<Entity> availableSpawnEntities,
           in ComponentLookup<NodeSettingsComponent> nodeSettingsLookup,
           in ComponentLookup<NodeCapacityComponent> nodeCapacityLookup,
           in NativeParallelHashMap<Entity, int> capacityNodeHashMap)
        {
            var currentAvailableSpawnPointCount = availableSpawnEntities.Length;

            if (currentAvailableSpawnPointCount == 0)
                return Entity.Null;

            int atemptCount = 0;

            while (true)
            {
                uint seed = MathUtilMethods.ModifySeed(baseSeed, atemptCount);
                var randomGen = new Random(seed);

                var localIndex = randomGen.NextInt(0, currentAvailableSpawnPointCount);

                var entity = availableSpawnEntities[localIndex];

                bool hasCapacity = HasCapacity(in capacityNodeHashMap, entity);

                if (hasCapacity && nodeCapacityLookup[entity].IsAvailable())
                {
                    randomGen = new Random(seed + (uint)(1 + atemptCount));

                    if (nodeSettingsLookup[entity].CanSpawn(randomGen))
                    {
                        return entity;
                    }
                }

                atemptCount++;

                if (atemptCount > ATTEMPT_COUNT)
                    return Entity.Null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity GetRandomAvailablePedestrianNodeTargetEntity(
            Entity sourceNode,
            uint baseSeed,
            bool allowCrosswalk,
            in NativeArray<Entity> availableEntities,
            in NativeHashSet<Entity> tempEntities,
            in DynamicBuffer<NodeConnectionDataElement> nodeConnectionBuffer,
            in ComponentLookup<NodeSettingsComponent> nodeSettingsLookup,
            in ComponentLookup<NodeCapacityComponent> nodeCapacityLookup,
            in ComponentLookup<NodeLightSettingsComponent> nodeLightSettingsLookup,
            in NativeParallelHashMap<Entity, int> capacityNodeHashMap)
        {
            Entity dstEntity = Entity.Null;
            bool canSpawn = false;
            int attemptCount = 0;
            tempEntities.Clear();

            while (!canSpawn)
            {
                canSpawn = true;

                var seed = MathUtilMethods.ModifySeed(baseSeed, attemptCount);

                var sumWeight = nodeSettingsLookup[sourceNode].SumWeight;

                dstEntity = PedestrianNodeEntityUtils.GetRandomDestinationEntity(
                    in nodeConnectionBuffer,
                    in nodeSettingsLookup,
                    in nodeCapacityLookup,
                    seed,
                    sumWeight);

                attemptCount++;

                if (tempEntities.Count == nodeConnectionBuffer.Length)
                {
                    return Entity.Null;
                }

                if (attemptCount > ATTEMPT_COUNT)
                {
                    return Entity.Null;
                }

                if (dstEntity == Entity.Null)
                {
                    continue;
                }

                if (tempEntities.Contains(dstEntity))
                {
                    canSpawn = false;
                    continue;
                }

                tempEntities.Add(dstEntity);

                bool hasCapacity = HasCapacity(in capacityNodeHashMap, dstEntity);

                if (!hasCapacity)
                {
                    canSpawn = false;
                    continue;
                }

                int sourceCrosswalkIndex = nodeLightSettingsLookup[sourceNode].CrosswalkIndex;
                int targetCrosswalkIndex = nodeLightSettingsLookup[dstEntity].CrosswalkIndex;

                bool sameCrosswalk = sourceCrosswalkIndex == targetCrosswalkIndex && targetCrosswalkIndex != -1;

                if (sameCrosswalk)
                {
                    var sourceLightEntity = nodeLightSettingsLookup[sourceNode].LightEntity;
                    var targetLightEntity = nodeLightSettingsLookup[dstEntity].LightEntity;

                    bool sameLight = sourceLightEntity == targetLightEntity && sourceLightEntity != Entity.Null;

                    if (sameLight && !allowCrosswalk)
                    {
                        canSpawn = false;
                        continue;
                    }

                    if (!nodeLightSettingsLookup[dstEntity].HasCrosswalk)
                    {
                        canSpawn = false;
                        continue;
                    }
                }

                if (!availableEntities.Contains(dstEntity))
                {
                    canSpawn = false;
                    continue;
                }
            }

            return dstEntity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasCapacity(in NativeParallelHashMap<Entity, int> capacityNodeHashMap, Entity targetEntity)
        {
            if (capacityNodeHashMap.TryGetValue(targetEntity, out var capacity))
            {
                return capacity > 0;
            }

            return true;
        }
    }
}