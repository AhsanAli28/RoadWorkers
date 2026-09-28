using Spirit604.DotsCity.Core;
using Spirit604.Gameplay.Road;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    [TemporaryBakingType]
    public struct TrafficLightHandlerBakingData : IComponentData
    {
        public Entity CrossroadEntity;
    }

    public class TrafficLightHandlerBaker : Baker<TrafficLightHandler>
    {
        public override void Bake(TrafficLightHandler authoring)
        {
            var entity = this.CreateAdditionalEntityWithBakerRef(authoring.gameObject, TransformUsageFlags.None, false);
            var crossroadEntity = Entity.Null;

            if (authoring.TrafficLightCrossroad != null)
            {
                crossroadEntity = GetEntity(authoring.TrafficLightCrossroad.gameObject, TransformUsageFlags.Dynamic);
            }

            AddComponent(entity, new TrafficLightHandlerBakingData()
            {
                CrossroadEntity = crossroadEntity
            });
        }
    }
}