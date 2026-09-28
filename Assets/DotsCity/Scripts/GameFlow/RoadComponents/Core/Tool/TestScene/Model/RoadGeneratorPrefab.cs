using UnityEngine;

namespace Spirit604.CityEditor.Road.Tests
{
    [System.Serializable]
    public class RoadGeneratorPrefab
    {
        public RoadSegmentCreator Prefab;
        public MeshRenderer RoadViewPrefab;

        [Range(0, 360f)] public float AngleOffset;
    }
}
