using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    public class PathGraphEntityAuthoring : MonoBehaviour
    {
        public class PathGraphEntityBaker : Baker<PathGraphEntityAuthoring>
        {
            public override void Bake(PathGraphEntityAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(entity, typeof(PathGraphReference));
            }
        }
    }
}