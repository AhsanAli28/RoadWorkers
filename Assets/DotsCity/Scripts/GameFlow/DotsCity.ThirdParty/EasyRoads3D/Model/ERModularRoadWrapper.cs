using EasyRoads3Dv3;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.RoadGeneration
{
    public class ERModularRoadWrapper : RoadWrapperBase, ISplineRoad
    {
        private readonly ERModularRoad modularRoad;

        public ERModularRoadWrapper(ERModularRoad modularRoad)
        {
            this.modularRoad = modularRoad;
            SceneObject = modularRoad.gameObject;
        }

        public Vector3 Position => modularRoad.GetComponent<MeshFilter>().sharedMesh.bounds.center;

        public Quaternion Rotation => Quaternion.identity;

        public float Width => modularRoad.GetRoadWidth();

        public int LaneCount => 0;

        public List<Vector3> Points => modularRoad.splinePoints;

        public Component StartConnectionObject => modularRoad.startPrefabScript?.crossingsScript ?? null;

        public Component EndConnectionObject => modularRoad.endPrefabScript?.crossingsScript ?? null;

        public bool IsAvailable
        {
            get
            {
                var meshFilter = modularRoad.GetComponent<MeshFilter>();

                if (meshFilter && meshFilter.sharedMesh)
                {
                    return meshFilter.sharedMesh.vertexCount > 0;
                }

                return true;
            }
        }
    }
}
