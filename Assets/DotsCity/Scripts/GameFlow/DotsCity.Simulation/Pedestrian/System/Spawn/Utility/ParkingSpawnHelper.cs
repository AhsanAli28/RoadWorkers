using Spirit604.DotsCity.Simulation.Pedestrian.State;
using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public static class ParkingSpawnHelper
    {
        public struct ParkingSpawnData
        {
            public uint Seed;
            public Entity PedestrianEntityPrefab;
            public int SkinIndex;
            public Entity SpawnPointEntity;
            public DynamicBuffer<NodeConnectionDataElement> NodeConnectionData;
            public ComponentLookup<LocalToWorld> WorldTransformLookup;
            public bool HasCustomSpawnPosition;
            public float3 CustomSpawnPosition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Spawn(
            ref EntityCommandBuffer commandBuffer,
            in ParkingSpawnData parkingData,
            in BlobAssetReference<PedestrianSettings> pedestrianSettings)
        {
            var entity = commandBuffer.Instantiate(parkingData.PedestrianEntityPrefab);

            var pedestrianNodeConnectorComponent = parkingData.NodeConnectionData;

            var random = new Random(parkingData.Seed);

            Entity targetEntity = Entity.Null;

            if (pedestrianNodeConnectorComponent.Length > 0)
            {
                var randomIndex = random.NextInt(0, pedestrianNodeConnectorComponent.Length);

                targetEntity = pedestrianNodeConnectorComponent[randomIndex].ConnectedEntity;
            }
            else
            {
                targetEntity = parkingData.SpawnPointEntity;
            }

            float3 targetPosition = parkingData.WorldTransformLookup[targetEntity].Position;
            float3 spawnPosition = default;

            if (!parkingData.HasCustomSpawnPosition)
            {
                spawnPosition = parkingData.WorldTransformLookup[parkingData.SpawnPointEntity].Position;
            }
            else
            {
                spawnPosition = parkingData.CustomSpawnPosition;
            }

            quaternion spawnRotation = quaternion.LookRotationSafe(math.normalizesafe(targetPosition - spawnPosition), math.up());

            var destinationComponent = new DestinationComponent()
            {
                Value = targetPosition,
                PreviousDestination = spawnPosition,
                PreviuosDestinationNode = parkingData.SpawnPointEntity,
                DestinationNode = targetEntity,
            };

            commandBuffer.SetComponentEnabled<HasTargetTag>(entity, true);

            var spawnParams = new SpawnParams()
            {
                DestinationComponent = destinationComponent,
                RigidTransform = new RigidTransform(spawnRotation, spawnPosition),
                Seed = parkingData.Seed,
                SkinIndex = parkingData.SkinIndex,
            };

            commandBuffer.SetComponent(entity, new NextStateComponent(ActionState.MovingToNextTargetPoint));

            PedestrianInitUtils.Initialize(ref commandBuffer, entity, in spawnParams, in pedestrianSettings);
        }
    }
}