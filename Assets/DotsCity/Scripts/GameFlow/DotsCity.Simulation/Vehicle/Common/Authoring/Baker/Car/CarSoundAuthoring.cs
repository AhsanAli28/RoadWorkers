using Spirit604.DotsCity.Simulation.Sound;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Sound.Authoring
{
    public class CarSoundAuthoring : MonoBehaviour
    {
        class CarSoundAuthoringBaker : Baker<CarSoundAuthoring>
        {
            public override void Bake(CarSoundAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent(entity, new CarSoundData()
                {
                });

                AddComponent<CarUpdateSound>(entity);
                AddComponent<HasSoundTag>(entity);

                AddComponent(entity, new CarHornComponent()
                {
                });

                //TemporaryBakingType
                AddComponent<CarSoundEntityBakingTag>(entity);
            }
        }
    }
}