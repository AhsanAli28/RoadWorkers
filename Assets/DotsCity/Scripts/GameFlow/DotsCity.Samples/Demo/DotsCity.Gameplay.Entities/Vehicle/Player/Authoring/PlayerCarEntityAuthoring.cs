using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Car.Authoring;
using Spirit604.DotsCity.Simulation.Common;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Player.Authoring
{
    public class PlayerCarEntityAuthoring : CarEntityAuthoringBase
    {
        [TemporaryBakingType]
        public struct PlayerCarEntityBakingTag : IComponentData { }

        public class PlayerCarEntityAuthoringBaker : Baker<PlayerCarEntityAuthoring>
        {
            public override void Bake(PlayerCarEntityAuthoring sourceAuthoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                CarEntityAuthoringBase.CarEntityAuthoringBaseBaker.Bake(this, entity, sourceAuthoring);

                Bake(this, entity);
            }

            public static void Bake(IBaker baker, Entity entity)
            {
                baker.AddComponent(entity, typeof(PlayerTag));
                baker.AddComponent(entity, typeof(PlayerPhysicsShapeComponent));
                baker.AddComponent(entity, typeof(SpeedComponent));
                baker.AddComponent(entity, typeof(PlayerCarEntityBakingTag));
                baker.AddComponent(entity, typeof(TrafficCustomRaycastTargetTag));

                baker.AddSharedComponent(entity, new WorldEntitySharedType(EntityWorldType.HybridEntity));

                baker.SetComponent(entity, new CarTypeComponent { CarType = CarType.Player });
                baker.SetComponent(entity, new FactionTypeComponent { Value = FactionType.Player });
            }
        }
    }
}