using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct DestinationComponent : IComponentData
    {
        public float3 Value;
        public float3 PreviousDestination;
        public Entity DestinationNode;
        public Entity PreviuosDestinationNode;
        public Entity DestinationLightEntity;
        public Entity PreviousLightEntity;
        public float CustomAchieveDistance;
        public float CustomAchieveDistanceSQ;

        public DestinationComponent SwapBack()
        {
            return new DestinationComponent()
            {
                Value = this.PreviousDestination,
                PreviousDestination = this.Value,
                DestinationNode = this.PreviuosDestinationNode,
                PreviuosDestinationNode = this.DestinationNode,
                DestinationLightEntity = this.PreviousLightEntity,
                PreviousLightEntity = this.DestinationLightEntity
            };
        }
    }
}