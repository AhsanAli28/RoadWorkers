using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    public struct StateComponent : IComponentData
    {
        public ActionState ActionState;
        public ActionState AdditiveStateFlags;
        public MovementState MovementState;
        public MovementState PreviousMovementState;
    }
}