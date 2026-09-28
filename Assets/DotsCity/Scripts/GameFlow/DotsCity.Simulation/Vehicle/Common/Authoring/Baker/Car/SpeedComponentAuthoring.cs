using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    public class SpeedComponentAuthoring : MonoBehaviour
    {
        [Tooltip("Speed limit in kilometers per hour. -1 => no limit, 0 => default lane speed")]
        [SerializeField]
        private float limit = -1;

        class SpeedComponentAuthoringBaker : Baker<SpeedComponentAuthoring>
        {
            public override void Bake(SpeedComponentAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent(entity, SpeedComponent.SetSpeedLimitByKmh(authoring.limit));
            }
        }
    }
}
