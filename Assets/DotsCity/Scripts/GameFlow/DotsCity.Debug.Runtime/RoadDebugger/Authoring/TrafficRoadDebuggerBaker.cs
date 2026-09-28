using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Debug
{

    [TemporaryBakingType]
    public struct TrafficRoadDebuggerInfoBakingData : IComponentData
    {
        public NativeArray<DebugRoadLaneData> DataOfLanes;
    }

    public struct DebugRoadLaneData
    {
        public bool IdleCar;
        public float NormalizedPathPosition;
        public float SpawnDelay;
        public int SpawnCarModel;

        public Entity NodeScopeEntity;
        public int LaneIndex;
        public int LocalPathIndex;
    }

    public class TrafficRoadDebuggerBaker : Baker<TrafficRoadDebugger>
    {
        public override void Bake(TrafficRoadDebugger authoring)
        {
            var entity = CreateAdditionalEntity(TransformUsageFlags.ManualOverride);

            AddComponent(entity, new TrafficRoadDebuggerInfo()
            {
                InstanceId = authoring.GetInstanceID(),
                Hash = TrafficRoadSpawnDebuggerSystem.GetHash(authoring),
                DisableLaneChanging = authoring.disableLaneChanging
            });

            AddComponent(entity, LocalTransform.FromPosition(authoring.transform.position));

            var trafficSpawnTestInfos = authoring.TrafficSpawnTestInfos;

            NativeList<DebugRoadLaneData> laneData = new NativeList<DebugRoadLaneData>(Allocator.TempJob);

            for (int i = 0; i < trafficSpawnTestInfos.Count; i++)
            {
                if (trafficSpawnTestInfos[i].Path == null)
                    continue;

                var node = trafficSpawnTestInfos[i].Path.SourceTrafficNode;
                var scopeEntity = GetEntity(node, TransformUsageFlags.Dynamic);

                int laneIndex = node.GetLaneIndexOfPath(trafficSpawnTestInfos[i].Path);
                int localPathIndex = node.GetLocalLaneIndexOfPath(trafficSpawnTestInfos[i].Path);

                laneData.Add(new DebugRoadLaneData()
                {
                    IdleCar = trafficSpawnTestInfos[i].IdleCar,
                    SpawnDelay = trafficSpawnTestInfos[i].SpawnDelay,
                    SpawnCarModel = authoring.spawnCarModel,
                    NormalizedPathPosition = trafficSpawnTestInfos[i].NormalizedPathPosition,
                    NodeScopeEntity = scopeEntity,
                    LaneIndex = laneIndex,
                    LocalPathIndex = localPathIndex
                });
            }

            AddComponent(entity, new TrafficRoadDebuggerInfoBakingData()
            {
                DataOfLanes = laneData.ToArray(Allocator.Temp)
            });

            laneData.Dispose();

            AddComponent(entity, new TrafficRoadDebuggerInit());
            AddComponent(entity, new TrafficRoadDebuggerCleanup());
            AddBuffer<SpawnedCarDataElement>(entity);
        }
    }
}
