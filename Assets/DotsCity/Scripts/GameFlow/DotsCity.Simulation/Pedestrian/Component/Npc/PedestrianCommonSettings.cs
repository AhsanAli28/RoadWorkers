using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct PedestrianCommonSettings : IComponentData
    {
        public int SkinIndex;
        public float LoadSkinTimestamp;
    }
}