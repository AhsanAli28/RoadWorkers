using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct TalkComponent : IComponentData
    {
        public double SwitchTalkingTime;
        public double StopTalkingTime;
    }
}