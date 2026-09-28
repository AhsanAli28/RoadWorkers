using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Config
{
    public struct CommonGeneralSettingsData
    {
        public bool BulletSupport;
        public bool PropsPhysics;
        public bool HealthSupport;
        public bool PropsDamageSupport;
    }

    public struct CommonGeneralSettingsReference : IComponentData
    {
        public BlobAssetReference<CommonGeneralSettingsData> Config;
    }
}
