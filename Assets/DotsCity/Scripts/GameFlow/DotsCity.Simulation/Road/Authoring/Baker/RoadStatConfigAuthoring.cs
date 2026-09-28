using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Road
{
    public class RoadStatConfigAuthoring : MonoBehaviour
    {
        public class RoadStatConfigBaker : Baker<RoadStatConfigAuthoring>
        {
            public override void Bake(RoadStatConfigAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.None);

                AddComponent<RoadStatConfig>(entity);
            }
        }
    }
}
