using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    public class CarPoliceEntityAuthoring : CarEntityAuthoringBase
    {
        class PoliceCarEntityAuthoringBaseBaker : CarEntityAuthoringBaseBaker
        {
            public override void Bake(CarEntityAuthoringBase sourceAuthoring)
            {
                var entity = GetEntity(sourceAuthoring.gameObject, TransformUsageFlags.Dynamic);
                base.Bake(sourceAuthoring);
                AddComponent(entity, typeof(CarPoliceTag));
                AddComponent(entity, typeof(TrafficCustomRaycastTargetTag));

                SetComponent(entity, new CarTypeComponent { CarType = CarType.Police });
                //SetComponent(entity, new FactionTypeComponent { Value = FactionType.City });
            }
        }
    }
}