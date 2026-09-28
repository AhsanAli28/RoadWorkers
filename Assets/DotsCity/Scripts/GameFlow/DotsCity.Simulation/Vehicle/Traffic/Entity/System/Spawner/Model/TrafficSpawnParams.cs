using Spirit604.Gameplay.Road;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public enum TrafficCustomInitType { Default, TrafficPublic, RoadSegmentDebug, PlayerControlled, UserCallback }

    public struct TrafficSpawnParams
    {
        public bool isStartSpawn;
        public float3 spawnPosition;
        public quaternion spawnRotation;
        public TrafficDestinationComponent destinationComponent;
        public TrafficPathComponent trafficPathComponent;
        public PathConnectionType pathConnectionType;
        public float3 velocity;

        public Entity targetNodeEntity;
        public Entity previousNodeEntity;
        public Entity spawnNodeEntity;

        public float speedLimit;
        public bool hasDriver;
        public bool hasStoppingEngine;
        public int carModelIndex;
        public int globalPathIndex;

        public int customInitialHealth;
        public bool customSpawnData;
        public TrafficCustomInitType trafficCustomInit;
        public Entity customRelatedEntityIndex;
        public int customInitIndex;

        public bool CustomSpawnSystem => trafficCustomInit == TrafficCustomInitType.TrafficPublic;

        public TrafficSpawnParams(float3 spawnPosition, quaternion spawnRotation) : this()
        {
            this.spawnPosition = spawnPosition;
            this.spawnRotation = spawnRotation;
        }

        public TrafficSpawnParams(float3 spawnPosition, quaternion spawnRotation, TrafficDestinationComponent destinationComponent) : this(spawnPosition, spawnRotation)
        {
            this.destinationComponent = destinationComponent;
        }

        public void Reset()
        {
            carModelIndex = -1;
        }
    }
}
