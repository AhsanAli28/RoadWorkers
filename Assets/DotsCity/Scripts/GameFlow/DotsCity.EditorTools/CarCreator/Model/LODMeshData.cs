using System;
using UnityEngine;

namespace Spirit604.DotsCity.EditorTools
{
    [System.Serializable]
    public class LODMeshData
    {
        [NonSerialized]
        public MeshRenderer MeshRenderer;
        public Mesh Mesh;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;

        public LODMeshData(Mesh mesh)
        {
            Mesh = mesh;
        }

        public LODMeshData(MeshFilter meshFilter)
        {
            Mesh = meshFilter.sharedMesh;
            LocalPosition = meshFilter.transform.position;
            LocalRotation = meshFilter.transform.rotation;
        }

        public LODMeshData(Transform parent, MeshFilter meshFilter)
        {
            Mesh = meshFilter.sharedMesh;
            LocalPosition = parent.InverseTransformPoint(meshFilter.transform.position);
            LocalRotation = Quaternion.Inverse(parent.rotation) * meshFilter.transform.rotation;
        }

        public LODMeshData(Transform parent, MeshRenderer meshRenderer)
        {
            this.MeshRenderer = meshRenderer;
            var meshFilter = meshRenderer.GetComponent<MeshFilter>();
            Mesh = meshFilter.sharedMesh;
            LocalPosition = parent.InverseTransformPoint(meshFilter.transform.position);
            LocalRotation = Quaternion.Inverse(parent.rotation) * meshFilter.transform.rotation;
        }
    }
}