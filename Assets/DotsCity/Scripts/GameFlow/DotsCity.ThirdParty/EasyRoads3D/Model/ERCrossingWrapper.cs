using EasyRoads3Dv3;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.RoadGeneration
{
    public class ERCrossingWrapper : RoadWrapperBase, ICrossingRoad
    {
        private readonly ERCrossings crossings;

        public ERCrossingWrapper(ERCrossings crossings)
        {
            this.crossings = crossings;
            SceneObject = crossings.gameObject;
        }

        public Vector3 Position => crossings.transform.position + crossings.crossPointCenter;

        public Quaternion Rotation => crossings.transform.rotation;

        public bool Equals(ICrossingRoad other) => this.crossings == ((ERCrossingWrapper)other).crossings;

        public override int GetHashCode() => crossings.GetHashCode();
    }
}
