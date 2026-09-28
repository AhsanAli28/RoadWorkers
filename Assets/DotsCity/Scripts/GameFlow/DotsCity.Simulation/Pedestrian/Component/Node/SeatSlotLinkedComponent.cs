using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct SeatSlotLinkedComponent : ICleanupComponentData
    {
        public float DeactivateTimestamp;
        public bool Exited;
        public SitState SitState;
        public Entity SeatEntity;
        public int SeatIndex;
        public float3 SeatPosition;
        public quaternion SeatRotation;
        public float3 EnterSeatPosition;
    }
}
