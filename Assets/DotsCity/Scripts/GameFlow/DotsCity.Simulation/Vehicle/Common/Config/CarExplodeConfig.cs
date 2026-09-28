using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car
{
    public struct CarExplodeConfig
    {
        public float InitialYForce;
        public float InitialForwardForce;
        public float VelocityMultiplier;
        public float ApplyForceOffset;
        public float SourceMass;

        public bool ApplyAngularForce;
        public float ConstantAngularForce;
        public float InitialAngularForce;
        public float ApplyAngularForceTime;
    }

    public struct CarExplodeConfigReference : IComponentData
    {
        public BlobAssetReference<CarExplodeConfig> Config;
    }
}