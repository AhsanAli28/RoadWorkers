using Spirit604.Extensions;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    public class TrafficSettingsEntityBaker : Baker<TrafficSettings>
    {
        public override void Bake(TrafficSettings trafficSettings)
        {
            DependsOn(trafficSettings.TrafficSpawnerConfig);
            DependsOn(trafficSettings.TrafficSettingsConfig);

            var trafficSpawnSettingsEntity = CreateAdditionalEntity(TransformUsageFlags.None);
            AddComponent(trafficSpawnSettingsEntity, TrafficSpawnerSettingsAuthoring.CreateConfigStatic(this, trafficSettings));

            var trafficCommonSettingsEntity = CreateAdditionalEntity(TransformUsageFlags.None);
            AddComponent(trafficCommonSettingsEntity, TrafficCommonSettingsAuthoring.CreateConfigStatic(this, trafficSettings));

            var trafficSettingsEntity = CreateAdditionalEntity(TransformUsageFlags.None);
            AddComponent(trafficSettingsEntity, TrafficSettingsAuthoring.CreateConfigStatic(this, trafficSettings));

            var rotationCurveEntity = CreateAdditionalEntity(TransformUsageFlags.None);
            AddComponent(rotationCurveEntity, TrafficRotationCurveAuthoring.CreateConfigStatic(this, trafficSettings));
        }
    }
}

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficSpawnerConfigBlob
    {
        public int PreferableCount;
        public float MaxCarsPerNode;
        public int HashMapCapacity;
        public int MaxSpawnCountByIteration;
        public int MaxParkingCarsCount;
        public float MinSpawnDelay;
        public float MaxSpawnDelay;
        public float MinSpawnCarDistance;
        public float MinSpawnCarDistanceSQ;
    }

    public struct TrafficCommonSettingsConfigBlob
    {
        public EntityType EntityType;
        public DetectObstacleMode DetectObstacleMode;
        public DetectNpcMode DetectNpcMode;
        public bool HasRaycast;
        public bool CullWheelSupported;
        public bool CullWheels;
        public float DefaultLaneSpeed;
        public float MaxSpeedSteerDirectionAngle;
        public bool CullPhysics;

        public PhysicsSimulationType PhysicsSimulation;
        public SimplePhysicsSimulationType SimplePhysicsType;
    }

    public struct TrafficSettingsConfigBlob
    {
        public float MaxSpeed;
        public float Acceleration;
        public float BackwardAcceleration;
        public float BrakePower;
        public float BrakingRate;
        public float MaxSteerAngle;
        public float MaxSteerDirectionAngle;
        public bool UseSteeringDamping;
        public bool LimitSteeringPower;
        public float SteeringDamping;
        public bool HasRotationLerp;
        public int HealthCount;
        public bool HasNavMeshObstacle;
        public BlobPtr<AnimationCurveBlob> SteeringPowerCurve;
    }

    public struct TrafficSpawnerConfigBlobReference : IComponentData
    {
        public BlobAssetReference<TrafficSpawnerConfigBlob> Reference;
    }

    public struct TrafficCommonSettingsConfigBlobReference : IComponentData
    {
        public BlobAssetReference<TrafficCommonSettingsConfigBlob> Reference;
    }

    public struct TrafficSettingsConfigBlobReference : IComponentData
    {
        public BlobAssetReference<TrafficSettingsConfigBlob> Reference;
    }

    public struct CurveData
    {
        public float RotationSpeed;
        public BlobArray<float> Values;
    }

    public struct RotationCurveReference : IComponentData
    {
        public BlobAssetReference<CurveData> Curve;
    }
}