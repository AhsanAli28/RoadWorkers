using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct NodeSeatSettingsComponent : IComponentData
    {
        public float3 InitialPosition;
        public quaternion InitialRotation;
        public int SeatsCount;
        public float3 BaseOffset;
        public float SeatOffset;
        public float EnterSeatOffset;
        public float SeatHeight;
        public bool RevertSeatDirection;
    }
}
