using System;
using Unity.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Common
{
    // Copied from Unity.Rendering.RenderMesh
    public struct MyRenderMesh : ISharedComponentData, IEquatable<MyRenderMesh>
    {
        public Mesh mesh;
        public Material material;
        public int subMesh;

        /// <summary>
        /// Two RenderMesh objects are equal if their respective property values are equal.
        /// </summary>
        /// <param name="other">Another RenderMesh.</param>
        /// <returns>True, if the properties of both RenderMeshes are equal.</returns>
        public bool Equals(MyRenderMesh other)
        {
            return
                mesh == other.mesh &&
                material == other.material &&
                subMesh == other.subMesh;
        }

        /// <summary>
        /// A representative hash code.
        /// </summary>
        /// <returns>A number that is guaranteed to be the same when generated from two objects that are the same.</returns>
        public override int GetHashCode()
        {
            int hash = 0;

            unsafe
            {
                var buffer = stackalloc[]
                {
                    ReferenceEquals(mesh, null) ? 0 : mesh.GetHashCode(),
                    ReferenceEquals(material, null) ? 0 : material.GetHashCode(),
                    subMesh.GetHashCode(),
                };

                hash = (int)XXHash.Hash32((byte*)buffer, 3 * 4);
            }

            return hash;
        }
    }
}
