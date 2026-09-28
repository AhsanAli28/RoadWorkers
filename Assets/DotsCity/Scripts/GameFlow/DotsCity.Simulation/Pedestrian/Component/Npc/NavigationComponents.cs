using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Npc.Navigation
{
    public enum NpcNavigationType
    {
        Temp,
        Persist,
    }

    public enum ObstacleAvoidanceType
    {
        Disabled,
        CalcNavPath,
        LocalAvoidance,
        AgentsNavigation,
    }

    public enum ObstacleAvoidanceMethod
    {
        Simple,
        FindNeighbors,
    }

    public struct ObstacleAvoidanceSettingsData
    {
        public ObstacleAvoidanceMethod ObstacleAvoidanceMethod;
        public float MaxSurfaceAngle;
        public float ObstacleAvoidanceOffset;
        public float AchieveDistanceSQ;
        public bool CheckTargetAvailability;
        public int SearchNewTargetAttemptCount;
    }

    public struct ObstacleAvoidanceSettingsReference : IComponentData
    {
        public BlobAssetReference<ObstacleAvoidanceSettingsData> SettingsReference;
    }

    public struct NavAgentComponent : IComponentData
    {
        public float RemainingDistance;
        public int PathIndex;
        public int HasPath;

        public float3 PathEndPosition;
        public float LastUpdateTimestamp;
        public Entity ObstacleEntity;
    }

    public struct NavAgentSteeringComponent : IComponentData
    {
        public float3 SteeringTargetValue;
        public int SteeringTarget;

        public bool HasSteeringTarget => SteeringTarget == 1;
    }

    public struct NavAgentTag : IComponentData { }

    public struct EnabledNavigationTag : IComponentData, IEnableableComponent { }

    public struct PersistNavigationTag : IComponentData { }

    public struct PersistNavigationComponent : IComponentData
    {
        public Entity CurrentEntity;
    }

    public struct UpdateNavTargetTag : IComponentData, IEnableableComponent { }

    public struct AchievedNavTargetTag : IComponentData, IEnableableComponent { }
}