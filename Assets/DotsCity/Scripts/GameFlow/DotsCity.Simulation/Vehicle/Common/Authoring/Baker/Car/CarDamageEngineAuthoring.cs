using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    public class CarDamageEngineAuthoring : MonoBehaviour
    {
        [SerializeField] private Vector3 engineDamageVfxOffset = new Vector3(0, 1.2f, 1.5f);

        class CarDamageEngineAuthoringBaker : Baker<CarDamageEngineAuthoring>
        {
            public override void Bake(CarDamageEngineAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new EngineDamageBakingData() { CurrentState = -1, SpawnOffset = authoring.engineDamageVfxOffset });
            }
        }
    }
}