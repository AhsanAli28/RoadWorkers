using System;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    [Serializable]
    public class MeshData
    {
        public Mesh Mesh { get; set; }
        public Material Material { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
    }
}
