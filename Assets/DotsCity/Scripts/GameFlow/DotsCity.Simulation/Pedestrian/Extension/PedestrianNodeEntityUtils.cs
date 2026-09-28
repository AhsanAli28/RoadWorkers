using Spirit604.Extensions;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public static class PedestrianNodeEntityUtils
    {
        private const int AttemptCount = 10;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity GetRandomDestinationEntity(
            in DynamicBuffer<NodeConnectionDataElement> pedestrianNodeConnector,
            in ComponentLookup<NodeSettingsComponent> nodeSettingsLookup,
            in ComponentLookup<NodeCapacityComponent> nodeCapacityLookup,
            uint sourceSeed,
            float sumWeight = 0)
        {
            return GetRandomDestinationEntity(in pedestrianNodeConnector, in nodeSettingsLookup, in nodeCapacityLookup, sourceSeed, Entity.Null, sumWeight);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity GetRandomDestinationEntity(
            in DynamicBuffer<NodeConnectionDataElement> pedestrianNodeConnector,
            in ComponentLookup<NodeSettingsComponent> nodeSettingsLookup,
            in ComponentLookup<NodeCapacityComponent> nodeCapacityLookup,
            uint sourceSeed,
            Entity ignoreEntity,
            float sumWeight = 0)
        {
            if (!pedestrianNodeConnector.IsCreated || pedestrianNodeConnector.Length == 0)
            {
                return Entity.Null;
            }

            if (pedestrianNodeConnector.Length == 1)
            {
                return pedestrianNodeConnector[0].ConnectedEntity;
            }

            while (true)
            {
                int attemptCount = 0;
                var seed = sourceSeed;

                while (attemptCount < AttemptCount)
                {
                    var randomGen = new Random(seed);

                    Entity connectedEntity = Entity.Null;

                    if (sumWeight > 0)
                    {
                        float randomWeight = randomGen.NextFloat(0, sumWeight);

                        for (int i = 0; i < pedestrianNodeConnector.Length; i++)
                        {
                            var currentSumWeight = pedestrianNodeConnector[i].SumWeight;

                            if (randomWeight <= currentSumWeight)
                            {
                                connectedEntity = pedestrianNodeConnector[i].ConnectedEntity;
                                break;
                            }
                        }
                    }
                    else
                    {
                        int randomIndex = randomGen.NextInt(0, pedestrianNodeConnector.Length);
                        connectedEntity = pedestrianNodeConnector[randomIndex].ConnectedEntity;
                    }

                    if (connectedEntity != ignoreEntity && nodeSettingsLookup.HasComponent(connectedEntity) && nodeCapacityLookup[connectedEntity].IsAvailable())
                    {
                        return connectedEntity;
                    }

                    seed = MathUtilMethods.ModifySeed(sourceSeed, attemptCount);
                    attemptCount++;
                }

                if (attemptCount >= AttemptCount || ignoreEntity == Entity.Null)
                {
                    var index = math.clamp(math.abs(((int)sourceSeed)) % pedestrianNodeConnector.Length, 0, pedestrianNodeConnector.Length - 1);
                    return pedestrianNodeConnector[index].ConnectedEntity;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveConnection(EntityManager entityManager, Entity sourceEntity, Entity removeEntity)
        {
            bool removed = false;

            if (RemoveConnectedEntity(entityManager, sourceEntity, removeEntity))
                removed = true;

            if (RemoveConnectedEntity(entityManager, removeEntity, sourceEntity))
                removed = true;

            return removed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnjoinEntityAndReconnect(EntityManager entityManager, Entity node1, Entity node2, Entity unjoinEntity, bool reconnect = true)
        {
            RemoveConnection(entityManager, node1, unjoinEntity);
            RemoveConnection(entityManager, node2, unjoinEntity);

            if (!reconnect) return;

            var nodeSettings1 = entityManager.GetComponentData<NodeSettingsComponent>(node1);
            var nodeSettings2 = entityManager.GetComponentData<NodeSettingsComponent>(node2);

            AddConnection(entityManager, node1, node2, nodeSettings1.Weight, nodeSettings2.Weight);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveConnectedEntity(EntityManager entityManager, Entity sourceEntity, Entity removeEntity)
        {
            bool removed = false;
            var buffer = entityManager.GetBuffer<NodeConnectionDataElement>(sourceEntity);
            var settings = entityManager.GetComponentData<NodeSettingsComponent>(sourceEntity);

            float removedWeight = 0;
            float previousWeight = 0;

            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].ConnectedEntity == removeEntity)
                {
                    removedWeight = buffer[i].SumWeight - previousWeight;

                    for (int j = i + 1; j < buffer.Length; j++)
                    {
                        var entry = buffer[j];
                        entry.SumWeight -= removedWeight;
                        buffer[j] = entry;
                    }

                    buffer.RemoveAt(i);
                    removed = true;
                    break;
                }
                else
                {
                    previousWeight = buffer[i].SumWeight;
                }
            }

            if (removed)
            {
                settings.SumWeight -= removedWeight;
                entityManager.SetComponentData(sourceEntity, settings);
            }

            return removed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AddConnection(EntityManager entityManager, Entity node1, Entity node2, float nodeWeight1 = 1f, float nodeWeight2 = 1f, bool checkForDuplicates = true)
        {
            bool added = false;

            if (PedestrianNodeEntityUtils.AddConnectedEntity(entityManager, node1, node2, nodeWeight2))
                added = true;

            if (PedestrianNodeEntityUtils.AddConnectedEntity(entityManager, node2, node1, nodeWeight1))
                added = true;

            return added;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AddConnectedEntity(EntityManager entityManager, Entity sourceEntity, Entity connectedEntity, float connectedWeight = 1f, bool checkForDuplicates = true)
        {
            bool added = false;
            var buffer = entityManager.GetBuffer<NodeConnectionDataElement>(sourceEntity);
            var settings = entityManager.GetComponentData<NodeSettingsComponent>(sourceEntity);

            if (checkForDuplicates)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i].ConnectedEntity == connectedEntity)
                    {
                        added = true;
                        break;
                    }
                }
            }

            if (!added)
            {
                settings.SumWeight += connectedWeight;

                buffer.Add(new NodeConnectionDataElement()
                {
                    SumWeight = settings.SumWeight,
                    ConnectedEntity = connectedEntity
                });

                entityManager.SetComponentData(sourceEntity, settings);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AddConnectedEntities(EntityManager entityManager, Entity sourceEntity, NativeList<Entity> connectedEntities, bool checkForDuplicates = true)
        {
            var buffer = entityManager.GetBuffer<NodeConnectionDataElement>(sourceEntity);
            var initialBufferLength = buffer.Length;

            var settings = entityManager.GetComponentData<NodeSettingsComponent>(sourceEntity);
            bool changed = false;

            for (int i = 0; i < connectedEntities.Length; i++)
            {
                var connectedEntity = connectedEntities[i];
                bool added = false;

                if (checkForDuplicates)
                {
                    for (int j = 0; j < initialBufferLength; j++)
                    {
                        if (buffer[j].ConnectedEntity == connectedEntity)
                        {
                            added = true;
                            break;
                        }
                    }
                }

                if (!added)
                {
                    float weight = entityManager.GetComponentData<NodeSettingsComponent>(connectedEntity).Weight;

                    settings.SumWeight += weight;

                    buffer.Add(new NodeConnectionDataElement()
                    {
                        SumWeight = settings.SumWeight,
                        ConnectedEntity = connectedEntity
                    });

                    changed = true;
                }
            }

            if (changed)
            {
                entityManager.SetComponentData(sourceEntity, settings);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<Entity> GetConnectedEntities(EntityManager entityManager, Entity sourceEntity, Allocator allocator = Allocator.Temp, NativeList<Entity> ignoreEntities = default)
        {
            var entities = new NativeList<Entity>(allocator);

            GetConnectedEntities(entityManager, sourceEntity, ref entities, ignoreEntities);
            return entities;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetConnectedEntities(EntityManager entityManager, Entity sourceEntity, ref NativeList<Entity> entities, NativeList<Entity> ignoreEntities = default)
        {
            var buffer = entityManager.GetBuffer<NodeConnectionDataElement>(sourceEntity, true);

            for (int i = 0; i < buffer.Length; i++)
            {
                if (ignoreEntities.IsCreated && ignoreEntities.Contains(buffer[i].ConnectedEntity)) continue;

                entities.Add(buffer[i].ConnectedEntity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsConnectedTo(EntityManager entityManager, Entity sourceEntity, Entity connectedEntity)
        {
            var buffer = entityManager.GetBuffer<NodeConnectionDataElement>(sourceEntity, true);

            return IsConnectedTo(in buffer, connectedEntity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsConnectedTo(in this DynamicBuffer<NodeConnectionDataElement> buffer, Entity connectedEntity)
        {
            if (connectedEntity == Entity.Null) return false;

            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].ConnectedEntity == connectedEntity)
                {
                    return true;
                }
            }

            return false;
        }
    }
}