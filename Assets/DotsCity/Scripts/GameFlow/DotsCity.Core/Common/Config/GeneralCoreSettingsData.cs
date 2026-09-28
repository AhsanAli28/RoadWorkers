using Unity.Entities;
using Unity.Physics;

namespace Spirit604.DotsCity.Core
{
    public struct GeneralCoreSettingsData
    {
        public WorldSimulationType WorldSimulationType;
        public SimulationType SimulationType;
        public bool CullPhysics;
        public bool CullStaticPhysics;

        public bool DOTSPhysics => DOTSSimulation && SimulationType == SimulationType.UnityPhysics;
        public bool DOTSSimulation => WorldSimulationType == WorldSimulationType.DOTS;
    }

    public struct GeneralCoreSettingsDataReference : IComponentData
    {
        public BlobAssetReference<GeneralCoreSettingsData> Config;
    }
}
