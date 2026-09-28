using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.RoadGeneration
{
    public interface IRoadObject
    {
        public GameObject SceneObject { get; }
        public string Name { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
    }
}
