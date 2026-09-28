using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player
{
    public class PlayerTrafficEntityControlAuthoring : MonoBehaviour, ICustomTrafficCar
    {
        public bool CustomHandling => true;

        class PlayerTrafficEntityControlAuthoringBaker : Baker<PlayerTrafficEntityControlAuthoring>
        {
            public override void Bake(PlayerTrafficEntityControlAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent(entity, typeof(TrafficPlayerControlTag));
                AddComponent(entity, typeof(TrafficPlayerControlInitTag));
            }
        }
    }
}
