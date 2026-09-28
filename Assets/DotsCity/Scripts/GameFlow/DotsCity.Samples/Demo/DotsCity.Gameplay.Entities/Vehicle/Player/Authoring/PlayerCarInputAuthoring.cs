using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player.Authoring
{
    public class PlayerCarInputAuthoring : MonoBehaviour
    {
        class PlayerCarInputAuthoringBaker : Baker<PlayerCarInputAuthoring>
        {
            public override void Bake(PlayerCarInputAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent(entity, new PlayerTag());
                AddComponent(entity, new CarEngineStartedTag());
                AddComponent(entity, new VehicleInputReader());
                AddComponent(entity, new HasDriverTag());
            }
        }
    }
}
