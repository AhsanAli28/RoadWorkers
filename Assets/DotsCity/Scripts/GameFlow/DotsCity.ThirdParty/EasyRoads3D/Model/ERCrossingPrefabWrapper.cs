using EasyRoads3Dv3;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.RoadGeneration
{
    public class ERCrossingPrefabWrapper : RoadWrapperBase, ICustomPrefabRoad
    {
        private readonly ERCrossingPrefabs crossingPrefab;

        public ERCrossingPrefabWrapper(ERCrossingPrefabs crossingPrefab)
        {
            this.crossingPrefab = crossingPrefab;
            SceneObject = crossingPrefab.gameObject;
        }

        public Vector3 Position => crossingPrefab.transform.position;

        public Quaternion Rotation => crossingPrefab.transform.rotation;
    }
}
