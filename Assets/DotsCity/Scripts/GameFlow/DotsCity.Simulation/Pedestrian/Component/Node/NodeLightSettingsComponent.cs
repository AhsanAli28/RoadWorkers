using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct NodeLightSettingsComponent : IComponentData
    {
        public Entity LightEntity;
        public bool HasCrosswalk;
        public int CrosswalkIndex;

        public bool IsCrosswalk(NodeLightSettingsComponent dst) =>
            this.HasCrosswalk && dst.HasCrosswalk &&
            this.LightEntity == dst.LightEntity &&
            this.CrosswalkIndex != -1 &&
            this.CrosswalkIndex == dst.CrosswalkIndex;
    }
}