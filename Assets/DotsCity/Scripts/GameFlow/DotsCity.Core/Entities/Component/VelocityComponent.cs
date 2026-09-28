using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public struct VelocityComponent : IComponentData
    {
        public Vector3 Value;
    }
}