using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.NavMesh
{
    public struct NavMeshObstacleData : IComponentData
    {
        public Entity ObstacleEntity;
        public Vector3 Center;
        public Vector3 Size;
        public bool Carve;
        public float MoveThreshold;
        public float TimeToStationary;
        public bool CarveOnlyStationary;
        public float LoadTime;

        public NavMeshObstacleData(NavMeshObstacleBakingData navMeshObstacleBakingData)
        {
            ObstacleEntity = Entity.Null;
            Center = navMeshObstacleBakingData.Center;
            Size = navMeshObstacleBakingData.Size;
            Carve = navMeshObstacleBakingData.Carve;
            MoveThreshold = navMeshObstacleBakingData.MoveThreshold;
            TimeToStationary = navMeshObstacleBakingData.TimeToStationary;
            CarveOnlyStationary = navMeshObstacleBakingData.CarveOnlyStationary;
            LoadTime = 0;
        }
    }

    [TemporaryBakingType]
    public struct NavMeshObstacleBakingData : IComponentData
    {
        public Vector3 Center;
        public Vector3 Size;
        public bool Carve;
        public float MoveThreshold;
        public float TimeToStationary;
        public bool CarveOnlyStationary;
    }

    public struct NavMeshObstacleLoadTag : IComponentData, IEnableableComponent { }

    public struct NavMeshObstaclePrefabTag : IComponentData { }
}