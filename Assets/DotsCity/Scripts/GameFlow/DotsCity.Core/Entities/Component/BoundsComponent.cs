using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public struct BoundsComponent : IComponentData
    {
        public Vector3 Extents;

        public Vector3 Center;

        public Bounds Bounds => new Bounds()
        {
            center = this.Center,
            extents = this.Extents
        };

        public Vector3 Size => Extents * 2;
    }
}
