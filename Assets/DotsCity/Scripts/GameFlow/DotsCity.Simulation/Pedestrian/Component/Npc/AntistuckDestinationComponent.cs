using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct AntistuckDestinationComponent : IComponentData
    {
        public float ActivateTimestamp;
        public float3 Destination;
        public float3 DstDirection;
        public quaternion DstRotation;
        public ActionState PreviousActionState;
        public ActionState PreviousFlags;
        public bool RotationComplete;
    }

    public struct AntistuckActivateTag : IComponentData, IEnableableComponent { }

    public struct AntistuckDeactivateTag : IComponentData, IEnableableComponent { }
}